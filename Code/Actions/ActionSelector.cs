using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

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
        /// - Roll 14+ = COMBO action (all actions in game are combo actions)
        /// - Roll 6-13 = BASIC ATTACK (normal attack, non-combo)
        /// - Roll < 6 = BASIC ATTACK (normal attack, non-combo)
        /// For heroes only
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
            
            // Calculate roll bonus for action selection (using null action for base bonus)
            int rollBonus = ActionUtilities.CalculateRollBonus(source, null);
            int totalRoll = baseRoll + rollBonus;
            
            // Determine action type based on base roll (not total roll with bonuses)
            // Bonuses affect hit chance and damage, but base roll determines action type
            Action? selectedAction = null;
            
            if (baseRoll == 20) // Natural 20 - always combo + critical hit
            {
                selectedAction = SelectComboAction(source);
            }
            else if (baseRoll >= 14) // Combo threshold (14-19) - base roll only, no bonuses
            {
                selectedAction = SelectComboAction(source);
            }
            else // Rolls 6-13 or <6 - use non-combo action (normal attack)
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

            // Calculate roll bonus for action selection (enemies generally have no base roll bonus)
            int rollBonus = ActionUtilities.CalculateRollBonus(source, null);
            int totalRoll = baseRoll + rollBonus;

            // 20 or 14-19: use combo actions (all actions in game are combo actions)
            // Action type determined by base roll, not total roll with bonuses
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

            // 6-13 or <6: use non-combo action (normal attack)
            return SelectNormalAction(source);
        }

        /// <summary>
        /// Selects a combo action for the given Actor
        /// </summary>
        /// <param name="source">The Actor to select combo action for</param>
        /// <returns>Selected combo action or fallback to basic attack</returns>
        private static Action SelectComboAction(Actor source)
        {
            var comboActions = ActionUtilities.GetComboActions(source);
            if (comboActions.Count > 0)
            {
                int actionIdx = ActionUtilities.GetComboStep(source) % comboActions.Count;
                return comboActions[actionIdx];
            }
            else
            {
                // Try to find any combo action from the action pool
                // Optimized: Single pass instead of LINQ chain
                foreach (var actionEntry in source.ActionPool)
                {
                    if (actionEntry.action.IsComboAction)
                    {
                        return actionEntry.action;
                    }
                }
                
                // Last resort: use first available action (BASIC ATTACK removed)
                if (source.ActionPool.Count > 0)
                {
                    return source.ActionPool[0].action;
                }
                // This should never happen, but return null if no actions available
                return null!; // Explicitly return null - this is an error state
            }
        }

        /// <summary>
        /// Selects a normal (non-combo) action for the given Actor
        /// Used when roll is less than 14 (normal attack range)
        /// Returns a basic attack action that is NOT a combo action
        /// </summary>
        /// <param name="source">The Actor to select normal action for</param>
        /// <returns>Basic attack action (non-combo) for normal attacks</returns>
        private static Action SelectNormalAction(Actor source)
        {
            // For rolls 6-13, always use a basic attack (non-combo action)
            // All other actions in the game are combo actions and only trigger at 14+
            return ActionFactory.CreateNormalAttack();
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

