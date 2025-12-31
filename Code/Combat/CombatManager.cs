using System.Linq;
using System;
using System.IO;
using System.Text.Json;

namespace RPGGame
{
    /// <summary>
    /// Manages combat loop logic including turn-based combat, effect processing, and combat state
    /// Refactored from 687 lines to a clean orchestrator using specialized managers
    /// </summary>
    public class CombatManager
    {
        // Flag to disable UI output during balance analysis
        public static bool DisableCombatUIOutput = false;

        // Flag to disable battle narrative tracking (separate from UI output)
        // This is used to disable logging of battle events for memory efficiency
        // By default, narrative is always tracked since it's needed for metrics
        public static bool DisableBattleNarrative = false;

        // Specialized managers using composition pattern
        private readonly CombatStateManager stateManager;
        private readonly CombatTurnHandlerSimplified turnHandler;

        public CombatManager()
        {
            stateManager = new CombatStateManager();
            turnHandler = new CombatTurnHandlerSimplified(stateManager);
        }

        /// <summary>
        /// Starts battle narrative and initializes combat state
        /// </summary>
        public void StartBattleNarrative(string playerName, string enemyName, string locationName, int playerHealth, int enemyHealth)
        {
            stateManager.StartBattleNarrative(playerName, enemyName, locationName, playerHealth, enemyHealth);
        }

        /// <summary>
        /// Ends battle narrative and cleans up combat state
        /// </summary>
        public void EndBattleNarrative()
        {
            stateManager.EndBattleNarrative();
        }

        /// <summary>
        /// Ends the battle narrative with final health values from actual entities
        /// </summary>
        public void EndBattleNarrative(Character player, Enemy enemy)
        {
            stateManager.EndBattleNarrative(player, enemy);
        }

        /// <summary>
        /// Gets the current battle narrative
        /// </summary>
        public BattleNarrative? GetCurrentBattleNarrative()
        {
            return stateManager.GetCurrentBattleNarrative();
        }

        /// <summary>
        /// Gets the current action speed system
        /// </summary>
        public ActionSpeedSystem? GetCurrentActionSpeedSystem()
        {
            return stateManager.GetCurrentActionSpeedSystem();
        }

        /// <summary>
        /// Initializes combat entities in the action speed system
        /// </summary>
        public void InitializeCombatEntities(Character player, Enemy enemy, Environment? environment = null, bool playerGetsFirstAttack = false, bool enemyGetsFirstAttack = false)
        {
            stateManager.InitializeCombatEntities(player, enemy, environment, playerGetsFirstAttack, enemyGetsFirstAttack);
        }

        /// <summary>
        /// Gets the next entity that should act based on action speed
        /// </summary>
        public Actor? GetNextEntityToAct()
        {
            return stateManager.GetNextEntityToAct();
        }

        /// <summary>
        /// Updates the last player action for DEJA VU functionality
        /// </summary>
        public void UpdateLastPlayerAction(Action action)
        {
            stateManager.UpdateLastPlayerAction(action);
        }

        /// <summary>
        /// Gets the last player action for DEJA VU functionality
        /// </summary>
        public Action? GetLastPlayerAction()
        {
            return stateManager.GetLastPlayerAction();
        }

        /// <summary>
        /// Gets the current turn information for display
        /// </summary>
        public string GetTurnInfo()
        {
            return stateManager.GetTurnInfo();
        }

        /// <summary>
        /// Gets the total number of actions performed in the current battle
        /// </summary>
        public int GetTotalActionCount()
        {
            return stateManager.GetTotalActionCount();
        }

        /// <summary>
        /// Gets the current turn number (turns increment every 10 actions)
        /// </summary>
        public int GetCurrentTurn()
        {
            return stateManager.GetCurrentTurn();
        }

        /// <summary>
        /// Gets the fun moment tracker for this combat
        /// </summary>
        public FunMomentTracker? GetFunMomentTracker()
        {
            return stateManager.GetFunMomentTracker();
        }

