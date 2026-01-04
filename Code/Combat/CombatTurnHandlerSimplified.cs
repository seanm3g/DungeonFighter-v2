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
        public async System.Threading.Tasks.Task<bool> ProcessPlayerTurnAsync(Character player, Enemy currentEnemy, Environment room)
        {
            // Check if player is stunned
            if (player.StunTurnsRemaining > 0)
            {
                StunProcessor.ProcessStunnedEntity(player, stateManager);
            }
            else
            {
                // Use the new ActionSelector system for action selection and execution
                await ProcessPlayerActionAsync(player, currentEnemy, room);
            }
            
            // Process health regeneration for player after they act
            ProcessPlayerHealthRegeneration(player);
            
            // Return false if enemy is dead (combat should end)
            return currentEnemy.IsAlive;
        }

        /// <summary>
        /// Processes a single enemy turn using the new ActionSelector system
        /// </summary>
        public async System.Threading.Tasks.Task<bool> ProcessEnemyTurnAsync(Character player, Enemy currentEnemy, Environment room)
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
                // Only retrieve significant narratives (not every critical hit)
                var battleNarrative = stateManager.GetCurrentBattleNarrative();
                if (textDisplayed && actionText != null && rollInfo != null && battleNarrative != null)
                {
                    var narratives = battleNarrative.GetTriggeredNarrativesIfSignificant();
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
                    // Display using the new ColoredText method (async to wait for display delay)
                    // Pass player character to filter display for multi-character support (enemy actions are part of player's combat)
                    await TextDisplayIntegration.DisplayCombatActionAsync(actionText, rollInfo, statusEffects, narrativeColored, player);
                }
                
                // Update enemy's action timing in the action speed system
                // CRITICAL: Always update action speed system, even if no action was selected
                // This prevents infinite loops when ActionSelector returns null
                var actionSpeedSystem = stateManager.GetCurrentActionSpeedSystem();
                if (actionSpeedSystem != null)
                {
                    if (usedAction != null)
                    {
                        bool isCriticalMiss = ActionExecutor.GetLastCriticalMissStatus(currentEnemy);
                        actionSpeedSystem.ExecuteAction(currentEnemy, usedAction, isCriticalMiss: isCriticalMiss);
                    }
                    else
                    {
                        // No action was selected (empty ActionPool or stunned) - still advance turn to prevent infinite loop
                        DebugLogger.WriteCombatDebug("CombatTurnHandlerSimplified", $"Enemy {currentEnemy.Name} had no action selected - advancing turn to prevent infinite loop");
                        actionSpeedSystem.AdvanceEntityTurn(currentEnemy, 1.0);
                    }
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
                    
                    // Use structured environmental action execution
                    var ((actionText, rollInfo), statusEffects) = EnvironmentalActionHandler.ExecuteEnvironmentalActionStructured(room, allTargets, envAction, room);
                    bool textDisplayed = actionText != null && actionText.Count > 0;
                    
                    // Add environmental action to narrative system (convert to string for backward compatibility)
                    var battleNarrative = stateManager.GetCurrentBattleNarrative();
                    if (battleNarrative != null && textDisplayed && actionText != null)
                    {
                        string resultString = UI.ColorSystem.ColoredTextRenderer.RenderAsPlainText(actionText);
                        if (rollInfo != null && rollInfo.Count > 0)
                        {
                            resultString += "\n" + UI.ColorSystem.ColoredTextRenderer.RenderAsPlainText(rollInfo);
                        }
                        foreach (var effect in statusEffects)
                        {
                            if (effect != null && effect.Count > 0)
                            {
                                resultString += "\n" + UI.ColorSystem.ColoredTextRenderer.RenderAsPlainText(effect);
                            }
                        }
                        battleNarrative.AddEnvironmentalAction(resultString);
                    }
                    
                    // Record the environmental action for turn counting
                    bool newTurn = stateManager.RecordAction(room.Name, envAction.Name);
                    if (newTurn && !CombatManager.DisableCombatUIOutput)
                    {
                        // Turn separator line removed for cleaner combat logs
                    }
                    
                    // Display environmental action result using unified action block system
                    if (textDisplayed && actionText != null)
                    {
                        // Use empty list if rollInfo is null (DisplayActionBlock expects non-nullable rollInfo)
                        var rollInfoToDisplay = rollInfo ?? new List<UI.ColorSystem.ColoredText>();
                        BlockDisplayManager.DisplayActionBlock(actionText, rollInfoToDisplay, statusEffects, null, null, null);
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
        private async System.Threading.Tasks.Task ProcessPlayerActionAsync(Character player, Enemy currentEnemy, Environment room)
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
            // Only retrieve significant narratives (not every critical hit)
            var battleNarrative = stateManager.GetCurrentBattleNarrative();
            if (textDisplayed && actionText != null && rollInfo != null && battleNarrative != null)
            {
                var narratives = battleNarrative.GetTriggeredNarrativesIfSignificant();
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
                // Display using the new ColoredText method (async to wait for display delay)
                // Pass player character to filter display for multi-character support
                await TextDisplayIntegration.DisplayCombatActionAsync(actionText, rollInfo, statusEffects, narrativeColored, player);
            }
                
                // End turn for statistics tracking
            player.EndTurn();
            
            // Update last player action for DEJA VU functionality
            if (usedAction != null)
            {
                stateManager.UpdateLastPlayerAction(usedAction);
            }
            
            // Update player's action timing in the action speed system
            // CRITICAL: Always update action speed system, even if no action was selected
            // This prevents infinite loops when ActionSelector returns null
            var actionSpeedSystem = stateManager.GetCurrentActionSpeedSystem();
            if (actionSpeedSystem != null)
            {
                if (usedAction != null)
                {
                    bool isCriticalMiss = ActionExecutor.GetLastCriticalMissStatus(player);
                    actionSpeedSystem.ExecuteAction(player, usedAction, isCriticalMiss: isCriticalMiss);
                }
                else
                {
                    // No action was selected (empty ActionPool or stunned) - still advance turn to prevent infinite loop
                    DebugLogger.WriteCombatDebug("CombatTurnHandlerSimplified", $"Player {player.Name} had no action selected - advancing turn to prevent infinite loop");
                    actionSpeedSystem.AdvanceEntityTurn(player, 1.0);
                }
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
                    UIManager.WriteLine(""); // Add blank line after regeneration message
                }
            }
        }
    }
}

