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
                
                // Group related messages together - display damage and status effects as one block
                var damageMessages = new List<string>();
                var statusMessages = new List<string>();
                
                for (int i = 0; i < results.Count; i++)
                {
                    var result = results[i];
                    if (result.StartsWith("    ")) // Status effect message (indented)
                    {
                        statusMessages.Add(result); // Keep indentation for proper formatting
                    }
                    else // Damage message
                    {
                        damageMessages.Add(result);
                    }
                }
                
                // Combine damage and status messages into single blocks to avoid spacing issues
                if (damageMessages.Count > 0 && statusMessages.Count > 0)
                {
                    // Combine damage and status messages into one block
                    string combinedMessage = string.Join("\n", damageMessages.Concat(statusMessages));
                    BlockDisplayManager.DisplaySystemBlock(ColoredTextParser.Parse(combinedMessage));
                    // Record as poison damage block for spacing system
                    TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.PoisonDamage);
                }
                else if (damageMessages.Count > 0)
                {
                    // Only damage messages
                    foreach (var damageMsg in damageMessages)
                    {
                        BlockDisplayManager.DisplaySystemBlock(ColoredTextParser.Parse(damageMsg));
                    }
                    // Record as poison damage block for spacing system
                    TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.PoisonDamage);
                }
                else if (statusMessages.Count > 0)
                {
                    // Only status messages
                    foreach (var status in statusMessages)
                    {
                        BlockDisplayManager.DisplaySystemBlock(ColoredTextParser.Parse(status));
                    }
                    // Record as poison damage block for spacing system
                    TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.PoisonDamage);
                }
            }
        }
    }
}

