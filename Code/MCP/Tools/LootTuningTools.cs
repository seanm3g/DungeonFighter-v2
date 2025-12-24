using System.ComponentModel;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using RPGGame.MCP.Tools.LootTuning;

namespace RPGGame.MCP.Tools
{
    /// <summary>
    /// Facade for MCP Tools for item loot generation tuning and testing
    /// Provides 17 tools across 3 categories: Rarity Tuning (6), Item Generation (5), Analysis & Testing (7)
    /// 
    /// Refactored from 655 lines to ~100 lines using Facade pattern.
    /// Delegates to:
    /// - RarityTuningTools: Rarity tuning operations
    /// - ItemGenerationTools: Item generation operations
    /// - LootAnalysisTools: Analysis and testing operations
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
            return RarityTuningTools.AdjustRarityWeight(rarityName, weight);
        }

        [McpServerTool(Name = "adjust_rarity_bonuses", Title = "Adjust Rarity Bonuses")]
        [Description("Adjusts the number of stat/action bonuses and modifications for a rarity tier.")]
        public static Task<string> AdjustRarityBonuses(
            [Description("Rarity name")] string rarityName,
            [Description("Number of stat bonuses (e.g., 3)")] int statBonuses,
            [Description("Number of action bonuses (e.g., 2)")] int actionBonuses,
            [Description("Number of modifications (e.g., 2)")] int modifications)
        {
            return RarityTuningTools.AdjustRarityBonuses(rarityName, statBonuses, actionBonuses, modifications);
        }

        [McpServerTool(Name = "enable_rarity_upgrades", Title = "Enable/Disable Rarity Upgrades")]
        [Description("Enables or disables the cascading rarity upgrade system.")]
        public static Task<string> EnableRarityUpgrades(
            [Description("Enable (true) or disable (false) upgrades")] bool enabled)
        {
            return RarityTuningTools.EnableRarityUpgrades(enabled);
        }

        [McpServerTool(Name = "adjust_upgrade_chance", Title = "Adjust Upgrade Chance")]
        [Description("Adjusts the base upgrade chance and decay rate for rarity upgrades.")]
        public static Task<string> AdjustUpgradeChance(
            [Description("Base upgrade chance (e.g., 0.05 = 5%)")] double baseChance,
            [Description("Decay multiplier per tier (e.g., 0.5 = halves each tier)")] double decayPerTier)
        {
            return RarityTuningTools.AdjustUpgradeChance(baseChance, decayPerTier);
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
            return RarityTuningTools.AdjustTierDistribution(level, tier1, tier2, tier3, tier4, tier5);
        }

        [McpServerTool(Name = "get_rarity_distribution", Title = "Get Rarity Distribution")]
        [Description("Gets current rarity weights and calculates probability percentages.")]
        public static Task<string> GetRarityDistribution()
        {
            return RarityTuningTools.GetRarityDistribution();
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
            return ItemGenerationTools.GenerateItemBatch(count, playerLevel, dungeonLevel, magicFind);
        }

        [McpServerTool(Name = "generate_single_item", Title = "Generate Single Item")]
        [Description("Generates a single item and returns full details for inspection.")]
        public static Task<string> GenerateSingleItem(
            [Description("Player level (default: 1)")] int playerLevel = 1,
            [Description("Dungeon level (default: 1)")] int dungeonLevel = 1,
            [Description("Magic find value (default: 0)")] double magicFind = 0.0)
        {
            return ItemGenerationTools.GenerateSingleItem(playerLevel, dungeonLevel, magicFind);
        }

        [McpServerTool(Name = "reload_loot_data", Title = "Reload Loot Data")]
        [Description("Reloads all loot data from JSON files.")]
        public static Task<string> ReloadLootData()
        {
            return ItemGenerationTools.ReloadLootData();
        }

        [McpServerTool(Name = "adjust_drop_rate", Title = "Adjust Drop Rate")]
        [Description("Adjusts global loot drop rate parameters.")]
        public static Task<string> AdjustDropRate(
            [Description("Base drop chance (e.g., 0.15 = 15%)")] double? baseDropChance = null,
            [Description("Drop chance per level (e.g., 0.005 = 0.5% per level)")] double? dropChancePerLevel = null)
        {
            return ItemGenerationTools.AdjustDropRate(baseDropChance, dropChancePerLevel);
        }

        [McpServerTool(Name = "adjust_magic_find_effectiveness", Title = "Adjust Magic Find Effectiveness")]
        [Description("Adjusts how effective Magic Find stat is for loot drops and upgrades.")]
        public static Task<string> AdjustMagicFindEffectiveness(
            [Description("Magic find effectiveness for drop chance (e.g., 0.001)")] double? dropEffectiveness = null,
            [Description("Magic find bonus for rarity upgrades (e.g., 0.0001)")] double? upgradeBonus = null)
        {
            return ItemGenerationTools.AdjustMagicFindEffectiveness(dropEffectiveness, upgradeBonus);
        }

        // ============ CATEGORY 3: ANALYSIS & TESTING (7 tools) ============

        [McpServerTool(Name = "analyze_rarity_distribution", Title = "Analyze Rarity Distribution")]
        [Description("Generates many items and analyzes actual vs expected rarity distribution.")]
        public static Task<string> AnalyzeRarityDistribution(
            [Description("Number of items to generate (default: 10000)")] int sampleSize = 10000,
            [Description("Player level (default: 1)")] int playerLevel = 1)
        {
            return LootAnalysisTools.AnalyzeRarityDistribution(sampleSize, playerLevel);
        }

        [McpServerTool(Name = "analyze_tier_distribution", Title = "Analyze Tier Distribution")]
        [Description("Analyzes tier distribution for a specific level.")]
        public static Task<string> AnalyzeTierDistribution(
            [Description("Number of items to generate (default: 5000)")] int sampleSize = 5000,
            [Description("Player level (default: 50)")] int playerLevel = 50)
        {
            return LootAnalysisTools.AnalyzeTierDistribution(sampleSize, playerLevel);
        }

        [McpServerTool(Name = "analyze_item_power", Title = "Analyze Item Power")]
        [Description("Generates items and analyzes power distribution (damage, armor, bonuses).")]
        public static Task<string> AnalyzeItemPower(
            [Description("Number of items to generate (default: 1000)")] int sampleSize = 1000,
            [Description("Player level (default: 50)")] int playerLevel = 50)
        {
            return LootAnalysisTools.AnalyzeItemPower(sampleSize, playerLevel);
        }

        [McpServerTool(Name = "test_upgrade_cascade", Title = "Test Upgrade Cascade")]
        [Description("Generates many items to observe rarity upgrade distribution.")]
        public static Task<string> TestUpgradeCascade(
            [Description("Number of items to generate (default: 100000)")] int sampleSize = 100000,
            [Description("Magic find value (default: 0)")] double magicFind = 0.0)
        {
            return LootAnalysisTools.TestUpgradeCascade(sampleSize, magicFind);
        }

        [McpServerTool(Name = "validate_loot_tables", Title = "Validate Loot Tables")]
        [Description("Validates all loot data for consistency and errors.")]
        public static Task<string> ValidateLootTables()
        {
            return LootAnalysisTools.ValidateLootTables();
        }

        [McpServerTool(Name = "test_edge_cases", Title = "Test Edge Cases")]
        [Description("Tests edge cases like level 1, max level, etc.")]
        public static Task<string> TestEdgeCases()
        {
            return LootAnalysisTools.TestEdgeCases();
        }
    }
}
