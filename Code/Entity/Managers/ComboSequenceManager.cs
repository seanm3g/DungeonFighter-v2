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
        /// Allows duplicate actions (same name) by checking if the exact Action object is already in combo
        /// This supports items that have the same action multiple times
        /// </summary>
        public void AddToCombo(Action action)
        {
            if (action.IsComboAction)
            {
                // Check if this exact Action object is already in the combo
                // This allows multiple instances of the same action name (duplicates)
                bool alreadyInCombo = ComboSequence.Contains(action);

                if (!alreadyInCombo)
                {
                    ComboSequence.Add(action);
                    ReorderComboSequence();
                }
            }
        }

        /// <summary>
        /// Removes an action from the combo sequence
        /// Removes the exact Action object if it exists in the combo
        /// </summary>
        public void RemoveFromCombo(Action action)
        {
            if (ComboSequence.Remove(action))
            {
                action.ComboOrder = 0;
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
        /// </summary>
        public void UpdateComboSequenceAfterGearChange(Actor entity) {  
            // Remove actions that are no longer in the action pool
            var actionsToRemove = new List<Action>();
            foreach (var comboAction in ComboSequence)
            {
                var stillInPool = entity.ActionPool.Any(item => 
                    item.action.Name == comboAction.Name);
                
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





