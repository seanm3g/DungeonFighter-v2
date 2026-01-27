using Avalonia;
using Avalonia.Media;
using System.Collections.Generic;
using System.Linq;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Effects;

namespace RPGGame.UI.Avalonia.Canvas
{

    /// <summary>
    /// Handles rendering operations for canvas elements (boxes, progress bars, text).
    /// </summary>
    public class CanvasRenderer
    {
        private readonly CanvasCoordinateConverter coordinateConverter;
        
        public CanvasRenderer(CanvasCoordinateConverter coordinateConverter)
        {
            this.coordinateConverter = coordinateConverter ?? throw new ArgumentNullException(nameof(coordinateConverter));
        }
        
        /// <summary>
        /// Renders all canvas elements to the drawing context
        /// IMPORTANT: Text is rendered AFTER boxes to ensure text appears on top of box borders
        /// This prevents box border strokes from covering text that's positioned near the border
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
            
            // Render all elements
            // NOTE: Boxes render first (they have transparent backgrounds, only borders are drawn)
            // Text renders last to ensure it appears on top of box borders
            RenderBoxes(context, boxElements);
            RenderProgressBars(context, progressBars);
            RenderText(context, textElements);
        }
        
        private void RenderBoxes(DrawingContext context, List<CanvasBox> boxElements)
        {
            foreach (var box in boxElements)
            {
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

            // Draw border
            var pen = new Pen(new SolidColorBrush(box.BorderColor), 1);
            context.DrawRectangle(null, pen, new Rect(x, y, width, height));

            // Fill background if specified
            if (box.BackgroundColor != Colors.Transparent)
            {
                context.FillRectangle(new SolidColorBrush(box.BackgroundColor), new Rect(x, y, width, height));
            }
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
            
            // Render damage delta overlay (yellow) if applicable
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
                            // Create yellow color with alpha fade
                            var yellowColor = Color.FromArgb(
                                (byte)(255 * alpha),
                                Colors.Yellow.R,
                                Colors.Yellow.G,
                                Colors.Yellow.B
                            );
                            
                            context.FillRectangle(new SolidColorBrush(yellowColor), new Rect(deltaStartX, y, deltaWidth, height));
                        }
                    }
                }
            }

            // Progress (current health)
            double progressWidth = width * progressBar.Progress;
            context.FillRectangle(new SolidColorBrush(progressBar.ForegroundColor), new Rect(x, y, progressWidth, height));

            // Border
            var pen = new Pen(new SolidColorBrush(progressBar.BorderColor), 1);
            context.DrawRectangle(null, pen, new Rect(x, y, width, height));
        }
        
        private void RenderText(DrawingContext context, List<CanvasText> textElements)
        {
            foreach (var text in textElements)
            {
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

