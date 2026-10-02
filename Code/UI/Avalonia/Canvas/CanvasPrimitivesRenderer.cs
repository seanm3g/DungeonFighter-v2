using Avalonia;
using Avalonia.Media;
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
        private HashSet<long>? interactiveCells;
        private int _interactiveCellsFingerprint = int.MinValue;
        private double interactiveHighlightBrighten = 0.55;
        private double interactiveHighlightGlow = 0.40;
        private bool interactiveHighlightEnabled;
        /// <summary>True for the current paint while <see cref="WindSwayField.BeginFrame"/> succeeded.</summary>
        private bool windFrameActive;
        /// <summary>True for the current paint while <see cref="TextClickBurstField.BeginFrame"/> succeeded.</summary>
        private bool burstFrameActive;
        private WindSwayConfig? windFrameConfig;
        private TextClickBurstConfig? burstFrameConfig;

        // Per-paint FormattedText / brush reuse (cleared at Render start).
        private readonly Dictionary<(string Text, uint ColorArgb, double FontSize), FormattedText> _formattedTextCache = new();
        private readonly Dictionary<uint, SolidColorBrush> _brushCache = new();
        private double _cachedFontSize = -1;
        private Typeface? _cachedTypeface;

        public CanvasPrimitivesRenderer(CanvasCoordinateConverter coordinateConverter)
        {
            this.coordinateConverter = coordinateConverter ?? throw new ArgumentNullException(nameof(coordinateConverter));
        }

        /// <summary>Optional mouse-driven wind field for per-glyph side-panel sway.</summary>
        public void SetWindSwayField(WindSwayField? field) => windSway = field;

        /// <summary>Optional click-charge explode/reform field (any non-overlay text).</summary>
        public void SetClickBurstField(TextClickBurstField? field) => clickBurst = field;

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
            bool fillBackground = true)
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

            // Refresh highlight knobs from frame snapshot once BeginFrame has run.
            if (windFrameConfig != null)
            {
                interactiveHighlightEnabled = windFrameConfig.InteractiveHighlightEnabled;
                interactiveHighlightBrighten = windFrameConfig.InteractiveHighlightBrighten;
                interactiveHighlightGlow = windFrameConfig.InteractiveHighlightGlow;
            }

            try
            {
                RenderBoxes(context, boxElements, overlayPass: false);
                RenderProgressBars(context, progressBars);
                RenderSegmentedBars(context, segmentedBars);
                RenderText(context, textElements, overlayPass: false);
                RenderBoxes(context, boxElements, overlayPass: true);
                RenderText(context, textElements, overlayPass: true);
            }
            finally
            {
                if (burstFrameActive)
                    clickBurst!.EndFrame();
                if (windFrameActive)
                    windSway!.EndFrame();
                windFrameActive = false;
                burstFrameActive = false;
                windFrameConfig = null;
                burstFrameConfig = null;
            }
        }

        private void ClearDrawCachesIfFontChanged()
        {
            double fontSize = coordinateConverter.GetFontSize();
            Typeface typeface = coordinateConverter.GetTypeface();
            if (_cachedFontSize != fontSize || !Equals(_cachedTypeface, typeface))
            {
                _formattedTextCache.Clear();
                _brushCache.Clear();
                _cachedFontSize = fontSize;
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

        private FormattedText GetCachedFormattedText(string content, Color color)
        {
            double fontSize = coordinateConverter.GetFontSize();
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
        
        private void RenderBoxes(DrawingContext context, List<CanvasBox> boxElements, bool overlayPass)
        {
            foreach (var box in boxElements)
            {
                if (box.IsOverlay != overlayPass)
                    continue;
                RenderBox(context, box);
            }
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
            bool distort = ShouldApplyDistortion(text);
            string content = text.Content;
            var drawText = ResolveInteractiveHighlight(text, content.Length, charWidth, charHeight);

            // Outside the wake/burst AABB: one string draw (avoids per-glyph FormattedText).
            if (!distort || !MayAffectDistortedText(text.X, text.Y, content.Length, charWidth, charHeight))
            {
                DrawTextBlock(context, drawText, content, text.X * charWidth, text.Y * charHeight);
                return;
            }

            // Section headers (═══ LABEL ═══ / ====  LABEL  ====): draw as one block so sway/chromatic
            // does not shift prefix glyphs into the following space.
            if (ShouldRenderHeaderAsDistortionUnit(content))
            {
                RenderDistortedHeaderLine(context, drawText, content, charWidth, charHeight);
                return;
            }

            // Near the disturbance: sample per glyph, but coalesce unbroken zero-offset runs into one draw.
            var cfg = windFrameConfig ?? windSway?.FrameConfig ?? new WindSwayConfig();
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
                    DrawTextBlock(context, drawText, run, (text.X + runStart) * charWidth, text.Y * charHeight);
                    continue;
                }

                double x = (text.X + i) * charWidth + pose.OffsetX;
                double y = text.Y * charHeight + pose.OffsetY;
                string glyph = content[i].ToString();

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
                        if (Math.Abs(fdx) > WindSwayChromatic.IdleEpsilon || Math.Abs(fdy) > WindSwayChromatic.IdleEpsilon)
                        {
                            DrawTintedGlyph(context, glyph, x - fdx, y - fdy,
                                Color.FromArgb(ghostAlpha, 255, 40, 40));
                            DrawTintedGlyph(context, glyph, x + fdx, y + fdy,
                                Color.FromArgb(ghostAlpha, 40, 220, 255));
                        }
                    }

                    DrawTextBlock(context, drawText, glyph, x, y);
                }

                if (Math.Abs(pose.RotationRadians) > WindSwayChromatic.IdleEpsilon)
                {
                    double cx = x + charWidth * 0.5;
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

        private void DrawTintedGlyph(DrawingContext context, string content, double x, double y, Color color)
        {
            var formatted = GetCachedFormattedText(content, color);
            context.DrawText(formatted, SnapDrawPoint(x, y));
        }

        private void DrawTextBlock(DrawingContext context, CanvasText text, string content, double x, double y)
        {
            var formatted = GetCachedFormattedText(content, text.Color);
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
                    coordinateConverter.GetFontSize()
                );
            }
            else
            {
                context.DrawText(formatted, origin);
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
            double charHeight)
        {
            var pose = SampleDistortionPose(text.X, text.Y, 0, charWidth, charHeight);
            double baseX = text.X * charWidth;
            double baseY = text.Y * charHeight;
            bool moving = Math.Abs(pose.OffsetX) > WindSwayChromatic.IdleEpsilon
                || Math.Abs(pose.OffsetY) > WindSwayChromatic.IdleEpsilon
                || Math.Abs(pose.RotationRadians) > WindSwayChromatic.IdleEpsilon;

            if (!moving)
            {
                DrawTextBlock(context, text, content, baseX, baseY);
                return;
            }

            var cfg = windFrameConfig ?? windSway?.FrameConfig ?? new WindSwayConfig();
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
                    if (Math.Abs(fdx) > WindSwayChromatic.IdleEpsilon || Math.Abs(fdy) > WindSwayChromatic.IdleEpsilon)
                    {
                        DrawTintedGlyph(context, content, x - fdx, y - fdy,
                            Color.FromArgb(ghostAlpha, 255, 40, 40));
                        DrawTintedGlyph(context, content, x + fdx, y + fdy,
                            Color.FromArgb(ghostAlpha, 40, 220, 255));
                    }
                }

                DrawTextBlock(context, text, content, x, y);
            }

            if (Math.Abs(pose.RotationRadians) > WindSwayChromatic.IdleEpsilon)
            {
                double cx = x + content.Length * charWidth * 0.5;
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
            if (text.IsOverlay)
                return false;

            // Use BeginFrame snapshot — avoids locked IsActive/Config during paint.
            if (burstFrameActive && (burstFrameConfig?.Enabled ?? true))
                return true;

            return ShouldSampleWind(text.X);
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

