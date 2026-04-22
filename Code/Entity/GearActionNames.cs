using System;
using System.Collections.Generic;
using System.Linq;

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
        /// Returns ordered action names for this item (GearAction, bonuses, weapon-type fallback, armor extras).
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

            if (gear is WeaponItem weapon && actions.Count == 0)
                actions.AddRange(GetWeaponTypeActionNames(weapon.WeaponType));
            else if (gear is HeadItem || gear is ChestItem || gear is FeetItem)
                actions.AddRange(GetArmorExtraActionNames(gear));

            if (gear is WeaponItem w)
                EnsureRequiredWeaponBasicInActionNames(w.WeaponType, actions);

            return actions;
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
        public static List<string> ResolveWeapon(WeaponItem weapon)
        {
            return Resolve(weapon);
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
                    if (actionData.Tags != null &&
                        (actionData.Tags.Any(t => t.Equals("enemy", StringComparison.OrdinalIgnoreCase)) ||
                         actionData.Tags.Any(t => t.Equals("environment", StringComparison.OrdinalIgnoreCase))))
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
                                     !action.Tags.Any(tag => tag.Equals("enemy", StringComparison.OrdinalIgnoreCase)) &&
                                     !action.Tags.Any(tag => tag.Equals("environment", StringComparison.OrdinalIgnoreCase)))
                    .Select(action => action.Name)
                    .ToList();
            }

            return weaponActions;
        }

        private static List<string> GetArmorExtraActionNames(Item armor)
        {
            var actions = new List<string>();
            if (!HasSpecialArmorActions(armor))
                return actions;

            if (!string.IsNullOrEmpty(armor.GearAction))
                actions.Add(armor.GearAction);
            else
                actions.AddRange(GetRandomArmorActionList(armor));

            return actions;
        }

        private static List<string> GetRandomArmorActionList(Item armor)
        {
            var randomActionName = GetRandomArmorActionNamePreferCombo();
            if (!string.IsNullOrEmpty(randomActionName))
                return new List<string> { randomActionName };

            var allActions = ActionLoader.GetAllActions();

            var armorActions = allActions
                .Where(action => action.Tags != null &&
                                 action.Tags.Contains("armor") &&
                                 !action.Tags.Contains("environment"))
                .Select(action => action.Name)
                .ToList();

            if (armorActions.Count == 0)
            {
                armorActions = allActions
                    .Where(action => action.IsComboAction &&
                                     (action.Tags == null || (!action.Tags.Contains("environment") &&
                                                              !action.Tags.Contains("enemy") &&
                                                              !action.Tags.Contains("weapon"))))
                    .Select(action => action.Name)
                    .ToList();
            }

            if (armorActions.Count > 0)
                return new List<string> { armorActions[Random.Shared.Next(armorActions.Count)] };

            return new List<string>();
        }

        private static string? GetRandomArmorActionNamePreferCombo()
        {
            var allActions = ActionLoader.GetAllActions();
            var availableActions = allActions
                .Where(action => action.IsComboAction &&
                                 action.Tags != null &&
                                 !action.Tags.Contains("environment") &&
                                 !action.Tags.Contains("enemy") &&
                                 !action.Tags.Contains("unique") &&
                                 !action.Tags.Any(t => t.Equals("weapon", StringComparison.OrdinalIgnoreCase)))
                .Select(action => action.Name)
                .ToList();

            if (availableActions.Count > 0)
                return availableActions[Random.Shared.Next(availableActions.Count)];

            return null;
        }

        private static bool HasSpecialArmorActions(Item armor)
        {
            if (armor.Modifications.Count > 0)
                return true;
            if (armor.StatBonuses.Count > 0)
                return true;
            if (armor.ActionBonuses.Count > 0)
                return true;

            string[] basicGearNames = { "Leather Helmet", "Leather Armor", "Leather Boots", "Cloth Hood", "Cloth Robes", "Cloth Shoes" };
            if (basicGearNames.Contains(armor.Name))
                return false;

            return false;
        }
    }
}
