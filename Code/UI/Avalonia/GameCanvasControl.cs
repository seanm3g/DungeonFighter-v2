using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.Avalonia.Canvas;
using RPGGame.UI.Avalonia.Effects;
using RPGGame.UI.Avalonia.Feedback;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.UI.Avalonia
{
    /// <summary>
    /// Canvas control for rendering game UI elements.
    /// Facade coordinator that delegates to specialized canvas components.
    /// Refactored to separate control lifecycle from rendering logic.
    /// </summary>
    public class GameCanvasControl : Control
    {
        // Specialized canvas components using composition pattern
        private readonly CanvasElementManager elementManager;
        private readonly CanvasCoordinateConverter coordinateConverter;
        private readonly CanvasPrimitivesRenderer renderer;
        private readonly HealthTracker healthTracker;
        private readonly CanvasElementBuilder elementBuilder;
        private readonly WindSwayField windSwayField = new();
        private readonly TextClickBurstField clickBurstField = new();
        private readonly TextEmphasisPulseField emphasisPulseField = new();
        private readonly TextEmphasisExplosionPlayer emphasisExplosionPlayer = new();
        private DispatcherTimer? damageDeltaAnimationTimer;
        private DispatcherTimer? windSwaySettleTimer;
        private DispatcherTimer? menuSceneTimer;
        private readonly RPGGame.UI.TitleScreen.MenuSceneBackdrop menuScene = new();
        private readonly LocationCrossfade locationFade = new();
        /// <summary>TickCount64 of last pointer-driven Refresh; used to throttle highlight/wind moves.</summary>
        private long _lastPointerRefreshTickMs = -1;
        /// <summary>True when a throttled pointer move still needs one catch-up paint.</summary>
        private bool _pointerRefreshCatchupPending;
        
        // Base grid dimensions (original design size)
        private const int BASE_GRID_WIDTH = CanvasGridSizer.DesignGridWidth;
        private const int BASE_GRID_HEIGHT = CanvasGridSizer.DesignGridHeight;
        
        // Grid properties (now calculated dynamically)
        private int _gridWidth = BASE_GRID_WIDTH;
        private int _gridHeight = BASE_GRID_HEIGHT;
        private int _lastLaidOutGridWidth = -1;
        private int _lastLaidOutGridHeight = -1;

        /// <summary>
        /// Raised when the character-grid width or height changes after measure/arrange (window resize).
        /// Subscribers should force a full layout re-render so panels pick up new column counts.
        /// </summary>
        public event System.Action? GridDimensionsChanged;

        /// <summary>
        /// When true, this control does not update global <see cref="LayoutConstants"/> (compact auxiliary windows, e.g. Action Lab tools).
        /// </summary>
        public bool IsAuxiliaryLayoutCanvas { get; }

        /// <summary>Logical width when <see cref="IsAuxiliaryLayoutCanvas"/> is true.</summary>
        public int AuxiliaryGridWidth { get; }

        /// <summary>Logical height when <see cref="IsAuxiliaryLayoutCanvas"/> is true.</summary>
        public int AuxiliaryGridHeight { get; }
        
        public int GridWidth 
        { 
            get => _gridWidth;
            private set
            {
                if (_gridWidth != value)
                {
                    _gridWidth = value;
                    UpdateLayoutConstants();
                }
            }
        }
        
        public int GridHeight 
        { 
            get => _gridHeight;
            private set
            {
                if (_gridHeight != value)
                {
                    _gridHeight = value;
                    UpdateLayoutConstants();
                }
            }
        }
        
        public int ViewportWidth => GridWidth;
        public int ViewportHeight => GridHeight;
        
        // Center point (half of GridWidth)
        public int CenterX => GridWidth / 2;

        /// <summary>
        /// Solid fill drawn under all canvas elements each frame (default black).
        /// Title screen sets a dark complementary tint during idle; reset to black afterward.
        /// </summary>
        public Color ClearBackgroundColor { get; set; } = Colors.Black;

        /// <summary>Main game canvas (XAML and default construction).</summary>
        public GameCanvasControl()
            : this(false, 30, 54)
        {
        }

        /// <param name="isAuxiliaryLayoutCanvas">Compact canvas that must not update global <see cref="LayoutConstants"/>.</param>
        public GameCanvasControl(bool isAuxiliaryLayoutCanvas, int auxiliaryGridWidth = 30, int auxiliaryGridHeight = 54)
        {
            IsAuxiliaryLayoutCanvas = isAuxiliaryLayoutCanvas;
            AuxiliaryGridWidth = Math.Max(8, auxiliaryGridWidth);
            AuxiliaryGridHeight = Math.Max(8, auxiliaryGridHeight);
            Focusable = true;
            // Control has no Background in Avalonia; hit-testing is handled by a transparent Border wrapper in MainWindow.axaml.
            this.coordinateConverter = new CanvasCoordinateConverter();
            this.elementManager = new CanvasElementManager();
            this.renderer = new CanvasPrimitivesRenderer(coordinateConverter);
            this.healthTracker = new HealthTracker();
            this.elementBuilder = new CanvasElementBuilder(elementManager, healthTracker, CenterX);
            ReloadWindSwayConfig();
            renderer.SetWindSwayField(windSwayField);
            renderer.SetClickBurstField(clickBurstField);
            renderer.SetEmphasisPulseField(emphasisPulseField);
            renderer.SetEmphasisExplosionPlayer(emphasisExplosionPlayer);
            InitializeDamageDeltaTimer();
            InitializeWindSwaySettleTimer();
            if (!IsAuxiliaryLayoutCanvas)
                UpdateLayoutConstants();
        }

        /// <summary>Mouse-driven wind field used for side-panel per-glyph sway.</summary>
        public WindSwayField WindSway => windSwayField;

        /// <summary>
        /// Clears residual wake and starts a short distortion amp ramp after a new screen/menu opens
        /// so text under a still cursor does not jitter. See <see cref="WindSwayField.BeginOpenDamper"/>.
        /// State-only (caller refreshes / paints).
        /// </summary>
        public void NotifyScreenOpened()
        {
            windSwayField.BeginOpenDamper();
        }

        /// <summary>Click-charge explode/reform field for any non-overlay text.</summary>
        public TextClickBurstField ClickBurst => clickBurstField;

        /// <summary>F7 DistortionPulse neighbor-push field.</summary>
        public TextEmphasisPulseField EmphasisPulse => emphasisPulseField;

        /// <summary>F7 DistortionPulse explosion GIF under the action word.</summary>
        public TextEmphasisExplosionPlayer EmphasisExplosion => emphasisExplosionPlayer;

        /// <summary>
        /// Supplies clickable hit regions so interactive text can brighten when the mouse wake is near.
        /// Set by <see cref="Handlers.MouseInteractionHandler"/> from the live interaction manager.
        /// </summary>
        public Func<IReadOnlyList<ClickableElement>?>? InteractiveHitRegionsProvider { get; set; }

        /// <summary>
        /// Reloads wind-sway settings from <see cref="UIConfiguration"/> (call after settings save).
        /// </summary>
        public void ReloadWindSwayConfig()
        {
            try
            {
                var cfg = UIConfiguration.LoadFromFile().DungeonSelectionAnimation?.WindSway
                    ?? new WindSwayConfig();
                windSwayField.ApplyConfig(cfg);
                clickBurstField.ApplyConfig(cfg.ClickBurst ?? new TextClickBurstConfig());
                var pulseCfg = cfg.EmphasisPulse ?? new TextEmphasisPulseConfig();
                emphasisPulseField.ApplyConfig(pulseCfg);
                emphasisExplosionPlayer.ApplyConfig(pulseCfg);
                if (windSwaySettleTimer != null)
                {
                    int ms = WindSwayField.EffectiveSettleIntervalMs(cfg.SettleIntervalMs);
                    windSwaySettleTimer.Interval = TimeSpan.FromMilliseconds(ms);
                }
            }
            catch
            {
                windSwayField.ApplyConfig(new WindSwayConfig());
                clickBurstField.ApplyConfig(new TextClickBurstConfig());
                var pulseCfg = new TextEmphasisPulseConfig();
                emphasisPulseField.ApplyConfig(pulseCfg);
                emphasisExplosionPlayer.ApplyConfig(pulseCfg);
            }
        }

        /// <summary>
        /// Locates <paramref name="word"/> in center-panel text and fires a DistortionPulse.
        /// Returns false when the word is not found or the effect is disabled.
        /// </summary>
        public bool TryTriggerEmphasisPulseForWord(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return false;

            double charWidth = coordinateConverter.GetCharWidth();
            double charHeight = coordinateConverter.GetCharHeight();
            if (charWidth <= 0 || charHeight <= 0)
                return false;

            if (!emphasisPulseField.Config.Enabled || !DeveloperModeState.AreDistortionEffectsEnabled)
                return false;

            if (!ActionTextEmphasisWordLocator.TryLocate(
                    elementManager.TextElements,
                    word,
                    charWidth,
                    charHeight,
                    out double originX,
                    out double originY,
                    out double excludeMinX,
                    out double excludeMaxX,
                    out double excludeMinY,
                    out double excludeMaxY))
            {
                return false;
            }

            emphasisPulseField.Trigger(
                originX,
                originY,
                excludeMinX,
                excludeMaxX,
                excludeMinY,
                excludeMaxY,
                charWidth,
                charHeight);
            emphasisExplosionPlayer.Trigger(originX, originY, charWidth, charHeight);
            EnsureWindSwaySettleTimer();
            Refresh();
            return true;
        }

        /// <summary>
        /// Feeds pointer motion into the wind field and keeps the canvas settling until wind dies out.
        /// Always tracks mouse for the wake-radius debug overlay and interactive proximity highlight.
        /// Full-canvas Refresh is throttled to <see cref="WindSwayConfig.SettleIntervalMs"/> so highlight
        /// mouse moves do not paint every OS event; settle timer + click still refresh immediately.
        /// </summary>
        public void NotifyPointerWind(Point position)
        {
            double charWidth = coordinateConverter.GetCharWidth();
            double charHeight = coordinateConverter.GetCharHeight();
            if (charWidth <= 0 || charHeight <= 0)
                return;

            // Wind/highlight sample in content pixel space (matches translated draw origin).
            Point content = ScreenToContentPixels(position);

            // Always track for debug circle / proximity highlight, even when sway is disabled / F6 off.
            windSwayField.TrackMousePosition(content.X, content.Y);

            // Hovering a dimmed combat-board half lights it up; repaint as the pointer crosses between areas.
            if (!IsAuxiliaryLayoutCanvas && CombatTurnSpotlight.TrackPointer(content.X / charWidth, content.Y / charHeight))
                Refresh();

            bool highlight = windSwayField.Config.InteractiveHighlightEnabled;
            bool windOn = windSwayField.Config.Enabled && DeveloperModeState.AreDistortionEffectsEnabled;

            if (windOn)
            {
                windSwayField.PushFromMouseDelta(content.X, content.Y, charWidth, charHeight);
                EnsureWindSwaySettleTimer();
            }

            if (windOn || highlight || windSwayField.ShowWakeRadiusDebug)
                RefreshPointerThrottled();
        }

        /// <summary>
        /// Throttles pointer-move Refresh to the settle interval while still tracking field state every move.
        /// Skipped paints schedule a catch-up via the settle timer so highlight does not stick stale.
        /// </summary>
        private void RefreshPointerThrottled()
        {
            int settleMs = WindSwayField.EffectiveSettleIntervalMs(windSwayField.Config.SettleIntervalMs);
            long now = System.Environment.TickCount64;
            if (_lastPointerRefreshTickMs >= 0 && (now - _lastPointerRefreshTickMs) < settleMs)
            {
                _pointerRefreshCatchupPending = true;
                EnsureWindSwaySettleTimer();
                return;
            }

            _lastPointerRefreshTickMs = now;
            _pointerRefreshCatchupPending = false;
            Refresh();
        }

        /// <summary>
        /// Feeds a left-click into the click-burst field (visual only; does not consume the click).
        /// </summary>
        public void NotifyPointerClick(Point position)
        {
            double charWidth = coordinateConverter.GetCharWidth();
            double charHeight = coordinateConverter.GetCharHeight();
            if (charWidth <= 0 || charHeight <= 0)
                return;

            if (!clickBurstField.Config.Enabled || !DeveloperModeState.AreDistortionEffectsEnabled)
                return;

            Point content = ScreenToContentPixels(position);
            clickBurstField.NotifyClick(content.X, content.Y, charWidth, charHeight);
            EnsureWindSwaySettleTimer();
            Refresh();
        }

        /// <summary>
        /// Keeps click-burst explosions attached to scrolled text in a viewport.
        /// <paramref name="scrollRows"/> is content rows skipped from the top (center log / left panel).
        /// </summary>
        public void SyncClickBurstViewportScroll(
            string viewportId,
            int scrollRows,
            int contentY,
            int contentHeight)
        {
            double charHeight = coordinateConverter.GetCharHeight();
            if (charHeight <= 0 || contentHeight <= 0)
                return;

            double minY = contentY * charHeight;
            double maxY = (contentY + contentHeight) * charHeight;
            clickBurstField.SyncViewportScroll(viewportId, scrollRows, minY, maxY, charHeight);
        }

        /// <summary>Viewport id for the framed center combat-log / display buffer.</summary>
        public const string ClickBurstCenterViewportId = "center-log";

        /// <summary>Viewport id for the left character panel body.</summary>
        public const string ClickBurstLeftViewportId = "left-panel";
        
        /// <summary>
        /// Initializes the timer for damage delta fade animations
        /// </summary>
        private void InitializeDamageDeltaTimer()
        {
            damageDeltaAnimationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60fps for smooth animation
            };
            damageDeltaAnimationTimer.Tick += (s, e) =>
            {
                // Check if health tracker has any active damage deltas
                if (healthTracker.HasActiveDamageDeltas())
                {
                    Refresh();
                }
                else
                {
                    // Stop timer when no active deltas
                    damageDeltaAnimationTimer.Stop();
                }
            };
        }

        private void InitializeWindSwaySettleTimer()
        {
            int ms = WindSwayField.EffectiveSettleIntervalMs(windSwayField.Config.SettleIntervalMs);
            windSwaySettleTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(ms)
            };
            windSwaySettleTimer.Tick += (_, _) =>
            {
                bool catchup = _pointerRefreshCatchupPending;
                _pointerRefreshCatchupPending = false;
                if (windSwayField.IsActive
                    || clickBurstField.IsActive
                    || emphasisPulseField.IsActive
                    || emphasisExplosionPlayer.IsActive
                    || catchup)
                {
                    _lastPointerRefreshTickMs = System.Environment.TickCount64;
                    Refresh();
                }
                else
                {
                    // One final paint at true rest so the last active frame cannot leave residual CA/offset.
                    windSwaySettleTimer.Stop();
                    Refresh();
                }
            };
        }

        private void EnsureWindSwaySettleTimer()
        {
            if (windSwaySettleTimer != null
                && !windSwaySettleTimer.IsEnabled
                && (windSwayField.IsActive
                    || clickBurstField.IsActive
                    || emphasisPulseField.IsActive
                    || emphasisExplosionPlayer.IsActive
                    || _pointerRefreshCatchupPending))
            {
                windSwaySettleTimer.Start();
            }
        }
        
        /// <summary>
        /// Gets the health tracker for tracking previous health values
        /// </summary>
        public HealthTracker HealthTracker => healthTracker;
        
        /// <summary>
        /// Updates LayoutConstants with current grid dimensions and effective visible width
        /// </summary>
        private void UpdateLayoutConstants()
        {
            if (IsAuxiliaryLayoutCanvas)
                return;
            LayoutConstants.UpdateUiZoom(GameFonts.ActiveZoom);
            LayoutConstants.UpdateGridDimensions(GridWidth, GridHeight);
            UpdateEffectiveVisibleWidth();
        }
        
        /// <summary>
        /// Updates effective visible width based on actual canvas bounds
        /// </summary>
        private void UpdateEffectiveVisibleWidth()
        {
            if (IsAuxiliaryLayoutCanvas)
                return;
            if (coordinateConverter == null)
                return;
            coordinateConverter.EnsureCharWidthMeasured();
            double charWidth = coordinateConverter.GetCharWidth();
            if (charWidth <= 0)
                return;
            // Use the laid-out pixel width so center/right panels track horizontal resize.
            // GridWidth already matches floor(width/charWidth) from CalculateScaleAndGrid.
            double widthForLayout = Bounds.Width > 0
                ? Bounds.Width
                : GridWidth * charWidth;
            LayoutConstants.UpdateEffectiveVisibleWidth(widthForLayout, charWidth);
        }

        /// <summary>
        /// Calculates scale factor and grid dimensions based on available space.
        /// Main canvas: font scale fills available height; column count shrinks with width.
        /// </summary>
        private void CalculateScaleAndGrid(double availableWidth, double availableHeight)
        {
            coordinateConverter.SetScaleFactor(1.0);
            coordinateConverter.EnsureCharWidthMeasured();
            double baseCharWidth = coordinateConverter.GetCharWidth();
            double baseCharHeight = coordinateConverter.GetCharHeight();

            double scaleFactor;
            int targetW;
            int targetH;
            if (IsAuxiliaryLayoutCanvas)
            {
                (scaleFactor, targetW, targetH) = CanvasGridSizer.CalculateFixedGrid(
                    availableWidth,
                    availableHeight,
                    baseCharWidth,
                    baseCharHeight,
                    AuxiliaryGridWidth,
                    AuxiliaryGridHeight);
            }
            else if (availableWidth > 0 && availableHeight > 0
                     && availableWidth != double.PositiveInfinity
                     && availableHeight != double.PositiveInfinity)
            {
                // UI zoom scales center-column glyphs only; grid stays window-fit 100%.
                (scaleFactor, targetW, targetH) = CanvasGridSizer.Calculate(
                    availableWidth,
                    availableHeight,
                    baseCharWidth,
                    baseCharHeight);
            }
            else
            {
                scaleFactor = 1.0;
                targetW = BASE_GRID_WIDTH;
                targetH = BASE_GRID_HEIGHT;
            }

            coordinateConverter.SetScaleFactor(scaleFactor);
            // Font size may snap to whole pixels; recompute column count from the snapped cell width.
            coordinateConverter.EnsureCharWidthMeasured();
            double snappedCharWidth = coordinateConverter.GetCharWidth();
            if (!IsAuxiliaryLayoutCanvas
                && availableWidth > 0
                && !double.IsInfinity(availableWidth)
                && snappedCharWidth > 0)
            {
                targetW = Math.Max(
                    CanvasGridSizer.AbsoluteMinGridWidth + CanvasGridSizer.OuterPaddingRight,
                    (int)Math.Floor(availableWidth / snappedCharWidth + 1e-6));
            }

            GridWidth = targetW;
            GridHeight = targetH;

            if (!IsAuxiliaryLayoutCanvas)
            {
                // Keep LayoutConstants in sync during measure/arrange (Bounds may still be stale).
                LayoutConstants.UpdateUiZoom(GameFonts.ActiveZoom);
                LayoutConstants.UpdateGridDimensions(GridWidth, GridHeight);
                double charWidth = coordinateConverter.GetCharWidth();
                if (charWidth > 0)
                    LayoutConstants.UpdateEffectiveVisibleWidth(
                        CanvasGridSizer.LayoutColumnCount(GridWidth) * charWidth,
                        charWidth);

                if (_lastLaidOutGridWidth >= 0
                    && (_lastLaidOutGridWidth != GridWidth || _lastLaidOutGridHeight != GridHeight))
                {
                    GridDimensionsChanged?.Invoke();
                }

                _lastLaidOutGridWidth = GridWidth;
                _lastLaidOutGridHeight = GridHeight;
            }
        }

        /// <summary>
        /// Pixel size of the full character grid at the current font scale (may exceed the viewport when zoomed in).
        /// </summary>
        public Size GetContentPixelSize()
        {
            coordinateConverter.EnsureCharWidthMeasured();
            double width = GridWidth * coordinateConverter.GetCharWidth();
            double height = IsAuxiliaryLayoutCanvas
                ? GridHeight * coordinateConverter.GetCharHeight()
                : CanvasGridSizer.CanvasRowCount(GridHeight) * coordinateConverter.GetCharHeight();
            return new Size(width, height);
        }

        /// <summary>
        /// Top-left of the character grid inside the control. Centers zoomed content so UI stays oriented on screen.
        /// </summary>
        public Point GetContentOriginOffset()
        {
            Size content = GetContentPixelSize();
            var (ox, oy) = CanvasGridSizer.CalculateContentOrigin(
                Bounds.Width, Bounds.Height, content.Width, content.Height);
            return new Point(ox, oy);
        }

        /// <summary>Maps a pointer position in control space to character-grid content pixels.</summary>
        public Point ScreenToContentPixels(Point screenPosition)
        {
            Point origin = GetContentOriginOffset();
            return new Point(screenPosition.X - origin.X, screenPosition.Y - origin.Y);
        }

        /// <summary>Maps a pointer position in control space to character-grid indices.</summary>
        public (int X, int Y) ScreenToGrid(Point screenPosition)
        {
            double charWidth = GetCharWidth();
            double charHeight = GetCharHeight();
            if (charWidth <= 0 || charHeight <= 0)
                return (0, 0);

            Point content = ScreenToContentPixels(screenPosition);
            int gridX = (int)Math.Floor(content.X / charWidth);
            int gridY = (int)Math.Floor(content.Y / charHeight);
            return (gridX, gridY);
        }

        /// <summary>
        /// Measures the control size and calculates scale factor based on available space
        /// </summary>
        protected override Size MeasureOverride(Size availableSize)
        {
            CalculateScaleAndGrid(availableSize.Width, availableSize.Height);

            // Prefer the allocated viewport so Ctrl+/- zoom cannot fight the fixed window size.
            if (availableSize.Width > 0
                && availableSize.Height > 0
                && !double.IsInfinity(availableSize.Width)
                && !double.IsInfinity(availableSize.Height))
            {
                return availableSize;
            }

            return GetContentPixelSize();
        }
        
        /// <summary>
        /// Called when the control is arranged - recalculates scale if size changed
        /// </summary>
        protected override Size ArrangeOverride(Size finalSize)
        {
            CalculateScaleAndGrid(finalSize.Width, finalSize.Height);
            // Fill the stretch slot; content is centered inside via GetContentOriginOffset.
            return finalSize;
        }

        /// <summary>
        /// Exposes character width for coordinate conversion
        /// </summary>
        public double GetCharWidth()
        {
            return coordinateConverter.GetCharWidth();
        }

        /// <summary>
        /// Exposes character height for coordinate conversion
        /// </summary>
        public double GetCharHeight()
        {
            return coordinateConverter.GetCharHeight();
        }

        public override void Render(DrawingContext context)
        {
            // Update effective visible width before rendering (bounds are now available)
            if (!IsAuxiliaryLayoutCanvas)
                UpdateEffectiveVisibleWidth();

            Color clear = ClearBackgroundColor.A == 0 ? Colors.Black : ClearBackgroundColor;
            context.FillRectangle(new SolidColorBrush(clear), new Rect(0, 0, Bounds.Width, Bounds.Height));

            Size content = GetContentPixelSize();
            Point origin = GetContentOriginOffset();

            // Don't call base.Render() to prevent any default Control rendering
            renderer.SetInteractiveHitRegions(InteractiveHitRegionsProvider?.Invoke());
            var textElements = elementManager.TextElements.ToList();
            var location = new LocationCrossfade.Location(
                MenuSceneProvider?.Invoke() ?? RPGGame.UI.TitleScreen.MenuScene.None, BackdropProvider?.Invoke());
            var (incomingOpacity, outgoingOpacity) = locationFade.Update(location);
            var outgoing = locationFade.Outgoing;

            var sceneUnderlay = menuScene.BuildFrame(GridWidth, GridHeight, textElements, location.Scene);
            var outgoingSceneUnderlay = outgoing.Scene != location.Scene
                ? menuScene.BuildFrame(GridWidth, GridHeight, textElements, outgoing.Scene)
                : null;
            var backdrop = location.Backdrop;
            if (backdrop != null)
                backdrop.Opacity = incomingOpacity;
            var outgoingBackdrop = outgoing.Backdrop != null && !ReferenceEquals(outgoing.Backdrop.Source, backdrop?.Source)
                ? outgoing.Backdrop
                : null;
            if (outgoingBackdrop != null)
                outgoingBackdrop.Opacity = outgoingOpacity;
            UpdateMenuSceneTimer(sceneUnderlay != null || locationFade.IsActive);
            using (context.PushTransform(Matrix.CreateTranslation(origin.X, origin.Y)))
            {
                renderer.Render(
                    context,
                    content.Width,
                    content.Height,
                    textElements,
                    elementManager.BoxElements.ToList(),
                    elementManager.ProgressBars.ToList(),
                    elementManager.SegmentedBars.ToList(),
                    Colors.Transparent,
                    fillBackground: false,
                    images: elementManager.Images.ToList(),
                    backdrop: backdrop,
                    sceneUnderlay: sceneUnderlay,
                    sceneUnderlayOpacity: locationFade.IsActive ? incomingOpacity : 1,
                    outgoingBackdrop: outgoingBackdrop,
                    outgoingSceneUnderlay: outgoingSceneUnderlay,
                    outgoingSceneUnderlayOpacity: outgoingOpacity);

                if (!IsAuxiliaryLayoutCanvas)
                    CombatTurnSpotlight.Paint(context, content.Width, content.Height,
                        coordinateConverter.GetCharWidth(), coordinateConverter.GetCharHeight(),
                        elementManager.ProgressBars.Where(b => b.IsHealthBar));
                DrawConnectorLines(context);
                if (!IsAuxiliaryLayoutCanvas)
                    ComboTension.Paint(context, content.Width, content.Height,
                        coordinateConverter.GetCharWidth(), coordinateConverter.GetCharHeight());
                DrawWakeRadiusDebugOverlay(context);
                if (!IsAuxiliaryLayoutCanvas)
                    CombatTurnBanner.Paint(context, content.Height,
                        coordinateConverter.GetCharWidth(), coordinateConverter.GetCharHeight(), coordinateConverter.GetFontSize());
                if (!IsAuxiliaryLayoutCanvas)
                    CombatTransitionWipe.Paint(context, content.Height, coordinateConverter.GetCharWidth());
            }
        }

        private void DrawConnectorLines(DrawingContext context)
        {
            var lines = elementManager.Lines;
            if (lines.Count == 0)
                return;
            double charWidth = coordinateConverter.GetCharWidth();
            double charHeight = coordinateConverter.GetCharHeight();
            foreach (var line in lines.ToList())
            {
                var pen = new Pen(new SolidColorBrush(line.Color), line.ThicknessPixels);
                context.DrawLine(pen,
                    new Point(line.X1 * charWidth, line.Y1 * charHeight),
                    new Point(line.X2 * charWidth, line.Y2 * charHeight));
            }
        }

        /// <summary>Adds a connector line between grid points (fractional cells allowed).</summary>
        public void AddLine(double x1, double y1, double x2, double y2, Color color, double thicknessPixels = 1.5)
        {
            elementManager.AddLine(new CanvasLine { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Color = color, ThicknessPixels = thicknessPixels });
        }

        public void ClearLinesInArea(int startX, int startY, int width, int height)
        {
            elementManager.ClearLinesInArea(startX, startY, width, height);
        }

        private void DrawWakeRadiusDebugOverlay(DrawingContext context)
        {
            if (!windSwayField.ShowWakeRadiusDebug)
                return;

            double charWidth = coordinateConverter.GetCharWidth();
            double charHeight = coordinateConverter.GetCharHeight();
            if (!windSwayField.TryGetWakeDebugEllipse(
                    charWidth, charHeight,
                    out double cx, out double cy, out double rx, out double ry, out double rotationRadians))
                return;

            var pen = new Pen(new SolidColorBrush(Color.FromArgb(200, 80, 220, 255)), 1.5);
            // Avalonia matrices are row-vector (p' = p * M): rotate about local origin, then translate to cursor.
            // Translation * Rotation would spin the oval around world (0,0) as facing changes.
            using (context.PushTransform(CreateWakeDebugTransform(cx, cy, rotationRadians)))
            {
                context.DrawEllipse(null, pen, new Point(0, 0), rx, ry);
            }

            // Crosshair at cursor for size reference.
            var cross = new Pen(new SolidColorBrush(Color.FromArgb(180, 255, 200, 80)), 1);
            context.DrawLine(cross, new Point(cx - 6, cy), new Point(cx + 6, cy));
            context.DrawLine(cross, new Point(cx, cy - 6), new Point(cx, cy + 6));
        }

        /// <summary>
        /// Local→world transform for the wake debug oval: rotate about (0,0), then place at the cursor.
        /// Avalonia uses row-vector composition (<c>p' = p * M</c>), so rotation must be left of translation.
        /// </summary>
        internal static Matrix CreateWakeDebugTransform(double centerX, double centerY, double rotationRadians)
            => Matrix.CreateRotation(rotationRadians) * Matrix.CreateTranslation(centerX, centerY);

        // Public methods for adding elements
        public void Clear()
        {
            elementManager.Clear();
        }

        /// <summary>
        /// Picture drawn behind the text each frame (the dungeon room's environment plate); null = none.
        /// Asked at paint time so every render path, reactive or imperative, shows the same backdrop.
        /// </summary>
        public Func<CanvasBackdrop?>? BackdropProvider { get; set; }

        /// <summary>
        /// Which animated inn scene the current screen shows behind it (screens outside a run).
        /// Asked at paint time; the scene keeps repainting itself until a paint finds <see cref="RPGGame.UI.TitleScreen.MenuScene.None"/>
        /// (and while a location change is still crossfading, <see cref="LocationCrossfade"/>).
        /// </summary>
        public Func<RPGGame.UI.TitleScreen.MenuScene>? MenuSceneProvider { get; set; }

        private void UpdateMenuSceneTimer(bool shown)
        {
            if (!shown)
            {
                menuSceneTimer?.Stop();
                return;
            }

            if (menuSceneTimer == null)
            {
                menuSceneTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(RPGGame.UI.TitleScreen.MenuSceneBackdrop.FrameIntervalMs)
                };
                menuSceneTimer.Tick += (_, _) => InvalidateVisual();
            }
            if (!menuSceneTimer.IsEnabled)
                menuSceneTimer.Start();
        }
        
        /// <summary>
        /// Clears text elements within a specific Y range (inclusive)
        /// Clears ALL text in the Y range across the entire canvas width
        /// This is the standard method for clearing panels/areas - always clears full width
        /// </summary>
        public void ClearTextInRange(int startY, int endY)
        {
            elementManager.ClearTextInRange(startY, endY);
        }
        
        /// <summary>
        /// Clears text elements within a specific rectangular area (inclusive)
        /// Use this only when you need to clear a specific rectangular region (e.g., center panel only)
        /// For clearing full-width panels, use ClearTextInRange() instead
        /// </summary>
        public void ClearTextInArea(int startX, int startY, int width, int height)
        {
            elementManager.ClearTextInArea(startX, startY, width, height);
        }

        /// <summary>See <see cref="CanvasElementManager.MaskNonOverlayTextInArea"/>.</summary>
        public void MaskNonOverlayTextInArea(int startX, int startY, int width, int height)
        {
            elementManager.MaskNonOverlayTextInArea(startX, startY, width, height);
        }
        
        /// <summary>
        /// Clears progress bars within a specific rectangular area (inclusive)
        /// Used to clear specific areas of the canvas without affecting other panels
        /// </summary>
        public void ClearProgressBarsInArea(int startX, int startY, int width, int height)
        {
            elementManager.ClearProgressBarsInArea(startX, startY, width, height);
        }

        /// <summary>
        /// Clears segmented bars within a specific rectangular area (inclusive).
        /// </summary>
        public void ClearSegmentedBarsInArea(int startX, int startY, int width, int height)
        {
            elementManager.ClearSegmentedBarsInArea(startX, startY, width, height);
        }

        /// <summary>
        /// Clears box elements (borders) within a specific rectangular area.
        /// Used when re-rendering a panel without full canvas clear to prevent duplicate borders.
        /// </summary>
        public void ClearBoxesInArea(int startX, int startY, int width, int height)
        {
            elementManager.ClearBoxesInArea(startX, startY, width, height);
        }

        /// <summary>See <see cref="CanvasElementManager.ClearOverlayTextInArea"/>.</summary>
        public void ClearOverlayTextInArea(int startX, int startY, int width, int height)
        {
            elementManager.ClearOverlayTextInArea(startX, startY, width, height);
        }

        /// <summary>See <see cref="CanvasElementManager.ClearOverlayBoxesInArea"/>.</summary>
        public void ClearOverlayBoxesInArea(int startX, int startY, int width, int height)
        {
            elementManager.ClearOverlayBoxesInArea(startX, startY, width, height);
        }

        /// <summary>
        /// Plain-text snapshot of drawn canvas text in a character-grid rectangle (for clipboard export).
        /// </summary>
        public string GetPlainTextSnapshotInGridRect(int startX, int startY, int width, int height, bool excludeOverlay = true)
        {
            return elementManager.BuildPlainTextSnapshotInRect(startX, startY, width, height, excludeOverlay);
        }

        /// <summary>
        /// Samples non-overlay glyphs and colors in a character-grid rectangle (combat-log / center band).
        /// </summary>
        public void SampleCombatLogCellColors(
            int startX,
            int startY,
            int width,
            int height,
            char[] glyphs,
            Color[] colors)
        {
            elementManager.SampleCellColorsInRect(startX, startY, width, height, glyphs, colors);
        }

        /// <summary>
        /// Gets the element builder for creating canvas elements with convenience methods
        /// </summary>
        public CanvasElementBuilder Builder => elementBuilder;

        /// <summary>
        /// Adds text to the canvas at the specified position
        /// Delegates to element builder for convenience
        /// </summary>
        public void AddText(int x, int y, string text, Color color)
        {
            elementBuilder.AddText(x, y, text, color);
        }

        /// <summary>
        /// Adds text shifted right by a fraction of a cell (sub-cell centering)
        /// </summary>
        public void AddTextNudged(int x, int y, string text, Color color, double nudgeCellsX)
        {
            elementBuilder.AddTextNudged(x, y, text, color, nudgeCellsX);
        }

        /// <summary>
        /// Adds text with glow effect to the canvas at the specified position
        /// </summary>
        public void AddText(int x, int y, string text, Color color, Color glowColor, double glowIntensity = 0.5, int glowRadius = 3)
        {
            elementBuilder.AddText(x, y, text, color, glowColor, glowIntensity, glowRadius);
        }
        
        /// <summary>
        /// Adds full-screen scene text with no merging/dedupe (title screen paints thousands of cells per frame).
        /// Skips center-column zoom and clipping.
        /// </summary>
        public void AddSceneText(int x, int y, string text, Color color)
        {
            elementManager.AddText(new CanvasText { X = x, Y = y, Content = text, Color = color, IgnoresCenterColumn = true });
        }

        /// <summary>
        /// Adds text to the canvas at the specified position without removing existing text
        /// This is used for rendering multi-segment colored text where segments may round to the same position
        /// </summary>
        public void AppendText(int x, int y, string text, Color color)
        {
            elementBuilder.AppendText(x, y, text, color);
        }
        
        /// <summary>
        /// Measures the actual pixel width of text using FormattedText
        /// Returns the width in character units (pixels / charWidth)
        /// </summary>
        public double MeasureTextWidth(string text)
        {
            return coordinateConverter.MeasureTextWidth(text);
        }

        /// <summary>
        /// Adds text to the canvas, optionally centered
        /// </summary>
        public void AddText(int x, int y, string text, Color color, bool centered)
        {
            elementBuilder.AddText(x, y, text, color, centered);
        }

        /// <summary>
        /// Adds a box to the canvas
        /// </summary>
        public void AddBox(int x, int y, int width, int height, Color borderColor, Color backgroundColor = default, int opaqueBackgroundBleedDevicePixels = 0, int borderThicknessPixels = 1, bool aboveBackdrop = false)
        {
            elementBuilder.AddBox(x, y, width, height, borderColor, backgroundColor, opaqueBackgroundBleedDevicePixels, borderThicknessPixels, aboveBackdrop);
        }

        /// <summary>
        /// Draws <paramref name="sourceRect"/> of <paramref name="source"/> in a grid-cell rectangle.
        /// Square and centered unless <paramref name="fillFrame"/> is set, which paints the whole cell.
        /// </summary>
        public void AddImage(int x, int y, int width, int height, IImage source, Rect sourceRect, double paddingPixels = 2, bool fillFrame = false,
            bool pixelAligned = false, Action<DrawingContext>? overlay = null, bool followsAsciiStyle = false)
        {
            elementManager.AddImage(new CanvasImage
            {
                X = x, Y = y, Width = width, Height = height,
                Source = source, SourceRect = sourceRect, PaddingPixels = paddingPixels, FillFrame = fillFrame,
                PixelAligned = pixelAligned, Overlay = overlay, FollowsAsciiStyle = followsAsciiStyle
            });
        }

        public bool TryUpdateBox(int x, int y, int width, int height, Color borderColor, Color backgroundColor)
        {
            return elementManager.TryUpdateBox(x, y, width, height, borderColor, backgroundColor);
        }

        /// <summary>See <see cref="CanvasElementBuilder.AddOverlayBox"/>.</summary>
        public void AddOverlayBox(int x, int y, int width, int height, Color borderColor, Color backgroundColor, int opaqueBackgroundBleedDevicePixels = 0)
        {
            elementBuilder.AddOverlayBox(x, y, width, height, borderColor, backgroundColor, opaqueBackgroundBleedDevicePixels);
        }

        /// <summary>See <see cref="CanvasElementBuilder.AddOverlayText"/>.</summary>
        public void AddOverlayText(int x, int y, string text, Color color)
        {
            elementBuilder.AddOverlayText(x, y, text, color);
        }

        /// <summary>Appends overlay text at a cell when the color matches the existing overlay segment.</summary>
        public void AppendOverlayText(int x, int y, string text, Color color)
        {
            elementBuilder.AppendOverlayText(x, y, text, color);
        }

        /// <summary>
        /// Adds a progress bar to the canvas
        /// </summary>
        public void AddProgressBar(int x, int y, int width, double progress, Color foregroundColor, Color backgroundColor, Color borderColor)
        {
            elementBuilder.AddProgressBar(x, y, width, progress, foregroundColor, backgroundColor, borderColor);
        }

        /// <summary>
        /// Adds a multi-segment bar (e.g. d20 threshold outcomes).
        /// </summary>
        public void AddSegmentedBar(
            int x,
            int y,
            int width,
            Layout.ThresholdDisplayFormatting.D20OutcomeSegment[] segments,
            Color borderColor,
            Color dividerColor,
            int totalFaces = 20,
            System.Func<int, Color, Color>? segmentHighlight = null,
            double heightScale = 1.0,
            double verticalOffsetScale = 0.0,
            System.Func<int?>? rollMarkerRoll = null,
            bool rollMarkerBelow = false)
        {
            elementBuilder.AddSegmentedBar(x, y, width, segments, borderColor, dividerColor, totalFaces, segmentHighlight, heightScale, verticalOffsetScale, rollMarkerRoll, rollMarkerBelow);
        }

        /// <summary>
        /// Adds a border (box with no background) to the canvas
        /// </summary>
        public void AddBorder(int x, int y, int width, int height, Color color, int borderThicknessPixels = 1)
        {
            elementBuilder.AddBorder(x, y, width, height, color, borderThicknessPixels);
        }

        /// <summary>
        /// Adds centered text to the canvas
        /// </summary>
        public void AddCenteredText(int y, string text, Color color)
        {
            elementBuilder.AddCenteredText(y, text, color);
        }

        /// <summary>
        /// Adds a title (centered text with padding) to the canvas
        /// </summary>
        public void AddTitle(int y, string title, Color color = default)
        {
            elementBuilder.AddTitle(y, title, color);
        }

        /// <summary>
        /// Adds a menu option to the canvas
        /// </summary>
        public void AddMenuOption(int x, int y, int number, string option, Color color = default, bool isHovered = false)
        {
            elementBuilder.AddMenuOption(x, y, number, option, color, isHovered);
        }

        /// <summary>
        /// Adds an item display to the canvas
        /// </summary>
        public void AddItem(int x, int y, int number, string itemName, string stats, Color nameColor = default, Color statsColor = default, bool isHovered = false)
        {
            elementBuilder.AddItem(x, y, number, itemName, stats, nameColor, statsColor, isHovered);
        }

        /// <summary>
        /// Adds a character stat display to the canvas
        /// </summary>
        public void AddCharacterStat(int x, int y, string statName, int value, int maxValue, Color nameColor = default, Color valueColor = default)
        {
            elementBuilder.AddCharacterStat(x, y, statName, value, maxValue, nameColor, valueColor);
        }

        /// <summary>
        /// Adds a health bar to the canvas with damage delta animation support
        /// </summary>
        public void AddHealthBar(
            int x,
            int y,
            int width,
            int currentHealth,
            int maxHealth,
            Color healthColor = default,
            Color backgroundColor = default,
            string? entityId = null,
            double heightScale = 1.0,
            double verticalOffsetScale = 0.0,
            System.Collections.Generic.IReadOnlyList<double>? dividerFractions = null)
        {
            elementBuilder.AddHealthBar(x, y, width, currentHealth, maxHealth, healthColor, backgroundColor, entityId, heightScale, verticalOffsetScale, dividerFractions);
            
            // Start animation timer if damage delta is active
            if (!string.IsNullOrEmpty(entityId))
            {
                var damageDeltaStartTime = healthTracker.GetDamageDeltaStartTime(entityId);
                if (damageDeltaStartTime.HasValue && damageDeltaAnimationTimer != null && !damageDeltaAnimationTimer.IsEnabled)
                {
                    damageDeltaAnimationTimer.Start();
                }
            }
        }
        
        /// <summary>
        /// Cleans up resources when control is disposed
        /// </summary>
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            damageDeltaAnimationTimer?.Stop();
            damageDeltaAnimationTimer = null;
            windSwaySettleTimer?.Stop();
            windSwaySettleTimer = null;
            base.OnDetachedFromVisualTree(e);
        }

        public void Refresh()
        {
            if (Dispatcher.UIThread.CheckAccess())
                InvalidateVisual();
            else
                Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Render);
        }

        /// <summary>
        /// Re-measures the grid after <see cref="GameFonts"/> changes (F3) so column/row counts
        /// and glyph metrics match the new typeface.
        /// </summary>
        public void ApplyActiveFont()
        {
            void Apply()
            {
                coordinateConverter.InvalidateCharMetrics();
                InvalidateMeasure();
                InvalidateArrange();
                InvalidateVisual();
            }

            if (Dispatcher.UIThread.CheckAccess())
                Apply();
            else
                Dispatcher.UIThread.Post(Apply, DispatcherPriority.Render);
        }
    }
}
