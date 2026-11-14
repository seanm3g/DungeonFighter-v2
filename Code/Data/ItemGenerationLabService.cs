using System;
using System.Collections.Generic;
using System.Linq;

using RPGGame.Data.ItemGeneration;

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
        Legs,
        Feet
    }

    public sealed class ItemGenerationSpec
    {
        public ItemGenerationItemType ItemType { get; set; } = ItemGenerationItemType.Both;
        public string? Rarity { get; set; } = null; // null/empty => any
        public int? Tier { get; set; } = null; // null => any
        public WeaponType? WeaponType { get; set; } = null; // null => any
        public ItemGenerationArmorSlot ArmorSlot { get; set; } = ItemGenerationArmorSlot.Any;

        /// <summary>Hero level; with <see cref="DungeonLevel"/> it sets the item level exactly as dungeon loot does.</summary>
        public int PlayerLevel { get; set; } = 1;
        public int DungeonLevel { get; set; } = 1;
        public int Seed { get; set; } = 12345;

        /// <summary>Magic find (0–100): scales roll-up chances.</summary>
        public double MagicFind { get; set; } = 0;

        /// <summary>
        /// Optional fixed rarity percent chances used when <see cref="Rarity"/> is Any.
        /// If set, the lab rolls rarity from these values (normalized) and ignores level gating.
        /// Keys are rarity names (Common/Uncommon/Rare/Epic/Legendary/Mythic).
        /// </summary>
        public Dictionary<string, double>? FixedRarityChancesPercent { get; set; } = null;
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
            : Item is LegsItem l ? l.GetTotalArmor()
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
            "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic"
        };

        /// <summary>
        /// Rolls <paramref name="count"/> items with the same roller as dungeon loot (<see cref="LootGenerator.Roll(ItemRollRequest)"/>);
        /// the spec's type / slot / weapon type / tier / rarity become request constraints. Item seeds come from
        /// <see cref="ItemGenerationSpec.Seed"/>, so a batch is reproducible.
        /// </summary>
        public static List<ItemGeneratedRow> GenerateBatch(ItemGenerationSpec spec, int count)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            count = Math.Clamp(count, 1, 1_000_000);

            var rnd = new Random(spec.Seed);
            bool rollRarityEachItem = string.IsNullOrWhiteSpace(spec.Rarity) ||
                                      spec.Rarity.Equals("Any", StringComparison.OrdinalIgnoreCase);
            bool? weapon = spec.ItemType switch
            {
                ItemGenerationItemType.Weapons => true,
                ItemGenerationItemType.Armor => false,
                _ => null
            };
            string? slot = spec.ItemType == ItemGenerationItemType.Armor && spec.ArmorSlot != ItemGenerationArmorSlot.Any
                ? spec.ArmorSlot.ToString().ToLowerInvariant()
                : null;
            var results = new List<ItemGeneratedRow>(count);

            for (int i = 0; i < count; i++)
            {
                string? rarity = !rollRarityEachItem
                    ? spec.Rarity!.Trim()
                    : spec.FixedRarityChancesPercent is { Count: > 0 } chances
                        ? RollRarityFromFixedChances(rnd, chances)
                        : null;

                var item = LootGenerator.Roll(new ItemRollRequest
                {
                    HeroLevel = Math.Clamp(spec.PlayerLevel, 1, 99),
                    DungeonLevel = Math.Max(1, spec.DungeonLevel),
                    Weapon = weapon,
                    Slot = slot,
                    WeaponType = weapon == false ? null : spec.WeaponType,
                    Tier = spec.Tier is int t ? Math.Clamp(t, 1, 5) : null,
                    Rarity = rarity,
                    MagicFind = Math.Clamp(spec.MagicFind, 0, 100),
                    Seed = rnd.Next()
                });
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

        /// <summary>Rarity name from the lab's fixed percent chances (normalized; empty = Common).</summary>
        private static string RollRarityFromFixedChances(Random rnd, Dictionary<string, double> chancesPercent)
        {
            double total = 0;
            var rows = new List<(string Name, double Weight)>();
            foreach (string name in RarityOrder)
            {
                double w = chancesPercent.FirstOrDefault(kv => string.Equals(kv.Key?.Trim(), name, StringComparison.OrdinalIgnoreCase)).Value;
                if (w > 0)
                {
                    rows.Add((name, w));
                    total += w;
                }
            }
            if (total <= 0)
                return "Common";
            double roll = rnd.NextDouble() * total;
            foreach (var (name, w) in rows)
            {
                roll -= w;
                if (roll < 0)
                    return name;
            }
            return rows[^1].Name;
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
            int primary = item is WeaponItem w ? w.GetTotalDamage() : item is HeadItem h ? h.GetTotalArmor() : item is ChestItem c ? c.GetTotalArmor() : item is LegsItem l ? l.GetTotalArmor() : item is FeetItem f ? f.GetTotalArmor() : 0;
            double speed = item is WeaponItem ww ? ww.GetTotalAttackSpeed() : 0;
            int affix = (item.Modifications?.Count ?? 0) + (item.StatBonuses?.Count ?? 0) + (item.ActionBonuses?.Count ?? 0);
            return $"{r:D2}-{tier:D2}-{primary:D5}-{speed:0000.000}-{affix:D2}-{item.Name}";
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

