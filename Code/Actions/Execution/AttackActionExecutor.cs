using RPGGame;
using RPGGame.Actions.RollModification;
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
            int naturalRoll,
            BattleNarrative? battleNarrative)
        {
            double damageMultiplier = ActionUtilities.CalculateDamageMultiplier(source, selectedAction);
            int totalRoll = baseRoll + rollBonus;

            // Persistent + temporary combo-threshold bonuses must affect combo outcome in the colored execution path too.
            // Consume temp here (one roll), matching ActionExecutionFlow's behavior.
            int consumedComboThresholdBonus = 0;
            if (source is Character comboBonusCharacter && comboBonusCharacter is not Enemy)
            {
                int temp = comboBonusCharacter.Effects.ConsumeTempComboBonus();
                consumedComboThresholdBonus = comboBonusCharacter.Effects.ComboBonus + temp;
            }
            
            // Check for multi-hit attacks (base + consumed MULTIHIT_MOD from ACTION/ABILITY keyword)
            int multiHitCount = selectedAction.Advanced.MultiHitCount;
            if (source is Character multiHitCharacter && multiHitCharacter.Effects.ConsumedMultiHitMod != 0)
                multiHitCount = Math.Max(1, multiHitCount + (int)Math.Max(0, multiHitCharacter.Effects.ConsumedMultiHitMod));
            multiHitCount = Math.Max(1, multiHitCount + ChainPositionBonusApplier.GetMultiHitDelta(source, selectedAction, ActionUtilities.GetComboActions(source), ActionUtilities.GetComboStep(source)));
            
            // If multi-hit, process multiple hits; otherwise single hit
            if (multiHitCount > 1)
            {
                int totalDamage = 0;

                // Always resolve every planned tick so totals match (attack − armor) × hits.
                for (int hit = 0; hit < multiHitCount; hit++)
                {
                    int perHitTotalRoll = MultiHitProcessor.GetMultihitDamageTotalRoll(totalRoll, source, hit);
                    int hitDamage = CombatCalculator.CalculateDamage(source, target, selectedAction, damageMultiplier, 1.0, rollBonus, perHitTotalRoll);
                    
                    // Apply damage
                    ActionUtilities.ApplyDamage(target, hitDamage);
                    totalDamage += hitDamage;
                    
                    if (!ActionExecutor.DisableCombatDebugOutput)
                    {
                        DebugLogger.WriteCombatDebug("ActionExecutor", $"{source.Name} dealt {hitDamage} damage (hit {hit + 1}/{multiHitCount}) to {target.Name} with {selectedAction.Name}");
                    }
                }
                
                // Use same crit rule as ActionExecutionFlow (crit-eval vs threshold; not raw total >= threshold).
                int critEval = CombatCalculator.GetCritThresholdEvaluationRoll(totalRoll, rollBonus, source.RollPenalty);
                bool isCriticalHit = critEval >= RollModificationManager.GetThresholdManager().GetCriticalHitThreshold(source);
                
                // Format single consolidated damage display for multi-hit attack
                // Show total damage with hit count indicator
                // Check if this is a critical miss (natural roll <= 1 is typically critical miss)
                bool isCriticalMiss = naturalRoll <= 1;
                var (allDamageText, allRollInfo) = CombatResults.FormatDamageDisplayColored(source, target, totalDamage, totalDamage, selectedAction, damageMultiplier, 1.0, rollBonus, baseRoll, multiHitCount, isCriticalMiss, isCriticalHit);
                
                // Track statistics for total damage
                if (source is Character character)
                {
                    ActionStatisticsTracker.RecordAttackAction(character, totalRoll, naturalRoll, rollBonus, totalDamage, selectedAction, target as Enemy, isCriticalHit);
                }
                
                if (target is Character targetCharacter)
                {
                    ActionStatisticsTracker.RecordDamageReceived(targetCharacter, totalDamage);
                }
                
                int effectiveComboThreshold = RollModificationManager.GetThresholdManager().GetComboThreshold(source) - consumedComboThresholdBonus;
                if (effectiveComboThreshold < 1) effectiveComboThreshold = 1;
                bool isComboEvent = selectedAction.IsComboAction && totalRoll >= effectiveComboThreshold;
                ActionUtilities.CreateAndAddBattleEvent(source, target, selectedAction, totalDamage, totalRoll, rollBonus, true, isComboEvent, 0, 0, isCriticalHit, naturalRoll, battleNarrative);
                
                // Reset combo when a non-combo (normal) attack completes successfully
                if (source is Character resetCharacter && !(resetCharacter is Enemy) && !selectedAction.IsComboAction
                    && (selectedAction.Type == ActionType.Attack || selectedAction.Type == ActionType.Spell))
                    resetCharacter.ResetCombo();
                
                // Handle combo advancement based on roll value; only advance when executed action was a combo action
                if (source is Character comboCharacter && !(comboCharacter is Enemy) && selectedAction.IsComboAction)
                {
                    int effectiveComboThresholdForAdvance = RollModificationManager.GetThresholdManager().GetComboThreshold(comboCharacter) - consumedComboThresholdBonus;
                    if (effectiveComboThresholdForAdvance < 1) effectiveComboThresholdForAdvance = 1;
                    
                    if (comboCharacter.ComboStep == 0)
                    {
                        if (totalRoll >= effectiveComboThresholdForAdvance)
                            comboCharacter.IncrementComboStep(selectedAction);
                    }
                    else if (comboCharacter.ComboStep == 1)
                    {
                        if (totalRoll >= effectiveComboThresholdForAdvance)
                            comboCharacter.IncrementComboStep(selectedAction);
                        else
                            comboCharacter.ComboStep = 0;
                    }
                    else if (comboCharacter.ComboStep >= 2)
                    {
                        if (totalRoll >= effectiveComboThresholdForAdvance)
                            comboCharacter.IncrementComboStep(selectedAction);
                        else
                            comboCharacter.ComboStep = 0;
                    }
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
                }
                
                int critEvalSingle = CombatCalculator.GetCritThresholdEvaluationRoll(totalRoll, rollBonus, source.RollPenalty);
                bool isCriticalHit = critEvalSingle >= RollModificationManager.GetThresholdManager().GetCriticalHitThreshold(source);
                
                // Track statistics
                if (source is Character character)
                {
                    ActionStatisticsTracker.RecordAttackAction(character, totalRoll, naturalRoll, rollBonus, damage, selectedAction, target as Enemy, isCriticalHit);
                }
                
                if (target is Character targetCharacter)
                {
                    ActionStatisticsTracker.RecordDamageReceived(targetCharacter, damage);
                }
                
                int effectiveComboThreshold = RollModificationManager.GetThresholdManager().GetComboThreshold(source) - consumedComboThresholdBonus;
                if (effectiveComboThreshold < 1) effectiveComboThreshold = 1;
                bool isCombo = selectedAction.IsComboAction && totalRoll >= effectiveComboThreshold;
                ActionUtilities.CreateAndAddBattleEvent(source, target, selectedAction, damage, totalRoll, rollBonus, true, isCombo, 0, 0, isCriticalHit, naturalRoll, battleNarrative);
                
                // Check if this is a critical miss (natural roll <= 1 is typically critical miss)
                bool isCriticalMiss = naturalRoll <= 1;
                var (damageText, rollInfo) = CombatResults.FormatDamageDisplayColored(source, target, damage, damage, selectedAction, damageMultiplier, 1.0, rollBonus, baseRoll, 1, isCriticalMiss, isCriticalHit);
                
                // Reset combo when a non-combo (normal) attack completes successfully
                if (source is Character resetCharacter && !(resetCharacter is Enemy) && !selectedAction.IsComboAction
                    && (selectedAction.Type == ActionType.Attack || selectedAction.Type == ActionType.Spell))
                    resetCharacter.ResetCombo();
                
                // Handle combo advancement; only advance when executed action was a combo action
                if (source is Character comboCharacter && !(comboCharacter is Enemy) && selectedAction.IsComboAction)
                {
                    int effectiveComboThresholdForAdvance = RollModificationManager.GetThresholdManager().GetComboThreshold(comboCharacter) - consumedComboThresholdBonus;
                    if (effectiveComboThresholdForAdvance < 1) effectiveComboThresholdForAdvance = 1;
                    
                    if (comboCharacter.ComboStep == 0)
                    {
                        if (totalRoll >= effectiveComboThresholdForAdvance)
                            comboCharacter.IncrementComboStep(selectedAction);
                    }
                    else if (comboCharacter.ComboStep == 1)
                    {
                        if (totalRoll >= effectiveComboThresholdForAdvance)
                            comboCharacter.IncrementComboStep(selectedAction);
                        else
                            comboCharacter.ComboStep = 0;
                    }
                    else if (comboCharacter.ComboStep >= 2)
                    {
                        if (totalRoll >= effectiveComboThresholdForAdvance)
                            comboCharacter.IncrementComboStep(selectedAction);
                        else
                            comboCharacter.ComboStep = 0;
                    }
                }
                
                return (damageText, rollInfo);
            }
        }
    }
}

