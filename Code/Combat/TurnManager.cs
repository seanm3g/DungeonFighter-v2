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
            bool blankLineAdded = false;
            
            // Process effects for player
            var playerResults = new List<string>();
            int playerDamage = CombatEffectsSimplified.ProcessStatusEffects(player, playerResults);
            if (playerDamage > 0)
            {
                player.TakeDamage(playerDamage);
            }
            if (playerResults.Count > 0)
            {
                // Apply spacing for poison damage (context-aware)
                if (!DisableCombatUIOutput)
                {
                    TextSpacingSystem.ApplySpacingBefore(TextSpacingSystem.BlockType.PoisonDamage);
                    blankLineAdded = true;
                }
                
                // Group related messages together - display damage and status effects as one block
                var damageMessages = new List<string>();
                var statusMessages = new List<string>();
                
                for (int i = 0; i < playerResults.Count; i++)
                {
                    var result = playerResults[i];
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
                    BlockDisplayManager.DisplaySystemBlock(combinedMessage);
                    // Record as poison damage block for spacing system
                    TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.PoisonDamage);
                }
                else if (damageMessages.Count > 0)
                {
                    // Only damage messages
                    foreach (var damage in damageMessages)
                    {
                        BlockDisplayManager.DisplaySystemBlock(damage);
                    }
                    // Record as poison damage block for spacing system
                    TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.PoisonDamage);
                }
                else if (statusMessages.Count > 0)
                {
                    // Only status messages
                    foreach (var status in statusMessages)
                    {
                        BlockDisplayManager.DisplaySystemBlock(status);
                    }
                    // Record as poison damage block for spacing system
                    TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.PoisonDamage);
                }
            }
            
            // Process effects for enemy (only if living)
            if (enemy.IsLiving)
            {
                var enemyResults = new List<string>();
                int enemyDamage = CombatEffectsSimplified.ProcessStatusEffects(enemy, enemyResults);
                if (enemyDamage > 0)
                {
                    enemy.TakeDamage(enemyDamage);
                }
                if (enemyResults.Count > 0)
                {
                    // Apply spacing for poison damage (context-aware, avoids double spacing)
                    if (!DisableCombatUIOutput && !blankLineAdded)
                    {
                        TextSpacingSystem.ApplySpacingBefore(TextSpacingSystem.BlockType.PoisonDamage);
                    }
                    
                    // Group related messages together - display damage and status effects as one block
                    var damageMessages = new List<string>();
                    var statusMessages = new List<string>();
                    
                    for (int i = 0; i < enemyResults.Count; i++)
                    {
                        var result = enemyResults[i];
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
                        BlockDisplayManager.DisplaySystemBlock(combinedMessage);
                        // Record as poison damage block for spacing system
                        TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.PoisonDamage);
                    }
                    else if (damageMessages.Count > 0)
                    {
                        // Only damage messages
                        foreach (var damage in damageMessages)
                        {
                            BlockDisplayManager.DisplaySystemBlock(damage);
                        }
                        // Record as poison damage block for spacing system
                        TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.PoisonDamage);
                    }
                    else if (statusMessages.Count > 0)
                    {
                        // Only status messages
                        foreach (var status in statusMessages)
                        {
                            BlockDisplayManager.DisplaySystemBlock(status);
                        }
                        // Record as poison damage block for spacing system
                        TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.PoisonDamage);
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
                    // Use ColoredText for regeneration message
                    var regenText = CombatFlowColoredText.FormatHealthRegenerationColored(
                        player.Name, actualRegen, player.CurrentHealth, player.GetEffectiveMaxHealth());
                    TextDisplayIntegration.DisplayCombatAction(regenText, new List<ColoredText>(), null, null);
                    UIManager.WriteLine(""); // Add blank line after regeneration message
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
            if (healthTracker == null)
                return;

            var events = healthTracker.CheckHealthMilestones(Actor, damageAmount);
            foreach (var evt in events)
            {
                // Parse string message to ColoredText for consistent display
                var coloredEvent = ColoredTextParser.Parse(evt);
                if (coloredEvent.Count > 0)
                {
                    TextDisplayIntegration.DisplayCombatAction(coloredEvent, new List<ColoredText>(), null, null);
                }
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
        /// Records an action and increments the turn counter if needed
        /// Turns represent rounds of combat, not individual actions
        /// </summary>
        /// <param name="entityName">Name of the Actor that performed the action</param>
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



