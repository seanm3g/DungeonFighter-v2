using RPGGame;
using RPGGame.ActionInteractionLab;
using RPGGame.Actions.RollModification;
using RPGGame.Combat.Events;
using RPGGame.UI.Avalonia.Feedback;
using RPGGame.Utils;
using RPGGame.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Actions.Execution
{
    internal static partial class ActionExecutionFlow
    {
        private static void SelectActionAndResolveRoll(
            Actor source,
            Actor? target,
            ActionExecutionResult result,
            Dictionary<Actor, Action> lastUsedActions,
            Dictionary<Actor, bool> lastCriticalMissStatus,
            Action? forcedAction)
        {
            // Reset before combo vs normal selection so <see cref="ActionSelector"/> does not read stale
            // <see cref="Combat.ThresholdManager"/> state left by the previous attack's ApplyThresholdOverrides.
            var thresholdManager = RollModificationManager.GetThresholdManager();
            thresholdManager.ResetThresholds(source);
            if (target != null)
                thresholdManager.ResetThresholds(target);

            if (source is Character intMilestoneCharacter)
            {
                IntelligenceMilestoneThresholdBonuses.Apply(thresholdManager, intMilestoneCharacter);
            }

            // Catalog + suffix + prefix (Swift/HIT, etc.) dice stats: literal threshold shifts, same sign as INT milestones / FIFO HIT.
            if (source is Character gearCharacter && gearCharacter is not Enemy)
            {
                int eqHit = gearCharacter.Equipment.GetEquipmentStatBonus("HIT", gearCharacter);
                int eqCombo = gearCharacter.Equipment.GetEquipmentStatBonus("COMBO", gearCharacter);
                int eqCrit = gearCharacter.Equipment.GetEquipmentStatBonus("CRIT", gearCharacter);
                if (eqHit != 0)
                    thresholdManager.AdjustHitThreshold(gearCharacter, eqHit);
                if (eqCombo != 0)
                    thresholdManager.AdjustComboThreshold(gearCharacter, eqCombo);
                if (eqCrit != 0)
                    thresholdManager.AdjustCriticalHitThreshold(gearCharacter, eqCrit);
                DungeonSearchBuffThresholdApplicator.Apply(gearCharacter, thresholdManager);
            }

            result.SelectedAction = forcedAction ?? ActionSelector.SelectActionByEntityType(source);
            if (result.SelectedAction == null) return;
            lastUsedActions[source] = result.SelectedAction;
            result.BaseRoll = ActionSelector.GetActionRoll(source);
            if (source is Character character && !(character is Enemy) && forcedAction == null)
                result.SelectedAction = ActionUtilities.HandleUniqueActionChance(character, result.SelectedAction);

            // Roll and threshold bonuses: ATTACK (consumed per roll), ACTION (FIFO per hero/enemy attack roll + legacy per-slot), ABILITY (consumed on hit)
            int actionBonusAccumulator = 0, actionBonusHit = 0, actionBonusCombo = 0, actionBonusCrit = 0, actionBonusCritMiss = 0;
            if (source is Character actionBonusCharacter && !(actionBonusCharacter is Enemy))
            {
                // 1. ACTION cadence: legacy slot-based pending + one FIFO layer per hero attack roll (enemy turns do not consume hero layers)
                var comboActions = ActionUtilities.GetComboActions(actionBonusCharacter);
                int comboLength = comboActions.Count;
                var pendingActionRollBonuses = new List<ActionAttackBonusItem>();
                if (comboLength > 0)
                {
                    int currentSlot = ActionUtilities.GetComboSlotForPendingBonuses(
                        actionBonusCharacter, result.SelectedAction, comboActions);
                    pendingActionRollBonuses.AddRange(actionBonusCharacter.Effects.ConsumePendingActionBonusesForSlot(currentSlot));
                }
                pendingActionRollBonuses.AddRange(actionBonusCharacter.Effects.ConsumePendingActionBonusesNextHeroRoll());
                if (pendingActionRollBonuses.Count > 0)
                {
                    RollModificationManager.ApplyDeferredThresholdPackageSetPhase(source, pendingActionRollBonuses);
                    actionBonusCharacter.Effects.AccumulateConsumedModifierBonuses(pendingActionRollBonuses);
                    foreach (var bonus in pendingActionRollBonuses)
                    {
                        switch ((bonus.Type ?? "").ToUpper())
                        {
                            case "ACCURACY": actionBonusAccumulator += (int)bonus.Value; break;
                            case "HIT": actionBonusHit += (int)bonus.Value; break;
                            case "COMBO": actionBonusCombo += (int)bonus.Value; break;
                            case "CRIT": actionBonusCrit += (int)bonus.Value; break;
                            case "CRIT_MISS": actionBonusCritMiss += (int)bonus.Value; break;
                        }
                    }
                }
                // 2. Consume ATTACK bonuses (roll-based; consumed per roll, apply only on hit for stat bonuses)
                var actionBonuses = actionBonusCharacter.Effects.GetAndConsumeAttackBonuses();
                RollModificationManager.ApplyDeferredThresholdPackageSetPhase(source, actionBonuses);
                actionBonusCharacter.Effects.AccumulateConsumedModifierBonuses(actionBonuses);
                actionBonusCharacter.Effects.SetConsumedAttackBonusesThisRoll(actionBonuses);
                foreach (var bonus in actionBonuses)
                {
                    switch (bonus.Type.ToUpper())
                    {
                        case "ACCURACY": actionBonusAccumulator += (int)bonus.Value; break;
                        case "HIT": actionBonusHit += (int)bonus.Value; break;
                        case "COMBO": actionBonusCombo += (int)bonus.Value; break;
                        case "CRIT": actionBonusCrit += (int)bonus.Value; break;
                        case "CRIT_MISS": actionBonusCritMiss += (int)bonus.Value; break;
                    }
                }
                // Apply ability-queued roll/threshold bonuses to this roll (consumed on hit in ApplyHitOutcome).
                var abilityBonusesPeek = actionBonusCharacter.Effects.PeekAbilityBonuses();
                RollModificationManager.ApplyDeferredThresholdPackageSetPhase(source, abilityBonusesPeek);
                foreach (var bonus in abilityBonusesPeek)
                {
                    switch (bonus.Type.ToUpper())
                    {
                        case "ACCURACY": actionBonusAccumulator += (int)bonus.Value; break;
                        case "HIT": actionBonusHit += (int)bonus.Value; break;
                        case "COMBO": actionBonusCombo += (int)bonus.Value; break;
                        case "CRIT": actionBonusCrit += (int)bonus.Value; break;
                        case "CRIT_MISS": actionBonusCritMiss += (int)bonus.Value; break;
                    }
                }
            }
            else if (source is Enemy enemyAttacker)
            {
                // Fumble-routed ACTION cadence: consume one layer per enemy attack roll only (not on hero turns)
                var layer = enemyAttacker.Effects.ConsumePendingActionBonusesNextHeroRoll();
                if (layer.Count > 0)
                {
                    RollModificationManager.ApplyDeferredThresholdPackageSetPhase(source, layer);
                    enemyAttacker.Effects.AccumulateConsumedModifierBonuses(layer);
                    foreach (var bonus in layer)
                    {
                        switch ((bonus.Type ?? "").ToUpper())
                        {
                            case "ACCURACY": actionBonusAccumulator += (int)bonus.Value; break;
                            case "HIT": actionBonusHit += (int)bonus.Value; break;
                            case "COMBO": actionBonusCombo += (int)bonus.Value; break;
                            case "CRIT": actionBonusCrit += (int)bonus.Value; break;
                            case "CRIT_MISS": actionBonusCritMiss += (int)bonus.Value; break;
                        }
                    }
                }
            }
            result.ModifiedBaseRoll = RollModificationManager.ApplyActionRollModifications(
                result.BaseRoll, result.SelectedAction, source, target);
            RollModificationManager.ApplyThresholdOverrides(result.SelectedAction, source, target);
            // FIFO / ATTACK / ABILITY ACCURACY: shift hit and combo thresholds only (not crit or crit miss).
            if (actionBonusAccumulator != 0)
            {
                thresholdManager.AdjustHitThreshold(source, actionBonusAccumulator);
                thresholdManager.AdjustComboThreshold(source, actionBonusAccumulator);
            }
            // Pending queue + ABILITY peek: SET_* deferred overrides first, then deltas (crit miss â†’ crit â†’ combo â†’ hit).
            if (actionBonusCritMiss != 0 && source is Character cmBonusCharacter)
                thresholdManager.AdjustCriticalMissThreshold(cmBonusCharacter, actionBonusCritMiss);
            if (actionBonusCrit != 0 && source is Character critBonusCharacter)
                thresholdManager.AdjustCriticalHitThreshold(critBonusCharacter, actionBonusCrit);
            if (actionBonusCombo != 0 && source is Character comboBonusCharacter)
                thresholdManager.AdjustComboThreshold(comboBonusCharacter, actionBonusCombo);
            if (actionBonusHit != 0 && source is Character hitBonusCharacter)
                thresholdManager.AdjustHitThreshold(hitBonusCharacter, actionBonusHit);

            // Persistent + temporary combo-threshold bonuses (gear-derived ComboBonus and short-lived TempComboBonus).
            // These lower the required combo threshold, and must affect both action selection previews and combat resolution.
            if (source is Character comboThresholdBonusCharacter && comboThresholdBonusCharacter is not Enemy)
            {
                int temp = comboThresholdBonusCharacter.Effects.ConsumeTempComboBonus();
                int bonus = comboThresholdBonusCharacter.Effects.ComboBonus + temp;
                if (bonus != 0)
                    thresholdManager.AdjustComboThreshold(comboThresholdBonusCharacter, bonus);
            }

            result.RollBonus = ActionUtilities.CalculateRollBonus(source, result.SelectedAction);
            result.AttackRoll = result.ModifiedBaseRoll + result.RollBonus;
            int hitThreshold = thresholdManager.GetHitThreshold(source);
            int criticalMissThreshold = thresholdManager.GetCriticalMissThreshold(source);
            // Crit / crit-miss: exclude full roll bonus (stats + temp + chain/sheet terms in bonus) from attack total.
            int critThresholdRoll = CombatCalculator.GetCritThresholdEvaluationRoll(
                result.AttackRoll, result.RollBonus, source.RollPenalty);
            // Critical miss only when crit-eval roll is both <= crit-miss threshold and in miss band (<= hit threshold).
            result.IsCriticalMiss = critThresholdRoll <= criticalMissThreshold && critThresholdRoll <= hitThreshold;
            if (result.IsCriticalMiss)
            {
                source.HasCriticalMissPenalty = true;
                source.CriticalMissPenaltyTurns = 1;
            }
            lastCriticalMissStatus[source] = result.IsCriticalMiss;
            // Combo flag: combo-slot action and attack total meets combo threshold (avoids "combo" on unnamed normal hits that hit 14+ total)
            result.IsCombo = result.SelectedAction.IsComboAction && result.AttackRoll >= thresholdManager.GetComboThreshold(source);
            result.IsCritical = critThresholdRoll >= thresholdManager.GetCriticalHitThreshold(source);
            ActionEventPublisher.PublishActionExecuted(source, target, result.SelectedAction, result.AttackRoll, result.IsCombo, result.IsCritical);
            result.Hit = CombatCalculator.CalculateHit(source, target, result.RollBonus, result.AttackRoll);
            // Sheet accuracy + threshold adjustments (and deferred overrides when not ATTACK cadence) queue for the next application.
            RollModificationManager.EnqueueDeferredRollModThresholdAdjustmentsForNextRoll(result.SelectedAction, source, target);
        }
    }
}
