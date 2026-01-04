using System.Collections.Generic;
using RPGGame.UI.ColorSystem;

namespace RPGGame
{
    /// <summary>
    /// Facade for handling environmental action execution and area-of-effect actions
    /// 
    /// Refactored from 411 lines to ~100 lines using Facade pattern.
    /// Delegates to:
    /// - AreaOfEffectExecutor: Area-of-effect action execution
    /// - EnvironmentalActionExecutor: Environmental action execution with duration-based effects
    /// </summary>
    public static class EnvironmentalActionHandler
    {
        /// <summary>
        /// Executes an environmental action and returns structured ColoredText data for action block display
        /// </summary>
        /// <param name="source">The environment performing the action</param>
        /// <param name="targets">List of target entities</param>
        /// <param name="selectedAction">The action to perform (optional)</param>
        /// <param name="environment">The environment context</param>
        /// <returns>Structured ColoredText data: ((actionText, rollInfo), statusEffects)</returns>
        public static ((List<ColoredText> actionText, List<ColoredText>? rollInfo) mainResult, List<List<ColoredText>> statusEffects) ExecuteEnvironmentalActionStructured(Actor source, List<Actor> targets, Action? selectedAction = null, Environment? environment = null)
        {
            // Get the action to use
            var action = selectedAction ?? source.SelectAction();
            if (action == null)
            {
                var noActionBuilder = new UI.ColorSystem.ColoredTextBuilder();
                noActionBuilder.Add(source.Name, UI.ColorSystem.ColorPalette.Green);
                noActionBuilder.Add(" has no actions available.", Avalonia.Media.Colors.White);
                return ((noActionBuilder.Build(), null), new List<List<ColoredText>>());
            }
            
            return EnvironmentalActionExecutor.ExecuteEnvironmentalAction(source, targets, action, environment);
        }
        
        /// <summary>
        /// Executes an area of effect action
        /// </summary>
        /// <param name="source">The Actor performing the action</param>
        /// <param name="targets">List of target entities</param>
        /// <param name="environment">The environment affecting the action</param>
        /// <param name="selectedAction">The action to perform (optional)</param>
        /// <param name="battleNarrative">The battle narrative to add events to</param>
        /// <returns>A string describing the results</returns>
        [System.Obsolete("Use ExecuteEnvironmentalActionStructured for environmental actions. This method is kept for backward compatibility with non-environmental area-of-effect actions.")]
        public static string ExecuteAreaOfEffectAction(Actor source, List<Actor> targets, Environment? environment = null, Action? selectedAction = null, BattleNarrative? battleNarrative = null)
        {
            // Get the action to use
            var action = selectedAction ?? source.SelectAction();
            if (action == null)
            {
                return $"{source.Name} has no actions available.";
            }
            
            // For environmental actions, use special duration-based system
            if (source is Environment)
            {
                // Convert structured result to string for backward compatibility
                var ((actionText, rollInfo), statusEffects) = EnvironmentalActionExecutor.ExecuteEnvironmentalAction(source, targets, action, environment);
                var result = new System.Text.StringBuilder();
                result.Append(UI.ColorSystem.ColoredTextRenderer.RenderAsMarkup(actionText));
                if (rollInfo != null && rollInfo.Count > 0)
                {
                    result.Append("\n");
                    result.Append(UI.ColorSystem.ColoredTextRenderer.RenderAsMarkup(rollInfo));
                }
                foreach (var effect in statusEffects)
                {
                    if (effect != null && effect.Count > 0)
                    {
                        result.Append("\n");
                        result.Append(UI.ColorSystem.ColoredTextRenderer.RenderAsMarkup(effect));
                    }
                }
                return result.ToString();
            }
            
            // For non-environmental actions, use area-of-effect executor
            return AreaOfEffectExecutor.ExecuteAreaOfEffectAction(source, targets, environment, selectedAction, battleNarrative);
        }
    }
}



