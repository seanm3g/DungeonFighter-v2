using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using RPGGame;
using RPGGame.Combat.Sequence;
using RPGGame.Config;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Effects;
using RPGGame.UI.Avalonia.Help;
using RPGGame.UI.Avalonia.Helpers;
using RPGGame.UI.Avalonia.Handlers;
using RPGGame.UI.Avalonia.Settings;
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
        private SettingsPanel? settingsMenuPanel;
        private TuningMenuPanel? tuningMenuPanel;

        /// <summary>Client size at 100% UI zoom; Ctrl+/- scales the window from this reference.</summary>
        private double _uiZoomReferenceWidth;
        private double _uiZoomReferenceHeight;
        private bool _uiZoomReferenceReady;
        private bool _applyingUiZoomWindowSize;

        public MainWindow()
        {
            InitializeComponent();
            Opened += OnMainWindowOpened;
            Resized += OnMainWindowResized;
            // Tunnel so Ctrl/Cmd chords are handled before focused children; bubble KeyDown on the window often never runs when focus is on the canvas.
            this.AddHandler(InputElement.KeyDownEvent, OnGlobalKeyDownTunnel, RoutingStrategies.Tunnel);
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
            GameCanvas.GridDimensionsChanged += OnGameCanvasGridDimensionsChanged;

            Closed += OnMainWindowClosed;
            NarrativeVideoOverlay?.AttachCanvas(GameCanvas);
            NarrativeVideoOverlay?.SetInDungeonProvider(
                () =>
                {
                    var sm = initializationHandler?.Game?.StateManager;
                    return NarrativeVideoOverlayGate.CountsAsActiveDungeonRunForOverlay(
                        sm?.HasCurrentDungeon == true,
                        sm?.CurrentState);
                });
            NarrativeVideoOverlay?.ReloadConfigAndMaybeStart();

            LoadPersistedGameFontPreferences();
            
            // Initialize the game and UI
            InitializeGame();
        }

        private void LoadPersistedGameFontPreferences()
        {
            try
            {
                GeneralSettingsStore.EnsureBootstrapped();
                GameFonts.LoadFromStore();
            }
            catch (Exception ex)
            {
                DebugLogger.Log("MainWindow", $"Could not load UI font preferences: {ex.Message}");
            }

            ApplyActiveGameFontToChrome();
            GameCanvas.ApplyActiveFont();
        }

        private void OnMainWindowClosed(object? sender, EventArgs e)
        {
            NarrativeVideoOverlay?.Dispose();
        }

        private void OnGameCanvasGridDimensionsChanged()
        {
            // Measure/arrange may raise this; rebuild chrome on the next UI tick so panel widths track the new column count.
            Dispatcher.UIThread.Post(() =>
            {
                if (initializationHandler?.CanvasUIManager is CanvasUICoordinator canvasUI)
                    canvasUI.ForceFullLayoutRender();
                NarrativeVideoOverlay?.NotifyContextChanged();
            }, DispatcherPriority.Render);
        }

        /// <summary>
        /// Fills window letterbox regions (outside the character-grid canvas) with a solid color.
        /// Pass null to restore the default transparent hit-surface + black window chrome.
        /// </summary>
        public void SetShellBackgroundColor(Color? color)
        {
            if (color.HasValue)
            {
                var brush = new SolidColorBrush(color.Value);
                Background = brush;
                if (Content is Panel rootPanel)
                    rootPanel.Background = brush;
                GameCanvasHitSurface.Background = brush;
            }
            else
            {
                Background = Brushes.Black;
                if (Content is Panel rootPanel)
                    rootPanel.Background = Brushes.Black;
                GameCanvasHitSurface.Background = Brushes.Transparent;
            }
        }

        /// <summary>
        /// Makes the window visible after the first title frame is painted while minimized.
        /// Opacity 0 still shows a black window on Windows, so startup stays minimized.
        /// </summary>
        public void RevealAfterTitleReady()
        {
            Opacity = TitleToMenuBootstrap.GetStartupWindowOpacity(titleFirstFrameReady: true);
            ShowInTaskbar = TitleToMenuBootstrap.GetStartupShowInTaskbar(titleFirstFrameReady: true);
            WindowState = TitleToMenuBootstrap.GetStartupWindowState(titleFirstFrameReady: true);
            ShowActivated = true;
            if (!IsVisible)
                Show();
            Activate();
        }

        private void OnMainWindowOpened(object? sender, EventArgs e)
        {
            Opened -= OnMainWindowOpened;

            if (OperatingSystem.IsMacOS())
            {
                Dispatcher.UIThread.Post(() =>
                {
                    ApplyMacStartupWindowSizing();
                    CaptureUiZoomReferenceFromCurrentSize(atZoom: 1.0);
                    ApplyPersistedUiZoomWindowSize();
                    _ = initializationHandler?.StartTitleScreenAfterWindowReadyAsync();
                    BuildExecutionMetrics.RecordLaunchTime("GUI");
                    Dispatcher.UIThread.Post(() =>
                    {
                        ApplyMacStartupWindowSizing();
                        CaptureUiZoomReferenceFromCurrentSize(atZoom: 1.0);
                        ApplyPersistedUiZoomWindowSize();
                    }, DispatcherPriority.Background);
                }, DispatcherPriority.Loaded);
                return;
            }

            CaptureUiZoomReferenceFromCurrentSize(atZoom: 1.0);
            ApplyPersistedUiZoomWindowSize();
            _ = initializationHandler?.StartTitleScreenAfterWindowReadyAsync();
            BuildExecutionMetrics.RecordLaunchTime("GUI");
        }

        private void OnMainWindowResized(object? sender, WindowResizedEventArgs e)
        {
            if (_applyingUiZoomWindowSize)
                return;
            // Manual drag/maximize: treat current client size as zoomed size for the active zoom.
            if (Width > 0 && Height > 0)
                CaptureUiZoomReferenceFromCurrentSize(GameFonts.ActiveZoom);
        }

        /// <summary>
        /// Stores the 100%-zoom client size derived from the current window and <paramref name="atZoom"/>.
        /// </summary>
        private void CaptureUiZoomReferenceFromCurrentSize(double atZoom)
        {
            var (refW, refH) = MainWindowStartupSizing.ComputeZoomReferenceSize(Width, Height, atZoom);
            if (refW <= 0 || refH <= 0)
                return;
            _uiZoomReferenceWidth = refW;
            _uiZoomReferenceHeight = refH;
            _uiZoomReferenceReady = true;
        }

        private void EnsureUiZoomReference()
        {
            if (_uiZoomReferenceReady && _uiZoomReferenceWidth > 0 && _uiZoomReferenceHeight > 0)
                return;
            CaptureUiZoomReferenceFromCurrentSize(GameFonts.ActiveZoom);
        }

        /// <summary>
        /// Resizes the window so Ctrl+/- zoom fills the client area (no letterbox under the panels).
        /// </summary>
        private void ApplyUiZoomWindowSize(double zoom)
        {
            EnsureUiZoomReference();
            if (!_uiZoomReferenceReady)
                return;

            _applyingUiZoomWindowSize = true;
            try
            {
                MainWindowStartupSizing.ApplyUiZoomWindowSize(
                    this,
                    _uiZoomReferenceWidth,
                    _uiZoomReferenceHeight,
                    zoom);
            }
            finally
            {
                _applyingUiZoomWindowSize = false;
            }
        }

        private void ApplyPersistedUiZoomWindowSize()
        {
            double zoom = GameFonts.ActiveZoom;
            if (Math.Abs(zoom - 1.0) < 1e-9)
                return;
            ApplyUiZoomWindowSize(zoom);
        }

        private void ApplyMacStartupWindowSizing()
        {
            MainWindowStartupSizing.ApplyMacStartupSizingIfNeeded(this);

            if (!OperatingSystem.IsMacOS())
                return;

            double sizeRatio = Width / MainWindowStartupSizing.DesignWidth;
            if (sizeRatio <= 0)
                return;

            MainWindowStartupSizing.ScaleOverlayPanel(settingsMenuPanel, 1728, 972, 1400, 800, sizeRatio);
            MainWindowStartupSizing.ScaleOverlayPanel(tuningMenuPanel, 1000, 650, 900, 650, sizeRatio);
        }
        
        private void InitializeGame()
        {
            initializationHandler = new GameInitializationHandler(GameCanvas, this);
            initializationHandler.InitializeGame(UpdateStatus);
            CombatTuningNavigation.OpenSettingsAndNavigateToProgressionCurve = () => OpenCombatTuningProgressionCurveSettings();
        }

        private void OpenCombatTuningProgressionCurveSettings()
        {
            ShowSettingsPanel();
            Dispatcher.UIThread.Post(() => EnsureSettingsPanel()?.OpenCombatTuningProgressionCurve(), DispatcherPriority.Loaded);
        }

        private async void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Handled)
                return;

            TrySyncAltTooltipDetail(e.KeyModifiers);

            if (IsHelpOverlayVisible)
            {
                if (e.Key == Key.H || e.Key == Key.Escape)
                {
                    e.Handled = true;
                    HideHelpOverlay();
                }
                else
                {
                    e.Handled = true;
                }
                return;
            }

            if (e.Key == Key.H)
            {
                e.Handled = true;
                ToggleHelp();
                return;
            }

            if (TryHandleCombatSpeedKey(e.Key))
            {
                e.Handled = true;
                return;
            }

            if (TryHandleNarrativeCombatLogKey(e.Key))
            {
                e.Handled = true;
                return;
            }

            if (TryHandleNarrativeVideoFeedKey(e.Key))
            {
                e.Handled = true;
                return;
            }

            if (TryHandleDistortionEffectsKey(e.Key))
            {
                e.Handled = true;
                return;
            }

            if (TryHandleGameFontKey(e.Key))
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
                        GameCoordinator.GetBarbarianStarterWeaponChoice1Based(),
                        alsoOpenSettingsWindow: true).ConfigureAwait(true);
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

            var currentState = initializationHandler?.Game?.CurrentState;
            // Skill Tree / Action Lab own PageUp/PageDown for scrolling / stepping.
            if (currentState is GameState.ActionInteractionLab or GameState.SkillTree)
                return false;

            int speed = key == Key.PageUp
                ? DeveloperModeState.IncreaseCombatSpeed()
                : DeveloperModeState.DecreaseCombatSpeed();

            if (initializationHandler?.CanvasUIManager is CanvasUICoordinator canvasUI)
                canvasUI.RefreshCenterPanelModeTint();

            ShowCombatSpeedNotification($"Combat speed: {speed}x");
            return true;
        }

        private bool TryHandleNarrativeCombatLogKey(Key key)
        {
            if (key != Key.F7)
                return false;

            bool wasNarrative = DeveloperModeState.IsNarrativeCombatLog;
            bool enabled = DeveloperModeState.ToggleNarrativeCombatLog();
            var state = initializationHandler?.Game?.CurrentState;
            CombatSequenceHudState.SyncReservation(state);
            if (initializationHandler?.CanvasUIManager is CanvasUICoordinator canvasUI)
            {
                // Convert already-buffered swings to the other log form (uses dual-view bindings).
                canvasUI.SwapCombatLogDualView(currentlyShowingNarrative: wasNarrative);
                canvasUI.RefreshCenterPanelModeTint();
            }

            ShowCombatSpeedNotification(
                enabled ? "Narrative log: ON" : "Narrative log: OFF",
                forceAutoDismiss: true);
            NarrativeVideoOverlay?.NotifyContextChanged();
            return true;
        }

        /// <summary>Applies narrative-video overlay knobs live from Settings (no file reload required).</summary>
        public void ApplyNarrativeVideoOverlayConfig(NarrativeVideoOverlayConfig? config) =>
            NarrativeVideoOverlay?.ApplyConfig(config);

        /// <summary>Re-evaluates narrative-video playback/visibility after game-state or mode changes.</summary>
        public void NotifyNarrativeVideoContextChanged() =>
            NarrativeVideoOverlay?.NotifyContextChanged();

        /// <summary>Reloads narrative-video overlay settings from <c>UIConfiguration.json</c>.</summary>
        public void ReloadNarrativeVideoOverlayConfig() =>
            NarrativeVideoOverlay?.ReloadConfigAndMaybeStart();

        private bool TryHandleNarrativeVideoFeedKey(Key key)
        {
            if (key != Key.F5)
                return false;

            bool enabled = DeveloperModeState.ToggleNarrativeVideoFeed();
            NarrativeVideoOverlay?.NotifyContextChanged();
            ShowCombatSpeedNotification(
                enabled ? "Video feed: ON" : "Video feed: OFF",
                forceAutoDismiss: true);
            return true;
        }

        private bool TryHandleDistortionEffectsKey(Key key)
        {
            if (key != Key.F6)
                return false;

            bool enabled = DeveloperModeState.ToggleDistortionEffects();
            GameCanvas.WindSway.Reset();
            GameCanvas.ClickBurst.Reset();
            GameCanvas.Refresh();

            ShowCombatSpeedNotification(
                enabled ? "Distortion: ON" : "Distortion: OFF",
                forceAutoDismiss: true);
            return true;
        }

        private bool TryHandleGameFontKey(Key key)
        {
            if (key != Key.F3)
                return false;

            var preset = GameFonts.Cycle();
            PersistGameFontPreferences();
            ApplyActiveGameFontToChrome();
            // Each font keeps its own zoom; resize so the canvas still fills the client area.
            ApplyUiZoomWindowSize(GameFonts.ActiveZoom);
            GameCanvas.ApplyActiveFont();
            if (initializationHandler?.CanvasUIManager is CanvasUICoordinator canvasUI)
                canvasUI.ForceFullLayoutRender();

            int percent = (int)Math.Round(GameFonts.ActiveZoom * 100);
            ShowCombatSpeedNotification($"Font: {preset.DisplayName} ({percent}%)", forceAutoDismiss: true);
            return true;
        }

        private bool TryHandleUiZoomKey(Key key, KeyModifiers modifiers)
        {
            if (!KeyInputConverter.IsUiZoomChord(key, modifiers))
                return false;

            int direction = KeyInputConverter.GetUiZoomDirection(key);
            double zoom = GameFonts.AdjustActiveZoom(direction);
            PersistGameFontPreferences();
            ApplyUiZoomWindowSize(zoom);
            GameCanvas.ApplyActiveFont();
            if (initializationHandler?.CanvasUIManager is CanvasUICoordinator canvasUI)
                canvasUI.ForceFullLayoutRender();

            int percent = (int)Math.Round(zoom * 100);
            ShowCombatSpeedNotification(
                $"UI size: {percent}% ({GameFonts.ActiveInfo.DisplayName})",
                forceAutoDismiss: true);
            return true;
        }

        private static void PersistGameFontPreferences()
        {
            try
            {
                GameFonts.SaveToStore();
            }
            catch (Exception ex)
            {
                DebugLogger.Log("MainWindow", $"Could not save UI font preferences: {ex.Message}");
            }
        }

        /// <summary>
        /// Keeps MainWindow overlay TextBlocks / TextBox on the same face as the ASCII canvas.
        /// </summary>
        private void ApplyActiveGameFontToChrome()
        {
            var family = GameFonts.ActiveFamily;
            var weight = GameFonts.ActiveWeight;

            CombatSpeedNotificationText.FontFamily = family;
            CombatSpeedNotificationText.FontWeight = weight;

            HelpTitleText.FontFamily = family;
            HelpTitleText.FontWeight = weight;

            HelpFooterText.FontFamily = family;

            HelpHotkeyListText.FontFamily = family;

            HiddenTextBox.FontFamily = family;
            HiddenTextBox.FontWeight = weight;
        }

        private void ShowCombatSpeedNotification(string message, bool forceAutoDismiss = false)
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
            // Narrative-log toast always auto-dismisses.
            if (forceAutoDismiss || DeveloperModeState.CombatSpeedMultiplier <= 1)
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
            TrySyncAltTooltipDetail(e.KeyModifiers);
        }

        /// <summary>
        /// Tracks Alt for expanded item/action hover tooltips and refreshes when already hovering.
        /// </summary>
        private void TrySyncAltTooltipDetail(KeyModifiers modifiers)
        {
            if (!HoverTooltipDetailState.SetFromModifiers(modifiers))
                return;
            initializationHandler?.MouseHandler?.RefreshTooltipDetailModeIfHovered();
        }

        private async void OnGlobalKeyDownTunnel(object? sender, KeyEventArgs e)
        {
            if (TryHandleUiZoomKey(e.Key, e.KeyModifiers))
            {
                e.Handled = true;
                return;
            }

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
            await ClipboardHelper.CopyDisplayBufferToClipboard(canvasForCopy, this);
        }

        private bool IsHelpOverlayVisible => HelpOverlay?.IsVisible == true;

        private void ToggleHelp()
        {
            if (IsHelpOverlayVisible)
                HideHelpOverlay();
            else
                ShowHelpOverlay();
        }

        private void ShowHelpOverlay()
        {
            if (HelpHotkeyListText != null)
                HelpHotkeyListText.Text = HotkeyHelpCatalog.FormatHelpBody();
            if (HelpOverlay != null)
                HelpOverlay.IsVisible = true;
        }

        private void HideHelpOverlay()
        {
            if (HelpOverlay != null)
                HelpOverlay.IsVisible = false;
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
            await ClipboardHelper.CopyDisplayBufferToClipboard(canvasUI, this);
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
                await ClipboardHelper.CopyDisplayBufferToClipboard(canvasUI, this);
        }

        /// <summary>
        /// Shows the tuning menu panel with the specified variable editor
        /// </summary>
        public void ShowTuningMenuPanel(RPGGame.Editors.VariableEditor variableEditor)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var panel = EnsureTuningMenuPanel();
                if (panel != null && TuningPanelOverlay != null)
                {
                    panel.Initialize(variableEditor);
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
                var panel = EnsureSettingsPanel();
                if (SettingsPanelOverlay != null && panel != null)
                {
                    // Only reload from file when actually opening (overlay was hidden). Avoids overwriting in-memory edits if ShowSettingsPanel runs again while already visible.
                    if (!SettingsPanelOverlay.IsVisible)
                    {
                        GameSettings.ReloadFromFile();
                        panel.RefreshSettingsFromFile();
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
                    panel.SetBackCallback(() =>
                    {
                        HideSettingsPanel();
                        if (initializationHandler?.Game != null)
                        {
                            // Fire and forget - HandleEscapeKey will handle state transitions
                            _ = initializationHandler.Game.HandleEscapeKey();
                        }
                    });
                    
                    panel.SetStatusCallback(UpdateStatus);
                    
                    // Initialize handlers for testing and developer tools
                    // Always call InitializeHandlers, even if some values might be null
                    // This ensures the panel has references to what's available
                    CanvasUICoordinator? canvasUIForHandlers = initializationHandler?.CanvasUIManager as CanvasUICoordinator;
                    panel.InitializeHandlers(
                        initializationHandler?.Game?.DeveloperMenuHandler,
                        initializationHandler?.Game,
                        canvasUIForHandlers,
                        initializationHandler?.Game?.StateManager);
                    
                    SettingsPanelOverlay.IsVisible = true;
                }
            });
        }

        private SettingsPanel? EnsureSettingsPanel()
        {
            if (settingsMenuPanel != null)
                return settingsMenuPanel;
            if (SettingsPanelOverlay == null)
                return null;

            settingsMenuPanel = new SettingsPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 1728,
                Height = 972,
                MinWidth = 1400,
                MinHeight = 800
            };
            SettingsPanelOverlay.Child = settingsMenuPanel;

            if (OperatingSystem.IsMacOS() && Width > 0)
            {
                double sizeRatio = Width / MainWindowStartupSizing.DesignWidth;
                MainWindowStartupSizing.ScaleOverlayPanel(settingsMenuPanel, 1728, 972, 1400, 800, sizeRatio);
            }

            return settingsMenuPanel;
        }

        private TuningMenuPanel? EnsureTuningMenuPanel()
        {
            if (tuningMenuPanel != null)
                return tuningMenuPanel;
            if (TuningPanelOverlay == null)
                return null;

            tuningMenuPanel = new TuningMenuPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 1000,
                Height = 650,
                MinWidth = 900,
                MinHeight = 650
            };
            TuningPanelOverlay.Child = tuningMenuPanel;

            if (OperatingSystem.IsMacOS() && Width > 0)
            {
                double sizeRatio = Width / MainWindowStartupSizing.DesignWidth;
                MainWindowStartupSizing.ScaleOverlayPanel(tuningMenuPanel, 1000, 650, 900, 650, sizeRatio);
            }

            return tuningMenuPanel;
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
