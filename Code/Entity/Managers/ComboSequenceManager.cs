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
                    // If no combo actions are available, create a default combo action
                    var defaultComboAction = new Action(
                        name: "BASIC ATTACK",
                        type: ActionType.Attack,
                        targetType: TargetType.SingleTarget,
                        baseValue: 0,
                        range: 1,
                        cooldown: 0,
                        description: "A standard physical attack using STR + weapon damage",
                        comboOrder: 0,
                        damageMultiplier: 1.0,
                        length: 1.0,
                        causesBleed: false,
                        causesWeaken: false,
                        isComboAction: false
                    );
                    entity.AddAction(defaultComboAction, 1.0);
                    AddToCombo(defaultComboAction);
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
        /// </summary>
        private List<string> GetWeaponActionsForCombo(WeaponItem weapon)
        {
            // Return weapon-specific actions based on weapon type
            return weapon.WeaponType switch
            {
                WeaponType.Sword => new List<string> { "SWORD SLASH", "PARRY" },
                WeaponType.Dagger => new List<string> { "QUICK STAB", "EVADE" },
                WeaponType.Axe => new List<string> { "CLEAVE", "BRACE" },
                WeaponType.Mace => new List<string> { "CRUSHING BLOW", "SHIELD BREAK" },
                WeaponType.Staff => new List<string> { "STAFF STRIKE", "FOCUS" },
                WeaponType.Bow => new List<string> { "AIMED SHOT", "RAPID FIRE" },
                _ => new List<string> { "BASIC ATTACK" }
            };
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





