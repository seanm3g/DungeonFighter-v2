using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Canonical item materials: class weapon ladders and class-less pool.
    /// </summary>
    public static class ItemMaterialRules
    {
        public static readonly string[] ClassMaterials =
        {
            "Bone", "Steel", "Iron",
            "Bronze", "Gold", "Mithril",
            "Glass", "Obsidian", "Shadow",
            "Willow", "Silver", "Crystal"
        };

        public static readonly string[] ClassLessMaterials =
        {
            "Stone", "Unknown", "Strange", "Celestial", "Wood", "Leather", "Cloth"
        };

        /// <summary>
        /// Step 8 (D4.3, D4.8): Wood, Cloth and Unknown are cosmetic. They no longer drop and form no set; an item made
        /// of one keeps its name. <c>DF_LEGACY_MATERIALS=1</c> brings them back (deleted in step 12).
        /// </summary>
        public static readonly string[] CosmeticMaterials = { "Wood", "Cloth", "Unknown" };

        public static bool CosmeticEnabled => _cosmeticOverride ?? System.Environment.GetEnvironmentVariable("DF_LEGACY_MATERIALS") != "1";

        private static bool? _cosmeticOverride;

        /// <summary>Pins <see cref="CosmeticEnabled"/> until disposed (tests of the old Wood / Cloth / Unknown sets).</summary>
        public static IDisposable PinCosmetic(bool enabled)
        {
            var previous = _cosmeticOverride;
            _cosmeticOverride = enabled;
            return new PinScope(() => _cosmeticOverride = previous);
        }

        private sealed class PinScope : IDisposable
        {
            private readonly System.Action _undo;
            public PinScope(System.Action undo) => _undo = undo;
            public void Dispose() => _undo();
        }

        public static bool IsCosmetic(string? material) =>
            CosmeticEnabled && CosmeticMaterials.Any(c => string.Equals(c, RemapLegacyMaterial(material), StringComparison.OrdinalIgnoreCase));

        public static readonly string[] AllMaterials =
            ClassMaterials.Concat(ClassLessMaterials).ToArray();

        /// <summary>
        /// Weapon type → the attribute family of materials (Mace: the STR materials Bone / Steel / Iron, ...). Families
        /// group materials by the attribute they pay; any class can wear any of them.
        /// </summary>
        public static bool TryGetWeaponClassLadder(WeaponType weaponType, out string common, out string uncommon, out string rare)
        {
            switch (weaponType)
            {
                case WeaponType.Mace:
                    common = "Bone"; uncommon = "Steel"; rare = "Iron";
                    return true;
                case WeaponType.Sword:
                    common = "Bronze"; uncommon = "Gold"; rare = "Mithril";
                    return true;
                case WeaponType.Dagger:
                    common = "Glass"; uncommon = "Obsidian"; rare = "Shadow";
                    return true;
                case WeaponType.Wand:
                    common = "Willow"; uncommon = "Silver"; rare = "Crystal";
                    return true;
                default:
                    common = uncommon = rare = "";
                    return false;
            }
        }

        /// <summary>Default chance a weapon rolls one of its own family's three materials (rest: any set material).</summary>
        public const double DefaultWeaponFamilyMaterialShare = 0.9;

        /// <summary>
        /// Weapon material: with <see cref="LootSystemConfig.WeaponFamilyMaterialShare"/> (default 90%) one of the weapon
        /// type's three family materials (Wand: Willow / Silver / Crystal), otherwise any of the twelve set materials
        /// (a Wand can still be Bone). Materials stay wearable by any class.
        /// </summary>
        public static string PickClassMaterial(WeaponType weaponType, Random random) =>
            PickClassMaterial(weaponType, random,
                GameConfiguration.Instance?.LootSystem?.WeaponFamilyMaterialShare ?? DefaultWeaponFamilyMaterialShare);

        public static string PickClassMaterial(WeaponType weaponType, Random random, double familyShare)
        {
            if (random.NextDouble() < Math.Clamp(familyShare, 0, 1)
                && TryPickFamilyMaterial(weaponType, random, out string family))
                return family;
            return ClassMaterials[random.Next(ClassMaterials.Length)];
        }

        /// <summary>One of the weapon type's three family materials, picked evenly.</summary>
        public static bool TryPickFamilyMaterial(WeaponType weaponType, Random random, out string material)
        {
            material = "";
            if (!TryGetWeaponClassLadder(weaponType, out string common, out string uncommon, out string rare))
                return false;
            material = random.Next(3) switch { 0 => common, 1 => uncommon, _ => rare };
            return true;
        }

        /// <summary>True for the twelve set materials (Bone ... Crystal), not the class-less pool.</summary>
        public static bool IsSetMaterial(string? material)
        {
            string m = RemapLegacyMaterial(material);
            return ClassMaterials.Any(c => string.Equals(c, m, StringComparison.OrdinalIgnoreCase));
        }

        public static int RarityLadderIndex(string? rarity)
        {
            string r = rarity?.Trim() ?? "Common";
            if (r.Equals("Common", StringComparison.OrdinalIgnoreCase)) return 0;
            if (r.Equals("Uncommon", StringComparison.OrdinalIgnoreCase)) return 1;
            if (r.Equals("Rare", StringComparison.OrdinalIgnoreCase)) return 2;
            if (r.Equals("Epic", StringComparison.OrdinalIgnoreCase)) return 3;
            if (r.Equals("Legendary", StringComparison.OrdinalIgnoreCase)) return 4;
            if (r.Equals("Mythic", StringComparison.OrdinalIgnoreCase)) return 5;
            return 0;
        }

        /// <summary>
        /// Armor material when it doesn't take its class's materials: an even pick among every material whose
        /// <c>ItemRank</c> is at or below the item's rarity. The twelve set materials are Common rank, so they are always
        /// in the pool; classless ones join as rarity allows (Stone at Common, Unknown at Uncommon, Strange at Rare,
        /// Celestial at Epic).
        /// </summary>
        public static string PickNonWeaponMaterial(IEnumerable<Modification> materialMods, string? itemRarity, Random random)
        {
            var list = materialMods?.Where(m => m != null && !string.IsNullOrWhiteSpace(m.Name)).ToList()
                       ?? new List<Modification>();
            if (list.Count == 0)
                return ClassLessMaterials[random.Next(ClassLessMaterials.Length)];

            list = list.Where(m => !IsCosmetic(m.Name)).ToList();
            if (list.Count == 0)
                return "Leather";
            int itemRank = RarityLadderIndex(itemRarity);
            var pool = list.Where(m => RarityLadderIndex(m.ItemRank) <= itemRank).ToList();
            if (pool.Count == 0)
                pool = list;
            return pool[random.Next(pool.Count)].Name.Trim();
        }

        /// <summary>
        /// Normalizes material names for combat/display: title case, Damascus→Iron, MITHIRL→Mithril.
        /// </summary>
        public static string RemapLegacyMaterial(string? material)
            => Data.MaterialBuildData.CanonicalMaterialName(material);
    }
}
