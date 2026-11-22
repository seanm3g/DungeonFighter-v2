using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Data.ItemGeneration
{
    /// <summary>
    /// Item class affinity (Documentation/CORE_DESIGN_PASS_2026-11-17.md, Phase 11, D11.2 / D11.7 / D11.8): every item
    /// belongs to one of 10 classes, the 4 core classes or one of the 6 hybrids (<see cref="HybridTrees"/>). The
    /// affinity picks the item's material (its class's materials, a hybrid's either parent's) and tilts its lines.
    /// </summary>
    public static class ClassAffinity
    {
        /// <summary>The 10 affinity ids: barbarian, warrior, rogue, wizard, warbrute, ravager, ragecaller, duelist, spellblade, hexblade.</summary>
        public static IReadOnlyList<string> All { get; } =
            ClassTags.All.Concat(HybridTrees.All.Select(h => h.Tag)).ToArray();

        public static bool IsCore(string? id) => ClassTags.All.Contains(Norm(id));

        public static HybridInfo? Hybrid(string? id) =>
            HybridTrees.All.FirstOrDefault(h => string.Equals(h.Tag, Norm(id), StringComparison.Ordinal));

        public static bool IsAffinity(string? id) => All.Contains(Norm(id));

        /// <summary>The core classes behind an affinity: itself for a core class, both parents for a hybrid.</summary>
        public static IReadOnlyList<string> CoreClasses(string? id)
        {
            string n = Norm(id);
            if (IsCore(n))
                return new[] { n };
            return Hybrid(n) is { } h ? new[] { h.ClassA, h.ClassB } : Array.Empty<string>();
        }

        /// <summary>The hybrids that have <paramref name="coreClass"/> as a parent (3 per class).</summary>
        public static IReadOnlyList<string> HybridsOf(string? coreClass)
        {
            string n = Norm(coreClass);
            return HybridTrees.All.Where(h => h.ClassA == n || h.ClassB == n).Select(h => h.Tag).ToArray();
        }

        /// <summary>
        /// Related but not the same: a hybrid and one of its parents, either way round. Two hybrids sharing a parent
        /// are not related (they stay off-class for each other).
        /// </summary>
        public static bool Related(string? a, string? b)
        {
            string x = Norm(a), y = Norm(b);
            if (x.Length == 0 || y.Length == 0 || x == y)
                return false;
            if (IsCore(x) && Hybrid(y) is { } hy)
                return hy.ClassA == x || hy.ClassB == x;
            if (IsCore(y) && Hybrid(x) is { } hx)
                return hx.ClassA == y || hx.ClassB == y;
            return false;
        }

        /// <summary>The set materials of an affinity's core classes (3 for a core class, 6 for a hybrid).</summary>
        public static IReadOnlyList<string> Materials(string? id)
        {
            var list = new List<string>();
            foreach (string core in CoreClasses(id))
                if (ClassTags.WeaponOf(core) is WeaponType wt
                    && ItemMaterialRules.TryGetWeaponClassLadder(wt, out string c, out string u, out string r))
                    list.AddRange(new[] { c, u, r });
            return list;
        }

        /// <summary>The core class whose materials include <paramref name="material"/>; null for class-less materials.</summary>
        public static string? ClassOfMaterial(string? material)
        {
            string m = ItemMaterialRules.RemapLegacyMaterial(material);
            foreach (string core in ClassTags.All)
                if (Materials(core).Any(x => string.Equals(x, m, StringComparison.OrdinalIgnoreCase)))
                    return core;
            return null;
        }

        public static string Norm(string? id) => (id ?? "").Trim().ToLowerInvariant();
    }
}
