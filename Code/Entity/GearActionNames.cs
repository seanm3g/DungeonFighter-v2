using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Single source of truth for action names contributed by a piece of gear (weapon or armor).
    /// Used by <see cref="GearActionManager"/>, <see cref="EquipmentActionProvider"/>, and
    /// <see cref="ComboSequenceManager"/> default combo initialization so inventory,
    /// action pool rebuild, and combo defaults stay aligned.
    /// </summary>
    public static class GearActionNames
    {
        /// <summary>
        /// Returns ordered action names for this item (explicit GearAction, rolled ActionBonuses, and weapon-type fallback).
        /// </summary>
        public static List<string> Resolve(Item? gear)
        {
            var actions = new List<string>();
            if (gear == null)
                return actions;

            if (!string.IsNullOrEmpty(gear.GearAction))
                actions.Add(gear.GearAction);

            foreach (var actionBonus in gear.ActionBonuses)
            {
                if (!string.IsNullOrEmpty(actionBonus.Name))
                    actions.Add(actionBonus.Name);
            }

            bool isWeapon = TryResolveWeaponType(gear, out var weaponType);

            if (isWeapon && actions.Count == 0)
                actions.AddRange(GetWeaponTypeActionNames(weaponType));

            if (isWeapon)
                EnsureRequiredWeaponBasicInActionNames(weaponType, actions);

            return CanonicalizeLoadedActionNames(StripNonHeroGearActionNames(actions));
        }

        /// <summary>
        /// Resolves weapon type for both fully typed weapons and base <see cref="Item"/> instances in a weapon slot.
        /// </summary>
        public static bool TryResolveWeaponType(Item? gear, out WeaponType weaponType)
        {
            weaponType = WeaponType.Sword;
            if (gear == null || gear.Type != ItemType.Weapon)
                return false;

            if (gear is WeaponItem weapon)
            {
                weaponType = weapon.WeaponType;
                return true;
            }

            if (WeaponTypeFromCatalog.TryGetByWeaponName(gear.Name, out var catalogType))
            {
                weaponType = catalogType;
                return true;
            }

            weaponType = gear.WeaponType;
            return true;
        }

        /// <summary>
        /// Drops <c>environment</c> and <c>enemy</c> actions so hero gear never grants room hazards or enemy-only moves.
        /// </summary>
        private static List<string> StripNonHeroGearActionNames(List<string> names)
        {
            if (names.Count == 0)
                return names;
            ActionLoader.LoadActions();
            var kept = new List<string>();
            foreach (var n in names)
            {
                if (string.IsNullOrEmpty(n))
                    continue;
                if (!GameDataTagHelper.IsGrantableOnHeroGearByName(n))
                    continue;
                kept.Add(n);
            }
            return kept;
        }

        /// <summary>
        /// Aligns resolved gear action names with keys in loaded action data so inventory previews and pool loading match.
        /// </summary>
        private static List<string> CanonicalizeLoadedActionNames(List<string> names)
        {
            if (names.Count == 0)
                return names;

            ActionLoader.LoadActions();
            var canonical = new List<string>(names.Count);
            foreach (var n in names)
            {
                if (string.IsNullOrEmpty(n))
                    continue;
                var resolved = ActionLoader.ResolveActionName(n);
                if (!string.IsNullOrEmpty(resolved) && ActionLoader.HasAction(resolved))
                    canonical.Add(resolved);
                else
                    canonical.Add(n);
            }

            return canonical;
        }

        /// <summary>
        /// The per-type required basic (<see cref="WeaponRequiredComboAction"/>) must exist in the pool for
        /// removal guards and <see cref="WeaponRequiredComboAction.EnsureRequiredBasicInCombo"/> to work.
        /// Starter or loot weapons may list only non-basic actions in <see cref="Item.GearAction"/> / bonuses.
        /// </summary>
        private static void EnsureRequiredWeaponBasicInActionNames(WeaponType weaponType, List<string> actions)
        {
            var required = WeaponRequiredComboAction.TryGetRequiredBasicActionName(weaponType);
            if (string.IsNullOrEmpty(required))
                return;
            if (actions.Any(a => string.Equals(a, required, StringComparison.OrdinalIgnoreCase)))
                return;
            actions.Insert(0, required);
        }

        /// <summary>
        /// Action names from a weapon only (same rules as <see cref="Resolve"/> for <see cref="WeaponItem"/>).
        /// </summary>
        public static List<string> ResolveWeapon(Item weapon)
        {
            return Resolve(weapon);
        }

        /// <summary>
        /// Returns the action names granted to every weapon of this type from action data.
        /// Useful for settings/catalog previews where an item instance does not exist yet.
        /// </summary>
        public static List<string> ResolveWeaponType(WeaponType weaponType)
        {
            var actions = GetWeaponTypeActionNames(weaponType);
            EnsureRequiredWeaponBasicInActionNames(weaponType, actions);
            return StripNonHeroGearActionNames(actions);
        }

        private static List<string> GetWeaponTypeActionNames(WeaponType weaponType)
        {
            var weaponTag = weaponType.ToString().ToLowerInvariant();
            var weaponTypeName = weaponType.ToString();
            var allActionData = ActionLoader.GetAllActionData();
            var weaponActions = new List<string>();

            foreach (var actionData in allActionData)
            {
                if (actionData.WeaponTypes != null &&
                    actionData.WeaponTypes.Any(wt => wt.Equals(weaponTypeName, StringComparison.OrdinalIgnoreCase)))
                {
                    if (!GameDataTagHelper.IsGrantableOnHeroGear(actionData.Tags))
                        continue;
                    weaponActions.Add(actionData.Name);
                }
            }

            if (weaponActions.Count == 0)
            {
                var allActions = ActionLoader.GetAllActions();
                weaponActions = allActions
                    .Where(action => action.Tags != null &&
                                     action.Tags.Any(tag => tag.Equals("weapon", StringComparison.OrdinalIgnoreCase)) &&
                                     action.Tags.Any(tag => tag.Equals(weaponTag, StringComparison.OrdinalIgnoreCase)) &&
                                     !action.Tags.Any(tag => tag.Equals("unique", StringComparison.OrdinalIgnoreCase)) &&
                                     !action.Tags.Any(tag => tag.Equals("class", StringComparison.OrdinalIgnoreCase)) &&
                                     GameDataTagHelper.IsGrantableOnHeroGear(action.Tags))
                    .Select(action => action.Name)
                    .ToList();
            }

            return weaponActions;
        }

    }
}
