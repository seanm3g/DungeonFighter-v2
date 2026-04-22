using Avalonia;
using Avalonia.Media;
using System.Collections.Generic;
using System.Linq;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Effects;

namespace RPGGame.UI.Avalonia.Canvas
{

    /// <summary>
    /// Low-level drawing of canvas primitives (boxes, progress bars, text) to a <see cref="DrawingContext"/>.
    /// Distinct from <see cref="RPGGame.UI.Avalonia.Renderers.CanvasRenderer"/>, which orchestrates full-screen game UI.
    /// </summary>
    public class CanvasPrimitivesRenderer
    {
        private const double OnePixelPenHalfThickness = 0.5;

        private readonly CanvasCoordinateConverter coordinateConverter;
        
        public CanvasPrimitivesRenderer(CanvasCoordinateConverter coordinateConverter)
        {
            this.coordinateConverter = coordinateConverter ?? throw new ArgumentNullException(nameof(coordinateConverter));
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
            List<CanvasProgressBar> progressBars)
        {
            // Clear the canvas
            context.FillRectangle(Brushes.Black, new Rect(0, 0, boundsWidth, boundsHeight));
            
            RenderBoxes(context, boxElements, overlayPass: false);
            RenderProgressBars(context, progressBars);
            RenderText(context, textElements, overlayPass: false);
            RenderBoxes(context, boxElements, overlayPass: true);
            RenderText(context, textElements, overlayPass: true);
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

            var pen = new Pen(new SolidColorBrush(box.BorderColor), 1);
            context.DrawRectangle(null, pen, InsetRectForOnePixelStroke(x, y, width, height));
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
            double y = progressBar.Y * charHeight;
            double width = progressBar.Width * charWidth;
            double height = charHeight;

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

            // Border
            var pen = new Pen(new SolidColorBrush(progressBar.BorderColor), 1);
            context.DrawRectangle(null, pen, InsetRectForOnePixelStroke(x, y, width, height));
        }

        /// <summary>
        /// Shrinks the logical rect by half a 1px pen thickness on each side so the stroke stays inside pixel bounds.
        /// </summary>
        private static Rect InsetRectForOnePixelStroke(double x, double y, double width, double height)
        {
            double w = System.Math.Max(0, width - 2 * OnePixelPenHalfThickness);
            double h = System.Math.Max(0, height - 2 * OnePixelPenHalfThickness);
            return new Rect(x + OnePixelPenHalfThickness, y + OnePixelPenHalfThickness, w, h);
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
            double charWidth = coordinateConverter.GetCharWidth();
            double charHeight = coordinateConverter.GetCharHeight();
            
            double x = text.X * charWidth;
            double y = text.Y * charHeight;

            // Create FormattedText with MaxTextWidth set to prevent letter-spacing issues
            // Using explicit monospace font (Courier New) ensures consistent character widths
            var formatted = new FormattedText(
                text.Content,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                coordinateConverter.GetTypeface(),
                coordinateConverter.GetFontSize(),
                new SolidColorBrush(text.Color)
            )
            {
                // Prevent text wrapping which can add extra spacing
                MaxTextWidth = double.PositiveInfinity,
                MaxTextHeight = double.PositiveInfinity,
                // Ensure no letter-spacing is applied
                Trimming = TextTrimming.None
            };

            // Render with glow if enabled
            if (text.HasGlow)
            {
                TextGlowRenderer.RenderTextWithGlow(
                    context,
                    formatted,
                    new Point(x, y),
                    text.GlowColor,
                    text.GlowIntensity,
                    text.GlowRadius,
                    text.Content,
                    coordinateConverter.GetTypeface(),
                    coordinateConverter.GetFontSize()
                );
            }
            else
            {
                context.DrawText(formatted, new Point(x, y));
            }
        }
    }
}

