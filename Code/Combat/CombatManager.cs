using System.Linq;

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
        public void InitializeCombatEntities(Character player, Enemy enemy, Environment? environment = null)
        {
            stateManager.InitializeCombatEntities(player, enemy, environment);
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
        /// Runs the main combat loop between player and enemy
        /// </summary>
        /// <param name="player">The player character</param>
        /// <param name="currentEnemy">The current enemy</param>
        /// <param name="room">The current room/environment</param>
        /// <returns>True if combat completed successfully, false if player died</returns>
        public bool RunCombat(Character player, Enemy currentEnemy, Environment room)
        {
            DebugLogger.WriteCombatDebug("CombatManager", $"Starting combat: {player.Name} vs {currentEnemy.Name} in {room.Name}");
            
            // Reset game time FIRST to ensure clean timing state
            GameTicker.Instance.Reset();
            
            // Start battle narrative and initialize action speed system
            StartBattleNarrative(player.Name, currentEnemy.Name, room.Name, player.CurrentHealth, currentEnemy.CurrentHealth);
            
            // Initialize combat entities AFTER action speed system is created
            InitializeCombatEntities(player, currentEnemy, room);
            
            // Reset environment action count for new fight
            room.ResetForNewFight();

            // Combat Loop with action speed system
            while (player.IsAlive && currentEnemy.IsAlive)
            {
                // Get the next entity that should act based on action speed
                Actor? nextEntity = GetNextEntityToAct();
                
                if (nextEntity == null)
                {
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
                            break;
                        }
                        else
                        {
                            // No entities ready and no next ready time - this shouldn't happen
                            var errorText = CombatFlowColoredText.FormatSystemErrorColored("No entities ready and no next ready time. Breaking combat loop.");
                            BlockDisplayManager.DisplaySystemBlock(errorText);
                            break;
                        }
                    }
                    else
                    {
                        // ActionSpeedSystem is null - this shouldn't happen
                        break;
                    }
                    continue;
                }

                // Handle entity change detection and add blank lines when needed
                stateManager.HandleEntityChange(nextEntity);

                // Player acts
                if (nextEntity == player && player.IsAlive)
                {
                    if (!turnHandler.ProcessPlayerTurn(player, currentEnemy, room))
                    {
                        break; // Combat ended
                    }
                    
                    // Add delay after player action
                    CombatDelayManager.DelayAfterAction();
                }
                // Enemy acts
                else if (nextEntity == currentEnemy && currentEnemy.IsAlive)
                {
                    if (!turnHandler.ProcessEnemyTurn(player, currentEnemy, room))
                    {
                        break; // Combat ended
                    }
                    
                    // Add delay after enemy action
                    CombatDelayManager.DelayAfterAction();
                }
                // Environment acts
                else if (nextEntity == room && room.IsHostile && room.ActionPool.Count > 0)
                {
                    if (!turnHandler.ProcessEnvironmentTurn(player, currentEnemy, room))
                    {
                        break; // Combat ended
                    }
                    
                    // Add delay after environmental action
                    CombatDelayManager.DelayAfterAction();
                }
            }
            
            // End the battle narrative with final health values
            EndBattleNarrative(player, currentEnemy);
            
            DebugLogger.WriteCombatDebug("CombatManager", $"Combat ended: {player.Name} {(player.IsAlive ? "survived" : "died")} vs {currentEnemy.Name}");
            
            // Return true if player survived, false if player died
            return player.IsAlive;
        }
    }
}

