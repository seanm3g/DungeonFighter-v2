using System;
using Avalonia.Media;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// Static utility methods for Color type conversions.
    /// Provides extension methods and helpers for converting between different color types.
    /// </summary>
    public static class ColorUtils
    {
        /// <summary>
        /// Converts a System.Drawing.Color to Avalonia.Media.Color.
        /// </summary>
        public static Color ToAvaloniaColor(this System.Drawing.Color color)
        {
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }
        
        /// <summary>
        /// Converts an Avalonia.Media.Color to System.Drawing.Color.
        /// </summary>
        public static System.Drawing.Color ToSystemColor(this Color color)
        {
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        }
        
        /// <summary>
        /// Converts an Avalonia.Media.Color to itself (no-op for compatibility).
        /// </summary>
        public static Color ToAvaloniaColor(this Color color)
        {
            return color;
        }
        
        /// <summary>
        /// Creates an Avalonia.Media.Color from RGB values.
        /// </summary>
        public static Color ToAvaloniaColor(byte r, byte g, byte b, byte a = 255)
        {
            return Color.FromArgb(a, r, g, b);
        }
        
        /// <summary>
        /// Creates an Avalonia.Media.Color from a hex string.
        /// Supports formats: #RRGGBB or #AARRGGBB or RRGGBB or AARRGGBB.
        /// </summary>
        public static Color ToAvaloniaColor(this string hexColor)
        {
            if (string.IsNullOrEmpty(hexColor))
                return Colors.White;
            
            // Remove # if present
            if (hexColor.StartsWith("#"))
                hexColor = hexColor.Substring(1);
            
            // Parse hex color
            if (hexColor.Length == 6)
            {
                var r = Convert.ToByte(hexColor.Substring(0, 2), 16);
                var g = Convert.ToByte(hexColor.Substring(2, 2), 16);
                var b = Convert.ToByte(hexColor.Substring(4, 2), 16);
                return Color.FromArgb(255, r, g, b);
            }
            else if (hexColor.Length == 8)
            {
                var a = Convert.ToByte(hexColor.Substring(0, 2), 16);
                var r = Convert.ToByte(hexColor.Substring(2, 2), 16);
                var g = Convert.ToByte(hexColor.Substring(4, 2), 16);
                var b = Convert.ToByte(hexColor.Substring(6, 2), 16);
                return Color.FromArgb(a, r, g, b);
            }
            
            return Colors.White;
        }
    }
}

