namespace RPGGame
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Handles game loop menu input routing.
    /// Extracted from Game.cs to manage game loop navigation.
    /// </summary>
    public class GameLoopInputHandler
    {
        private GameStateManager stateManager;
        
        // Delegates for different menu options
        public delegate Task OnSelectDungeon();
        public delegate void OnShowInventory();
        public delegate void OnShowCharacterInfo();
        public delegate void OnShowMessage(string message);
        
        public event OnSelectDungeon? SelectDungeonEvent;
        public event OnShowInventory? ShowInventoryEvent;
        public event OnShowCharacterInfo? ShowCharacterInfoEvent;
        public event OnShowMessage? ShowMessageEvent;

        public GameLoopInputHandler(GameStateManager stateManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        }

        /// <summary>
        /// Handle game loop menu input
        /// </summary>
        public async Task HandleMenuInput(string input)
        {
            if (stateManager.CurrentPlayer == null)
            {
                ShowMessageEvent?.Invoke("No character loaded.");
                return;
            }

            switch (input)
            {
                case "1":
                    // Start Dungeon
                    if (SelectDungeonEvent != null)
                        await SelectDungeonEvent.Invoke();
                    break;
                case "2":
                    // Show Inventory
                    ShowInventoryEvent?.Invoke();
                    break;
                case "3":
                    // Show Character Info
                    ShowCharacterInfoEvent?.Invoke();
                    break;
                default:
                    ShowMessageEvent?.Invoke("Invalid choice. Press 1 (Dungeon), 2 (Inventory), 3 (Character Info), or ESC to return.");
                    break;
            }
        }
    }
}

