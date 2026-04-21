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
        /// Combined attack total (modified d20 + roll bonus) for roll-based damage on multihit tick <paramref name="hitIndex"/> (0-based).
        /// Each hit after the first applies <see cref="Actor.RollPenalty"/> again so a debuff like "Accuracy -1" affects every strike, not only the first damage tick.
        /// </summary>
        internal static int GetMultihitDamageTotalRoll(int combinedTotalRoll, Actor attacker, int hitIndex)
        {
            if (hitIndex <= 0 || attacker == null) return combinedTotalRoll;
            int p = attacker.RollPenalty;
            if (p == 0) return combinedTotalRoll;
            return System.Math.Max(1, combinedTotalRoll - p * hitIndex);
        }

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
            BattleNarrative? battleNarrative,
            int rollPenalty = 0)
        {
            int multiHitCount = action.Advanced.MultiHitCount;
            if (source is Character character && character.Effects.ConsumedMultiHitMod != 0)
                multiHitCount = Math.Max(1, multiHitCount + (int)Math.Max(0, character.Effects.ConsumedMultiHitMod));
            multiHitCount = Math.Max(1, multiHitCount + ChainPositionBonusApplier.GetMultiHitDelta(source, action, ActionUtilities.GetComboActions(source), ActionUtilities.GetComboStep(source)));
            int totalDamage = 0;

            // Process each hit
            for (int hit = 0; hit < multiHitCount; hit++)
            {
                // Check if target is still alive
                if (target is Character targetChar && targetChar.CurrentHealth <= 0)
                    break;
                if (target is Enemy hitTargetEnemy && hitTargetEnemy.CurrentHealth <= 0)
                    break;

                // Roll-based damage: apply RollPenalty per hit (hit 0 uses the same total as the hit roll; later hits stack the debuff again)
                int perHitTotalRoll = GetMultihitDamageTotalRoll(totalRoll, source, hit);
                int hitDamage = CombatCalculator.CalculateDamage(source, target, action, damageMultiplier, 1.0, rollBonus, perHitTotalRoll);

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
            int critEval = CombatCalculator.GetCritThresholdEvaluationRoll(
                totalRoll, rollBonus, rollPenalty);
            bool isCriticalHit = critEval >= RPGGame.Actions.RollModification.RollModificationManager.GetThresholdManager().GetCriticalHitThreshold(source);
            bool isComboEvent = action.IsComboAction && totalRoll >= RPGGame.Actions.RollModification.RollModificationManager.GetThresholdManager().GetComboThreshold(source);
            ActionUtilities.CreateAndAddBattleEvent(source, target, action, totalDamage, totalRoll, rollBonus, true, isComboEvent, 0, 0, isCriticalHit, naturalRoll, battleNarrative);

            return totalDamage;
        }
    }
}

