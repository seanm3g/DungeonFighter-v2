using System;
using System.Collections.Generic;
using RPGGame.UI.ColorSystem;

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
        
        // Turn counter system - increments with every action
        private int actionCount = 0;
        private int turnNumber = 0;

        /// <summary>
        /// Initializes the turn manager for a new battle
        /// </summary>
        public void InitializeBattle()
        {
            actionSpeedSystem = new ActionSpeedSystem();
            healthTracker = new BattleHealthTracker();
            lastPlayerAction = null;
            actionCount = 0;
            turnNumber = 0;
        }

        /// <summary>
        /// Cleans up the turn manager after battle
        /// NOTE: Does NOT reset turn/action counters - they're needed for post-battle metrics
        /// These will be reset when InitializeBattle() is called for the next battle
        /// </summary>
        public void EndBattle()
        {
            actionSpeedSystem = null;
            healthTracker = null;
            lastPlayerAction = null;
            // NOTE: We do NOT reset actionCount and turnNumber here
            // They need to be available for post-battle metrics calculation
            // They will be reset in InitializeBattle() for the next battle
        }

        /// <summary>
        /// Determines turn order based on action speeds
        /// </summary>
        /// <param name="player">The player character</param>
        /// <param name="enemy">The enemy</param>
        /// <param name="playerAction">The player's chosen action</param>
        /// <param name="enemyAction">The enemy's action</param>
        /// <returns>List of entities in turn order</returns>
        public List<Actor> DetermineTurnOrder(Character player, Enemy enemy, Action playerAction, Action enemyAction)
        {
            if (actionSpeedSystem == null)
                throw new InvalidOperationException("TurnManager not initialized. Call InitializeBattle() first.");

            // Use ActionSpeedSystem to determine turn order based on action speeds
            var turnOrder = new List<Actor>();
            
            // Get the next Actor to act from the speed system
            var nextEntity = actionSpeedSystem.GetNextEntityToAct();
            if (nextEntity != null)
            {
                turnOrder.Add(nextEntity);
                
                // Add the other Actor
                if (nextEntity == player)
                {
                    turnOrder.Add(enemy);
                }
                else
                {
                    turnOrder.Add(player);
                }
            }
            else
            {
                // Fallback: return player first if speed system doesn't have entities ready
                turnOrder.Add(player);
                turnOrder.Add(enemy);
            }
            
            return turnOrder;
        }

        /// <summary>
        /// Executes a turn for an Actor
        /// </summary>
        /// <param name="Actor">The Actor taking the turn</param>
        /// <param name="target">The target Actor</param>
        /// <param name="action">The action to execute</param>
        /// <param name="environment">The environment affecting the action</param>
        /// <param name="battleNarrative">The battle narrative to add events to</param>
        /// <returns>True if the Actor is still alive after the turn</returns>
        public bool ExecuteTurn(Actor Actor, Actor target, Action action, Environment? environment = null, BattleNarrative? battleNarrative = null)
        {
            if (actionSpeedSystem == null || healthTracker == null)
                throw new InvalidOperationException("TurnManager not initialized. Call InitializeBattle() first.");

            // Execute the action using the new ColoredText system
            var ((actionText, rollInfo), statusEffects) = CombatResults.ExecuteActionWithUIAndStatusEffectsColored(Actor, target, action, environment, lastPlayerAction, battleNarrative);
            if (!DisableCombatUIOutput)
            {
                // Use TextDisplayIntegration for consistent Actor tracking with ColoredText
                TextDisplayIntegration.DisplayCombatAction(actionText, rollInfo, statusEffects, null);
            }

            // Track last player action for DEJA VU functionality
            if (Actor is Character)
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
            Combat.Turn.StatusEffectProcessor.ProcessDamageOverTimeEffects(player, enemy);
        }

        /// <summary>
        /// Processes regeneration effects for the player
        /// </summary>
        /// <param name="player">The player character</param>
        public void ProcessRegeneration(Character player)
        {
            if (player.CurrentHealth < player.GetEffectiveMaxHealth())
            {
                // Get regeneration from equipment bonuses (same approach as CombatTurnHandlerSimplified)
                int regenAmount = player.GetEquipmentHealthRegenBonus();
                if (regenAmount > 0)
                {
                    int oldHealth = player.CurrentHealth;
                    // Use negative damage to heal (TakeDamage with negative value heals)
                    player.TakeDamage(-regenAmount);
                    // Cap at max health
                    if (player.CurrentHealth > player.GetEffectiveMaxHealth())
                    {
                        player.TakeDamage(player.CurrentHealth - player.GetEffectiveMaxHealth());
                    }
                    int actualRegen = player.CurrentHealth - oldHealth;
                    
                    if (actualRegen > 0)
                    {
                        // Use ColoredText for regeneration message
                        var regenText = CombatFlowColoredText.FormatHealthRegenerationColored(
                            player.Name, actualRegen, player.CurrentHealth, player.GetEffectiveMaxHealth());
                        TextDisplayIntegration.DisplayCombatAction(regenText, new List<ColoredText>(), null, null);
                        UIManager.WriteLine(""); // Add blank line after regeneration message
                    }
                }
            }
        }

        /// <summary>
        /// Checks health milestones and adds battle events
        /// </summary>
        /// <param name="Actor">The Actor to check</param>
        /// <param name="damageAmount">The amount of damage taken</param>
        public void CheckHealthMilestones(Actor Actor, int damageAmount)
        {
            Combat.Turn.HealthMilestoneTracker.CheckHealthMilestones(Actor, damageAmount, healthTracker);
        }

        /// <summary>
        /// Gets pending health notifications
        /// </summary>
        /// <returns>List of pending health notifications</returns>
        public List<string> GetPendingHealthNotifications()
        {
            // Health notifications are handled by BattleHealthTracker.CheckHealthMilestones()
            // which returns events directly. This method is kept for compatibility but returns empty list
            // as health milestone events are processed immediately when CheckHealthMilestones is called.
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
        public void InitializeHealthTracker(List<Actor> participants)
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
        /// Records an action and increments the turn counter
        /// Only hero actions increment the turn counter (hero action + enemy response = 1 turn)
        /// </summary>
        /// <param name="entityName">Name of the Actor that performed the action</param>
        /// <param name="actionName">Name of the action performed</param>
        /// <returns>True if a new turn was reached, false otherwise</returns>
        public bool RecordAction(string entityName, string actionName)
        {
            actionCount++;

            // Only increment turn counter for player/hero actions
            // Enemy actions don't increment the turn (hero + enemy response = 1 turn)
            if (IsPlayerAction(entityName))
            {
                turnNumber++;
                return true; // New player turn reached
            }
            return false; // Enemy action, no turn increment
        }

        /// <summary>
        /// Checks if the action is from the player/hero (not enemy or environment)
        /// This is a heuristic: player names are stored when battle starts via StartBattleNarrative
        /// For now, we'll identify players by excluding known enemy/environment keywords
        /// </summary>
        private bool IsPlayerAction(string entityName)
        {
            // Enemy names typically contain keywords like "Enemy", and monster type names
            // Environment names are room/area names
            // Player names are character names, typically stored as "Character" or custom names
            //
            // We'll exclude obvious non-player types:
            // - Anything with "Enemy"
            // - Room/chamber names
            // - Environmental entities

            // For now, use a whitelist of known player name patterns
            // This is not ideal, but works until we have better actor type identification
            if (entityName.Contains("Player") || entityName.Contains("Character") ||
                entityName.Contains("Hero") || entityName.Contains("Test"))
            {
                return !entityName.Contains("Enemy"); // Exclude if it also contains "Enemy"
            }

            // If none of the above patterns match, exclude it (assume it's an enemy or environment)
            return false;
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



