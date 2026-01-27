using Avalonia.Threading;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Handlers;
using RPGGame.UI.TitleScreen;
using RPGGame.Utils;
using System;
using System.Threading.Tasks;

namespace RPGGame.UI.Avalonia.Handlers
{
    /// <summary>
    /// Handles game initialization logic extracted from MainWindow
    /// Separates initialization concerns from window event handling
    /// </summary>
    public class GameInitializationHandler
    {
        private readonly GameCanvasControl gameCanvas;
        private readonly MainWindow mainWindow;
        private GameCoordinator? game;
        private IUIManager? canvasUIManager;
        private MouseInteractionHandler? mouseHandler;
        private bool isInitialized = false;
        private bool waitingForKeyAfterAnimation = false;

        public GameInitializationHandler(GameCanvasControl gameCanvas, MainWindow mainWindow)
        {
            this.gameCanvas = gameCanvas ?? throw new ArgumentNullException(nameof(gameCanvas));
            this.mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
        }

        public bool IsInitialized => isInitialized;
        public bool WaitingForKeyAfterAnimation => waitingForKeyAfterAnimation;
        public GameCoordinator? Game => game;
        public IUIManager? CanvasUIManager => canvasUIManager;
        public MouseInteractionHandler? MouseHandler => mouseHandler;

        /// <summary>
        /// Initializes the game and UI components
        /// </summary>
        public void InitializeGame(Action<string> updateStatus)
        {
            try
            {
                // Initialize the canvas UI manager
                canvasUIManager = new CanvasUICoordinator(gameCanvas);

                // Set the close action for the UI manager
                if (canvasUIManager is CanvasUICoordinator canvasUI)
                {
                    canvasUI.SetCloseAction(() =>
                    {
                        Dispatcher.UIThread.Post(() => mainWindow.Close());
                    });
                    // Set the main window reference so the coordinator can show/hide panels
                    canvasUI.SetMainWindow(mainWindow);
                }

                // Set the UI manager for the static UIManager class
                UIManager.SetCustomUIManager(canvasUIManager);

                // Initialize mouse interaction handler
                if (canvasUIManager is CanvasUICoordinator canvasUIForMouse)
                {
                    mouseHandler = new MouseInteractionHandler(gameCanvas, canvasUIForMouse, null);
                }

                // Show static title screen (no animation)
                if (canvasUIManager is CanvasUICoordinator canvasUI2)
                {
                    // Suppress display buffer rendering to prevent it from clearing the title screen
                    canvasUI2.SuppressDisplayBufferRendering();
                    canvasUI2.ClearDisplayBufferWithoutRender();
                    
                    // Show the static title screen immediately
                    try
                    {
                        TitleScreenHelper.ShowStaticTitleScreen();

                        // Set flag to wait for key press
                        waitingForKeyAfterAnimation = true;
                    }
                    catch (Exception ex)
                    {
                        updateStatus($"Error displaying title screen: {ex.Message}");
                        // If title screen fails, proceed to main menu
                        InitializeGameAfterAnimation(updateStatus);
                    }

                    // Don't proceed to main menu yet - wait for key press
                    return;
                }

                // If no animation, initialize normally
                InitializeGameAfterAnimation(updateStatus);
            }
            catch (Exception ex)
            {
                updateStatus($"Error initializing game: {ex.Message}");
                // If initialization fails, try to initialize without animation
                try
                {
                    InitializeGameAfterAnimation(updateStatus);
                }
                catch (Exception ex2)
                {
                    updateStatus($"Critical error: {ex2.Message}");
                }
            }
        }

