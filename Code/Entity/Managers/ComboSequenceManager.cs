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
        /// Reorders the combo sequence and updates combo order values.
        /// Openers are forced to the first slot(s), finishers to the last slot(s); middle actions keep relative order by ComboOrder.
        /// </summary>
        private void ReorderComboSequence()
        {
            ComboSequence.Sort((a, b) => a.ComboOrder.CompareTo(b.ComboOrder));
            var openers = new List<Action>();
            var middle = new List<Action>();
            var finishers = new List<Action>();
            foreach (var a in ComboSequence)
            {
                bool isOpener = a.ComboRouting?.IsOpener == true;
                bool isFinisher = a.ComboRouting?.IsFinisher == true;
                if (isOpener && !isFinisher)
                    openers.Add(a);
                else if (isFinisher)
                    finishers.Add(a);
                else
                    middle.Add(a);
            }
            var ordered = new List<Action>();
            ordered.AddRange(openers);
            ordered.AddRange(middle);
            ordered.AddRange(finishers);
            for (int i = 0; i < ordered.Count; i++)
            {
                ordered[i].ComboOrder = i + 1;
            }
            ComboSequence.Clear();
            ComboSequence.AddRange(ordered);
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

            WeaponRequiredComboAction.EnsureRequiredBasicInCombo(entity);
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

            WeaponRequiredComboAction.EnsureRequiredBasicInCombo(entity);
        }

        /// <summary>
        /// Weapon action names for combo initialization — same rules as <see cref="GearActionNames.Resolve"/> / action pool.
        /// </summary>
        private static List<string> GetWeaponActionsForCombo(WeaponItem weapon)
        {
            return GearActionNames.ResolveWeapon(weapon);
        }

        /// <summary>
        /// Clears the entire combo sequence
        /// </summary>
        public void ClearCombo()
        {
            ComboSequence.Clear();
        }

        /// <summary>
        /// Restores the combo sequence from a list of action names.
        /// Matches actions by name from the entity's ActionPool and adds them in order.
        /// Skips names that are not in the pool or not combo actions.
        /// </summary>
        /// <param name="entity">The actor whose ActionPool to use</param>
        /// <param name="actionNames">Ordered list of action names to restore</param>
        /// <returns>True if at least one action was restored; false if combo is empty</returns>
        public bool RestoreComboFromActionNames(Actor entity, IReadOnlyList<string> actionNames)
        {
            if (actionNames == null || actionNames.Count == 0)
                return false;

            ClearCombo();

            int nextSlot = 1;
            foreach (var actionName in actionNames)
            {
                if (string.IsNullOrWhiteSpace(actionName))
                    continue;

                var actionEntry = entity.ActionPool.FirstOrDefault(item =>
                    string.Equals(item.action.Name, actionName, StringComparison.OrdinalIgnoreCase));

                if (actionEntry.action != null && actionEntry.action.IsComboAction)
                {
                    // Fresh Action instances from ActionLoader keep JSON ComboOrder; ReorderComboSequence sorts by it.
                    // Match saved name order so the combo matches the player's sequence after RebuildCharacterActions.
                    actionEntry.action.ComboOrder = nextSlot++;
                    AddToCombo(actionEntry.action);
                }
            }

            WeaponRequiredComboAction.EnsureRequiredBasicInCombo(entity);

            return ComboSequence.Count > 0;
        }
    }
}





