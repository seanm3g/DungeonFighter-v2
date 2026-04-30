using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;
using RPGGame.UI.ColorSystem;
using Avalonia.Media;

namespace RPGGame.Actions.Execution
{
    /// <summary>
    /// Converts status-effect log lines to ColoredText for the combat UI.
    /// </summary>
    public static class ActionStatusEffectApplier
    {
        /// <summary>
        /// Appends markup lines already collected on the action execution result (<c>StatusEffectMessages</c>) as ColoredText blocks.
        /// Does not call <see cref="CombatEffectsSimplified.ApplyStatusEffects"/> — <see cref="ActionExecutionFlow"/> already applied effects once on hit.
        /// </summary>
        public static void AppendColoredStatusEffectMessages(IEnumerable<string>? statusEffectMessages, List<List<ColoredText>> coloredStatusEffects)
        {
            if (statusEffectMessages == null || coloredStatusEffects == null) return;

            foreach (var statusString in statusEffectMessages)
            {
                if (string.IsNullOrEmpty(statusString)) continue;
                var statusColored = ColoredTextParser.Parse(statusString);
                if (statusColored.Count > 0)
                    coloredStatusEffects.Add(statusColored);
            }
        }
        
        /// <summary>
        /// Enemy roll penalty removed; method kept for API compatibility (no-op).
        /// </summary>
        public static void ApplyEnemyRollPenaltyColored(Action selectedAction, Actor target, List<List<ColoredText>> coloredStatusEffects)
        {
            // No-op: EnemyRollPenalty feature removed
        }
        
        /// <summary>
        /// Applies stat bonus messages (one per entry). Uses StatBonuses list if non-empty, else legacy single StatBonus/StatBonusType.
        /// </summary>
        public static void ApplyStatBonusColored(Action selectedAction, Actor source, List<List<ColoredText>> coloredStatusEffects)
        {
            if (selectedAction == null || source == null || coloredStatusEffects == null) return;
            if (!(source is Character statBonusCharacter) || source is Enemy) return;

            var entries = GetStatBonusEntries(selectedAction);
            foreach (var entry in entries)
            {
                if (entry.Value == 0 && string.IsNullOrEmpty(entry.Type)) continue;
                string statType = (entry.Type ?? "").ToUpper();
                if (string.IsNullOrEmpty(statType)) continue;
                string statName = statType switch
                {
                    "STR" or "STRENGTH" => "Strength",
                    "AGI" or "AGILITY" => "Agility",
                    "TEC" or "TECH" or "TECHNIQUE" => "Technique",
                    "INT" or "INTELLIGENCE" => "Intelligence",
                    _ => statType
                };
                var statBonusBuilder = new ColoredTextBuilder();
                statBonusBuilder.Add("     (", Colors.White);
                statBonusBuilder.Add(statName, ColorPalette.Success);
                statBonusBuilder.Add(" increased by ", Colors.White);
                statBonusBuilder.Add(entry.Value.ToString(), ColorPalette.Success);
                statBonusBuilder.Add("!)", Colors.White);
                coloredStatusEffects.Add(statBonusBuilder.Build());
            }
        }

        /// <summary>
        /// Displays action modifier bonuses (SPEED_MOD / DAMAGE_MOD / MULTIHIT_MOD / AMP_MOD) immediately when queued by an action.
        /// These bonuses are consumed on the next roll/attack as normal; this is UI-only visibility so players see the queue right away.
        /// </summary>
        public static void AppendQueuedActionModMessages(Action selectedAction, Actor source, Actor target, bool actionHit, bool actionWasComboSuccess, List<List<ColoredText>> coloredStatusEffects)
        {
            if (selectedAction == null || source == null || target == null || coloredStatusEffects == null)
                return;

            bool isAbilityCadence = string.Equals((selectedAction.Cadence ?? "").Trim(), "Ability", StringComparison.OrdinalIgnoreCase);
            // Mirrors ActionExecutionFlow: on miss, ability-cadence modifier bonuses do not apply/queue.
            if (!actionHit && isAbilityCadence)
                return;

            bool sourceUsesEnemySpreadsheetMods = source is Enemy;
            var sourceBonuses = BuildModifierBonusesForQueuedDisplay(selectedAction, useEnemySpreadsheetMods: sourceUsesEnemySpreadsheetMods);
            if (sourceBonuses.Count > 0)
            {
                // Mirrors ActionExecutionFlow: ability-cadence on a successful combo hit routes to "next combo slot" pending instead of the ability queue.
                bool routedToNextComboSlot = !sourceUsesEnemySpreadsheetMods
                    && isAbilityCadence
                    && actionWasComboSuccess
                    && source is Character;

                string label = routedToNextComboSlot ? "Next action" : (isAbilityCadence ? "Next ability" : "Next attack");
                AddQueuedModsLine(coloredStatusEffects, label, sourceBonuses);
            }

            // Also show enemy-facing queued mods when a hero action applies enemy spreadsheet mods (AD–AG) and the target is an enemy.
            // These are queued onto the enemy (and consumed on their next roll).
            if (source is Character && source is not Enemy)
            {
                var enemyTargetBonuses = BuildModifierBonusesForQueuedDisplay(selectedAction, useEnemySpreadsheetMods: true);
                if (enemyTargetBonuses.Count > 0 && target is Enemy)
                {
                    AddQueuedModsLine(coloredStatusEffects, "Enemy next attack", enemyTargetBonuses);
                }
            }
        }

