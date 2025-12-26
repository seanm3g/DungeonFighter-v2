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
        /// Display the settings menu - now uses GUI panel instead of canvas rendering
        /// </summary>
        public void ShowSettings()
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                // Show the GUI settings panel instead of rendering on canvas
                var mainWindow = canvasUI.GetMainWindow();
                if (mainWindow != null)
                {
                    try
                    {
                        mainWindow.ShowSettingsPanel();
                        stateManager.TransitionToState(GameState.Settings);
                        return;
                    }
                    catch (Exception ex)
                    {
                        ScrollDebugLogger.Log($"SettingsMenuHandler: Error showing settings panel: {ex.Message}");
                    }
                }
                
                // Fallback to canvas rendering if MainWindow not available or error occurred
                canvasUI.SuppressDisplayBufferRendering();
                canvasUI.ClearDisplayBufferWithoutRender();
                canvasUI.RenderSettings();
            }
            else
            {
                ScrollDebugLogger.Log($"SettingsMenuHandler: UI manager is not CanvasUICoordinator (type={customUIManager?.GetType().Name ?? "null"})");
            }
            stateManager.TransitionToState(GameState.Settings);
        }

        /// <summary>
        /// Handle settings menu input
        /// </summary>
        public void HandleMenuInput(string input)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                switch (input)
                {
                    case "1":
                        // Run Tests
                        // ShowTestingMenu will transition the state, so don't do it here
                        ShowTestingMenuEvent?.Invoke();
                        break;
                    case "2":
                        // Developer Menu
                        canvasUI.ResetDeleteConfirmation();
                        stateManager.TransitionToState(GameState.DeveloperMenu);
                        canvasUI.RenderDeveloperMenu();
                        break;
                    case "0":
                        // Back to Main Menu
                        canvasUI.ResetDeleteConfirmation();
                        stateManager.TransitionToState(GameState.MainMenu);
                        ShowMainMenuEvent?.Invoke();
                        break;
                    default:
                        // Any other input goes back to settings
                        canvasUI.ResetDeleteConfirmation();
                        ShowSettings();
                        break;
                }
            }
            else
            {
                ScrollDebugLogger.Log($"SettingsMenuHandler: ERROR - customUIManager is not CanvasUICoordinator (type={customUIManager?.GetType().Name ?? "null"})");
            }
        }

        /// <summary>
        /// Save the current game
        /// </summary>
        public void SaveGame()
        {
            var activeCharacter = stateManager.GetActiveCharacter();
            if (activeCharacter != null)
            {
                try
                {
                    // Get character ID for multi-character save support
                    var characterId = stateManager.GetCharacterId(activeCharacter);
                    activeCharacter.SaveCharacter(characterId);
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

