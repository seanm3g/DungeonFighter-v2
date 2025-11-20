using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Handles action selection logic for different Actor types
    /// Refactored to focus purely on selection using shared utilities
    /// </summary>
    public static class ActionSelector
    {
        // Store the last action selection roll for consistency
        private static readonly Dictionary<Actor, int> _lastActionSelectionRolls = new Dictionary<Actor, int>();

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
        /// Selects an action based on dice roll logic (6+ = BASIC ATTACK, 14+ = COMBO) - for heroes only
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
            _lastActionSelectionRolls[source] = baseRoll;
            
            // Calculate roll bonus for action selection (using null action for base bonus)
            int rollBonus = ActionUtilities.CalculateRollBonus(source, null);
            int totalRoll = baseRoll + rollBonus;
            
            // Determine action type based on total roll result (base roll + bonuses)
            Action? selectedAction = null;
            
            if (baseRoll == 20) // Natural 20 - always combo + critical hit
            {
                selectedAction = SelectComboAction(source);
            }
            else if (totalRoll >= 14) // Combo threshold (14-20) - with bonuses
            {
                selectedAction = SelectComboAction(source);
            }
            else if (totalRoll >= 6) // Basic attack threshold (6-13) - with bonuses
            {
                selectedAction = ActionFactory.GetBasicAttack(source);
            }
            else // totalRoll < 6 - still attempt basic attack (will likely miss)
            {
                selectedAction = ActionFactory.GetBasicAttack(source);
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
            _lastActionSelectionRolls[source] = baseRoll;

            // Calculate roll bonus for action selection (enemies generally have no base roll bonus)
            int rollBonus = ActionUtilities.CalculateRollBonus(source, null);
            int totalRoll = baseRoll + rollBonus;

            // 20 or 14-19: prefer combo actions, but only if they have both combo and basic actions
            if (baseRoll == 20 || totalRoll >= 14)
            {
                var comboActions = ActionUtilities.GetComboActions(source);
                var hasBasicAttack = source.ActionPool.Any(a => a.action.Name == "BASIC ATTACK");
                
                // Only use combo actions if the enemy has both combo actions AND basic attacks available
                // This prevents enemies from always using combo actions when they should do basic attacks
                if (comboActions.Count > 0 && hasBasicAttack)
                {
                    int idx = comboActions.Count > 1 ? Dice.Roll(1, comboActions.Count) - 1 : 0;
                    return comboActions[idx];
                }
                // If no basic attack available, fall back to weighted selection
                return source.SelectAction();
            }

            // 6-13: BASIC ATTACK
            if (totalRoll >= 6)
            {
                return ActionFactory.GetBasicAttack(source);
            }

            // <6: treat as basic attack attempt (will likely miss)
            return ActionFactory.GetBasicAttack(source);
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
                // This should never happen - combo actions should always be available
                // If we reach here, there's a bug in the combo initialization
                DebugLogger.Log("ActionSelector", $"ERROR: No combo actions available for {source.Name} on combo roll! This should never happen.");
                
                // Try to find any combo action from the action pool
                var anyComboAction = source.ActionPool
                    .Where(a => a.action.IsComboAction)
                    .Select(a => a.action)
                    .FirstOrDefault();
                
                if (anyComboAction != null)
                {
                    DebugLogger.Log("ActionSelector", $"Found combo action {anyComboAction.Name} for {source.Name}");
                    return anyComboAction;
                }
                else
                {
                    // Last resort: create a combo action on the fly
                    var emergencyAction = ActionFactory.CreateEmergencyComboAction();
                    source.AddAction(emergencyAction, 1.0);
                    DebugLogger.Log("ActionSelector", $"Created emergency combo action for {source.Name}");
                    return emergencyAction;
                }
            }
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

