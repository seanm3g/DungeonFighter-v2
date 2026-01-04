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
        /// Returns structured ColoredText data matching the action block format
        /// </summary>
        /// <param name="source">The environment performing the action</param>
        /// <param name="targets">List of target entities</param>
        /// <param name="action">The action to perform</param>
        /// <param name="environment">The environment context</param>
        /// <returns>Structured ColoredText data: ((actionText, rollInfo), statusEffects)</returns>
        public static ((List<ColoredText> actionText, List<ColoredText>? rollInfo) mainResult, List<List<ColoredText>> statusEffects) ExecuteEnvironmentalAction(Actor source, List<Actor> targets, Action action, Environment? environment = null)
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
            
            // Build main action text
            var actionTextBuilder = new ColoredTextBuilder();
            actionTextBuilder.Add(source.Name, ColorPalette.Green);
            AddUsesAction(actionTextBuilder, action.Name, ColorPalette.Success);
            actionTextBuilder.Add("!", Colors.White);
            var actionText = actionTextBuilder.Build();
            
            // If no targets, return no effect message
            if (aliveTargets.Count == 0)
            {
                var noEffectBuilder = new ColoredTextBuilder();
                noEffectBuilder.Add(source.Name, ColorPalette.Green);
                noEffectBuilder.Add("'s", Colors.White);
                noEffectBuilder.AddSpace();
                noEffectBuilder.Add(action.Name, ColorPalette.Success);
                noEffectBuilder.Add(" has no effect!", Colors.White);
                return ((noEffectBuilder.Build(), null), new List<List<ColoredText>>());
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
                var noEffectBuilder = new ColoredTextBuilder();
                noEffectBuilder.Add(source.Name, ColorPalette.Green);
                noEffectBuilder.Add("'s", Colors.White);
                noEffectBuilder.AddSpace();
                noEffectBuilder.Add(action.Name, ColorPalette.Success);
                noEffectBuilder.Add(" has no effect!", Colors.White);
                return ((noEffectBuilder.Build(), null), new List<List<ColoredText>>());
            }
            
            // Build status effects list (one per affected target)
            var statusEffects = new List<List<ColoredText>>();
            List<ColoredText>? rollInfo = null;
            
            // Handle attack type environmental actions separately (they need rollInfo)
            if (action.Type == ActionType.Attack && affectedTargets.Count == 1)
            {
                var (target, duration) = affectedTargets[0];
                double damageMultiplier = CalculateDamageMultiplier(source, action);
                int damage = CombatCalculator.CalculateDamage(source, target, action, damageMultiplier, 1.0, 0, 0);
                
                // Use standard damage formatting for attacks
                var (damageText, attackRollInfo) = CombatResults.FormatDamageDisplayColored(source, target, damage, damage, action, 1.0, damageMultiplier, 0, 0);
                
                // For environmental attacks, we replace the action text with the damage text
                // and use the roll info
                return ((damageText, attackRollInfo), new List<List<ColoredText>>());
            }
            
            // For non-attack actions or multi-target attacks, build status effects
            foreach (var (target, duration) in affectedTargets)
            {
                var effectMessage = FormatEnvironmentalEffectMessageColored(source, target, action, duration);
                if (effectMessage != null && effectMessage.Count > 0)
                {
                    statusEffects.Add(effectMessage);
                }
            }
            
            return ((actionText, rollInfo), statusEffects);
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
        /// Formats a single environmental effect message as ColoredText
        /// Note: Indentation is handled by BlockMessageCollector, so we don't add it here
        /// </summary>
        /// <param name="source">The environment source</param>
        /// <param name="target">The target Actor</param>
        /// <param name="action">The action being applied</param>
        /// <param name="duration">Duration of the effect</param>
        /// <returns>Formatted effect message as ColoredText, or null if not applicable</returns>
        private static List<ColoredText>? FormatEnvironmentalEffectMessageColored(Actor source, Actor target, Action action, int duration)
        {
            string displayName = GetDisplayName(target);
            
            // Build effect line - "affected by BLEED for x turns"
            // Note: Don't add indentation here - BlockMessageCollector handles it
            var effectBuilder = new ColoredTextBuilder();
            effectBuilder.Add(displayName, EntityColorHelper.GetActorColor(target));
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
            else
            {
                // Generic environmental effect
                var coloredEffect = StatusEffectColorHelper.GetColoredStatusEffect("EFFECT");
                effectBuilder.AddRange(coloredEffect);
            }
            
            // Add duration information
            AddForAmountUnit(effectBuilder, duration.ToString(), ColorPalette.White, "turns", ColorPalette.White);
            
            return effectBuilder.Build();
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

