using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.UI.ColorSystem;

namespace RPGGame
{
    /// <summary>
    /// Simplified combat turn handler using generic processors to eliminate duplication
    /// Replaces the original CombatTurnHandler with cleaner, more maintainable code
    /// </summary>
    public class CombatTurnHandlerSimplified
    {
        private readonly CombatStateManager stateManager;

        public CombatTurnHandlerSimplified(CombatStateManager stateManager)
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
                StunProcessor.ProcessStunnedEntity(player, stateManager);
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
                StunProcessor.ProcessStunnedEntity(currentEnemy, stateManager);
            }
            else
            {
                // Use the new ActionExecutor system for consistent action handling with ColoredText
                var ((actionText, rollInfo), statusEffects) = CombatResults.ExecuteActionWithUIAndStatusEffectsColored(
                    currentEnemy, player, null, room, 
                    stateManager.GetLastPlayerAction(), 
                    stateManager.GetCurrentBattleNarrative());
                
                bool textDisplayed = actionText != null && actionText.Count > 0;
                
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
                if (textDisplayed && actionText != null && rollInfo != null && battleNarrative != null)
                {
                    var narratives = battleNarrative.GetTriggeredNarratives();
                    // Convert narrative strings to ColoredText
                    var narrativeColored = new List<List<ColoredText>>();
                    foreach (var narrative in narratives)
                    {
                        if (!string.IsNullOrEmpty(narrative))
                        {
                            var parsed = ColoredTextParser.Parse(narrative);
                            if (parsed.Count > 0)
                            {
                                narrativeColored.Add(parsed);
                            }
                        }
                    }
                    // Display using the new ColoredText method
                    TextDisplayIntegration.DisplayCombatAction(actionText, rollInfo, statusEffects, narrativeColored);
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
                    var allTargets = new List<Actor> { player, currentEnemy };
                    
                    // Use area of effect action to target all characters
                    string result = EnvironmentalActionHandler.ExecuteAreaOfEffectAction(room, allTargets, room, envAction);
                    bool textDisplayed = !string.IsNullOrEmpty(result);
                    
                    // Add environmental action to narrative system
                    var battleNarrative = stateManager.GetCurrentBattleNarrative();
                    if (battleNarrative != null && textDisplayed)
                    {
                        battleNarrative.AddEnvironmentalAction(result);
                    }
                    
                    // Record the environmental action for turn counting
                    bool newTurn = stateManager.RecordAction(room.Name, envAction.Name);
                    if (newTurn && !CombatManager.DisableCombatUIOutput)
                    {
                        // Turn separator line removed for cleaner combat logs
                    }
                    
                    // Display environmental action result using block system
                    if (textDisplayed)
                    {
                        // Parse the environmental action to extract main text and effects
                        var lines = result.Split('\n');
                        string mainText = lines[0];
                        var effects = new List<string>();
                        
                        // Extract any effects from additional lines
                        for (int i = 1; i < lines.Length; i++)
                        {
                            if (!string.IsNullOrEmpty(lines[i].Trim()))
                            {
                                effects.Add(lines[i].Trim());
                            }
                        }
                        
                        BlockDisplayManager.DisplayEnvironmentalBlock(mainText, effects);
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
            // Use the new ColoredText system to execute the action
            var ((actionText, rollInfo), statusEffects) = CombatResults.ExecuteActionWithUIAndStatusEffectsColored(
                player, currentEnemy, null, room, 
                stateManager.GetLastPlayerAction(), 
                stateManager.GetCurrentBattleNarrative());
            
            bool textDisplayed = actionText != null && actionText.Count > 0;
            
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
            if (textDisplayed && actionText != null && rollInfo != null && battleNarrative != null)
            {
                var narratives = battleNarrative.GetTriggeredNarratives();
                // Convert narrative strings to ColoredText
                var narrativeColored = new List<List<ColoredText>>();
                foreach (var narrative in narratives)
                {
                    if (!string.IsNullOrEmpty(narrative))
                    {
                        var parsed = ColoredTextParser.Parse(narrative);
                        if (parsed.Count > 0)
                        {
                            narrativeColored.Add(parsed);
                        }
                    }
                }
                // Display using the new ColoredText method
                TextDisplayIntegration.DisplayCombatAction(actionText, rollInfo, statusEffects, narrativeColored);
            }
            
            // End turn for statistics tracking
            player.EndTurn();
            
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
                    // Use new ColoredText system for health regeneration
                    var coloredText = CombatFlowColoredText.FormatHealthRegenerationColored(
                        player.Name, actualRegen, player.CurrentHealth, player.GetEffectiveMaxHealth());
                    BlockDisplayManager.DisplaySystemBlock(coloredText);
                }
            }
        }
    }
}

