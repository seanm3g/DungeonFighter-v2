using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Manages and provides actions from equipped gear (weapons and armor).
    /// Handles action retrieval from JSON, armor-specific actions, and action bonuses.
    /// </summary>
    public class EquipmentActionProvider
    {
        private readonly EquipmentSlotManager slots;

        public EquipmentActionProvider(EquipmentSlotManager slots)
        {
            this.slots = slots;
        }

        /// <summary>
        /// Gets all available actions from a piece of equipment.
        /// Matches the behavior of GearActionManager.GetGearActions to ensure inventory display matches equipped actions.
        /// </summary>
        /// <param name="gear">The equipment piece to get actions from</param>
        /// <returns>List of action names from this gear</returns>
        public List<string> GetGearActions(Item gear)
        {
            var actions = new List<string>();

            // Add the specific GearAction assigned to this item (if any)
            if (!string.IsNullOrEmpty(gear.GearAction))
            {
                actions.Add(gear.GearAction);
            }

            // Add action bonuses from any gear
            foreach (var actionBonus in gear.ActionBonuses)
            {
                if (!string.IsNullOrEmpty(actionBonus.Name))
                {
                    actions.Add(actionBonus.Name);
                }
            }

            // For weapons: If no actions found, fall back to ALL weapon-type actions
            // This ensures weapons ALWAYS show at least one action in inventory
            // This matches what gets equipped (GearActionManager behavior)
            if (gear is WeaponItem weapon && actions.Count == 0)
            {
                actions.AddRange(GetWeaponActions(weapon));
            }
            else if (gear is HeadItem || gear is ChestItem || gear is FeetItem)
            {
                actions.AddRange(GetArmorActions(gear));
            }

            return actions;
        }

        /// <summary>
        /// Gets all actions from all equipped gear.
        /// </summary>
        public List<string> GetAllEquippedGearActions()
        {
            var allActions = new List<string>();

            var equippedItems = slots.GetEquippedItems();
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    allActions.AddRange(GetGearActions(item));
                }
            }

            return allActions.Distinct().ToList();
        }

        /// <summary>
        /// Gets actions specific to a weapon using tag-based matching from JSON.
        /// </summary>
        private List<string> GetWeaponActions(WeaponItem weapon)
        {
            var weaponTag = weapon.WeaponType.ToString().ToLower();
            var allActions = ActionLoader.GetAllActions();

            // Get weapon-specific actions from JSON using tag matching
            // Actions must have both "weapon" tag and the weapon type tag (e.g., "wand", "mace")
            // Exclude "class" tagged actions (class-only actions cannot appear on weapons)
            var weaponActions = allActions
                .Where(action => action.Tags != null &&
                                action.Tags.Any(tag => tag.Equals("weapon", StringComparison.OrdinalIgnoreCase)) &&
                                action.Tags.Any(tag => tag.Equals(weaponTag, StringComparison.OrdinalIgnoreCase)) &&
                                !action.Tags.Any(tag => tag.Equals("unique", StringComparison.OrdinalIgnoreCase)) &&
                                !action.Tags.Any(tag => tag.Equals("class", StringComparison.OrdinalIgnoreCase)))
                .Select(action => action.Name)
                .ToList();

            // Return empty list if no weapon-specific actions found
            // (No fallback - weapons should have actions defined)
            return weaponActions;
        }

        /// <summary>
        /// Gets actions specific to a piece of armor.
        /// </summary>
        private List<string> GetArmorActions(Item armor)
        {
            var actions = new List<string>();

            // Check if armor has special properties warranting actions
            if (HasSpecialArmorActions(armor))
            {
                if (!string.IsNullOrEmpty(armor.GearAction))
                {
                    actions.Add(armor.GearAction);
                }
                else
                {
                    actions.AddRange(GetRandomArmorAction(armor));
                }
            }

            return actions;
        }

        /// <summary>
        /// Gets a random armor action from available options.
        /// </summary>
        private List<string> GetRandomArmorAction(Item armor)
        {
            var randomActionName = GetRandomArmorActionName();
            if (!string.IsNullOrEmpty(randomActionName))
            {
                return new List<string> { randomActionName };
            }

            var allActions = ActionLoader.GetAllActions();

            // First try: Get armor-tagged actions
            var armorActions = allActions
                .Where(action => action.Tags.Contains("armor") &&
                                !action.Tags.Contains("environment"))
                .Select(action => action.Name)
                .ToList();

            // Fallback: Get combo actions
            if (armorActions.Count == 0)
            {
                armorActions = allActions
                    .Where(action => action.IsComboAction &&
                                    !action.Tags.Contains("environment") &&
                                    !action.Tags.Contains("enemy") &&
                                    !action.Tags.Contains("weapon"))
                    .Select(action => action.Name)
                    .ToList();
            }

            // Final fallback: Return best available
            if (armorActions.Count > 0)
            {
                var selectedAction = armorActions[Random.Shared.Next(armorActions.Count)];
                return new List<string> { selectedAction };
            }

            return new List<string>();
        }

        /// <summary>
        /// Gets a random armor action name from available combo actions.
        /// </summary>
        private string? GetRandomArmorActionName()
        {
            var allActions = ActionLoader.GetAllActions();
            var availableActions = allActions
                .Where(action => action.IsComboAction &&
                               !action.Tags.Contains("environment") &&
                               !action.Tags.Contains("enemy") &&
                               !action.Tags.Contains("unique"))
                .Select(action => action.Name)
                .ToList();

            if (availableActions.Count > 0)
            {
                return availableActions[Random.Shared.Next(availableActions.Count)];
            }

            return null;
        }

        /// <summary>
        /// Checks if an armor piece has special properties (modifications, stat bonuses, action bonuses).
        /// </summary>
        private bool HasSpecialArmorActions(Item armor)
        {
            // Has modifications
            if (armor.Modifications.Count > 0)
                return true;

            // Has stat bonuses
            if (armor.StatBonuses.Count > 0)
                return true;

            // Has action bonuses
            if (armor.ActionBonuses.Count > 0)
                return true;

            // Exclude basic starting gear
            string[] basicGearNames = { "Leather Helmet", "Leather Armor", "Leather Boots", "Cloth Hood", "Cloth Robes", "Cloth Shoes" };
            if (basicGearNames.Contains(armor.Name))
                return false;

            return false;
        }
    }
}

