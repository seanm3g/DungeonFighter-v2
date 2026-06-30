using System;
using System.Linq;

namespace RPGGame
{
    public static class EarlyGameBalanceHelper
    {
        public static int GetStartingWeaponDamageOverride(WeaponType weaponType, WeaponScalingConfig? scaling = null)
        {
            scaling ??= GameConfiguration.Instance?.WeaponScaling;
            if (scaling?.StartingWeaponDamage == null)
                return 0;

            return weaponType switch
            {
                WeaponType.Mace => scaling.StartingWeaponDamage.Mace,
                WeaponType.Sword => scaling.StartingWeaponDamage.Sword,
                WeaponType.Dagger => scaling.StartingWeaponDamage.Dagger,
                WeaponType.Wand => scaling.StartingWeaponDamage.Wand,
                _ => 0
            };
        }

        public static int GetStartingClassPointsBonus(WeaponType weaponType, EarlyGameConfig? earlyGame = null)
        {
            earlyGame ??= GameConfiguration.Instance?.EarlyGame;
            if (earlyGame?.StartingClassPointsBonus == null)
                return 0;

            return weaponType switch
            {
                WeaponType.Mace => earlyGame.StartingClassPointsBonus.Mace,
                WeaponType.Sword => earlyGame.StartingClassPointsBonus.Sword,
                WeaponType.Dagger => earlyGame.StartingClassPointsBonus.Dagger,
                WeaponType.Wand => earlyGame.StartingClassPointsBonus.Wand,
                _ => 0
            };
        }

        public static double GetStartingActionDamageMultiplier(WeaponType weaponType, EarlyGameConfig? earlyGame = null)
        {
            earlyGame ??= GameConfiguration.Instance?.EarlyGame;
            if (earlyGame?.StartingActionDamageMultiplier == null)
                return 1.0;

            double mult = weaponType switch
            {
                WeaponType.Mace => earlyGame.StartingActionDamageMultiplier.Mace,
                WeaponType.Sword => earlyGame.StartingActionDamageMultiplier.Sword,
                WeaponType.Dagger => earlyGame.StartingActionDamageMultiplier.Dagger,
                WeaponType.Wand => earlyGame.StartingActionDamageMultiplier.Wand,
                _ => 1.0
            };

            return mult > 0 ? mult : 1.0;
        }

        public static bool IsStartingWeaponAction(Action? action)
        {
            if (action?.Tags == null || action.Tags.Count == 0)
                return false;

            return action.Tags.Any(tag =>
                tag.Equals("startingWeapon", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Applies configured bonus class points and refreshes class actions for the chosen weapon path.
        /// </summary>
        public static void ApplyStartingClassPointsBonus(Character character, WeaponType weaponType)
        {
            int bonus = GetStartingClassPointsBonus(weaponType);
            if (bonus <= 0)
                return;

            for (int i = 0; i < bonus; i++)
                character.Progression.AwardClassPoint(weaponType);

            character.Actions.AddClassActions(character, character.Progression, weaponType);
        }

        /// <summary>
        /// Starting-action damage boost for heroes still within the early-game level cap.
        /// </summary>
        public static double GetStartingActionDamageMultiplier(Character character, Action? action)
        {
            if (action == null || !IsStartingWeaponAction(action))
                return 1.0;

            var earlyGame = GameConfiguration.Instance?.EarlyGame;
            if (earlyGame == null)
                return 1.0;

            int cap = earlyGame.StartingActionBonusLevelCap > 0
                ? earlyGame.StartingActionBonusLevelCap
                : 5;
            if (character.Level > cap)
                return 1.0;

            if (character.Weapon is not WeaponItem weapon)
                return 1.0;

            return GetStartingActionDamageMultiplier(weapon.WeaponType, earlyGame);
        }

        public static bool TryParseWeaponType(string? token, out WeaponType weaponType)
        {
            weaponType = default;
            if (string.IsNullOrWhiteSpace(token))
                return false;

            return Enum.TryParse(token.Trim(), ignoreCase: true, out weaponType);
        }

        public static bool TryParseClassBalanceKey(string? token, out WeaponType weaponType, out string classKey)
        {
            classKey = "";
            weaponType = default;
            if (string.IsNullOrWhiteSpace(token))
                return false;

            string normalized = token.Trim().ToLowerInvariant();
            switch (normalized)
            {
                case "barbarian":
                case "mace":
                    weaponType = WeaponType.Mace;
                    classKey = "barbarian";
                    return true;
                case "warrior":
                case "sword":
                    weaponType = WeaponType.Sword;
                    classKey = "warrior";
                    return true;
                case "rogue":
                case "dagger":
                    weaponType = WeaponType.Dagger;
                    classKey = "rogue";
                    return true;
                case "wizard":
                case "wand":
                    weaponType = WeaponType.Wand;
                    classKey = "wizard";
                    return true;
                default:
                    return false;
            }
        }

        public static string GetClassBalanceKey(WeaponType weaponType) =>
            weaponType switch
            {
                WeaponType.Mace => "barbarian",
                WeaponType.Sword => "warrior",
                WeaponType.Dagger => "rogue",
                WeaponType.Wand => "wizard",
                _ => weaponType.ToString().ToLowerInvariant()
            };

        public static ClassMultipliers GetClassMultipliers(string classKey)
        {
            var balance = GameConfiguration.Instance.ClassBalance;
            return classKey.ToLowerInvariant() switch
            {
                "barbarian" => balance.Barbarian,
                "warrior" => balance.Warrior,
                "rogue" => balance.Rogue,
                "wizard" => balance.Wizard,
                _ => new ClassMultipliers { DamageMultiplier = 1, HealthMultiplier = 1, SpeedMultiplier = 1 }
            };
        }
    }
}
