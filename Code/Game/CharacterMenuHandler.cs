namespace RPGGame
{
    using System;
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
        
        public event OnShowMainMenu? ShowMainMenuEvent;

        public CharacterMenuHandler(GameStateManager stateManager, IUIManager? customUIManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
        }

        /// <summary>
        /// Display character information (read-only)
        /// </summary>
        public void ShowCharacterInfo()
        {
            if (customUIManager is CanvasUICoordinator canvasUI && stateManager.CurrentPlayer != null)
            {
                canvasUI.SetCharacter(stateManager.CurrentPlayer);
                // Character info displayed in UI
            }
            stateManager.TransitionToState(GameState.CharacterInfo);
        }

        /// <summary>
        /// Handle character info input (read-only, just go back)
        /// </summary>
        public void HandleMenuInput(string input)
        {
            // Character info is read-only, any input returns to main menu
            stateManager.TransitionToState(GameState.MainMenu);
            ShowMainMenuEvent?.Invoke();
        }
    }
}

