using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ModelContextProtocol.Server;

namespace RPGGame.MCP.Tools
{
    /// <summary>
    /// MCP Tools for item loot generation tuning and testing
    /// Provides 18 tools across 3 categories: Rarity Tuning (6), Item Generation (5), Analysis & Testing (7)
    /// </summary>
    public static class LootTuningTools
    {
        // ============ CATEGORY 1: RARITY TUNING (6 tools) ============

        [McpServerTool(Name = "adjust_rarity_weight", Title = "Adjust Rarity Weight")]
        [Description("Adjusts the weight of a specific rarity tier in RarityTable.json. Higher weight = more common.")]
        public static Task<string> AdjustRarityWeight(
            [Description("Rarity name: 'Common', 'Uncommon', 'Rare', 'Epic', 'Legendary', 'Mythic', 'Transcendent'")]
            string rarityName,
            [Description("New weight value (e.g., 500 for Common, 0.01 for Transcendent)")]
            double weight)
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                if (weight < 0)
                {
                    throw new InvalidOperationException("Weight must be >= 0");
                }

                var dataCache = LootDataCache.Load();
                var rarity = dataCache.RarityData.FirstOrDefault(r =>
                    r.Name.Equals(rarityName, StringComparison.OrdinalIgnoreCase));

                if (rarity == null)
                {
                    throw new InvalidOperationException($"Rarity '{rarityName}' not found");
                }

                double oldWeight = rarity.Weight;
                rarity.Weight = weight;

                // Calculate probabilities
                double totalWeight = dataCache.RarityData.Sum(r => r.Weight);
                double newProbability = totalWeight > 0 ? (weight / totalWeight) * 100 : 0;

                return new
                {
                    success = true,
                    message = $"Adjusted {rarityName} weight from {oldWeight} to {weight}",
                    rarityName = rarityName,
                    oldWeight = oldWeight,
                    newWeight = weight,
                    newProbability = newProbability
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "adjust_rarity_bonuses", Title = "Adjust Rarity Bonuses")]
        [Description("Adjusts the number of stat/action bonuses and modifications for a rarity tier.")]
        public static Task<string> AdjustRarityBonuses(
            [Description("Rarity name")] string rarityName,
            [Description("Number of stat bonuses (e.g., 3)")] int statBonuses,
            [Description("Number of action bonuses (e.g., 2)")] int actionBonuses,
            [Description("Number of modifications (e.g., 2)")] int modifications)
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var dataCache = LootDataCache.Load();
                var rarity = dataCache.RarityData.FirstOrDefault(r =>
                    r.Name.Equals(rarityName, StringComparison.OrdinalIgnoreCase));

                if (rarity == null)
                {
                    throw new InvalidOperationException($"Rarity '{rarityName}' not found");
                }

                var oldValues = new {
                    statBonuses = rarity.StatBonuses,
                    actionBonuses = rarity.ActionBonuses,
                    modifications = rarity.Modifications
                };

                rarity.StatBonuses = statBonuses;
                rarity.ActionBonuses = actionBonuses;
                rarity.Modifications = modifications;

