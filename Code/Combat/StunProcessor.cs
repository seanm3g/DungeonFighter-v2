using System;

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
        public static void ProcessStunnedEntity<T>(T entity, CombatStateManager stateManager) where T : Entity
        {
            if (!CombatManager.DisableCombatUIOutput)
            {
                // Use the new block-based system for stun messages
                BlockDisplayManager.DisplayEffectBlock(
                    $"[{entity.Name}] is stunned and cannot act!", 
                    $"{entity.StunTurnsRemaining} turns remaining"
                );
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
        private static double GetEntityActionSpeed<T>(T entity) where T : Entity
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
