using RPGGame;
using RPGGame.ActionInteractionLab;
using RPGGame.Actions;
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
        private static void ApplyHitOutcome(Actor source, Actor target, ActionExecutionResult result, BattleNarrative? battleNarrative)
        {
            var selected = result.SelectedAction;
            if (selected == null) return;

            if (source is Character turnBonusCharacter && !(turnBonusCharacter is Enemy))
            {
                // TURN stat bonuses were tentatively applied before the roll (Selection); kept on hit, reverted on miss.
                turnBonusCharacter.Effects.GetAndClearConsumedTurnBonusesThisRoll();
                ResolvePendingActionCadenceBonuses(turnBonusCharacter, selected, result);
            }
            var hitEvent = ActionEventPublisher.PublishActionHit(
                source, target, selected, result.AttackRoll, result.IsCombo, result.IsCritical);

            if (selected.Type == ActionType.Attack || selected.Type == ActionType.Spell)
            {
                Actor primaryRecipient = ActionEffectTargetResolver.ResolvePrimaryRecipient(selected, source, target);
                double targetLowHealthBefore = ActionEventPublisher.GetActorHealthPercentage(primaryRecipient);
                double sourceLowHealthBefore = selected.Target == TargetType.SelfAndTarget
                    ? ActionEventPublisher.GetActorHealthPercentage(source)
                    : double.NaN;

                double damageMultiplier = ActionUtilities.CalculateDamageMultiplier(source, selected);
                int totalRoll = result.ModifiedBaseRoll + result.RollBonus;
                int multiHitCount = selected.Advanced.MultiHitCount;
                if (source is Character multiHitCharacter && multiHitCharacter.Effects.ConsumedMultiHitMod != 0)
                    multiHitCount = Math.Max(1, multiHitCount + (int)Math.Max(0, multiHitCharacter.Effects.ConsumedMultiHitMod));
                multiHitCount = Math.Max(1, multiHitCount + ChainPositionBonusApplier.GetMultiHitDelta(source, selected, ActionUtilities.GetComboActions(source), ActionUtilities.GetComboStep(source)));
                // Capture before ACTION cadence deposits next-action Multihit (must not inflate this swing's log / deferred layers).
                result.ResolvedMultiHitCount = multiHitCount;
                if (multiHitCount > 1)
                {
                    result.Damage = MultiHitProcessor.ProcessMultiHit(
                        source, target, selected, damageMultiplier, totalRoll,
                        result.ModifiedBaseRoll, result.RollBonus, result.BaseRoll, battleNarrative,
                        source.RollPenalty);
                    ActionEffectTargetResolver.ApplyLifestealHealing(source, selected, result.Damage);
                }
                else
                {
                    result.Damage = selected.DamageMultiplier > 0
                        ? CombatCalculator.CalculateDamage(source, target, selected, damageMultiplier, 1.0, result.RollBonus, totalRoll)
                        : 0;
                    if (result.Damage > 0)
                    {
                        if (selected.Target == TargetType.SelfAndTarget)
                        {
                            ActionUtilities.ApplyDamage(target, result.Damage);
                            ActionUtilities.ApplyDamage(source, result.Damage);
                        }
                        else if (selected.Target == TargetType.Self)
                            ActionUtilities.ApplyDamage(source, result.Damage);
                        else
                            ActionUtilities.ApplyDamage(target, result.Damage);
                    }
                    if (source is Character sourceCharacter)
                        ActionStatisticsTracker.RecordAttackAction(sourceCharacter, totalRoll, result.BaseRoll, result.RollBonus, result.Damage, selected, target as Enemy, result.IsCritical);
                    if (primaryRecipient is Character targetCharacter && result.Damage > 0)
                        ActionStatisticsTracker.RecordDamageReceived(targetCharacter, result.Damage);
                    if (selected.Target == TargetType.SelfAndTarget && source is Character selfCharacter && result.Damage > 0)
                        ActionStatisticsTracker.RecordDamageReceived(selfCharacter, result.Damage);
                    ActionUtilities.CreateAndAddBattleEvent(source, primaryRecipient, selected, result.Damage, totalRoll, result.RollBonus, true, result.IsCombo, 0, 0, result.IsCritical, result.BaseRoll, battleNarrative);
                    ActionEffectTargetResolver.ApplyLifestealHealing(source, selected, result.Damage);
                }

                ActionEventPublisher.PublishLowHealthThresholdIfCrossed(primaryRecipient, targetLowHealthBefore);
                if (selected.Target == TargetType.SelfAndTarget && !ReferenceEquals(source, target))
                    ActionEventPublisher.PublishLowHealthThresholdIfCrossed(source, sourceLowHealthBefore);
            }
            else if (selected.Type == ActionType.Heal)
            {
                Actor healRecipient = ActionEffectTargetResolver.ResolvePrimaryRecipient(selected, source, target);
                result.HealAmount = ActionUtilities.CalculateHealAmount(source, selected);
                ActionUtilities.ApplyHealing(healRecipient, result.HealAmount);
                if (healRecipient is Character healCharacter)
                {
                    ActionStatisticsTracker.RecordHealingReceived(healCharacter, result.HealAmount);
                    if (selected.Advanced.MaxHealthIncrease > 0)
                        ActionUtilities.ApplyMaxHealthIncrease(healCharacter, selected.Advanced.MaxHealthIncrease);
                }
                ActionUtilities.CreateAndAddBattleEvent(source, healRecipient, selected, 0, result.BaseRoll + result.RollBonus, result.RollBonus, true, result.IsCombo, 0, result.HealAmount, false, result.BaseRoll, battleNarrative);
            }
            else if (selected.Type == ActionType.Buff || selected.Type == ActionType.Debuff)
            {
                Actor effectRecipient = ActionEffectTargetResolver.ResolvePrimaryRecipient(selected, source, target);
                if (selected.Type == ActionType.Buff && effectRecipient is Character buffCharacter && selected.Advanced.MaxHealthIncrease > 0)
                    ActionUtilities.ApplyMaxHealthIncrease(buffCharacter, selected.Advanced.MaxHealthIncrease);
                int totalRoll = result.ModifiedBaseRoll + result.RollBonus;
                ActionUtilities.CreateAndAddBattleEvent(source, effectRecipient, selected, 0, totalRoll, result.RollBonus, true, result.IsCombo, 0, 0, result.IsCritical, result.BaseRoll, battleNarrative);
            }
            else
            {
                Actor displayRecipient = ActionEffectTargetResolver.ResolvePrimaryRecipient(selected, source, target);
                ActionUtilities.CreateAndAddBattleEvent(source, displayRecipient, selected, 0, result.ModifiedBaseRoll + result.RollBonus, result.RollBonus, true, result.IsCombo, 0, 0, false, result.BaseRoll, battleNarrative);
            }

            if (selected.ActionAttackBonuses != null && source is Character bonusSourceCharacter && !(bonusSourceCharacter is Enemy))
            {
                bonusSourceCharacter.Effects.AddActionAttackBonuses(selected.ActionAttackBonuses, selected, bonusSourceCharacter);
                // ACTION cadence: FIFO layers â€” one layer consumed per hero attack roll (enemy turns do not count).
                // Spreadsheet Count / duration = number of consecutive hero actions that receive one application each.
                bool grantOnComboSuccess = result.IsCombo && selected.IsComboAction;
                if (grantOnComboSuccess)
                {
                    var comboActions = ActionUtilities.GetComboActions(bonusSourceCharacter);
                    if (comboActions.Count > 0)
                    {
                        foreach (var group in selected.ActionAttackBonuses.BonusGroups)
                        {
                            var ct = CadenceKeywords.NormalizeCadenceType(
                                string.IsNullOrEmpty(group.CadenceType) ? group.Keyword : group.CadenceType);
                            if (!CadenceKeywords.IsAction(ct) || group.Bonuses == null)
                                continue;
                            int layers = ActionCadenceDurationResolver.ResolveGrantedLayers(selected, group, comboActions, selected);
                            if (layers <= 0) continue;
                            var payload = CloneActionAttackBonusItems(group.Bonuses);
                            bonusSourceCharacter.Effects.AccumulatePendingActionCadenceBank(payload, layers);
                        }
                    }
                }
            }
            // Sheet accuracy (hero / enemy columns) is omitted from this roll when cadence is not TURN
            // (see Action.DefersSheetCombatPackagesToNextHeroRoll). On a successful hit only: queue hero ACCURACY
            // FIFO layers (same pipeline as peeked ACCURACY in ActionExecutionFlow / ActionSelector) â€” not SetTempRollBonus,
            // so the HUD shows a single "Accuracy" line. Enemy debuff uses RollPenalty on the target for their next N attack/spell rolls (N = duration).
            if (Action.DefersSheetCombatPackagesToNextHeroRoll(selected))
            {
                bool actionCadenceDeferred = CadenceKeywords.IsAction(selected.Cadence);
                if (!actionCadenceDeferred || result.IsCombo)
                {
                int accTurns = selected.Advanced.RollBonusDuration > 0
                    ? selected.Advanced.RollBonusDuration
                    : (selected.ComboBonusDuration > 0 ? selected.ComboBonusDuration : 1);
                if (accTurns < 1) accTurns = 1;

                if (actionCadenceDeferred && selected.IsComboAction && source is Character comboClipHero && !(comboClipHero is Enemy))
                {
                    var comboActionsForClip = ActionUtilities.GetComboActions(comboClipHero);
                    if (comboActionsForClip.Count > 0)
                    {
                        int remaining = ActionUtilities.CountRemainingComboActionsAfter(selected, comboActionsForClip);
                        if (remaining >= 0)
                            accTurns = Math.Min(accTurns, remaining);
                    }
                }

                // Use this swing's resolved hit count — not strip peek after bank deposit (would apply Rapid Strike's +1 MH to itself).
                int hitLayers = Math.Max(1, result.ResolvedMultiHitCount);

                if (selected.Advanced.RollBonus != 0 && accTurns > 0)
                {
                    int scaledRollBonus = selected.Advanced.RollBonus * hitLayers;
                    if (source is Character heroAcc && !(heroAcc is Enemy))
                    {
                        if (CadenceKeywords.IsFight(selected.Cadence) || CadenceKeywords.IsDungeon(selected.Cadence))
                        {
                            CadenceScopedBuffApplicator.DepositToScope(heroAcc, CadenceKeywords.Normalize(selected.Cadence),
                                new List<ActionAttackBonusItem>
                                {
                                    new ActionAttackBonusItem { Type = "ACCURACY", Value = scaledRollBonus }
                                }, accTurns);
                        }
                        else
                        {
                            heroAcc.Effects.AccumulatePendingActionCadenceBank(new List<ActionAttackBonusItem>
                            {
                                new ActionAttackBonusItem { Type = "ACCURACY", Value = scaledRollBonus }
                            }, accTurns);
                        }
                    }
                    else if (source is Enemy enemyAcc)
                    {
                        enemyAcc.Effects.AccumulatePendingActionCadenceBank(new List<ActionAttackBonusItem>
                        {
                            new ActionAttackBonusItem { Type = "ACCURACY", Value = scaledRollBonus }
                        }, accTurns);
                    }
                }

                if (!ReferenceEquals(source, target) && selected.Advanced.EnemyRollBonus < 0 && accTurns > 0)
                    target.ApplyRollPenalty(-selected.Advanced.EnemyRollBonus * hitLayers, accTurns);
                }
            }
            if (source is Character modSourceCharacter && !(modSourceCharacter is Enemy))
            {
                int? nextComboSlot = null;
                if (result.IsCombo && selected.IsComboAction && CadenceKeywords.IsAction(selected.Cadence))
                {
                    var comboActions = ActionUtilities.GetComboActions(modSourceCharacter);
                    if (comboActions.Count > 0)
                        nextComboSlot = ActionUtilities.GetNextComboSlotForPendingBonuses(modSourceCharacter, selected, comboActions);
                }
                modSourceCharacter.Effects.AddModifierBonusesFromAction(selected, nextComboSlot, useEnemySpreadsheetMods: false, modSourceCharacter);
                var enemyTargetMods = CharacterEffectsState.BuildModifierBonusesFromActionFields(selected, useEnemySpreadsheetMods: true);
                if (enemyTargetMods.Count > 0 && target is Enemy enemyReceivingMods)
                    enemyReceivingMods.Effects.AddPendingActionBonusesNextHeroRoll(enemyTargetMods);
            }
            else if (source is Enemy enemyModSource)
            {
                int? nextComboSlot = null;
                if (result.IsCombo && selected.IsComboAction && CadenceKeywords.IsAction(selected.Cadence))
                {
                    var enemyComboActions = ActionUtilities.GetComboActions(enemyModSource);
                    if (enemyComboActions.Count > 0)
                        nextComboSlot = ActionUtilities.GetNextComboSlotForPendingBonuses(enemyModSource, selected, enemyComboActions);
                }
                enemyModSource.Effects.AddModifierBonusesFromAction(selected, nextComboSlot, useEnemySpreadsheetMods: true);
            }
            CombatEffectsSimplified.ApplyStatusEffects(selected, source, target, result.StatusEffectMessages, hitEvent);

            if (source is Character mechanicHero && mechanicHero is not Enemy)
                ActionMechanicTagProcessor.QueueNextActionBonuses(mechanicHero, selected);

            if (selected.Name == "FOLLOW THROUGH" && source is Character followThroughCharacter && !(followThroughCharacter is Enemy))
            {
                followThroughCharacter.Effects.NextTurnDamageMultiplier = 3.0;
                var followStatEntries = GetStatBonusEntries(selected);
                var firstFollow = followStatEntries.Count > 0 ? followStatEntries[0] : null;
                if (firstFollow != null && (firstFollow.Value != 0 || !string.IsNullOrEmpty(firstFollow.Type)))
                {
                    followThroughCharacter.Effects.NextTurnStatBonus = firstFollow.Value;
                    followThroughCharacter.Effects.NextTurnStatBonusType = firstFollow.Type;
                    followThroughCharacter.Effects.NextTurnStatBonusDuration = CadenceToStatBonusDuration(selected.Cadence);
                }
            }
            else
            {
                var statEntries = GetStatBonusEntries(selected);
                if (statEntries.Count > 0 && source is Character statBonusCharacter && !(statBonusCharacter is Enemy)
                    && !DefersStatBonusToCadenceQueue(selected))
                {
                    if (CadenceKeywords.IsFight(selected.Cadence) || CadenceKeywords.IsDungeon(selected.Cadence))
                    {
                        int stackTimes = selected.ComboBonusDuration > 0 ? selected.ComboBonusDuration : 1;
                        var items = new List<ActionAttackBonusItem>();
                        foreach (var entry in statEntries)
                        {
                            if (entry.Value == 0 && string.IsNullOrEmpty(entry.Type)) continue;
                            string statType = (entry.Type ?? "").ToUpper();
                            if (string.IsNullOrEmpty(statType)) continue;
                            items.Add(new ActionAttackBonusItem { Type = statType, Value = entry.Value });
                        }
                        if (items.Count > 0)
                            CadenceScopedBuffApplicator.DepositToScope(statBonusCharacter, CadenceKeywords.Normalize(selected.Cadence), items, stackTimes);
                    }
                    else
                    {
                        int duration = CadenceToStatBonusDuration(selected.Cadence);
                        foreach (var entry in statEntries)
                        {
                            if (entry.Value == 0 && string.IsNullOrEmpty(entry.Type)) continue;
                            string statType = (entry.Type ?? "").ToUpper();
                            if (string.IsNullOrEmpty(statType)) continue;
                            if (!DynamicAttributeCategoryResolver.IsStatOrDynamicCategoryType(statType)) continue;
                            string concrete = DynamicAttributeCategoryResolver.ResolveStatBonusTypeToConcreteCode(statBonusCharacter, statType);
                            if (concrete != DynamicAttributeCategoryResolver.CodeStrength && concrete != DynamicAttributeCategoryResolver.CodeAgility
                                && concrete != DynamicAttributeCategoryResolver.CodeTechnique && concrete != DynamicAttributeCategoryResolver.CodeIntelligence)
                                continue;
                            int currentBonus = ReadTempStatBonusForResolvedCode(statBonusCharacter, concrete);
                            statBonusCharacter.ApplyStatBonus(currentBonus + entry.Value, statType, duration);
                        }
                    }
                }
            }

            if (target is Enemy enemyTarget)
            {
                if (enemyTarget.CurrentHealth <= 0)
                    ActionEventPublisher.PublishEnemyDeath(source, target, selected, result.Damage);
                else
                {
                    double healthPercentage = (double)enemyTarget.CurrentHealth / enemyTarget.MaxHealth;
                    var thresholds = GetHealthThresholds(selected);
                    var candidates = thresholds.Where(t => t >= healthPercentage).OrderBy(t => t).ToList();
                    if (candidates.Count > 0)
                        ActionEventPublisher.PublishHealthThreshold(source, target, selected, candidates[0]);
                }
            }

            // Heroes and enemies (Enemy : Character) share combo-step advancement on successful combo-threshold rolls.
            if (source is Character comboCharacter)
            {
                int comboStepBeforeAdvance = comboCharacter.ComboStep;
                // Must match the effective combo threshold used for IsCombo / UI (per-roll COMBO bonuses, action roll mods, cascades).
                int comboThreshold = RollModificationManager.GetThresholdManager().GetComboThreshold(comboCharacter);
                // Only advance combo step when the executed action was a combo action (prevents skipping slots on normal attacks)
                if (selected.IsComboAction)
                {
                    if (comboCharacter.ComboStep == 0)
                    {
                        if (result.AttackRoll >= comboThreshold)
                            comboCharacter.IncrementComboStep(selected);
                    }
                    else if (comboCharacter.ComboStep == 1)
                    {
                        if (result.AttackRoll >= comboThreshold)
                            comboCharacter.IncrementComboStep(selected);
                        else
                            comboCharacter.ComboStep = 0;
                    }
                    else if (comboCharacter.ComboStep >= 2)
                    {
                        if (result.AttackRoll >= comboThreshold)
                            comboCharacter.IncrementComboStep(selected);
                        else
                            comboCharacter.ComboStep = 0;
                    }

                    // Action Interaction Lab: on a successful combo hit, advance the strip if threshold logic did not
                    // (sandbox UX â€” avoids catalog stuck on slot 1 when attack total is below combo threshold).
                    if (ActionInteractionLabSession.Current is { } labForStrip
                        && !(comboCharacter is Enemy)
                        && result.Hit
                        && comboCharacter.ComboStep == comboStepBeforeAdvance
                        && string.Equals(comboCharacter.Name, labForStrip.LabPlayer.Name, StringComparison.Ordinal))
                    {
                        comboCharacter.IncrementComboStep(selected);
                    }
                }
                else if (selected.Type == ActionType.Attack || selected.Type == ActionType.Spell)
                {
                    // A completed normal (non-combo) attack breaks the chain â€” start the combo sequence over
                    comboCharacter.ResetCombo();
                }
            }
        }

        private static void ApplyMissOutcome(Actor source, Actor target, ActionExecutionResult result, BattleNarrative? battleNarrative)
        {
            if (source is Character modMissCharacter && !(modMissCharacter is Enemy))
            {
                modMissCharacter.Effects.AddModifierBonusesFromAction(result.SelectedAction, useEnemySpreadsheetMods: false);
                modMissCharacter.Effects.GetAndClearConsumedTurnBonusesThisRoll(); // Discard; TURN bonuses consumed on roll but not applied on miss
                if (result.TurnStatSnapshot is { } snapshot)
                    TempStatSnapshot.Restore(modMissCharacter, snapshot);
            }
            else if (source is Enemy enemyMiss)
            {
                enemyMiss.Effects.AddModifierBonusesFromAction(result.SelectedAction, useEnemySpreadsheetMods: true);
            }
            if (source is Character heroMissCadence && !(heroMissCadence is Enemy)
                && result.SelectedAction != null
                && result.PendingActionCadenceLayerPeekedForRoll)
            {
                ResolvePendingActionCadenceBonuses(heroMissCadence, result.SelectedAction, result);
            }
            // Critical miss (fumble): ACTION cadence affects the enemy â€” duration counts down on the enemy's attack rolls, not the hero's.
            if (result.IsCriticalMiss
                && target is Enemy enemyForFumble
                && source is Character heroFumble
                && !(heroFumble is Enemy)
                && result.SelectedAction?.ActionAttackBonuses?.BonusGroups != null)
            {
                foreach (var group in result.SelectedAction.ActionAttackBonuses.BonusGroups)
                {
                    var ct = CadenceKeywords.NormalizeCadenceType(
                        string.IsNullOrEmpty(group.CadenceType) ? group.Keyword : group.CadenceType);
                    if (!CadenceKeywords.IsAction(ct) || group.Bonuses == null)
                        continue;
                    int layers = ActionCadenceDurationResolver.GetRequestedLayerCount(result.SelectedAction, group);
                    if (layers < 1) layers = 1;
                    var payload = CloneActionAttackBonusItems(group.Bonuses);
                    enemyForFumble.Effects.AccumulatePendingActionCadenceBank(payload, layers);
                }
            }
            ActionEventPublisher.PublishActionMiss(source, target, result.SelectedAction!, result.AttackRoll, result.IsCriticalMiss);
            if (source is Character characterMiss)
                ActionStatisticsTracker.RecordMissAction(characterMiss, result.BaseRoll, result.RollBonus);
            if (source is Character comboCharacterMiss)
                comboCharacterMiss.ComboStep = 0;
            ActionUtilities.CreateAndAddBattleEvent(source, target, result.SelectedAction!, 0, result.ModifiedBaseRoll + result.RollBonus, result.RollBonus, false, false, 0, 0, false, result.BaseRoll, battleNarrative);
        }

        /// <summary>
        /// ACTION cadence slot + bank: peeked during roll prep for threshold/roll help.
        /// Redeemed (consumed + applied) only on hit+combo. Miss and non-combo hit leave
        /// pending bonuses in place so the next-action strip stays updated until a combo lands.
        /// </summary>
        private static void ResolvePendingActionCadenceBonuses(Character hero, Action selectedAction, ActionExecutionResult result)
        {
            if (!result.PendingActionCadenceLayerPeekedForRoll)
                return;

            // Stay pending across misses / normal hits; only combo redeems the bank and slot.
            if (!result.IsCombo)
                return;

            var slotConsumed = new List<ActionAttackBonusItem>();
            var comboActions = ActionUtilities.GetComboActions(hero);
            if (comboActions.Count > 0)
            {
                int currentSlot = ActionUtilities.GetComboSlotForPendingBonuses(hero, selectedAction, comboActions);
                slotConsumed = hero.Effects.ConsumePendingActionBonusesForSlot(currentSlot);
            }
            var bankConsumed = hero.Effects.ConsumePendingActionBonusesNextHeroRoll();

            int duration = CadenceToStatBonusDuration(CadenceKeywords.Action);
            ApplyStatBonusesFromCadenceItems(hero, slotConsumed, duration);
            ApplyStatBonusesFromCadenceItems(hero, bankConsumed, duration);
            hero.Effects.AccumulateConsumedModifierBonuses(slotConsumed);
            hero.Effects.AccumulateConsumedModifierBonuses(bankConsumed);
        }

        /// <summary>
        /// Gold strip flash: any successful combo action hit.
        /// Non-combo hits stay green.
        /// </summary>
        internal static bool ShouldFlashComboComplete(Character hero, int stripIndexForResolvedSwing, ActionExecutionResult result)
        {
            if (!result.Hit || result.SelectedAction?.IsComboAction != true)
                return false;
            return true;
        }
    }
}