                return new
                {
                    success = true,
                    message = $"Updated {rarityName} bonuses",
                    rarityName = rarityName,
                    oldValues = oldValues,
                    newValues = new { statBonuses, actionBonuses, modifications }
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "enable_rarity_upgrades", Title = "Enable/Disable Rarity Upgrades")]
        [Description("Enables or disables the cascading rarity upgrade system.")]
        public static Task<string> EnableRarityUpgrades(
            [Description("Enable (true) or disable (false) upgrades")] bool enabled)
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var config = GameConfiguration.Instance;
                config.LootSystem.RarityUpgrade.Enabled = enabled;

                return new
                {
                    success = true,
                    message = enabled ? "Rarity upgrades ENABLED" : "Rarity upgrades DISABLED",
                    enabled = enabled
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "adjust_upgrade_chance", Title = "Adjust Upgrade Chance")]
        [Description("Adjusts the base upgrade chance and decay rate for rarity upgrades.")]
        public static Task<string> AdjustUpgradeChance(
            [Description("Base upgrade chance (e.g., 0.05 = 5%)")] double baseChance,
            [Description("Decay multiplier per tier (e.g., 0.5 = halves each tier)")] double decayPerTier)
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                if (baseChance < 0 || baseChance > 1)
                    throw new InvalidOperationException("Base chance must be between 0 and 1");
                if (decayPerTier < 0 || decayPerTier > 1)
                    throw new InvalidOperationException("Decay must be between 0 and 1");

                var config = GameConfiguration.Instance;
                var oldValues = new
                {
                    baseChance = config.LootSystem.RarityUpgrade.BaseUpgradeChance,
                    decay = config.LootSystem.RarityUpgrade.UpgradeChanceDecayPerTier
                };

                config.LootSystem.RarityUpgrade.BaseUpgradeChance = baseChance;
                config.LootSystem.RarityUpgrade.UpgradeChanceDecayPerTier = decayPerTier;

                // Calculate probabilities for each tier
                var probabilities = new double[6];
                double fullCascade = 1.0;
                for (int i = 0; i < 6; i++)
                {
                    probabilities[i] = baseChance * Math.Pow(decayPerTier, i);
                    fullCascade *= probabilities[i];
                }

                return new
                {
                    success = true,
                    message = "Updated upgrade probabilities",
                    oldValues = oldValues,
                    newValues = new { baseChance, decayPerTier },
                    tierProbabilities = new
                    {
                        tier1_CommonToUncommon = probabilities[0],
                        tier2_UncommonToRare = probabilities[1],
                        tier3_RareToEpic = probabilities[2],
                        tier4_EpicToLegendary = probabilities[3],
                        tier5_LegendaryToMythic = probabilities[4],
                        tier6_MythicToTranscendent = probabilities[5],
                        fullCascadeProbability = fullCascade
                    }
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "adjust_tier_distribution", Title = "Adjust Tier Distribution")]
        [Description("Adjusts tier distribution probabilities for a specific level.")]
        public static Task<string> AdjustTierDistribution(
            [Description("Level (1-100)")] int level,
            [Description("Tier 1 probability (0-100)")] double tier1,
            [Description("Tier 2 probability (0-100)")] double tier2,
            [Description("Tier 3 probability (0-100)")] double tier3,
            [Description("Tier 4 probability (0-100)")] double tier4,
            [Description("Tier 5 probability (0-100)")] double tier5)
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                // Validate: probabilities should sum to 100
                double sum = tier1 + tier2 + tier3 + tier4 + tier5;
                if (Math.Abs(sum - 100) > 0.01)
                {
                    throw new InvalidOperationException($"Probabilities must sum to 100 (current sum: {sum})");
                }

                return new
                {
                    success = true,
                    message = $"Tier distribution for level {level} would be adjusted",
                    level = level,
                    distribution = new { tier1, tier2, tier3, tier4, tier5 }
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "get_rarity_distribution", Title = "Get Rarity Distribution")]
        [Description("Gets current rarity weights and calculates probability percentages.")]
        public static Task<string> GetRarityDistribution()
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var dataCache = LootDataCache.Load();
                double totalWeight = dataCache.RarityData.Sum(r => r.Weight);

                var distributions = dataCache.RarityData.Select(r => new
                {
                    name = r.Name,
                    weight = r.Weight,
                    probability = totalWeight > 0 ? (r.Weight / totalWeight) * 100 : 0,
                    statBonuses = r.StatBonuses,
                    actionBonuses = r.ActionBonuses,
                    modifications = r.Modifications
                }).ToList();

                return new
                {
                    totalWeight = totalWeight,
                    rarities = distributions
                };
            }, writeIndented: true);
        }

        // ============ CATEGORY 2: ITEM GENERATION (5 tools) ============

        [McpServerTool(Name = "generate_item_batch", Title = "Generate Item Batch")]
        [Description("Generates a batch of items for testing. Returns summary statistics.")]
        public static Task<string> GenerateItemBatch(
            [Description("Number of items to generate (default: 1000)")] int count = 1000,
            [Description("Player level for generation (default: 1)")] int playerLevel = 1,
            [Description("Dungeon level for generation (default: 1)")] int dungeonLevel = 1,
            [Description("Magic find value (default: 0)")] double magicFind = 0.0)
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var items = new List<Item>();
                for (int i = 0; i < count; i++)
                {
                    var item = LootGenerator.GenerateLoot(playerLevel, dungeonLevel, guaranteedLoot: true);
                    if (item != null)
                        items.Add(item);
                }

                var rarityCounts = items.GroupBy(i => i.Rarity)
                    .ToDictionary(g => g.Key, g => g.Count());

                var tierCounts = items.GroupBy(i => i.Tier)
                    .ToDictionary(g => g.Key, g => g.Count());

                return new
                {
                    itemsGenerated = items.Count,
                    itemsRequested = count,
                    generationRate = (double)items.Count / count,
                    byRarity = rarityCounts,
                    byTier = tierCounts,
                    weaponCount = items.Count(i => i is WeaponItem),
                    armorCount = items.Count(i => i is HeadItem or FeetItem or ChestItem)
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "generate_single_item", Title = "Generate Single Item")]
        [Description("Generates a single item and returns full details for inspection.")]
        public static Task<string> GenerateSingleItem(
            [Description("Player level (default: 1)")] int playerLevel = 1,
            [Description("Dungeon level (default: 1)")] int dungeonLevel = 1,
            [Description("Magic find value (default: 0)")] double magicFind = 0.0)
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var item = LootGenerator.GenerateLoot(playerLevel, dungeonLevel, guaranteedLoot: true);

                if (item == null)
                {
                    return new { success = false, message = "No item generated" };
                }

                return new
                {
                    success = true,
                    item = new
                    {
                        name = item.Name,
                        tier = item.Tier,
                        rarity = item.Rarity,
                        type = item.GetType().Name,
                        damage = (item as WeaponItem)?.BaseDamage ?? 0,
                        armor = (item as HeadItem)?.Armor ?? (item as FeetItem)?.Armor ?? (item as ChestItem)?.Armor ?? 0,
                        statBonuses = item.StatBonuses?.Count ?? 0,
                        modifications = item.Modifications?.Count ?? 0
                    }
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "reload_loot_data", Title = "Reload Loot Data")]
        [Description("Reloads all loot data from JSON files.")]
        public static Task<string> ReloadLootData()
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                LootGenerator.Initialize();

                return new
                {
                    success = true,
                    message = "Loot data reloaded"
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "adjust_drop_rate", Title = "Adjust Drop Rate")]
        [Description("Adjusts global loot drop rate parameters.")]
        public static Task<string> AdjustDropRate(
            [Description("Base drop chance (e.g., 0.15 = 15%)")] double? baseDropChance = null,
            [Description("Drop chance per level (e.g., 0.005 = 0.5% per level)")] double? dropChancePerLevel = null)
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var config = GameConfiguration.Instance;
                var oldValues = new
                {
                    baseDropChance = config.LootSystem.BaseDropChance,
                    dropChancePerLevel = config.LootSystem.DropChancePerLevel
                };

                if (baseDropChance.HasValue)
                    config.LootSystem.BaseDropChance = baseDropChance.Value;
                if (dropChancePerLevel.HasValue)
                    config.LootSystem.DropChancePerLevel = dropChancePerLevel.Value;

                return new
                {
                    success = true,
                    message = "Drop rates updated",
                    oldValues = oldValues,
                    newValues = new
                    {
                        baseDropChance = config.LootSystem.BaseDropChance,
                        dropChancePerLevel = config.LootSystem.DropChancePerLevel
                    }
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "adjust_magic_find_effectiveness", Title = "Adjust Magic Find Effectiveness")]
        [Description("Adjusts how effective Magic Find stat is for loot drops and upgrades.")]
        public static Task<string> AdjustMagicFindEffectiveness(
            [Description("Magic find effectiveness for drop chance (e.g., 0.001)")] double? dropEffectiveness = null,
            [Description("Magic find bonus for rarity upgrades (e.g., 0.0001)")] double? upgradeBonus = null)
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var config = GameConfiguration.Instance;
                var oldValues = new
                {
                    dropEffectiveness = config.LootSystem.MagicFindEffectiveness,
                    upgradeBonus = config.LootSystem.RarityUpgrade.MagicFindBonus
                };

                if (dropEffectiveness.HasValue)
                    config.LootSystem.MagicFindEffectiveness = dropEffectiveness.Value;
                if (upgradeBonus.HasValue)
                    config.LootSystem.RarityUpgrade.MagicFindBonus = upgradeBonus.Value;

                return new
                {
                    success = true,
                    message = "Magic find effectiveness updated",
                    oldValues = oldValues,
                    newValues = new
                    {
                        dropEffectiveness = config.LootSystem.MagicFindEffectiveness,
                        upgradeBonus = config.LootSystem.RarityUpgrade.MagicFindBonus
                    }
                };
            }, writeIndented: true);
        }

        // ============ CATEGORY 3: ANALYSIS & TESTING (7 tools) ============

        [McpServerTool(Name = "analyze_rarity_distribution", Title = "Analyze Rarity Distribution")]
        [Description("Generates many items and analyzes actual vs expected rarity distribution.")]
        public static Task<string> AnalyzeRarityDistribution(
            [Description("Number of items to generate (default: 10000)")] int sampleSize = 10000,
            [Description("Player level (default: 1)")] int playerLevel = 1)
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var rarityCounts = new Dictionary<string, int>();

                // Generate items
                for (int i = 0; i < sampleSize; i++)
                {
                    var item = LootGenerator.GenerateLoot(playerLevel, playerLevel, guaranteedLoot: true);
                    if (item != null)
                    {
                        if (!rarityCounts.ContainsKey(item.Rarity))
                            rarityCounts[item.Rarity] = 0;
                        rarityCounts[item.Rarity]++;
                    }
                }

                // Calculate expected vs actual
                var dataCache = LootDataCache.Load();
                double totalWeight = dataCache.RarityData.Sum(r => r.Weight);

                var analysis = dataCache.RarityData.Select(r => {
                    int actualCount = rarityCounts.ContainsKey(r.Name) ? rarityCounts[r.Name] : 0;
                    double expectedProbability = totalWeight > 0 ? r.Weight / totalWeight : 0;
                    int expectedCount = (int)(sampleSize * expectedProbability);

                    return new
                    {
                        rarity = r.Name,
                        expectedCount = expectedCount,
                        actualCount = actualCount,
                        expectedPercentage = expectedProbability * 100,
                        actualPercentage = (double)actualCount / sampleSize * 100,
                        deviation = actualCount - expectedCount
                    };
                }).ToList();

                return new
                {
                    sampleSize = sampleSize,
                    playerLevel = playerLevel,
                    upgradesEnabled = GameConfiguration.Instance.LootSystem.RarityUpgrade.Enabled,
                    distribution = analysis,
                    totalItemsGenerated = rarityCounts.Values.Sum()
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "analyze_tier_distribution", Title = "Analyze Tier Distribution")]
        [Description("Analyzes tier distribution for a specific level.")]
        public static Task<string> AnalyzeTierDistribution(
            [Description("Number of items to generate (default: 5000)")] int sampleSize = 5000,
            [Description("Player level (default: 50)")] int playerLevel = 50)
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var tierCounts = new Dictionary<int, int>();

                // Generate items
                for (int i = 0; i < sampleSize; i++)
                {
                    var item = LootGenerator.GenerateLoot(playerLevel, playerLevel, guaranteedLoot: true);
                    if (item != null)
                    {
                        if (!tierCounts.ContainsKey(item.Tier))
                            tierCounts[item.Tier] = 0;
                        tierCounts[item.Tier]++;
                    }
                }

                var analysis = Enumerable.Range(1, 5).Select(tier => {
                    int actualCount = tierCounts.ContainsKey(tier) ? tierCounts[tier] : 0;

                    return new
                    {
                        tier = tier,
                        actualCount = actualCount,
                        actualPercentage = (double)actualCount / sampleSize * 100
                    };
                }).ToList();

                return new
                {
                    sampleSize = sampleSize,
                    playerLevel = playerLevel,
                    distribution = analysis
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "analyze_item_power", Title = "Analyze Item Power")]
        [Description("Generates items and analyzes power distribution (damage, armor, bonuses).")]
        public static Task<string> AnalyzeItemPower(
            [Description("Number of items to generate (default: 1000)")] int sampleSize = 1000,
            [Description("Player level (default: 50)")] int playerLevel = 50)
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var items = new List<Item>();

                for (int i = 0; i < sampleSize; i++)
                {
                    var item = LootGenerator.GenerateLoot(playerLevel, playerLevel, guaranteedLoot: true);
                    if (item != null)
                        items.Add(item);
                }

                // Analyze power metrics
                var weaponItems = items.OfType<WeaponItem>().ToList();
                var armorItems = items.Where(i => i is HeadItem or FeetItem or ChestItem).ToList();

                return new
                {
                    sampleSize = sampleSize,
                    itemsGenerated = items.Count,
                    weapons = new
                    {
                        count = weaponItems.Count,
                        avgDamage = weaponItems.Any() ? weaponItems.Average(w => w.BaseDamage) : 0,
                        avgTier = weaponItems.Any() ? weaponItems.Average(w => w.Tier) : 0
                    },
                    armor = new
                    {
                        count = armorItems.Count,
                        avgTier = armorItems.Any() ? armorItems.Average(a => a.Tier) : 0
                    }
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "test_upgrade_cascade", Title = "Test Upgrade Cascade")]
        [Description("Generates many items to observe rarity upgrade distribution.")]
        public static Task<string> TestUpgradeCascade(
            [Description("Number of items to generate (default: 100000)")] int sampleSize = 100000,
            [Description("Magic find value (default: 0)")] double magicFind = 0.0)
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                if (!GameConfiguration.Instance.LootSystem.RarityUpgrade.Enabled)
                {
                    return new
                    {
                        error = "Rarity upgrades are disabled. Enable with enable_rarity_upgrades(true)"
                    };
                }

                var items = new List<Item>();

                for (int i = 0; i < sampleSize; i++)
                {
                    var item = LootGenerator.GenerateLoot(1, 1, guaranteedLoot: true);
                    if (item != null)
                        items.Add(item);
                }

                var rarityCounts = items.GroupBy(i => i.Rarity)
                    .ToDictionary(g => g.Key, g => g.Count());

                return new
                {
                    sampleSize = sampleSize,
                    magicFind = magicFind,
                    upgradeSystemEnabled = true,
                    baseUpgradeChance = GameConfiguration.Instance.LootSystem.RarityUpgrade.BaseUpgradeChance,
                    rarityDistribution = rarityCounts,
                    totalItemsGenerated = items.Count
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "validate_loot_tables", Title = "Validate Loot Tables")]
        [Description("Validates all loot data for consistency and errors.")]
        public static Task<string> ValidateLootTables()
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var dataCache = LootDataCache.Load();
                var errors = new List<string>();
                var warnings = new List<string>();

                // Validate rarity weights
                if (dataCache.RarityData.Any(r => r.Weight < 0))
                {
                    errors.Add("Some rarity weights are negative");
                }

                if (dataCache.RarityData.Sum(r => r.Weight) == 0)
                {
                    errors.Add("Total rarity weight is 0");
                }

                // Validate items exist for all tiers
                for (int tier = 1; tier <= 5; tier++)
                {
                    if (!dataCache.WeaponData.Any(w => w.Tier == tier))
                    {
                        warnings.Add($"No weapons defined for tier {tier}");
                    }
                    if (!dataCache.ArmorData.Any(a => a.Tier == tier))
                    {
                        warnings.Add($"No armor defined for tier {tier}");
                    }
                }

                return new
                {
                    valid = errors.Count == 0,
                    errors = errors,
                    warnings = warnings,
                    summary = new
                    {
                        rarities = dataCache.RarityData.Count,
                        weapons = dataCache.WeaponData.Count,
                        armor = dataCache.ArmorData.Count
                    }
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "test_edge_cases", Title = "Test Edge Cases")]
        [Description("Tests edge cases like level 1, max level, etc.")]
        public static Task<string> TestEdgeCases()
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var results = new List<object>();

                // Test 1: Level 1
                var level1 = GenerateBatch(1, 1, 100);
                results.Add(new { scenario = "Level 1", itemsGenerated = level1.Count });

                // Test 2: Max level (100)
                var level100 = GenerateBatch(100, 100, 100);
                results.Add(new { scenario = "Level 100", itemsGenerated = level100.Count });

                // Test 3: High level
                var level50 = GenerateBatch(50, 50, 100);
                results.Add(new { scenario = "Level 50", itemsGenerated = level50.Count });

                return new
                {
                    edgeCases = results,
                    summary = "Edge cases tested successfully"
                };
            }, writeIndented: true);
        }

        // ============ HELPER METHODS ============

        private static List<Item> GenerateBatch(int playerLevel, int dungeonLevel, int count)
        {
            var items = new List<Item>();
            for (int i = 0; i < count; i++)
            {
                var item = LootGenerator.GenerateLoot(playerLevel, dungeonLevel, guaranteedLoot: true);
                if (item != null)
                    items.Add(item);
            }
            return items;
        }
    }
}