        private static List<StatBonusEntry> GetStatBonusEntries(Action action)
        {
            if (action?.Advanced == null) return new List<StatBonusEntry>();
            if (action.Advanced.StatBonuses != null && action.Advanced.StatBonuses.Count > 0)
                return action.Advanced.StatBonuses;
            if (action.Advanced.StatBonus != 0 || !string.IsNullOrEmpty(action.Advanced.StatBonusType))
                return new List<StatBonusEntry> { new StatBonusEntry { Value = action.Advanced.StatBonus, Type = action.Advanced.StatBonusType ?? "" } };
            return new List<StatBonusEntry>();
        }

        private static List<ActionAttackBonusItem> BuildModifierBonusesForQueuedDisplay(Action action, bool useEnemySpreadsheetMods)
        {
            // Reuse the same parsing and percent/value semantics as the execution pipeline.
            // This intentionally duplicates the queued bonuses rather than reading effect state, so it stays stable even if the queue is consumed later in the same frame.
            var bonuses = CharacterEffectsState.BuildModifierBonusesFromActionFields(action, useEnemySpreadsheetMods);
            return bonuses ?? new List<ActionAttackBonusItem>();
        }

        private static void AddQueuedModsLine(List<List<ColoredText>> coloredStatusEffects, string label, List<ActionAttackBonusItem> bonuses)
        {
            string desc = FormatBonusItemsShort(bonuses);
            if (string.IsNullOrWhiteSpace(desc))
                return;

            var builder = new ColoredTextBuilder();
            builder.Add("     (", Colors.White);
            builder.Add(label, ColorPalette.Info);
            builder.Add(":", Colors.White);
            builder.AddSpace();
            builder.Add(desc, ColorPalette.Success);
            builder.Add(")", Colors.White);
            coloredStatusEffects.Add(builder.Build());
        }

        private static string FormatBonusItemsShort(List<ActionAttackBonusItem> bonuses)
        {
            if (bonuses == null || bonuses.Count == 0) return "";
            var parts = new List<string>();
            foreach (var b in bonuses)
            {
                string part = FormatBonusItemShort(b);
                if (!string.IsNullOrEmpty(part)) parts.Add(part);
            }
            return string.Join(", ", parts);
        }

        private static string FormatBonusItemShort(ActionAttackBonusItem b)
        {
            if (b == null) return "";
            string type = (b.Type ?? "").ToUpperInvariant();
            // Modifier bonuses are stored as "percent points" for *_MOD percent types (e.g. 20 => +20%).
            string sign = b.Value >= 0 ? "+" : "";
            return type switch
            {
                "DAMAGE_MOD" when b.Value >= 0 => b.Value >= 100 ? $"{(b.Value / 100.0):0.#}x DMG" : $"{sign}{b.Value:0}% DMG",
                "DAMAGE_MOD" => $"{b.Value:0}% DMG",
                "SPEED_MOD" => $"{sign}{b.Value:0}% SPD",
                "MULTIHIT_MOD" => $"{sign}{b.Value:0} MH",
                "AMP_MOD" => $"{sign}{b.Value:0}% AMP",
                _ => $"{sign}{b.Value:0} {type}"
            };
        }
    }
}

