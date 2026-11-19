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
        public static int CountOnItem(Character? hero, Item? item, string classTag) =>
            (ItemHasTag(item, classTag) ? 1 : 0) + (MaterialCounts(hero, item, classTag) ? 1 : 0);

        private static bool ItemHasTag(Item? item, string classTag) =>
            item?.Tags != null && item.Tags.Any(t => string.Equals(t, classTag, StringComparison.OrdinalIgnoreCase));

        /// <summary>True when <paramref name="item"/>'s material is one of the class's and the Core root is learned.</summary>
        private static bool MaterialCounts(Character? hero, Item? item, string classTag)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Material) || !MaterialsCountAsClass(hero, classTag))
                return false;
            string material = ItemMaterialRules.RemapLegacyMaterial(item.Material);
            return MaterialsOf(classTag).Any(m => string.Equals(m, material, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Gear class tags (direct tag or counted material) only, no strip actions.</summary>
        public static int CountOnGear(Character? hero, string classTag)
        {
            if (hero == null || string.IsNullOrWhiteSpace(classTag))
                return 0;
            int count = 0;
            foreach (var item in new Item?[] { hero.Weapon, hero.Head, hero.Body, hero.Legs, hero.Feet })
                count += CountOnItem(hero, item, classTag);
            return count;
        }

        /// <summary>Where <see cref="CountOwn"/> comes from: gear tags, gear materials (after the root), strip actions.</summary>
        public static (int GearTags, int Materials, int Strip) Breakdown(Character? hero, string classTag)
        {
            if (hero == null || string.IsNullOrWhiteSpace(classTag))
                return (0, 0, 0);
            int gearTags = 0, materials = 0;
            foreach (var item in new Item?[] { hero.Weapon, hero.Head, hero.Body, hero.Legs, hero.Feet })
            {
                if (ItemHasTag(item, classTag)) gearTags++;
                if (MaterialCounts(hero, item, classTag)) materials++;
            }
            return (gearTags, materials, CountOwn(hero, classTag) - gearTags - materials);
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
            int count = CountOnGear(hero, classTag);
            foreach (var action in hero.GetComboActions())
                if (CrossWires.HasTag(hero, action, classTag))
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
            return WithGear(c, customEffectId, Lookup(c.Progression).TryGetValue(customEffectId, out var e) ? e : (0, 0)).Rank;
        }

        /// <summary>
        /// Worn <c>RANK node n</c> modifiers raise a learned node's effective rank (and its rank × power) for the effects the
        /// router runs; learned ranks (prerequisites, the skill screen) are unchanged.
        /// </summary>
        private static (int Rank, double Power) WithGear(Character c, string customEffectId, (int Rank, double Power) e)
        {
            if (c is Enemy)
                return e;
            var bonus = CrossWires.SkillRankBonuses(c);
            if (bonus.Count == 0)
                return e;
            foreach (var kv in bonus)
            {
                var node = SkillTreeService.Trees?.GetNode(kv.Key);
                int learned = c.Progression.GetSkillRank(kv.Key);
                if (node == null || learned < 1 || kv.Value <= 0
                    || !string.Equals(node.CustomEffectId, customEffectId, StringComparison.OrdinalIgnoreCase))
                    continue;
                int rank = learned + kv.Value;
                e = (Math.Max(e.Rank, rank), Math.Max(e.Power, rank * Math.Max(0, node.Power)));
            }
            return e;
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
            return WithGear(c, customEffectId, Lookup(c.Progression).TryGetValue(customEffectId, out var e) ? e : (0, 0)).Power;
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
    /// Class cadence. The "+1 Combo per 2 class tags" nodes (Heavy / Drill / Soft / Weave Cadence) are rules now
    /// (<c>WHILE -> COMBO 1 PER TAGS barbarian 2</c>), applied with every other COMBO rule; this keeps Spellblade's
    /// banked steps and the tooltip's view of those rules.
    /// </summary>
    public static class ClassCadence
    {
        /// <summary>Spellblade's banked combo steps for the swing being resolved (spends them).</summary>
        public static int ComboSteps(Character? hero)
        {
            if (hero == null || hero is Enemy)
                return 0;
            return SkillEffectRouter.Instance.ConsumePendingComboSteps(hero);
        }

        /// <summary>Learned COMBO rules that scale with <paramref name="classTag"/>'s tags.</summary>
        private static IEnumerable<CrossWireRule> CadenceRules(Character hero, string? classTag) =>
            CrossWires.LearnedNodeRules(hero).Select(nr => nr.Rule)
                .Where(r => r.IsPassive && r.Verb == "COMBO" && r.Per?.Kind == "tags"
                    && ClassTags.All.Contains(r.Per.Arg, StringComparer.OrdinalIgnoreCase)
                    && (classTag == null || string.Equals(r.Per.Arg, classTag, StringComparison.OrdinalIgnoreCase)));

        /// <summary>Combo steps from the learned cadence rules alone (already in the hero's COMBO rule bonus).</summary>
        public static int RuleSteps(Character? hero)
        {
            if (hero == null || hero is Enemy)
                return 0;
            double steps = 0;
            foreach (var rule in CadenceRules(hero, null))
                steps += rule.Amount * CrossWires.PerValue(hero, null, rule.Per!);
            return (int)Math.Round(steps);
        }

        /// <summary>Has <paramref name="hero"/> learned the cadence rule for <paramref name="classTag"/>?</summary>
        public static bool HasRule(Character? hero, string classTag) =>
            hero != null && hero is not Enemy && CadenceRules(hero, classTag).Any();
    }
}
