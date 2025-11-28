namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Avalonia.Media;
    using Avalonia.Threading;
    using RPGGame.UI.Avalonia;
    using RPGGame.UI.ColorSystem;

    /// <summary>
    /// Handles dungeon execution, room processing, and enemy encounters.
    /// Extracted from Game.cs to manage the entire dungeon run orchestration.
    /// 
    /// This is the most complex manager - handles:
    /// - Dungeon orchestration
    /// - Room processing
    /// - Enemy encounters
    /// - Combat integration
    /// - Narrative formatting for combat
    /// </summary>
    public class DungeonRunnerManager
    {
        private GameStateManager stateManager;
        private GameNarrativeManager narrativeManager;
        private CombatManager? combatManager;
        private IUIManager? customUIManager;
        private DungeonDisplayManager displayManager;
        
        // Delegates for dungeon completion with reward data
        public delegate void OnDungeonCompleted(int xpGained, Item? lootReceived);
        public delegate void OnShowDeathScreen(Character player);
        
        public event OnDungeonCompleted? DungeonCompletedEvent;
        public event OnShowDeathScreen? ShowDeathScreenEvent;
        
        // Store last reward data for completion screen
        private int lastXPGained;
        private Item? lastLootReceived;

        public DungeonRunnerManager(
            GameStateManager stateManager,
            GameNarrativeManager narrativeManager,
            CombatManager? combatManager,
            IUIManager? customUIManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.narrativeManager = narrativeManager ?? throw new ArgumentNullException(nameof(narrativeManager));
            this.combatManager = combatManager;
            this.customUIManager = customUIManager;
            this.displayManager = new DungeonDisplayManager(narrativeManager, customUIManager);
        }

        /// <summary>
        /// Run the entire dungeon
        /// </summary>
        public async Task RunDungeon()
        {
            DebugLogger.Log("DungeonRunnerManager", "RunDungeon called");
            DebugLogger.Log("DungeonRunnerManager", $"CurrentPlayer: {stateManager.CurrentPlayer?.Name ?? "null"}");
            DebugLogger.Log("DungeonRunnerManager", $"CurrentDungeon: {stateManager.CurrentDungeon?.Name ?? "null"}");
            DebugLogger.Log("DungeonRunnerManager", $"CombatManager: {(combatManager != null ? "initialized" : "null")}");
            
            if (stateManager.CurrentPlayer == null || stateManager.CurrentDungeon == null || combatManager == null)
            {
                DebugLogger.Log("DungeonRunnerManager", "ERROR: Cannot run dungeon - missing required components");
                if (customUIManager is CanvasUICoordinator canvasUIError)
                {
                    canvasUIError.WriteLine("ERROR: Cannot start dungeon - missing required components.", UIMessageType.System);
                }
                // Return to dungeon selection on error
                stateManager.TransitionToState(GameState.DungeonSelection);
                if (customUIManager is CanvasUICoordinator canvasUIError2 && stateManager.CurrentPlayer != null)
                {
                    canvasUIError2.RenderDungeonSelection(stateManager.CurrentPlayer, stateManager.AvailableDungeons);
                }
                return;
            }
            
            // Validate dungeon has been generated and has rooms
            if (stateManager.CurrentDungeon.Rooms == null || stateManager.CurrentDungeon.Rooms.Count == 0)
            {
                DebugLogger.Log("DungeonRunnerManager", $"ERROR: Dungeon '{stateManager.CurrentDungeon.Name}' has no rooms! Regenerating...");
                
                // Try to regenerate the dungeon
                try
                {
                    stateManager.CurrentDungeon.Generate();
                    DebugLogger.Log("DungeonRunnerManager", $"Dungeon regenerated. Room count: {stateManager.CurrentDungeon?.Rooms?.Count ?? 0}");
                }
                catch (Exception ex)
                {
                    DebugLogger.Log("DungeonRunnerManager", $"ERROR: Failed to generate dungeon: {ex.Message}");
                    if (customUIManager is CanvasUICoordinator canvasUIError2)
                    {
                        canvasUIError2.WriteLine($"ERROR: Failed to generate dungeon: {ex.Message}", UIMessageType.System);
                    }
                    // Return to dungeon selection on error
                    stateManager.TransitionToState(GameState.DungeonSelection);
                    if (customUIManager is CanvasUICoordinator canvasUIError3 && stateManager.CurrentPlayer != null)
                    {
                        canvasUIError3.RenderDungeonSelection(stateManager.CurrentPlayer, stateManager.AvailableDungeons);
                    }
                    return;
                }
                
                // Check again after regeneration
                if (stateManager.CurrentDungeon?.Rooms == null || stateManager.CurrentDungeon.Rooms.Count == 0)
                {
                    DebugLogger.Log("DungeonRunnerManager", "ERROR: Dungeon still has no rooms after regeneration!");
                    if (customUIManager is CanvasUICoordinator canvasUIError3)
                    {
                        canvasUIError3.WriteLine("ERROR: Dungeon generation failed - no rooms created.", UIMessageType.System);
                    }
                    // Return to dungeon selection on error
                    stateManager.TransitionToState(GameState.DungeonSelection);
                    if (customUIManager is CanvasUICoordinator canvasUIError4 && stateManager.CurrentPlayer != null)
                    {
                        canvasUIError4.RenderDungeonSelection(stateManager.CurrentPlayer, stateManager.AvailableDungeons);
                    }
                    return;
                }
            }
            
            // Set game state to Dungeon
            DebugLogger.Log("DungeonRunnerManager", "Transitioning to Dungeon state");
            stateManager.TransitionToState(GameState.Dungeon);
            
            // Start dungeon using unified display manager
            // This prepares the dungeon data but doesn't add content to buffer yet
            // Content (including dungeon header) will be added in ProcessRoom() for the first room
            displayManager.StartDungeon(stateManager.CurrentDungeon, stateManager.CurrentPlayer);
            
            // Process all rooms
            try
            {
                int roomNumber = 0;
                int totalRooms = stateManager.CurrentDungeon.Rooms.Count;
                DebugLogger.Log("DungeonRunnerManager", $"Starting dungeon with {totalRooms} rooms");
                bool isFirstRoom = true;
                foreach (Environment room in stateManager.CurrentDungeon.Rooms)
                {
                    roomNumber++;
                    DebugLogger.Log("DungeonRunnerManager", $"Processing room {roomNumber} of {totalRooms}");
                    if (!await ProcessRoom(room, roomNumber, totalRooms, isFirstRoom))
                    {
                        // Player died - transition to death screen
                        if (stateManager.CurrentPlayer != null)
                        {
                            stateManager.TransitionToState(GameState.Death);
                            ShowDeathScreenEvent?.Invoke(stateManager.CurrentPlayer);
                        }
                        return;
                    }
                    isFirstRoom = false;
                }
                
                // Dungeon completed successfully
                DebugLogger.Log("DungeonRunnerManager", "Dungeon completed successfully");
                await CompleteDungeon();
            }
            catch (Exception ex)
            {
                DebugLogger.Log("DungeonRunnerManager", $"ERROR: Exception in RunDungeon: {ex.Message}");
                DebugLogger.Log("DungeonRunnerManager", $"Stack trace: {ex.StackTrace}");
                if (customUIManager is CanvasUICoordinator canvasUIError)
                {
                    canvasUIError.WriteLine($"ERROR: Failed to run dungeon: {ex.Message}", UIMessageType.System);
                }
                throw; // Re-throw to ensure caller knows about the failure
            }
        }

        /// <summary>
        /// Process a single room in the dungeon
        /// </summary>
        private async Task<bool> ProcessRoom(Environment room, int roomNumber, int totalRooms, bool isFirstRoom)
        {
            if (stateManager.CurrentPlayer == null || combatManager == null) return false;
            
            stateManager.SetCurrentRoom(room);
            
            // Show room entry screen (handles entering room, adding to buffer, and rendering)
            if (stateManager.CurrentPlayer != null && customUIManager is CanvasUICoordinator canvasUI)
            {
                // Simplified: one method call handles everything
                displayManager.ShowRoomEntry(room, roomNumber, totalRooms, isFirstRoom);
                
                // Render the room entry screen
                canvasUI.RenderRoomEntry(room, stateManager.CurrentPlayer, stateManager.CurrentDungeon?.Name);
                
                // Delay to show dungeon and room information
                await Task.Delay(3500);
            }
            
            // Clear temporary effects
            if (stateManager.CurrentPlayer != null)
            {
                stateManager.CurrentPlayer.ClearAllTempEffects();
            }
            
            // Check if room is empty (no enemies)
            bool roomWasHostile = room.IsHostile;
            if (!room.HasLivingEnemies())
            {
                // Room is empty - display safe message
                if (customUIManager is CanvasUICoordinator canvasUISafe && stateManager.CurrentPlayer != null)
                {
                    // Add blank line before safe message (after room info)
                    displayManager.AddCombatEvent("");
                    displayManager.AddCombatEvent("It appears you are safe... for now.");
                    // Re-render room entry to show the safe message
                    canvasUISafe.RenderRoomEntry(room, stateManager.CurrentPlayer, stateManager.CurrentDungeon?.Name);
                    await Task.Delay(2000);
                }
                return true; // Player survived the room (no enemies to fight)
            }
            
            // Process all enemies in the room
            while (room.HasLivingEnemies())
            {
                Enemy? currentEnemy = room.GetNextLivingEnemy();
                if (currentEnemy == null) break;
                
                if (!await ProcessEnemyEncounter(currentEnemy))
                {
                    return false; // Player died
                }
            }
            
            // Room completion message
            if (roomWasHostile && customUIManager is CanvasUICoordinator canvasUI2)
            {
                canvasUI2.AddRoomClearedMessage();
                await Task.Delay(2000);
            }
            return true; // Player survived the room
        }

        /// <summary>
        /// Process a single enemy encounter
        /// </summary>
        private async Task<bool> ProcessEnemyEncounter(Enemy enemy)
        {
            if (stateManager.CurrentPlayer == null || combatManager == null) return false;
            
            // Start enemy encounter using unified display manager
            displayManager.StartEnemyEncounter(enemy);
            
            // Reset for new battle
            if (customUIManager is CanvasUICoordinator canvasUISetup)
            {
                canvasUISetup.ResetForNewBattle();
            }
            
            // Render enemy encounter screen to show enemy information after room info
            // This ensures the enemy encounter information is visible before combat starts
            if (customUIManager is CanvasUICoordinator canvasUIEnemy)
            {
                canvasUIEnemy.RenderEnemyEncounter(enemy, stateManager.CurrentPlayer, displayManager.CompleteDisplayLog, 
                    stateManager.CurrentDungeon?.Name, stateManager.CurrentRoom?.Name);
                // Brief delay to show enemy encounter information
                await Task.Delay(2000);
            }
            
            // Initial render of combat screen with structured content
            // This sets up the layout and enables structured combat mode
            if (customUIManager is CanvasUICoordinator canvasUIInitial)
            {
                canvasUIInitial.RenderCombat(stateManager.CurrentPlayer, enemy, displayManager.CompleteDisplayLog);
            }
            
            // Run combat on background thread to prevent UI blocking, but update UI on UI thread
            var room = stateManager.CurrentRoom;
            var player = stateManager.CurrentPlayer;
            
            // Event-driven UI updates with debouncing (replaces polling)
            System.DateTime lastRefreshTime = System.DateTime.MinValue;
            Timer? debounceTimer = null;
            const int minRefreshIntervalMs = 200; // Minimum time between refreshes to reduce flickering
            object refreshLock = new object();
            
            // Handler for combat event updates - uses debouncing to batch rapid updates
            void OnCombatEventAdded()
            {
                lock (refreshLock)
                {
                    var now = System.DateTime.Now;
                    var timeSinceLastRefresh = (now - lastRefreshTime).TotalMilliseconds;
                    
                    // If enough time has passed, refresh immediately
                    if (timeSinceLastRefresh >= minRefreshIntervalMs)
                    {
                        lastRefreshTime = now;
                        
                        // Cancel any pending debounced refresh
                        debounceTimer?.Dispose();
                        debounceTimer = null;
                        
                        // Refresh UI on UI thread (use Post to avoid blocking)
                        if (customUIManager is CanvasUICoordinator canvasUI)
                        {
                            Dispatcher.UIThread.Post(() =>
                            {
                                canvasUI.RenderCombat(player, enemy, displayManager.CompleteDisplayLog);
                            });
                        }
                    }
                    else
                    {
                        // Schedule a debounced refresh
                        var delayMs = minRefreshIntervalMs - (int)timeSinceLastRefresh;
                        
                        // Cancel any existing timer
                        debounceTimer?.Dispose();
                        
                        // Create new timer for debounced refresh
                        debounceTimer = new Timer(_ =>
                        {
                            lock (refreshLock)
                            {
                                lastRefreshTime = System.DateTime.Now;
                                debounceTimer?.Dispose();
                                debounceTimer = null;
                                
                                // Refresh UI on UI thread
                                if (customUIManager is CanvasUICoordinator canvasUI)
                                {
                                    Dispatcher.UIThread.Post(() =>
                                    {
                                        canvasUI.RenderCombat(player, enemy, displayManager.CompleteDisplayLog);
                                    });
                                }
                            }
                        }, null, delayMs, Timeout.Infinite);
                    }
                }
            }
            
            // Subscribe to combat event updates
            displayManager.CombatEventAdded += OnCombatEventAdded;
            
            bool playerWon = false;
            try
            {
                // Run combat on background thread
                var combatTask = Task.Run(async () =>
                {
                    return await combatManager.RunCombat(player, enemy, room!);
                });
                
                // Wait for combat to complete (no polling needed - events handle UI updates)
                playerWon = await combatTask;
            }
            finally
            {
                // Unsubscribe from events and clean up timer
                displayManager.CombatEventAdded -= OnCombatEventAdded;
                lock (refreshLock)
                {
                    debounceTimer?.Dispose();
                    debounceTimer = null;
                }
            }
            
            // Final refresh to show complete combat log (use Post to avoid blocking)
            if (customUIManager is CanvasUICoordinator canvasUI4)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    canvasUI4.RenderCombat(player, enemy, displayManager.CompleteDisplayLog);
                });
                // Small delay to ensure the final render completes
                await Task.Delay(100);
            }
            
            if (!playerWon)
            {
                // Player died - transition to death screen
                if (stateManager.CurrentPlayer != null)
                {
                    // Delete save file when character dies
                    Character.DeleteSaveFile();
                    
                    stateManager.TransitionToState(GameState.Death);
                    ShowDeathScreenEvent?.Invoke(stateManager.CurrentPlayer);
                }
                return false;
            }
            
            // Enemy defeated - add victory message with proper spacing
            if (enemy != null)
            {
                // Add blank line for spacing after informational summary
                displayManager.AddCombatEvent("");
                displayManager.AddCombatEvent("");
                var victoryBuilder = new ColoredTextBuilder();
                victoryBuilder.Add(enemy.Name, ColorPalette.Enemy);
                victoryBuilder.Add(" has been defeated!", ColorPalette.Success);
                displayManager.AddCombatEvent(ColoredTextRenderer.RenderAsMarkup(victoryBuilder.Build()));
            }
            
            // Small delay before next
            await Task.Delay(1000);
            return true; // Player survived this encounter
        }

        /// <summary>
        /// Complete the dungeon run
        /// </summary>
        private async Task CompleteDungeon()
        {
            if (stateManager.CurrentPlayer == null || stateManager.CurrentDungeon == null) return;
            
            // Award rewards and get the data
            var dungeonLevel = stateManager.CurrentDungeon.MaxLevel;
            var dungeonManager = new DungeonManagerWithRegistry();
            var (xpGained, lootReceived) = dungeonManager.AwardLootAndXPWithReturns(
                stateManager.CurrentPlayer, 
                stateManager.CurrentInventory, 
                new List<Dungeon> { stateManager.CurrentDungeon }
            );
            
            // Store reward data
            lastXPGained = xpGained;
            lastLootReceived = lootReceived;
            
            // Add a delay to let rewards display if in console
            await Task.Delay(1500);
            
            // Transition to completion state
            stateManager.TransitionToState(GameState.DungeonCompletion);
            
            // Trigger event to handle UI display with reward data
            DungeonCompletedEvent?.Invoke(xpGained, lootReceived);
        }
        
        /// <summary>
        /// Get the last reward data from dungeon completion
        /// </summary>
        public (int xpGained, Item? lootReceived) GetLastRewardData()
        {
            return (lastXPGained, lastLootReceived);
        }

    }
}

