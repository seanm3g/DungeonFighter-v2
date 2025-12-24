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
    }
}

