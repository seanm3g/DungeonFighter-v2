namespace RPGGame
{
    using System;
    using RPGGame.UI.Avalonia;

    /// <summary>
    /// Handles character creation and customization flow.
    /// Extracted from Game.cs to manage character customization after weapon selection.
    /// 
    /// Responsibilities:
    /// - Display character customization options
    /// - Handle character customization input
    /// - Apply character customizations
    /// - Transition to game loop when complete
    /// </summary>
    public class CharacterCreationHandler
    {
        private GameStateManager stateManager;
        private IUIManager? customUIManager;
        
        // Delegates
        public delegate void OnGameLoopStart();
        public delegate void OnShowMessage(string message);
        
        public event OnGameLoopStart? StartGameLoopEvent;
        public event OnShowMessage? ShowMessageEvent;

        public CharacterCreationHandler(GameStateManager stateManager, IUIManager? customUIManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
        }

        /// <summary>
        /// Display character creation/customization screen
        /// </summary>
        public void ShowCharacterCreation()
        {
            if (customUIManager is CanvasUICoordinator canvasUI && stateManager.CurrentPlayer != null)
            {
                // Render the character creation screen showing character details
                canvasUI.RenderCharacterCreation(stateManager.CurrentPlayer);
                DebugLogger.Log("CharacterCreationHandler", $"Displaying character creation for {stateManager.CurrentPlayer.Name}");
            }
            stateManager.TransitionToState(GameState.CharacterCreation);
            DebugLogger.Log("CharacterCreationHandler", "Showing character creation screen");
        }

        /// <summary>
        /// Handle character creation input
        /// Input: Any key (except 0) = Start Game, 0=Back to weapon selection
        /// </summary>
        public void HandleMenuInput(string input)
        {
            if (stateManager.CurrentPlayer == null)
            {
                ShowMessageEvent?.Invoke("No character selected.");
                return;
            }

            DebugLogger.Log("CharacterCreationHandler", $"HandleMenuInput: input='{input}'");

            string trimmedInput = input?.Trim() ?? "";

            if (trimmedInput == "0")
            {
                DebugLogger.Log("CharacterCreationHandler", "Going back to weapon selection");
                ShowMessageEvent?.Invoke("Going back to weapon selection...");
                
                // Go back to weapon selection
                stateManager.TransitionToState(GameState.WeaponSelection);
            }
            else if (!string.IsNullOrEmpty(trimmedInput))
            {
                DebugLogger.Log("CharacterCreationHandler", "Starting game loop");
                ShowMessageEvent?.Invoke($"Welcome, {stateManager.CurrentPlayer.Name}!");
                
                // Transition to game loop
                stateManager.TransitionToState(GameState.GameLoop);
                StartGameLoopEvent?.Invoke();
            }
        }
    }
}

