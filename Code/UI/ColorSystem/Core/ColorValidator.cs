using System;
using Avalonia.Media;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// Utility class for validating and ensuring colors are visible on black background.
    /// Also provides color comparison utilities.
    /// </summary>
    public static class ColorValidator
    {
        /// <summary>
        /// Minimum brightness threshold (0-255) to ensure visibility on black background.
        /// Colors below this threshold will be lightened.
        /// </summary>
        private const int MIN_BRIGHTNESS = 50;
        
        /// <summary>
        /// Checks if two colors are equal by comparing RGB and alpha values.
        /// Centralized utility method to avoid duplication across color system files.
        /// </summary>
        public static bool AreColorsEqual(Color a, Color b)
        {
            return a.R == b.R && a.G == b.G && a.B == b.B && a.A == b.A;
        }
        
        /// <summary>
        /// Calculates the perceived brightness (luminance) of a color.
        /// Uses the standard formula: 0.299*R + 0.587*G + 0.114*B
        /// </summary>
        public static double GetBrightness(Color color)
        {
            return 0.299 * color.R + 0.587 * color.G + 0.114 * color.B;
        }
        
        /// <summary>
        /// Checks if a color is too dark to be visible on a black background.
        /// </summary>
        public static bool IsTooDark(Color color)
        {
            return GetBrightness(color) < MIN_BRIGHTNESS;
        }
        
        /// <summary>
        /// Ensures a color is visible on black background by lightening it if necessary.
        /// Preserves the color's hue while increasing brightness.
        /// </summary>
        public static Color EnsureVisible(Color color)
        {
            if (!IsTooDark(color))
                return color;
            
            // Calculate current brightness
            double brightness = GetBrightness(color);
            
            // Lighten the color while preserving hue
            // Use a blend with white to lighten while maintaining color character
            double blendFactor = 0.5; // Blend 50% with white for very dark colors
            
            byte newR = (byte)Math.Min(255, color.R + (255 - color.R) * blendFactor);
            byte newG = (byte)Math.Min(255, color.G + (255 - color.G) * blendFactor);
            byte newB = (byte)Math.Min(255, color.B + (255 - color.B) * blendFactor);
            
            // If still too dark, use a minimum brightness approach
            Color lightened = Color.FromRgb(newR, newG, newB);
            if (IsTooDark(lightened))
            {
                // For extremely dark colors, use a gray that's definitely visible
                // But try to preserve some color character
                int minComponent = Math.Max(newR, Math.Max(newG, newB));
                if (minComponent < MIN_BRIGHTNESS)
                {
                    // Scale up the brightest component to at least MIN_BRIGHTNESS
                    double scale = MIN_BRIGHTNESS / (double)minComponent;
                    newR = (byte)Math.Min(255, (int)(color.R * scale));
                    newG = (byte)Math.Min(255, (int)(color.G * scale));
                    newB = (byte)Math.Min(255, (int)(color.B * scale));
                }
            }
            
            return Color.FromRgb(newR, newG, newB);
        }
    }
}

