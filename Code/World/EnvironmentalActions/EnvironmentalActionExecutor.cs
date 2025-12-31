using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications;
using static RPGGame.Combat.Formatting.DamageFormatter;

namespace RPGGame
{
    /// <summary>
    /// Handles environmental action execution with duration-based effects
    /// Extracted from EnvironmentalActionHandler to separate environmental action logic
    /// </summary>
    public static class EnvironmentalActionExecutor
    {
        /// <summary>
        /// Executes an environmental action with duration-based effects
        /// </summary>
        /// <param name="source">The environment performing the action</param>
        /// <param name="targets">List of target entities</param>
        /// <param name="action">The action to perform</param>
        /// <param name="environment">The environment context</param>
        /// <returns>A string describing the results</returns>
        public static string ExecuteEnvironmentalAction(Actor source, List<Actor> targets, Action action, Environment? environment = null)
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
                builder.Add("'s", Colors.White);
                builder.AddSpace();
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
                builder.Add("'s", Colors.White);
                builder.AddSpace();
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
                AddUsesAction(mainBuilder, action.Name, ColorPalette.Success);
                mainBuilder.Add("!", Colors.White);
                var result = ColoredTextRenderer.RenderAsMarkup(mainBuilder.Build());
                
                // Add individual effect messages for each affected target
                for (int i = 0; i < affectedTargets.Count; i++)
                {
                    var (target, duration) = affectedTargets[i];
                    string displayName = GetDisplayName(target);
                    var effectMessage = GetEnvironmentalEffectMessageColored(action, duration);
                    var builder = new ColoredTextBuilder();
                    // Add 5 spaces for indentation to match roll detail lines
                    builder.Add("     ", Colors.White);
                    builder.Add(displayName, EntityColorHelper.GetActorColor(target));
                    builder.AddSpace();
                    builder.Add("affected", Colors.White);
                    builder.AddSpace();
                    builder.Add("by", Colors.White);
                    builder.AddSpace();
                    builder.AddRange(effectMessage);
                    result += "\n" + ColoredTextRenderer.RenderAsMarkup(builder.Build());
                    
                    // Add blank line after last status effect message to separate from next character action
                    bool isLast = i == affectedTargets.Count - 1;
                    if (isLast)
                    {
                        result += "\n";
                    }
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
            AddUsesAction(mainBuilder, action.Name, ColorPalette.Success);
            mainBuilder.Add("!", Colors.White);
            string mainLine = ColoredTextRenderer.RenderAsMarkup(mainBuilder.Build());
            
            // Build effect line - "affected by BLEED for x turns"
            // Add 5 spaces for indentation to match roll detail lines
            var effectBuilder = new ColoredTextBuilder();
            effectBuilder.Add("     ", Colors.White);
            effectBuilder.Add(displayName, target is Enemy ? ColorPalette.Enemy : ColorPalette.Player);
            effectBuilder.AddSpace();
            effectBuilder.Add("affected", Colors.White);
            effectBuilder.AddSpace();
            effectBuilder.Add("by", Colors.White);
            
            effectBuilder.AddSpace();
            string? effectName = GetStatusEffectName(action);
            if (effectName != null)
            {
                // Use themed color template for status effect
                var coloredEffect = StatusEffectColorHelper.GetColoredStatusEffect(effectName);
                effectBuilder.AddRange(coloredEffect);
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
                var coloredEffect = StatusEffectColorHelper.GetColoredStatusEffect("EFFECT");
                effectBuilder.AddRange(coloredEffect);
            }
            
            if (action.Type != ActionType.Attack)
            {
                AddForAmountUnit(effectBuilder, duration.ToString(), ColorPalette.White, "turns", ColorPalette.White);
                string effectLine = ColoredTextRenderer.RenderAsMarkup(effectBuilder.Build());
                // Add blank line after all status effect messages to separate from next character action
                return mainLine + "\n" + effectLine + "\n";
            }
            
            return mainLine;
        }

        /// <summary>
        /// Gets the display name for an Actor in environmental messages
        /// </summary>
        /// <param name="actor">The Actor to get display name for</param>
        /// <returns>Display name with Actor's actual name</returns>
        private static string GetDisplayName(Actor actor)
        {
            return actor.Name;
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
            
            string? effectName = GetStatusEffectName(action);
            if (effectName != null)
            {
                // Use themed color template for status effect
                var coloredEffect = StatusEffectColorHelper.GetColoredStatusEffect(effectName);
                builder.AddRange(coloredEffect);
            }
            else
            {
                // Generic environmental effect
                var coloredEffect = StatusEffectColorHelper.GetColoredStatusEffect("EFFECT");
                builder.AddRange(coloredEffect);
            }
            
            AddForAmountUnit(builder, duration.ToString(), ColorPalette.White, "turns", ColorPalette.White);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Gets the status effect name for an action based on its properties
        /// </summary>
        /// <param name="action">The action to check</param>
        /// <returns>The status effect name, or null if no status effect</returns>
        private static string? GetStatusEffectName(Action action)
        {
            if (action.CausesBleed)
                return "BLEED";
            else if (action.CausesWeaken)
                return "WEAKEN";
            else if (action.CausesSlow)
                return "SLOW";
            else if (action.CausesPoison)
                return "POISON";
            else if (action.CausesStun)
                return "STUN";
            else if (action.CausesBurn)
                return "BURN";
            // Note: CausesFreeze doesn't exist on Action class, so removed
            
            return null;
        }

        /// <summary>
        /// Calculates damage multiplier for environmental actions
        /// Uses amplification based on the step the action is executing at
        /// </summary>
        private static double CalculateDamageMultiplier(Actor source, Action action)
        {
            if (source is Character character)
            {
                // Only apply combo amplification to combo actions
                if (action.IsComboAction)
                {
                    var comboActions = character.GetComboActions();
                    if (comboActions.Count > 0)
                    {
                        int currentStep = character.ComboStep % comboActions.Count;
                        double baseAmp = character.GetComboAmplifier();
                        // Step 0 adds no bonus (1.0x), bonus starts at Step 1+
                        // This ensures each sequential action gets progressively higher amplification
                        return Math.Pow(baseAmp, currentStep);
                    }
                }
            }
            else if (source is Enemy enemy)
            {
                // Enemies also get combo amplification (same as heroes)
                if (action.IsComboAction)
                {
                    var comboActions = enemy.GetComboActions();
                    if (comboActions.Count > 0)
                    {
                        int currentStep = enemy.ComboStep % comboActions.Count;
                        double baseAmp = enemy.GetComboAmplifier();
                        // Step 0 adds no bonus (1.0x), bonus starts at Step 1+
                        return Math.Pow(baseAmp, currentStep);
                    }
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

