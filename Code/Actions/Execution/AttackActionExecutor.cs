using RPGGame.Combat;
using RPGGame.UI.ColorSystem;
using RPGGame.Utils;

namespace RPGGame.Actions.Execution
{
    /// <summary>
    /// Executes attack actions and returns ColoredText results
    /// Handles damage calculation, application, and formatting
    /// </summary>
    public static class AttackActionExecutor
    {
        /// <summary>
        /// Executes an attack action and returns ColoredText results
        /// </summary>
        public static (List<ColoredText> actionText, List<ColoredText> rollInfo) ExecuteAttackActionColored(
            Actor source, 
            Actor target, 
            Action selectedAction, 
            int baseRoll, 
            int rollBonus, 
            BattleNarrative? battleNarrative)
        {
            double damageMultiplier = ActionUtilities.CalculateDamageMultiplier(source, selectedAction);
            int totalRoll = baseRoll + rollBonus;
            int damage = CombatCalculator.CalculateDamage(source, target, selectedAction, damageMultiplier, 1.0, rollBonus, totalRoll);
            
            ActionUtilities.ApplyDamage(target, damage);
            
            if (!ActionExecutor.DisableCombatDebugOutput)
            {
                DebugLogger.WriteCombatDebug("ActionExecutor", $"{source.Name} dealt {damage} damage to {target.Name} with {selectedAction.Name}");
            }
            
            // Track statistics
            if (source is Character character)
            {
                ActionStatisticsTracker.RecordAttackAction(character, totalRoll, baseRoll, rollBonus, damage, selectedAction, target as Enemy);
            }
            
            if (target is Character targetCharacter)
            {
                ActionStatisticsTracker.RecordDamageReceived(targetCharacter, damage);
            }
            
            bool isCombo = selectedAction.Name != "BASIC ATTACK";
            bool isCriticalHit = totalRoll >= 20;
            ActionUtilities.CreateAndAddBattleEvent(source, target, selectedAction, damage, totalRoll, rollBonus, true, isCombo, 0, 0, isCriticalHit, battleNarrative);
            
            var (damageText, rollInfo) = CombatResults.FormatDamageDisplayColored(source, target, damage, damage, selectedAction, damageMultiplier, 1.0, rollBonus, baseRoll);
            
            // Handle enemy roll penalty
            if (selectedAction.Advanced.EnemyRollPenalty > 0 && target is Enemy targetEnemy)
            {
                targetEnemy.ApplyRollPenalty(selectedAction.Advanced.EnemyRollPenalty, 1);
            }
            
            // Handle combo advancement (with routing support)
            if (source is Character comboCharacter && !(comboCharacter is Enemy))
            {
                comboCharacter.IncrementComboStep(selectedAction);
            }
            
            return (damageText, rollInfo);
        }
    }
}

