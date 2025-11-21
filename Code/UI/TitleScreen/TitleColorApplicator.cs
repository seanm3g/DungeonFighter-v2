using System;
using System.Collections.Generic;
using System.Text;
using RPGGame.UI.ColorSystem;
using Avalonia.Media;

namespace RPGGame.UI.TitleScreen
{
    /// <summary>
    /// Handles color application to ASCII art text using the game's color template system
    /// Supports solid colors and progressive character-by-character transitions
    /// </summary>
    public static class TitleColorApplicator
    {
        /// <summary>
        /// Applies a template to text using the color template system
        /// </summary>
        /// <param name="text">Text to colorize</param>
        /// <param name="templateName">Template name (e.g., "golden", "fiery")</param>
        /// <returns>List of ColoredText segments</returns>
        public static List<ColoredText> ApplyTemplate(string text, string templateName)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new List<ColoredText>();
            }

            // Use the color template library to apply the template
            return ColorTemplateLibrary.GetTemplate(templateName, text);
        }

        /// <summary>
        /// Applies a solid color to text using a single color from the palette
        /// Used for transitions and simple color application
        /// </summary>
        /// <param name="text">Text to colorize</param>
        /// <param name="colorCode">Single-letter color code (e.g., "R", "G", "W")</param>
        /// <returns>List of ColoredText segments</returns>
        public static List<ColoredText> ApplySolidColor(string text, string colorCode)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new List<ColoredText>();
            }

            // Convert color code to ColorPalette and use SingleColor template
            var color = ConvertColorCodeToColor(colorCode);
            return ColorTemplateLibrary.SingleColor(text, color);
        }

        /// <summary>
        /// Applies a transitioning color to text using multi-stage progression
        /// Creates smooth transitions by progressing through intermediate colors
        /// </summary>
        /// <param name="text">Text to colorize</param>
        /// <param name="sourceColor">Color to transition from (e.g., "Y" for white)</param>
        /// <param name="targetColor">Color to transition to (e.g., "W" for gold or "O" for orange)</param>
        /// <param name="progress">Transition progress (0.0 to 1.0)</param>
        /// <returns>List of ColoredText segments</returns>
        public static List<ColoredText> ApplyTransitionColor(string text, string sourceColor, string targetColor, float progress)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new List<ColoredText>();
            }

            // Multi-stage color progression based on target color
            string colorCode = GetProgressiveColor(sourceColor, targetColor, progress);
            var color = ConvertColorCodeToColor(colorCode);
            
            return ColorTemplateLibrary.SingleColor(text, color);
        }

        /// <summary>
        /// Determines the appropriate color for a given progress value
        /// Implements multi-stage transitions for smooth color progression
        /// </summary>
        private static string GetProgressiveColor(string sourceColor, string targetColor, float progress)
        {
            // Clamp progress to valid range
            progress = Math.Clamp(progress, 0.0f, 1.0f);

            // Define intermediate color stages based on target color
            // This creates smooth visual transitions that match the game's aesthetic
            
            // Transition to warm colors (W = gold/yellow)
            if (targetColor == "W")
            {
                if (progress < 0.40f) return "Y";  // White (0-40%)
                if (progress < 0.70f) return "y";  // Pale grey (40-70%)
                if (progress < 0.85f) return "y";  // Grey (70-85%)
                return "W";                         // Gold (85-100%)
            }
            
            // Transition to orange (O)
            if (targetColor == "O")
            {
                if (progress < 0.30f) return "Y";  // White (0-30%)
                if (progress < 0.50f) return "y";  // Grey (30-50%)
                if (progress < 0.70f) return "W";  // Yellow/Warm (50-70%)
                if (progress < 0.85f) return "o";  // Light orange (70-85%)
                return "O";                         // Full orange (85-100%)
            }
            
            // Transition to red (R)  
            if (targetColor == "R")
            {
                if (progress < 0.30f) return "Y";  // White (0-30%)
                if (progress < 0.50f) return "y";  // Grey (30-50%)
                if (progress < 0.70f) return "r";  // Dark red (50-70%)
                if (progress < 0.85f) return "R";  // Bright red (70-85%)
                return "R";                         // Bright red (85-100%)
            }
            
            // Transition to cyan (C)
            if (targetColor == "C")
            {
                if (progress < 0.30f) return "Y";  // White (0-30%)
                if (progress < 0.60f) return "y";  // Grey (30-60%)
                if (progress < 0.80f) return "B";  // Blue (60-80%)
                return "C";                         // Cyan (80-100%)
            }
            
            // Transition to green (G)
            if (targetColor == "G")
            {
                if (progress < 0.30f) return "Y";  // White (0-30%)
                if (progress < 0.60f) return "y";  // Grey (30-60%)
                if (progress < 0.80f) return "g";  // Dark green (60-80%)
                return "G";                         // Green (80-100%)
            }
            
            // Default: simple binary transition for other colors
            return progress < 0.5f ? sourceColor : targetColor;
        }

        /// <summary>
        /// Validates that a color code is a single character
        /// </summary>
        /// <param name="colorCode">Color code to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidColorCode(string colorCode)
        {
            return !string.IsNullOrEmpty(colorCode) && colorCode.Length == 1;
        }

        /// <summary>
        /// Converts a color code to an Avalonia Color
        /// Maps single-letter color codes to their corresponding colors
        /// </summary>
        private static Color ConvertColorCodeToColor(string colorCode)
        {
            if (string.IsNullOrEmpty(colorCode))
                return Colors.White;

            return colorCode.ToUpper() switch
            {
                "R" => ColorPalette.Red.GetColor(),
                "r" => ColorPalette.DarkRed.GetColor(),
                "G" => ColorPalette.Green.GetColor(),
                "g" => ColorPalette.DarkGreen.GetColor(),
                "B" => ColorPalette.Blue.GetColor(),
                "b" => ColorPalette.DarkBlue.GetColor(),
                "C" => ColorPalette.Cyan.GetColor(),
                "c" => ColorPalette.DarkCyan.GetColor(),
                "M" => ColorPalette.Magenta.GetColor(),
                "m" => ColorPalette.DarkMagenta.GetColor(),
                "O" => ColorPalette.Orange.GetColor(),
                "o" => Color.FromRgb(200, 100, 0), // Dark orange
                "W" => ColorPalette.Gold.GetColor(),
                "w" => ColorPalette.Brown.GetColor(),
                "Y" => Colors.White,
                "y" => ColorPalette.Gray.GetColor(),
                "K" => ColorPalette.DarkGray.GetColor(),
                "k" => Colors.Black,
                _ => Colors.White
            };
        }
    }
}

