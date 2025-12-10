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
            if (stateManager.CurrentPlayer == null)
            {
                return;
            }
            if (int.TryParse(input, out int choice))
            {
                if (choice >= 1 && choice <= stateManager.AvailableDungeons.Count)
                {
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
                if (stateManager.CurrentDungeon != null)
                {
                    stateManager.CurrentDungeon.Generate();
                    // Start the dungeon run
                    if (StartDungeonEvent != null)
                    {
                        await StartDungeonEvent.Invoke();
                    }
                    else
                    {
                    }
                }
                else
                {
                }
            }
            else
            {
            }
        }
    }
}

