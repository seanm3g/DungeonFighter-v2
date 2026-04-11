using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.ActionInteractionLab
{
    /// <summary>
    /// Builds armor <see cref="Item"/> instances for the Action Interaction Lab from <see cref="ArmorData"/> plus optional prefix/suffix picks.
    /// </summary>
    public static class ActionLabArmorFactory
    {
        private static readonly string[] RarityOrder = { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic", "Transcendent" };

        /// <summary>Maps equip slot names (<c>head</c>, <c>body</c>, <c>feet</c>) to <see cref="ArmorData.Slot"/> JSON values.</summary>
        public static string ArmorJsonSlotFromEquipSlot(string equipSlot)
        {
            return equipSlot.ToLowerInvariant() switch
            {
                "head" => "head",
                "body" => "chest",
                "feet" => "feet",
                _ => throw new ArgumentException("Slot must be head, body, or feet.", nameof(equipSlot))
            };
        }

        /// <summary>Armor rows for one equip slot, sorted by name.</summary>
        public static List<ArmorData> FilterArmorDataForEquipSlot(IReadOnlyList<ArmorData> all, string equipSlot)
        {
            string json = ArmorJsonSlotFromEquipSlot(equipSlot);
            return all
                .Where(a => !string.IsNullOrWhiteSpace(a.Slot) && string.Equals(a.Slot, json, StringComparison.OrdinalIgnoreCase))
                .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Creates armor with optional rolled prefix modification and optional stat-bonus suffix. Does not assign <see cref="Item.GearAction"/>.
        /// </summary>
        public static Item CreateArmor(
            ArmorData armorData,
            Modification? prefixTemplate,
            StatBonus? suffixTemplate)
        {
            var item = ItemGenerator.GenerateArmorItem(armorData);
            item.Modifications.Clear();
            item.StatBonuses.Clear();
            item.ActionBonuses.Clear();
            item.GearAction = null;

            if (prefixTemplate != null)
                item.Modifications.Add(CloneModificationWithRoll(prefixTemplate));

            if (suffixTemplate != null)
                item.StatBonuses.Add(CloneStatBonus(suffixTemplate));

            ApplyMinimumRarity(item);
            item.Name = ItemGenerator.GenerateItemNameWithBonuses(item);
            return item;
        }

        private static void ApplyMinimumRarity(Item item)
        {
            string? required = null;

            foreach (var mod in item.Modifications)
            {
                if (string.IsNullOrEmpty(mod.ItemRank))
                    continue;
                if (required == null || IsRarityStrictlyHigher(mod.ItemRank, required))
                    required = mod.ItemRank;
            }

            foreach (var sb in item.StatBonuses)
            {
                if (sb.Name.Equals("of the Sage", StringComparison.OrdinalIgnoreCase))
                {
                    if (required == null || IsRarityStrictlyHigher("Rare", required))
                        required = "Rare";
                }
            }

            if (required != null)
                item.Rarity = required;
        }

        private static bool IsRarityStrictlyHigher(string a, string b)
        {
            int ia = Array.IndexOf(RarityOrder, a);
            int ib = Array.IndexOf(RarityOrder, b);
            if (ia < 0) ia = 0;
            if (ib < 0) ib = 0;
            return ia > ib;
        }

        private static Modification CloneModificationWithRoll(Modification template)
        {
            var m = new Modification
            {
                DiceResult = template.DiceResult,
                ItemRank = template.ItemRank,
                Name = template.Name,
                Description = template.Description,
                Effect = template.Effect,
                MinValue = template.MinValue,
                MaxValue = template.MaxValue,
                RolledValue = RollValueBetween(template.MinValue, template.MaxValue),
            };
            if (template.StatusEffects != null && template.StatusEffects.Count > 0)
                m.StatusEffects = new List<string>(template.StatusEffects);
            return m;
        }

        private static double RollValueBetween(double minValue, double maxValue)
        {
            if (Math.Abs(minValue - maxValue) < 0.001)
                return minValue;
            return minValue + Random.Shared.NextDouble() * (maxValue - minValue);
        }

        private static StatBonus CloneStatBonus(StatBonus s) => new()
        {
            Name = s.Name,
            Description = s.Description,
            Value = s.Value,
            Weight = s.Weight,
            StatType = s.StatType,
        };

        /// <summary>
        /// Finds a reasonable default <see cref="ArmorData"/> row for the current lab armor (match JSON slot, then tier).
        /// </summary>
        public static int FindBestArmorDataIndex(IReadOnlyList<ArmorData> armors, Item? equipped)
        {
            if (armors.Count == 0)
                return -1;
            if (equipped == null)
                return 0;

            string? jsonSlot = JsonSlotFromItemType(equipped.Type);
            if (jsonSlot == null)
                return 0;

            int tier = equipped.Tier;
            for (int i = 0; i < armors.Count; i++)
            {
                if (string.Equals(armors[i].Slot, jsonSlot, StringComparison.OrdinalIgnoreCase)
                    && armors[i].Tier == tier)
                    return i;
            }

            for (int i = 0; i < armors.Count; i++)
            {
                if (string.Equals(armors[i].Slot, jsonSlot, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return 0;
        }

        private static string? JsonSlotFromItemType(ItemType type) => type switch
        {
            ItemType.Head => "head",
            ItemType.Chest => "chest",
            ItemType.Feet => "feet",
            _ => null,
        };
    }
}
