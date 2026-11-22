using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Data
{
    /// <summary>
    /// Which actions can roll on which gear, from action tags (so new actions join the right pool automatically).
    /// Class actions carry a class tag (barbarian / warrior / rogue / wizard) or a weapon tag (mace / sword / dagger / wand);
    /// hybrid actions carry both of their classes' tags; neutral actions carry neither.
    /// Never on gear: weapon starter attacks (startingweapon / weapon_basic / required), <c>unique</c> rows and material
    /// set converts (granted by the worn set, <see cref="ActionData.MaterialScale"/>).
    /// The item roller (<see cref="ItemGeneration.ItemRoller"/>) picks gear actions: weapons lean to their own class's actions,
    /// every other slot draws from the whole pool, tilted by the item's influence tags.
    /// </summary>
    public static class GearActionPools
    {
        private static readonly string[] ClassTags = { "barbarian", "warrior", "rogue", "wizard" };
        private static readonly string[] WeaponTags = { "mace", "sword", "dagger", "wand" };
        private static readonly string[] BasicAttackTags = { "startingweapon", "weapon_basic", "required", "unique" };

        /// <summary>Class key and weapon tag for a weapon type (Mace → barbarian / mace).</summary>
        public static (string classTag, string weaponTag) TagsFor(WeaponType weaponType) =>
            (EarlyGameBalanceHelper.GetClassBalanceKey(weaponType), weaponType.ToString().ToLowerInvariant());

        /// <summary>Weapon type for a class name (Barbarian → Mace); null when unknown.</summary>
        public static WeaponType? WeaponTypeForClass(string? playerClass)
        {
            foreach (WeaponType type in new[] { WeaponType.Mace, WeaponType.Sword, WeaponType.Dagger, WeaponType.Wand })
                if (string.Equals(TagsFor(type).classTag, playerClass?.Trim(), StringComparison.OrdinalIgnoreCase))
                    return type;
            return null;
        }

        public static bool IsBasicAttack(IEnumerable<string>? tags) => HasAny(tags, BasicAttackTags);

        public static bool IsNeutral(IEnumerable<string>? tags) => !HasAny(tags, ClassTags) && !HasAny(tags, WeaponTags);

        public static bool IsClassAction(IEnumerable<string>? tags, WeaponType weaponType)
        {
            var (classTag, weaponTag) = TagsFor(weaponType);
            return HasAny(tags, new[] { classTag, weaponTag });
        }

        /// <summary>True when the action carries any of the hero's class tags (or their weapon tags); hybrids count for both classes.</summary>
        public static bool IsClassActionFor(IEnumerable<string>? tags, IEnumerable<string> heroClassTags)
        {
            foreach (string classTag in heroClassTags)
            {
                var weaponType = RPGGame.ClassTags.WeaponOf(classTag);
                if (weaponType.HasValue ? IsClassAction(tags, weaponType.Value) : HasAny(tags, new[] { classTag }))
                    return true;
            }
            return false;
        }

        /// <summary>In the hero's 95% pool: neutral, or tagged with one of the hero's classes.</summary>
        public static bool IsInClassPool(IEnumerable<string>? tags, IReadOnlyCollection<string> heroClassTags) =>
            IsNeutral(tags) || IsClassActionFor(tags, heroClassTags);

        /// <summary>False for starter attacks, <c>unique</c> rows, material set converts and retired cards (step 11 cut).</summary>
        public static bool CanRollOnGear(ActionData? data) =>
            data == null || (!IsBasicAttack(data.Tags) && string.IsNullOrWhiteSpace(data.MaterialScale)
                && !GameDataTagHelper.IsRetired(data.Tags));

        /// <summary>The hero's class tags for <paramref name="item"/>: <paramref name="heroClassTags"/>, else the weapon's own class.</summary>
        public static IReadOnlyCollection<string> ResolveClassTags(Item item, IReadOnlyCollection<string>? heroClassTags)
        {
            if (heroClassTags != null && heroClassTags.Count > 0)
                return heroClassTags;
            if (item is WeaponItem weapon && RPGGame.ClassTags.ClassOf(weapon.WeaponType) is string tag)
                return new[] { tag };
            return Array.Empty<string>();
        }

        private static bool HasAny(IEnumerable<string>? tags, IEnumerable<string> wanted) =>
            tags != null && tags.Any(t => wanted.Any(w => string.Equals(t?.Trim(), w, StringComparison.OrdinalIgnoreCase)));
    }
}
