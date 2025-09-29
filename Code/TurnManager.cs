using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Manages turn-based combat logic including turn order, action execution, and turn progression
    /// Extracted from CombatManager to reduce complexity
    /// </summary>
    public class TurnManager
    {
        private ActionSpeedSystem? actionSpeedSystem;
        private BattleHealthTracker? healthTracker;
        private Action? lastPlayerAction;

        /// <summary>
        /// Initializes the turn manager for a new battle
        /// </summary>
        public void InitializeBattle()
        {
            actionSpeedSystem = new ActionSpeedSystem();
            healthTracker = new BattleHealthTracker();
            lastPlayerAction = null;
        }

        /// <summary>
        /// Cleans up the turn manager after battle
        /// </summary>
        public void EndBattle()
        {
            actionSpeedSystem = null;
            healthTracker = null;
            lastPlayerAction = null;
        }

        /// <summary>
        /// Determines turn order based on action speeds
        /// </summary>
        /// <param name="player">The player character</param>
        /// <param name="enemy">The enemy</param>
        /// <param name="playerAction">The player's chosen action</param>
        /// <param name="enemyAction">The enemy's action</param>
        /// <returns>List of entities in turn order</returns>
        public List<Entity> DetermineTurnOrder(Character player, Enemy enemy, Action playerAction, Action enemyAction)
        {
            if (actionSpeedSystem == null)
                throw new InvalidOperationException("TurnManager not initialized. Call InitializeBattle() first.");

            // TODO: Fix turn order determination - DetermineTurnOrder method doesn't exist
            // For now, return player first
            return new List<Entity> { player, enemy };
        }

        /// <summary>
        /// Executes a turn for an entity
        /// </summary>
        /// <param name="entity">The entity taking the turn</param>
        /// <param name="target">The target entity</param>
        /// <param name="action">The action to execute</param>
        /// <param name="environment">The environment affecting the action</param>
        /// <returns>True if the entity is still alive after the turn</returns>
        public bool ExecuteTurn(Entity entity, Entity target, Action action, Environment? environment = null)
        {
            if (actionSpeedSystem == null || healthTracker == null)
                throw new InvalidOperationException("TurnManager not initialized. Call InitializeBattle() first.");

            // Execute the action
            string result = CombatResults.ExecuteActionWithUI(entity, target, action, environment, lastPlayerAction);
            UIManager.WriteCombatLine(result);

            // Track last player action for DEJA VU functionality
            if (entity is Character)
            {
                lastPlayerAction = action;
            }

            // Check if target is still alive
            if (target is Character character)
                return character.CurrentHealth > 0;
            else if (target is Enemy enemy)
                return enemy.CurrentHealth > 0;
            return true; // Fallback
        }

        /// <summary>
        /// Processes damage over time effects for both entities
        /// </summary>
        /// <param name="player">The player character</param>
        /// <param name="enemy">The enemy</param>
        public void ProcessDamageOverTimeEffects(Character player, Enemy enemy)
        {
            // Process effects for player
            var playerResults = new List<string>();
            int playerDamage = CombatEffects.ProcessStatusEffects(player, playerResults);
            if (playerDamage > 0)
            {
                player.TakeDamage(playerDamage);
            }
            foreach (string message in playerResults)
            {
                UIManager.WriteCombatLine(message);
            }
            
            // Process effects for enemy (only if living)
            if (enemy.IsLiving)
            {
                var enemyResults = new List<string>();
                int enemyDamage = CombatEffects.ProcessStatusEffects(enemy, enemyResults);
                if (enemyDamage > 0)
                {
                    enemy.TakeDamage(enemyDamage);
                }
                foreach (string message in enemyResults)
                {
                    UIManager.WriteCombatLine(message);
                }
            }
        }

        /// <summary>
        /// Processes regeneration effects for the player
        /// </summary>
        /// <param name="player">The player character</param>
        public void ProcessRegeneration(Character player)
        {
            if (player.CurrentHealth < player.GetEffectiveMaxHealth())
            {
                var tuning = TuningConfig.Instance;
                // TODO: Fix player regeneration - PlayerRegenPerTurn property doesn't exist
                int regenAmount = 1; // Default regeneration
                int actualRegen = Math.Min(regenAmount, player.GetEffectiveMaxHealth() - player.CurrentHealth);
                
                if (actualRegen > 0)
                {
                    player.Heal(actualRegen);
                    UIManager.WriteCombatLine($"[{player.Name}] regenerates {actualRegen} health ({player.CurrentHealth}/{player.GetEffectiveMaxHealth()})");
                }
            }
        }

        /// <summary>
        /// Checks health milestones and adds battle events
        /// </summary>
        /// <param name="entity">The entity to check</param>
        /// <param name="damageAmount">The amount of damage taken</param>
        public void CheckHealthMilestones(Entity entity, int damageAmount)
        {
            if (healthTracker == null)
                return;

            var events = healthTracker.CheckHealthMilestones(entity, damageAmount);
            foreach (var evt in events)
            {
                UIManager.WriteCombatLine(evt);
            }
        }

        /// <summary>
        /// Gets pending health notifications
        /// </summary>
        /// <returns>List of pending health notifications</returns>
        public List<string> GetPendingHealthNotifications()
        {
            // TODO: Fix health notifications - GetAndClearPendingHealthNotifications method doesn't exist
            return new List<string>();
        }

        /// <summary>
        /// Gets turn order information for display
        /// </summary>
        /// <returns>Turn order information string</returns>
        public string GetTurnOrderInfo()
        {
            return actionSpeedSystem?.GetTurnOrderInfo() ?? "Turn order not available";
        }

        /// <summary>
        /// Gets the last player action for DEJA VU functionality
        /// </summary>
        /// <returns>The last player action, or null if none</returns>
        public Action? GetLastPlayerAction()
        {
            return lastPlayerAction;
        }

        /// <summary>
        /// Gets the current action speed system
        /// </summary>
        /// <returns>The action speed system, or null if not initialized</returns>
        public ActionSpeedSystem? GetActionSpeedSystem()
        {
            return actionSpeedSystem;
        }

        /// <summary>
        /// Initializes the health tracker for battle participants
        /// </summary>
        /// <param name="participants">List of entities participating in the battle</param>
        public void InitializeHealthTracker(List<Entity> participants)
        {
            if (healthTracker == null)
                healthTracker = new BattleHealthTracker();
            
            healthTracker.InitializeBattle(participants);
        }

        /// <summary>
        /// Updates the last player action for DEJA VU functionality
        /// </summary>
        /// <param name="action">The action to set as the last player action</param>
        public void UpdateLastPlayerAction(Action action)
        {
            lastPlayerAction = action;
        }
    }
}
