using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    public enum ItemGenerationItemType
    {
        Both,
        Weapons,
        Armor
    }

    public enum ItemGenerationArmorSlot
    {
        Any,
        Head,
        Chest,
        Feet
    }

    public sealed class ItemGenerationSpec
    {
        public ItemGenerationItemType ItemType { get; set; } = ItemGenerationItemType.Both;
        public string? Rarity { get; set; } = null; // null/empty => any
        public int? Tier { get; set; } = null; // null => any
        public WeaponType? WeaponType { get; set; } = null; // null => any
        public ItemGenerationArmorSlot ArmorSlot { get; set; } = ItemGenerationArmorSlot.Any;

        public int PlayerLevel { get; set; } = 1;
        public int DungeonLevel { get; set; } = 1;
        public int Seed { get; set; } = 12345;
    }

    public sealed class ItemGeneratedRow
    {
        public required Item Item { get; init; }
        public required int Index { get; init; }
        public required string SortKey { get; init; }

        public int Tier => Item.Tier;
        public string Rarity => Item.Rarity?.Trim() ?? "Common";
        public string TypeLabel => Item.Type == ItemType.Weapon
            ? $"Weapon:{Item.WeaponType}"
            : $"Armor:{Item.Type}";
        public int Damage => Item is WeaponItem w ? w.GetTotalDamage() : 0;
        public double Speed => Item is WeaponItem w ? w.GetTotalAttackSpeed() : 0;
        public int Armor => Item is HeadItem h ? h.GetTotalArmor()
            : Item is ChestItem c ? c.GetTotalArmor()
            : Item is FeetItem f ? f.GetTotalArmor()
            : 0;
        public int PrefixCount => Item.Modifications?.Count ?? 0;
        public int SuffixCount => Item.StatBonuses?.Count ?? 0;
        public int ActionBonusCount => Item.ActionBonuses?.Count ?? 0;
    }

    public static class ItemGenerationLabService
    {
        private static readonly string[] RarityOrder =
        {
            "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic", "Transcendent"
        };

        public static List<ItemGeneratedRow> GenerateBatch(ItemGenerationSpec spec, int count)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            count = Math.Clamp(count, 1, 1000);

            var cache = LootDataCache.Load();
            var rnd = new Random(spec.Seed);

            var rarity = ResolveForcedRarity(cache, spec.Rarity);
            var results = new List<ItemGeneratedRow>(count);

            for (int i = 0; i < count; i++)
            {
                var item = GenerateSingleForced(cache, rnd, spec, rarity);
                if (item == null)
                    continue;

                results.Add(new ItemGeneratedRow
                {
                    Item = item,
                    Index = i + 1,
                    SortKey = BuildSortKey(item)
                });
            }

            return results;
        }

        public static List<ItemGeneratedRow> SortBestToWorst(IReadOnlyList<ItemGeneratedRow> rows)
        {
            if (rows == null) return new List<ItemGeneratedRow>();
            return rows
                .OrderByDescending(r => IndexOfRarity(r.Rarity))
                .ThenByDescending(r => r.Tier)
                .ThenByDescending(r => r.Item is WeaponItem ? r.Damage : r.Armor)
                .ThenByDescending(r => r.Item is WeaponItem ? r.Speed : 0)
                .ThenByDescending(r => r.Item.StatBonuses?.Sum(s => s.Value) ?? 0)
                .ThenByDescending(r => (r.PrefixCount + r.SuffixCount + r.ActionBonusCount))
                .ThenBy(r => r.Item.Name ?? "", StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string BuildSortKey(Item item)
        {
            // Stable tiebreak to keep list deterministic.
            int r = IndexOfRarity(item.Rarity);
            int tier = item.Tier;
            int primary = item is WeaponItem w ? w.GetTotalDamage() : item is HeadItem h ? h.GetTotalArmor() : item is ChestItem c ? c.GetTotalArmor() : item is FeetItem f ? f.GetTotalArmor() : 0;
            double speed = item is WeaponItem ww ? ww.GetTotalAttackSpeed() : 0;
            int affix = (item.Modifications?.Count ?? 0) + (item.StatBonuses?.Count ?? 0) + (item.ActionBonuses?.Count ?? 0);
            return $"{r:D2}-{tier:D2}-{primary:D5}-{speed:0000.000}-{affix:D2}-{item.Name}";
        }

        private static RarityData ResolveForcedRarity(LootDataCache cache, string? rarityName)
        {
            if (cache.RarityData == null || cache.RarityData.Count == 0)
                return new RarityData { Name = "Common", Weight = 1, StatBonuses = 0, ActionBonuses = 0, Modifications = 0 };

            if (string.IsNullOrWhiteSpace(rarityName) || rarityName.Equals("Any", StringComparison.OrdinalIgnoreCase))
                return cache.RarityData.FirstOrDefault(r => r.Name.Equals("Common", StringComparison.OrdinalIgnoreCase)) ?? cache.RarityData[0];

            return cache.RarityData.FirstOrDefault(r => r.Name.Equals(rarityName.Trim(), StringComparison.OrdinalIgnoreCase))
                   ?? cache.RarityData[0];
        }

        private static Item? GenerateSingleForced(LootDataCache cache, Random rnd, ItemGenerationSpec spec, RarityData forcedRarity)
        {
            bool wantWeapon;
            if (spec.ItemType == ItemGenerationItemType.Weapons) wantWeapon = true;
            else if (spec.ItemType == ItemGenerationItemType.Armor) wantWeapon = false;
            else wantWeapon = rnd.NextDouble() < 0.5;

            int tier = spec.Tier ?? rnd.Next(1, 6);

            Item? item;
            if (wantWeapon)
            {
                var rows = cache.WeaponData.Where(w => w.Tier == tier).ToList();
                if (spec.WeaponType != null)
                    rows = rows.Where(w => string.Equals(w.Type, spec.WeaponType.Value.ToString(), StringComparison.OrdinalIgnoreCase)).ToList();

                if (rows.Count == 0)
                    return null;

                var weaponData = rows[rnd.Next(rows.Count)];
                item = ItemGenerator.GenerateWeaponItem(weaponData);
            }
            else
            {
                var rows = cache.ArmorData.Where(a => a.Tier == tier).ToList();
                if (spec.ArmorSlot != ItemGenerationArmorSlot.Any)
                {
                    string slot = spec.ArmorSlot.ToString().ToLowerInvariant();
                    rows = rows.Where(a => string.Equals(a.Slot, slot, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (rows.Count == 0)
                    return null;

                var armorData = rows[rnd.Next(rows.Count)];
                item = ItemGenerator.GenerateArmorItem(armorData);
            }

            // Level + tier are needed for scaling and effect rolls
            item.Tier = tier;
            item.Level = Math.Max(1, spec.DungeonLevel);

            var tuning = GameConfiguration.Instance;
            if (tuning.ItemScaling != null)
                ItemGenerator.ApplyScaling(item, tuning.ItemScaling, item.Level);

            item.Rarity = forcedRarity.Name?.Trim() ?? "Common";

            var context = LootContext.Create(player: null, dungeonTheme: null, enemyArchetype: null);
            if (item is WeaponItem wItem)
                context.WeaponType = wItem.WeaponType.ToString();

            var applier = new LootBonusApplier(cache, rnd);
            applier.ApplyBonuses(item, forcedRarity, context);

            // Ensure final tier/rarity stay forced even if bonuses upgrade rarity.
            if (!string.IsNullOrWhiteSpace(spec.Rarity) && !spec.Rarity.Equals("Any", StringComparison.OrdinalIgnoreCase))
                item.Rarity = forcedRarity.Name?.Trim() ?? item.Rarity;
            item.Tier = tier;

            // Name already finalized inside LootBonusApplier.ApplyBonuses; do not re-run name assembly here
            // or prefixes would be prepended again (GetBaseItemName would see an already-prefixed name).
            return item;
        }

        private static int IndexOfRarity(string? rarity)
        {
            if (string.IsNullOrWhiteSpace(rarity))
                return 0;
            int idx = Array.FindIndex(RarityOrder, r => string.Equals(r, rarity.Trim(), StringComparison.OrdinalIgnoreCase));
            return idx < 0 ? 0 : idx;
        }
    }
}

