using System.Collections.Generic;
using System.Linq;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Combat.Turn
{
    /// <summary>
    /// Processes damage over time and status effects for combat entities
    /// </summary>
    public static class StatusEffectProcessor
    {
        /// <summary>
        /// Processes damage over time effects for an entity
        /// </summary>
        /// <param name="entity">The entity to process effects for</param>
        /// <param name="results">Output list for effect messages</param>
        /// <returns>Total damage dealt</returns>
        public static int ProcessEntityStatusEffects(Actor entity, List<string> results)
        {
            return CombatEffectsSimplified.ProcessStatusEffects(entity, results);
        }

        /// <summary>
        /// Processes damage over time effects for both player and enemy
        /// </summary>
        /// <param name="player">The player character</param>
        /// <param name="enemy">The enemy</param>
        public static void ProcessDamageOverTimeEffects(Character player, Enemy enemy)
        {
            bool blankLineAdded = false;
            
            // Process effects for player
            ProcessEntityEffects(player, ref blankLineAdded);
            
            // Process effects for enemy (only if living)
            if (enemy.IsLiving)
            {
                ProcessEntityEffects(enemy, ref blankLineAdded);
            }
        }

        private static void ProcessEntityEffects(Actor entity, ref bool blankLineAdded)
        {
            var results = new List<string>();
            int damage = ProcessEntityStatusEffects(entity, results);
            
            if (damage > 0)
            {
                // TakeDamage is defined on Character and Enemy, not Actor
                if (entity is Character character)
                {
                    character.TakeDamage(damage);
                }
                else if (entity is Enemy enemy)
                {
                    enemy.TakeDamage(damage);
                }
            }
            
            if (results.Count > 0)
            {
                // Apply spacing for poison damage (context-aware)
                if (!RPGGame.TurnManager.DisableCombatUIOutput && !blankLineAdded)
                {
                    TextSpacingSystem.ApplySpacingBefore(TextSpacingSystem.BlockType.PoisonDamage);
                    blankLineAdded = true;
                }
                
                // Combine ALL status effect messages into a single block
                // Parse each message separately and combine with explicit newlines to ensure proper line breaks
                var combinedSegments = new List<ColoredText>();
                for (int i = 0; i < results.Count; i++)
                {
                    if (i > 0)
                    {
                        // Add explicit newline segment between messages to ensure proper line breaks
                        // This prevents the "no longer affected" message from wrapping onto the previous line
                        combinedSegments.Add(new ColoredText(System.Environment.NewLine, Avalonia.Media.Colors.White));
                    }
                    
                    // Parse each message separately
                    var messageSegments = ColoredTextParser.Parse(results[i]);
                    combinedSegments.AddRange(messageSegments);
                }
                
                BlockDisplayManager.DisplaySystemBlock(combinedSegments);
                // Record as poison damage block for spacing system (only once for the entire block)
                TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.PoisonDamage);
            }
        }
    }
}

