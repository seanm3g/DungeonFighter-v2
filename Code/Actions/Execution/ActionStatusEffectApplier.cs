using System;
using System.Collections.Generic;
using RPGGame.Combat;
using RPGGame.UI.ColorSystem;
using Avalonia.Media;

namespace RPGGame.Actions.Execution
{
    /// <summary>
    /// Applies status effects from actions and converts them to ColoredText
    /// </summary>
    public static class ActionStatusEffectApplier
    {
        /// <summary>
        /// Applies status effects from an action and converts them to ColoredText
        /// </summary>
        public static void ApplyStatusEffectsColored(Action selectedAction, Actor source, Actor target, List<List<ColoredText>> coloredStatusEffects)
        {
            if (selectedAction == null || source == null || target == null || coloredStatusEffects == null) return;
            
            var stringResults = new List<string>();
            CombatEffectsSimplified.ApplyStatusEffects(selectedAction, source, target, stringResults);
            
            // Convert status effect strings to ColoredText
            foreach (var statusString in stringResults)
            {
                if (!string.IsNullOrEmpty(statusString))
                {
                    var statusColored = ColoredTextParser.Parse(statusString);
                    if (statusColored.Count > 0)
                    {
                        coloredStatusEffects.Add(statusColored);
                    }
                }
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

        private static List<StatBonusEntry> GetStatBonusEntries(Action action)
        {
            if (action?.Advanced == null) return new List<StatBonusEntry>();
            if (action.Advanced.StatBonuses != null && action.Advanced.StatBonuses.Count > 0)
                return action.Advanced.StatBonuses;
            if (action.Advanced.StatBonus != 0 || !string.IsNullOrEmpty(action.Advanced.StatBonusType))
                return new List<StatBonusEntry> { new StatBonusEntry { Value = action.Advanced.StatBonus, Type = action.Advanced.StatBonusType ?? "" } };
            return new List<StatBonusEntry>();
        }
    }
}

