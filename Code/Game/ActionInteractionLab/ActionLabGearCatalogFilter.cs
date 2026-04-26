using System;

namespace RPGGame.ActionInteractionLab
{
    /// <summary>
    /// Filtering rules for Action Lab gear pickers: weapon and armor base rows use tier→rarity bands when JSON has no per-item rank;
    /// prefixes use <see cref="Modification.ItemRank"/>; suffixes use optional <see cref="StatBonus.ItemRank"/> (blank = matches any rarity filter).
    /// Armor class (filter label) is derived from display names (all words except the last), e.g. <c>Cloth Cap</c> to <c>Cloth</c>.
    /// Optional tier filter matches the item row’s numeric <c>Tier</c> exactly.
    /// </summary>
    public static class ActionLabGearCatalogFilter
    {
        public static readonly string[] RarityOrder =
            { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic" };

        /// <summary>
        /// Maps weapon tier to a catalog rarity band for list filtering (Weapons.json has no ItemRank today).
        /// </summary>
        public static string GetWeaponTierRarityBand(int tier)
        {
            int t = Math.Clamp(tier, 1, 5);
            return t switch
            {
                1 => "Common",
                2 => "Uncommon",
                3 => "Rare",
                4 => "Epic",
                _ => "Legendary",
            };
        }

        public static bool WeaponMatchesRarityFilter(WeaponData weapon, string? selectedRarity) =>
            TierMatchesEquipmentRarityBand(weapon.Tier, selectedRarity);

        /// <summary>Same tier bands as <see cref="WeaponMatchesRarityFilter"/> (Armor.json uses tier only, no per-row rank).</summary>
        public static bool ArmorMatchesRarityFilter(ArmorData armor, string? selectedRarity) =>
            TierMatchesEquipmentRarityBand(armor.Tier, selectedRarity);

        private static bool TierMatchesEquipmentRarityBand(int tier, string? selectedRarity)
        {
            if (string.IsNullOrWhiteSpace(selectedRarity))
                return true;

            string want = selectedRarity.Trim();
            string band = GetWeaponTierRarityBand(tier);
            if (string.Equals(band, want, StringComparison.OrdinalIgnoreCase))
                return true;

            if (tier >= 5 && IsHighTierWeaponRarity(want))
                return true;

            return false;
        }

        /// <summary>
        /// Catalog class for armor list filtering: all words of <paramref name="displayName"/> except the last (the piece: Cap, Boots, …).
        /// Single-word names return the trimmed whole string.
        /// </summary>
        public static string GetArmorCatalogClass(string? displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return "";
            string trimmed = displayName.Trim();
            string[] parts = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length <= 1)
                return trimmed;
            return string.Join(" ", parts, 0, parts.Length - 1);
        }

        public static bool ArmorMatchesClassFilter(ArmorData armor, string? selectedClass)
        {
            if (string.IsNullOrWhiteSpace(selectedClass))
                return true;
            string cls = GetArmorCatalogClass(armor.Name);
            return string.Equals(cls, selectedClass.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsHighTierWeaponRarity(string r) =>
            r.Equals("Legendary", StringComparison.OrdinalIgnoreCase)
            || r.Equals("Mythic", StringComparison.OrdinalIgnoreCase);

        public static bool ModificationMatchesRarityFilter(Modification mod, string? selectedRarity)
        {
            if (string.IsNullOrWhiteSpace(selectedRarity))
                return true;
            if (string.IsNullOrWhiteSpace(mod.ItemRank))
                return false;
            return string.Equals(mod.ItemRank.Trim(), selectedRarity.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Blank <see cref="StatBonus.ItemRank"/> means the suffix is pool-wide and stays visible for every rarity filter.
        /// </summary>
        public static bool StatBonusMatchesRarityFilter(StatBonus suffix, string? selectedRarity)
        {
            if (string.IsNullOrWhiteSpace(selectedRarity))
                return true;
            if (string.IsNullOrWhiteSpace(suffix.ItemRank))
                return true;
            return string.Equals(suffix.ItemRank.Trim(), selectedRarity.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public static bool WeaponMatchesTypeFilter(WeaponData weapon, string? selectedType)
        {
            if (string.IsNullOrWhiteSpace(selectedType))
                return true;
            return string.Equals(weapon.Type?.Trim(), selectedType.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>When <paramref name="selectedTier"/> is null, all tiers match; otherwise <paramref name="itemTier"/> must equal that value.</summary>
        public static bool ItemMatchesTierFilter(int itemTier, int? selectedTier)
        {
            if (selectedTier == null)
                return true;
            return itemTier == selectedTier.Value;
        }
    }
}
