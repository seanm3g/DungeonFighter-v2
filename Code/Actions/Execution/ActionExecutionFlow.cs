using RPGGame;
using RPGGame.ActionInteractionLab;
using RPGGame.Actions.Conditional;
using RPGGame.Actions.RollModification;
using RPGGame.Combat.Events;
using RPGGame.Diagnostics;
using RPGGame.UI.Avalonia.Feedback;
using RPGGame.UI.Avalonia.Layout;
using RPGGame.Utils;
using RPGGame.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RPGGame.Actions.Execution
{
    /// <summary>
    /// Handles the core action execution flow
    /// Manages action selection, roll calculation, hit/miss determination, and damage/healing application
    /// </summary>
    internal static partial class ActionExecutionFlow
    {
        internal static event System.Action? OneShotKillOccurred;

        /// <summary>Nested retrigger depth (0 = outer swing). Retrigger swings do not schedule further retriggers.</summary>
        [ThreadStatic]
        private static int RetriggerDepth;

        internal static void NotifyOneShotKillOccurred() => OneShotKillOccurred?.Invoke();

        private static List<ActionAttackBonusItem> CloneActionAttackBonusItems(List<ActionAttackBonusItem>? src)
        {
            if (src == null || src.Count == 0) return new List<ActionAttackBonusItem>();
            return src.Select(b => new ActionAttackBonusItem { Type = b.Type, Value = b.Value }).ToList();
        }

        private static int ReadTempStatBonusForResolvedCode(Character character, string concreteCode) =>
            concreteCode switch
            {
                DynamicAttributeCategoryResolver.CodeStrength => character.Stats.TempStrengthBonus,
                DynamicAttributeCategoryResolver.CodeAgility => character.Stats.TempAgilityBonus,
                DynamicAttributeCategoryResolver.CodeTechnique => character.Stats.TempTechniqueBonus,
                DynamicAttributeCategoryResolver.CodeIntelligence => character.Stats.TempIntelligenceBonus,
                _ => 0
            };

        /// <summary>Applies STR/AGI/TEC/INT items from cadence bonus lists (TURN consumed, ACTION bank redeemed, etc.).</summary>
        private static void ApplyStatBonusesFromCadenceItems(Character hero, IEnumerable<ActionAttackBonusItem>? bonuses, int duration = 999)
        {
            if (bonuses == null) return;
            foreach (var bonus in bonuses)
            {
                string bonusType = (bonus.Type ?? "").ToUpper();
                if (!DynamicAttributeCategoryResolver.IsStatOrDynamicCategoryType(bonusType)) continue;
                string concrete = DynamicAttributeCategoryResolver.ResolveStatBonusTypeToConcreteCode(hero, bonusType);
                if (concrete != DynamicAttributeCategoryResolver.CodeStrength && concrete != DynamicAttributeCategoryResolver.CodeAgility
                    && concrete != DynamicAttributeCategoryResolver.CodeTechnique && concrete != DynamicAttributeCategoryResolver.CodeIntelligence)
                    continue;
                int currentBonus = ReadTempStatBonusForResolvedCode(hero, concrete);
                hero.ApplyStatBonus(currentBonus + (int)bonus.Value, bonusType, duration);
            }
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

        /// <summary>Stat bonuses on TURN/ACTION cadence actions are queued via <see cref="ActionAttackBonuses"/>, not applied on the granting hit.</summary>
        private static bool DefersStatBonusToCadenceQueue(Action? action)
        {
            if (action == null) return false;
            string cadence = CadenceKeywords.Normalize(action.Cadence);
            return CadenceKeywords.IsTurn(cadence) || CadenceKeywords.IsAction(cadence);
        }

        internal readonly struct TempStatSnapshot
        {
            public int Str { get; init; }
            public int Agi { get; init; }
            public int Tec { get; init; }
            public int Int { get; init; }
            public int Turns { get; init; }

            public static TempStatSnapshot Capture(Character character) => new()
            {
                Str = character.Stats.TempStrengthBonus,
                Agi = character.Stats.TempAgilityBonus,
                Tec = character.Stats.TempTechniqueBonus,
                Int = character.Stats.TempIntelligenceBonus,
                Turns = character.Stats.TempStatBonusTurns
            };

            public static void Restore(Character character, TempStatSnapshot snapshot)
            {
                character.Stats.TempStrengthBonus = snapshot.Str;
                character.Stats.TempAgilityBonus = snapshot.Agi;
                character.Stats.TempTechniqueBonus = snapshot.Tec;
                character.Stats.TempIntelligenceBonus = snapshot.Int;
                character.Stats.TempStatBonusTurns = snapshot.Turns;
            }
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
            IDictionary<Actor, Action> lastUsedActions,
            IDictionary<Actor, bool> lastCriticalMissStatus)
        {
            var sw = CombatHotPathMetrics.IsEnabled ? Stopwatch.StartNew() : null;

            var result = new ActionExecutionResult();
            if (source is Character tempDecayCharacter)
                tempDecayCharacter.UpdateTempEffects(Character.DEFAULT_ACTION_LENGTH);
            ApplyPreRollBonuses(source);
            SelectActionAndResolveRoll(source, target, result, lastUsedActions, lastCriticalMissStatus, forcedAction);
            if (result.SelectedAction == null)
            {
                if (sw != null)
                {
                    sw.Stop();
                    CombatHotPathMetrics.RecordActionExecutionFlow(sw.Elapsed);
                }
                return result;
            }

            Actor combatTarget = target;
            if (target != null)
            {
                combatTarget = ActionEffectTargetResolver.ResolveConfusedCombatTarget(
                    result.SelectedAction, source, target);
                result.EffectiveTarget = combatTarget;
            }

            TriggerThresholdBarFeedback(source, result);

            Character? heroForStripFeedback = null;
            int? stripIndexForFeedback = null;
            if (source is Character heroStrip && heroStrip is not Enemy)
            {
                heroForStripFeedback = heroStrip;
                var comboForFeedback = ActionUtilities.GetComboActions(heroStrip);
                if (comboForFeedback.Count > 0)
                    stripIndexForFeedback = heroStrip.ComboStep % comboForFeedback.Count;
            }

            if (result.Hit)
                ApplyHitOutcome(source, combatTarget, result, battleNarrative);
            else
                ApplyMissOutcome(source, combatTarget, result, battleNarrative);

            // Nested strip retrigger (max depth 1) — distinct from Multihit damage ticks.
            // Nested HP damage is applied inside the nested Execute; keep outer Damage as this swing only
            // so the combat log headline stays correct. Nested lines are formatted separately.
            if (RetriggerDepth == 0
                && result.Hit
                && RetriggerScheduler.TryConsume(source, out Action? retriggerAction)
                && retriggerAction != null)
            {
                RetriggerDepth++;
                RetriggerScheduler.AllowScheduling = false;
                Action? outerLastUsed = result.SelectedAction;
                try
                {
                    var nestedTarget = target ?? combatTarget;
                    var nested = Execute(
                        source,
                        nestedTarget!,
                        environment,
                        lastPlayerAction,
                        retriggerAction,
                        battleNarrative,
                        lastUsedActions,
                        lastCriticalMissStatus);
                    result.NestedRetriggerResults.Add(nested);
                    // Nested status lines are formatted with the nested hit/miss block in ActionExecutor
                    // (not merged here) so "prepares a retrigger" stays above the encore swing.
                }
                finally
                {
                    // Turn timing / last-action memory should stay on the outer swing, not the encore.
                    if (outerLastUsed != null)
                        lastUsedActions[source] = outerLastUsed;
                    RetriggerScheduler.AllowScheduling = true;
                    RetriggerDepth--;
                }
            }

            // Redeemed DAMAGE/SPEED/MULTIHIT/AMP mods are applied during this swing only.
            // Clear them before the next strip paint so cards do not keep showing spent ACTION bonuses.
            if (source is Character clearConsumedAfterSwing)
                clearConsumedAfterSwing.Effects.ClearConsumedModifierBonuses();

            if (stripIndexForFeedback.HasValue && heroForStripFeedback != null)
            {
                HeroActionStripFlashKind flashKind;
                if (!result.Hit)
                    flashKind = HeroActionStripFlashKind.Miss;
                else if (ShouldFlashComboComplete(heroForStripFeedback, stripIndexForFeedback.Value, result))
                    flashKind = HeroActionStripFlashKind.ComboComplete;
                else
                    flashKind = HeroActionStripFlashKind.Hit;
                HeroActionStripFeedback.Trigger(stripIndexForFeedback.Value, flashKind);
            }

            source.ConsumeRollPenaltyAfterCombatRoll(result.SelectedAction);
            source.ConsumeConfusionAfterCombatAction(result.SelectedAction);

            if (sw != null)
            {
                sw.Stop();
                CombatHotPathMetrics.RecordActionExecutionFlow(sw.Elapsed);
            }

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
                var (nextBonus, nextStatType, nextDuration) = nextAttackStatCharacter.Effects.ConsumeNextTurnStatBonus();
                if (nextBonus == 0 || string.IsNullOrEmpty(nextStatType)) return;
                string statType = nextStatType!.ToUpper();
                if (!DynamicAttributeCategoryResolver.IsStatOrDynamicCategoryType(statType))
                    return;
                string concrete = DynamicAttributeCategoryResolver.ResolveStatBonusTypeToConcreteCode(nextAttackStatCharacter, statType);
                if (concrete != DynamicAttributeCategoryResolver.CodeStrength && concrete != DynamicAttributeCategoryResolver.CodeAgility
                    && concrete != DynamicAttributeCategoryResolver.CodeTechnique && concrete != DynamicAttributeCategoryResolver.CodeIntelligence)
                    return;
                int currentBonus = ReadTempStatBonusForResolvedCode(nextAttackStatCharacter, concrete);
                int newBonus = currentBonus + nextBonus;
                int duration = nextDuration > 0 ? nextDuration : 999;
                nextAttackStatCharacter.ApplyStatBonus(newBonus, statType, duration);
            }
        }

        private static void TriggerThresholdBarFeedback(Actor source, ActionExecutionResult result)
        {
            var thresholdManager = RollModificationManager.GetThresholdManager();
            int segmentIndex = ThresholdDisplayFormatting.FindSegmentIndexForRoll(
                result.BaseRoll,
                thresholdManager.GetCriticalHitThreshold(source),
                thresholdManager.GetComboThreshold(source),
                thresholdManager.GetHitThreshold(source) + 1,
                thresholdManager.GetCriticalMissThreshold(source));
            if (segmentIndex < 0)
                return;

            var panel = source is Enemy ? ThresholdBarPanel.Enemy : ThresholdBarPanel.Hero;
            ThresholdBarFeedback.Trigger(panel, segmentIndex, result.BaseRoll);
        }
    }
}