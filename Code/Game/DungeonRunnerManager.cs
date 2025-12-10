namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Avalonia.Media;
    using Avalonia.Threading;
    using RPGGame.GameCore.Helpers;
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
        public delegate void OnDungeonCompleted(int xpGained, Item? lootReceived, List<LevelUpInfo> levelUpInfos);
        public delegate void OnShowDeathScreen(Character player);
        
        public event OnDungeonCompleted? DungeonCompletedEvent;
        public event OnShowDeathScreen? ShowDeathScreenEvent;
        
        // Store last reward data for completion screen
        private int lastXPGained;
        private Item? lastLootReceived;
        private List<LevelUpInfo> lastLevelUpInfos = new List<LevelUpInfo>();

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
            DebugLogger.Log("DungeonRunnerManager", $"CombatManager: {(combatManager != null ? "initialized" : "null")}");
            
            if (stateManager.CurrentPlayer == null || stateManager.CurrentDungeon == null || combatManager == null)
            {
                DungeonErrorHandler.HandleMissingComponents(stateManager, customUIManager);
                return;
            }
            
            // Validate dungeon has been generated and has rooms
            if (stateManager.CurrentDungeon.Rooms == null || stateManager.CurrentDungeon.Rooms.Count == 0)
            {
                // Try to regenerate the dungeon
                try
                {
                    stateManager.CurrentDungeon.Generate();
                }
                catch (Exception ex)
                {
                    DungeonErrorHandler.HandleDungeonGenerationError(stateManager, customUIManager, $"Failed to generate dungeon: {ex.Message}");
                    return;
                }
                
                if (stateManager.CurrentDungeon?.Rooms == null || stateManager.CurrentDungeon.Rooms.Count == 0)
                {
                    DungeonErrorHandler.HandleDungeonGenerationError(stateManager, customUIManager, "Dungeon generation failed - no rooms created.");
                    return;
                }
            }
            
            // Set game state to Dungeon
            stateManager.TransitionToState(GameState.Dungeon);
            
            // Restore display buffer rendering in case it was suppressed (e.g., from dungeon selection screen)
            // This ensures dungeon exploration and combat screens work correctly
            if (customUIManager is CanvasUICoordinator canvasUIRestore)
            {
                canvasUIRestore.RestoreDisplayBufferRendering();
            }
            
            // Start dungeon using unified display manager
            // This prepares the dungeon data but doesn't add content to buffer yet
            // Content (including dungeon header) will be added in ProcessRoom() for the first room
            displayManager.StartDungeon(stateManager.CurrentDungeon, stateManager.CurrentPlayer);
            
            // Process all rooms
            try
            {
                int roomNumber = 0;
                int totalRooms = stateManager.CurrentDungeon.Rooms.Count;
                bool isFirstRoom = true;
                foreach (Environment room in stateManager.CurrentDungeon.Rooms)
                {
                    roomNumber++;
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
                await CompleteDungeon();
            }
            catch (Exception ex)
            {
                DungeonErrorHandler.HandleException(ex, customUIManager);
                throw;
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
            
            var room = stateManager.CurrentRoom;
            var player = stateManager.CurrentPlayer;
            
            // Create debouncer for UI updates
            CombatEventDebouncer? debouncer = null;
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                debouncer = new CombatEventDebouncer(200, () =>
                    canvasUI.RenderCombat(player, enemy, displayManager.CompleteDisplayLog));
                displayManager.CombatEventAdded += debouncer.TriggerRefresh;
            }
            
            bool playerWon = false;
            try
            {
                playerWon = await Task.Run(async () => await combatManager.RunCombat(player, enemy, room!));
            }
            finally
            {
                if (debouncer != null)
                {
                    displayManager.CombatEventAdded -= debouncer.TriggerRefresh;
                    debouncer.Dispose();
                }
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
            
            // Record enemy defeat in session statistics
            if (player != null)
            {
                player.RecordEnemyDefeat();
            }
            
            // Enemy defeated - add victory message with proper spacing BEFORE final render
            // This ensures the victory message is included in the final render and prevents overlapping text
            if (enemy != null)
            {
                // Add blank line for spacing after informational summary
                displayManager.AddCombatEvent("");
                displayManager.AddCombatEvent("");
                var victoryBuilder = new ColoredTextBuilder();
                victoryBuilder.Add(enemy.Name, ColorPalette.Enemy);
                victoryBuilder.Add(" has been defeated!", ColorPalette.Success);
                displayManager.AddCombatEvent(ColoredTextRenderer.RenderAsMarkup(victoryBuilder.Build()));
                
                // Add remaining health right after defeat message
                if (player != null)
                {
                    string healthMsg = string.Format(AsciiArtAssets.UIText.RemainingHealth, 
                        player.CurrentHealth, player.GetEffectiveMaxHealth());
                    var healthBuilder = new ColoredTextBuilder();
                    healthBuilder.Add(healthMsg, ColorPalette.Gold);
                    displayManager.AddCombatEvent(ColoredTextRenderer.RenderAsMarkup(healthBuilder.Build()));
                }
            }
            
            // Wait for any reactive renders triggered by AddCombatEvent to complete before final render
            // This prevents text from overlapping when multiple renders happen in quick succession
            await Task.Delay(250);
            
            // Final refresh to show complete combat log including victory message (use Post to avoid blocking)
            // This single render will replace any previous renders and show the complete state
            if (customUIManager is CanvasUICoordinator canvasUI4 && enemy != null && player != null)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    canvasUI4.RenderCombat(player, enemy, displayManager.CompleteDisplayLog);
                });
                // Small delay to ensure the final render completes before continuing
                await Task.Delay(100);
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
            var (xpGained, lootReceived, levelUpInfos) = dungeonManager.AwardLootAndXPWithReturns(
                stateManager.CurrentPlayer, 
                stateManager.CurrentInventory, 
                new List<Dungeon> { stateManager.CurrentDungeon }
            );
            
            // Store reward data
            lastXPGained = xpGained;
            lastLootReceived = lootReceived;
            lastLevelUpInfos = levelUpInfos ?? new List<LevelUpInfo>();
            
            // Add a delay to let rewards display if in console
            await Task.Delay(1500);
            
            // Transition to completion state
            stateManager.TransitionToState(GameState.DungeonCompletion);
            
            // Trigger event to handle UI display with reward data
            DungeonCompletedEvent?.Invoke(xpGained, lootReceived, lastLevelUpInfos);
        }
        
        /// <summary>
        /// Get the last reward data from dungeon completion
        /// </summary>
        public (int xpGained, Item? lootReceived, List<LevelUpInfo> levelUpInfos) GetLastRewardData()
        {
            return (lastXPGained, lastLootReceived, lastLevelUpInfos);
        }

    }
}

