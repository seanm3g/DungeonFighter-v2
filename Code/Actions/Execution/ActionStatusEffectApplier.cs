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
        /// Applies enemy roll penalty and creates ColoredText message
        /// </summary>
        public static void ApplyEnemyRollPenaltyColored(Action selectedAction, Actor target, List<List<ColoredText>> coloredStatusEffects)
        {
            if (selectedAction == null || target == null || coloredStatusEffects == null) return;
            
            if (selectedAction.EnemyRollPenalty > 0 && target is Enemy targetEnemy)
            {
                targetEnemy.ApplyRollPenalty(selectedAction.EnemyRollPenalty, 1);
                
                var penaltyBuilder = new ColoredTextBuilder();
                penaltyBuilder.Add("    ", Colors.White);
                penaltyBuilder.Add(target.Name, target is Character ? ColorPalette.Player : ColorPalette.Enemy);
                penaltyBuilder.Add(" suffers a -", Colors.White);
                penaltyBuilder.Add(selectedAction.EnemyRollPenalty.ToString(), ColorPalette.Error);
                penaltyBuilder.Add(" roll penalty!", Colors.White);
                coloredStatusEffects.Add(penaltyBuilder.Build());
            }
        }
    }
}

