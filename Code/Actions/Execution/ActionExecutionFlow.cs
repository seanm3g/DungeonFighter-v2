using RPGGame;
using RPGGame.ActionInteractionLab;
using RPGGame.Actions.RollModification;
using RPGGame.Combat.Events;
using RPGGame.Utils;
using RPGGame.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Actions.Execution
{
    /// <summary>
    /// Handles the core action execution flow
    /// Manages action selection, roll calculation, hit/miss determination, and damage/healing application
    /// </summary>
    internal static class ActionExecutionFlow
    {
        internal static event System.Action? OneShotKillOccurred;

        internal static void NotifyOneShotKillOccurred() => OneShotKillOccurred?.Invoke();

        private static List<ActionAttackBonusItem> CloneActionAttackBonusItems(List<ActionAttackBonusItem>? src)
        {
            if (src == null || src.Count == 0) return new List<ActionAttackBonusItem>();
            return src.Select(b => new ActionAttackBonusItem { Type = b.Type, Value = b.Value }).ToList();
        }

        /// <summary>Returns stat bonus entries from the action (list if non-empty, else legacy single as one entry).</summary>
        private static List<StatBonusEntry> GetStatBonusEntries(Action action)
        {
            if (action?.Advanced == null) return new List<StatBonusEntry>();
            if (action.Advanced.StatBonuses != null && action.Advanced.StatBonuses.Count > 0)
                return action.Advanced.StatBonuses;
            if (action.Advanced.StatBonus != 0 || !string.IsNullOrEmpty(action.Advanced.StatBonusType))
                return new List<StatBonusEntry> { new StatBonusEntry { Value = action.Advanced.StatBonus, Type = action.Advanced.StatBonusType ?? "" } };
            return new List<StatBonusEntry>();
        }

        /// <summary>Returns Health-type threshold values from the action (for HP band publishing), ascending.</summary>
        private static List<double> GetHealthThresholds(Action action)
        {
            if (action?.Advanced == null) return new List<double> { 0.1, 0.25, 0.5 };
            if (action.Advanced.Thresholds != null && action.Advanced.Thresholds.Count > 0)
            {
                var healthValues = action.Advanced.Thresholds
                    .Where(t => string.Equals(t.Type, "Health", System.StringComparison.OrdinalIgnoreCase))
                    .Select(t => t.Value)
                    .OrderBy(v => v)
                    .ToList();
                if (healthValues.Count > 0) return healthValues;
            }
            if (action.Advanced.HealthThreshold > 0.0)
                return new List<double> { action.Advanced.HealthThreshold };
            return new List<double> { 0.1, 0.25, 0.5 };
        }

        /// <summary>Maps cadence (Action, Ability, Chain, Fight, Dungeon) to stat bonus duration in turns.</summary>
        private static int CadenceToStatBonusDuration(string? cadence)
        {
            if (string.IsNullOrWhiteSpace(cadence)) return 1;
            var c = cadence.Trim();
            if (string.Equals(c, "Fight", System.StringComparison.OrdinalIgnoreCase)) return 999;
            if (string.Equals(c, "Dungeon", System.StringComparison.OrdinalIgnoreCase)) return 999;
            if (string.Equals(c, "Action", System.StringComparison.OrdinalIgnoreCase)) return 1;
            if (string.Equals(c, "Ability", System.StringComparison.OrdinalIgnoreCase)) return 1;
            if (string.Equals(c, "Chain", System.StringComparison.OrdinalIgnoreCase)) return 1;
            return 1;
        }
        /// <summary>
        /// Executes the core action execution sequence
        /// </summary>
        public static ActionExecutionResult Execute(
            Actor source,
            Actor target,
            Environment? environment,
            Action? lastPlayerAction,
            Action? forcedAction,
            BattleNarrative? battleNarrative,
            Dictionary<Actor, Action> lastUsedActions,
            Dictionary<Actor, bool> lastCriticalMissStatus)
        {
            var result = new ActionExecutionResult();
            ApplyPreRollBonuses(source);
            SelectActionAndResolveRoll(source, target, result, lastUsedActions, lastCriticalMissStatus, forcedAction);
            if (result.SelectedAction == null)
                return result;
            if (result.Hit)
                ApplyHitOutcome(source, target, result, battleNarrative);
            else
                ApplyMissOutcome(source, target, result, battleNarrative);
            source.ConsumeRollPenaltyAfterCombatRoll(result.SelectedAction);
            return result;
        }

        private static void ApplyPreRollBonuses(Actor source)
        {
            // Heroes and enemies (Enemy : Character) must reset consumed sheet mods each attack.
            // Otherwise DAMAGE_MOD / AMP_MOD from the previous swing stays in Consumed* and stacks
            // with the next roll's FIFO layer (enemy damage bank from ModTrade looked "ignored").
            if (source is Character clearModCharacter)
                clearModCharacter.Effects.ClearConsumedModifierBonuses();
            if (source is Character nextAttackStatCharacter && !(nextAttackStatCharacter is Enemy))
            {
                var (nextBonus, nextStatType, nextDuration) = nextAttackStatCharacter.Effects.ConsumeNextAttackStatBonus();
                if (nextBonus == 0 || string.IsNullOrEmpty(nextStatType)) return;
                string statType = nextStatType!.ToUpper();
                int currentBonus = statType switch
                {
                    "STR" or "STRENGTH" => nextAttackStatCharacter.Stats.TempStrengthBonus,
                    "AGI" or "AGILITY" => nextAttackStatCharacter.Stats.TempAgilityBonus,
                    "TEC" or "TECH" or "TECHNIQUE" => nextAttackStatCharacter.Stats.TempTechniqueBonus,
                    "INT" or "INTELLIGENCE" => nextAttackStatCharacter.Stats.TempIntelligenceBonus,
                    _ => 0
                };
                int newBonus = currentBonus + nextBonus;
                int duration = nextDuration > 0 ? nextDuration : 999;
                nextAttackStatCharacter.ApplyStatBonus(newBonus, statType, duration);
            }
        }

        private static void SelectActionAndResolveRoll(
            Actor source,
            Actor? target,
            ActionExecutionResult result,
            Dictionary<Actor, Action> lastUsedActions,
            Dictionary<Actor, bool> lastCriticalMissStatus,
            Action? forcedAction)
        {
            result.SelectedAction = forcedAction ?? ActionSelector.SelectActionByEntityType(source);
            if (result.SelectedAction == null) return;
            lastUsedActions[source] = result.SelectedAction;
            result.BaseRoll = ActionSelector.GetActionRoll(source);
            if (source is Character character && !(character is Enemy) && forcedAction == null)
                result.SelectedAction = ActionUtilities.HandleUniqueActionChance(character, result.SelectedAction);

            // Baseline thresholds for this roll only (prevents one-shot ACTION/ATTACK/ABILITY bonuses and action RollMods from stacking across attacks).
            var thresholdManager = RollModificationManager.GetThresholdManager();
            thresholdManager.ResetThresholds(source);
            if (target != null)
                thresholdManager.ResetThresholds(target);

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
                    int currentSlot = actionBonusCharacter.ComboStep % comboLength;
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
            // Pending queue + ABILITY peek: SET_* deferred overrides first, then deltas (crit miss → crit → combo → hit).
            if (actionBonusCritMiss != 0 && source is Character cmBonusCharacter)
                thresholdManager.AdjustCriticalMissThreshold(cmBonusCharacter, actionBonusCritMiss);
            if (actionBonusCrit != 0 && source is Character critBonusCharacter)
                thresholdManager.AdjustCriticalHitThreshold(critBonusCharacter, actionBonusCrit);
            if (actionBonusCombo != 0 && source is Character comboBonusCharacter)
                thresholdManager.AdjustComboThreshold(comboBonusCharacter, actionBonusCombo);
            if (actionBonusHit != 0 && source is Character hitBonusCharacter)
                thresholdManager.AdjustHitThreshold(hitBonusCharacter, actionBonusHit);

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
                    if (bonusType != "STR" && bonusType != "AGI" && bonusType != "TECH" && bonusType != "INT") continue;
                    int currentBonus = bonusType switch
                    {
                        "STR" => abilityBonusCharacter.Stats.TempStrengthBonus,
                        "AGI" => abilityBonusCharacter.Stats.TempAgilityBonus,
                        "TECH" => abilityBonusCharacter.Stats.TempTechniqueBonus,
                        _ => abilityBonusCharacter.Stats.TempIntelligenceBonus
                    };
                    abilityBonusCharacter.ApplyStatBonus(currentBonus + (int)bonus.Value, bonusType, 999);
                }
                // Apply consumed ATTACK bonuses (stat bonuses only on hit)
                var consumedAttack = abilityBonusCharacter.Effects.GetAndClearConsumedAttackBonusesThisRoll();
                foreach (var bonus in consumedAttack)
                {
                    string bonusType = (bonus.Type ?? "").ToUpper();
                    if (bonusType != "STR" && bonusType != "AGI" && bonusType != "TECH" && bonusType != "INT") continue;
                    int currentBonus = bonusType switch
                    {
                        "STR" => abilityBonusCharacter.Stats.TempStrengthBonus,
                        "AGI" => abilityBonusCharacter.Stats.TempAgilityBonus,
                        "TECH" => abilityBonusCharacter.Stats.TempTechniqueBonus,
                        _ => abilityBonusCharacter.Stats.TempIntelligenceBonus
                    };
                    abilityBonusCharacter.ApplyStatBonus(currentBonus + (int)bonus.Value, bonusType, 999);
                }
            }
            var hitEvent = ActionEventPublisher.PublishActionHit(
                source, target, selected, result.AttackRoll, result.IsCombo, result.IsCritical);

            if (selected.Type == ActionType.Attack || selected.Type == ActionType.Spell)
            {
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
                        ActionStatisticsTracker.RecordAttackAction(sourceCharacter, totalRoll, result.BaseRoll, result.RollBonus, result.Damage, selected, target as Enemy);
                    if (target is Character targetCharacter)
                        ActionStatisticsTracker.RecordDamageReceived(targetCharacter, result.Damage);
                    if (selected.Target == TargetType.SelfAndTarget && source is Character selfCharacter)
                        ActionStatisticsTracker.RecordDamageReceived(selfCharacter, result.Damage);
                    ActionUtilities.CreateAndAddBattleEvent(source, target, selected, result.Damage, totalRoll, result.RollBonus, true, result.IsCombo, 0, 0, result.IsCritical, result.BaseRoll, battleNarrative);
                }
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
                // ACTION cadence: FIFO layers — one layer consumed per hero attack roll (enemy turns do not count).
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
            // FIFO layers (same pipeline as peeked ACCURACY in ActionExecutionFlow / ActionSelector) — not SetTempRollBonus,
            // so the HUD shows a single "Accuracy" line. Enemy debuff uses RollPenalty on the target for their next N attack/spell rolls (N = duration).
            if (Action.DefersSheetCombatPackagesToNextHeroRoll(selected))
            {
                int accTurns = selected.Advanced.RollBonusDuration > 0
                    ? selected.Advanced.RollBonusDuration
                    : (selected.ComboBonusDuration > 0 ? selected.ComboBonusDuration : 1);
                if (accTurns < 1) accTurns = 1;

                int hitLayers = RollModificationManager.GetEffectiveMultiHitCountForModifierScaling(selected, source);
                if (hitLayers < 1) hitLayers = 1;

                if (source is Character heroAcc && !(heroAcc is Enemy) && selected.Advanced.RollBonus != 0)
                {
                    int scaledRollBonus = selected.Advanced.RollBonus * hitLayers;
                    for (int t = 0; t < accTurns; t++)
                    {
                        heroAcc.Effects.AddPendingActionBonusesNextHeroRoll(new List<ActionAttackBonusItem>
                        {
                            new ActionAttackBonusItem { Type = "ACCURACY", Value = scaledRollBonus }
                        });
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
                        int currentBonus = statType switch
                        {
                            "STR" or "STRENGTH" => statBonusCharacter.Stats.TempStrengthBonus,
                            "AGI" or "AGILITY" => statBonusCharacter.Stats.TempAgilityBonus,
                            "TEC" or "TECH" or "TECHNIQUE" => statBonusCharacter.Stats.TempTechniqueBonus,
                            "INT" or "INTELLIGENCE" => statBonusCharacter.Stats.TempIntelligenceBonus,
                            _ => -1
                        };
                        if (currentBonus < 0) continue;
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
                    // (sandbox UX — avoids catalog stuck on slot 1 when attack total is below combo threshold).
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
                    // A completed normal (non-combo) attack breaks the chain — start the combo sequence over
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
            // Critical miss (fumble): ACTION cadence affects the enemy — duration counts down on the enemy's attack rolls, not the hero's.
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
            ActionEventPublisher.PublishActionMiss(source, target, result.SelectedAction!, result.AttackRoll);
            if (source is Character characterMiss)
                ActionStatisticsTracker.RecordMissAction(characterMiss, result.BaseRoll, result.RollBonus);
            if (source is Character comboCharacterMiss)
                comboCharacterMiss.ComboStep = 0;
            ActionUtilities.CreateAndAddBattleEvent(source, target, result.SelectedAction!, 0, result.ModifiedBaseRoll + result.RollBonus, result.RollBonus, false, false, 0, 0, false, result.BaseRoll, battleNarrative);
        }
    }
}

