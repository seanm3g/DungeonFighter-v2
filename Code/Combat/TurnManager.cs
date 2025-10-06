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
        // Flag to disable UI output during balance analysis
        public static bool DisableCombatUIOutput = false;
        
        private ActionSpeedSystem? actionSpeedSystem;
        private BattleHealthTracker? healthTracker;
        private Action? lastPlayerAction;
        
        // Turn counter system - increments every 10 actions
        private int actionCount = 0;
        private int turnNumber = 1;

        /// <summary>
        /// Initializes the turn manager for a new battle
        /// </summary>
        public void InitializeBattle()
        {
            actionSpeedSystem = new ActionSpeedSystem();
            healthTracker = new BattleHealthTracker();
            lastPlayerAction = null;
            actionCount = 0;
            turnNumber = 1;
        }

        /// <summary>
        /// Cleans up the turn manager after battle
        /// </summary>
        public void EndBattle()
        {
            actionSpeedSystem = null;
            healthTracker = null;
            lastPlayerAction = null;
            actionCount = 0;
            turnNumber = 1;
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
        /// <param name="battleNarrative">The battle narrative to add events to</param>
        /// <returns>True if the entity is still alive after the turn</returns>
        public bool ExecuteTurn(Entity entity, Entity target, Action action, Environment? environment = null, BattleNarrative? battleNarrative = null)
        {
            if (actionSpeedSystem == null || healthTracker == null)
                throw new InvalidOperationException("TurnManager not initialized. Call InitializeBattle() first.");

            // Execute the action
            var (result, statusEffects) = CombatResults.ExecuteActionWithUIAndStatusEffects(entity, target, action, environment, lastPlayerAction, battleNarrative);
            if (!DisableCombatUIOutput)
            {
                // Use TextDisplayIntegration for consistent entity tracking
                TextDisplayIntegration.DisplayCombatAction(result, new List<string>(), statusEffects, entity.Name);
            }

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
            if (playerResults.Count > 0)
            {
                // Group related messages together - first display damage, then status effects
                for (int i = 0; i < playerResults.Count; i++)
                {
                    var result = playerResults[i];
                    if (result.StartsWith("    ")) // Status effect message (indented)
                    {
                        // This is a status effect message, display it with effect timing
                        UIManager.WriteEffectLine(result);
                    }
                    else // Damage message
                    {
                        // This is a damage message, display it with damage over time timing
                        UIManager.WriteDamageOverTimeLine(result);
                    }
                }
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
                if (enemyResults.Count > 0)
                {
                    // Group related messages together - first display damage, then status effects
                    for (int i = 0; i < enemyResults.Count; i++)
                    {
                        var result = enemyResults[i];
                        if (result.StartsWith("    ")) // Status effect message (indented)
                        {
                            // This is a status effect message, display it with effect timing
                            UIManager.WriteEffectLine(result);
                        }
                        else // Damage message
                        {
                            // This is a damage message, display it with damage over time timing
                            UIManager.WriteDamageOverTimeLine(result);
                        }
                    }
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
                var tuning = GameConfiguration.Instance;
                // TODO: Fix player regeneration - PlayerRegenPerTurn property doesn't exist
                int regenAmount = 1; // Default regeneration
                int actualRegen = Math.Min(regenAmount, player.GetEffectiveMaxHealth() - player.CurrentHealth);
                
                if (actualRegen > 0)
                {
                    player.Heal(actualRegen);
                    // Use TextDisplayIntegration for consistent entity tracking
                    string regenMessage = $"[{player.Name}] regenerates {actualRegen} health ({player.CurrentHealth}/{player.GetEffectiveMaxHealth()})";
                    TextDisplayIntegration.DisplayCombatAction(regenMessage, new List<string>(), new List<string>(), player.Name);
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
                // Use TextDisplayIntegration for consistent entity tracking
                TextDisplayIntegration.DisplayCombatAction(evt, new List<string>(), new List<string>(), entity.Name);
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

        /// <summary>
        /// Records an action and increments the turn counter if needed
        /// Turns represent rounds of combat, not individual actions
        /// </summary>
        /// <param name="entityName">Name of the entity that performed the action</param>
        /// <param name="actionName">Name of the action performed</param>
        /// <returns>True if a new turn was reached, false otherwise</returns>
        public bool RecordAction(string entityName, string actionName)
        {
            actionCount++;
            
            // Only increment turn number every 10 actions to represent combat rounds
            // This prevents every action from being treated as a separate turn
            if (actionCount % 10 == 0)
            {
                turnNumber++;
                return true; // New turn reached
            }
            
            return false; // Still in the same turn
        }

        /// <summary>
        /// Gets the current turn number
        /// </summary>
        /// <returns>Current turn number</returns>
        public int GetCurrentTurn()
        {
            return turnNumber;
        }

        /// <summary>
        /// Gets the current action count within the current turn
        /// Returns the number of actions since the last turn increment
        /// </summary>
        /// <returns>Current action count within the current turn</returns>
        public int GetCurrentActionCount()
        {
            return actionCount % 10; // Actions within current turn (0-9)
        }

        /// <summary>
        /// Gets the total number of actions performed in the battle
        /// </summary>
        /// <returns>Total action count</returns>
        public int GetTotalActionCount()
        {
            return actionCount;
        }

        /// <summary>
        /// Gets turn information for display
        /// </summary>
        /// <returns>Formatted turn information string</returns>
        public string GetTurnInfo()
        {
            return $"Turn {turnNumber}";
        }
    }
}
