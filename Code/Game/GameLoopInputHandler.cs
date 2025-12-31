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
        public delegate void OnShowMainMenu();
        
        public event OnSelectDungeon? SelectDungeonEvent;
        public event OnShowInventory? ShowInventoryEvent;
        public event OnShowMessage? ShowMessageEvent;
#pragma warning disable CS0067 // Event is never used - reserved for future use
        public event OnExitGame? ExitGameEvent;
#pragma warning restore CS0067
        public event OnShowCharacterSelection? ShowCharacterSelectionEvent;
        public event OnShowMainMenu? ShowMainMenuEvent;

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
                    // Save & Return to Main Menu
                    var activeCharacter = stateManager.GetActiveCharacter();
                    if (activeCharacter != null)
                    {
                        // Save character before returning to main menu
                        try
                        {
                            var characterId = stateManager.GetCharacterId(activeCharacter);
                            activeCharacter.SaveCharacter(characterId);
                        }
                        catch (Exception)
                        {
                            // Error already logged and shown by CharacterSaveService
                            // Continue to return to main menu even if save failed
                        }
                    }
                    stateManager.TransitionToState(GameState.MainMenu);
                    ShowMainMenuEvent?.Invoke();
                    break;
                default:
                    ShowMessageEvent?.Invoke("Invalid choice. Press 1 (Dungeon), 2 (Inventory), C (Characters), or 0 (Back to Main Menu).");
                    break;
            }
        }
    }
}

