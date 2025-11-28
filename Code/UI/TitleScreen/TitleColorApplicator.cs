using System;
using System.Collections.Generic;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.TitleScreen
{
    /// <summary>
    /// Handles color application to ASCII art text using the game's modern color template system
    /// Delegates to ColorTemplateLibrary for all color operations
    /// </summary>
    public static class TitleColorApplicator
    {
        /// <summary>
        /// Applies a template to text using the color template system
        /// </summary>
        /// <param name="text">Text to colorize</param>
        /// <param name="templateName">Template name (e.g., "golden", "title_fighter")</param>
        /// <returns>List of ColoredText segments</returns>
        public static List<ColoredText> ApplyTemplate(string text, string templateName)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new List<ColoredText>();
            }

            // Use the color template library directly
            return ColorTemplateLibrary.GetTemplate(templateName, text);
        }

        /// <summary>
        /// Applies a solid color to text using a single color code
        /// Uses the modern color system's color code conversion
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

            // Use ColorTemplateLibrary's public helper method
            var color = ColorTemplateLibrary.ColorCodeToColor(colorCode);
            return ColorTemplateLibrary.SingleColor(text, color);
        }

        /// <summary>
        /// Applies a transitioning color to text using multi-stage progression
        /// Creates smooth transitions by progressing through intermediate colors
        /// Uses the modern color system for all color conversions
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
            
            // Use ColorTemplateLibrary's public helper method
            var color = ColorTemplateLibrary.ColorCodeToColor(colorCode);
            return ColorTemplateLibrary.SingleColor(text, color);
        }

        /// <summary>
        /// Determines the appropriate color code for a given progress value
        /// Implements multi-stage transitions for smooth color progression
        /// </summary>
        private static string GetProgressiveColor(string sourceColor, string targetColor, float progress)
        {
            // Clamp progress to valid range
            progress = Math.Clamp(progress, 0.0f, 1.0f);

            // Define intermediate color stages based on target color
            // This creates smooth visual transitions that match the game's aesthetic
            
            // Transition to warm colors (W = gold/yellow)
            // Skip white - go directly from grey to yellow/orange
            if (targetColor == "W")
            {
                if (progress < 0.50f) return "y";  // Grey (0-50%)
                if (progress < 0.75f) return "W";  // Yellow (50-75%)
                if (progress < 0.90f) return "W";  // Yellow (75-90%)
                return "W";                         // Gold (90-100%)
            }
            
            // Transition to orange (O)
            // Skip white - go directly from grey/yellow to orange
            if (targetColor == "O")
            {
                if (progress < 0.40f) return "y";  // Grey (0-40%)
                if (progress < 0.60f) return "W";  // Yellow/Warm (40-60%)
                if (progress < 0.80f) return "o";  // Light orange (60-80%)
                return "O";                         // Full orange (80-100%)
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
    }
}

