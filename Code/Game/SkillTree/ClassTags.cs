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

        /// <summary>Class tags on equipped gear (materials count after the Core root) plus tags on strip actions.</summary>
        public static int Count(Character? hero, string classTag)
        {
            if (hero == null || string.IsNullOrWhiteSpace(classTag))
                return 0;
            int count = 0;
            foreach (var item in new Item?[] { hero.Weapon, hero.Head, hero.Body, hero.Legs, hero.Feet })
                count += CountOnItem(hero, item, classTag);
            foreach (var action in hero.GetComboActions())
                if (ActionHas(action, classTag))
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
            c.Progression.EnsureSkillTreeRootsGranted();
            int best = 0;
            foreach (var kv in c.Progression.LearnedSkillRanks)
            {
                if (kv.Value <= 0) continue;
                var node = SkillTreeService.Trees.GetNode(kv.Key);
                if (node != null &&
                    string.Equals(node.CustomEffectId, customEffectId, StringComparison.OrdinalIgnoreCase))
                    best = Math.Max(best, kv.Value);
            }
            return best;
        }

        /// <summary>Rank × the node's <see cref="SkillTreeNodeDefinition.Power"/>.</summary>
        public static double GetPower(Character? c, string customEffectId)
        {
            if (c?.Progression == null || string.IsNullOrWhiteSpace(customEffectId)) return 0;
            c.Progression.EnsureSkillTreeRootsGranted();
            double best = 0;
            foreach (var kv in c.Progression.LearnedSkillRanks)
            {
                if (kv.Value <= 0) continue;
                var node = SkillTreeService.Trees.GetNode(kv.Key);
                if (node != null &&
                    string.Equals(node.CustomEffectId, customEffectId, StringComparison.OrdinalIgnoreCase))
                    best = Math.Max(best, kv.Value * Math.Max(0, node.Power));
            }
            return best;
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
            return steps;
        }
    }
}
