using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Media;
using Avalonia.Threading;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.TitleScreen;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RPGGame.UI.Avalonia
{
    public partial class MainWindow : Window
    {
        private Game? game;
        private IUIManager? canvasUIManager;
        private bool isInitialized = false;

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
                }
                
                // Set the UI manager for the static UIManager class
                UIManager.SetCustomUIManager(canvasUIManager);
                
                // Show opening animation first (using the new animated title screen)
                if (canvasUIManager is CanvasUICoordinator canvasUI2)
                {
                    // Run animation on UI thread to avoid deadlocks
                    Dispatcher.UIThread.Post(async () =>
                    {
                        try
                        {
                            // Step 1: Show black screen for 1 second
                            await Task.Delay(1000);
                            
                            // Step 2: Show a simple static title screen
                            TitleScreenHelper.ShowStaticTitleScreen();
                            
                            // Set flag to wait for key press
                            waitingForKeyAfterAnimation = true;
                        }
                        catch (Exception ex)
                        {
                            UpdateStatus($"Error during title animation: {ex.Message}");
                            // If animation fails, proceed to main menu
                            InitializeGameAfterAnimation();
                        }
                    }, DispatcherPriority.Background);
                    
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
                game = new Game(canvasUIManager);
                game.SetUIManager(canvasUIManager);
                
                // Show the main menu (now async)
                game.ShowMainMenu();
                
                isInitialized = true;
                UpdateStatus("Game initialized. Press H for help.");
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
                    await game.HandleInput(input);
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
        }

        public void UpdateGameState(string status, string help = "")
        {
            UpdateStatus(status);
            HelpText.Text = help;
        }

        // Mouse event handlers
        private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (!isInitialized || game == null) return;

            var point = e.GetCurrentPoint(GameCanvas);
            if (point.Properties.IsLeftButtonPressed)
            {
                HandleMouseClick(point.Position);
            }
        }

        private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
        {
            if (!isInitialized || game == null) return;

            var point = e.GetCurrentPoint(GameCanvas);
            HandleMouseHover(point.Position);
        }

        private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            // Handle mouse release if needed
        }

        private void HandleMouseClick(Point position)
        {
            if (canvasUIManager is CanvasUICoordinator canvasUI)
            {
                // Convert screen coordinates to character grid coordinates
                var gridPos = ScreenToGrid(position);
                
                // Check if click is on a clickable element
                var clickedElement = canvasUI.GetElementAt(gridPos.X, gridPos.Y);
                if (clickedElement != null)
                {
                    // Process the click
                    ProcessElementClick(clickedElement);
                }
            }
        }

        private void HandleMouseHover(Point position)
        {
            if (canvasUIManager is CanvasUICoordinator canvasUI)
            {
                // Convert screen coordinates to character grid coordinates
                var gridPos = ScreenToGrid(position);
                
                // Update hover state
                canvasUI.SetHoverPosition(gridPos.X, gridPos.Y);
            }
        }

        private (int X, int Y) ScreenToGrid(Point screenPosition)
        {
            // Convert screen coordinates to character grid coordinates
            // Assuming monospace font with 9.5px width and 16px height
            const double charWidth = 9;
            const double charHeight = 16;
            
            int gridX = (int)(screenPosition.X / charWidth);
            int gridY = (int)(screenPosition.Y / charHeight);
            
            return (gridX, gridY);
        }

        private async void ProcessElementClick(ClickableElement element)
        {
            if (game == null) return;

            switch (element.Type)
            {
                case ElementType.MenuOption:
                    await game.HandleInput(element.Value);
                    break;
                case ElementType.Item:
                    // Handle item selection
                    UpdateStatus($"Selected item: {element.Value}");
                    break;
                case ElementType.Button:
                    // Handle button click
                    await game.HandleInput(element.Value);
                    break;
            }
        }
    }
}



