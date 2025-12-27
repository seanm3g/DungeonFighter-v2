using RPGGame;
using RPGGame.Utils;

namespace RPGGame.Actions.Execution
{
    /// <summary>
    /// Handles multi-hit attack processing
    /// Processes multiple damage applications and tracks statistics per hit
    /// </summary>
    internal static class MultiHitProcessor
    {
        /// <summary>
        /// Processes a multi-hit attack, applying damage for each hit
        /// </summary>
        public static int ProcessMultiHit(
            Actor source,
            Actor target,
            Action action,
            double damageMultiplier,
            int totalRoll,
            int modifiedBaseRoll,
            int rollBonus,
            int naturalRoll,
            BattleNarrative? battleNarrative)
        {
            int multiHitCount = action.Advanced.MultiHitCount;
            int totalDamage = 0;

            // Process each hit
            for (int hit = 0; hit < multiHitCount; hit++)
            {
                // Check if target is still alive
                if (target is Character targetChar && targetChar.CurrentHealth <= 0)
                    break;
                if (target is Enemy hitTargetEnemy && hitTargetEnemy.CurrentHealth <= 0)
                    break;

                // Calculate damage for this hit (action.DamageMultiplier already contains per-hit scaling)
                int hitDamage = CombatCalculator.CalculateDamage(source, target, action, damageMultiplier, 1.0, rollBonus, totalRoll);

                // Handle SelfAndTarget - apply damage to both self and enemy
                if (action.Target == TargetType.SelfAndTarget)
                {
                    // Apply damage to the enemy target
                    ActionUtilities.ApplyDamage(target, hitDamage);
                    
                    // Apply damage to self (source)
                    ActionUtilities.ApplyDamage(source, hitDamage);
                    
                    if (!ActionExecutor.DisableCombatDebugOutput)
                    {
                        DebugLogger.WriteCombatDebug("ActionExecutor", $"{source.Name} dealt {hitDamage} damage (hit {hit + 1}/{multiHitCount}) to both {target.Name} and themselves with {action.Name}");
                    }
                }
                else
                {
                    // Normal single target behavior
                    ActionUtilities.ApplyDamage(target, hitDamage);
                    
                    if (!ActionExecutor.DisableCombatDebugOutput)
                    {
                        DebugLogger.WriteCombatDebug("ActionExecutor", $"{source.Name} dealt {hitDamage} damage (hit {hit + 1}/{multiHitCount}) to {target.Name} with {action.Name}");
                    }
                }
                
                totalDamage += hitDamage;
            }

            // Track statistics for total damage
            if (source is Character sourceCharacter)
            {
                ActionStatisticsTracker.RecordAttackAction(sourceCharacter, totalRoll, naturalRoll, rollBonus, totalDamage, action, target as Enemy);
            }
            if (target is Character targetCharacter)
            {
                ActionStatisticsTracker.RecordDamageReceived(targetCharacter, totalDamage);
            }
            
            // Track self damage if SelfAndTarget
            if (action.Target == TargetType.SelfAndTarget && source is Character selfCharacter)
            {
                ActionStatisticsTracker.RecordDamageReceived(selfCharacter, totalDamage);
            }

            // Use threshold manager to determine critical hit (consistent with ActionExecutionFlow)
            bool isCriticalHit = totalRoll >= RPGGame.Actions.RollModification.RollModificationManager.GetThresholdManager().GetCriticalHitThreshold(source);
            // BASIC ATTACK removed - all actions are now combo actions
            ActionUtilities.CreateAndAddBattleEvent(source, target, action, totalDamage, totalRoll, rollBonus, true, true, 0, 0, isCriticalHit, naturalRoll, battleNarrative);

            return totalDamage;
        }
    }
}

