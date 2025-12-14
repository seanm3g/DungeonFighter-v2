using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Media;
using Avalonia.Threading;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Helpers;
using RPGGame.UI.Avalonia.Handlers;
using RPGGame.UI.TitleScreen;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RPGGame.UI.Avalonia
{
    public partial class MainWindow : Window
    {
        private GameCoordinator? game;
        private IUIManager? canvasUIManager;
        private bool isInitialized = false;
        private MouseInteractionHandler? mouseHandler;

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

        private bool waitingForKeyAfterAnimation = false;
        
        private void InitializeGame()
        {
            try
            {
                // Initialize the canvas UI manager
                canvasUIManager = new CanvasUICoordinator(GameCanvas);
                
                // Set the close action for the UI manager
                if (canvasUIManager is CanvasUICoordinator canvasUI)
                {
                    canvasUI.SetCloseAction(() =>
                    {
                        Dispatcher.UIThread.Post(() => this.Close());
                    });
                    // Set the main window reference so the coordinator can show/hide panels
                    canvasUI.SetMainWindow(this);
                }
                
                // Set the UI manager for the static UIManager class
                UIManager.SetCustomUIManager(canvasUIManager);
                
                // Initialize mouse interaction handler
                if (canvasUIManager is CanvasUICoordinator canvasUIForMouse)
                {
                    mouseHandler = new MouseInteractionHandler(GameCanvas, canvasUIForMouse, null);
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
                        UpdateStatus($"Error displaying title screen: {ex.Message}");
                        // If title screen fails, proceed to main menu
                        InitializeGameAfterAnimation();
                    }
                    
                    // Don't proceed to main menu yet - wait for key press
                    return;
                }
                
                // If no animation, initialize normally
                InitializeGameAfterAnimation();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error initializing game: {ex.Message}");
                // If initialization fails, try to initialize without animation
                try
                {
                    InitializeGameAfterAnimation();
                }
                catch (Exception ex2)
                {
                    UpdateStatus($"Critical error: {ex2.Message}");
                }
            }
        }
        
        private void InitializeGameAfterAnimation()
        {
            try
            {
                // Check if canvasUIManager is initialized
                if (canvasUIManager == null)
                {
                    UpdateStatus("Error: UI Manager not initialized");
                    return;
                }
                
                // Initialize the game with canvas UI
                game = new GameCoordinator(canvasUIManager);
                game.SetUIManager(canvasUIManager);
                
                // Update mouse handler with game reference
                if (mouseHandler != null && canvasUIManager is CanvasUICoordinator canvasUIForMouse)
                {
                    mouseHandler = new MouseInteractionHandler(GameCanvas, canvasUIForMouse, game);
                }
                
                // Show the main menu (now async)
                game.ShowMainMenu();
                
                isInitialized = true;
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error initializing game: {ex.Message}");
            }
        }

        private async void OnKeyDown(object? sender, KeyEventArgs e)
        {
            // If waiting for key after animation, initialize game
            if (waitingForKeyAfterAnimation)
            {
                waitingForKeyAfterAnimation = false;
                InitializeGameAfterAnimation();
                return;
            }
            
            if (!isInitialized || game == null) return;

            try
            {
                // Handle special keys first
                if (e.Key == Key.H)
                {
                    ToggleHelp();
                    return;
                }

                if (e.Key == Key.Escape)
                {
                    // Handle escape key based on current game state
                    await game.HandleEscapeKey();
                    return;
                }

                // Convert Avalonia keys to game input
                string? input = ConvertKeyToInput(e.Key, e.KeyModifiers);
                if (input != null)
                {
                    // Debug: Log key press for troubleshooting
                    DebugLogger.Log("MainWindow", $"Calling game.HandleInput('{input}')");
                    await game.HandleInput(input);
                    DebugLogger.Log("MainWindow", $"game.HandleInput('{input}') completed");
                }
                else
                {
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

        private string? ConvertKeyToInput(Key key, KeyModifiers modifiers)
        {
            return key switch
            {
                Key.D1 or Key.NumPad1 => "1",
                Key.D2 or Key.NumPad2 => "2",
                Key.D3 or Key.NumPad3 => "3",
                Key.D4 or Key.NumPad4 => "4",
                Key.D5 or Key.NumPad5 => "5",
                Key.D6 or Key.NumPad6 => "6",
                Key.D7 or Key.NumPad7 => "7",
                Key.D8 or Key.NumPad8 => "8",
                Key.D9 or Key.NumPad9 => "9",
                Key.D0 or Key.NumPad0 => "0",
                Key.Enter => "enter",
                Key.Space => "space",
                Key.Back => "backspace",
                Key.Delete => "delete",
                Key.Left => "left",
                Key.Right => "right",
                Key.Up => "up",
                Key.Down => "down",
                Key.Tab => "tab",
                _ => null
            };
        }

        private void ToggleHelp()
        {
            // Toggle help display on canvas
            if (canvasUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.ToggleHelp();
            }
        }

        private void UpdateStatus(string message)
        {
            // Update status on canvas
            if (canvasUIManager is CanvasUICoordinator canvasUI)
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
            if (!isInitialized || mouseHandler == null) return;
            mouseHandler.HandlePointerPressed(e);
        }

        private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
        {
            if (!isInitialized || mouseHandler == null) return;
            mouseHandler.HandlePointerMoved(e);
        }

        private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (!isInitialized || mouseHandler == null) return;
            mouseHandler.HandlePointerReleased(e);
        }

        private async Task CopyCenterPanelToClipboard()
        {
            if (canvasUIManager is CanvasUICoordinator canvasUI)
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

    }
}
