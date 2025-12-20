using Avalonia.Threading;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Handlers;
using RPGGame.UI.TitleScreen;
using System;

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
        public void InitializeGameAfterAnimation(Action<string> updateStatus)
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

                // Show the main menu (now async)
                game.ShowMainMenu();

                isInitialized = true;
                waitingForKeyAfterAnimation = false;
            }
            catch (Exception ex)
            {
                updateStatus($"Error initializing game: {ex.Message}");
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

