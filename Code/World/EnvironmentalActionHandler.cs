using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame
{
    /// <summary>
    /// Handles environmental action execution and area-of-effect actions
    /// </summary>
    public static class EnvironmentalActionHandler
    {
        /// <summary>
        /// Executes an area of effect action
        /// </summary>
        /// <param name="source">The Actor performing the action</param>
        /// <param name="targets">List of target entities</param>
        /// <param name="environment">The environment affecting the action</param>
        /// <param name="selectedAction">The action to perform (optional)</param>
        /// <param name="battleNarrative">The battle narrative to add events to</param>
        /// <returns>A string describing the results</returns>
        public static string ExecuteAreaOfEffectAction(Actor source, List<Actor> targets, Environment? environment = null, Action? selectedAction = null, BattleNarrative? battleNarrative = null)
        {
            var results = new List<string>();
            
            // Get the action to use
            var action = selectedAction ?? source.SelectAction();
            if (action == null)
            {
                return $"{source.Name} has no actions available.";
            }
            
            // For environmental actions, use special duration-based system
            if (source is Environment)
            {
                return ExecuteEnvironmentalAction(source, targets, action, environment);
            }
            
            // Execute the action on each target (for non-environmental actions)
            foreach (var target in targets)
            {
                bool isAlive = false;
                if (target is Character targetCharacter)
                    isAlive = targetCharacter.CurrentHealth > 0;
                else if (target is Enemy targetEnemy)
                    isAlive = targetEnemy.CurrentHealth > 0;
                
                if (isAlive)
                {
                    string result = ActionExecutor.ExecuteAction(source, target, environment, null, null, battleNarrative);
                    if (!string.IsNullOrEmpty(result))
                    {
                        results.Add(result);
                    }
                }
            }
            
            return string.Join("\n", results);
        }

        /// <summary>
        /// Executes an environmental action with duration-based effects
        /// </summary>
        /// <param name="source">The environment performing the action</param>
        /// <param name="targets">List of target entities</param>
        /// <param name="action">The action to perform</param>
        /// <param name="environment">The environment context</param>
        /// <returns>A string describing the results</returns>
        private static string ExecuteEnvironmentalAction(Actor source, List<Actor> targets, Action action, Environment? environment = null)
        {
            // Get list of alive targets
            var aliveTargets = new List<Actor>();
            foreach (var target in targets)
            {
                bool isAlive = false;
                if (target is Character targetCharacter)
                    isAlive = targetCharacter.CurrentHealth > 0;
                else if (target is Enemy targetEnemy)
                    isAlive = targetEnemy.CurrentHealth > 0;
                
                if (isAlive)
                {
                    aliveTargets.Add(target);
                }
            }
            
            if (aliveTargets.Count == 0)
            {
                var builder = new ColoredTextBuilder();
                builder.Add(source.Name, ColorPalette.Green);
                builder.Add("'s", Colors.White); // Apostrophe without trailing space - spacing will be handled automatically
                builder.AddSpace(); // Explicit space between "'s" and action name
                builder.Add(action.Name, ColorPalette.Success);
                builder.Add(" has no effect!", Colors.White);
                return ColoredTextRenderer.RenderAsMarkup(builder.Build());
            }
            
            // Roll separately for each target and track which ones are affected
            var affectedTargets = new List<(Actor target, int duration)>();
            
            foreach (var target in aliveTargets)
            {
                // Roll 2d2-2 to determine duration (0-2 turns) for this specific target
                int duration = Dice.Roll(1, 2) + Dice.Roll(1, 2) - 2;
                
                // If duration is 0, the effect is not applied to this target
                if (duration > 0)
                {
                    ApplyEnvironmentalEffectSilent(source, target, action, duration);
                    affectedTargets.Add((target, duration));
                }
            }
            
            // If no targets were affected, show no effect message
            if (affectedTargets.Count == 0)
            {
                var builder = new ColoredTextBuilder();
                builder.Add(source.Name, ColorPalette.Green);
                builder.Add("'s", Colors.White); // Apostrophe without trailing space - spacing will be handled automatically
                builder.AddSpace(); // Explicit space between "'s" and action name
                builder.Add(action.Name, ColorPalette.Success);
                builder.Add(" has no effect!", Colors.White);
                return ColoredTextRenderer.RenderAsMarkup(builder.Build());
            }
            
            // Format the message based on number of affected targets
            if (affectedTargets.Count == 1)
            {
                // Single target affected - use the original format
                var (target, duration) = affectedTargets[0];
                return FormatEnvironmentalEffectMessage(source, target, action, duration);
            }
            else
            {
                // Multiple targets affected - format as area of effect with individual results
                var mainBuilder = new ColoredTextBuilder();
                mainBuilder.Add(source.Name, ColorPalette.Green);
                mainBuilder.Add("uses", Colors.White);
                mainBuilder.AddSpace(); // Explicit space between "uses" and action name
                mainBuilder.Add(action.Name, ColorPalette.Success);
                mainBuilder.Add("!", Colors.White);
                var result = ColoredTextRenderer.RenderAsMarkup(mainBuilder.Build());
                
                // Add individual effect messages for each affected target
                foreach (var (target, duration) in affectedTargets)
                {
                    string displayName = GetDisplayName(target);
                    var effectMessage = GetEnvironmentalEffectMessageColored(action, duration);
                    var builder = new ColoredTextBuilder();
                    builder.Add("    ", Colors.White);
                    builder.Add(displayName, target is Character ? ColorPalette.Gold : ColorPalette.Enemy);
                    builder.AddSpace(); // Explicit space between display name and "affected"
                    builder.Add("affected", Colors.White);
                    builder.AddSpace(); // Explicit space between "affected" and "by"
                    builder.Add("by", Colors.White);
                    builder.AddSpace(); // Explicit space between "by" and effect name
                    builder.AddRange(effectMessage);
                    result += "\n" + ColoredTextRenderer.RenderAsMarkup(builder.Build());
                }
                
                return result;
            }
        }

        /// <summary>
        /// Applies environmental effects to a target without adding messages to results list
        /// </summary>
        /// <param name="source">The environment source</param>
        /// <param name="target">The target Actor</param>
        /// <param name="action">The action being applied</param>
        /// <param name="duration">Duration of the effect</param>
        private static void ApplyEnvironmentalEffectSilent(Actor source, Actor target, Action action, int duration)
        {
            // Apply effects based on action type and properties
            if (action.CausesBleed)
            {
                target.ApplyPoison(2, duration, true); // 2 damage per turn, bleeding type
            }
            else if (action.CausesWeaken)
            {
                target.ApplyWeaken(duration);
            }
            else if (action.CausesSlow)
            {
                // Apply slow effect - for characters, use the character-specific method
                if (target is Character character)
                {
                    character.ApplySlow(0.5, duration); // 50% speed reduction
                }
                // For enemies, we can't easily apply slow without modifying the base class
            }
            else if (action.CausesPoison)
            {
                target.ApplyPoison(2, duration); // 2 damage per turn for the duration
            }
            else if (action.CausesStun)
            {
                target.IsStunned = true;
                target.StunTurnsRemaining = duration;
            }
            else if (action.CausesBurn)
            {
                target.ApplyBurn(3, duration); // 3 damage per turn for the duration
            }
            else if (action.Type == ActionType.Attack)
            {
                // For environmental attacks, calculate damage normally
                double damageMultiplier = CalculateDamageMultiplier(source, action);
                int damage = CombatCalculator.CalculateDamage(source, target, action, damageMultiplier, 1.0, 0, 0);
                
                // Apply damage
                ApplyDamage(target, damage);
            }
        }

        /// <summary>
        /// Formats a single environmental effect message
        /// </summary>
        /// <param name="source">The environment source</param>
        /// <param name="target">The target Actor</param>
        /// <param name="action">The action being applied</param>
        /// <param name="duration">Duration of the effect</param>
        /// <returns>Formatted message</returns>
        private static string FormatEnvironmentalEffectMessage(Actor source, Actor target, Action action, int duration)
        {
            string displayName = GetDisplayName(target);
            
            // Build main action line - just "uses Ancient Trap!" without target
            var mainBuilder = new ColoredTextBuilder();
            mainBuilder.Add(source.Name, ColorPalette.Green);
            mainBuilder.Add("uses", Colors.White);
            mainBuilder.AddSpace(); // Explicit space between "uses" and action name
            mainBuilder.Add(action.Name, ColorPalette.Success);
            mainBuilder.Add("!", Colors.White);
            string mainLine = ColoredTextRenderer.RenderAsMarkup(mainBuilder.Build());
            
            // Build effect line - "affected by BLEED for x turns"
            var effectBuilder = new ColoredTextBuilder();
            effectBuilder.Add("    ", Colors.White);
            effectBuilder.Add(displayName, target is Character ? ColorPalette.Gold : ColorPalette.Enemy);
            effectBuilder.AddSpace(); // Explicit space between display name and "affected"
            effectBuilder.Add("affected", Colors.White);
            effectBuilder.AddSpace(); // Explicit space between "affected" and "by"
            effectBuilder.Add("by", Colors.White);
            
            effectBuilder.AddSpace(); // Explicit space between "by" and effect name
            if (action.CausesBleed)
            {
                effectBuilder.Add("BLEED", ColorPalette.Error);
            }
            else if (action.CausesWeaken)
            {
                effectBuilder.Add("WEAKEN", ColorPalette.Orange);
            }
            else if (action.CausesSlow)
            {
                effectBuilder.Add("SLOW", ColorPalette.Cyan);
            }
            else if (action.CausesPoison)
            {
                effectBuilder.Add("POISON", ColorPalette.Green);
            }
            else if (action.CausesStun)
            {
                effectBuilder.Add("STUN", ColorPalette.Magenta);
            }
            else if (action.Type == ActionType.Attack)
            {
                // For environmental attacks, calculate damage normally
                double damageMultiplier = CalculateDamageMultiplier(source, action);
                int damage = CombatCalculator.CalculateDamage(source, target, action, damageMultiplier, 1.0, 0, 0);
                var (damageText, rollInfo) = CombatResults.FormatDamageDisplayColored(source, target, damage, damage, action, 1.0, damageMultiplier, 0, 0);
                return ColoredTextRenderer.RenderAsMarkup(damageText) + "\n" + ColoredTextRenderer.RenderAsMarkup(rollInfo);
            }
            else
            {
                // Generic environmental effect
                effectBuilder.Add("EFFECT", ColorPalette.Warning);
            }
            
            if (action.Type != ActionType.Attack)
            {
                effectBuilder.AddSpace(); // Explicit space between effect name and "for"
                effectBuilder.Add("for", Colors.White);
                effectBuilder.AddSpace(); // Explicit space between "for" and duration
                effectBuilder.Add(duration.ToString(), Colors.White);
                effectBuilder.AddSpace(); // Explicit space between duration and "turns"
                effectBuilder.Add("turns", Colors.White);
                string effectLine = ColoredTextRenderer.RenderAsMarkup(effectBuilder.Build());
                return mainLine + "\n" + effectLine;
            }
            
            return mainLine;
        }

        /// <summary>
        /// Gets unique target names for environmental messages, ensuring HERO and ENEMY are shown correctly
        /// </summary>
        /// <param name="targets">List of target entities</param>
        /// <returns>Comma-separated list of unique target names</returns>
        private static string GetUniqueTargetNames(List<Actor> targets)
        {
            var uniqueNames = new HashSet<string>();
            
            foreach (var target in targets)
            {
                string displayName = GetDisplayName(target);
                uniqueNames.Add(displayName);
            }
            
            return string.Join(" and ", uniqueNames);
        }

        /// <summary>
        /// Gets the display name for an Actor in environmental messages
        /// </summary>
        /// <param name="Actor">The Actor to get display name for</param>
        /// <returns>Display name with Actor's actual name</returns>
        private static string GetDisplayName(Actor Actor)
        {
            return Actor.Name;
        }

        /// <summary>
        /// Gets the environmental effect message for area-of-effect actions as ColoredText
        /// </summary>
        /// <param name="action">The action being applied</param>
        /// <param name="duration">Duration of the effect</param>
        /// <returns>Effect message as ColoredText</returns>
        private static List<ColoredText> GetEnvironmentalEffectMessageColored(Action action, int duration)
        {
            var builder = new ColoredTextBuilder();
            
            if (action.CausesBleed)
            {
                builder.Add("BLEED", ColorPalette.Error);
            }
            else if (action.CausesWeaken)
            {
                builder.Add("WEAKEN", ColorPalette.Orange);
            }
            else if (action.CausesSlow)
            {
                builder.Add("SLOW", ColorPalette.Cyan);
            }
            else if (action.CausesPoison)
            {
                builder.Add("POISON", ColorPalette.Green);
            }
            else if (action.CausesStun)
            {
                builder.Add("STUN", ColorPalette.Magenta);
            }
            else
            {
                builder.Add("EFFECT", ColorPalette.Warning);
            }
            
            builder.Add("for", Colors.White);
            builder.Add(duration.ToString(), Colors.White);
            builder.Add("turns", Colors.White);
            
            return builder.Build();
        }

        /// <summary>
        /// Calculates damage multiplier for environmental actions
        /// </summary>
        private static double CalculateDamageMultiplier(Actor source, Action action)
        {
            if (source is Character character)
            {
                // Only apply combo amplification to combo actions, and only after the first one
                if (action.IsComboAction && character.ComboStep > 0)
                {
                    return character.GetCurrentComboAmplification();
                }
            }
            else if (source is Enemy enemy)
            {
                // Enemies also get combo amplification (same as heroes)
                if (action.IsComboAction && enemy.ComboStep > 0)
                {
                    return enemy.GetCurrentComboAmplification();
                }
            }
            return 1.0;
        }

        /// <summary>
        /// Applies damage to target Actor
        /// </summary>
        private static void ApplyDamage(Actor target, int damage)
        {
            if (target is Character targetCharacter)
            {
                targetCharacter.TakeDamage(damage);
            }
            else if (target is Enemy targetEnemy)
            {
                targetEnemy.TakeDamage(damage);
            }
        }
    }
}



