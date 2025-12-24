using System;
using System.Collections.Generic;
using RPGGame.UI.ColorSystem;

namespace RPGGame
{
    /// <summary>
    /// Handles area-of-effect action execution
    /// Extracted from EnvironmentalActionHandler to separate AoE logic
    /// </summary>
    public static class AreaOfEffectExecutor
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
    }
}

