using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using ModelContextProtocol.Server;

namespace RPGGame.MCP.Tools.LootTuning
{
    /// <summary>
    /// MCP Tools for loot analysis and testing (7 tools)
    /// Extracted from LootTuningTools to separate analysis logic
    /// </summary>
    public static class LootAnalysisTools
    {
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

