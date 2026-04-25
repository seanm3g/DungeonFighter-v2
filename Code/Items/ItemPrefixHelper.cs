using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Prefix category for loot modifications (Adjective / Material / Quality). At most one of each per item.
    /// </summary>
    public enum ModificationPrefixCategory
    {
        Adjective,
        Material,
        Quality
    }

    /// <summary>
    /// Helpers for three-category prefix ordering, quality multipliers, and category parsing.
    /// </summary>
    public static class ItemPrefixHelper
    {
        private static readonly string[] RarityOrder =
        {
            "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic", "Transcendent"
        };

        /// <summary>
        /// Display order: Quality, then Adjective, then Material (matches name assembly and UI stripping).
        /// </summary>
        public static int DisplayOrderRank(ModificationPrefixCategory c) => c switch
        {
            ModificationPrefixCategory.Quality => 0,
            ModificationPrefixCategory.Adjective => 1,
            ModificationPrefixCategory.Material => 2,
            _ => 1
        };

        public static ModificationPrefixCategory ParsePrefixCategory(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return ModificationPrefixCategory.Adjective;
            if (string.Equals(raw, "Material", StringComparison.OrdinalIgnoreCase))
                return ModificationPrefixCategory.Material;
            if (string.Equals(raw, "Quality", StringComparison.OrdinalIgnoreCase))
                return ModificationPrefixCategory.Quality;
            return ModificationPrefixCategory.Adjective;
        }

        public static ModificationPrefixCategory GetPrefixCategory(this Modification m) =>
            ParsePrefixCategory(m.PrefixCategory);

        /// <summary>
        /// Prefix-style modifications in display order (excludes "of …" suffix-style modification names).
        /// </summary>
        public static List<Modification> OrderedPrefixModifications(IReadOnlyList<Modification>? mods)
        {
            if (mods == null || mods.Count == 0)
                return new List<Modification>();

            return mods
                .Where(m => m != null && !string.IsNullOrEmpty(m.Name) && !m.Name.StartsWith("of ", StringComparison.OrdinalIgnoreCase))
                .OrderBy(m => DisplayOrderRank(m.GetPrefixCategory()))
                .ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Product of <see cref="Modification.RolledValue"/> for <c>gearPrimaryStatMultiplier</c> on this item (typically one Quality row). Empty / none = 1.0.
        /// </summary>
        public static double GetGearPrimaryStatMultiplier(Item item)
        {
            if (item?.Modifications == null || item.Modifications.Count == 0)
                return 1.0;

            double product = 1.0;
            foreach (var m in item.Modifications)
            {
                if (m != null && m.Effect == "gearPrimaryStatMultiplier")
                    product *= m.RolledValue <= 0 ? 1.0 : m.RolledValue;
            }

            return product;
        }

        /// <summary>
        /// True if <paramref name="itemRarity"/> is at least as high as <paramref name="minRank"/> (from template <see cref="Modification.ItemRank"/>).
        /// </summary>
        public static bool MeetsMinimumItemRank(string? itemRarity, string? minRank)
        {
            if (string.IsNullOrWhiteSpace(minRank))
                return true;

            int itemIdx = IndexOfRarity(itemRarity);
            int minIdx = IndexOfRarity(minRank);
            if (minIdx < 0)
                return true;
            if (itemIdx < 0)
                return false;
            return itemIdx >= minIdx;
        }

        public static int IndexOfRarity(string? rarityName)
        {
            if (string.IsNullOrWhiteSpace(rarityName))
                return 0;
            int idx = Array.FindIndex(RarityOrder, r => string.Equals(r, rarityName.Trim(), StringComparison.OrdinalIgnoreCase));
            return idx < 0 ? 0 : idx;
        }
    }
}
