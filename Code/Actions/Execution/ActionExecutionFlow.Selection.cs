using RPGGame;
using RPGGame.ActionInteractionLab;
using RPGGame.Actions.Conditional;
using RPGGame.Actions.RollModification;
using RPGGame.Combat;
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
            IDictionary<Actor, Action> lastUsedActions,
            IDictionary<Actor, bool> lastCriticalMissStatus,
            Action? forcedAction)
        {
            // Reset before combo vs normal selection so <see cref="ActionSelector"/> does not read stale
            // <see cref="Combat.ThresholdManager"/> state left by the previous attack's ApplyThresholdOverrides.
            var thresholdManager = RollModificationManager.GetThresholdManager();
            thresholdManager.ResetThresholds(source);
            if (target != null)
                thresholdManager.ResetThresholds(target);

            if (source is Character techMilestoneCharacter)
            {
                TechniqueMilestoneThresholdBonuses.Apply(thresholdManager, techMilestoneCharacter);
                NaiveteThresholdBonuses.Apply(thresholdManager, techMilestoneCharacter);
            }

            // Catalog + suffix + prefix (Swift/HIT, etc.) dice stats: literal threshold shifts, same sign as TECH milestones / FIFO HIT.
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
                CadenceScopedBuffApplicator.ApplyThresholds(gearCharacter, thresholdManager);
            }

            if (CombatTriggerContext.TryGetCritFaceMin(source, out int critFaceMin))
                thresholdManager.SetCriticalHitThreshold(source, critFaceMin);

            Action? selected = forcedAction;
            if (selected == null && StripMutationApplier.TryConsumeReplaceNext(source, out var stripReplace) && stripReplace != null)
                selected = stripReplace;
            result.SelectedAction = selected ?? ActionSelector.SelectActionByEntityType(source);
            if (result.SelectedAction == null) return;
            lastUsedActions[source] = result.SelectedAction;
            result.BaseRoll = ActionSelector.GetActionRoll(source);
            if (CombatTriggerContext.TryConsumePendingReplaceRollFace(source, out int replacedFace))
                result.BaseRoll = replacedFace;
            if (source is Character character && !(character is Enemy) && forcedAction == null)
                result.SelectedAction = ActionUtilities.HandleUniqueActionChance(character, result.SelectedAction);

            // Roll and threshold bonuses: TURN (consumed per roll), ACTION (peek slot + bank; redeem on hit+combo only — miss/non-combo keep pending)
            int actionBonusAccumulator = 0, actionBonusHit = 0, actionBonusCombo = 0, actionBonusCrit = 0, actionBonusCritMiss = 0;
            bool pendingAdvantage = false, pendingDisadvantage = false;
            if (source is Character actionBonusCharacter && !(actionBonusCharacter is Enemy))
            {
                // 1. ACTION cadence: legacy slot-based pending (peeked per roll) + bank peeked until hit+combo redeems (miss keeps pending)
                var comboActions = ActionUtilities.GetComboActions(actionBonusCharacter);
                int comboLength = comboActions.Count;
                var pendingActionRollBonuses = new List<ActionAttackBonusItem>();
                if (comboLength > 0)
                {
                    int currentSlot = ActionUtilities.GetComboSlotForPendingBonuses(
                        actionBonusCharacter, result.SelectedAction, comboActions);
                    var slotBonuses = actionBonusCharacter.Effects.GetPendingActionBonusesForSlot(currentSlot);
                    if (slotBonuses.Count > 0)
                    {
                        result.PendingActionCadenceLayerPeekedForRoll = true;
                        pendingActionRollBonuses.AddRange(slotBonuses);
                    }
                }
                // Additive bank sticks to PendingActionCadencePreviewSlot so miss→ComboStep=0
                // does not redeem on the wrong strip action (e.g. Rapid Strike after failing Slam).
                if (comboLength > 0)
                {
                    int currentSlotForBank = ActionUtilities.GetComboSlotForPendingBonuses(
                        actionBonusCharacter, result.SelectedAction, comboActions);
                    if (actionBonusCharacter.Effects.ShouldPeekActionCadenceBankForExecutedSlot(currentSlotForBank))
                    {
                        var fifoPeeked = actionBonusCharacter.Effects.PeekPendingActionBonusesNextHeroRoll();
                        if (fifoPeeked.Count > 0)
                        {
                            result.PendingActionCadenceLayerPeekedForRoll = true;
                            pendingActionRollBonuses.AddRange(fifoPeeked);
                        }
                    }
                }
                else
                {
                    var fifoPeeked = actionBonusCharacter.Effects.PeekPendingActionBonusesNextHeroRoll();
                    if (fifoPeeked.Count > 0)
                    {
                        result.PendingActionCadenceLayerPeekedForRoll = true;
                        pendingActionRollBonuses.AddRange(fifoPeeked);
                    }
                }
                if (pendingActionRollBonuses.Count > 0)
                {
                    RollModificationManager.ApplyDeferredThresholdPackageSetPhase(source, pendingActionRollBonuses);
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
                    RollModificationManager.CollectAdvantageFlags(pendingActionRollBonuses, ref pendingAdvantage, ref pendingDisadvantage);
                }
                // 2. Consume TURN bonuses (roll-based; consumed per roll, apply only on hit for stat bonuses)
                var turnBonuses = actionBonusCharacter.Effects.GetAndConsumeTurnBonuses();
                RollModificationManager.ApplyDeferredThresholdPackageSetPhase(source, turnBonuses);
                actionBonusCharacter.Effects.AccumulateConsumedModifierBonuses(turnBonuses);
                actionBonusCharacter.Effects.SetConsumedTurnBonusesThisRoll(turnBonuses);
                if (turnBonuses.Any(b => DynamicAttributeCategoryResolver.IsStatOrDynamicCategoryType((b.Type ?? "").ToUpper())))
                {
                    result.TurnStatSnapshot = TempStatSnapshot.Capture(actionBonusCharacter);
                    ApplyStatBonusesFromCadenceItems(actionBonusCharacter, turnBonuses, duration: 1);
                }
                foreach (var bonus in turnBonuses)
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
                RollModificationManager.CollectAdvantageFlags(turnBonuses, ref pendingAdvantage, ref pendingDisadvantage);
                // 3. Fight/Dungeon scoped bonuses (persistent for scope; peek without consumption)
                var scopedBonuses = new List<ActionAttackBonusItem>();
                scopedBonuses.AddRange(actionBonusCharacter.FightCadenceBuffs.CopyBonuses());
                scopedBonuses.AddRange(actionBonusCharacter.DungeonCadenceBuffs.CopyBonuses());
                if (scopedBonuses.Count > 0)
                {
                    RollModificationManager.ApplyDeferredThresholdPackageSetPhase(source, scopedBonuses);
                    actionBonusCharacter.Effects.AccumulateConsumedModifierBonuses(scopedBonuses);
                    foreach (var bonus in scopedBonuses)
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
                    RollModificationManager.CollectAdvantageFlags(scopedBonuses, ref pendingAdvantage, ref pendingDisadvantage);
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
                    RollModificationManager.CollectAdvantageFlags(layer, ref pendingAdvantage, ref pendingDisadvantage);
                }
            }
            result.ModifiedBaseRoll = RollModificationManager.ApplyMultiDiceRoll(
                result.BaseRoll, pendingAdvantage, pendingDisadvantage, result.SelectedAction, source, target,
                out var multiDiceDetail);
            result.MultiDiceRollDetail = multiDiceDetail;
            if (source is Character envHero && envHero is not Enemy)
            {
                result.ModifiedBaseRoll = EnvironmentRollModifier.ApplyStructureRollShift(
                    CombatEnvironmentContext.CurrentRoom, envHero, result.ModifiedBaseRoll);
            }
            RollModificationManager.ApplyThresholdOverrides(result.SelectedAction, source, target);
            ActionRollTagProcessor.ApplyRollTags(result.SelectedAction, source);
            EnvironmentRollModifier.ApplyUnstableThresholdShift(CombatEnvironmentContext.CurrentRoom, source);
            // FIFO / TURN ACCURACY: shift hit and combo thresholds only (not crit or crit miss).
            if (actionBonusAccumulator != 0)
            {
                thresholdManager.AdjustHitThreshold(source, actionBonusAccumulator);
                thresholdManager.AdjustComboThreshold(source, actionBonusAccumulator);
            }
            // Pending queue: SET_* deferred overrides first, then deltas (crit miss → crit → combo → hit).
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
            result.NaturalRollValue = result.ModifiedBaseRoll;
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
            // Natural-face crit override (Balatro-style high faces): ModifiedBaseRoll is the die after adv/replace.
            if (CombatTriggerContext.TryGetCritFaceMin(source, out int naturalCritMin)
                && result.ModifiedBaseRoll >= naturalCritMin)
                result.IsCritical = true;
            ActionEventPublisher.PublishActionExecuted(source, target, result.SelectedAction, result.AttackRoll, result.IsCombo, result.IsCritical, result.NaturalRollValue);
            result.Hit = CombatCalculator.CalculateHit(source, target, result.RollBonus, result.AttackRoll);
            if (!result.Hit && !result.IsCriticalMiss && CombatTriggerContext.TryConsumeMissSalvage(source))
            {
                result.Hit = true;
                result.MissSalvaged = true;
            }
            // Sheet accuracy + threshold adjustments (and deferred overrides when not TURN cadence) queue for the next application.
            RollModificationManager.EnqueueDeferredRollModThresholdAdjustmentsForNextRoll(result.SelectedAction, source, target);
        }
    }
}
