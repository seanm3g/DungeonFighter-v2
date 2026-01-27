using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using RPGGame.Utils;

namespace RPGGame
{
    /// <summary>
    /// Handles action selection logic for different Actor types
    /// Refactored to focus purely on selection using shared utilities
    /// </summary>
    public static class ActionSelector
    {
        // Store the last action selection roll for consistency - using thread-safe concurrent dictionary
        private static readonly ConcurrentDictionary<Actor, int> _lastActionSelectionRolls = new ConcurrentDictionary<Actor, int>();

        /// <summary>
        /// Selects an action based on Actor type - heroes use roll-based logic, enemies use random selection
        /// </summary>
        /// <param name="source">The Actor selecting the action</param>
        /// <returns>The selected action or null if no action available</returns>
        public static Action? SelectActionByEntityType(Actor source)
        {
            // Heroes/Characters use advanced roll-based system with combos
            if (source is Character character && !(character is Enemy))
            {
                return SelectActionBasedOnRoll(source);
            }
            // Enemies use simple random probability-based selection
            else
            {
                return SelectEnemyActionBasedOnRoll(source);
            }
        }

        /// <summary>
        /// Selects an action based on dice roll logic:
        /// - Natural 20 or base roll 14+ = COMBO action
        /// - Base roll 1-13 = Normal action (non-combo)
        /// For heroes only
        /// Note: Action type is determined by base roll only, not total roll with bonuses
        /// </summary>
        /// <param name="source">The Actor selecting the action</param>
        /// <returns>The selected action or null if no action available</returns>
        public static Action? SelectActionBasedOnRoll(Actor source)
        {
            if (source.ActionPool.Count == 0)
                return null;

            // Check if Actor is stunned
            if (source.IsStunned)
                return null;

            // Roll first to determine what type of action to use
            int baseRoll = Dice.Roll(1, 20);

            // Store the base roll for use in the main execution
            _lastActionSelectionRolls.AddOrUpdate(source, baseRoll, (_, _) => baseRoll);
            
            // Determine action type based on base roll only (not total roll with bonuses)
            // Natural 20 always triggers combo, otherwise use base roll for threshold
            Action? selectedAction = null;
            
            if (baseRoll == 20) // Natural 20 - always combo + critical hit
            {
                selectedAction = SelectComboAction(source);
            }
            else if (baseRoll >= 14) // Combo threshold (14+) - use base roll only
            {
                selectedAction = SelectComboAction(source);
            }
            else // Base roll < 14 (1-13) - use non-combo action (normal attack)
            {
                // Select a non-combo action for normal attacks
                selectedAction = SelectNormalAction(source);
            }
            
            return selectedAction;
        }

        /// <summary>
        /// Selects an enemy action based on roll thresholds
        /// </summary>
        /// <param name="source">The enemy Actor</param>
        /// <returns>The selected action or null if no action available</returns>
        public static Action? SelectEnemyActionBasedOnRoll(Actor source)
        {
            if (source.ActionPool.Count == 0)
                return null;

            // Enemies can be stunned too
            if (source.IsStunned)
                return null;

            int baseRoll = Dice.Roll(1, 20);

            // Store the base roll for use in hit calculation (same as heroes)
            _lastActionSelectionRolls.AddOrUpdate(source, baseRoll, (_, _) => baseRoll);

            // Natural 20 or base roll >= 14: use combo actions
            // Action type determined by base roll only (not total roll with bonuses)
            if (baseRoll == 20 || baseRoll >= 14)
            {
                var comboActions = ActionUtilities.GetComboActions(source);
                if (comboActions.Count > 0)
                {
                    int idx = comboActions.Count > 1 ? Dice.Roll(1, comboActions.Count) - 1 : 0;
                    return comboActions[idx];
                }
                // If no combo actions available, fall back to weighted selection
                return source.SelectAction();
            }

            // Base roll < 14 (1-13): use non-combo action (normal attack)
            return SelectNormalAction(source);
        }

        /// <summary>
        /// Selects a combo action for the given Actor
        /// Only selects actions that are actually in the combo sequence
        /// If no combo actions are in the sequence, falls back to a normal action
        /// </summary>
        /// <param name="source">The Actor to select combo action for</param>
        /// <returns>Selected combo action from sequence, or fallback to normal action if sequence is empty, or null if no actions available</returns>
        private static Action? SelectComboAction(Actor source)
        {
            var comboActions = ActionUtilities.GetComboActions(source);
            if (comboActions.Count > 0)
            {
                int actionIdx = ActionUtilities.GetComboStep(source) % comboActions.Count;
                return comboActions[actionIdx];
            }
            else
            {
                // If combo sequence is empty, fall back to normal action instead of searching ActionPool
                // This ensures that actions not in the combo sequence (like CHANNEL) won't be used
                // unless they're explicitly added to the combo sequence
                return SelectNormalAction(source);
            }
        }

        /// <summary>
        /// Selects a normal (non-combo) action for the given Actor
        /// Used when base roll is less than 14 (normal attack range, below combo threshold)
        /// Returns a non-combo action if available, otherwise falls back to any available action
        /// </summary>
        /// <param name="source">The Actor to select normal action for</param>
        /// <returns>Non-combo action for normal attacks, or first available action if no non-combo actions exist, or null if no actions available</returns>
        private static Action? SelectNormalAction(Actor source)
        {
            // First, try to find a non-combo action from the source's ActionPool
            foreach (var actionEntry in source.ActionPool)
            {
                if (!actionEntry.action.IsComboAction)
                {
                    return actionEntry.action;
                }
            }
            
            // If no non-combo action found, use first available action
            if (source.ActionPool.Count > 0)
            {
                return source.ActionPool[0].action;
            }
            
            // No actions available - this should be caught earlier, but return null for safety
            DebugLogger.LogFormat("ActionSelector", 
                "WARNING: {0} has no actions in ActionPool when trying to select normal action", source.Name);
            return null;
        }

        /// <summary>
        /// Gets the action roll for an Actor - uses stored roll for both heroes and enemies
        /// </summary>
        /// <param name="source">The Actor to get roll for</param>
        /// <returns>The stored roll or a new roll if not found</returns>
        public static int GetActionRoll(Actor source)
        {
            // Both heroes and enemies use the stored roll from action selection
            if (_lastActionSelectionRolls.TryGetValue(source, out int roll))
            {
                return roll;
            }
            else
            {
                // Fallback to a new roll if not found (shouldn't happen in normal flow)
                return Dice.Roll(1, 20);
            }
        }

        /// <summary>
        /// Clears stored action selection rolls (useful for testing or cleanup)
        /// </summary>
        public static void ClearStoredRolls()
        {
            _lastActionSelectionRolls.Clear();
        }
    }
}

