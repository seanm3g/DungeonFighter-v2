namespace RPGGame
{
    using System;
    using RPGGame.UI.Avalonia;
    using RPGGame.Utils;

    /// <summary>
    /// Handles settings menu display and input.
    /// Extracted from Game.cs to separate settings concerns.
    /// </summary>
    public class SettingsMenuHandler
    {
        private GameStateManager stateManager;
        private IUIManager? customUIManager;
        
        // Delegates
        public delegate void OnShowMainMenu();
        public delegate void OnShowTestingMenu();
        public delegate void OnShowMessage(string message);
        
        public event OnShowMainMenu? ShowMainMenuEvent;
        public event OnShowTestingMenu? ShowTestingMenuEvent;
        public event OnShowMessage? ShowMessageEvent;

        public SettingsMenuHandler(GameStateManager stateManager, IUIManager? customUIManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
        }

        /// <summary>
        /// Display the settings menu
        /// </summary>
        public void ShowSettings()
        {
            ScrollDebugLogger.Log("SettingsMenuHandler: ShowSettings called");
            DebugLogger.Log("SettingsMenuHandler", "ShowSettings called");
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                ScrollDebugLogger.Log("SettingsMenuHandler: UI manager is CanvasUICoordinator, suppressing display buffer rendering and rendering settings");
                // Suppress display buffer auto-rendering FIRST to prevent any pending renders
                canvasUI.SuppressDisplayBufferRendering();
                // Clear buffer without triggering a render (since we're suppressing rendering anyway)
                canvasUI.ClearDisplayBufferWithoutRender();
                canvasUI.RenderSettings();
                ScrollDebugLogger.Log("SettingsMenuHandler: RenderSettings completed");
            }
            else
            {
                ScrollDebugLogger.Log($"SettingsMenuHandler: UI manager is not CanvasUICoordinator (type={customUIManager?.GetType().Name ?? "null"})");
            }
            stateManager.TransitionToState(GameState.Settings);
            ScrollDebugLogger.Log("SettingsMenuHandler: State transitioned to Settings");
        }

        /// <summary>
        /// Handle settings menu input
        /// </summary>
        public void HandleMenuInput(string input)
        {
            DebugLogger.Log("SettingsMenuHandler", $"HandleMenuInput called with input: '{input}'");
            ScrollDebugLogger.Log($"SettingsMenuHandler.HandleMenuInput: input='{input}', state={stateManager.CurrentState}");
            
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                switch (input)
                {
                    case "1":
                        // Run Tests
                        DebugLogger.Log("SettingsMenuHandler", "Showing testing menu");
                        ScrollDebugLogger.Log("SettingsMenuHandler: Showing testing menu");
                        // ShowTestingMenu will transition the state, so don't do it here
                        ShowTestingMenuEvent?.Invoke();
                        break;
                    case "2":
                        // Developer Menu
                        DebugLogger.Log("SettingsMenuHandler", "Showing developer menu");
                        ScrollDebugLogger.Log("SettingsMenuHandler: Showing developer menu");
                        canvasUI.ResetDeleteConfirmation();
                        stateManager.TransitionToState(GameState.DeveloperMenu);
                        canvasUI.RenderDeveloperMenu();
                        break;
                    case "0":
                        // Back to Main Menu
                        DebugLogger.Log("SettingsMenuHandler", "Returning to Main Menu");
                        ScrollDebugLogger.Log("SettingsMenuHandler: Returning to Main Menu");
                        canvasUI.ResetDeleteConfirmation();
                        stateManager.TransitionToState(GameState.MainMenu);
                        ShowMainMenuEvent?.Invoke();
                        break;
                    default:
                        // Any other input goes back to settings
                        DebugLogger.Log("SettingsMenuHandler", $"Unknown input '{input}', refreshing settings");
                        ScrollDebugLogger.Log($"SettingsMenuHandler: Unknown input '{input}', refreshing settings");
                        canvasUI.ResetDeleteConfirmation();
                        ShowSettings();
                        break;
                }
            }
            else
            {
                DebugLogger.Log("SettingsMenuHandler", "ERROR: customUIManager is not CanvasUICoordinator");
                ScrollDebugLogger.Log($"SettingsMenuHandler: ERROR - customUIManager is not CanvasUICoordinator (type={customUIManager?.GetType().Name ?? "null"})");
            }
        }

        /// <summary>
        /// Save the current game
        /// </summary>
        public void SaveGame()
        {
            if (stateManager.CurrentPlayer != null)
            {
                try
                {
                    stateManager.CurrentPlayer.SaveCharacter();
                    ShowMessageEvent?.Invoke("Game saved successfully!");
                }
                catch (Exception ex)
                {
                    ShowMessageEvent?.Invoke($"Error saving game: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Exit/quit the game
        /// </summary>
        public void ExitGame()
        {
            ShowMessageEvent?.Invoke("Thanks for playing Dungeon Fighter!");
            // Close the application
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.Close();
            }
            System.Environment.Exit(0);
        }
    }
}

