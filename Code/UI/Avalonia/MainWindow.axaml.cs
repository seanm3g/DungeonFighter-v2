using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Helpers;
using RPGGame.UI.Avalonia.Handlers;
using RPGGame.UI.Avalonia.Utils;
using RPGGame.Utils;
using System;
using System.Threading.Tasks;

namespace RPGGame.UI.Avalonia
{
    public partial class MainWindow : Window
    {
        private GameInitializationHandler? initializationHandler;
        private MainWindowInputHandler? inputHandler;
        private DispatcherTimer? combatSpeedNotificationTimer;

        public MainWindow()
        {
            InitializeComponent();
            Opened += OnMainWindowOpened;
            // Tunnel so Ctrl/Cmd+C is handled before focused children; bubble KeyDown on the window often never runs when focus is on the canvas.
            this.AddHandler(InputElement.KeyDownEvent, OnCombatLogCopyKeyDownTunnel, RoutingStrategies.Tunnel);
            this.KeyDown += OnKeyDown;
            this.KeyUp += OnKeyUp;
            
            // Pointer events on the transparent Border wrapper — GameCanvasControl (Control) has no Background, so hits would otherwise pass through.
            GameCanvasHitSurface.PointerPressed += OnCanvasPointerPressed;
            GameCanvasHitSurface.PointerMoved += OnCanvasPointerMoved;
            GameCanvasHitSurface.PointerReleased += OnCanvasPointerReleased;
            GameCanvasHitSurface.PointerWheelChanged += OnCanvasPointerWheelChanged;
            // After Pointer.Capture(GameCanvas), released/moved are routed to the captured element, not the parent Border — subscribe on the canvas too or combo drag never completes.
            GameCanvas.PointerMoved += OnCanvasPointerMoved;
            GameCanvas.PointerReleased += OnCanvasPointerReleased;
            
            // Initialize the game and UI
            InitializeGame();
        }

        private void OnMainWindowOpened(object? sender, EventArgs e)
        {
            Opened -= OnMainWindowOpened;
            BuildExecutionMetrics.RecordLaunchTime("GUI");
        }
        
        private void InitializeGame()
        {
            initializationHandler = new GameInitializationHandler(GameCanvas, this);
            initializationHandler.InitializeGame(UpdateStatus);
        }

        private async void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (TryHandleCombatSpeedKey(e.Key))
            {
                e.Handled = true;
                return;
            }

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

                // Combat log copy is handled in OnCombatLogCopyKeyDownTunnel (tunneling) so it runs with canvas focus.

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

                if (e.Key == Key.F8 && initializationHandler.Game.CurrentState == GameState.MainMenu
                    && initializationHandler.CanvasUIManager is CanvasUICoordinator canvasUiForLab)
                {
                    e.Handled = true;
                    await initializationHandler.Game.StartActionInteractionLabAsync(
                        canvasUiForLab,
                        GameCoordinator.GetBarbarianStarterWeaponChoice1Based()).ConfigureAwait(true);
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

        private bool TryHandleCombatSpeedKey(Key key)
        {
            if (key != Key.PageUp && key != Key.PageDown)
                return false;

            int speed = key == Key.PageUp
                ? DeveloperModeState.IncreaseCombatSpeed()
                : DeveloperModeState.DecreaseCombatSpeed();

            if (initializationHandler?.CanvasUIManager is CanvasUICoordinator canvasUI)
                canvasUI.RefreshCenterPanelModeTint();

            ShowCombatSpeedNotification($"Combat speed: {speed}x");
            return true;
        }

        private void ShowCombatSpeedNotification(string message)
        {
            CombatSpeedNotificationText.Text = message;
            CombatSpeedNotificationText.IsVisible = true;

            combatSpeedNotificationTimer ??= new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            combatSpeedNotificationTimer.Stop();
            combatSpeedNotificationTimer.Tick -= HideCombatSpeedNotification;

            // Keep the footer label visible while combat is accelerated; only 1x auto-dismisses.
            if (DeveloperModeState.CombatSpeedMultiplier <= 1)
            {
                combatSpeedNotificationTimer.Tick += HideCombatSpeedNotification;
                combatSpeedNotificationTimer.Start();
            }
        }

        private void HideCombatSpeedNotification(object? sender, EventArgs e)
        {
            combatSpeedNotificationTimer?.Stop();
            CombatSpeedNotificationText.IsVisible = false;
        }

        private void OnKeyUp(object? sender, KeyEventArgs e)
        {
            // Handle key up events if needed
        }

        private async void OnCombatLogCopyKeyDownTunnel(object? sender, KeyEventArgs e)
        {
            if (!KeyInputConverter.IsCombatLogCopyChord(e.Key, e.KeyModifiers))
                return;
            if (initializationHandler == null || !initializationHandler.IsInitialized || initializationHandler.Game == null)
                return;
            if (SettingsPanelOverlay?.IsVisible == true || TuningPanelOverlay?.IsVisible == true)
                return;
            if (initializationHandler.CanvasUIManager is not CanvasUICoordinator canvasForCopy)
                return;
            if (!canvasForCopy.IsCombatLogClipboardContext())
                return;
            e.Handled = true;
            await ClipboardHelper.CopyDisplayBufferToClipboard(canvasForCopy, this, null, UpdateStatus);
        }

        private void ToggleHelp()
        {
            // Toggle help display on canvas
            if (initializationHandler?.CanvasUIManager is CanvasUICoordinator canvasUI) {
                canvasUI.ToggleHelp();
            }
        }

        private void UpdateStatus(string message)
        {
            // Update status on canvas
            if (initializationHandler?.CanvasUIManager is CanvasUICoordinator canvasUI) {
                canvasUI.UpdateStatus(message);
            }
            
            // Status bar removed - status updates only go to canvas
        }

        public void UpdateGameState(string status, string help = "")
        {
            UpdateStatus(status);
            // Status bar removed - help text no longer displayed
        }

        // Mouse event handlers - delegate to MouseInteractionHandler
        private async void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (initializationHandler == null || !initializationHandler.IsInitialized)
                return;
            // Combat log copy does not require MouseInteractionHandler; avoid gating copy on mouse wiring.
            if (await TryHandleCombatLogRightClickCopy(e))
                return;
            if (initializationHandler.MouseHandler == null)
                return;
            initializationHandler.MouseHandler.HandlePointerPressed(e);
        }

