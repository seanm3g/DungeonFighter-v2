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
        /// </summary>
        public async void InitializeGameAfterAnimation(Action<string> updateStatus)
        {
            try
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

                // Update mouse handler with game reference
                if (mouseHandler != null && canvasUIManager is CanvasUICoordinator canvasUIForMouse)
                {
                    mouseHandler = new MouseInteractionHandler(gameCanvas, canvasUIForMouse, game);
                }

                // Always try to auto-load a saved character first
                // If no save exists or load fails, it will show the main menu
                await AutoLoadCharacter(updateStatus);

                isInitialized = true;
                waitingForKeyAfterAnimation = false;
            }
            catch (Exception ex)
            {
                updateStatus($"Error initializing game: {ex.Message}");
                // On error, show main menu as fallback
                game?.ShowMainMenu();
            }
        }

        /// <summary>
        /// Automatically loads the first available saved character
        /// If no save exists or loading fails, silently shows the main menu without a character
        /// </summary>
        private async Task AutoLoadCharacter(Action<string> updateStatus)
        {
            try
            {
                if (game == null || canvasUIManager == null)
                {
                    // Silently show main menu if game not initialized
                    game?.ShowMainMenu();
                    return;
                }

                // Check if save file exists first (same check MainMenuHandler uses)
                if (!CharacterSaveManager.SaveFileExists())
                {
                    // No save file, silently show main menu without character
                    game.ShowMainMenu();
                    return;
                }

                // Show loading message briefly
                if (canvasUIManager is CanvasUICoordinator canvasUI)
                {
                    canvasUI.ShowLoadingStatus("Loading saved character...");
                }

                var stateManager = game.StateManager;
                var gameInitializer = new GameInitializer();
                var gameLoader = new GameLoader(stateManager, gameInitializer, canvasUIManager);

                // Load the character with appropriate callbacks
                // The GameLoader will handle showing the game loop on success
                // or calling the onShowMainMenu callback on failure
                // For auto-load, we suppress error messages - just show main menu silently
                bool loadSuccess = await gameLoader.LoadGame(
                    (msg) => {
                        // Only show loading status messages (not error messages) during auto-load
                        // Error messages start with "Error:" - suppress those for auto-load
                        if (!msg.StartsWith("Error:", StringComparison.OrdinalIgnoreCase) && 
                            !msg.StartsWith("No saved", StringComparison.OrdinalIgnoreCase) &&
                            !msg.StartsWith("Failed", StringComparison.OrdinalIgnoreCase))
                        {
                            if (canvasUIManager is CanvasUICoordinator canvasUIMsg)
                            {
                                canvasUIMsg.ShowLoadingStatus(msg);
                            }
                        }
                    },
                    () => {
                        // Show game loop after successful load
                        // This is called by GameLoader when load succeeds
                        game.ShowGameLoop();
                    },
                    () => {
                        // This callback is only called if load fails
                        // Silently show main menu without character (no error message)
                        game.ShowMainMenu();
                    }
                );

                // If load failed, GameLoader already called onShowMainMenu callback
                // Silently show main menu without character
                if (!loadSuccess)
                {
                    game.ShowMainMenu();
                }
            }
            catch (Exception ex)
            {
                // Log error but don't show to user - just silently show main menu
                DebugLogger.Log("GameInitializationHandler", $"Error auto-loading character: {ex.Message}");
                // On error, silently show main menu without character
                game?.ShowMainMenu();
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

