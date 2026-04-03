namespace RPGGame
{
    using System;
    using System.Threading.Tasks;
    using RPGGame.UI.Avalonia;

    /// <summary>
    /// Handles character info menu display and input.
    /// Extracted from Game.cs to separate character info concerns.
    /// </summary>
    public class CharacterMenuHandler
    {
        private GameStateManager stateManager;
        private IUIManager? customUIManager;
        
        // Delegates
        public delegate void OnShowMainMenu();
        public delegate Task OnShowDungeonSelection();
        public delegate void OnShowGameLoop();
        
        public event OnShowMainMenu? ShowMainMenuEvent;
        public event OnShowDungeonSelection? ShowDungeonSelectionEvent;
        public event OnShowGameLoop? ShowGameLoopEvent;

        public CharacterMenuHandler(GameStateManager stateManager, IUIManager? customUIManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
        }

        /// <summary>
        /// Display character information (read-only).
        /// Uses GameScreenCoordinator for standardized screen transition.
        /// </summary>
        public void ShowCharacterInfo()
        {
            var screenCoordinator = new GameScreenCoordinator(stateManager);
            screenCoordinator.ShowCharacterInfo();
        }

        /// <summary>
        /// Handle character info input - transition to game loop for new characters
        /// After starting a new game, character info should lead to game loop, not dungeon selection
        /// </summary>
        public async Task HandleMenuInput(string input)
        {
            // For new characters (after weapon selection), go to game loop
            // Any input transitions to game loop where player can choose to go to dungeon selection
            if (ShowGameLoopEvent != null)
            {
                stateManager.TransitionToState(GameState.GameLoop);
                ShowGameLoopEvent.Invoke();
            }
            else if (ShowDungeonSelectionEvent != null)
            {
                // Fallback to dungeon selection if game loop event is not wired
                await ShowDungeonSelectionEvent.Invoke();
            }
            else
            {
                // Final fallback to main menu if neither event is wired
                stateManager.TransitionToState(GameState.MainMenu);
                ShowMainMenuEvent?.Invoke();
            }
        }
    }
}

