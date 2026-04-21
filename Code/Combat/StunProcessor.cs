using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        /// Processes a stunned entity (player or enemy) with generic logic.
        /// Uses synchronous display; combat loop does not wait for the full action delay.
        /// Prefer <see cref="ProcessStunnedEntityAsync"/> when the caller can await so stun blocks get the same pacing as normal actions.
        /// </summary>
        /// <typeparam name="T">Type of entity (Character or Enemy)</typeparam>
        /// <param name="entity">The stunned entity</param>
        /// <param name="stateManager">Combat state manager for turn tracking</param>
        public static void ProcessStunnedEntity<T>(T entity, CombatStateManager stateManager) where T : Actor
        {
            DisplayStunBlock(entity);
            ApplyStunTurnUpdates(entity, stateManager);
        }

        /// <summary>
        /// Processes a stunned entity (player or enemy) and waits for the stun block display delay.
        /// Use this from the combat turn handler so stun blocks get the same ActionDelayMs pacing as normal combat actions.
        /// </summary>
        /// <typeparam name="T">Type of entity (Character or Enemy)</typeparam>
        /// <param name="entity">The stunned entity</param>
        /// <param name="stateManager">Combat state manager for turn tracking</param>
        public static async Task ProcessStunnedEntityAsync<T>(T entity, CombatStateManager stateManager) where T : Actor
        {
            await DisplayStunBlockAsync(entity);
            ApplyStunTurnUpdates(entity, stateManager);
        }

        private static void DisplayStunBlock<T>(T entity) where T : Actor
        {
            if (CombatManager.DisableCombatUIOutput) return;

            var (actionText, statusEffects, character) = BuildStunBlockContent(entity);
            BlockDisplayManager.DisplayActionBlock(actionText, new List<ColoredText>(), statusEffects, null, null, character);
        }

        private static async Task DisplayStunBlockAsync<T>(T entity) where T : Actor
        {
            if (CombatManager.DisableCombatUIOutput) return;

            var (actionText, statusEffects, character) = BuildStunBlockContent(entity);
            await BlockDisplayManager.DisplayActionBlockAsync(actionText, new List<ColoredText>(), statusEffects, null, null, character);
        }

        private static (List<ColoredText> actionText, List<List<ColoredText>> statusEffects, Character? character) BuildStunBlockContent<T>(T entity) where T : Actor
        {
            var builder = new ColoredTextBuilder();
            builder.Add(entity.Name, EntityColorHelper.GetActorColor(entity));
            builder.AddSpace();
            builder.Add("is", Colors.White);
            builder.AddSpace();
            var stunnedSegments = ColorTemplateLibrary.GetTemplate("stunned", "stunned");
            builder.AddRange(stunnedSegments);
            builder.AddSpace();
            builder.Add("and cannot act!", Colors.White);
            var actionText = builder.Build();

            var statusEffectBuilder = new ColoredTextBuilder();
            statusEffectBuilder.Add("(");
            statusEffectBuilder.Add($"{entity.StunTurnsRemaining} turns remaining", Colors.White);
            statusEffectBuilder.Add(")");
            var turnsRemainingStatusEffect = statusEffectBuilder.Build();
            var statusEffects = new List<List<ColoredText>> { turnsRemainingStatusEffect };

            Character? character = entity as Character;
            return (actionText, statusEffects, character);
        }

        private static void ApplyStunTurnUpdates<T>(T entity, CombatStateManager stateManager) where T : Actor
        {
            double entityActionSpeed = GetEntityActionSpeed(entity);
            entity.UpdateTempEffects(entityActionSpeed / 10.0);
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
            // Enemy before Character (Enemy subclasses Character; see ActionSpeedCalculator / ActionSpeedSystem).
            if (entity is Enemy enemy)
            {
                return enemy.GetTotalAttackSpeed();
            }
            
            if (entity is Character character)
            {
                return character.GetTotalAttackSpeed();
            }
            
            return 1.0; // Default fallback
        }
    }
}

