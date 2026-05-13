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
        private static void ApplyHitOutcome(Actor source, Actor target, ActionExecutionResult result, BattleNarrative? battleNarrative)
        {
            var selected = result.SelectedAction;
            if (selected == null) return;

            if (source is Character abilityBonusCharacter && !(abilityBonusCharacter is Enemy))
            {
                // Apply ABILITY bonuses (consumed on hit)
                var abilityBonuses = abilityBonusCharacter.Effects.GetAndConsumeAbilityBonuses(true);
                abilityBonusCharacter.Effects.AccumulateConsumedModifierBonuses(abilityBonuses);
                foreach (var bonus in abilityBonuses)
                {
                    string bonusType = bonus.Type.ToUpper();
                    if (!DynamicAttributeCategoryResolver.IsStatOrDynamicCategoryType(bonusType)) continue;
                    string concrete = DynamicAttributeCategoryResolver.ResolveStatBonusTypeToConcreteCode(abilityBonusCharacter, bonusType);
                    if (concrete != DynamicAttributeCategoryResolver.CodeStrength && concrete != DynamicAttributeCategoryResolver.CodeAgility
                        && concrete != DynamicAttributeCategoryResolver.CodeTechnique && concrete != DynamicAttributeCategoryResolver.CodeIntelligence)
                        continue;
                    int currentBonus = ReadTempStatBonusForResolvedCode(abilityBonusCharacter, concrete);
                    abilityBonusCharacter.ApplyStatBonus(currentBonus + (int)bonus.Value, bonusType, 999);
                }
                // Apply consumed ATTACK bonuses (stat bonuses only on hit)
                var consumedAttack = abilityBonusCharacter.Effects.GetAndClearConsumedAttackBonusesThisRoll();
                foreach (var bonus in consumedAttack)
                {
                    string bonusType = (bonus.Type ?? "").ToUpper();
                    if (!DynamicAttributeCategoryResolver.IsStatOrDynamicCategoryType(bonusType)) continue;
                    string concrete = DynamicAttributeCategoryResolver.ResolveStatBonusTypeToConcreteCode(abilityBonusCharacter, bonusType);
                    if (concrete != DynamicAttributeCategoryResolver.CodeStrength && concrete != DynamicAttributeCategoryResolver.CodeAgility
                        && concrete != DynamicAttributeCategoryResolver.CodeTechnique && concrete != DynamicAttributeCategoryResolver.CodeIntelligence)
                        continue;
                    int currentBonus = ReadTempStatBonusForResolvedCode(abilityBonusCharacter, concrete);
                    abilityBonusCharacter.ApplyStatBonus(currentBonus + (int)bonus.Value, bonusType, 999);
                }
            }
            var hitEvent = ActionEventPublisher.PublishActionHit(
                source, target, selected, result.AttackRoll, result.IsCombo, result.IsCritical);

            if (selected.Type == ActionType.Attack || selected.Type == ActionType.Spell)
            {
                double targetLowHealthBefore = ActionEventPublisher.GetActorHealthPercentage(target);
                double sourceLowHealthBefore = selected.Target == TargetType.SelfAndTarget
                    ? ActionEventPublisher.GetActorHealthPercentage(source)
                    : double.NaN;

                double damageMultiplier = ActionUtilities.CalculateDamageMultiplier(source, selected);
                int totalRoll = result.ModifiedBaseRoll + result.RollBonus;
                int multiHitCount = selected.Advanced.MultiHitCount;
                if (source is Character multiHitCharacter && multiHitCharacter.Effects.ConsumedMultiHitMod != 0)
                    multiHitCount = Math.Max(1, multiHitCount + (int)Math.Max(0, multiHitCharacter.Effects.ConsumedMultiHitMod));
                multiHitCount = Math.Max(1, multiHitCount + ChainPositionBonusApplier.GetMultiHitDelta(source, selected, ActionUtilities.GetComboActions(source), ActionUtilities.GetComboStep(source)));
                if (multiHitCount > 1)
                {
                    result.Damage = MultiHitProcessor.ProcessMultiHit(
                        source, target, selected, damageMultiplier, totalRoll,
                        result.ModifiedBaseRoll, result.RollBonus, result.BaseRoll, battleNarrative,
                        source.RollPenalty);
                }
                else
                {
                    result.Damage = CombatCalculator.CalculateDamage(source, target, selected, damageMultiplier, 1.0, result.RollBonus, totalRoll);
                    if (selected.Target == TargetType.SelfAndTarget)
                    {
                        ActionUtilities.ApplyDamage(target, result.Damage);
                        ActionUtilities.ApplyDamage(source, result.Damage);
                    }
                    else
                        ActionUtilities.ApplyDamage(target, result.Damage);
                    if (source is Character sourceCharacter)
                        ActionStatisticsTracker.RecordAttackAction(sourceCharacter, totalRoll, result.BaseRoll, result.RollBonus, result.Damage, selected, target as Enemy, result.IsCritical);
                    if (target is Character targetCharacter)
                        ActionStatisticsTracker.RecordDamageReceived(targetCharacter, result.Damage);
                    if (selected.Target == TargetType.SelfAndTarget && source is Character selfCharacter)
                        ActionStatisticsTracker.RecordDamageReceived(selfCharacter, result.Damage);
                    ActionUtilities.CreateAndAddBattleEvent(source, target, selected, result.Damage, totalRoll, result.RollBonus, true, result.IsCombo, 0, 0, result.IsCritical, result.BaseRoll, battleNarrative);
                }

                ActionEventPublisher.PublishLowHealthThresholdIfCrossed(target, targetLowHealthBefore);
                if (selected.Target == TargetType.SelfAndTarget && !ReferenceEquals(source, target))
                    ActionEventPublisher.PublishLowHealthThresholdIfCrossed(source, sourceLowHealthBefore);
            }
            else if (selected.Type == ActionType.Heal)
            {
                result.HealAmount = ActionUtilities.CalculateHealAmount(source, selected);
                ActionUtilities.ApplyHealing(target, result.HealAmount);
                if (target is Character targetCharacterHeal)
                    ActionStatisticsTracker.RecordHealingReceived(targetCharacterHeal, result.HealAmount);
                ActionUtilities.CreateAndAddBattleEvent(source, target, selected, 0, result.BaseRoll + result.RollBonus, result.RollBonus, true, result.IsCombo, 0, result.HealAmount, false, result.BaseRoll, battleNarrative);
            }
            else
            {
                ActionUtilities.CreateAndAddBattleEvent(source, target, selected, 0, result.ModifiedBaseRoll + result.RollBonus, result.RollBonus, true, result.IsCombo, 0, 0, false, result.BaseRoll, battleNarrative);
            }

            if (selected.ActionAttackBonuses != null && source is Character bonusSourceCharacter && !(bonusSourceCharacter is Enemy))
            {
                bonusSourceCharacter.Effects.AddActionAttackBonuses(selected.ActionAttackBonuses);
                // ACTION cadence: FIFO layers â€” one layer consumed per hero attack roll (enemy turns do not count).
                // Spreadsheet Count / duration = number of consecutive hero actions that receive one application each.
                bool grantOnComboSuccess = result.IsCombo && selected.IsComboAction;
                bool grantOnComboFailSelf = result.Hit && selected.IsComboAction && !result.IsCombo;
                if (grantOnComboSuccess || grantOnComboFailSelf)
                {
                    var comboActions = ActionUtilities.GetComboActions(bonusSourceCharacter);
                    if (comboActions.Count > 0)
                    {
                        foreach (var group in selected.ActionAttackBonuses.BonusGroups)
                        {
                            var ct = string.IsNullOrEmpty(group.CadenceType) ? group.Keyword : group.CadenceType;
                            if (!string.Equals(ct, "ACTION", StringComparison.OrdinalIgnoreCase) || group.Bonuses == null)
                                continue;
                            int layers = grantOnComboSuccess
                                ? (group.Count > 0 ? group.Count : 1)
                                : 1;
                            var payload = CloneActionAttackBonusItems(group.Bonuses);
                            for (int i = 0; i < layers; i++)
                                bonusSourceCharacter.Effects.AddPendingActionBonusesNextHeroRoll(payload);
                        }
                    }
                }
            }
            // Sheet accuracy (hero / enemy columns) is omitted from this roll when cadence is not ATTACK
            // (see Action.DefersSheetCombatPackagesToNextHeroRoll). On a successful hit only: queue hero ACCURACY
            // FIFO layers (same pipeline as peeked ACCURACY in ActionExecutionFlow / ActionSelector) â€” not SetTempRollBonus,
            // so the HUD shows a single "Accuracy" line. Enemy debuff uses RollPenalty on the target for their next N attack/spell rolls (N = duration).
            if (Action.DefersSheetCombatPackagesToNextHeroRoll(selected))
            {
                int accTurns = selected.Advanced.RollBonusDuration > 0
                    ? selected.Advanced.RollBonusDuration
                    : (selected.ComboBonusDuration > 0 ? selected.ComboBonusDuration : 1);
                if (accTurns < 1) accTurns = 1;

                int hitLayers = RollModificationManager.GetEffectiveMultiHitCountForModifierScaling(selected, source);
                if (hitLayers < 1) hitLayers = 1;

                if (selected.Advanced.RollBonus != 0)
                {
                    int scaledRollBonus = selected.Advanced.RollBonus * hitLayers;
                    if (source is Character heroAcc && !(heroAcc is Enemy))
                    {
                        for (int t = 0; t < accTurns; t++)
                        {
                            heroAcc.Effects.AddPendingActionBonusesNextHeroRoll(new List<ActionAttackBonusItem>
                            {
                                new ActionAttackBonusItem { Type = "ACCURACY", Value = scaledRollBonus }
                            });
                        }
                    }
                    else if (source is Enemy enemyAcc)
                    {
                        for (int t = 0; t < accTurns; t++)
                        {
                            enemyAcc.Effects.AddPendingActionBonusesNextHeroRoll(new List<ActionAttackBonusItem>
                            {
                                new ActionAttackBonusItem { Type = "ACCURACY", Value = scaledRollBonus }
                            });
                        }
                    }
                }

                if (!ReferenceEquals(source, target) && selected.Advanced.EnemyRollBonus < 0)
                    target.ApplyRollPenalty(-selected.Advanced.EnemyRollBonus * hitLayers, accTurns);
            }
            if (source is Character modSourceCharacter && !(modSourceCharacter is Enemy))
            {
                int? nextSlotForAbilityMod = null;
                if (result.IsCombo && selected.IsComboAction
                    && string.Equals(selected.Cadence?.Trim(), "Ability", StringComparison.OrdinalIgnoreCase))
                {
                    var comboActions = ActionUtilities.GetComboActions(modSourceCharacter);
                    if (comboActions.Count > 0)
                        nextSlotForAbilityMod = ActionUtilities.GetNextComboSlotForPendingBonuses(modSourceCharacter, selected, comboActions);
                }
                modSourceCharacter.Effects.AddModifierBonusesFromAction(selected, nextSlotForAbilityMod, useEnemySpreadsheetMods: false);
                var enemyTargetMods = CharacterEffectsState.BuildModifierBonusesFromActionFields(selected, useEnemySpreadsheetMods: true);
                if (enemyTargetMods.Count > 0 && target is Enemy enemyReceivingMods)
                    enemyReceivingMods.Effects.AddPendingActionBonusesNextHeroRoll(enemyTargetMods);
            }
            else if (source is Enemy enemyModSource)
            {
                int? nextSlotForEnemyAbility = null;
                if (result.IsCombo && selected.IsComboAction
                    && string.Equals(selected.Cadence?.Trim(), "Ability", StringComparison.OrdinalIgnoreCase))
                {
                    var enemyComboActions = ActionUtilities.GetComboActions(enemyModSource);
                    if (enemyComboActions.Count > 0)
                        nextSlotForEnemyAbility = ActionUtilities.GetNextComboSlotForPendingBonuses(enemyModSource, selected, enemyComboActions);
                }
                enemyModSource.Effects.AddModifierBonusesFromAction(selected, nextSlotForEnemyAbility, useEnemySpreadsheetMods: true);
            }
            CombatEffectsSimplified.ApplyStatusEffects(selected, source, target, result.StatusEffectMessages, hitEvent);

            if (selected.Name == "FOLLOW THROUGH" && source is Character followThroughCharacter && !(followThroughCharacter is Enemy))
            {
                followThroughCharacter.Effects.NextAttackDamageMultiplier = 3.0;
                var followStatEntries = GetStatBonusEntries(selected);
                var firstFollow = followStatEntries.Count > 0 ? followStatEntries[0] : null;
                if (firstFollow != null && (firstFollow.Value != 0 || !string.IsNullOrEmpty(firstFollow.Type)))
                {
                    followThroughCharacter.Effects.NextAttackStatBonus = firstFollow.Value;
                    followThroughCharacter.Effects.NextAttackStatBonusType = firstFollow.Type;
                    followThroughCharacter.Effects.NextAttackStatBonusDuration = CadenceToStatBonusDuration(selected.Cadence);
                }
            }
            else
            {
                var statEntries = GetStatBonusEntries(selected);
                if (statEntries.Count > 0 && source is Character statBonusCharacter && !(statBonusCharacter is Enemy))
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
                // Ability cadence: modifier bonuses apply only when the next ability succeeds; skip on miss
                if (!string.Equals(result.SelectedAction!.Cadence?.Trim(), "Ability", StringComparison.OrdinalIgnoreCase))
                    modMissCharacter.Effects.AddModifierBonusesFromAction(result.SelectedAction, useEnemySpreadsheetMods: false);
                modMissCharacter.Effects.GetAndClearConsumedAttackBonusesThisRoll(); // Discard; ATTACK bonuses consumed on roll but not applied on miss
            }
            else if (source is Enemy enemyMiss)
            {
                if (!string.Equals(result.SelectedAction!.Cadence?.Trim(), "Ability", StringComparison.OrdinalIgnoreCase))
                    enemyMiss.Effects.AddModifierBonusesFromAction(result.SelectedAction, useEnemySpreadsheetMods: true);
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
                    var ct = string.IsNullOrEmpty(group.CadenceType) ? group.Keyword : group.CadenceType;
                    if (!string.Equals(ct, "ACTION", StringComparison.OrdinalIgnoreCase) || group.Bonuses == null)
                        continue;
                    int layers = group.Count > 0 ? group.Count : 1;
                    var payload = CloneActionAttackBonusItems(group.Bonuses);
                    for (int i = 0; i < layers; i++)
                        enemyForFumble.Effects.AddPendingActionBonusesNextHeroRoll(payload);
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
