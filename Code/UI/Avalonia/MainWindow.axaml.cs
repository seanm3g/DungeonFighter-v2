using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Helpers;
using RPGGame.UI.Avalonia.Handlers;
using RPGGame.Utils;
using System;
using System.Threading.Tasks;

namespace RPGGame.UI.Avalonia
{
    public partial class MainWindow : Window
    {
        private GameInitializationHandler? initializationHandler;
        private MainWindowInputHandler? inputHandler;

        public MainWindow()
        {
            InitializeComponent();
            this.KeyDown += OnKeyDown;
            this.KeyUp += OnKeyUp;
            
            // Add mouse event handling
            GameCanvas.PointerPressed += OnCanvasPointerPressed;
            GameCanvas.PointerMoved += OnCanvasPointerMoved;
            GameCanvas.PointerReleased += OnCanvasPointerReleased;
            
            // Initialize the game and UI
            InitializeGame();
        }
        
        private void InitializeGame()
        {
            initializationHandler = new GameInitializationHandler(GameCanvas, this);
            initializationHandler.InitializeGame(UpdateStatus);
        }

        private async void OnKeyDown(object? sender, KeyEventArgs e)
        {
            // If waiting for key after animation, initialize game
            if (initializationHandler != null && initializationHandler.WaitingForKeyAfterAnimation)
            {
                initializationHandler.HandleKeyAfterAnimation(UpdateStatus);
                return;
            }
            
            if (initializationHandler == null || !initializationHandler.IsInitialized || initializationHandler.Game == null) 
                return;

            try
            {
                // Initialize input handler if needed
                if (inputHandler == null)
                {
                    inputHandler = new MainWindowInputHandler(initializationHandler.Game);
                }

                // Handle special keys first
                if (e.Key == Key.H)
                {
                    ToggleHelp();
                    return;
                }

                if (e.Key == Key.Escape)
                {
                    await initializationHandler.Game.HandleEscapeKey();
                    return;
                }

                // Convert Avalonia keys to game input using utility
                string? input = inputHandler.ConvertKeyToInput(e.Key, e.KeyModifiers);
                if (input != null)
                {
                    DebugLogger.Log("MainWindow", $"Calling game.HandleInput('{input}')");
                    await initializationHandler.Game.HandleInput(input);
                    DebugLogger.Log("MainWindow", $"game.HandleInput('{input}') completed");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error handling input: {ex.Message}");
            }
        }

        private void OnKeyUp(object? sender, KeyEventArgs e)
        {
            // Handle key up events if needed
        }

        private void ToggleHelp()
        {
            // Toggle help display on canvas
            if (initializationHandler?.CanvasUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.ToggleHelp();
            }
        }

        private void UpdateStatus(string message)
        {
            // Update status on canvas
            if (initializationHandler?.CanvasUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.UpdateStatus(message);
            }
            
            // Also update the status text block in XAML as a fallback
            StatusUpdateHelper.UpdateStatusText(StatusText, message);
        }

        public void UpdateGameState(string status, string help = "")
        {
            UpdateStatus(status);
            HelpText.Text = help;
        }

        // Mouse event handlers - delegate to MouseInteractionHandler
        private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (initializationHandler == null || !initializationHandler.IsInitialized || initializationHandler.MouseHandler == null) 
                return;
            initializationHandler.MouseHandler.HandlePointerPressed(e);
        }

        private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
        {
            if (initializationHandler == null || !initializationHandler.IsInitialized || initializationHandler.MouseHandler == null) 
                return;
            initializationHandler.MouseHandler.HandlePointerMoved(e);
        }

        private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (initializationHandler == null || !initializationHandler.IsInitialized || initializationHandler.MouseHandler == null) 
                return;
            initializationHandler.MouseHandler.HandlePointerReleased(e);
        }

        private async Task CopyCenterPanelToClipboard()
        {
            if (initializationHandler?.CanvasUIManager is CanvasUICoordinator canvasUI)
            {
                await ClipboardHelper.CopyDisplayBufferToClipboard(canvasUI, this, StatusText);
            }
            else
            {
                UpdateStatus("Canvas UI not available");
            }
        }

        /// <summary>
        /// Shows the tuning menu panel with the specified variable editor
        /// </summary>
        public void ShowTuningMenuPanel(RPGGame.Editors.VariableEditor variableEditor)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (TuningMenuPanel != null && TuningPanelOverlay != null)
                {
                    TuningMenuPanel.Initialize(variableEditor);
                    TuningPanelOverlay.IsVisible = true;
                }
            });
        }

        /// <summary>
        /// Hides the tuning menu panel
        /// </summary>
        public void HideTuningMenuPanel()
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (TuningPanelOverlay != null)
                {
                    TuningPanelOverlay.IsVisible = false;
                }
            });
        }
        
        /// <summary>
        /// Shows the settings panel
        /// </summary>
        public void ShowSettingsPanel()
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (SettingsPanelOverlay != null && SettingsMenuPanel != null)
                {
                    // Suppress canvas rendering to hide ASCII menu
                    if (initializationHandler?.CanvasUIManager is CanvasUICoordinator canvasUI)
                    {
                        canvasUI.SuppressDisplayBufferRendering();
                        canvasUI.ClearDisplayBufferWithoutRender();
                    }
                    
                    // Set up callbacks for back button and status updates
                    SettingsMenuPanel.SetBackCallback(() =>
                    {
                        HideSettingsPanel();
                        if (initializationHandler?.Game != null)
                        {
                            // Fire and forget - HandleEscapeKey will handle state transitions
                            _ = initializationHandler.Game.HandleEscapeKey();
                        }
                    });
                    
                    SettingsMenuPanel.SetStatusCallback(UpdateStatus);
                    
                    // Initialize handlers for testing and developer tools
                    // Always call InitializeHandlers, even if some values might be null
                    // This ensures the panel has references to what's available
                    CanvasUICoordinator? canvasUIForHandlers = initializationHandler?.CanvasUIManager as CanvasUICoordinator;
                    SettingsMenuPanel.InitializeHandlers(
                        initializationHandler?.Game?.TestingSystemHandler,
                        initializationHandler?.Game?.DeveloperMenuHandler,
                        initializationHandler?.Game,
                        canvasUIForHandlers,
                        initializationHandler?.Game?.StateManager);
                    
                    SettingsPanelOverlay.IsVisible = true;
                }
            });
        }
        
        /// <summary>
        /// Hides the settings panel
        /// </summary>
        public void HideSettingsPanel()
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (SettingsPanelOverlay != null)
                {
                    SettingsPanelOverlay.IsVisible = false;
                    
                    // Restore canvas rendering when hiding settings
                    if (initializationHandler?.CanvasUIManager is CanvasUICoordinator canvasUI)
                    {
                        canvasUI.RestoreDisplayBufferRendering();
                    }
                }
            });
        }
    }
}
