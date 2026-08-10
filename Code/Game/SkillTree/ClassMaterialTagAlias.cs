using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Maps class ladder materials to class tags when the Level-1 skill root is learned
    /// (tribe_metal / iron_lineage / glass_veil / mage_circle).
    /// </summary>
    public static class ClassMaterialTagAlias
    {
        public const string Barbarian = "barbarian";
        public const string Warrior = "warrior";
        public const string Rogue = "rogue";
        public const string Wizard = "wizard";

        public static readonly string[] BarbarianMaterials = { "bone", "steel", "damascus" };
        public static readonly string[] WarriorMaterials = { "bronze", "gold", "mithril" };
        public static readonly string[] RogueMaterials = { "glass", "obsidian", "shadow" };
        public static readonly string[] WizardMaterials = { "willow", "silver", "crystal" };

        public static readonly string[] BarbarianCountTags =
            { "bone", "steel", "damascus", "barbarian" };
        public static readonly string[] WarriorCountTags =
            { "bronze", "gold", "mithril", "warrior" };
        public static readonly string[] RogueCountTags =
            { "glass", "obsidian", "shadow", "rogue" };
        public static readonly string[] WizardCountTags =
            { "willow", "silver", "crystal", "wizard" };

        public static bool HasRootAlias(Character? character, string classTag)
        {
            if (character == null || string.IsNullOrWhiteSpace(classTag))
                return false;
            string id = classTag.Trim().ToLowerInvariant() switch
            {
                Barbarian => "tribe_metal",
                Warrior => "iron_lineage",
                Rogue => "glass_veil",
                Wizard => "mage_circle",
                _ => ""
            };
            return !string.IsNullOrEmpty(id) && SkillEffectRouter.Instance.HasEffect(character, id);
        }

        public static string[]? MaterialsForClassTag(string classTag) =>
            classTag.Trim().ToLowerInvariant() switch
            {
                Barbarian => BarbarianMaterials,
                Warrior => WarriorMaterials,
                Rogue => RogueMaterials,
                Wizard => WizardMaterials,
                _ => null
            };

        public static string[]? CountTagsForClass(string classTag) =>
            classTag.Trim().ToLowerInvariant() switch
            {
                Barbarian => BarbarianCountTags,
                Warrior => WarriorCountTags,
                Rogue => RogueCountTags,
                Wizard => WizardCountTags,
                _ => null
            };

        /// <summary>
        /// True when the item's material or tags match <paramref name="wantedTag"/>,
        /// including material→class alias when the matching root skill is learned.
        /// </summary>
        public static bool ItemCountsAsTag(Character? hero, Item? item, string wantedTag)
        {
            if (item == null || string.IsNullOrWhiteSpace(wantedTag))
                return false;

            string want = wantedTag.Trim();
            if (!string.IsNullOrWhiteSpace(item.Material) &&
                string.Equals(item.Material, want, StringComparison.OrdinalIgnoreCase))
                return true;

            if (item.Tags != null &&
                item.Tags.Any(t => string.Equals(t, want, StringComparison.OrdinalIgnoreCase)))
                return true;

            var materials = MaterialsForClassTag(want);
            if (materials == null || !HasRootAlias(hero, want))
                return false;

            if (!string.IsNullOrWhiteSpace(item.Material) &&
                materials.Any(m => string.Equals(item.Material, m, StringComparison.OrdinalIgnoreCase)))
                return true;

            if (item.Tags != null &&
                item.Tags.Any(it => materials.Any(m => string.Equals(it, m, StringComparison.OrdinalIgnoreCase))))
                return true;

            return false;
        }

        public static int CountEquippedClassTags(Character hero, string classTag)
        {
            var tags = CountTagsForClass(classTag);
            if (tags == null)
                return 0;

            // When root is learned, material ladders count; otherwise only explicit class tag.
            bool alias = HasRootAlias(hero, classTag);
            int count = 0;
            foreach (var item in EnumerateEquipped(hero))
            {
                if (item == null) continue;
                if (alias)
                {
                    if (ItemMatchesAny(item, tags))
                        count++;
                }
                else if (ItemMatchesExact(item, classTag))
                {
                    count++;
                }
            }
            return count;
        }

        private static IEnumerable<Item?> EnumerateEquipped(Character hero)
        {
            yield return hero.Equipment.Head;
            yield return hero.Equipment.Body;
            yield return hero.Equipment.Legs;
            yield return hero.Equipment.Feet;
            yield return hero.Equipment.Weapon;
        }

        private static bool ItemMatchesAny(Item item, string[] tags)
        {
            if (!string.IsNullOrWhiteSpace(item.Material) &&
                tags.Any(t => string.Equals(item.Material, t, StringComparison.OrdinalIgnoreCase)))
                return true;
            return item.Tags != null &&
                   item.Tags.Any(it => tags.Any(t => string.Equals(it, t, StringComparison.OrdinalIgnoreCase)));
        }

        private static bool ItemMatchesExact(Item item, string tag)
        {
            if (!string.IsNullOrWhiteSpace(item.Material) &&
                string.Equals(item.Material, tag, StringComparison.OrdinalIgnoreCase))
                return true;
            return item.Tags != null &&
                   item.Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
        }
    }
}
