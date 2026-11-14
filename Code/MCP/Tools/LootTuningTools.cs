using System.ComponentModel;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using RPGGame.MCP.Tools.LootTuning;

namespace RPGGame.MCP.Tools
{
    /// <summary>
    /// Facade for the MCP loot tools: rarity table read-out, item generation (same roller as dungeon loot) and analysis.
    /// </summary>
    public static class LootTuningTools
    {
        // ============ RARITY ============

        [McpServerTool(Name = "get_rarity_distribution", Title = "Get Rarity Distribution")]
        [Description("Gets current rarity weights and calculates probability percentages.")]
        public static Task<string> GetRarityDistribution()
        {
            return RarityTuningTools.GetRarityDistribution();
        }

        // ============ ITEM GENERATION ============

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
