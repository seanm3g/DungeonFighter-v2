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

                // Apply damage
                ActionUtilities.ApplyDamage(target, hitDamage);
                totalDamage += hitDamage;

                if (!ActionExecutor.DisableCombatDebugOutput)
                {
                    DebugLogger.WriteCombatDebug("ActionExecutor", $"{source.Name} dealt {hitDamage} damage (hit {hit + 1}/{multiHitCount}) to {target.Name} with {action.Name}");
                }
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

            bool isCriticalHit = totalRoll >= 20;
            ActionUtilities.CreateAndAddBattleEvent(source, target, action, totalDamage, totalRoll, rollBonus, true, action.Name != "BASIC ATTACK", 0, 0, isCriticalHit, naturalRoll, battleNarrative);

            return totalDamage;
        }
    }
}

