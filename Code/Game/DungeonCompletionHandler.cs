namespace RPGGame
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Handles dungeon completion screen and post-dungeon options.
    /// Extracted from Game.cs to manage dungeon completion flow.
    /// </summary>
    public class DungeonCompletionHandler
    {
        private GameStateManager stateManager;
        
        // Delegates
        public delegate Task OnStartDungeonSelection();
        public delegate void OnShowInventory();
        public delegate void OnShowMainMenu();
        public delegate Task OnSaveGame();
        public delegate void OnShowMessage(string message);
        
        public event OnStartDungeonSelection? StartDungeonSelectionEvent;
        public event OnShowInventory? ShowInventoryEvent;
        public event OnShowMainMenu? ShowMainMenuEvent;
        public event OnSaveGame? SaveGameEvent;
        public event OnShowMessage? ShowMessageEvent;

        public DungeonCompletionHandler(GameStateManager stateManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        }

        /// <summary>
        /// Handle dungeon completion input
        /// </summary>
        public async Task HandleMenuInput(string input)
        {
            if (stateManager.CurrentPlayer == null) return;
            
            // Clear dungeon state
            stateManager.SetCurrentDungeon(null!);
            stateManager.SetCurrentRoom(null!);
            
            switch (input)
            {
                case "1":
                    // Go to Dungeon Selection
                    if (StartDungeonSelectionEvent != null)
                        await StartDungeonSelectionEvent.Invoke();
                    break;
                case "2":
                    // Show Inventory Menu
                    stateManager.TransitionToState(GameState.Inventory);
                    ShowInventoryEvent?.Invoke();
                    break;
                case "0":
                    // Save and Exit
                    if (SaveGameEvent != null)
                        await SaveGameEvent.Invoke();
                    stateManager.TransitionToState(GameState.MainMenu);
                    ShowMainMenuEvent?.Invoke();
                    break;
                default:
                    ShowMessageEvent?.Invoke("Invalid choice. Please select 1, 2, or 0.");
                    break;
            }
        }
    }
}