        /// <summary>
        /// Runs the main combat loop between player and enemy
        /// </summary>
        /// <param name="player">The player character</param>
        /// <param name="currentEnemy">The current enemy</param>
        /// <param name="room">The current room/environment</param>
        /// <param name="playerGetsFirstAttack">If true, player attacks first (from exploration)</param>
        /// <param name="enemyGetsFirstAttack">If true, enemy attacks first (surprise from exploration)</param>
        /// <returns>True if combat completed successfully, false if player died</returns>
        public async Task<bool> RunCombat(Character player, Enemy currentEnemy, Environment room, bool playerGetsFirstAttack = false, bool enemyGetsFirstAttack = false)
        {
            // Reset game time FIRST to ensure clean timing state
            GameTicker.Instance.Reset();
            
            // Reset combo step to 0 to start at the first action in the sequence
            player.ComboStep = 0;
            
            // Start battle narrative and initialize action speed system
            StartBattleNarrative(player.Name, currentEnemy.Name, room.Name, player.CurrentHealth, currentEnemy.CurrentHealth);
            
            // Initialize combat entities AFTER action speed system is created
            InitializeCombatEntities(player, currentEnemy, room, playerGetsFirstAttack, enemyGetsFirstAttack);
            
            // Reset environment action count for new fight
            room.ResetForNewFight();

            // Combat Loop with action speed system
            int maxTurns = 1000; // Safety limit to prevent infinite loops
            int turnCount = 0;
            int nullEntityCount = 0; // Track consecutive null entities
            const int MAX_NULL_ENTITIES = 100; // Max consecutive null entities before breaking
            
            while (player.IsAlive && currentEnemy.IsAlive)
            {
                // Safety check: prevent infinite loops
                turnCount++;
                if (turnCount > maxTurns)
                {
                    var errorText = CombatFlowColoredText.FormatSystemErrorColored($"Combat loop exceeded {maxTurns} turns. Breaking to prevent infinite loop.");
                    BlockDisplayManager.DisplaySystemBlock(errorText);
                    DebugLogger.WriteCombatDebug("CombatManager", $"Combat loop exceeded max turns: {turnCount}");
                    break;
                }
                
                // Get the next entity that should act based on action speed
                Actor? nextEntity = GetNextEntityToAct();
                
                if (nextEntity == null)
                {
                    nullEntityCount++;
                    
                    // Safety check: if we've had too many consecutive null entities, break
                    if (nullEntityCount > MAX_NULL_ENTITIES)
                    {
                        var errorText = CombatFlowColoredText.FormatSystemErrorColored($"Combat loop stuck: {nullEntityCount} consecutive null entities. Breaking to prevent infinite loop.");
                        BlockDisplayManager.DisplaySystemBlock(errorText);
                        DebugLogger.WriteCombatDebug("CombatManager", $"Combat loop stuck with {nullEntityCount} consecutive null entities");
                        break;
                    }
                    
                    // No entities ready, advance time to the next entity's action time
                    var currentSpeedSystem = GetCurrentActionSpeedSystem();
                    if (currentSpeedSystem != null)
                    {
                        // Find the next entity that will be ready and advance time to that point
                        var nextReadyTime = currentSpeedSystem.GetNextReadyTime();
                        if (nextReadyTime > 0)
                        {
                            double timeToAdvance = nextReadyTime - currentSpeedSystem.GetCurrentTime();
                            if (timeToAdvance > 0)
                            {
                                currentSpeedSystem.AdvanceTime(timeToAdvance);
                            }
                            else
                            {
                                // Fallback: advance time slightly
                                currentSpeedSystem.AdvanceTime(0.1);
                            }
                        }
                        else if (nextReadyTime == -1.0)
                        {
                            // No entities exist in the action speed system - this shouldn't happen
                            var errorText = CombatFlowColoredText.FormatSystemErrorColored("No entities in action speed system. Breaking combat loop.");
                            BlockDisplayManager.DisplaySystemBlock(errorText);
                            DebugLogger.WriteCombatDebug("CombatManager", "No entities in action speed system");
                            break;
                        }
                        else
                        {
                            // No entities ready and no next ready time - this shouldn't happen
                            var errorText = CombatFlowColoredText.FormatSystemErrorColored("No entities ready and no next ready time. Breaking combat loop.");
                            BlockDisplayManager.DisplaySystemBlock(errorText);
                            DebugLogger.WriteCombatDebug("CombatManager", "No entities ready and no next ready time");
                            break;
                        }
                    }
                    else
                    {
                        // ActionSpeedSystem is null - this shouldn't happen
                        DebugLogger.WriteCombatDebug("CombatManager", "ActionSpeedSystem is null");
                        break;
                    }
                    continue;
                }
                
                // Reset null entity counter when we get a valid entity
                nullEntityCount = 0;

                // Handle entity change detection and add blank lines when needed
                stateManager.HandleEntityChange(nextEntity);

                // Player acts
                if (nextEntity == player && player.IsAlive)
                {
                    bool combatContinues = await turnHandler.ProcessPlayerTurnAsync(player, currentEnemy, room);
                    if (!combatContinues)
                    {
                        break; // Combat ended
                    }
                    
                    // Add delay after player action
                    await CombatDelayManager.DelayAfterActionAsync();
                }
                // Enemy acts
                else if (nextEntity == currentEnemy && currentEnemy.IsAlive)
                {
                    bool combatContinues = await turnHandler.ProcessEnemyTurnAsync(player, currentEnemy, room);
                    if (!combatContinues)
                    {
                        break; // Combat ended
                    }
                    
                    // Add delay after enemy action
                    await CombatDelayManager.DelayAfterActionAsync();
                }
                // Environment acts
                else if (nextEntity == room && room.IsHostile && room.ActionPool.Count > 0)
                {
                    if (!turnHandler.ProcessEnvironmentTurn(player, currentEnemy, room))
                    {
                        break; // Combat ended
                    }
                    
                    // Add delay after environmental action
                    await CombatDelayManager.DelayAfterActionAsync();
                }
                else
                {
                    // Unknown entity type - this shouldn't happen, but handle it gracefully
                    DebugLogger.WriteCombatDebug("CombatManager", $"Unknown entity type in combat loop: {nextEntity?.GetType().Name ?? "null"}");
                    // Advance time slightly to prevent infinite loop
                    var currentSpeedSystem = GetCurrentActionSpeedSystem();
                    if (currentSpeedSystem != null && nextEntity != null)
                    {
                        currentSpeedSystem.AdvanceEntityTurn(nextEntity, 1.0);
                    }
                    else
                    {
                        // If we can't advance, break to prevent infinite loop
                        var errorText = CombatFlowColoredText.FormatSystemErrorColored($"Unknown entity in combat loop. Breaking to prevent infinite loop.");
                        BlockDisplayManager.DisplaySystemBlock(errorText);
                        break;
                    }
                }
            }
            
            // End the battle narrative with final health values
            EndBattleNarrative(player, currentEnemy);
            
            // Reset combo step to first action at the end of combat
            player.ComboStep = 0;
            
            DebugLogger.WriteCombatDebug("CombatManager", $"Combat ended: {player.Name} {(player.IsAlive ? "survived" : "died")} vs {currentEnemy.Name}");
            
            // Return true if player survived, false if player died
            return player.IsAlive;
        }
    }
}


