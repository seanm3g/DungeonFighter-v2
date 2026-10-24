using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Effects;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.UI.Avalonia.Canvas
{

    /// <summary>
    /// Low-level drawing of canvas primitives (boxes, progress bars, text) to a <see cref="DrawingContext"/>.
    /// Distinct from <see cref="RPGGame.UI.Avalonia.Renderers.CanvasRenderer"/>, which orchestrates full-screen game UI.
    /// </summary>
    public class CanvasPrimitivesRenderer
    {
        private readonly CanvasCoordinateConverter coordinateConverter;
        private WindSwayField? windSway;
        private TextClickBurstField? clickBurst;
        private TextEmphasisPulseField? emphasisPulse;
        private TextEmphasisExplosionPlayer? explosionPlayer;
        private HashSet<long>? interactiveCells;
        private int _interactiveCellsFingerprint = int.MinValue;
        private double interactiveHighlightBrighten = 0.55;
        private double interactiveHighlightGlow = 0.40;
        private bool interactiveHighlightEnabled;
        /// <summary>True for the current paint while <see cref="WindSwayField.BeginFrame"/> succeeded.</summary>
        private bool windFrameActive;
        /// <summary>True for the current paint while <see cref="TextClickBurstField.BeginFrame"/> succeeded.</summary>
        private bool burstFrameActive;
        /// <summary>True for the current paint while <see cref="TextEmphasisPulseField.BeginFrame"/> succeeded.</summary>
        private bool pulseFrameActive;
        private WindSwayConfig? windFrameConfig;
        private TextClickBurstConfig? burstFrameConfig;
        private TextEmphasisPulseConfig? pulseFrameConfig;

        // Per-paint FormattedText / brush reuse (cleared at Render start).
        private readonly Dictionary<(string Text, uint ColorArgb, double FontSize), FormattedText> _formattedTextCache = new();
        private readonly Dictionary<uint, SolidColorBrush> _brushCache = new();
        private double _cachedFontSize = -1;
        private double _cachedCenterFontScale = -1;
        private Typeface? _cachedTypeface;

        /// <summary>Avoid per-glyph <c>char.ToString()</c> allocations on the distortion paint path.</summary>
        private static readonly string[] AsciiGlyphStrings = CreateAsciiGlyphStrings();
        private static readonly WindSwayConfig FallbackWindConfig = new();

        public CanvasPrimitivesRenderer(CanvasCoordinateConverter coordinateConverter)
        {
            this.coordinateConverter = coordinateConverter ?? throw new ArgumentNullException(nameof(coordinateConverter));
        }

        /// <summary>Optional mouse-driven wind field for per-glyph side-panel sway.</summary>
        public void SetWindSwayField(WindSwayField? field) => windSway = field;

        /// <summary>Optional click-charge explode/reform field (any non-overlay text).</summary>
        public void SetClickBurstField(TextClickBurstField? field) => clickBurst = field;

        /// <summary>Optional F7 DistortionPulse neighbor-push field.</summary>
        public void SetEmphasisPulseField(TextEmphasisPulseField? field) => emphasisPulse = field;

        /// <summary>Optional DistortionPulse explosion GIF (letter-masked overlay on body text).</summary>
        public void SetEmphasisExplosionPlayer(TextEmphasisExplosionPlayer? player) => explosionPlayer = player;

        /// <summary>
        /// Clickable hit regions used to brighten interactive text when the mouse wake is near.
        /// Pass null/empty to disable for this paint.
        /// Rebuilds the cell set only when the region fingerprint changes.
        /// </summary>
        public void SetInteractiveHitRegions(IReadOnlyList<ClickableElement>? elements)
        {
            int fingerprint = InteractiveTextHighlight.FingerprintClickableRegions(elements);
            if (interactiveCells == null || fingerprint != _interactiveCellsFingerprint)
            {
                interactiveCells = InteractiveTextHighlight.BuildInteractiveCells(elements);
                _interactiveCellsFingerprint = fingerprint;
            }

            // Prefer paint-frame config when available; otherwise read live (pre-BeginFrame).
            var cfg = windFrameConfig ?? windSway?.Config;
            interactiveHighlightEnabled = cfg?.InteractiveHighlightEnabled ?? true;
            interactiveHighlightBrighten = cfg?.InteractiveHighlightBrighten ?? 0.55;
            interactiveHighlightGlow = cfg?.InteractiveHighlightGlow ?? 0.40;
        }
        
        /// <summary>
        /// Renders all canvas elements to the drawing context.
        /// Non-overlay boxes and text render first; overlay boxes/text render last so hover tooltips'
        /// opaque fills sit above center-panel narrative but below tooltip copy.
        /// </summary>
        public void Render(
            DrawingContext context,
            double boundsWidth,
            double boundsHeight,
            List<CanvasText> textElements,
            List<CanvasBox> boxElements,
            List<CanvasProgressBar> progressBars,
            List<CanvasSegmentedBar> segmentedBars,
            Color clearBackground = default,
            bool fillBackground = true,
            List<CanvasImage>? images = null,
            CanvasBackdrop? backdrop = null,
            RPGGame.UI.TitleScreen.TitleSceneFrame? sceneUnderlay = null)
        {
            // default(Color) / transparent → solid black (normal game backdrop)
            if (fillBackground)
            {
                if (clearBackground.A == 0)
                    clearBackground = Colors.Black;

                // Clear the canvas
                context.FillRectangle(GetCachedBrush(clearBackground), new Rect(0, 0, boundsWidth, boundsHeight));
            }

            ClearDrawCachesIfFontChanged();

            double charWidth = coordinateConverter.GetCharWidth();
            double charHeight = coordinateConverter.GetCharHeight();
            windFrameActive = windSway != null && windSway.BeginFrame(charWidth, charHeight);
            windFrameConfig = windFrameActive ? windSway!.FrameConfig : null;
            burstFrameActive = clickBurst != null && clickBurst.BeginFrame(charWidth, charHeight);
            burstFrameConfig = burstFrameActive ? clickBurst!.FrameConfig : null;
            pulseFrameActive = emphasisPulse != null && emphasisPulse.BeginFrame(charWidth, charHeight);
            pulseFrameConfig = pulseFrameActive ? emphasisPulse!.FrameConfig : null;

            // Refresh highlight knobs from frame snapshot once BeginFrame has run.
            if (windFrameConfig != null)
            {
                interactiveHighlightEnabled = windFrameConfig.InteractiveHighlightEnabled;
                interactiveHighlightBrighten = windFrameConfig.InteractiveHighlightBrighten;
                interactiveHighlightGlow = windFrameConfig.InteractiveHighlightGlow;
            }

            try
            {
                if (sceneUnderlay != null)
                    RenderSceneUnderlay(context, sceneUnderlay, charWidth, charHeight);
                RenderBoxes(context, boxElements, overlayPass: false);
                if (backdrop != null)
                    RenderBackdrop(context, backdrop, charWidth, charHeight);
                if (images != null)
                    RenderImages(context, images, charWidth, charHeight);
                RenderBoxes(context, boxElements, overlayPass: false, aboveBackdrop: true);
                RenderProgressBars(context, progressBars);
                RenderSegmentedBars(context, segmentedBars);
                RenderText(context, textElements, overlayPass: false);
                // DistortionPulse GIF overlays body letters only (opacity mask like narrative video).
                explosionPlayer?.Draw(context, textElements, charWidth, charHeight);
                RenderBoxes(context, boxElements, overlayPass: true);
                RenderText(context, textElements, overlayPass: true);
            }
            finally
            {
                if (pulseFrameActive)
                    emphasisPulse!.EndFrame();
                if (burstFrameActive)
                    clickBurst!.EndFrame();
                if (windFrameActive)
                    windSway!.EndFrame();
                windFrameActive = false;
                burstFrameActive = false;
                pulseFrameActive = false;
                windFrameConfig = null;
                burstFrameConfig = null;
                pulseFrameConfig = null;
            }
        }

        private void ClearDrawCachesIfFontChanged()
        {
            double fontSize = coordinateConverter.GetFontSize();
            double centerScale = CenterColumnTextZoom.FontScale;
            Typeface typeface = coordinateConverter.GetTypeface();
            if (_cachedFontSize != fontSize
                || Math.Abs(_cachedCenterFontScale - centerScale) > 1e-9
                || !Equals(_cachedTypeface, typeface))
            {
                _formattedTextCache.Clear();
                _brushCache.Clear();
                _cachedFontSize = fontSize;
                _cachedCenterFontScale = centerScale;
                _cachedTypeface = typeface;
            }
        }

        private SolidColorBrush GetCachedBrush(Color color)
        {
            uint key = color.ToUInt32();
            if (_brushCache.TryGetValue(key, out var brush))
                return brush;
            brush = new SolidColorBrush(color);
            _brushCache[key] = brush;
            return brush;
        }

        private double ResolveFontSize(CanvasText text) =>
            !text.IgnoresCenterColumn && CenterColumnTextZoom.IsCenterColumnX(text.X)
                ? CenterColumnTextZoom.ScaledFontSize(coordinateConverter.GetFontSize())
                : coordinateConverter.GetFontSize();

        /// <summary>
        /// Monospace advance for the font size actually drawn. Rounded so center-column zoom
        /// does not place per-glyph draws on fractional X (pixel fonts jump when Round flips).
        /// </summary>
        private double ResolveGlyphAdvance(double charWidth, double fontSize)
        {
            double baseSize = coordinateConverter.GetFontSize();
            if (baseSize <= 0 || Math.Abs(fontSize - baseSize) < 1e-9)
                return Math.Max(1.0, charWidth);
            return Math.Max(1.0, Math.Round(charWidth * (fontSize / baseSize)));
        }

        /// <summary>
        /// Pixel fonts must always paint on the snapped cell grid. A multi-character
        /// <see cref="FormattedText"/> draw uses the face's natural advances (often a
        /// fraction tighter than <see cref="CanvasCoordinateConverter.SnapCellMetric"/>),
        /// so switching to per-glyph wake placement looked like a sudden kerning jump.
        /// </summary>
        internal static bool ShouldForceMonospaceGlyphPlacement(bool usesPixelGrid) => usesPixelGrid;

        /// <summary>Grid X for glyph <paramref name="glyphIndex"/> in a monospace run.</summary>
        internal static double MonospaceGlyphOriginX(double baseX, int glyphIndex, double glyphAdvance) =>
            baseX + glyphIndex * glyphAdvance;

        /// <summary>Stable single-character string for FormattedText cache keys (ASCII interned).</summary>
        internal static string GlyphString(char ch)
        {
            if (ch < AsciiGlyphStrings.Length)
                return AsciiGlyphStrings[ch];
            return ch.ToString();
        }

        private static string[] CreateAsciiGlyphStrings()
        {
            var table = new string[128];
            for (int i = 0; i < table.Length; i++)
                table[i] = ((char)i).ToString();
            return table;
        }

        /// <summary>
        /// Snaps sway/burst offsets to whole pixels for bitmap fonts so draw-origin Round
        /// cannot flicker letters between adjacent device pixels.
        /// </summary>
        internal static (double X, double Y) SnapPixelFontOffset(double x, double y, bool usesPixelGrid)
        {
            if (!usesPixelGrid)
                return (x, y);
            return (Math.Round(x), Math.Round(y));
        }

        private FormattedText GetCachedFormattedText(string content, Color color, double fontSize)
        {
            var key = (content, color.ToUInt32(), fontSize);
            if (_formattedTextCache.TryGetValue(key, out var formatted))
                return formatted;

            formatted = new FormattedText(
                content,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                coordinateConverter.GetTypeface(),
                fontSize,
                GetCachedBrush(color)
            )
            {
                MaxTextWidth = double.PositiveInfinity,
                MaxTextHeight = double.PositiveInfinity,
                Trimming = TextTrimming.None
            };
            // Cap cache growth for long unique runs (combat log lines).
            if (_formattedTextCache.Count < 512)
                _formattedTextCache[key] = formatted;
            return formatted;
        }

        private IDisposable? PushCenterColumnClip(DrawingContext context, CanvasText text, double charWidth, double charHeight)
        {
            if (text.IgnoresCenterColumn || !CenterColumnTextZoom.IsCenterColumnX(text.X))
                return null;

            int leftCol = LayoutConstants.LEFT_PANEL_X + LayoutConstants.LEFT_PANEL_WIDTH;
            int rightCol = LayoutConstants.RIGHT_PANEL_X;
            double left = leftCol * charWidth;
            double width = Math.Max(0, (rightCol - leftCol) * charWidth);
            double height = LayoutConstants.CENTER_COLUMN_FULL_HEIGHT * charHeight;
            return context.PushClip(new Rect(left, 0, width, height));
        }
        
        private void RenderBoxes(DrawingContext context, List<CanvasBox> boxElements, bool overlayPass, bool aboveBackdrop = false)
        {
            foreach (var box in boxElements)
            {
                if (box.IsOverlay != overlayPass)
                    continue;
                if (!overlayPass && box.AboveBackdrop != aboveBackdrop)
                    continue;
                RenderBox(context, box);
            }
        }
        
        private static void RenderImages(DrawingContext context, List<CanvasImage> images, double charWidth, double charHeight)
        {
            if (images.Count == 0)
                return;
            // Icons are drawn at whatever size the cell grid gives them; filter so scaling stays smooth.
            // Pixel-aligned images (the travel map) are already at screen size, so they skip the costly filter.
            var smooth = new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.HighQuality };
            var exact = new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.None };
            foreach (var image in images)
            {
                double pad = Math.Max(0, image.PaddingPixels);
                double w = image.Width * charWidth - 2 * pad;
                double h = image.Height * charHeight - 2 * pad;
                if (w <= 0 || h <= 0 || image.Source == null)
                    continue;
                double x = image.X * charWidth + pad;
                double y = image.Y * charHeight + pad;
                double dw = w;
                double dh = h;
                if (!image.FillFrame)
                {
                    double side = Math.Min(w, h);
                    x += (w - side) / 2;
                    y += (h - side) / 2;
                    dw = dh = side;
                }
                using (context.PushRenderOptions(image.PixelAligned ? exact : smooth))
                    context.DrawImage(image.Source, image.SourceRect, new Rect(x, y, dw, dh));
                if (image.Overlay != null)
                {
                    using (context.PushTransform(Matrix.CreateTranslation(image.X * charWidth, image.Y * charHeight)))
                        image.Overlay(context);
                }
            }
        }

        /// <summary>
        /// Menu-screen cave (<see cref="RPGGame.UI.TitleScreen.MenuSceneBackdrop"/>): plain grid runs under every
        /// element, with no wind sway, click burst or interactive highlight.
        /// </summary>
        private void RenderSceneUnderlay(DrawingContext context, RPGGame.UI.TitleScreen.TitleSceneFrame scene, double charWidth, double charHeight)
        {
            foreach (var block in scene.Blocks)
                context.FillRectangle(GetCachedBrush(block.Color),
                    new Rect(block.X * charWidth, block.Y * charHeight, block.Width * charWidth, block.Height * charHeight));

            double fontSize = coordinateConverter.GetFontSize();
            double glyphAdvance = ResolveGlyphAdvance(charWidth, fontSize);
            bool forceMonospace = ShouldForceMonospaceGlyphPlacement(GameFonts.UsesPixelGrid);
            foreach (var run in scene.Texts)
            {
                var text = new CanvasText { X = run.X, Y = run.Y, Content = run.Text, Color = run.Color, IgnoresCenterColumn = true };
                DrawTextRun(context, text, run.Text, run.X * charWidth, run.Y * charHeight, fontSize, glyphAdvance, forceMonospace);
            }
        }

        private void RenderBackdrop(DrawingContext context, CanvasBackdrop backdrop, double charWidth, double charHeight)
        {
            double inset = Math.Max(0, backdrop.InsetPixels);
            var dest = new Rect(
                backdrop.X * charWidth + inset,
                backdrop.Y * charHeight + inset,
                backdrop.Width * charWidth - 2 * inset,
                backdrop.Height * charHeight - 2 * inset);
            if (dest.Width <= 0 || dest.Height <= 0 || backdrop.Source == null || backdrop.Source.Width <= 0 || backdrop.Source.Height <= 0)
                return;

            // Skia draw op: cover crop, color filters, blur and blend mode (Ascii style: glyphs on the grid cells).
            var grid = new Rect(backdrop.X * charWidth, backdrop.Y * charHeight, backdrop.Width * charWidth, backdrop.Height * charHeight);
            context.Custom(new RoomBackdropDrawOperation(dest, backdrop.Source, backdrop.Look, grid, charWidth, charHeight, coordinateConverter.GetFontSize()));
        }

        private void RenderBox(DrawingContext context, CanvasBox box)
        {
            double charWidth = coordinateConverter.GetCharWidth();
            double charHeight = coordinateConverter.GetCharHeight();
            
            double x = box.X * charWidth;
            double y = box.Y * charHeight;
            double width = box.Width * charWidth;
            double height = box.Height * charHeight;

            // Opaque fill first so panel lines behind the box are hidden; border draws on top.
            if (box.BackgroundColor != Colors.Transparent)
            {
                double b = System.Math.Max(0, box.OpaqueBackgroundBleedDevicePixels);
                var fillRect = b > 0
                    ? new Rect(x - b, y - b, width + 2 * b, height + 2 * b)
                    : new Rect(x, y, width, height);
                context.FillRectangle(new SolidColorBrush(box.BackgroundColor), fillRect);
            }

            double penThickness = System.Math.Max(1, box.BorderThicknessPixels);
            var pen = new Pen(new SolidColorBrush(box.BorderColor), penThickness);
            context.DrawRectangle(null, pen, InsetRectForStroke(x, y, width, height, penThickness));
        }
        
        private void RenderProgressBars(DrawingContext context, List<CanvasProgressBar> progressBars)
        {
            foreach (var progressBar in progressBars)
            {
                RenderProgressBar(context, progressBar);
            }
        }
        
        private void RenderProgressBar(DrawingContext context, CanvasProgressBar progressBar)
        {
            double charWidth = coordinateConverter.GetCharWidth();
            double charHeight = coordinateConverter.GetCharHeight();
            
            double x = progressBar.X * charWidth;
            double y = progressBar.Y * charHeight + (progressBar.VerticalOffsetScale * charHeight);
            double width = progressBar.Width * charWidth;
            double height = charHeight * progressBar.HeightScale;

            // Background
            context.FillRectangle(new SolidColorBrush(progressBar.BackgroundColor), new Rect(x, y, width, height));

            // Calculate current health from progress
            int currentHealth = (int)(progressBar.Progress * progressBar.MaxHealth);
            
            // Render damage delta overlay (default white; DoT chunks use poison/burn/bleed colors from builder)
            if (progressBar.PreviousHealth.HasValue && progressBar.DamageDeltaStartTime.HasValue)
            {
                int previousHealth = progressBar.PreviousHealth.Value;
                int damageDelta = previousHealth - currentHealth;
                
                if (damageDelta > 0)
                {
                    // Calculate elapsed time since damage
                    var elapsed = System.DateTime.Now - progressBar.DamageDeltaStartTime.Value;
                    double solidDuration = 1.0; // Stay solid for 1 second
                    double fadeDuration = 2.0; // Total duration: 1 second solid + 1 second fade = 2 seconds
                    
                    if (elapsed.TotalSeconds < fadeDuration)
                    {
                        double alpha;
                        if (elapsed.TotalSeconds < solidDuration)
                        {
                            // Stay at full opacity for 1 second
                            alpha = 1.0;
                        }
                        else
                        {
                            // Fade from 1.0 to 0.0 over the next 1 second
                            double fadeProgress = (elapsed.TotalSeconds - solidDuration) / (fadeDuration - solidDuration);
                            alpha = 1.0 - fadeProgress;
                            alpha = System.Math.Max(0.0, System.Math.Min(1.0, alpha));
                        }
                        
                        // Calculate the position and width of the damage delta overlay
                        // The delta starts at the current health position and extends to previous health
                        double previousProgress = (double)previousHealth / progressBar.MaxHealth;
                        double currentProgress = progressBar.Progress;
                        
                        // Delta overlay starts at current health and extends to previous health
                        double deltaStartX = x + (width * currentProgress);
                        double deltaWidth = width * (previousProgress - currentProgress);
                        
                        if (deltaWidth > 0)
                        {
                            var segs = progressBar.DamageDeltaSegments;
                            int segSum = 0;
                            if (segs != null)
                            {
                                for (int i = 0; i < segs.Count; i++)
                                    segSum += segs[i].Amount;
                            }

                            if (segs != null && segs.Count > 0 && segSum == damageDelta)
                            {
                                double cursorX = deltaStartX;
                                double maxW = progressBar.MaxHealth > 0 ? width / progressBar.MaxHealth : 0;
                                for (int i = 0; i < segs.Count; i++)
                                {
                                    var seg = segs[i];
                                    double segW = maxW * seg.Amount;
                                    if (segW <= 0)
                                        continue;
                                    var brushColor = Color.FromArgb(
                                        (byte)(255 * alpha),
                                        seg.Color.R,
                                        seg.Color.G,
                                        seg.Color.B);
                                    context.FillRectangle(new SolidColorBrush(brushColor), new Rect(cursorX, y, segW, height));
                                    cursorX += segW;
                                }
                            }
                            else
                            {
                                // Default: single white delta (normal damage or unknown composition / mismatch)
                                var defaultDeltaColor = Color.FromArgb(
                                    (byte)(255 * alpha),
                                    Colors.White.R,
                                    Colors.White.G,
                                    Colors.White.B
                                );
                                context.FillRectangle(new SolidColorBrush(defaultDeltaColor), new Rect(deltaStartX, y, deltaWidth, height));
                            }
                        }
                    }
                }
            }

            // Progress (current health)
            double progressWidth = width * progressBar.Progress;
            context.FillRectangle(new SolidColorBrush(progressBar.ForegroundColor), new Rect(x, y, progressWidth, height));

            // Pack body dividers sit inside the fill so they do not paint over the white border.
            const double barPenThickness = 1;
            if (progressBar.DividerFractions != null)
            {
                var dividerBrush = new SolidColorBrush(Colors.Black);
                double dividerY = y + barPenThickness;
                double dividerHeight = System.Math.Max(0, height - 2 * barPenThickness);
                foreach (double fraction in progressBar.DividerFractions)
                {
                    if (fraction <= 0 || fraction >= 1)
                        continue;
                    double dividerX = x + (width * fraction);
                    context.FillRectangle(dividerBrush, new Rect(dividerX, dividerY, 1, dividerHeight));
                }
            }

            // Border (drawn last so edges stay clean over fill and dividers)
            var pen = new Pen(new SolidColorBrush(progressBar.BorderColor), barPenThickness);
            context.DrawRectangle(null, pen, InsetRectForStroke(x, y, width, height, barPenThickness));
        }

        private void RenderSegmentedBars(DrawingContext context, List<CanvasSegmentedBar> segmentedBars)
        {
            foreach (var bar in segmentedBars)
                RenderSegmentedBar(context, bar);
        }

        private void RenderSegmentedBar(DrawingContext context, CanvasSegmentedBar bar)
        {
            double charWidth = coordinateConverter.GetCharWidth();
            double charHeight = coordinateConverter.GetCharHeight();

            double x = bar.X * charWidth;
            double y = bar.Y * charHeight + (bar.VerticalOffsetScale * charHeight);
            double width = bar.Width * charWidth;
            double height = charHeight * bar.HeightScale;
            int totalFaces = bar.TotalFaces > 0 ? bar.TotalFaces : 20;
            double faceWidth = width / totalFaces;

            double cursorX = x;
            for (int i = 0; i < bar.Segments.Count; i++)
            {
                var segment = bar.Segments[i];
                if (segment.FaceCount <= 0)
                    continue;

                var segmentColor = bar.SegmentHighlight?.Invoke(i, segment.Color) ?? segment.Color;
                double segWidth = faceWidth * segment.FaceCount;
                context.FillRectangle(new SolidColorBrush(segmentColor), new Rect(cursorX, y, segWidth, height));

                if (i < bar.Segments.Count - 1)
                {
                    double dividerX = cursorX + segWidth;
                    context.FillRectangle(new SolidColorBrush(bar.DividerColor), new Rect(dividerX, y, 1, height));
                }

                cursorX += segWidth;
            }

            const double barPenThickness = 1;
            var pen = new Pen(new SolidColorBrush(bar.BorderColor), barPenThickness);
            context.DrawRectangle(null, pen, InsetRectForStroke(x, y, width, height, barPenThickness));

            int? rollMarker = bar.RollMarkerRoll?.Invoke();
            if (rollMarker.HasValue && rollMarker.Value >= 1 && rollMarker.Value <= totalFaces)
                RenderRollCaret(context, x, y, width, height, totalFaces, rollMarker.Value);
        }

        /// <summary>Upward-pointing pixel caret centered on the rolled d20 face, just below the bar.</summary>
        private static void RenderRollCaret(
            DrawingContext context,
            double barLeftX,
            double barTopY,
            double barWidthPx,
            double barHeightPx,
            int totalFaces,
            int roll)
        {
            double faceWidth = barWidthPx / totalFaces;
            double centerX = barLeftX + (roll - 0.5) * faceWidth;
            double tipY = barTopY + barHeightPx;
            double caretHeight = System.Math.Clamp(barHeightPx * 0.85, 3, 6);
            double halfWidth = System.Math.Clamp(faceWidth * 0.22, 2, 4);

            var geometry = new StreamGeometry();
            using (var figure = geometry.Open())
            {
                figure.BeginFigure(new Point(centerX, tipY), true);
                figure.LineTo(new Point(centerX - halfWidth, tipY + caretHeight));
                figure.LineTo(new Point(centerX + halfWidth, tipY + caretHeight));
                figure.EndFigure(true);
            }

            context.DrawGeometry(
                new SolidColorBrush(AsciiArtAssets.Colors.Gold),
                null,
                geometry);
        }

        /// <summary>
        /// Shrinks the logical rect by half the pen thickness on each side so the stroke stays inside pixel bounds.
        /// </summary>
        private static Rect InsetRectForStroke(double x, double y, double width, double height, double penThickness)
        {
            double half = penThickness / 2.0;
            double w = System.Math.Max(0, width - 2 * half);
            double h = System.Math.Max(0, height - 2 * half);
            return new Rect(x + half, y + half, w, h);
        }
        
        private void RenderText(DrawingContext context, List<CanvasText> textElements, bool overlayPass)
        {
            foreach (var text in textElements)
            {
                if (text.IsOverlay != overlayPass)
                    continue;
                RenderText(context, text);
            }
        }
        
        private void RenderText(DrawingContext context, CanvasText text)
        {
            if (string.IsNullOrEmpty(text.Content))
                return;

            double charWidth = coordinateConverter.GetCharWidth();
            double charHeight = coordinateConverter.GetCharHeight();
            double fontSize = ResolveFontSize(text);
            double glyphAdvance = ResolveGlyphAdvance(charWidth, fontSize);
            bool distort = ShouldApplyDistortion(text);
            string content = text.Content;
            var drawText = ResolveInteractiveHighlight(text, content.Length, charWidth, charHeight);

            bool forceMonospace = ShouldForceMonospaceGlyphPlacement(GameFonts.UsesPixelGrid);
            double baseX = text.X * charWidth;
            double baseY = text.Y * charHeight;

            using (PushCenterColumnClip(context, text, charWidth, charHeight))
            {
                // Outside the wake/burst AABB: idle paint. Pixel fonts still use grid advances so
                // spacing matches the per-glyph wake path (avoids tight→spaced kerning jump).
                if (!distort || !MayAffectDistortedText(text.X, text.Y, content.Length, charWidth, charHeight))
                {
                    DrawTextRun(context, drawText, content, baseX, baseY, fontSize, glyphAdvance, forceMonospace);
                    return;
                }

                // Section headers (═══ LABEL ═══ / ====  LABEL  ====): draw as one block so sway/chromatic
                // does not shift prefix glyphs into the following space.
                if (ShouldRenderHeaderAsDistortionUnit(content))
                {
                    RenderDistortedHeaderLine(context, drawText, content, charWidth, charHeight, fontSize);
                    return;
                }

                // Near the disturbance: sample per glyph, but coalesce unbroken zero-offset runs into one draw.
                var cfg = windFrameConfig ?? windSway?.FrameConfig ?? FallbackWindConfig;
                bool chromaticEnabled = cfg.ChromaticAberrationEnabled;
                double spread = cfg.ChromaticSpreadFraction;
                byte ghostAlpha = WindSwayChromatic.OpacityToAlpha(cfg.ChromaticOpacity);
                bool windChromatic = chromaticEnabled && windFrameActive;

                int i = 0;
                while (i < content.Length)
                {
                    if (content[i] == '\0')
                    {
                        i++;
                        continue;
                    }

                    var pose = SampleDistortionPose(text.X, text.Y, i, charWidth, charHeight);
                    bool moving = Math.Abs(pose.OffsetX) > WindSwayChromatic.IdleEpsilon
                        || Math.Abs(pose.OffsetY) > WindSwayChromatic.IdleEpsilon
                        || Math.Abs(pose.RotationRadians) > WindSwayChromatic.IdleEpsilon;

                    if (!moving)
                    {
                        int runStart = i;
                        i++;
                        while (i < content.Length)
                        {
                            if (content[i] == '\0')
                            {
                                i++;
                                break;
                            }

                            var next = SampleDistortionPose(text.X, text.Y, i, charWidth, charHeight);
                            if (Math.Abs(next.OffsetX) > WindSwayChromatic.IdleEpsilon
                                || Math.Abs(next.OffsetY) > WindSwayChromatic.IdleEpsilon
                                || Math.Abs(next.RotationRadians) > WindSwayChromatic.IdleEpsilon)
                                break;
                            i++;
                        }

                        int runLen = i - runStart;
                        if (runLen <= 0)
                            continue;
                        string run = content.Substring(runStart, runLen);
                        double runX = MonospaceGlyphOriginX(baseX, runStart, glyphAdvance);
                        DrawTextRun(context, drawText, run, runX, baseY, fontSize, glyphAdvance, forceMonospace);
                        continue;
                    }

                    double x = MonospaceGlyphOriginX(baseX, i, glyphAdvance) + pose.OffsetX;
                    double y = baseY + pose.OffsetY;
                    string glyph = GlyphString(content[i]);

                    // Burst chromatic is velocity-only (landed letters stay clean). Wind still uses offset.
                    double chromX = pose.ChromaticX;
                    double chromY = pose.ChromaticY;
                    bool drawChromatic = chromaticEnabled && ghostAlpha > 0
                        && (windChromatic || Math.Abs(chromX) > WindSwayChromatic.IdleEpsilon
                            || Math.Abs(chromY) > WindSwayChromatic.IdleEpsilon);

                    void DrawGlyphPasses()
                    {
                        if (drawChromatic)
                        {
                            var (fdx, fdy) = WindSwayChromatic.ComputeFringe(chromX, chromY, spread);
                            (fdx, fdy) = SnapPixelFontOffset(fdx, fdy, GameFonts.UsesPixelGrid);
                            if (Math.Abs(fdx) > WindSwayChromatic.IdleEpsilon || Math.Abs(fdy) > WindSwayChromatic.IdleEpsilon)
                            {
                                DrawTintedGlyph(context, glyph, x - fdx, y - fdy,
                                    Color.FromArgb(ghostAlpha, 255, 40, 40), fontSize);
                                DrawTintedGlyph(context, glyph, x + fdx, y + fdy,
                                    Color.FromArgb(ghostAlpha, 40, 220, 255), fontSize);
                            }
                        }

                        DrawTextBlock(context, drawText, glyph, x, y, fontSize);
                    }

                    if (Math.Abs(pose.RotationRadians) > WindSwayChromatic.IdleEpsilon)
                    {
                        double cx = x + glyphAdvance * 0.5;
                        double cy = y + charHeight * 0.5;
                        using (context.PushTransform(
                                   CreateRotateAboutPointTransform(cx, cy, pose.RotationRadians)))
                        {
                            DrawGlyphPasses();
                        }
                    }
                    else
                    {
                        DrawGlyphPasses();
                    }

                    i++;
                }
            }
        }

        /// <summary>
        /// When text sits on a clickable hit region and the mouse wake is near, return a
        /// brightened/glow copy; otherwise the original element.
        /// Section headers skip proximity highlight — cyan glow shifts gold/red labels (STR/GEAR).
        /// </summary>
        private CanvasText ResolveInteractiveHighlight(
            CanvasText text,
            int length,
            double charWidth,
            double charHeight)
        {
            if (!interactiveHighlightEnabled
                || text.IsOverlay
                || windSway == null
                || interactiveCells == null
                || interactiveCells.Count == 0
                || ShouldRenderHeaderAsDistortionUnit(text.Content)
                || !InteractiveTextHighlight.TextOverlapsInteractive(text.X, text.Y, length, interactiveCells))
            {
                return text;
            }

            // Sample at the run center so one influence drives the whole label.
            int mid = Math.Max(0, length / 2);
            double influence = windSway.SampleProximityInfluence(
                text.X, text.Y, mid, charWidth, charHeight);
            if (influence <= 1e-3)
                return text;

            Color bright = InteractiveTextHighlight.ApplyBrighten(
                text.Color, influence, interactiveHighlightBrighten);
            bool allowGlow = InteractiveTextHighlight.ShouldApplyProximityGlow(text.Color);
            double glowStrength = allowGlow
                ? Math.Clamp(interactiveHighlightGlow, 0.0, 1.0) * influence
                : 0.0;

            return new CanvasText
            {
                X = text.X,
                Y = text.Y,
                Content = text.Content,
                Color = bright,
                IsOverlay = text.IsOverlay,
                HasGlow = text.HasGlow || glowStrength > 0.05,
                GlowColor = glowStrength > 0.05
                    ? InteractiveTextHighlight.GlowColorForInfluence(influence)
                    : text.GlowColor,
                GlowIntensity = text.HasGlow
                    ? Math.Max(text.GlowIntensity, glowStrength)
                    : glowStrength,
                GlowRadius = text.HasGlow ? text.GlowRadius : 2
            };
        }

        private void DrawTintedGlyph(
            DrawingContext context,
            string content,
            double x,
            double y,
            Color color,
            double fontSize)
        {
            var formatted = GetCachedFormattedText(content, color, fontSize);
            context.DrawText(formatted, SnapDrawPoint(x, y));
        }

        private void DrawTextBlock(
            DrawingContext context,
            CanvasText text,
            string content,
            double x,
            double y,
            double fontSize)
        {
            var formatted = GetCachedFormattedText(content, text.Color, fontSize);
            Point origin = SnapDrawPoint(x, y);

            if (text.HasGlow)
            {
                TextGlowRenderer.RenderTextWithGlow(
                    context,
                    formatted,
                    origin,
                    text.GlowColor,
                    text.GlowIntensity,
                    text.GlowRadius,
                    content,
                    coordinateConverter.GetTypeface(),
                    fontSize
                );
            }
            else
            {
                context.DrawText(formatted, origin);
            }
        }

        /// <summary>
        /// Draws a text run either as one FormattedText string (proportional/system mono)
        /// or glyph-by-glyph on the snapped cell grid (pixel fonts).
        /// </summary>
        private void DrawTextRun(
            DrawingContext context,
            CanvasText text,
            string content,
            double x,
            double y,
            double fontSize,
            double glyphAdvance,
            bool forceMonospace)
        {
            if (!forceMonospace || content.Length <= 1)
            {
                DrawTextBlock(context, text, content, x, y, fontSize);
                return;
            }

            for (int i = 0; i < content.Length; i++)
            {
                char ch = content[i];
                if (ch == '\0')
                    continue;
                DrawTextBlock(
                    context,
                    text,
                    GlyphString(ch),
                    MonospaceGlyphOriginX(x, i, glyphAdvance),
                    y,
                    fontSize);
            }
        }

        /// <summary>Whole-pixel draw origins keep pixel fonts crisp when UI zoom is fractional.</summary>
        private static Point SnapDrawPoint(double x, double y) =>
            new(Math.Round(x), Math.Round(y));

        private (double OffsetX, double OffsetY) SampleDistortionOffset(
            int gridX,
            int gridY,
            int glyphIndex,
            double charWidth,
            double charHeight)
        {
            var pose = SampleDistortionPose(gridX, gridY, glyphIndex, charWidth, charHeight);
            return (pose.OffsetX, pose.OffsetY);
        }

        private DistortionPose SampleDistortionPose(
            int gridX,
            int gridY,
            int glyphIndex,
            double charWidth,
            double charHeight)
        {
            double ox = 0;
            double oy = 0;
            double rot = 0;
            double chromX = 0;
            double chromY = 0;

            if (windSway != null && ShouldSampleWind(gridX))
            {
                var (wx, wy) = windSway.SampleOffset(gridX, gridY, glyphIndex, charWidth, charHeight);
                ox += wx;
                oy += wy;
                // Wind chromatic still follows sway offset.
                chromX += wx;
                chromY += wy;
            }

            if (clickBurst != null && burstFrameActive && (burstFrameConfig?.Enabled ?? true))
            {
                var burst = clickBurst.Sample(gridX, gridY, glyphIndex, charWidth, charHeight);
                ox += burst.OffsetX;
                oy += burst.OffsetY;
                rot += burst.RotationRadians;
                // Burst chromatic is velocity-only — landed scatter stays a clean letter.
                double velScale = Math.Max(0, burstFrameConfig?.ChromaticVelocitySeconds ?? 0);
                chromX += burst.VelocityX * velScale;
                chromY += burst.VelocityY * velScale;
            }

            if (emphasisPulse != null && pulseFrameActive && (pulseFrameConfig?.Enabled ?? true))
            {
                var pulse = emphasisPulse.Sample(gridX, gridY, glyphIndex, charWidth, charHeight);
                ox += pulse.OffsetX;
                oy += pulse.OffsetY;
                double pulseScale = Math.Max(0, pulseFrameConfig?.ChromaticVelocitySeconds ?? 0);
                chromX += pulse.VelocityX * pulseScale;
                chromY += pulse.VelocityY * pulseScale;
            }

            bool pixelGrid = GameFonts.UsesPixelGrid;
            (ox, oy) = SnapPixelFontOffset(ox, oy, pixelGrid);
            (chromX, chromY) = SnapPixelFontOffset(chromX, chromY, pixelGrid);

            return new DistortionPose(ox, oy, rot, chromX, chromY);
        }

        private readonly struct DistortionPose
        {
            public DistortionPose(double offsetX, double offsetY, double rotationRadians, double chromaticX, double chromaticY)
            {
                OffsetX = offsetX;
                OffsetY = offsetY;
                RotationRadians = rotationRadians;
                ChromaticX = chromaticX;
                ChromaticY = chromaticY;
            }

            public double OffsetX { get; }
            public double OffsetY { get; }
            public double RotationRadians { get; }
            public double ChromaticX { get; }
            public double ChromaticY { get; }
        }

        private bool MayAffectDistortedText(int gridX, int gridY, int length, double charWidth, double charHeight)
        {
            if (clickBurst != null && clickBurst.MayAffectText(gridX, gridY, length, charWidth, charHeight))
                return true;

            if (emphasisPulse != null && emphasisPulse.MayAffectText(gridX, gridY, length, charWidth, charHeight))
                return true;

            if (windSway != null && ShouldSampleWind(gridX)
                && windSway.MayAffectText(gridX, gridY, length, charWidth, charHeight))
            {
                return true;
            }

            return false;
        }

        private bool ShouldSampleWind(int gridX)
        {
            // Use BeginFrame snapshot — avoids locked IsActive/Config per text element.
            if (!windFrameActive || windSway == null)
                return false;
            var cfg = windFrameConfig ?? windSway.FrameConfig;
            if (!cfg.Enabled)
                return false;
            if (!cfg.SidePanelsOnly)
                return true;
            return IsInSidePanel(gridX);
        }

        /// <summary>
        /// Panel section titles must sway as one string; per-glyph offset makes the prefix run into the label space.
        /// </summary>
        internal static bool ShouldRenderHeaderAsDistortionUnit(string content)
        {
            if (string.IsNullOrEmpty(content))
                return false;

            if (content.StartsWith(AsciiArtAssets.UIText.HeaderPrefix + " ", StringComparison.Ordinal))
                return true;

            // Left panel: ====  LABEL  ====
            return content.StartsWith("====  ", StringComparison.Ordinal);
        }

        private void RenderDistortedHeaderLine(
            DrawingContext context,
            CanvasText text,
            string content,
            double charWidth,
            double charHeight,
            double fontSize)
        {
            var pose = SampleDistortionPose(text.X, text.Y, 0, charWidth, charHeight);
            double baseX = text.X * charWidth;
            double baseY = text.Y * charHeight;
            bool moving = Math.Abs(pose.OffsetX) > WindSwayChromatic.IdleEpsilon
                || Math.Abs(pose.OffsetY) > WindSwayChromatic.IdleEpsilon
                || Math.Abs(pose.RotationRadians) > WindSwayChromatic.IdleEpsilon;

            bool forceMonospace = ShouldForceMonospaceGlyphPlacement(GameFonts.UsesPixelGrid);
            double glyphAdvance = ResolveGlyphAdvance(charWidth, fontSize);

            // Stay on the same idle advances when the wake AABB is entered but offset is still 0;
            // DrawTextBlock(FormattedText) would tighten kerning vs DrawTextRun(forceMonospace).
            if (!moving)
            {
                DrawTextRun(context, text, content, baseX, baseY, fontSize, glyphAdvance, forceMonospace);
                return;
            }

            var cfg = windFrameConfig ?? windSway?.FrameConfig ?? FallbackWindConfig;
            bool chromaticEnabled = cfg.ChromaticAberrationEnabled;
            double spread = cfg.ChromaticSpreadFraction;
            byte ghostAlpha = WindSwayChromatic.OpacityToAlpha(cfg.ChromaticOpacity);

            double x = baseX + pose.OffsetX;
            double y = baseY + pose.OffsetY;

            void DrawHeaderPasses()
            {
                if (chromaticEnabled && ghostAlpha > 0)
                {
                    var (fdx, fdy) = WindSwayChromatic.ComputeFringe(pose.ChromaticX, pose.ChromaticY, spread);
                    (fdx, fdy) = SnapPixelFontOffset(fdx, fdy, GameFonts.UsesPixelGrid);
                    if (Math.Abs(fdx) > WindSwayChromatic.IdleEpsilon || Math.Abs(fdy) > WindSwayChromatic.IdleEpsilon)
                    {
                        DrawTintedGlyph(context, content, x - fdx, y - fdy,
                            Color.FromArgb(ghostAlpha, 255, 40, 40), fontSize);
                        DrawTintedGlyph(context, content, x + fdx, y + fdy,
                            Color.FromArgb(ghostAlpha, 40, 220, 255), fontSize);
                    }
                }

                DrawTextBlock(context, text, content, x, y, fontSize);
            }

            if (Math.Abs(pose.RotationRadians) > WindSwayChromatic.IdleEpsilon)
            {
                double cx = x + content.Length * glyphAdvance * 0.5;
                double cy = y + charHeight * 0.5;
                using (context.PushTransform(
                           CreateRotateAboutPointTransform(cx, cy, pose.RotationRadians)))
                {
                    DrawHeaderPasses();
                }
            }
            else
            {
                DrawHeaderPasses();
            }
        }

        private bool ShouldApplyDistortion(CanvasText text)
        {
            bool burst = burstFrameActive && (burstFrameConfig?.Enabled ?? true);
            bool pulse = pulseFrameActive && (pulseFrameConfig?.Enabled ?? true);
            return DistortionAppliesToBodyText(
                text.IsOverlay,
                burst,
                pulse,
                ShouldSampleWind(text.X));
        }

        /// <summary>
        /// Body text (not overlay) uses per-glyph distortion when click-burst, emphasis pulse,
        /// or wind applies. Pulse is independent of wind so F7 SLAM still shoves neighbors
        /// while the rest of the sentence is typing.
        /// </summary>
        internal static bool DistortionAppliesToBodyText(
            bool isOverlay,
            bool burstActive,
            bool pulseActive,
            bool windApplies)
        {
            if (isOverlay)
                return false;
            return burstActive || pulseActive || windApplies;
        }

        /// <summary>
        /// Rotate about an arbitrary point under Avalonia row-vector composition (<c>p' = p * M</c>).
        /// Must be <c>T(-C) * R * T(C)</c> — the column-vector order <c>T(C) * R * T(-C)</c> spins around world (0,0).
        /// </summary>
        internal static Matrix CreateRotateAboutPointTransform(
            double centerX,
            double centerY,
            double rotationRadians)
            => Matrix.CreateTranslation(-centerX, -centerY)
               * Matrix.CreateRotation(rotationRadians)
               * Matrix.CreateTranslation(centerX, centerY);

        /// <summary>Left or right character-panel columns (excludes center combat log / strip).</summary>
        internal static bool IsInSidePanel(int gridX)
        {
            int leftStart = LayoutConstants.LEFT_PANEL_X;
            int leftEnd = leftStart + LayoutConstants.LEFT_PANEL_WIDTH;
            if (gridX >= leftStart && gridX < leftEnd)
                return true;

            int rightStart = LayoutConstants.RIGHT_PANEL_X;
            int rightEnd = rightStart + LayoutConstants.RIGHT_PANEL_WIDTH;
            return gridX >= rightStart && gridX < rightEnd;
        }
    }
}

