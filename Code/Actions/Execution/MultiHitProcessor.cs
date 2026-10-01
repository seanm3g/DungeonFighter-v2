using RPGGame;
using RPGGame.Combat;
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
            int rollPenalty = 0,
            int? defenseFace = null,
            int? attackFace = null)
        {
            int multiHitCount = action.Advanced.MultiHitCount;
            if (source is Character character && character.Effects.ConsumedMultiHitMod != 0)
                multiHitCount = Math.Max(1, multiHitCount + (int)Math.Max(0, character.Effects.ConsumedMultiHitMod));
            multiHitCount = Math.Max(1, multiHitCount + ChainPositionBonusApplier.GetMultiHitDelta(source, action, ActionUtilities.GetComboActions(source), ActionUtilities.GetComboStep(source)));
            multiHitCount = PackCombat.ResolveOutgoingHitCount(source, multiHitCount);
            int totalDamage = 0;

            bool capTarget = target is Enemy && ((Enemy)target).IsPack && action.Target != TargetType.Self;
            bool capSelf = source is Enemy && ((Enemy)source).IsPack
                && (action.Target == TargetType.Self || action.Target == TargetType.SelfAndTarget);
            if (capTarget || capSelf)
                return ProcessPackCappedMultiHit(source, target, action, damageMultiplier, totalRoll, modifiedBaseRoll, rollBonus, naturalRoll, battleNarrative, multiHitCount, defenseFace, attackFace);

            // Always resolve every planned tick so combat-log totals match (attack − armor) × hits
            // (and the `(N hits)` label). TakeDamage already clamps HP at 0; remaining ticks overkill.
            for (int hit = 0; hit < multiHitCount; hit++)
            {
                // Roll-based damage: apply RollPenalty per hit (hit 0 uses the same total as the hit roll; later hits stack the debuff again)
                int perHitTotalRoll = GetMultihitDamageTotalRoll(totalRoll, source, hit);
                int hitDamage = action.DamageMultiplier > 0
                    ? CombatCalculator.CalculateDamage(source, target, action, damageMultiplier, 1.0, rollBonus, perHitTotalRoll, true, defenseFace, attackFace ?? modifiedBaseRoll, hit)
                    : 0;

                if (hitDamage <= 0)
                    continue;

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
                else if (action.Target == TargetType.Self)
                {
                    ActionUtilities.ApplyDamage(source, hitDamage);

                    if (!ActionExecutor.DisableCombatDebugOutput)
                    {
                        DebugLogger.WriteCombatDebug("ActionExecutor", $"{source.Name} dealt {hitDamage} damage (hit {hit + 1}/{multiHitCount}) to themselves with {action.Name}");
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

            // Use threshold manager to determine critical hit (consistent with ActionExecutionFlow)
            int critEval = CombatCalculator.GetCritThresholdEvaluationRoll(
                totalRoll, rollBonus, rollPenalty);
            bool isCriticalHit = critEval >= RPGGame.Actions.RollModification.RollModificationManager.GetThresholdManager().GetCriticalHitThreshold(source);
            bool isComboEvent = action.IsComboAction && totalRoll >= RPGGame.Actions.RollModification.RollModificationManager.GetThresholdManager().GetComboThreshold(source);

            // Track statistics for total damage
            if (source is Character sourceCharacter)
            {
                ActionStatisticsTracker.RecordAttackAction(sourceCharacter, totalRoll, naturalRoll, rollBonus, totalDamage, action, target as Enemy, isCriticalHit);
            }
            Actor primaryRecipient = ActionEffectTargetResolver.ResolvePrimaryRecipient(action, source, target);
            if (primaryRecipient is Character targetCharacter)
            {
                ActionStatisticsTracker.RecordDamageReceived(targetCharacter, totalDamage);
            }
            
            // Track self damage if SelfAndTarget
            if (action.Target == TargetType.SelfAndTarget && source is Character selfCharacter)
            {
                ActionStatisticsTracker.RecordDamageReceived(selfCharacter, totalDamage);
            }

            ActionUtilities.CreateAndAddBattleEvent(source, primaryRecipient, action, totalDamage, totalRoll, rollBonus, true, isComboEvent, 0, 0, isCriticalHit, naturalRoll, battleNarrative);

            return totalDamage;
        }

        /// <summary>
        /// One swing against a pack hits one body. Tick damage is summed, then applied once so leftover ticks cannot spill.
        /// </summary>
        private static int ProcessPackCappedMultiHit(
            Actor source,
            Actor target,
            Action action,
            double damageMultiplier,
            int totalRoll,
            int modifiedBaseRoll,
            int rollBonus,
            int naturalRoll,
            BattleNarrative? battleNarrative,
            int multiHitCount,
            int? defenseFace,
            int? attackFace)
        {
            int rawTarget = 0;
            int rawSelf = 0;
            for (int hit = 0; hit < multiHitCount; hit++)
            {
                int perHitTotalRoll = GetMultihitDamageTotalRoll(totalRoll, source, hit);
                int hitDamage = action.DamageMultiplier > 0
                    ? CombatCalculator.CalculateDamage(source, target, action, damageMultiplier, 1.0, rollBonus, perHitTotalRoll, true, defenseFace, attackFace ?? modifiedBaseRoll, hit)
                    : 0;
                if (hitDamage <= 0)
                    continue;
                if (action.Target == TargetType.Self)
                    rawSelf += hitDamage;
                else if (action.Target == TargetType.SelfAndTarget)
                {
                    rawTarget += hitDamage;
                    rawSelf += hitDamage;
                }
                else
                    rawTarget += hitDamage;
            }

            int appliedTarget = ApplyRawOnce(target, rawTarget);
            int appliedSelf = ApplyRawOnce(source, rawSelf);
            int totalDamage = action.Target == TargetType.Self ? appliedSelf : appliedTarget;

            int critEval = CombatCalculator.GetCritThresholdEvaluationRoll(totalRoll, rollBonus, source.RollPenalty);
            bool isCriticalHit = critEval >= RPGGame.Actions.RollModification.RollModificationManager.GetThresholdManager().GetCriticalHitThreshold(source);
            bool isComboEvent = action.IsComboAction && totalRoll >= RPGGame.Actions.RollModification.RollModificationManager.GetThresholdManager().GetComboThreshold(source);

            if (source is Character sourceCharacter)
                ActionStatisticsTracker.RecordAttackAction(sourceCharacter, totalRoll, naturalRoll, rollBonus, totalDamage, action, target as Enemy, isCriticalHit);
            Actor primaryRecipient = ActionEffectTargetResolver.ResolvePrimaryRecipient(action, source, target);
            if (primaryRecipient is Character targetCharacter && totalDamage > 0)
                ActionStatisticsTracker.RecordDamageReceived(targetCharacter, totalDamage);
            if (action.Target == TargetType.SelfAndTarget && source is Character selfCharacter && appliedSelf > 0)
                ActionStatisticsTracker.RecordDamageReceived(selfCharacter, appliedSelf);

            ActionUtilities.CreateAndAddBattleEvent(source, primaryRecipient, action, totalDamage, totalRoll, rollBonus, true, isComboEvent, 0, 0, isCriticalHit, naturalRoll, battleNarrative);
            return totalDamage;
        }

        private static int ApplyRawOnce(Actor actor, int raw)
        {
            if (raw <= 0 || actor is not Character character)
                return 0;
            int before = character.CurrentHealth;
            ActionUtilities.ApplyDamage(actor, raw);
            return Math.Max(0, before - character.CurrentHealth);
        }
    }
}

