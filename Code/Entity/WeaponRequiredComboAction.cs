using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Each equipped weapon type has one "basic" combo action that must stay in the player's combo sequence.
    /// Marked in action data with tag <c>weapon_basic</c> plus a matching <see cref="ActionData.WeaponTypes"/> entry.
    /// </summary>
    public static class WeaponRequiredComboAction
    {
        public const string WeaponBasicTag = "weapon_basic";

        private static readonly Dictionary<WeaponType, string> FallbackBasicByType = new()
        {
            { WeaponType.Sword, "STRIKE" },
            { WeaponType.Dagger, "STAB" },
            { WeaponType.Mace, "SLAM" },
            { WeaponType.Wand, "CAST" },
        };

        /// <summary>
        /// Resolved basic action name for this weapon type (data-driven when tagged; otherwise canonical fallback).
        /// </summary>
        public static string? TryGetRequiredBasicActionName(WeaponType weaponType)
        {
            var typeName = weaponType.ToString();
            foreach (var data in ActionLoader.GetAllActionData())
            {
                if (data.Tags == null || data.WeaponTypes == null)
                    continue;
                if (!data.Tags.Any(t => t.Equals(WeaponBasicTag, StringComparison.OrdinalIgnoreCase)))
                    continue;
                if (!data.WeaponTypes.Any(wt => wt.Equals(typeName, StringComparison.OrdinalIgnoreCase)))
                    continue;
                if (!string.IsNullOrWhiteSpace(data.Name))
                    return data.Name;
            }

            return FallbackBasicByType.TryGetValue(weaponType, out var fb) ? fb : null;
        }

        /// <summary>
        /// True when <paramref name="action"/> is the required basic for the character's currently equipped weapon.
        /// </summary>
        public static bool IsRequiredBasicForEquippedWeapon(Character? character, Action? action)
        {
            if (character == null || action == null)
                return false;
            if (character.Equipment?.Weapon is not WeaponItem weapon)
                return false;
            var required = TryGetRequiredBasicActionName(weapon.WeaponType);
            return !string.IsNullOrEmpty(required) &&
                   string.Equals(action.Name, required, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// False when removing this instance would leave the sequence without the required basic (same name) for the equipped weapon.
        /// </summary>
        public static bool CanRemoveFromCombo(Character? character, Action? action)
        {
            if (character == null || action == null)
                return true;
            if (character.Equipment?.Weapon is not WeaponItem weapon)
                return true;
            var requiredName = TryGetRequiredBasicActionName(weapon.WeaponType);
            if (string.IsNullOrEmpty(requiredName) ||
                !string.Equals(action.Name, requiredName, StringComparison.OrdinalIgnoreCase))
                return true;

            var combo = character.GetComboActions();
            int withName = combo.Count(a => a != null &&
                string.Equals(a.Name, requiredName, StringComparison.OrdinalIgnoreCase));
            return withName > 1;
        }

        /// <summary>
        /// If the actor is a character with a weapon equipped and the required basic is missing from the combo but present in the pool, adds it.
        /// </summary>
        public static void EnsureRequiredBasicInCombo(Actor actor)
        {
            if (actor is not Character character)
                return;
            if (character.Equipment?.Weapon is not WeaponItem weapon)
                return;
            var requiredName = TryGetRequiredBasicActionName(weapon.WeaponType);
            if (string.IsNullOrEmpty(requiredName))
                return;

            var combo = character.GetComboActions();
            if (combo.Any(a => a != null && string.Equals(a.Name, requiredName, StringComparison.OrdinalIgnoreCase)))
                return;

            if (character.ActionPool == null)
                return;

            foreach (var (poolAction, _) in character.ActionPool)
            {
                if (poolAction != null &&
                    string.Equals(poolAction.Name, requiredName, StringComparison.OrdinalIgnoreCase) &&
                    poolAction.IsComboAction)
                {
                    character.AddToCombo(poolAction);
                    return;
                }
            }
        }
    }
}
