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
            "Bone", "Steel", "Damascus",
            "Bronze", "Gold", "Mithril",
            "Glass", "Obsidian", "Shadow",
            "Willow", "Silver", "Crystal"
        };

        public static readonly string[] ClassLessMaterials =
        {
            "Stone", "Unknown", "Strange", "Celestial", "Wood", "Leather", "Cloth"
        };

        public static readonly string[] AllMaterials =
            ClassMaterials.Concat(ClassLessMaterials).ToArray();

        /// <summary>Weapon type → Common / Uncommon / Rare+ class materials.</summary>
        public static bool TryGetWeaponClassLadder(WeaponType weaponType, out string common, out string uncommon, out string rare)
        {
            switch (weaponType)
            {
                case WeaponType.Mace:
                    common = "Bone"; uncommon = "Steel"; rare = "Damascus";
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

        /// <summary>
        /// Weapons always use their class ladder. Item rarity Common→common, Uncommon→uncommon, Rare+→rare.
        /// </summary>
        public static string ResolveWeaponMaterial(WeaponType weaponType, string? itemRarity)
        {
            if (!TryGetWeaponClassLadder(weaponType, out string common, out string uncommon, out string rare))
                return "Bone";

            string r = itemRarity?.Trim() ?? "Common";
            if (r.Equals("Common", StringComparison.OrdinalIgnoreCase))
                return common;
            if (r.Equals("Uncommon", StringComparison.OrdinalIgnoreCase))
                return uncommon;
            return rare;
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

        /// <summary>Prefer materials whose ItemRank matches item rarity; otherwise any.
        /// When <paramref name="biasedMaterials"/> is set, those names are weighted ×1.5 in the pick.</summary>
        public static string PickNonWeaponMaterial(
            IEnumerable<Modification> materialMods,
            string? itemRarity,
            Random random,
            IReadOnlyCollection<string>? biasedMaterials = null)
        {
            var list = materialMods?.Where(m => m != null && !string.IsNullOrWhiteSpace(m.Name)).ToList()
                       ?? new List<Modification>();
            if (list.Count == 0)
                return ClassLessMaterials[random.Next(ClassLessMaterials.Length)];

            string r = itemRarity?.Trim() ?? "Common";
            var matching = list
                .Where(m => string.Equals(m.ItemRank?.Trim(), r, StringComparison.OrdinalIgnoreCase)
                            || (string.IsNullOrWhiteSpace(m.ItemRank) && r.Equals("Common", StringComparison.OrdinalIgnoreCase)))
                .ToList();
            var pool = matching.Count > 0 ? matching : list;

            if (biasedMaterials == null || biasedMaterials.Count == 0)
                return pool[random.Next(pool.Count)].Name.Trim();

            // Weighted pick: biased materials get weight 3, others weight 2 (≈ +50% relative).
            var weights = new List<(Modification Mod, int Weight)>(pool.Count);
            int total = 0;
            foreach (var m in pool)
            {
                bool biased = biasedMaterials.Any(b =>
                    string.Equals(b, m.Name?.Trim(), StringComparison.OrdinalIgnoreCase));
                int w = biased ? 3 : 2;
                weights.Add((m, w));
                total += w;
            }
            int roll = random.Next(total);
            int acc = 0;
            foreach (var (mod, w) in weights)
            {
                acc += w;
                if (roll < acc)
                    return mod.Name.Trim();
            }
            return pool[pool.Count - 1].Name.Trim();
        }

        /// <summary>
        /// Returns class-ladder material names to bias when the player has the matching market skill.
        /// </summary>
        public static IReadOnlyList<string>? GetMarketBiasedMaterials(Character? player)
        {
            if (player == null) return null;
            var router = SkillEffectRouter.Instance;
            if (router.HasEffect(player, "blood_market"))
                return new[] { "Bone", "Steel", "Damascus" };
            if (router.HasEffect(player, "muster_hall"))
                return new[] { "Bronze", "Gold", "Mithril" };
            if (router.HasEffect(player, "black_market"))
                return new[] { "Glass", "Obsidian", "Shadow" };
            if (router.HasEffect(player, "reagent_market"))
                return new[] { "Willow", "Silver", "Crystal" };
            return null;
        }
    }
}
