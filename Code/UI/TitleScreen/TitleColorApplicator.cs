using System;
using System.Text;

namespace RPGGame.UI.TitleScreen
{
    /// <summary>
    /// Handles color application to ASCII art text using the game's color markup system
    /// Supports solid colors and progressive character-by-character transitions
    /// </summary>
    public static class TitleColorApplicator
    {
        /// <summary>
        /// Applies a solid color to text using modern single-prefix approach
        /// Uses one color code at the start instead of per-character coloring
        /// </summary>
        /// <param name="text">Text to colorize</param>
        /// <param name="colorCode">Single-letter color code (e.g., "R", "G", "k")</param>
        /// <returns>Text with color markup applied</returns>
        public static string ApplySolidColor(string text, string colorCode)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(colorCode))
            {
                return text;
            }

            // Modern approach: Single color code at the start applies to entire line
            // This is much more efficient than per-character coloring
            return $"&{colorCode}{text}";
        }

        /// <summary>
        /// Applies a transitioning color to text using multi-stage progression
        /// Creates smooth transitions by progressing through intermediate colors
        /// </summary>
        /// <param name="text">Text to colorize</param>
        /// <param name="sourceColor">Color to transition from (e.g., "Y" for white)</param>
        /// <param name="targetColor">Color to transition to (e.g., "W" for gold or "O" for orange)</param>
        /// <param name="progress">Transition progress (0.0 to 1.0)</param>
        /// <returns>Text with color markup applied</returns>
        public static string ApplyTransitionColor(string text, string sourceColor, string targetColor, float progress)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            // Multi-stage color progression based on target color
            string color = GetProgressiveColor(sourceColor, targetColor, progress);
            
            return $"&{color}{text}";
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
    }
}

