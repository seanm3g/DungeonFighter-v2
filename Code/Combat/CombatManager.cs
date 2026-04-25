using System.Linq;
using System;
using System.IO;
using System.Text.Json;
using RPGGame.Actions.RollModification;

namespace RPGGame
{
    /// <summary>Result of executing at most one actor turn (used by normal combat loop and action interaction lab).</summary>
    public enum CombatSingleTurnResult
    {
        Advanced,
        PlayerDefeated,
        EnemyDefeated,
        LoopLimitExceeded,
        StuckNullEntities,
        NoSpeedSystem,
        UnknownEntity
    }

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
            
            // Clear damage cache on combat manager creation to prevent stale zero-damage values
            Combat.Calculators.DamageCalculator.ClearAllCaches();
            
            // Subscribe to one-shot kill events
            Actions.Execution.ActionExecutionFlow.OneShotKillOccurred += OnOneShotKill;
        }
        
        /// <summary>
        /// Handles one-shot kill events
        /// </summary>
        private void OnOneShotKill()
        {
            stateManager.RecordOneShotKill();
        }
        
        /// <summary>
        /// Gets whether a one-shot kill occurred in the last combat
        /// </summary>
        public bool HadOneShotKill()
        {
            return stateManager.HadOneShotKill();
        }
        
        /// <summary>
        /// Cleanup method to unsubscribe from events
        /// Should be called when CombatManager is no longer needed
        /// </summary>
        public void Cleanup()
        {
            Actions.Execution.ActionExecutionFlow.OneShotKillOccurred -= OnOneShotKill;
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
        /// <summary>
        /// Runs one combat iteration: resolves time until an actor acts, then processes that actor's turn.
        /// </summary>
        /// <param name="forcedAction">When non-null, used only when the resolved turn is the <paramref name="player"/>'s (Action Interaction Lab catalog pick). Enemy turns always select from the enemy's own action pool.</param>
        public async Task<CombatSingleTurnResult> AdvanceSingleTurnAsync(Character player, Enemy currentEnemy, Environment room, Action? forcedAction = null)
        {
            int maxTurns = 1000;
            int turnCount = 0;
            int nullEntityCount = 0;
            const int MAX_NULL_ENTITIES = 100;

            while (player.IsAlive && currentEnemy.IsAlive)
            {
                turnCount++;
                if (turnCount > maxTurns)
                {
                    var errorText = CombatFlowColoredText.FormatSystemErrorColored($"Combat loop exceeded {maxTurns} turns. Breaking to prevent infinite loop.");
                    BlockDisplayManager.DisplaySystemBlock(errorText);
                    DebugLogger.WriteCombatDebug("CombatManager", $"Combat loop exceeded max turns: {turnCount}");
                    return CombatSingleTurnResult.LoopLimitExceeded;
                }

                Actor? nextEntity = GetNextEntityToAct();

                if (nextEntity == null)
                {
                    nullEntityCount++;
                    if (nullEntityCount > MAX_NULL_ENTITIES)
                    {
                        var errorText = CombatFlowColoredText.FormatSystemErrorColored($"Combat loop stuck: {nullEntityCount} consecutive null entities. Breaking to prevent infinite loop.");
                        BlockDisplayManager.DisplaySystemBlock(errorText);
                        DebugLogger.WriteCombatDebug("CombatManager", $"Combat loop stuck with {nullEntityCount} consecutive null entities");
                        return CombatSingleTurnResult.StuckNullEntities;
                    }

                    var currentSpeedSystem = GetCurrentActionSpeedSystem();
                    if (currentSpeedSystem != null)
                    {
                        var nextReadyTime = currentSpeedSystem.GetNextReadyTime();
                        if (nextReadyTime > 0)
                        {
                            double timeToAdvance = nextReadyTime - currentSpeedSystem.GetCurrentTime();
                            if (timeToAdvance > 0)
                                currentSpeedSystem.AdvanceTime(timeToAdvance);
                            else
                                currentSpeedSystem.AdvanceTime(0.1);
                        }
                        else if (nextReadyTime == -1.0)
                        {
                            var errorText = CombatFlowColoredText.FormatSystemErrorColored("No entities in action speed system. Breaking combat loop.");
                            BlockDisplayManager.DisplaySystemBlock(errorText);
                            DebugLogger.WriteCombatDebug("CombatManager", "No entities in action speed system");
                            return CombatSingleTurnResult.NoSpeedSystem;
                        }
                        else
                        {
                            var errorText = CombatFlowColoredText.FormatSystemErrorColored("No entities ready and no next ready time. Breaking combat loop.");
                            BlockDisplayManager.DisplaySystemBlock(errorText);
                            DebugLogger.WriteCombatDebug("CombatManager", "No entities ready and no next ready time");
                            return CombatSingleTurnResult.NoSpeedSystem;
                        }
                    }
                    else
                    {
                        DebugLogger.WriteCombatDebug("CombatManager", "ActionSpeedSystem is null");
                        return CombatSingleTurnResult.NoSpeedSystem;
                    }
                    continue;
                }

                nullEntityCount = 0;
                stateManager.HandleEntityChange(nextEntity);

                if (nextEntity == player && player.IsAlive)
                {
                    bool combatContinues = await turnHandler.ProcessPlayerTurnAsync(player, currentEnemy, room, forcedAction);
                    await CombatDelayManager.DelayAfterActionAsync();
                    if (!player.IsAlive)
                        return CombatSingleTurnResult.PlayerDefeated;
                    if (!currentEnemy.IsAlive)
                        return CombatSingleTurnResult.EnemyDefeated;
                    if (!combatContinues)
                        return CombatSingleTurnResult.EnemyDefeated;
                    return CombatSingleTurnResult.Advanced;
                }

                if (nextEntity == currentEnemy && currentEnemy.IsAlive)
                {
                    bool combatContinues = await turnHandler.ProcessEnemyTurnAsync(player, currentEnemy, room, forcedAction: null);
                    await CombatDelayManager.DelayAfterActionAsync();
                    if (!player.IsAlive)
                        return CombatSingleTurnResult.PlayerDefeated;
                    if (!currentEnemy.IsAlive)
                        return CombatSingleTurnResult.EnemyDefeated;
                    if (!combatContinues)
                        return CombatSingleTurnResult.PlayerDefeated;
                    return CombatSingleTurnResult.Advanced;
                }

                if (nextEntity == room && room.IsHostile && room.ActionPool.Count > 0)
                {
                    if (!turnHandler.ProcessEnvironmentTurn(player, currentEnemy, room))
                    {
                        if (!player.IsAlive)
                            return CombatSingleTurnResult.PlayerDefeated;
                        return CombatSingleTurnResult.EnemyDefeated;
                    }
                    await CombatDelayManager.DelayAfterActionAsync();
                    if (!player.IsAlive)
                        return CombatSingleTurnResult.PlayerDefeated;
                    if (!currentEnemy.IsAlive)
                        return CombatSingleTurnResult.EnemyDefeated;
                    return CombatSingleTurnResult.Advanced;
                }

                DebugLogger.WriteCombatDebug("CombatManager", $"Unknown entity type in combat loop: {nextEntity?.GetType().Name ?? "null"}");
                var speedSys = GetCurrentActionSpeedSystem();
                if (speedSys != null && nextEntity != null)
                {
                    speedSys.AdvanceEntityTurn(nextEntity, 1.0);
                }
                else
                {
                    var errorText = CombatFlowColoredText.FormatSystemErrorColored("Unknown entity in combat loop. Breaking to prevent infinite loop.");
                    BlockDisplayManager.DisplaySystemBlock(errorText);
                    return CombatSingleTurnResult.UnknownEntity;
                }
            }

            if (!player.IsAlive)
                return CombatSingleTurnResult.PlayerDefeated;
            if (!currentEnemy.IsAlive)
                return CombatSingleTurnResult.EnemyDefeated;
            return CombatSingleTurnResult.Advanced;
        }

        public async Task<bool> RunCombat(Character player, Enemy currentEnemy, Environment room, bool playerGetsFirstAttack = false, bool enemyGetsFirstAttack = false)
        {
            // Reset game time FIRST to ensure clean timing state
            GameTicker.Instance.Reset();
            
            // Full combo reset at fight start (InitializeCombatEntities also resets; belt-and-suspenders before narrative)
            player.ResetCombo();
            
            // Start battle narrative and initialize action speed system
            StartBattleNarrative(player.Name, currentEnemy.Name, room.Name, player.CurrentHealth, currentEnemy.CurrentHealth);
            
            // Initialize combat entities AFTER action speed system is created
            InitializeCombatEntities(player, currentEnemy, room, playerGetsFirstAttack, enemyGetsFirstAttack);
            
            // Reset environment action count for new fight
            room.ResetForNewFight();

            while (player.IsAlive && currentEnemy.IsAlive)
            {
                var step = await AdvanceSingleTurnAsync(player, currentEnemy, room, forcedAction: null);
                if (step == CombatSingleTurnResult.Advanced)
                    continue;
                break;
            }
            
            // End the battle narrative with final health values
            EndBattleNarrative(player, currentEnemy);

            // Clear roll threshold modifiers so UI and the next fight start from config defaults
            // (per-roll logic resets at attack time; this clears any state left after the last roll).
            var thresholdManager = RollModificationManager.GetThresholdManager();
            thresholdManager.ResetThresholds(player);
            thresholdManager.ResetThresholds(currentEnemy);
            
            // Full combo reset after combat so exploration / next encounter never inherits combo mode or strip index
            player.ResetCombo();

            // Wipe combat status (DoT, debuffs, advanced stacks) so the hero never carries encounter state onward
            player.ClearAllTempEffects();
            
            DebugLogger.WriteCombatDebug("CombatManager", $"Combat ended: {player.Name} {(player.IsAlive ? "survived" : "died")} vs {currentEnemy.Name}");
            
            // Return true if player survived, false if player died
            return player.IsAlive;
        }
    }
}


