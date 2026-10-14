using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Class tags a hero "has": class tags on equipped gear and on the actions in the combo strip. A piece of a class
    /// material (Bone / Steel / Iron for Barbarian, …) also counts once that class's Core root is learned
    /// ("Bone, Steel, and Iron tags now count as a Barbarian tag").
    /// </summary>
    public static class ClassTags
    {
        public const string Barbarian = "barbarian";
        public const string Warrior = "warrior";
        public const string Rogue = "rogue";
        public const string Wizard = "wizard";

        public static readonly string[] All = { Barbarian, Warrior, Rogue, Wizard };

        /// <summary>Core root custom effect that lets a class's materials count as its tag.</summary>
        private static readonly Dictionary<string, string> RootEffect = new(StringComparer.OrdinalIgnoreCase)
        {
            [Barbarian] = "tribe_metal",
            [Warrior] = "iron_lineage",
            [Rogue] = "glass_veil",
            [Wizard] = "mage_circle"
        };

        public static WeaponType? WeaponOf(string classTag) => classTag?.ToLowerInvariant() switch
        {
            Barbarian => WeaponType.Mace,
            Warrior => WeaponType.Sword,
            Rogue => WeaponType.Dagger,
            Wizard => WeaponType.Wand,
            _ => null
        };

        public static string? ClassOf(WeaponType weaponType) => weaponType switch
        {
            WeaponType.Mace => Barbarian,
            WeaponType.Sword => Warrior,
            WeaponType.Dagger => Rogue,
            WeaponType.Wand => Wizard,
            _ => null
        };

        /// <summary>The class's three materials in Common / Uncommon / Rare order.</summary>
        public static IReadOnlyList<string> MaterialsOf(string classTag)
        {
            if (WeaponOf(classTag) is WeaponType wt &&
                ItemMaterialRules.TryGetWeaponClassLadder(wt, out string common, out string uncommon, out string rare))
                return new[] { common, uncommon, rare };
            return Array.Empty<string>();
        }

        /// <summary>True when the class's Core root is learned, so its materials count as its tag.</summary>
        public static bool MaterialsCountAsClass(Character? hero, string classTag) =>
            RootEffect.TryGetValue(classTag, out string? effect) && SkillRanks.GetRank(hero, effect) > 0;

        /// <summary>Does <paramref name="item"/> carry <paramref name="classTag"/> (directly, or via its material)?</summary>
        public static int CountOnItem(Character? hero, Item? item, string classTag)
        {
            if (item == null)
                return 0;
            int count = 0;
            if (item.Tags != null && item.Tags.Any(t => string.Equals(t, classTag, StringComparison.OrdinalIgnoreCase)))
                count++;
            if (!string.IsNullOrWhiteSpace(item.Material) && MaterialsCountAsClass(hero, classTag) &&
                MaterialsOf(classTag).Any(m => string.Equals(m, item.Material, StringComparison.OrdinalIgnoreCase)))
                count++;
            return count;
        }

        public static bool ActionHas(Action? action, string classTag) =>
            action?.Tags != null && action.Tags.Any(t => string.Equals(t, classTag, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Class tags the hero has for <paramref name="classTag"/>: its own tags plus any hybrid partner's (learned hybrid
        /// material node). A hybrid tag name (e.g. "warbrute") returns the hybrid tag count.
        /// </summary>
        public static int Count(Character? hero, string classTag)
        {
            if (hero == null || string.IsNullOrWhiteSpace(classTag))
                return 0;
            if (HybridTrees.All.FirstOrDefault(h => string.Equals(h.Tag, classTag, StringComparison.OrdinalIgnoreCase)) is HybridInfo hybrid)
                return HybridTrees.TagCount(hero, hybrid);
            int count = CountOwn(hero, classTag);
            foreach (string partner in HybridTrees.MergedPartners(hero, classTag))
                count += CountOwn(hero, partner);
            return count;
        }

        /// <summary>Class tags on equipped gear (materials count after the Core root) plus tags on strip actions.</summary>
        public static int CountOwn(Character? hero, string classTag)
        {
            if (hero == null || string.IsNullOrWhiteSpace(classTag))
                return 0;
            int count = 0;
            foreach (var item in new Item?[] { hero.Weapon, hero.Head, hero.Body, hero.Legs, hero.Feet })
                count += CountOnItem(hero, item, classTag);
            bool wandMastery = string.Equals(classTag, Wizard, StringComparison.OrdinalIgnoreCase)
                && SkillEffectRouter.WandMasteryTagsStrip(hero);
            foreach (var action in hero.GetComboActions())
                if (wandMastery || ActionHas(action, classTag))
                    count++;
            return count;
        }
    }

    /// <summary>Learned skill rank / power lookups by custom effect id (shared by the router and other systems).</summary>
    public static class SkillRanks
    {
        public static int GetRank(Character? c, string customEffectId)
        {
            if (c?.Progression == null || string.IsNullOrWhiteSpace(customEffectId)) return 0;
            return Lookup(c.Progression).TryGetValue(customEffectId, out var e) ? e.Rank : 0;
        }

        /// <summary>round(rank × power), at least 1 once the node is learned (integer magnitudes).</summary>
        public static int GetScaledRank(Character? c, string customEffectId)
        {
            double power = GetPower(c, customEffectId);
            return power <= 0 ? 0 : Math.Max(1, (int)Math.Round(power));
        }

        /// <summary>Rank × the node's <see cref="SkillTreeNodeDefinition.Power"/>.</summary>
        public static double GetPower(Character? c, string customEffectId)
        {
            if (c?.Progression == null || string.IsNullOrWhiteSpace(customEffectId)) return 0;
            return Lookup(c.Progression).TryGetValue(customEffectId, out var e) ? e.Power : 0;
        }

        // Per-hero table: custom effect id -> (best rank, best rank x node power). The skill router asks ~60 effects per
        // hit; each used to re-run EnsureSkillTreeRootsGranted (LINQ over the trees) and walk every learned node, which was
        // ~45% of simulation time. The table is rebuilt (roots granted first, as before) whenever learned ranks, node
        // powers, class points or the tree config change, so results are identical.
        private sealed class EffectTable
        {
            public long Signature = long.MinValue;
            public object? Trees;
            public Dictionary<string, (int Rank, double Power)> Map = new(StringComparer.OrdinalIgnoreCase);
        }

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<CharacterProgression, EffectTable> Tables = new();

        private static long Signature(CharacterProgression p, SkillTreesConfig trees)
        {
            unchecked
            {
                long h = 17;
                foreach (WeaponType path in ClassPresentationConfig.ClassWeaponOrder)
                    h = h * 31 + p.GetClassPoints(path);
                h = h * 31 + p.LearnedSkillRanks.Count;
                foreach (var kv in p.LearnedSkillRanks)
                {
                    long entry = StringComparer.OrdinalIgnoreCase.GetHashCode(kv.Key) * 397L + kv.Value;
                    var node = trees.GetNode(kv.Key);
                    if (node != null)
                        entry = entry * 31 + node.Power.GetHashCode();
                    h += entry * 1000003L; // order-independent sum
                }
                return h;
            }
        }

        [ThreadStatic] private static CharacterProgression? _pinned;
        [ThreadStatic] private static Dictionary<string, (int Rank, double Power)>? _pinnedMap;

        /// <summary>
        /// Within the returned scope, lookups for <paramref name="p"/> skip the change check (one hit asks ~60 effects and
        /// ranks / class points cannot change mid-hit). Dispose to restore.
        /// </summary>
        public static IDisposable Pin(CharacterProgression? p)
        {
            var prev = (_pinned, _pinnedMap);
            if (p != null)
            {
                _pinnedMap = Lookup(p);
                _pinned = p;
            }
            return new PinScope(prev);
        }

        private sealed class PinScope : IDisposable
        {
            private readonly (CharacterProgression?, Dictionary<string, (int Rank, double Power)>?) _prev;
            public PinScope((CharacterProgression?, Dictionary<string, (int Rank, double Power)>?) prev) => _prev = prev;
            public void Dispose() => (_pinned, _pinnedMap) = _prev;
        }

        private static Dictionary<string, (int Rank, double Power)> Lookup(CharacterProgression p)
        {
            if (_pinnedMap != null && ReferenceEquals(p, _pinned))
                return _pinnedMap;
            var trees = SkillTreeService.Trees;
            var table = Tables.GetValue(p, _ => new EffectTable());
            lock (table)
            {
                long sig = Signature(p, trees);
                if (sig == table.Signature && ReferenceEquals(table.Trees, trees))
                    return table.Map;

                p.EnsureSkillTreeRootsGranted();
                var map = new Dictionary<string, (int Rank, double Power)>(StringComparer.OrdinalIgnoreCase);
                foreach (var kv in p.LearnedSkillRanks)
                {
                    if (kv.Value <= 0) continue;
                    var node = trees.GetNode(kv.Key);
                    if (node == null || string.IsNullOrWhiteSpace(node.CustomEffectId)) continue;
                    double power = kv.Value * Math.Max(0, node.Power);
                    map[node.CustomEffectId] = map.TryGetValue(node.CustomEffectId, out var cur)
                        ? (Math.Max(cur.Rank, kv.Value), Math.Max(cur.Power, power))
                        : (kv.Value, power);
                }
                table.Map = map;
                table.Trees = trees;
                table.Signature = Signature(p, trees); // after roots were granted
                return map;
            }
        }
    }
}

namespace RPGGame
{
    /// <summary>
    /// "+1 Combo per 2 class tags" rules (Heavy / Drill / Soft Cadence): combo-threshold steps from the hero's class
    /// tags, × node power.
    /// </summary>
    public static class ClassCadence
    {
        private static readonly (string Effect, string ClassTag)[] Rules =
        {
            ("heavy_cadence", ClassTags.Barbarian),
            ("drill_cadence", ClassTags.Warrior),
            ("soft_cadence", ClassTags.Rogue),
            ("soft_cadence_wiz", ClassTags.Wizard)
        };

        public static int ComboSteps(Character? hero)
        {
            if (hero == null || hero is Enemy)
                return 0;
            int steps = 0;
            foreach (var (effect, classTag) in Rules)
            {
                double power = SkillRanks.GetPower(hero, effect);
                if (power > 0)
                    steps += (int)Math.Floor(ClassTags.Count(hero, classTag) / 2.0 * power);
            }
            return steps + SkillEffectRouter.Instance.ConsumePendingComboSteps(hero);
        }
    }
}
