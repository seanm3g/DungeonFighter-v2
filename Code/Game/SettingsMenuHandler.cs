namespace RPGGame
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using Avalonia;
    using Avalonia.Controls.ApplicationLifetimes;
    using RPGGame.UI.Avalonia;
    using RPGGame.UI.Avalonia.Layout;
    using RPGGame.Utils;

    /// <summary>
    /// Handles settings menu display and input.
    /// Extracted from Game.cs to separate settings concerns.
    /// </summary>
    public class SettingsMenuHandler
    {
        private GameStateManager stateManager;
        private IUIManager? customUIManager;
        private SettingsWindow? currentSettingsWindow;
        
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
        /// Display the settings menu - now opens a separate window instead of an overlay panel.
        /// Uses GameScreenCoordinator for standardized screen transition when falling back to canvas.
        /// Clears and prepares the center panel for test output when tests are run from settings.
        /// </summary>
        public void ShowSettings()
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                try
                {
                    // Clear and prepare the center panel for test output
                    // This ensures the center panel is ready to display test results when tests are run from settings
                    var centerX = LayoutConstants.CENTER_PANEL_X + 1;
                    var centerY = LayoutConstants.CENTER_PANEL_Y + 1;
                    var centerWidth = LayoutConstants.CENTER_PANEL_WIDTH - 2;
                    var centerHeight = LayoutConstants.CENTER_PANEL_HEIGHT - 2;
                    canvasUI.ClearTextInArea(centerX, centerY, centerWidth, centerHeight);
                    
                    // Clear the display buffer to start fresh
                    canvasUI.ClearDisplayBuffer();
                    
                    // Restore display buffer rendering so test output can be displayed in the center panel
                    // This ensures the center panel is ready for test output when tests are run from settings
                    canvasUI.RestoreDisplayBufferRendering();
                    
                    // Force a layout render to ensure the center panel is visible and ready
                    canvasUI.ForceFullLayoutRender();
                    
                    // Get game references from the UI coordinator
                    var game = canvasUI.GetGame();
                    
                    // Close any existing settings window first (synchronously on UI thread)
                    // We need to do this synchronously to avoid race conditions
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        try
                        {
                            // Close the tracked window if it exists
                            var oldWindow = currentSettingsWindow;
                            if (oldWindow != null)
                            {
                                try
                                {
                                    oldWindow.Close();
                                    ScrollDebugLogger.Log("SettingsMenuHandler.ShowSettings: Closed existing settings window");
                                }
                                catch (Exception ex)
                                {
                                    ScrollDebugLogger.Log($"SettingsMenuHandler.ShowSettings: Error closing old window: {ex.Message}");
                                }
                                currentSettingsWindow = null;
                            }
                            
                            // Also find and close any other SettingsWindow instances
                            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                            {
                                var settingsWindows = desktop.Windows.OfType<SettingsWindow>().ToList();
                                foreach (var window in settingsWindows)
                                {
                                    if (window != null && window != oldWindow)
                                    {
                                        try
                                        {
                                            window.Close();
                                            ScrollDebugLogger.Log("SettingsMenuHandler.ShowSettings: Closed additional SettingsWindow instance");
                                        }
                                        catch (Exception ex)
                                        {
                                            ScrollDebugLogger.Log($"SettingsMenuHandler.ShowSettings: Error closing additional window: {ex.Message}");
                                        }
                                    }
                                }
                            }
                            
                            // Now create and show the new window
                            var newWindow = new SettingsWindow();
                            
                            // Initialize the settings window
                            newWindow.Initialize(
                                game,
                                canvasUI,
                                stateManager,
                                (message) => 
                                {
                                    // Status update callback - can update main window status if needed
                                    ScrollDebugLogger.Log($"Settings: {message}");
                                });
                            
                            // Handle window closing to clear reference
                            newWindow.Closing += (sender, e) =>
                            {
                                if (sender == newWindow)
                                {
                                    currentSettingsWindow = null;
                                }
                            };
                            
                            // Store the new window reference
                            currentSettingsWindow = newWindow;
                            
                            // Show the new window
                            newWindow.Show();
                            stateManager.TransitionToState(GameState.Settings);
                        }
                        catch (Exception ex)
                        {
                            ScrollDebugLogger.Log($"SettingsMenuHandler.ShowSettings: Error in UI thread operation: {ex.Message}\n{ex.StackTrace}");
                        }
                    }, Avalonia.Threading.DispatcherPriority.Normal);
                    
                    return;
                }
                catch (Exception ex)
                {
                    ScrollDebugLogger.Log($"SettingsMenuHandler: Error showing settings window: {ex.Message}\n{ex.StackTrace}");
                }
                
                // Fallback to canvas rendering using GameScreenCoordinator
                var screenCoordinator = new GameScreenCoordinator(stateManager);
                screenCoordinator.ShowSettings();
            }
            else
            {
                ScrollDebugLogger.Log($"SettingsMenuHandler: UI manager is not CanvasUICoordinator (type={customUIManager?.GetType().Name ?? "null"})");
                stateManager.TransitionToState(GameState.Settings);
            }
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
        /// Closes the settings window if it's open
        /// </summary>
        public void CloseSettingsWindow()
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    // Capture the window reference to avoid race conditions
                    var windowToClose = currentSettingsWindow;
                    
                    // First, try to close the tracked window
                    if (windowToClose != null)
                    {
                        try
                        {
                            // Only clear the reference if it's still the same window
                            if (currentSettingsWindow == windowToClose)
                            {
                                windowToClose.Close();
                                currentSettingsWindow = null;
                                ScrollDebugLogger.Log("SettingsMenuHandler.CloseSettingsWindow: Tracked settings window closed");
                            }
                            else
                            {
                                // Window was replaced, just close the old one without clearing the reference
                                windowToClose.Close();
                                ScrollDebugLogger.Log("SettingsMenuHandler.CloseSettingsWindow: Closed old settings window (new window exists)");
                            }
                        }
                        catch (Exception ex)
                        {
                            ScrollDebugLogger.Log($"SettingsMenuHandler.CloseSettingsWindow: Error closing tracked window: {ex.Message}");
                            // Only clear reference if we successfully closed it or if it's still the same window
                            if (currentSettingsWindow == windowToClose)
                            {
                                currentSettingsWindow = null;
                            }
                        }
                    }
                    
                    // Also find and close any SettingsWindow instances that might exist
                    // This is a fallback in case the reference wasn't tracked properly
                    if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                    {
                        var settingsWindows = desktop.Windows.OfType<SettingsWindow>().ToList();
                        foreach (var window in settingsWindows)
                        {
                            try
                            {
                                // Don't close the current window if it exists
                                if (window != null && window.IsVisible && window != currentSettingsWindow)
                                {
                                    window.Close();
                                    ScrollDebugLogger.Log("SettingsMenuHandler.CloseSettingsWindow: Found and closed SettingsWindow instance");
                                }
                            }
                            catch (Exception ex)
                            {
                                ScrollDebugLogger.Log($"SettingsMenuHandler.CloseSettingsWindow: Error closing found window: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ScrollDebugLogger.Log($"SettingsMenuHandler.CloseSettingsWindow: Unexpected error: {ex.Message}");
                }
            });
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

