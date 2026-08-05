using Avalonia.Threading;
using RPGGame;
using RPGGame.Audio;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Feedback;
using RPGGame.UI.Avalonia.Handlers;
using RPGGame.UI.TitleScreen;
using RPGGame.Utils;
using System;
using System.Threading;
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
        private CancellationTokenSource? titleIdleCts;
        private Task? gameWarmupTask;
        private int gameWarmupStarted;

        public GameInitializationHandler(GameCanvasControl gameCanvas, MainWindow mainWindow)
        {
            this.gameCanvas = gameCanvas ?? throw new ArgumentNullException(nameof(gameCanvas));
            this.mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
        }

        public bool IsInitialized => isInitialized;
        public bool WaitingForKeyAfterAnimation => waitingForKeyAfterAnimation;
        /// <summary>True once background GameCoordinator construction has finished (menu can appear immediately).</summary>
        public bool IsGameWarmupCompleted => gameWarmupTask?.IsCompletedSuccessfully == true && game != null;
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

                HeroActionStripFeedback.SetRequestInvalidate(() => gameCanvas.Refresh());
                ThresholdBarFeedback.SetRequestInvalidate(() => gameCanvas.Refresh());
                ActionBonusBorderShimmer.SetRequestInvalidate(() => gameCanvas.Refresh());

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

                // Animated title: idle gradient until any key. Warm GameCoordinator in parallel
                // so a keypress only needs to show the main menu (avoids post-key hitch).
                if (canvasUIManager is CanvasUICoordinator canvasUI2)
                {
                    // Suppress display buffer rendering to prevent it from clearing the title screen
                    canvasUI2.SuppressDisplayBufferRendering();
                    canvasUI2.ClearDisplayBufferWithoutRender();

                    try
                    {
                        try
                        {
                            AudioBootstrap.InitializeTitleScreenMusic();
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.Log("GameInitializationHandler", $"InitializeTitleScreenMusic: {ex.Message}");
                        }

                        // Start heavy init while the title idle plays (Background priority so frames stay smooth).
                        StartGameWarmup(updateStatus);

                        titleIdleCts?.Cancel();
                        titleIdleCts?.Dispose();
                        titleIdleCts = new CancellationTokenSource();
                        var idleToken = titleIdleCts.Token;

                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await TitleScreenHelper.ShowAnimatedTitleScreenAsync(
                                    idleToken,
                                    onReadyForKey: () =>
                                    {
                                        waitingForKeyAfterAnimation = true;
                                    }).ConfigureAwait(false);
                            }
                            catch (OperationCanceledException)
                            {
                                // Expected when the player presses a key during idle.
                            }
                            catch (Exception ex)
                            {
                                DebugLogger.Log("GameInitializationHandler",
                                    $"Title animation failed: {ex.Message}");
                                await Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    if (!waitingForKeyAfterAnimation && !isInitialized)
                                    {
                                        updateStatus($"Error displaying title screen: {ex.Message}");
                                        InitializeGameAfterAnimation(updateStatus);
                                    }
                                });
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        updateStatus($"Error displaying title screen: {ex.Message}");
                        InitializeGameAfterAnimation(updateStatus);
                    }

                    // Don't proceed to main menu yet - wait for key press after intro
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
        /// Builds GameCoordinator + audio wiring on the UI thread at Background priority so title
        /// idle frames (Normal priority) stay responsive. Idempotent.
        /// </summary>
        private void StartGameWarmup(Action<string> updateStatus)
        {
            if (System.Threading.Interlocked.Exchange(ref gameWarmupStarted, 1) != 0)
                return;

            gameWarmupTask = Task.Run(async () =>
            {
                try
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (canvasUIManager == null)
                        {
                            updateStatus("Error: UI Manager not initialized");
                            return;
                        }

                        if (game != null)
                            return;

                        game = new GameCoordinator(canvasUIManager);
                        game.SetUIManager(canvasUIManager);

                        // Wire up the audio system once the GameStateManager exists so the music controller
                        // reacts to state changes (MainMenu / Dungeon / Combat / Death / etc.) and the cue
                        // dispatcher is subscribed to the combat event bus. Silent fallback if the audio
                        // backend cannot initialize (e.g. headless CI).
                        try
                        {
                            AudioBootstrap.Initialize(game.StateManager);
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.Log("GameInitializationHandler", $"AudioBootstrap.Initialize threw: {ex.Message}");
                        }

                        // Set the game reference in CanvasUICoordinator so it can access handlers for interactive panels
                        if (canvasUIManager is CanvasUICoordinator canvasUIForGame)
                        {
                            canvasUIForGame.SetGame(game);
                        }

                        // Update mouse handler with game reference (stats state is resolved live from the active display manager)
                        if (mouseHandler != null && canvasUIManager is CanvasUICoordinator canvasUIForMouse)
                        {
                            mouseHandler = new MouseInteractionHandler(gameCanvas, canvasUIForMouse, game);
                        }

                        // Warm save-file listing so ShowMainMenu does not hitch on first disk scan.
                        try { CharacterSaveManager.ListAllSavedCharacters(); }
                        catch (Exception ex)
                        {
                            DebugLogger.Log("GameInitializationHandler", $"Save list warm failed: {ex.Message}");
                        }
                    }, DispatcherPriority.Background);
                }
                catch (Exception ex)
                {
                    DebugLogger.Log("GameInitializationHandler",
                        $"Game warmup failed: {ex.Message}\n{ex.StackTrace}");
                    throw;
                }
            });
        }

        /// <summary>
        /// After title keypress (or title failure): await warmup if needed, then show main menu.
        /// </summary>
        private async Task TransitionToMainMenuAfterWarmupAsync(Action<string> updateStatus)
        {
            try
            {
                StartGameWarmup(updateStatus);

                if (gameWarmupTask != null)
                    await gameWarmupTask.ConfigureAwait(false);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (game == null)
                    {
                        updateStatus("Error initializing game: GameCoordinator was not created");
                        return;
                    }

                    game.ShowMainMenu();
                    isInitialized = true;
                    waitingForKeyAfterAnimation = false;
                });
            }
            catch (Exception ex)
            {
                DebugLogger.Log("GameInitializationHandler",
                    $"Error in TransitionToMainMenuAfterWarmupAsync: {ex.Message}\n{ex.StackTrace}");
                Dispatcher.UIThread.Post(() =>
                {
                    updateStatus($"Error initializing game: {ex.Message}");
                    game?.ShowMainMenu();
                    isInitialized = true;
                    waitingForKeyAfterAnimation = false;
                });
            }
        }

        /// <summary>
        /// Completes game initialization after title screen animation.
        /// Safe to call when title is skipped/fails: starts warmup if needed then shows the menu.
        /// </summary>
        public void InitializeGameAfterAnimation(Action<string> updateStatus)
        {
            _ = TransitionToMainMenuAfterWarmupAsync(updateStatus);
        }

        /// <summary>
        /// Automatically loads the first available saved character
        /// If no save exists or loading fails, silently shows the main menu without a character
        /// Includes timeout mechanism to prevent hanging
        /// </summary>
        private async Task AutoLoadCharacter(Action<string> updateStatus)
        {
            // Capture references on UI thread
            GameCoordinator? localGame = null;
            IUIManager? localCanvasUIManager = null;
            
            try
            {
                DebugLogger.Log("GameInitializationHandler", "AutoLoadCharacter started");
                
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    localGame = game;
                    localCanvasUIManager = canvasUIManager;
                    
                    if (localGame == null || localCanvasUIManager == null)
                    {
                        DebugLogger.Log("GameInitializationHandler", "Game or UI manager not initialized, showing main menu");
                        // Silently show main menu if game not initialized
                        localGame?.ShowMainMenu();
                        return;
                    }

                    // Check if save file exists first (same check MainMenuHandler uses)
                    bool saveExists = CharacterSaveManager.SaveFileExists();
                    DebugLogger.Log("GameInitializationHandler", $"Save file exists: {saveExists}");
                    
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
                        canvasUI.ShowLoadingStatus("Loading saved character...");
                    }
                    DebugLogger.Log("GameInitializationHandler", "Showing loading status, starting character load");
                });

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
                        DebugLogger.Log("GameInitializationHandler", "LoadGame succeeded, showing game loop");
                        Dispatcher.UIThread.Post(() =>
                        {
                            if (localCanvasUIManager is CanvasUICoordinator canvasUIClear)
                            {
                                canvasUIClear.ClearLoadingStatus();
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
                var completedTask = await Task.WhenAny(loadTask, timeoutTask);

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
                bool loadSuccess = await loadTask;

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
            }
            catch (Exception ex)
            {
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
        /// Handles key press after title screen animation.
        /// Cancels idle and shows the main menu; GameCoordinator was warmed during the title.
        /// </summary>
        public void HandleKeyAfterAnimation(Action<string> updateStatus)
        {
            if (waitingForKeyAfterAnimation)
            {
                waitingForKeyAfterAnimation = false;
                try
                {
                    titleIdleCts?.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // Ignore — idle already stopped.
                }
                _ = TransitionToMainMenuAfterWarmupAsync(updateStatus);
            }
        }
    }
}

