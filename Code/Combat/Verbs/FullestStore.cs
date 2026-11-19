using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// The hero's fullest material store (D6.6, 2026-11-19; it replaced attunement, the most-worn material): the material
    /// mint holding the most units, ties going to MaterialBuilds.json order. CASH IN spends it. Verb specs name it
    /// <c>FULLEST</c>; <c>ATTUNED</c> in older data means the same.
    /// </summary>
    public static class FullestStore
    {
        /// <summary>Store id verb specs use for "your fullest material store".</summary>
        public const string StoreToken = "FULLEST";
        /// <summary>The pre-2026-11-19 spelling (attunement), read as <see cref="StoreToken"/>.</summary>
        public const string LegacyToken = "ATTUNED";

        public static bool IsToken(string? store) =>
            string.Equals(store, StoreToken, StringComparison.OrdinalIgnoreCase)
            || string.Equals(store, LegacyToken, StringComparison.OrdinalIgnoreCase);

        public static MaterialBuildData? GetBuild(Character? hero)
        {
            if (hero == null || hero is Enemy)
                return null;
            MaterialBuildData? best = null;
            int bestUnits = 0;
            foreach (var build in MaterialBuildsLoader.GetAll())
            {
                if (string.IsNullOrWhiteSpace(build.Keyword))
                    continue;
                int units = hero.Effects.GetMaterialKeyword(build.Keyword);
                if (units > bestUnits)
                {
                    bestUnits = units;
                    best = build;
                }
            }
            return best;
        }

        public static string? GetMaterial(Character? hero) => GetBuild(hero)?.Material;

        /// <summary>The fullest store's keyword (RAGE, GRAZE, …), or null when every material store is empty.</summary>
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
