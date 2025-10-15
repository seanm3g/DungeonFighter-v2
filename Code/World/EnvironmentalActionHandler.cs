using System;
using System.Collections.Generic;
using System.Linq;

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
        /// <param name="source">The entity performing the action</param>
        /// <param name="targets">List of target entities</param>
        /// <param name="environment">The environment affecting the action</param>
        /// <param name="selectedAction">The action to perform (optional)</param>
        /// <param name="battleNarrative">The battle narrative to add events to</param>
        /// <returns>A string describing the results</returns>
        public static string ExecuteAreaOfEffectAction(Entity source, List<Entity> targets, Environment? environment = null, Action? selectedAction = null, BattleNarrative? battleNarrative = null)
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
        private static string ExecuteEnvironmentalAction(Entity source, List<Entity> targets, Action action, Environment? environment = null)
        {
            // Get list of alive targets
            var aliveTargets = new List<Entity>();
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
                string coloredSourceName = $"{{{{natural|{source.Name}}}}}";
                return $"{coloredSourceName}'s {action.Name} has no effect!";
            }
            
            // Roll separately for each target and track which ones are affected
            var affectedTargets = new List<(Entity target, int duration)>();
            
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
                string coloredSourceName = $"{{{{natural|{source.Name}}}}}";
                return $"{coloredSourceName}'s {action.Name} has no effect!";
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
                var targetNames = GetUniqueTargetNames(aliveTargets);
                string coloredSourceName = $"{{{{natural|{source.Name}}}}}";
                var result = $"{coloredSourceName} uses {action.Name} on {targetNames}!";
                
                // Add individual effect messages for each affected target
                foreach (var (target, duration) in affectedTargets)
                {
                    string displayName = GetDisplayName(target);
                    string effectMessage = GetEnvironmentalEffectMessage(action, duration);
                    result += $"\n    {displayName} &Yaffected by&y {effectMessage}";
                }
                
                return result;
            }
        }

        /// <summary>
        /// Applies environmental effects to a target without adding messages to results list
        /// </summary>
        /// <param name="source">The environment source</param>
        /// <param name="target">The target entity</param>
        /// <param name="action">The action being applied</param>
        /// <param name="duration">Duration of the effect</param>
        private static void ApplyEnvironmentalEffectSilent(Entity source, Entity target, Action action, int duration)
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
        /// <param name="target">The target entity</param>
        /// <param name="action">The action being applied</param>
        /// <param name="duration">Duration of the effect</param>
        /// <returns>Formatted message</returns>
        private static string FormatEnvironmentalEffectMessage(Entity source, Entity target, Action action, int duration)
        {
            string displayName = GetDisplayName(target);
            string coloredSourceName = $"{{{{natural|{source.Name}}}}}";
            // Use explicit color codes to prevent keyword coloring interference and spacing issues
            if (action.CausesBleed)
            {
                return $"{coloredSourceName} uses {action.Name} on {displayName}!\n    {displayName} &Yis &RBLEEDING &Yfor {duration} turns!&y";
            }
            else if (action.CausesWeaken)
            {
                return $"{coloredSourceName} uses {action.Name} on {displayName}!\n    {displayName} &Yis &OWEAKENED &Yfor {duration} turns!&y";
            }
            else if (action.CausesSlow)
            {
                return $"{coloredSourceName} uses {action.Name} on {displayName}!\n    {displayName} &Yis &CSLOWED &Yfor {duration} turns!&y";
            }
            else if (action.CausesPoison)
            {
                return $"{coloredSourceName} uses {action.Name} on {displayName}!\n    {displayName} &Yis &GPOISONED &Yfor {duration} turns!&y";
            }
            else if (action.CausesStun)
            {
                return $"{coloredSourceName} uses {action.Name} on {displayName}!\n    {displayName} &Yis &MSTUNNED &Yfor {duration} turns!&y";
            }
            else if (action.Type == ActionType.Attack)
            {
                // For environmental attacks, calculate damage normally
                double damageMultiplier = CalculateDamageMultiplier(source, action);
                int damage = CombatCalculator.CalculateDamage(source, target, action, damageMultiplier, 1.0, 0, 0);
                return CombatResults.FormatDamageDisplay(source, target, damage, damage, action, 1.0, damageMultiplier, 0, 0);
            }
            else
            {
                // Generic environmental effect
                return $"{coloredSourceName} uses {action.Name} on {displayName}!\n    &YEffect lasts for {duration} turns!&y";
            }
        }

        /// <summary>
        /// Gets unique target names for environmental messages, ensuring HERO and ENEMY are shown correctly
        /// </summary>
        /// <param name="targets">List of target entities</param>
        /// <returns>Comma-separated list of unique target names</returns>
        private static string GetUniqueTargetNames(List<Entity> targets)
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
        /// Gets the display name for an entity in environmental messages
        /// </summary>
        /// <param name="entity">The entity to get display name for</param>
        /// <returns>Display name with entity's actual name</returns>
        private static string GetDisplayName(Entity entity)
        {
            return entity.Name;
        }

        /// <summary>
        /// Gets the environmental effect message for area-of-effect actions
        /// </summary>
        /// <param name="action">The action being applied</param>
        /// <param name="duration">Duration of the effect</param>
        /// <returns>Effect message</returns>
        private static string GetEnvironmentalEffectMessage(Action action, int duration)
        {
            // Use explicit color codes to prevent keyword coloring interference
            if (action.CausesBleed)
            {
                return $"&RBLEED&Y for {duration} turns&y";
            }
            else if (action.CausesWeaken)
            {
                return $"&OWEAKEN&Y for {duration} turns&y";
            }
            else if (action.CausesSlow)
            {
                return $"&CSLOW&Y for {duration} turns&y";
            }
            else if (action.CausesPoison)
            {
                return $"&GPOISON&Y for {duration} turns&y";
            }
            else if (action.CausesStun)
            {
                return $"&MSTUN&Y for {duration} turns&y";
            }
            else
            {
                return $"&YEFFECT&Y for {duration} turns&y";
            }
        }

        /// <summary>
        /// Calculates damage multiplier for environmental actions
        /// </summary>
        private static double CalculateDamageMultiplier(Entity source, Action action)
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
        /// Applies damage to target entity
        /// </summary>
        private static void ApplyDamage(Entity target, int damage)
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