        /// <summary>
        /// Completes game initialization after title screen animation
        /// Wrapped in Task.Run to prevent blocking UI thread and handle async void properly
        /// </summary>
        public void InitializeGameAfterAnimation(Action<string> updateStatus)
        {
            // Use Task.Run to prevent blocking and handle errors properly
            _ = Task.Run(async () =>
            {
                try
                {
                    // Post UI updates to UI thread
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        // Check if canvasUIManager is initialized
                        if (canvasUIManager == null)
                        {
                            updateStatus("Error: UI Manager not initialized");
                            return;
                        }

                        // Initialize the game with canvas UI
                        game = new GameCoordinator(canvasUIManager);
                        game.SetUIManager(canvasUIManager);

                        // Set the game reference in CanvasUICoordinator so it can access handlers for interactive panels
                        if (canvasUIManager is CanvasUICoordinator canvasUIForGame)
                        {
                            canvasUIForGame.SetGame(game);
                        }

                        // Update mouse handler with game reference and stats panel state manager
                        if (mouseHandler != null && canvasUIManager is CanvasUICoordinator canvasUIForMouse)
                        {
                            // Get stats panel state manager from coordinator
                            var statsPanelStateManager = canvasUIForMouse.GetStatsPanelStateManager();
                            mouseHandler = new MouseInteractionHandler(gameCanvas, canvasUIForMouse, game, statsPanelStateManager);
                        }
                    });

                    // Show main menu - user can choose to load a saved game or start new
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        game?.ShowMainMenu();
                    });

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        isInitialized = true;
                        waitingForKeyAfterAnimation = false;
                    });
                }
                catch (Exception ex)
                {
                    DebugLogger.Log("GameInitializationHandler", $"Error in InitializeGameAfterAnimation: {ex.Message}\n{ex.StackTrace}");
                    // On error, show main menu as fallback
                    Dispatcher.UIThread.Post(() =>
                    {
                        updateStatus($"Error initializing game: {ex.Message}");
                        game?.ShowMainMenu();
                        isInitialized = true;
                        waitingForKeyAfterAnimation = false;
                    });
                }
            });
        }

        /// <summary>
        /// Automatically loads the first available saved character
        /// If no save exists or loading fails, silently shows the main menu without a character
        /// Includes timeout mechanism to prevent hanging
        /// </summary>
        private async Task AutoLoadCharacter(Action<string> updateStatus)
        {
            // #region agent log
            try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H1,H2,H3,H4,H5", location = "GameInitializationHandler.cs:183", message = "AutoLoadCharacter ENTRY", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion
            // Capture references on UI thread
            GameCoordinator? localGame = null;
            IUIManager? localCanvasUIManager = null;
            
            try
            {
                DebugLogger.Log("GameInitializationHandler", "AutoLoadCharacter started");
                
                // #region agent log
                try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H3", location = "GameInitializationHandler.cs:193", message = "BEFORE UI thread InvokeAsync", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    // #region agent log
                    try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H3", location = "GameInitializationHandler.cs:195", message = "INSIDE UI thread InvokeAsync", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                    // #endregion
                    localGame = game;
                    localCanvasUIManager = canvasUIManager;
                    
                    if (localGame == null || localCanvasUIManager == null)
                    {
                        DebugLogger.Log("GameInitializationHandler", "Game or UI manager not initialized, showing main menu");
                        // #region agent log
                        try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H4", location = "GameInitializationHandler.cs:200", message = "Game or UI manager null", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                        // #endregion
                        // Silently show main menu if game not initialized
                        localGame?.ShowMainMenu();
                        return;
                    }

                    // Check if save file exists first (same check MainMenuHandler uses)
                    bool saveExists = CharacterSaveManager.SaveFileExists();
                    DebugLogger.Log("GameInitializationHandler", $"Save file exists: {saveExists}");
                    // #region agent log
                    try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H5", location = "GameInitializationHandler.cs:208", message = "Save file check result", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), saveExists, threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                    // #endregion
                    
                    if (!saveExists)
                    {
                        DebugLogger.Log("GameInitializationHandler", "No save file found, showing main menu");
                        // No save file, silently show main menu without character
                        localGame.ShowMainMenu();
                        return;
                    }

                    // Show loading message briefly
                    if (localCanvasUIManager is CanvasUICoordinator canvasUI)
                    {
                        // #region agent log
                        try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H5", location = "GameInitializationHandler.cs:221", message = "BEFORE ShowLoadingStatus", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                        // #endregion
                        canvasUI.ShowLoadingStatus("Loading saved character...");
                        // #region agent log
                        try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H5", location = "GameInitializationHandler.cs:223", message = "AFTER ShowLoadingStatus", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                        // #endregion
                    }
                    DebugLogger.Log("GameInitializationHandler", "Showing loading status, starting character load");
                });
                // #region agent log
                try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H3", location = "GameInitializationHandler.cs:226", message = "AFTER UI thread InvokeAsync", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion

                // If game is null after UI thread check, return
                if (localGame == null || localCanvasUIManager == null)
                {
                    DebugLogger.Log("GameInitializationHandler", "Game or UI manager is null after UI thread check, exiting");
                    return;
                }

                var stateManager = localGame.StateManager;
                var gameInitializer = new GameInitializer();
                var gameLoader = new GameLoader(stateManager, gameInitializer, localCanvasUIManager);

                // Create a timeout task (5 seconds max)
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
                // #region agent log
                try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H2,H4", location = "GameInitializationHandler.cs:238", message = "BEFORE LoadGame call", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion
                
                // Create the load task
                DebugLogger.Log("GameInitializationHandler", "Starting LoadGame with 5 second timeout");
                var loadTask = gameLoader.LoadGame(
                    (msg) => {
                        // Only show loading status messages (not error messages) during auto-load
                        // Error messages start with "Error:" - suppress those for auto-load
                        if (!msg.StartsWith("Error:", StringComparison.OrdinalIgnoreCase) && 
                            !msg.StartsWith("No saved", StringComparison.OrdinalIgnoreCase) &&
                            !msg.StartsWith("Failed", StringComparison.OrdinalIgnoreCase))
                        {
                            DebugLogger.Log("GameInitializationHandler", $"LoadGame message: {msg}");
                            Dispatcher.UIThread.Post(() =>
                            {
                                if (localCanvasUIManager is CanvasUICoordinator canvasUIMsg)
                                {
                                    canvasUIMsg.ShowLoadingStatus(msg);
                                }
                            });
                        }
                        else
                        {
                            DebugLogger.Log("GameInitializationHandler", $"Suppressed error message during auto-load: {msg}");
                        }
                    },
                    () => {
                        // Show game loop after successful load
                        // This is called by GameLoader when load succeeds
                        // Clear loading status before showing game loop
                        // #region agent log
                        try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H4", location = "GameInitializationHandler.cs:291", message = "onGameLoop callback CALLED", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                        // #endregion
                        DebugLogger.Log("GameInitializationHandler", "LoadGame succeeded, showing game loop");
                        // #region agent log
                        try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H4", location = "GameInitializationHandler.cs:296", message = "BEFORE UI thread Post in onGameLoop", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                        // #endregion
                        Dispatcher.UIThread.Post(() =>
                        {
                            // #region agent log
                            try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H4", location = "GameInitializationHandler.cs:299", message = "INSIDE UI thread Post in onGameLoop", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                            // #endregion
                            if (localCanvasUIManager is CanvasUICoordinator canvasUIClear)
                            {
                                // #region agent log
                                try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H4", location = "GameInitializationHandler.cs:302", message = "BEFORE ClearLoadingStatus in onGameLoop", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                                // #endregion
                                canvasUIClear.ClearLoadingStatus();
                                // #region agent log
                                try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H4", location = "GameInitializationHandler.cs:305", message = "AFTER ClearLoadingStatus in onGameLoop", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                                // #endregion
                            }
                            localGame?.ShowGameLoop();
                        });
                    },
                    () => {
                        // This callback is only called if load fails
                        // Silently show main menu without character (no error message)
                        DebugLogger.Log("GameInitializationHandler", "LoadGame failed, showing main menu");
                        Dispatcher.UIThread.Post(() => localGame?.ShowMainMenu());
                    }
                );

                // Wait for either load to complete or timeout
                // #region agent log
                try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H2,H4", location = "GameInitializationHandler.cs:287", message = "BEFORE Task.WhenAny", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), loadTaskStatus = loadTask.Status.ToString(), timeoutTaskStatus = timeoutTask.Status.ToString(), threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion
                var completedTask = await Task.WhenAny(loadTask, timeoutTask);
                // #region agent log
                try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H2,H4", location = "GameInitializationHandler.cs:289", message = "AFTER Task.WhenAny", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), completedTaskIsTimeout = (completedTask == timeoutTask), loadTaskStatus = loadTask.Status.ToString(), timeoutTaskStatus = timeoutTask.Status.ToString(), threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion

                if (completedTask == timeoutTask)
                {
                    // Timeout occurred - show main menu immediately
                    DebugLogger.Log("GameInitializationHandler", "Auto-load timed out after 5 seconds, showing main menu");
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (localCanvasUIManager is CanvasUICoordinator canvasUI)
                        {
                            canvasUI.ClearLoadingStatus();
                        }
                        localGame?.ShowMainMenu();
                    });
                    return;
                }

                // Load completed (either success or failure)
                DebugLogger.Log("GameInitializationHandler", "LoadGame task completed, checking result");
                // #region agent log
                try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H2,H4", location = "GameInitializationHandler.cs:305", message = "BEFORE await loadTask", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), loadTaskStatus = loadTask.Status.ToString(), threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion
                bool loadSuccess = await loadTask;
                // #region agent log
                try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H2,H4", location = "GameInitializationHandler.cs:307", message = "AFTER await loadTask", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), loadSuccess, threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion

                // If load succeeded, ensure loading status is cleared
                // (GameLoader should have cleared it, but add safeguard in case onGameLoop callback hasn't run yet)
                if (loadSuccess)
                {
                    DebugLogger.Log("GameInitializationHandler", "LoadGame succeeded, ensuring loading status is cleared");
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (localCanvasUIManager is CanvasUICoordinator canvasUI)
                        {
                            canvasUI.ClearLoadingStatus();
                        }
                        // onGameLoop callback should have been called by GameLoader, but ensure game loop is shown
                        // (Don't call ShowGameLoop here as GameLoader already called the callback)
                    });
                }

                // If load failed, GameLoader already called onShowMainMenu callback
                // But ensure main menu is shown
                if (!loadSuccess)
                {
                    DebugLogger.Log("GameInitializationHandler", "LoadGame failed, ensuring main menu is shown and loading status cleared");
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (localCanvasUIManager is CanvasUICoordinator canvasUI)
                        {
                            canvasUI.ClearLoadingStatus();
                        }
                        localGame?.ShowMainMenu();
                    });
                }
                
                DebugLogger.Log("GameInitializationHandler", "AutoLoadCharacter completed");
                // #region agent log
                try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H1,H2,H3,H4,H5", location = "GameInitializationHandler.cs:339", message = "AutoLoadCharacter EXIT (success)", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion
            }
            catch (Exception ex)
            {
                // #region agent log
                try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H1,H2,H3,H4,H5", location = "GameInitializationHandler.cs:342", message = "AutoLoadCharacter EXCEPTION", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), exception = ex.Message, stackTrace = ex.StackTrace, threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion
                // Log error but don't show to user - just silently show main menu
                DebugLogger.Log("GameInitializationHandler", $"Error auto-loading character: {ex.Message}\n{ex.StackTrace}");
                // On error, silently show main menu without character
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (canvasUIManager is CanvasUICoordinator canvasUI)
                    {
                        canvasUI.ClearLoadingStatus();
                    }
                    game?.ShowMainMenu();
                });
            }
            finally
            {
                // Always ensure loading status is cleared, even if an exception occurred
                // This is a safety net to prevent the loading message from persisting
                try
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (localCanvasUIManager is CanvasUICoordinator canvasUIFinally)
                        {
                            canvasUIFinally.ClearLoadingStatus();
                        }
                        // Also check the instance variable as fallback
                        if (canvasUIManager is CanvasUICoordinator canvasUIFallback)
                        {
                            canvasUIFallback.ClearLoadingStatus();
                        }
                    });
                    DebugLogger.Log("GameInitializationHandler", "Finally block: Loading status cleared");
                }
                catch (Exception finallyEx)
                {
                    // Log but don't throw - this is cleanup code
                    DebugLogger.Log("GameInitializationHandler", $"Error in finally block: {finallyEx.Message}");
                }
            }
        }

        /// <summary>
        /// Handles key press after title screen animation
        /// </summary>
        public void HandleKeyAfterAnimation(Action<string> updateStatus)
        {
            if (waitingForKeyAfterAnimation)
            {
                waitingForKeyAfterAnimation = false;
                InitializeGameAfterAnimation(updateStatus);
            }
        }
    }
}

