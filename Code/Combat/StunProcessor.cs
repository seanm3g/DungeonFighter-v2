using System;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;
using static RPGGame.UI.Avalonia.AsciiArtAssets;

namespace RPGGame
{
    /// <summary>
    /// Generic stun processor to eliminate duplication between player and enemy stun handling
    /// </summary>
    public static class StunProcessor
    {
        /// <summary>
        /// Processes a stunned entity (player or enemy) with generic logic
        /// </summary>
        /// <typeparam name="T">Type of entity (Character or Enemy)</typeparam>
        /// <param name="entity">The stunned entity</param>
        /// <param name="stateManager">Combat state manager for turn tracking</param>
        public static void ProcessStunnedEntity<T>(T entity, CombatStateManager stateManager) where T : Actor
        {
            if (!CombatManager.DisableCombatUIOutput)
            {
                // Use the new block-based system for stun messages with ColoredText
                // Spacing is handled by TextSpacingSystem in DisplayEffectBlock
                // Build properly colored stun message with actor name and stunned template
                var builder = new ColoredTextBuilder();
                
                // Add actor name with appropriate color (no brackets)
                builder.Add(entity.Name, EntityColorHelper.GetActorColor(entity));
                // Add "is" with space
                builder.AddSpace();
                builder.Add("is", Colors.White);
                // Add "stunned" using the stunned template
                builder.AddSpace();
                var stunnedSegments = ColorTemplateLibrary.GetTemplate("stunned", "stunned");
                builder.AddRange(stunnedSegments);
                // Add "and cannot act!"
                builder.AddSpace();
                builder.Add("and cannot act!", Colors.White);
                
                var effectText = builder.Build();
                
                // Build details text
                var detailsBuilder = new ColoredTextBuilder();
                detailsBuilder.Add($"{entity.StunTurnsRemaining} turns remaining", Colors.White);
                var detailsText = detailsBuilder.Build();
                
                BlockDisplayManager.DisplayEffectBlock(effectText, detailsText);
            }
            
            // Get the entity's action speed to calculate proper stun reduction
            double entityActionSpeed = GetEntityActionSpeed(entity);
            
            // Update temp effects with action speed-based turn reduction
            entity.UpdateTempEffects(entityActionSpeed / 10.0); // Normalize to turn-based system
            
            // Advance the entity's turn in the action speed system based on their action speed
            var currentSpeedSystem = stateManager.GetCurrentActionSpeedSystem();
            if (currentSpeedSystem != null)
            {
                currentSpeedSystem.AdvanceEntityTurn(entity, entityActionSpeed);
            }
        }

        /// <summary>
        /// Gets the action speed for different entity types
        /// </summary>
        /// <typeparam name="T">Type of entity</typeparam>
        /// <param name="entity">The entity</param>
        /// <returns>Action speed value</returns>
        private static double GetEntityActionSpeed<T>(T entity) where T : Actor
        {
            if (entity is Character character)
            {
                return character.GetTotalAttackSpeed();
            }
            else if (entity is Enemy enemy)
            {
                return enemy.GetTotalAttackSpeed();
            }
            else
            {
                return 1.0; // Default fallback
            }
        }
    }
}

