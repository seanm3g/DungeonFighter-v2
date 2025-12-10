using Avalonia;
using Avalonia.Media;
using System;
using System.Globalization;

namespace RPGGame.UI.Avalonia.Effects
{
    /// <summary>
    /// Renders glow effects for text elements
    /// </summary>
    public static class TextGlowRenderer
    {
        /// <summary>
        /// Renders glow layers behind text, then the text itself
        /// </summary>
        public static void RenderTextWithGlow(
            DrawingContext context,
            FormattedText formattedText,
            Point position,
            Color glowColor,
            double glowIntensity,
            int glowRadius,
            string textContent,
            Typeface typeface,
            double fontSize)
        {
            if (glowIntensity <= 0 || glowRadius <= 0)
            {
                // No glow - just render text
                context.DrawText(formattedText, position);
                return;
            }

            // Render glow layers (outer to inner)
            int glowPasses = Math.Max(1, glowRadius);
            for (int i = glowPasses; i > 0; i--)
            {
                double offset = i * 0.5; // Slight offset for each layer
                double alpha = glowIntensity * (1.0 - (double)i / glowPasses) * 0.5; // Fade from outer to inner
                
                // Create glow text with adjusted color and opacity
                var glowFormattedText = new FormattedText(
                    textContent,
                    CultureInfo.InvariantCulture,
                    formattedText.FlowDirection,
                    typeface,
                    fontSize,
                    new SolidColorBrush(
                        Color.FromArgb(
                            (byte)(255 * alpha),
                            glowColor.R,
                            glowColor.G,
                            glowColor.B
                        )
                    )
                )
                {
                    MaxTextWidth = formattedText.MaxTextWidth,
                    MaxTextHeight = formattedText.MaxTextHeight,
                    Trimming = formattedText.Trimming
                };

                // Render glow layer with slight offsets in all directions
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue; // Skip center
                        
                        var glowPosition = new Point(
                            position.X + dx * offset,
                            position.Y + dy * offset
                        );
                        context.DrawText(glowFormattedText, glowPosition);
                    }
                }
            }

            // Render actual text on top
            context.DrawText(formattedText, position);
        }
    }
}

