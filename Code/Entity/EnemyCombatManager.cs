using System;
using System.Collections.Generic;
using RPGGame.UI.ColorSystem;

namespace RPGGame
{
    /// <summary>
    /// Manages enemy combat logic including action attempts and multi-attacks
    /// Extracts combat logic from the main Enemy class
    /// </summary>
    public class EnemyCombatManager
    {
        private readonly Enemy _enemy;

        public EnemyCombatManager(Enemy enemy)
        {
            _enemy = enemy;
        }

        /// <summary>
        /// Attempts multiple actions based on attack speed
        /// </summary>
        public (string result, bool success) AttemptMultiAction(Character target, Environment? environment = null)
        {
            int attacksPerTurn = _enemy.GetAttacksPerTurn();
            var results = new List<string>();
            bool anySuccess = false;
            
            for (int i = 0; i < attacksPerTurn; i++)
            {
                if (!target.IsAlive) break; // Stop if target is dead
                
                var (result, success) = AttemptAction(target, environment);
                if (!string.IsNullOrEmpty(result))
                {
                    results.Add(result);
                }
                if (success) anySuccess = true;
            }
            return (string.Join("\n", results), anySuccess);
        }

        /// <summary>
        /// Attempts a single action against a target
        /// </summary>
        public (string result, bool success) AttemptAction(Character target, Environment? environment = null)
        {
            var availableActions = new List<Action>();
            foreach (var entry in _enemy.ActionPool)
            {
                availableActions.Add(entry.action);
            }
            if (availableActions.Count == 0)
                return ($"{_enemy.Name} has no available actions!", false);
            
            // Select action based on weights
            var action = _enemy.SelectAction();
            if (action == null)
                return ($"{_enemy.Name} has no available actions!", false);
                
            // Use the same roll system as ActionExecutor for consistency
            int baseRoll = Dice.Roll(20);
            int rollBonus = ActionUtilities.CalculateRollBonus(_enemy, action);
            int totalRoll = baseRoll + rollBonus;
            int difficulty = 8 + (_enemy.Level / 2);  // Higher level enemies have better accuracy
            
            // Simplified combat logic - narrative mode handling moved to CombatManager
            if (totalRoll >= difficulty)
            {
                var settings = GameSettings.Instance;
                int finalEffect = CombatCalculator.CalculateDamage(_enemy, target, action, 1.0, settings.EnemyDamageMultiplier, rollBonus, baseRoll, false);
                
                if (action.Type == ActionType.Attack)
                {
                    target.TakeDamage(finalEffect);
                    // Use the same parameters as the actual damage calculation to avoid duplicate weakened messages
                    int actualDamage = CombatCalculator.CalculateDamage(_enemy, target, action, 1.0, settings.EnemyDamageMultiplier, rollBonus, baseRoll, false);
                    // Use new ColoredText system, then convert to string for backward compatibility
                    var (damageText, rollInfo) = CombatResults.FormatDamageDisplayColored(_enemy, target, finalEffect, actualDamage, action, 1.0, settings.EnemyDamageMultiplier, rollBonus, baseRoll);
                    string damageDisplay = ColoredTextRenderer.RenderAsPlainText(damageText) + "\n" + ColoredTextRenderer.RenderAsPlainText(rollInfo);
                    return ($"[{_enemy.Name}] uses [{action.Name}] on [{target.Name}]: deals {damageDisplay}. (Rolled {totalRoll}, need {difficulty})", true);
                }
                else if (action.Type == ActionType.Debuff)
                {
                    return ($"[{_enemy.Name}] uses [{action.Name}] on [{target.Name}]: applies debuff. (Rolled {totalRoll}, need {difficulty})", true);
                }
                return ($"[{_enemy.Name}] uses [{action.Name}] on [{target.Name}]. (Rolled {totalRoll}, need {difficulty})", true);
            }
            else
            {
                return ($"[{_enemy.Name}] attempts [{action.Name}] but fails. (Rolled {totalRoll}, need {difficulty}) No action performed.", false);
            }
        }
    }
}
