using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using ModelContextProtocol.Server;

namespace RPGGame.MCP.Tools.LootTuning
{
    /// <summary>
    /// MCP Tools for item generation (5 tools)
    /// Extracted from LootTuningTools to separate item generation logic
    /// </summary>
    public static class ItemGenerationTools
    {
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
                    var item = LootGenerator.GenerateLoot(playerLevel, dungeonLevel);
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
                    armorCount = items.Count(i => i is HeadItem or ChestItem or LegsItem or FeetItem)
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
                var item = LootGenerator.GenerateLoot(playerLevel, dungeonLevel);

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
                        damage = (item as WeaponItem)?.GetTotalDamage() ?? 0,
                        armor = (item as HeadItem)?.Armor ?? (item as ChestItem)?.Armor ?? (item as LegsItem)?.Armor ?? (item as FeetItem)?.Armor ?? 0,
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

    }
}

