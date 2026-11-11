using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// A hero's attuned material: the material with the most worn pieces, ties going to the weapon's material
    /// (then slot order weapon → head → body → legs → feet). Generic verb presets (CASH IN, RECKONING) resolve
    /// against the attuned material's store (Documentation/BUILD_SYSTEM_CONDENSED.md §4, decision 1).
    /// Only materials with a MATERIAL BUILDS row and a keyword count; raw pieces, no wildcard or hybrid joiners.
    /// </summary>
    public static class Attunement
    {
        /// <summary>Store id that verb specs use for "the attuned material's mint".</summary>
        public const string StoreToken = "ATTUNED";

        public static MaterialBuildData? GetBuild(Character? hero)
        {
            if (hero == null || hero is Enemy || hero.Equipment == null)
                return null;

            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var order = new List<string>();
            foreach (var item in SlotOrder(hero))
            {
                if (item == null)
                    continue;
                string material = MaterialBuildData.CanonicalMaterialName(ItemMaterialRules.RemapLegacyMaterial(item.Material));
                if (material.Length == 0)
                    continue;
                var build = MaterialBuildsLoader.FindByMaterial(material);
                if (build == null || string.IsNullOrWhiteSpace(build.Keyword))
                    continue;
                if (!counts.ContainsKey(material))
                {
                    counts[material] = 0;
                    order.Add(material);
                }
                counts[material]++;
            }
            if (order.Count == 0)
                return null;

            int best = counts.Values.Max();
            string winner = order.First(m => counts[m] == best);
            return MaterialBuildsLoader.FindByMaterial(winner);
        }

        public static string? GetMaterial(Character? hero) => GetBuild(hero)?.Material;

        /// <summary>The attuned material's mint keyword (RAGE, GRAZE, …), or null when nothing is attuned.</summary>
        public static string? GetKeyword(Character? hero)
        {
            string? keyword = GetBuild(hero)?.Keyword;
            return string.IsNullOrWhiteSpace(keyword) ? null : keyword;
        }

        /// <summary>Distinct materials (with a MATERIAL BUILDS row) the hero wears at least one piece of.</summary>
        public static int CountDistinctMaterials(Character? hero)
        {
            if (hero?.Equipment == null)
                return 0;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in SlotOrder(hero))
            {
                if (item == null)
                    continue;
                string material = MaterialBuildData.CanonicalMaterialName(ItemMaterialRules.RemapLegacyMaterial(item.Material));
                if (material.Length > 0 && MaterialBuildsLoader.FindByMaterial(material) != null)
                    seen.Add(material);
            }
            return seen.Count;
        }

        // Weapon first so it wins ties.
        private static IEnumerable<Item?> SlotOrder(Character hero)
        {
            yield return hero.Equipment.Weapon;
            yield return hero.Equipment.Head;
            yield return hero.Equipment.Body;
            yield return hero.Equipment.Legs;
            yield return hero.Equipment.Feet;
        }
    }
}
