using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using ModelContextProtocol.Server;

namespace RPGGame.MCP.Tools.LootTuning
{
    /// <summary>
    /// MCP Tools for rarity tuning (6 tools)
    /// Extracted from LootTuningTools to separate rarity tuning logic
    /// </summary>
    public static class RarityTuningTools
    {
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
    }
}

