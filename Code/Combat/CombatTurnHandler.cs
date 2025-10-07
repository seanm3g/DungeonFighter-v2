using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Handles turn processing logic using the new ActionSelector system
    /// Replaces CombatTurnProcessor with a cleaner, more robust implementation
    /// </summary>
    public class CombatTurnHandler
    {
        private readonly CombatStateManager stateManager;

        public CombatTurnHandler(CombatStateManager stateManager)
        {
            this.stateManager = stateManager;
        }

        /// <summary>
        /// Processes a single player turn using the new ActionSelector system
        /// </summary>
        public bool ProcessPlayerTurn(Character player, Enemy currentEnemy, Environment room)
        {
            // Check if player is stunned
            if (player.StunTurnsRemaining > 0)
            {
                ProcessStunnedPlayer(player);
            }
            else
            {
                // Use the new ActionSelector system for action selection and execution
                ProcessPlayerAction(player, currentEnemy, room);
            }
            
            // Process health regeneration for player after they act
            ProcessPlayerHealthRegeneration(player);
            
            // Return false if enemy is dead (combat should end)
            return currentEnemy.IsAlive;
        }

        /// <summary>
        /// Processes a single enemy turn using the new ActionSelector system
        /// </summary>
        public bool ProcessEnemyTurn(Character player, Enemy currentEnemy, Environment room)
        {
            // Check if enemy is stunned
            if (currentEnemy.StunTurnsRemaining > 0)
            {
                ProcessStunnedEnemy(currentEnemy);
            }
            else
            {
                // Use the new ActionExecutor system for consistent action handling
                var (result, statusEffects) = CombatResults.ExecuteActionWithUIAndStatusEffects(
                    currentEnemy, player, null, room, 
                    stateManager.GetLastPlayerAction(), 
                    stateManager.GetCurrentBattleNarrative());
                
                bool textDisplayed = !string.IsNullOrEmpty(result);
                
                // Get the action that was actually used for turn counting
                var usedAction = ActionExecutor.GetLastUsedAction(currentEnemy);
                string actionName = usedAction?.Name ?? "UNKNOWN ACTION";
                
                // Record the action for turn counting
                bool newTurn = stateManager.RecordAction(currentEnemy.Name, actionName);
                if (newTurn && !CombatManager.DisableCombatUIOutput)
                {
                    // Turn separator line removed for cleaner combat logs
                }
                
                // Get triggered narratives and display everything together
                var battleNarrative = stateManager.GetCurrentBattleNarrative();
                if (textDisplayed && battleNarrative != null)
                {
                    var narratives = battleNarrative.GetTriggeredNarratives();
                    // Add proper indentation to status effects
                    var indentedStatusEffects = statusEffects.Select(effect => $"    {effect}").ToList();
                    TextDisplayIntegration.DisplayCombatAction(result, narratives, indentedStatusEffects, currentEnemy.Name);
                }
                
                // Update enemy's action timing in the action speed system
                var actionSpeedSystem = stateManager.GetCurrentActionSpeedSystem();
                if (actionSpeedSystem != null && usedAction != null)
                {
                    actionSpeedSystem.ExecuteAction(currentEnemy, usedAction);
                }
            }
            
            // Return false if player is dead (combat should end)
            if (!player.IsAlive)
            {
                return false;
            }
            
            // Process poison/bleed and burn damage after enemy's turn
            stateManager.ProcessDamageOverTimeEffects(player, currentEnemy);
            
            return true;
        }

        /// <summary>
        /// Processes a single environment turn
        /// </summary>
        public bool ProcessEnvironmentTurn(Character player, Enemy currentEnemy, Environment room)
        {
            if (room.ShouldEnvironmentAct())
            {
                var envAction = room.SelectAction();
                if (envAction != null)
                {
                    // Create list of all characters in the room (player and current enemy)
                    var allTargets = new List<Entity> { player, currentEnemy };
                    
                    // Use area of effect action to target all characters
                    string result = EnvironmentalActionHandler.ExecuteAreaOfEffectAction(room, allTargets, room, envAction);
                    bool textDisplayed = !string.IsNullOrEmpty(result);
                    
                    // Record the environmental action for turn counting
                    bool newTurn = stateManager.RecordAction(room.Name, envAction.Name);
                    if (newTurn && !CombatManager.DisableCombatUIOutput)
                    {
                        // Turn separator line removed for cleaner combat logs
                    }
                    
                    // Display environmental action result
                    if (textDisplayed)
                    {
                        // Use WriteCombatLine for proper entity tracking and spacing
                        UIManager.WriteCombatLine(result);
                        
                        // Don't add explicit blank line - let UIManager handle entity-based spacing
                    }
                    
                    // Update environment's action timing in the action speed system
                    var actionSpeedSystem = stateManager.GetCurrentActionSpeedSystem();
                    if (actionSpeedSystem != null)
                    {
                        actionSpeedSystem.ExecuteAction(room, envAction);
                    }
                    
                    // Return false if player is dead (combat should end)
                    if (!player.IsAlive)
                    {
                        return false;
                    }
                }
            }
            else
            {
                // Environment doesn't want to act, but we still need to advance its turn
                // to prevent it from getting stuck in the action speed system
                var actionSpeedSystem = stateManager.GetCurrentActionSpeedSystem();
                if (actionSpeedSystem != null)
                {
                    // Advance the environment's turn by a small amount to prevent infinite loops
                    actionSpeedSystem.AdvanceEntityTurn(room, 1.0);
                }
            }
            
            return true;
        }

        /// <summary>
        /// Processes a player action using the new ActionSelector system
        /// </summary>
        private void ProcessPlayerAction(Character player, Enemy currentEnemy, Environment room)
        {
            // Use the new ActionSelector system to select and execute the action
            var (result, statusEffects) = CombatResults.ExecuteActionWithUIAndStatusEffects(
                player, currentEnemy, null, room, 
                stateManager.GetLastPlayerAction(), 
                stateManager.GetCurrentBattleNarrative());
            
            bool textDisplayed = !string.IsNullOrEmpty(result);
            
            // Get the action that was actually used for turn counting
            var usedAction = ActionExecutor.GetLastUsedAction(player);
            string actionName = usedAction?.Name ?? "UNKNOWN ACTION";
            
            // Record the action for turn counting
            bool newTurn = stateManager.RecordAction(player.Name, actionName);
            if (newTurn && !CombatManager.DisableCombatUIOutput)
            {
                // Turn separator line removed for cleaner combat logs
            }
            
            // Get triggered narratives and display everything together
            var battleNarrative = stateManager.GetCurrentBattleNarrative();
            if (textDisplayed && battleNarrative != null)
            {
                var narratives = battleNarrative.GetTriggeredNarratives();
                // Add proper indentation to status effects
                var indentedStatusEffects = statusEffects.Select(effect => $"    {effect}").ToList();
                TextDisplayIntegration.DisplayCombatAction(result, narratives, indentedStatusEffects, player.Name);
            }
            
            // Update last player action for DEJA VU functionality
            if (usedAction != null)
            {
                stateManager.UpdateLastPlayerAction(usedAction);
            }
            
            // Update player's action timing in the action speed system
            var actionSpeedSystem = stateManager.GetCurrentActionSpeedSystem();
            if (actionSpeedSystem != null && usedAction != null)
            {
                actionSpeedSystem.ExecuteAction(player, usedAction);
            }
        }

        /// <summary>
        /// Processes a stunned player
        /// </summary>
        private void ProcessStunnedPlayer(Character player)
        {
            if (!CombatManager.DisableCombatUIOutput)
            {
                // Use WriteStunLine for configurable stun message handling (no indentation)
                UIManager.WriteStunLine($"[{player.Name}] is stunned and cannot act! ({player.StunTurnsRemaining} turns remaining)");
                // Don't add explicit blank line - let UIManager handle entity-based spacing
            }
            
            // Get the player's action speed to calculate proper stun reduction
            double playerActionSpeed = player.GetTotalAttackSpeed();
            
            // Update temp effects with action speed-based turn reduction
            player.UpdateTempEffects(playerActionSpeed / 10.0); // Normalize to turn-based system
            
            // Advance the player's turn in the action speed system based on their action speed
            var currentSpeedSystem = stateManager.GetCurrentActionSpeedSystem();
            if (currentSpeedSystem != null)
            {
                currentSpeedSystem.AdvanceEntityTurn(player, playerActionSpeed);
            }
        }

        /// <summary>
        /// Processes a stunned enemy
        /// </summary>
        private void ProcessStunnedEnemy(Enemy enemy)
        {
            if (!CombatManager.DisableCombatUIOutput)
            {
                // Use WriteStunLine for configurable stun message handling (no indentation)
                UIManager.WriteStunLine($"[{enemy.Name}] is stunned and cannot act! ({enemy.StunTurnsRemaining} turns remaining)");
                // Don't add explicit blank line - let UIManager handle entity-based spacing
            }
            
            // Get the enemy's action speed to calculate proper stun reduction
            double enemyActionSpeed = enemy.GetTotalAttackSpeed();
            
            // Update temp effects with action speed-based turn reduction
            enemy.UpdateTempEffects(enemyActionSpeed / 10.0); // Normalize to turn-based system
            
            // Advance the enemy's turn in the action speed system based on their action speed
            var currentSpeedSystem = stateManager.GetCurrentActionSpeedSystem();
            if (currentSpeedSystem != null)
            {
                currentSpeedSystem.AdvanceEntityTurn(enemy, enemyActionSpeed);
            }
        }

        /// <summary>
        /// Processes health regeneration for the player
        /// </summary>
        private void ProcessPlayerHealthRegeneration(Character player)
        {
            int playerHealthRegen = player.GetEquipmentHealthRegenBonus();
            if (playerHealthRegen > 0 && player.CurrentHealth < player.GetEffectiveMaxHealth())
            {
                int oldHealth = player.CurrentHealth;
                // Use negative damage to heal (TakeDamage with negative value heals)
                player.TakeDamage(-playerHealthRegen);
                // Cap at max health
                if (player.CurrentHealth > player.GetEffectiveMaxHealth())
                {
                    player.TakeDamage(player.CurrentHealth - player.GetEffectiveMaxHealth());
                }
                int actualRegen = player.CurrentHealth - oldHealth;
                if (actualRegen > 0 && !CombatManager.DisableCombatUIOutput)
                {
                    UIManager.WriteLine($"[{player.Name}] regenerates {actualRegen} health ({player.CurrentHealth}/{player.GetEffectiveMaxHealth()})");
                }
            }
        }
    }
}
