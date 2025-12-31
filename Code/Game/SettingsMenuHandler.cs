namespace RPGGame
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Text.Json;
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
        public delegate void OnShowMessage(string message);
        
        public event OnShowMainMenu? ShowMainMenuEvent;
        public event OnShowMessage? ShowMessageEvent;

        public SettingsMenuHandler(GameStateManager stateManager, IUIManager? customUIManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
        }

        /// <summary>
        /// Display the settings menu - opens a separate pop-out window.
        /// Main window continues to function normally (main menu stays visible and interactive).
        /// </summary>
        public void ShowSettings()
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                try
                {
                    // Get game references from the UI coordinator
                    var game = canvasUI.GetGame();
                    
                    // Check if we're already on the UI thread - if so, execute directly
                    // If not, post to UI thread. This avoids unnecessary thread switching delays.
                    System.Action showWindowAction = () =>
                    {
                        try
                        {
                            // Check if settings window is already open
                            var existingWindow = currentSettingsWindow;
                            
                            // Also check for any other SettingsWindow instances
                            SettingsWindow? foundWindow = null;
                            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                            {
                                var settingsWindows = desktop.Windows.OfType<SettingsWindow>().ToList();
                                foundWindow = settingsWindows.FirstOrDefault(w => w != null && w.IsVisible);
                                
                                // If we found a window but don't have it tracked, track it
                                if (foundWindow != null && existingWindow == null)
                                {
                                    existingWindow = foundWindow;
                                    currentSettingsWindow = foundWindow;
                                }
                            }
                            
                            // If window is already open and visible, just bring it to front
                            if (existingWindow != null && existingWindow.IsVisible)
                            {
                                try
                                {
                                    existingWindow.Activate();
                                    ScrollDebugLogger.Log("SettingsMenuHandler.ShowSettings: Settings window already open, brought to front");
                                    return; // Don't create a new window
                                }
                                catch (Exception ex)
                                {
                                    ScrollDebugLogger.Log($"SettingsMenuHandler.ShowSettings: Error activating existing window: {ex.Message}");
                                    // If activation fails, continue to create new window
                                }
                            }
                            
                            // Close any non-visible or invalid windows before creating new one
                            if (existingWindow != null && !existingWindow.IsVisible)
                            {
                                try
                                {
                                    existingWindow.Close();
                                    ScrollDebugLogger.Log("SettingsMenuHandler.ShowSettings: Closed non-visible settings window");
                                }
                                catch (Exception ex)
                                {
                                    ScrollDebugLogger.Log($"SettingsMenuHandler.ShowSettings: Error closing non-visible window: {ex.Message}");
                                }
                                currentSettingsWindow = null;
                            }
                            
                            // Create and initialize the new window
                            var newWindow = new SettingsWindow();
                            
                            newWindow.Initialize(
                                game,
                                canvasUI,
                                stateManager,
                                (message) => 
                                {
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
                            
                            // Show the window - must be called synchronously on UI thread
                            newWindow.Show();
                            
                            // Don't transition to Settings state - keep current state (MainMenu or GameLoop)
                            // This ensures the main window continues to function normally
                            // The settings window is independent and doesn't affect the main window state
                        }
                        catch (Exception ex)
                        {
                            ScrollDebugLogger.Log($"SettingsMenuHandler.ShowSettings: Error in UI thread operation: {ex.Message}\n{ex.StackTrace}");
                        }
                    };
                    
                    // Execute directly if on UI thread, otherwise post
                    if (Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
                    {
                        showWindowAction();
                    }
                    else
                    {
                        Avalonia.Threading.Dispatcher.UIThread.Post(showWindowAction, Avalonia.Threading.DispatcherPriority.Normal);
                    }
                    
                    return;
                }
                catch (Exception ex)
                {
                    ScrollDebugLogger.Log($"SettingsMenuHandler: Error showing settings window: {ex.Message}\n{ex.StackTrace}");
                }
                
                // Fallback to canvas rendering using GameScreenCoordinator (shouldn't normally happen)
                var screenCoordinator = new GameScreenCoordinator(stateManager);
                screenCoordinator.ShowSettings();
            }
            else
            {
                ScrollDebugLogger.Log($"SettingsMenuHandler: UI manager is not CanvasUICoordinator (type={customUIManager?.GetType().Name ?? "null"})");
                // Don't transition state - settings window should be independent
            }
        }

        /// <summary>
        /// Handle settings menu input
        /// Note: Settings are now accessed via GUI panel, this is kept for backward compatibility
        /// </summary>
        public void HandleMenuInput(string input)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                switch (input)
                {
                    case "1":
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
                        // Any other input goes back to settings (opens GUI panel)
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

