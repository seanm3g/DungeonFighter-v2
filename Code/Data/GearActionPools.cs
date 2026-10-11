using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Data
{
    /// <summary>
    /// Which actions can roll on which gear, from action tags (so new actions join the right pool automatically).
    /// Class actions carry a class tag (barbarian / warrior / rogue / wizard) or a weapon tag (mace / sword / dagger / wand);
    /// neutral actions carry neither. Weapon starter attacks (startingweapon / weapon_basic / required) never roll on gear.
    /// A weapon rolls its own class's actions <see cref="LootSystemConfig.WeaponGearActionClassShare"/> of the time,
    /// otherwise a neutral action; armor uses <see cref="LootSystemConfig.ArmorGearActionClassShare"/> with the hero's class.
    /// </summary>
    public static class GearActionPools
    {
        private static readonly string[] ClassTags = { "barbarian", "warrior", "rogue", "wizard" };
        private static readonly string[] WeaponTags = { "mace", "sword", "dagger", "wand" };
        private static readonly string[] BasicAttackTags = { "startingweapon", "weapon_basic", "required" };

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

        /// <summary>
        /// Picks a gear action for <paramref name="item"/> from <paramref name="pool"/> (weighted by <see cref="ActionBonus.Weight"/>).
        /// <paramref name="heroWeaponType"/> is the hero's class for armor rolls (null = unknown, armor stays neutral).
        /// </summary>
        public static ActionBonus? Pick(Item item, IReadOnlyList<ActionBonus> pool, Random random, WeaponType? heroWeaponType = null)
        {
            var loot = GameConfiguration.Instance.LootSystem;
            WeaponType? classType = item is WeaponItem weapon ? weapon.WeaponType : heroWeaponType;
            double classShare = item is WeaponItem ? loot.WeaponGearActionClassShare : loot.ArmorGearActionClassShare;

            var candidates = pool
                .Select(ab => (bonus: ab, tags: ActionLoader.GetActionData(ab.Name)?.Tags))
                .Where(x => !IsBasicAttack(x.tags))
                .ToList();
            var neutral = candidates.Where(x => IsNeutral(x.tags)).Select(x => x.bonus).ToList();
            var classPool = classType.HasValue
                ? candidates.Where(x => IsClassAction(x.tags, classType.Value)).Select(x => x.bonus).ToList()
                : new List<ActionBonus>();

            bool useClass = classPool.Count > 0 && (neutral.Count == 0 || random.NextDouble() < classShare);
            return PickWeighted(useClass ? classPool : neutral, random);
        }

        private static ActionBonus? PickWeighted(List<ActionBonus> options, Random random)
        {
            if (options.Count == 0)
                return null;
            int total = options.Sum(a => Math.Max(1, a.Weight));
            int roll = random.Next(total);
            foreach (var option in options)
            {
                roll -= Math.Max(1, option.Weight);
                if (roll < 0)
                    return option;
            }
            return options[^1];
        }

        private static bool HasAny(IEnumerable<string>? tags, IEnumerable<string> wanted) =>
            tags != null && tags.Any(t => wanted.Any(w => string.Equals(t?.Trim(), w, StringComparison.OrdinalIgnoreCase)));
    }
}
