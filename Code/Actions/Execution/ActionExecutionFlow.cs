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

            // Roll and threshold bonuses apply to this roll based on cadence:
            // ACTION cadence: consumed now (next roll = this roll).
            // ABILITY cadence: applied now to this roll (next ability), consumed only when this ability succeeds (on hit).
            int actionBonusAccumulator = 0, actionBonusHit = 0, actionBonusCombo = 0, actionBonusCrit = 0;
            if (source is Character actionBonusCharacter && !(actionBonusCharacter is Enemy))
            {
                var actionBonuses = actionBonusCharacter.Effects.GetAndConsumeActionBonuses();
                actionBonusCharacter.Effects.AccumulateConsumedModifierBonuses(actionBonuses);
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
            if (actionBonusHit != 0 && source is Character hitBonusCharacter && !(hitBonusCharacter is Enemy))
                RollModificationManager.GetThresholdManager().AdjustHitThreshold(hitBonusCharacter, -actionBonusHit);
            if (actionBonusCombo != 0 && source is Character comboBonusCharacter && !(comboBonusCharacter is Enemy))
                RollModificationManager.GetThresholdManager().AdjustComboThreshold(comboBonusCharacter, -actionBonusCombo);
            if (actionBonusCrit != 0 && source is Character critBonusCharacter && !(critBonusCharacter is Enemy))
                RollModificationManager.GetThresholdManager().AdjustCriticalHitThreshold(critBonusCharacter, -actionBonusCrit);

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
            result.IsCombo = result.AttackRoll >= tm.GetComboThreshold(source);
            result.IsCritical = result.AttackRoll >= tm.GetCriticalHitThreshold(source);
            ActionEventPublisher.PublishActionExecuted(source, target, result.SelectedAction, result.AttackRoll, result.IsCombo, result.IsCritical);
            result.Hit = CombatCalculator.CalculateHit(source, target, result.RollBonus, result.AttackRoll);
        }

        private static void ApplyHitOutcome(Actor source, Actor target, ActionExecutionResult result, BattleNarrative? battleNarrative)
        {
            if (source is Character abilityBonusCharacter && !(abilityBonusCharacter is Enemy))
            {
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
            }
            var hitEvent = ActionEventPublisher.PublishActionHit(
                source, target, result.SelectedAction!, result.AttackRoll, result.IsCombo, result.IsCritical);

            if (result.SelectedAction!.Type == ActionType.Attack || result.SelectedAction.Type == ActionType.Spell)
            {
                double damageMultiplier = ActionUtilities.CalculateDamageMultiplier(source, result.SelectedAction);
                int totalRoll = result.ModifiedBaseRoll + result.RollBonus;
                int multiHitCount = result.SelectedAction.Advanced.MultiHitCount;
                if (source is Character multiHitCharacter && multiHitCharacter.Effects.ConsumedMultiHitMod != 0)
                    multiHitCount = Math.Max(1, multiHitCount + (int)Math.Max(0, multiHitCharacter.Effects.ConsumedMultiHitMod));
                if (multiHitCount > 1)
                {
                    result.Damage = MultiHitProcessor.ProcessMultiHit(
                        source, target, result.SelectedAction, damageMultiplier, totalRoll,
                        result.ModifiedBaseRoll, result.RollBonus, result.BaseRoll, battleNarrative);
                }
                else
                {
                    result.Damage = CombatCalculator.CalculateDamage(source, target, result.SelectedAction, damageMultiplier, 1.0, result.RollBonus, totalRoll);
                    if (result.SelectedAction.Target == TargetType.SelfAndTarget)
                    {
                        ActionUtilities.ApplyDamage(target, result.Damage);
                        ActionUtilities.ApplyDamage(source, result.Damage);
                    }
                    else
                        ActionUtilities.ApplyDamage(target, result.Damage);
                    if (source is Character sourceCharacter)
                        ActionStatisticsTracker.RecordAttackAction(sourceCharacter, totalRoll, result.BaseRoll, result.RollBonus, result.Damage, result.SelectedAction, target as Enemy);
                    if (target is Character targetCharacter)
                        ActionStatisticsTracker.RecordDamageReceived(targetCharacter, result.Damage);
                    if (result.SelectedAction.Target == TargetType.SelfAndTarget && source is Character selfCharacter)
                        ActionStatisticsTracker.RecordDamageReceived(selfCharacter, result.Damage);
                    ActionUtilities.CreateAndAddBattleEvent(source, target, result.SelectedAction, result.Damage, totalRoll, result.RollBonus, true, result.IsCombo, 0, 0, result.IsCritical, result.BaseRoll, battleNarrative);
                }
            }
            else if (result.SelectedAction.Type == ActionType.Heal)
            {
                result.HealAmount = ActionUtilities.CalculateHealAmount(source, result.SelectedAction);
                ActionUtilities.ApplyHealing(target, result.HealAmount);
                if (target is Character targetCharacterHeal)
                    ActionStatisticsTracker.RecordHealingReceived(targetCharacterHeal, result.HealAmount);
                ActionUtilities.CreateAndAddBattleEvent(source, target, result.SelectedAction, 0, result.BaseRoll + result.RollBonus, result.RollBonus, true, result.IsCombo, 0, result.HealAmount, false, result.BaseRoll, battleNarrative);
            }
            else
            {
                ActionUtilities.CreateAndAddBattleEvent(source, target, result.SelectedAction, 0, result.ModifiedBaseRoll + result.RollBonus, result.RollBonus, true, result.IsCombo, 0, 0, false, result.BaseRoll, battleNarrative);
            }

            if (result.SelectedAction.ActionAttackBonuses != null && source is Character bonusSourceCharacter && !(bonusSourceCharacter is Enemy))
                bonusSourceCharacter.Effects.AddActionAttackBonuses(result.SelectedAction.ActionAttackBonuses);
            if (source is Character modSourceCharacter && !(modSourceCharacter is Enemy))
                modSourceCharacter.Effects.AddModifierBonusesFromAction(result.SelectedAction);
            CombatEffectsSimplified.ApplyStatusEffects(result.SelectedAction, source, target, result.StatusEffectMessages, hitEvent);

            if (result.SelectedAction.Name == "FOLLOW THROUGH" && source is Character followThroughCharacter && !(followThroughCharacter is Enemy))
            {
                followThroughCharacter.Effects.NextAttackDamageMultiplier = 3.0;
                var followStatEntries = GetStatBonusEntries(result.SelectedAction);
                var firstFollow = followStatEntries.Count > 0 ? followStatEntries[0] : null;
                if (firstFollow != null && (firstFollow.Value != 0 || !string.IsNullOrEmpty(firstFollow.Type)))
                {
                    followThroughCharacter.Effects.NextAttackStatBonus = firstFollow.Value;
                    followThroughCharacter.Effects.NextAttackStatBonusType = firstFollow.Type;
                    followThroughCharacter.Effects.NextAttackStatBonusDuration = CadenceToStatBonusDuration(result.SelectedAction.Cadence);
                }
            }
            else
            {
                var statEntries = GetStatBonusEntries(result.SelectedAction);
                if (statEntries.Count > 0 && source is Character statBonusCharacter && !(statBonusCharacter is Enemy))
                {
                    int duration = CadenceToStatBonusDuration(result.SelectedAction.Cadence);
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
                    ActionEventPublisher.PublishEnemyDeath(source, target, result.SelectedAction, result.Damage);
                else
                {
                    double healthPercentage = (double)enemyTarget.CurrentHealth / enemyTarget.MaxHealth;
                    var thresholds = GetHealthThresholds(result.SelectedAction);
                    var candidates = thresholds.Where(t => t >= healthPercentage).OrderBy(t => t).ToList();
                    if (candidates.Count > 0)
                        ActionEventPublisher.PublishHealthThreshold(source, target, result.SelectedAction, candidates[0]);
                }
            }

            if (source is Character comboCharacter && !(comboCharacter is Enemy))
            {
                int comboThreshold = GameConfiguration.Instance.RollSystem.ComboThreshold.Min;
                // Only advance combo step when the executed action was a combo action (prevents skipping slots on normal attacks)
                if (result.SelectedAction.IsComboAction)
                {
                    if (comboCharacter.ComboStep == 0)
                    {
                        if (result.AttackRoll >= comboThreshold)
                            comboCharacter.IncrementComboStep(result.SelectedAction);
                    }
                    else if (comboCharacter.ComboStep == 1)
                    {
                        if (result.AttackRoll >= comboThreshold)
                            comboCharacter.IncrementComboStep(result.SelectedAction);
                        else
                            comboCharacter.ComboStep = 0;
                    }
                    else if (comboCharacter.ComboStep >= 2)
                    {
                        if (result.AttackRoll >= comboThreshold)
                            comboCharacter.IncrementComboStep(result.SelectedAction);
                        else
                            comboCharacter.ComboStep = 0;
                    }
                }
            }
        }

        private static void ApplyMissOutcome(Actor source, Actor target, ActionExecutionResult result, BattleNarrative? battleNarrative)
        {
            if (source is Character modMissCharacter && !(modMissCharacter is Enemy))
                modMissCharacter.Effects.AddModifierBonusesFromAction(result.SelectedAction!);
            ActionEventPublisher.PublishActionMiss(source, target, result.SelectedAction!, result.AttackRoll);
            if (source is Character characterMiss)
                ActionStatisticsTracker.RecordMissAction(characterMiss, result.BaseRoll, result.RollBonus);
            if (source is Character comboCharacterMiss && !(comboCharacterMiss is Enemy))
                comboCharacterMiss.ComboStep = 0;
            ActionUtilities.CreateAndAddBattleEvent(source, target, result.SelectedAction!, 0, result.ModifiedBaseRoll + result.RollBonus, result.RollBonus, false, false, 0, 0, false, result.BaseRoll, battleNarrative);
        }
    }
}

