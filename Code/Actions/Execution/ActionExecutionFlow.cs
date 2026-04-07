using RPGGame;
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
            return result;
        }

        private static void ApplyPreRollBonuses(Actor source)
        {
            if (source is Character clearModCharacter && !(clearModCharacter is Enemy))
                clearModCharacter.Effects.ClearConsumedModifierBonuses();
            if (source is Character nextAttackStatCharacter && !(nextAttackStatCharacter is Enemy))
            {
                var (nextBonus, nextStatType, nextDuration) = nextAttackStatCharacter.Effects.ConsumeNextAttackStatBonus();
                if (nextBonus == 0 || string.IsNullOrEmpty(nextStatType)) return;
                string statType = nextStatType!.ToUpper();
                int currentBonus = statType switch
                {
                    "STR" => nextAttackStatCharacter.Stats.TempStrengthBonus,
                    "AGI" => nextAttackStatCharacter.Stats.TempAgilityBonus,
                    "TEC" => nextAttackStatCharacter.Stats.TempTechniqueBonus,
                    "INT" => nextAttackStatCharacter.Stats.TempIntelligenceBonus,
                    _ => 0
                };
                int newBonus = currentBonus + nextBonus;
                int duration = nextDuration > 0 ? nextDuration : 999;
                nextAttackStatCharacter.ApplyStatBonus(newBonus, statType, duration);
            }
        }

        private static void SelectActionAndResolveRoll(
            Actor source,
            Actor target,
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

            // Roll and threshold bonuses: ATTACK (consumed per roll), per-slot ACTION (consumed when slot executes), ABILITY (consumed on hit)
            int actionBonusAccumulator = 0, actionBonusHit = 0, actionBonusCombo = 0, actionBonusCrit = 0;
            if (source is Character actionBonusCharacter && !(actionBonusCharacter is Enemy))
            {
                // 1. Consume per-slot ACTION bonuses for current slot (slot-based; consumed when this slot executes)
                var comboActions = ActionUtilities.GetComboActions(actionBonusCharacter);
                int comboLength = comboActions.Count;
                if (comboLength > 0)
                {
                    int currentSlot = actionBonusCharacter.ComboStep % comboLength;
                    var slotBonuses = actionBonusCharacter.Effects.ConsumePendingActionBonusesForSlot(currentSlot);
                    actionBonusCharacter.Effects.AccumulateConsumedModifierBonuses(slotBonuses);
                    foreach (var bonus in slotBonuses)
                    {
                        switch ((bonus.Type ?? "").ToUpper())
                        {
                            case "ACCURACY": actionBonusAccumulator += (int)bonus.Value; break;
                            case "HIT": actionBonusHit += (int)bonus.Value; break;
                            case "COMBO": actionBonusCombo += (int)bonus.Value; break;
                            case "CRIT": actionBonusCrit += (int)bonus.Value; break;
                        }
                    }
                }
                // 2. Consume ATTACK bonuses (roll-based; consumed per roll, apply only on hit for stat bonuses)
                var actionBonuses = actionBonusCharacter.Effects.GetAndConsumeAttackBonuses();
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
                    }
                }
                // Apply ability-queued roll/threshold bonuses to this roll (consumed on hit in ApplyHitOutcome).
                var abilityBonusesPeek = actionBonusCharacter.Effects.PeekAbilityBonuses();
                foreach (var bonus in abilityBonusesPeek)
                {
                    switch (bonus.Type.ToUpper())
                    {
                        case "ACCURACY": actionBonusAccumulator += (int)bonus.Value; break;
                        case "HIT": actionBonusHit += (int)bonus.Value; break;
                        case "COMBO": actionBonusCombo += (int)bonus.Value; break;
                        case "CRIT": actionBonusCrit += (int)bonus.Value; break;
                    }
                }
            }
            result.ModifiedBaseRoll = RollModificationManager.ApplyActionRollModifications(
                result.BaseRoll, result.SelectedAction, source, target);
            result.ModifiedBaseRoll += actionBonusAccumulator;
            RollModificationManager.ApplyThresholdOverrides(result.SelectedAction, source, target);
            // Positive bonus = easier (lower threshold). ThresholdManager subtracts adjustment for Hit/Combo/Crit.
            if (actionBonusHit != 0 && source is Character hitBonusCharacter && !(hitBonusCharacter is Enemy))
                RollModificationManager.GetThresholdManager().AdjustHitThreshold(hitBonusCharacter, actionBonusHit);
            if (actionBonusCombo != 0 && source is Character comboBonusCharacter && !(comboBonusCharacter is Enemy))
                RollModificationManager.GetThresholdManager().AdjustComboThreshold(comboBonusCharacter, actionBonusCombo);
            if (actionBonusCrit != 0 && source is Character critBonusCharacter && !(critBonusCharacter is Enemy))
                RollModificationManager.GetThresholdManager().AdjustCriticalHitThreshold(critBonusCharacter, actionBonusCrit);

            result.RollBonus = ActionUtilities.CalculateRollBonus(source, result.SelectedAction);
            result.AttackRoll = result.ModifiedBaseRoll + result.RollBonus;
            var tm = RollModificationManager.GetThresholdManager();
            int hitThreshold = tm.GetHitThreshold(source);
            int criticalMissThreshold = tm.GetCriticalMissThreshold(source);
            // Critical miss only when roll is both <= crit-miss threshold and would be a miss (roll <= hit threshold). So +5 to HIT (threshold 0) makes 1+ a hit and crit miss impossible (would need 0).
            result.IsCriticalMiss = result.BaseRoll <= criticalMissThreshold && result.BaseRoll <= hitThreshold;
            if (result.IsCriticalMiss)
            {
                source.HasCriticalMissPenalty = true;
                source.CriticalMissPenaltyTurns = 1;
            }
            lastCriticalMissStatus[source] = result.IsCriticalMiss;
            // Combo flag: combo-slot action and attack total meets combo threshold (avoids "combo" on unnamed normal hits that hit 14+ total)
            result.IsCombo = result.SelectedAction.IsComboAction && result.AttackRoll >= tm.GetComboThreshold(source);
            result.IsCritical = result.AttackRoll >= tm.GetCriticalHitThreshold(source);
            ActionEventPublisher.PublishActionExecuted(source, target, result.SelectedAction, result.AttackRoll, result.IsCombo, result.IsCritical);
            result.Hit = CombatCalculator.CalculateHit(source, target, result.RollBonus, result.AttackRoll);
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
                if (multiHitCount > 1)
                {
                    result.Damage = MultiHitProcessor.ProcessMultiHit(
                        source, target, selected, damageMultiplier, totalRoll,
                        result.ModifiedBaseRoll, result.RollBonus, result.BaseRoll, battleNarrative);
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
                // Add ACTION cadence bonuses to the next combo slot. Spreadsheet Count = "for next X ACTION(s)";
                // we apply Count copies onto that single upcoming slot (one roll consumes all lists in that slot).
                if (result.IsCombo && selected.IsComboAction)
                {
                    var comboActions = ActionUtilities.GetComboActions(bonusSourceCharacter);
                    int comboLength = comboActions.Count;
                    if (comboLength > 0)
                    {
                        int nextSlot = (bonusSourceCharacter.ComboStep + 1) % comboLength;
                        foreach (var group in selected.ActionAttackBonuses.BonusGroups)
                        {
                            var ct = string.IsNullOrEmpty(group.CadenceType) ? group.Keyword : group.CadenceType;
                            if (ct != "ACTION" || group.Bonuses == null) continue;
                            for (int i = 0; i < group.Count; i++)
                                bonusSourceCharacter.Effects.AddPendingActionBonuses(nextSlot, group.Bonuses);
                        }
                    }
                }
            }
            // Hand-edited JSON: rollBonus without ACTION bonus group — defer via temp roll bonus for next execution
            if (source is Character actionCadenceTempCharacter && !(actionCadenceTempCharacter is Enemy)
                && Action.IsActionCadenceRollDeferral(selected)
                && selected.Advanced.RollBonus != 0
                && !Action.HasActionCadenceBonusGroup(selected)
                && result.IsCombo
                && selected.IsComboAction)
            {
                int rollBonusDuration = selected.Advanced.RollBonusDuration > 0
                    ? selected.Advanced.RollBonusDuration
                    : 1;
                actionCadenceTempCharacter.Effects.SetTempRollBonus(selected.Advanced.RollBonus, rollBonusDuration);
            }
            if (source is Character modSourceCharacter && !(modSourceCharacter is Enemy))
            {
                int? nextSlotForAbilityMod = null;
                if (result.IsCombo && selected.IsComboAction
                    && string.Equals(selected.Cadence?.Trim(), "Ability", StringComparison.OrdinalIgnoreCase))
                {
                    var comboActions = ActionUtilities.GetComboActions(modSourceCharacter);
                    if (comboActions.Count > 0)
                        nextSlotForAbilityMod = (modSourceCharacter.ComboStep + 1) % comboActions.Count;
                }
                modSourceCharacter.Effects.AddModifierBonusesFromAction(selected, nextSlotForAbilityMod);
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

            if (source is Character comboCharacter && !(comboCharacter is Enemy))
            {
                int comboThreshold = GameConfiguration.Instance.RollSystem.ComboThreshold.Min;
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
                    modMissCharacter.Effects.AddModifierBonusesFromAction(result.SelectedAction);
                modMissCharacter.Effects.GetAndClearConsumedAttackBonusesThisRoll(); // Discard; ATTACK bonuses consumed on roll but not applied on miss
            }
            ActionEventPublisher.PublishActionMiss(source, target, result.SelectedAction!, result.AttackRoll);
            if (source is Character characterMiss)
                ActionStatisticsTracker.RecordMissAction(characterMiss, result.BaseRoll, result.RollBonus);
            if (source is Character comboCharacterMiss && !(comboCharacterMiss is Enemy))
                comboCharacterMiss.ComboStep = 0;
            ActionUtilities.CreateAndAddBattleEvent(source, target, result.SelectedAction!, 0, result.ModifiedBaseRoll + result.RollBonus, result.RollBonus, false, false, 0, 0, false, result.BaseRoll, battleNarrative);
        }
    }
}