        private async Task<bool> TryHandleCombatLogRightClickCopy(PointerPressedEventArgs e)
        {
            if (initializationHandler?.CanvasUIManager is not CanvasUICoordinator canvasUI)
                return false;

            var grid = PointerToCanvasGrid(e);
            Point localOnCanvas = e.GetPosition(GameCanvas);
            double cw = GameCanvas.GetCharWidth();
            double ch = GameCanvas.GetCharHeight();
            bool overlayOpen = SettingsPanelOverlay?.IsVisible == true || TuningPanelOverlay?.IsVisible == true;
            var pointOnHitSurface = e.GetCurrentPoint(GameCanvasHitSurface);
            var pointOnCanvas = e.GetCurrentPoint(GameCanvas);
            bool isRightClick = pointOnHitSurface.Properties.IsRightButtonPressed
                || pointOnCanvas.Properties.IsRightButtonPressed;
            if (!CombatLogCopyInput.ShouldCopyOnRightClick(
                isRightClick,
                overlayOpen,
                canvasUI.IsCombatLogClipboardContext(),
                grid.X,
                grid.Y,
                localOnCanvas.X,
                localOnCanvas.Y,
                cw,
                ch))
            {
                return false;
            }

            e.Handled = true;
            await ClipboardHelper.CopyDisplayBufferToClipboard(canvasUI, this, null, UpdateStatus);
            return true;
        }

        /// <summary>
        /// Maps a pointer position to character grid coordinates on the game canvas.
        /// Uses the hit surface and canvas origin so letterboxing (canvas smaller than the border) does not skew the cell index.
        /// </summary>
        private (int X, int Y) PointerToCanvasGrid(PointerEventArgs e)
        {
            double charWidth = GameCanvas.GetCharWidth();
            double charHeight = GameCanvas.GetCharHeight();
            if (charWidth <= 0 || charHeight <= 0)
                return (0, 0);

            // Prefer pointer position in GameCanvas coordinates (Avalonia handles parent/letterbox transform).
            // TranslatePoint(canvas origin → hit surface) can be null while the tree is updating and used to force (0,0), breaking hit-tests.
            Point local = e.GetPosition(GameCanvas);
            int gx = (int)Math.Floor(local.X / charWidth);
            int gy = (int)Math.Floor(local.Y / charHeight);
            return (gx, gy);
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

        private void OnCanvasPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (initializationHandler == null || !initializationHandler.IsInitialized || initializationHandler.MouseHandler == null)
                return;
            initializationHandler.MouseHandler.HandlePointerWheelChanged(e);
        }

        private async Task CopyCenterPanelToClipboard()
        {
            if (initializationHandler?.CanvasUIManager is CanvasUICoordinator canvasUI)
                await ClipboardHelper.CopyDisplayBufferToClipboard(canvasUI, this, null, UpdateStatus);
            else
                UpdateStatus("Canvas UI not available");
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
                    // Only reload from file when actually opening (overlay was hidden). Avoids overwriting in-memory edits if ShowSettingsPanel runs again while already visible.
                    if (!SettingsPanelOverlay.IsVisible)
                    {
                        GameSettings.ReloadFromFile();
                        SettingsMenuPanel.RefreshSettingsFromFile();
                    }
                    // Suppress canvas rendering to hide ASCII menu
                    if (initializationHandler?.CanvasUIManager is CanvasUICoordinator canvasUI) {
                        // Clear the entire canvas to remove the main menu
                        canvasUI.Clear();

                        // Suppress display buffer rendering to prevent main menu from re-rendering
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
            void DoHide()
            {
                if (SettingsPanelOverlay != null)
                {
                    SettingsPanelOverlay.IsVisible = false;

                    // Restore canvas rendering when hiding settings
                    if (initializationHandler?.CanvasUIManager is CanvasUICoordinator canvasUI)
                        canvasUI.RestoreDisplayBufferRendering();
                }
            }

            if (Dispatcher.UIThread.CheckAccess())
                DoHide();
            else
                Dispatcher.UIThread.Post(DoHide);
        }
    }
}
