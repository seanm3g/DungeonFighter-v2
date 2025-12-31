namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.IO;
    using System.Text.Json;
    using RPGGame.UI.Avalonia;
    using RPGGame.GameCore.Helpers;

    /// <summary>
    /// Orchestrates the main dungeon flow and room iteration
    /// Extracted from DungeonRunnerManager to separate orchestration logic
    /// </summary>
    public class DungeonOrchestrator
    {
        private readonly GameStateManager stateManager;
        private readonly IUIManager? customUIManager;
        private readonly DungeonDisplayManager displayManager;
        private readonly RoomProcessor roomProcessor;
        private readonly DungeonRewardManager rewardManager;
        private readonly DungeonExitChoiceHandler? exitChoiceHandler;
        private readonly System.Action<int, Item?, List<LevelUpInfo>, List<Item>>? onDungeonCompleted;
        private readonly System.Action<Character>? onPlayerDeath;
        private readonly System.Action? onDungeonExitedEarly;

        public DungeonOrchestrator(
            GameStateManager stateManager,
            IUIManager? customUIManager,
            DungeonDisplayManager displayManager,
            RoomProcessor roomProcessor,
            DungeonRewardManager rewardManager,
            DungeonExitChoiceHandler? exitChoiceHandler,
            System.Action<int, Item?, List<LevelUpInfo>, List<Item>>? onDungeonCompleted = null,
            System.Action<Character>? onPlayerDeath = null,
            System.Action? onDungeonExitedEarly = null)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
            this.displayManager = displayManager ?? throw new ArgumentNullException(nameof(displayManager));
            this.roomProcessor = roomProcessor ?? throw new ArgumentNullException(nameof(roomProcessor));
            this.rewardManager = rewardManager ?? throw new ArgumentNullException(nameof(rewardManager));
            this.exitChoiceHandler = exitChoiceHandler;
            this.onDungeonCompleted = onDungeonCompleted;
            this.onPlayerDeath = onPlayerDeath;
            this.onDungeonExitedEarly = onDungeonExitedEarly;
        }

        /// <summary>
        /// Run the entire dungeon
        /// </summary>
        public async Task RunDungeon()
        {
            if (stateManager.CurrentPlayer == null || stateManager.CurrentDungeon == null)
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
            
            // Reset combo step to 0 to start at the first action in the sequence
            if (stateManager.CurrentPlayer != null)
            {
                stateManager.CurrentPlayer.ComboStep = 0;
            }
            
            // Capture starting inventory to track items found during the run
            if (stateManager.CurrentPlayer != null)
            {
                rewardManager.CaptureStartingInventory(stateManager.CurrentPlayer);
            }
            
            // Restore display buffer rendering in case it was suppressed (e.g., from dungeon selection screen)
            // This ensures dungeon exploration and combat screens work correctly
            if (customUIManager is CanvasUICoordinator canvasUIRestore)
            {
                canvasUIRestore.RestoreDisplayBufferRendering();
            }
            
            // Start dungeon using unified display manager
            // This prepares the dungeon data but doesn't add content to buffer yet
            // Content (including dungeon header) will be added in ProcessRoom() for the first room
            if (stateManager.CurrentPlayer == null)
            {
                DungeonErrorHandler.HandleDungeonGenerationError(stateManager, customUIManager, "Cannot start dungeon - no player character available.");
                return;
            }
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
                    if (!await roomProcessor.ProcessRoom(room, roomNumber, totalRooms, isFirstRoom))
                    {
                        // Player died - transition to death screen
                        if (stateManager.CurrentPlayer != null)
                        {
                            stateManager.TransitionToState(GameState.Death);
                            onPlayerDeath?.Invoke(stateManager.CurrentPlayer);
                        }
                        return;
                    }
                    isFirstRoom = false;
                    
                    // Offer exit choice after every room (except the last room)
                    // Only check if there are more rooms to process
                    if (roomNumber < totalRooms)
                    {
                        // Offer exit choice after room completion
                        if (exitChoiceHandler != null)
                        {
                            bool shouldExit = await exitChoiceHandler.ShowExitChoiceMenu(roomNumber, totalRooms);
                            if (shouldExit)
                            {
                                // Player chose to leave - exit dungeon without rewards
                                // Fully heal the player before leaving
                                int healthRestored = 0;
                                if (stateManager.CurrentPlayer != null)
                                {
                                    int effectiveMaxHealth = stateManager.CurrentPlayer.GetEffectiveMaxHealth();
                                    healthRestored = effectiveMaxHealth - stateManager.CurrentPlayer.CurrentHealth;
                                    if (healthRestored > 0)
                                    {
                                        stateManager.CurrentPlayer.Heal(healthRestored);
                                    }
                                }
                                
                                if (customUIManager is CanvasUICoordinator canvasUIExit && stateManager.CurrentPlayer != null)
                                {
                                    displayManager.AddCombatEvent("", stateManager.CurrentPlayer);
                                    if (healthRestored > 0)
                                    {
                                        displayManager.AddCombatEvent($"You have been fully healed! (+{healthRestored} health)", stateManager.CurrentPlayer);
                                    }
                                    displayManager.AddCombatEvent("You leave the dungeon safely, but receive no rewards.", stateManager.CurrentPlayer);
                                    if (stateManager.CurrentPlayer != null && stateManager.CurrentRoom != null)
                                    {
                                        canvasUIExit.RenderRoomEntry(
                                            stateManager.CurrentRoom, 
                                            stateManager.CurrentPlayer, 
                                            stateManager.CurrentDungeon?.Name);
                                    }
                                    if (!RPGGame.MCP.MCPMode.IsActive)
                                    {
                                        await Task.Delay(2000);
                                    }
                                }
                                
                                // Clear dungeon state and return to game loop
                                stateManager.SetCurrentDungeon(null!);
                                stateManager.SetCurrentRoom(null!);
                                stateManager.TransitionToState(GameState.GameLoop);
                                
                                // Trigger event to show game loop screen
                                onDungeonExitedEarly?.Invoke();
                                
                                return; // Exit without calling CompleteDungeon()
                            }
                        }
                    }
                }
                
                // Dungeon completed successfully
                var (xpGained, lootReceived, levelUpInfos, itemsFoundDuringRun) = await rewardManager.CompleteDungeon();
                
                // Transition to completion state
                stateManager.TransitionToState(GameState.DungeonCompletion);
                
                // Trigger event to handle UI display with reward data
                onDungeonCompleted?.Invoke(xpGained, lootReceived, levelUpInfos, itemsFoundDuringRun);
            }
            catch (Exception ex)
            {
                DungeonErrorHandler.HandleException(ex, customUIManager);
                throw;
            }
        }
    }
}

