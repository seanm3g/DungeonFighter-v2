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
        
        public event OnShowMainMenu? ShowMainMenuEvent;
        public event OnShowDungeonSelection? ShowDungeonSelectionEvent;

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
        /// Handle character info input - transition to dungeon selection for new characters
        /// </summary>
        public async Task HandleMenuInput(string input)
        {
            // For new characters, go directly to dungeon selection
            // Any input transitions to dungeon selection
            if (ShowDungeonSelectionEvent != null)
            {
                await ShowDungeonSelectionEvent.Invoke();
            }
            else
            {
                // Fallback to main menu if dungeon selection event is not wired
                stateManager.TransitionToState(GameState.MainMenu);
                ShowMainMenuEvent?.Invoke();
            }
        }
    }
}

