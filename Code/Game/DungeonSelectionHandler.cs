namespace RPGGame
{
    using System;
    using System.Threading.Tasks;
    using RPGGame.UI.Avalonia;

    /// <summary>
    /// Handles dungeon selection menu and preparation.
    /// Extracted from Game.cs to manage dungeon selection flow.
    /// </summary>
    public class DungeonSelectionHandler
    {
        private GameStateManager stateManager;
        private DungeonManagerWithRegistry? dungeonManager;
        private IUIManager? customUIManager;
        
        // Delegates
        public delegate Task OnStartDungeon();
        public delegate void OnShowGameLoop();
        public delegate void OnShowMessage(string message);
        
        public event OnStartDungeon? StartDungeonEvent;
        public event OnShowGameLoop? ShowGameLoopEvent;
        public event OnShowMessage? ShowMessageEvent;

        public DungeonSelectionHandler(
            GameStateManager stateManager,
            DungeonManagerWithRegistry? dungeonManager,
            IUIManager? customUIManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.dungeonManager = dungeonManager;
            this.customUIManager = customUIManager;
        }

        /// <summary>
        /// Show dungeon selection screen
        /// </summary>
        public async Task ShowDungeonSelection()
        {
            if (stateManager.CurrentPlayer == null || dungeonManager == null) return;
            
            // Regenerate dungeons based on current player level
            dungeonManager.RegenerateDungeons(stateManager.CurrentPlayer, stateManager.AvailableDungeons);
            
            // Set state to dungeon selection
            stateManager.TransitionToState(GameState.DungeonSelection);
            
            // Show dungeon selection screen
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderDungeonSelection(stateManager.CurrentPlayer, stateManager.AvailableDungeons);
            }
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Handle dungeon selection input
        /// </summary>
        public async Task HandleMenuInput(string input)
        {
            DebugLogger.Log("DungeonSelectionHandler", $"HandleMenuInput: input='{input}'");
            
            if (stateManager.CurrentPlayer == null)
            {
                DebugLogger.Log("DungeonSelectionHandler", "CurrentPlayer is null");
                return;
            }
            
            DebugLogger.Log("DungeonSelectionHandler", $"Available dungeons: {stateManager.AvailableDungeons.Count}");
            
            if (int.TryParse(input, out int choice))
            {
                DebugLogger.Log("DungeonSelectionHandler", $"Parsed choice: {choice}");
                
                if (choice >= 1 && choice <= stateManager.AvailableDungeons.Count)
                {
                    DebugLogger.Log("DungeonSelectionHandler", $"Valid dungeon selection: {choice}");
                    
                    // Stop dungeon selection animation
                    if (customUIManager is CanvasUICoordinator canvasUI)
                    {
                        canvasUI.StopDungeonSelectionAnimation();
                    }
                    
                    // Select the dungeon (choice is 1-based, convert to 0-based)
                    await SelectDungeon(choice - 1);
                }
                else if (choice == 0)
                {
                    DebugLogger.Log("DungeonSelectionHandler", "Return to menu selected");
                    
                    // Stop dungeon selection animation
                    if (customUIManager is CanvasUICoordinator canvasUI)
                    {
                        canvasUI.StopDungeonSelectionAnimation();
                    }
                    
                    // Return to game menu
                    stateManager.TransitionToState(GameState.GameLoop);
                    ShowGameLoopEvent?.Invoke();
                }
                else
                {
                    DebugLogger.Log("DungeonSelectionHandler", $"Invalid choice: {choice} (valid: 1-{stateManager.AvailableDungeons.Count} or 0)");
                    ShowMessageEvent?.Invoke("Invalid choice. Please select a valid dungeon or 0 to return.");
                }
            }
            else
            {
                DebugLogger.Log("DungeonSelectionHandler", $"Failed to parse input as int: '{input}'");
                ShowMessageEvent?.Invoke("Invalid input. Please enter a number.");
            }
        }

        /// <summary>
        /// Select a specific dungeon and start it
        /// </summary>
        private async Task SelectDungeon(int dungeonIndex)
        {
            if (stateManager.CurrentPlayer == null || dungeonManager == null) return;
            
            if (dungeonIndex < 0 || dungeonIndex >= stateManager.AvailableDungeons.Count)
            {
                ShowMessageEvent?.Invoke("Invalid dungeon selection.");
                return;
            }
            
            if (stateManager.CurrentPlayer != null)
            {
                stateManager.SetCurrentDungeon(stateManager.AvailableDungeons[dungeonIndex]);
                DebugLogger.Log("DungeonSelectionHandler", $"Set current dungeon: {stateManager.CurrentDungeon?.Name ?? "null"}");
                
                if (stateManager.CurrentDungeon != null)
                {
                    stateManager.CurrentDungeon.Generate();
                    DebugLogger.Log("DungeonSelectionHandler", "Dungeon generated");
                    
                    // Start the dungeon run
                    if (StartDungeonEvent != null)
                    {
                        DebugLogger.Log("DungeonSelectionHandler", "Firing StartDungeonEvent");
                        await StartDungeonEvent.Invoke();
                    }
                    else
                    {
                        DebugLogger.Log("DungeonSelectionHandler", "ERROR: StartDungeonEvent is null!");
                    }
                }
                else
                {
                    DebugLogger.Log("DungeonSelectionHandler", "ERROR: CurrentDungeon is null after SetCurrentDungeon!");
                }
            }
            else
            {
                DebugLogger.Log("DungeonSelectionHandler", "ERROR: CurrentPlayer is null in SelectDungeon!");
            }
        }
    }
}

