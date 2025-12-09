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
            
            // Check for multi-hit attacks
            int multiHitCount = selectedAction.Advanced.MultiHitCount;
            
            // If multi-hit, process multiple hits; otherwise single hit
            if (multiHitCount > 1)
            {
                int totalDamage = 0;
                var allDamageText = new List<ColoredText>();
                var allRollInfo = new List<ColoredText>();
                
                // Process each hit
                for (int hit = 0; hit < multiHitCount; hit++)
                {
                    // Check if target is still alive
                    if (target is Character targetChar && targetChar.CurrentHealth <= 0)
                        break;
                    if (target is Enemy hitTargetEnemy && hitTargetEnemy.CurrentHealth <= 0)
                        break;
                    
                    // Calculate damage for this hit (action.DamageMultiplier already contains per-hit scaling)
                    int hitDamage = CombatCalculator.CalculateDamage(source, target, selectedAction, damageMultiplier, 1.0, rollBonus, totalRoll);
                    
                    // Apply damage
                    ActionUtilities.ApplyDamage(target, hitDamage);
                    totalDamage += hitDamage;
                    
                    if (!ActionExecutor.DisableCombatDebugOutput)
                    {
                        DebugLogger.WriteCombatDebug("ActionExecutor", $"{source.Name} dealt {hitDamage} damage (hit {hit + 1}/{multiHitCount}) to {target.Name} with {selectedAction.Name}");
                    }
                    
                    // Format damage display for this hit
                    var (hitDamageText, hitRollInfo) = CombatResults.FormatDamageDisplayColored(source, target, hitDamage, hitDamage, selectedAction, damageMultiplier, 1.0, rollBonus, baseRoll);
                    if (hitDamageText != null) allDamageText.AddRange(hitDamageText);
                    if (hitRollInfo != null) allRollInfo.AddRange(hitRollInfo);
                }
                
                // Track statistics for total damage
                if (source is Character character)
                {
                    ActionStatisticsTracker.RecordAttackAction(character, totalRoll, baseRoll, rollBonus, totalDamage, selectedAction, target as Enemy);
                }
                
                if (target is Character targetCharacter)
                {
                    ActionStatisticsTracker.RecordDamageReceived(targetCharacter, totalDamage);
                }
                
                bool isCombo = selectedAction.Name != "BASIC ATTACK";
                bool isCriticalHit = totalRoll >= 20;
                ActionUtilities.CreateAndAddBattleEvent(source, target, selectedAction, totalDamage, totalRoll, rollBonus, true, isCombo, 0, 0, isCriticalHit, battleNarrative);
                
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
                
                return (allDamageText, allRollInfo);
            }
            else
            {
                // Single hit (original behavior)
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
}

