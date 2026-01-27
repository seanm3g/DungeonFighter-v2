using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Manages character combo sequences and combo-related logic
    /// Handles adding, removing, and reordering combo actions
    /// </summary>
    public class ComboSequenceManager
    {
        private List<Action> ComboSequence { get; set; } = new();

        /// <summary>
        /// Gets the current combo sequence sorted by combo order
        /// </summary>
        public List<Action> GetComboActions()
        {
            var sortedCombo = ComboSequence.ToList();
            sortedCombo.Sort((a, b) => a.ComboOrder.CompareTo(b.ComboOrder));
            return sortedCombo;
        }

        /// <summary>
        /// Adds an action to the combo sequence if it's a valid combo action
        /// </summary>
        public void AddToCombo(Action action)
        {
            if (action.IsComboAction)
            {
                if (!ComboSequence.Any(comboAction => comboAction.Name == action.Name))
                {
                    ComboSequence.Add(action);
                    ReorderComboSequence();
                }
            }
        }

        /// <summary>
        /// Removes an action from the combo sequence
        /// </summary>
        public void RemoveFromCombo(Action action)
        {
            var actionToRemove = ComboSequence.FirstOrDefault(comboAction => comboAction.Name == action.Name);
            if (actionToRemove != null)
            {
                ComboSequence.Remove(actionToRemove);
                actionToRemove.ComboOrder = 0;
                ReorderComboSequence();
            }
        }

        /// <summary>
        /// Reorders the combo sequence and updates combo order values
        /// </summary>
        private void ReorderComboSequence()
        {
            ComboSequence.Sort((a, b) => a.ComboOrder.CompareTo(b.ComboOrder));
            for (int i = 0; i < ComboSequence.Count; i++)
            {
                ComboSequence[i].ComboOrder = i + 1;
            }
        }

        /// <summary>
        /// Initializes the default combo with weapon actions
        /// </summary>
        public void InitializeDefaultCombo(Actor entity, WeaponItem? weapon)
        {
            // Clear existing combo sequence
            ClearCombo();

            // Add the two weapon actions to the combo by default
            if (weapon != null)
            {
                var weaponActions = GetWeaponActionsForCombo(weapon);

                foreach (var actionName in weaponActions)
                {
                    // Find the action in the action pool and add it to combo
                    var actionEntry = entity.ActionPool.FirstOrDefault(item => 
                        item.action.Name == actionName);
                    
                    if (actionEntry.action != null && actionEntry.action.IsComboAction)
                    {
                        AddToCombo(actionEntry.action);
                    }
                }
            }

            // If no weapon or no weapon actions were added, ensure we have proper combo actions
            if (ComboSequence.Count == 0)
            {
                // Find any available combo actions from the action pool
                var availableComboActions = entity.ActionPool
                    .Where(item => item.action.IsComboAction)
                    .Select(item => item.action)
                    .ToList();

                if (availableComboActions.Count > 0)
                {
                    // Add the first available combo action
                    AddToCombo(availableComboActions[0]);
                }
                else
                {
                    // If no combo actions are available, entity will need actions from other sources
                    // (equipment, class, etc.)
                }
            }
        }

        /// <summary>
        /// Updates combo sequence after gear changes
        /// Removes actions from combo sequence if the exact Action object is no longer in the action pool
        /// This ensures that when an item is unequipped, its actions are removed from the combo sequence
        /// even if another source (like class actions) provides an action with the same name
        /// </summary>
        public void UpdateComboSequenceAfterGearChange(Actor entity) {  
            // Remove actions that are no longer in the action pool
            // Check by exact Action object reference, not just by name
            // This ensures that if a weapon action is removed, it's removed from combo even if
            // a class action with the same name exists
            var actionsToRemove = new List<Action>();
            foreach (var comboAction in ComboSequence)
            {
                // Check if the exact Action object is still in the action pool
                var stillInPool = entity.ActionPool.Any(item => 
                    ReferenceEquals(item.action, comboAction));
                
                if (!stillInPool)
                {
                    actionsToRemove.Add(comboAction);
                }
            }

            foreach (var action in actionsToRemove)
            {
                RemoveFromCombo(action);
            }
        }

        /// <summary>
        /// Helper method to get weapon actions for combo initialization
        /// Uses the weapon's actual actions (GearAction and ActionBonuses) instead of hardcoded values
        /// </summary>
        private List<string> GetWeaponActionsForCombo(WeaponItem weapon)
        {
            var actions = new List<string>();
            
            // Use the weapon's actual GearAction if it exists
            if (!string.IsNullOrEmpty(weapon.GearAction))
            {
                actions.Add(weapon.GearAction);
            }
            
            // Add all ActionBonuses from the weapon
            if (weapon.ActionBonuses != null)
            {
                foreach (var actionBonus in weapon.ActionBonuses)
                {
                    if (!string.IsNullOrEmpty(actionBonus.Name))
                    {
                        actions.Add(actionBonus.Name);
                    }
                }
            }
            
            // If no actions found on weapon, return empty list (fallback handled in InitializeDefaultCombo)
            return actions;
        }

        /// <summary>
        /// Clears the entire combo sequence
        /// </summary>
        public void ClearCombo()
        {
            ComboSequence.Clear();
        }
    }
}





