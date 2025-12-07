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
        private const double fontSize = 16; // Increased from 14 for better readability
        private double charWidth; // Actual measured width of a character
        private double charHeight; // Actual measured height of a character
        
        /// <summary>
        /// Gets the typeface used for rendering
        /// </summary>
        public Typeface GetTypeface() => typeface;
        
        /// <summary>
        /// Gets the font size used for rendering
        /// </summary>
        public double GetFontSize() => fontSize;
        
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
                    fontSize,
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
                fontSize,
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

