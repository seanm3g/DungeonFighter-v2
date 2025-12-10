using Avalonia.Media;
using System;

namespace RPGGame.UI.Avalonia.Canvas
{

    /// <summary>
    /// Handles coordinate conversion and character measurement for the canvas.
    /// </summary>
    public class CanvasCoordinateConverter
    {
        // Font properties - using explicit monospace font
        // "Courier New" is a guaranteed monospace font available on all systems
        // It's a serif, typewriter-style monospace font
        private readonly Typeface typeface = new("Courier New");
        private const double baseFontSize = 16; // Base font size for scaling
        private double fontSize = baseFontSize; // Current font size (can be scaled)
        private double charWidth; // Actual measured width of a character
        private double charHeight; // Actual measured height of a character
        private double scaleFactor = 1.0; // Scaling factor for pixel size
        
        /// <summary>
        /// Gets the typeface used for rendering
        /// </summary>
        public Typeface GetTypeface() => typeface;
        
        /// <summary>
        /// Gets the font size used for rendering
        /// </summary>
        public double GetFontSize() => fontSize;
        
        /// <summary>
        /// Sets the scaling factor for character size (pixel scaling)
        /// </summary>
        public void SetScaleFactor(double factor)
        {
            if (factor > 0)
            {
                scaleFactor = factor;
                fontSize = baseFontSize * scaleFactor;
                // Reset measured dimensions so they'll be recalculated with new font size
                charWidth = 0;
                charHeight = 0;
            }
        }
        
        /// <summary>
        /// Gets the current scaling factor
        /// </summary>
        public double GetScaleFactor() => scaleFactor;
        
        /// <summary>
        /// Measure actual character width and height on first use
        /// </summary>
        public void EnsureCharWidthMeasured()
        {
            if (charWidth == 0)
            {
                // Measure actual width and height of a single character in the monospace font
                var testText = new FormattedText(
                    "M", // Use 'M' as it's typically the widest character
                    System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    fontSize, // Use current scaled font size
                    Brushes.White
                )
                {
                    MaxTextWidth = double.PositiveInfinity,
                    MaxTextHeight = double.PositiveInfinity,
                    Trimming = TextTrimming.None
                };
                charWidth = testText.Width;
                charHeight = testText.Height;
            }
        }
        
        /// <summary>
        /// Exposes character width for coordinate conversion
        /// </summary>
        public double GetCharWidth()
        {
            EnsureCharWidthMeasured();
            return charWidth;
        }

        /// <summary>
        /// Exposes character height for coordinate conversion
        /// </summary>
        public double GetCharHeight()
        {
            EnsureCharWidthMeasured();
            return charHeight;
        }
        
        /// <summary>
        /// Measures the actual pixel width of text using FormattedText
        /// Returns the width in character units (pixels / charWidth)
        /// </summary>
        public double MeasureTextWidth(string text)
        {
            EnsureCharWidthMeasured();
            
            var formatted = new FormattedText(
                text,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize, // Use current scaled font size
                Brushes.White
            )
            {
                MaxTextWidth = double.PositiveInfinity,
                MaxTextHeight = double.PositiveInfinity,
                Trimming = TextTrimming.None
            };
            return formatted.Width / charWidth;
        }
    }
}

