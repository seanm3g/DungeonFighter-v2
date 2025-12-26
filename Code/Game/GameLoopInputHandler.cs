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
        public delegate void OnShowMessage(string message);
        public delegate void OnExitGame();
        public delegate void OnShowCharacterSelection();
        
        public event OnSelectDungeon? SelectDungeonEvent;
        public event OnShowInventory? ShowInventoryEvent;
        public event OnShowMessage? ShowMessageEvent;
        public event OnExitGame? ExitGameEvent;
        public event OnShowCharacterSelection? ShowCharacterSelectionEvent;

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

            switch (input.ToUpper())
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
                case "C":
                    // Character Selection (multi-character support)
                    ShowCharacterSelectionEvent?.Invoke();
                    break;
                case "0":
                    // Save & Exit
                    var activeCharacter = stateManager.GetActiveCharacter();
                    if (activeCharacter != null)
                    {
                        // Save character before exit
                        var characterId = stateManager.GetCharacterId(activeCharacter);
                        activeCharacter.SaveCharacter(characterId);
                    }
                    ExitGameEvent?.Invoke();
                    break;
                default:
                    ShowMessageEvent?.Invoke("Invalid choice. Press 1 (Dungeon), 2 (Inventory), C (Characters), or 0 (Save & Exit).");
                    break;
            }
        }
    }
}

