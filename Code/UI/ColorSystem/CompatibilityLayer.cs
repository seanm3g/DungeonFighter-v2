using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// Compatibility layer to bridge old and new color systems during migration
    /// This allows gradual migration without breaking existing code
    /// </summary>
    public static class CompatibilityLayer
    {
        /// <summary>
        /// Converts old color markup to new ColoredText system
        /// </summary>
        public static List<ColoredText> ConvertOldMarkup(string oldMarkup)
        {
            if (string.IsNullOrEmpty(oldMarkup))
                return new List<ColoredText>();
            
            // Simple conversion from old color codes to new system
            var result = new List<ColoredText>();
            var currentText = "";
            var currentColor = Colors.White;
            
            for (int i = 0; i < oldMarkup.Length; i++)
            {
                if (oldMarkup[i] == '&' && i + 1 < oldMarkup.Length)
                {
                    // Found a color code
                    if (currentText.Length > 0)
                    {
                        result.Add(new ColoredText(currentText, currentColor));
                        currentText = "";
                    }
                    
                    var colorCode = oldMarkup[i + 1];
                    currentColor = ConvertOldColorCode(colorCode);
                    i++; // Skip the color code character
                }
                else
                {
                    currentText += oldMarkup[i];
                }
            }
            
            // Add remaining text
            if (currentText.Length > 0)
            {
                result.Add(new ColoredText(currentText, currentColor));
            }
            
            return result;
        }
        
        /// <summary>
        /// Converts old color codes to new colors
        /// </summary>
        private static Color ConvertOldColorCode(char colorCode)
        {
            return colorCode switch
            {
                'R' => ColorPalette.Damage.GetColor(),
                'r' => ColorPalette.DarkRed.GetColor(),
                'G' => ColorPalette.Success.GetColor(),
                'g' => ColorPalette.DarkGreen.GetColor(),
                'B' => ColorPalette.Info.GetColor(),
                'b' => ColorPalette.DarkBlue.GetColor(),
                'Y' => ColorPalette.Warning.GetColor(),
                'C' => ColorPalette.Cyan.GetColor(),
                'c' => ColorPalette.DarkCyan.GetColor(),
                'M' => ColorPalette.Magenta.GetColor(),
                'm' => ColorPalette.DarkMagenta.GetColor(),
                'W' => ColorPalette.White.GetColor(),
                'w' => ColorPalette.Brown.GetColor(),
                'O' => ColorPalette.Orange.GetColor(),
                'o' => ColorPalette.Orange.GetColor(),
                'K' => ColorPalette.Black.GetColor(),
                'k' => ColorPalette.DarkGray.GetColor(),
                'y' => ColorPalette.White.GetColor(), // Reset to white
                _ => ColorPalette.White.GetColor()
            };
        }
        
        /// <summary>
        /// Creates a simple colored text from old-style parameters
        /// </summary>
        public static List<ColoredText> CreateSimpleColoredText(string text, string? colorPattern = null)
        {
            if (string.IsNullOrEmpty(text))
                return new List<ColoredText>();
            
            var color = string.IsNullOrEmpty(colorPattern) 
                ? Colors.White 
                : ColorPatterns.GetColorForPattern(colorPattern);
            
            return new List<ColoredText> { new ColoredText(text, color) };
        }
        
        /// <summary>
        /// Legacy method to parse color markup (redirects to new system)
        /// </summary>
        public static List<ColoredText> ParseColorMarkup(string markup)
        {
            return ColoredTextParser.Parse(markup);
        }
        
        /// <summary>
        /// Legacy method to check if text has color markup
        /// </summary>
        public static bool HasColorMarkup(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
            
            // Check for old-style color codes (both uppercase and lowercase)
            if (text.Contains("&R") || text.Contains("&G") || text.Contains("&B") || 
                text.Contains("&Y") || text.Contains("&C") || text.Contains("&M") || 
                text.Contains("&W") || text.Contains("&K") || text.Contains("&y") ||
                text.Contains("&r") || text.Contains("&g") || text.Contains("&b") || 
                text.Contains("&o") || text.Contains("&O") || text.Contains("&w") ||
                text.Contains("&c") || text.Contains("&m") || text.Contains("&k"))
                return true;
            
            // Check for new-style markup
            if (text.Contains("[") && text.Contains("]"))
                return true;
            
            return false;
        }
    }
    
    /// <summary>
    /// Legacy ColorDefinitions class for compatibility
    /// This provides the old interface while using the new system internally
    /// </summary>
    public static class ColorDefinitions
    {
        /// <summary>
        /// Legacy ColoredSegment class
        /// </summary>
        public class ColoredSegment
        {
            public string Text { get; set; } = "";
            public Color? Foreground { get; set; }
            public Color? Background { get; set; }
            
            public ColoredSegment(string text, Color? foreground = null, Color? background = null)
            {
                Text = text;
                Foreground = foreground;
                Background = background;
            }
            
            /// <summary>
            /// Converts to new ColoredText format
            /// </summary>
            public ColoredText ToColoredText()
            {
                var color = Foreground ?? Colors.White;
                return new ColoredText(Text, color);
            }
        }
        
        /// <summary>
        /// Legacy color definitions
        /// </summary>
        public static class Colors
        {
            public static Color Red => ColorPalette.Red.GetColor();
            public static Color Green => ColorPalette.Green.GetColor();
            public static Color Blue => ColorPalette.Blue.GetColor();
            public static Color Yellow => ColorPalette.Yellow.GetColor();
            public static Color Cyan => ColorPalette.Cyan.GetColor();
            public static Color Magenta => ColorPalette.Magenta.GetColor();
            public static Color White => ColorPalette.White.GetColor();
            public static Color Black => ColorPalette.Black.GetColor();
            public static Color Gray => ColorPalette.Gray.GetColor();
            public static Color DarkRed => ColorPalette.DarkRed.GetColor();
            public static Color DarkGreen => ColorPalette.DarkGreen.GetColor();
            public static Color DarkBlue => ColorPalette.DarkBlue.GetColor();
            public static Color DarkYellow => ColorPalette.DarkYellow.GetColor();
            public static Color DarkCyan => ColorPalette.DarkCyan.GetColor();
            public static Color DarkMagenta => ColorPalette.DarkMagenta.GetColor();
            public static Color DarkGray => ColorPalette.DarkGray.GetColor();
            public static Color LightGray => ColorPalette.LightGray.GetColor();
            public static Color Orange => ColorPalette.Orange.GetColor();
            public static Color Purple => ColorPalette.Purple.GetColor();
            public static Color Pink => ColorPalette.Pink.GetColor();
            public static Color Brown => ColorPalette.Brown.GetColor();
            public static Color Lime => ColorPalette.Lime.GetColor();
            public static Color Navy => ColorPalette.Navy.GetColor();
            public static Color Teal => ColorPalette.Teal.GetColor();
            public static Color Gold => ColorPalette.Gold.GetColor();
            public static Color Silver => ColorPalette.Silver.GetColor();
            public static Color Bronze => ColorPalette.Bronze.GetColor();
        }
        
        /// <summary>
        /// Default text color
        /// </summary>
        public static Color DefaultTextColor => Colors.White;
        
        /// <summary>
        /// Gets a color by character code
        /// </summary>
        public static Color? GetColor(char colorCode)
        {
            return colorCode switch
            {
                'R' => Colors.Red,
                'G' => Colors.Green,
                'B' => Colors.Blue,
                'Y' => Colors.Yellow,
                'C' => Colors.Cyan,
                'M' => Colors.Magenta,
                'W' => Colors.White,
                'O' => Colors.Orange,
                'K' => Colors.Black,
                'r' => Colors.DarkRed,
                'g' => Colors.DarkGreen,
                'b' => Colors.DarkBlue,
                'y' => Colors.DarkYellow,
                'c' => Colors.DarkCyan,
                'm' => Colors.DarkMagenta,
                'w' => Colors.LightGray,
                'k' => Colors.DarkGray,
                _ => null
            };
        }
        
        /// <summary>
        /// Legacy ColorRGB struct for compatibility
        /// </summary>
        public struct ColorRGB
        {
            public byte R { get; set; }
            public byte G { get; set; }
            public byte B { get; set; }
            public byte A { get; set; }

            public ColorRGB(byte r, byte g, byte b, byte a = 255)
            {
                R = r;
                G = g;
                B = b;
                A = a;
            }

            // Implicit conversion to Avalonia.Media.Color
            public static implicit operator Color(ColorRGB rgb)
            {
                return Color.FromArgb(rgb.A, rgb.R, rgb.G, rgb.B);
            }

            // Explicit conversion from Avalonia.Media.Color
            public static explicit operator ColorRGB(Color color)
            {
                return new ColorRGB(color.R, color.G, color.B, color.A);
            }
        }
    }
    
    /// <summary>
    /// Legacy ColorParser class for compatibility
    /// </summary>
    public static class ColorParser
    {
        /// <summary>
        /// Legacy parse method - redirects to new system
        /// </summary>
        public static List<ColorDefinitions.ColoredSegment> Parse(string text)
        {
            var coloredTexts = ColoredTextParser.Parse(text);
            return coloredTexts.Select(ct => new ColorDefinitions.ColoredSegment(ct.Text, ct.Color)).ToList();
        }
        
        /// <summary>
        /// Legacy method to check for color markup
        /// </summary>
        public static bool HasColorMarkup(string text)
        {
            return CompatibilityLayer.HasColorMarkup(text);
        }
        
        /// <summary>
        /// Legacy method to parse old-style color codes
        /// </summary>
        public static List<ColorDefinitions.ColoredSegment> ParseOldStyle(string text)
        {
            var coloredTexts = CompatibilityLayer.ConvertOldMarkup(text);
            return coloredTexts.Select(ct => new ColorDefinitions.ColoredSegment(ct.Text, ct.Color)).ToList();
        }
        
        /// <summary>
        /// Gets the display length of text (without color markup)
        /// </summary>
        public static int GetDisplayLength(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;
                
            var parsed = Parse(text);
            return parsed.Sum(s => s.Text.Length);
        }
        
        /// <summary>
        /// Strips color markup from text
        /// </summary>
        public static string StripColorMarkup(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
                
            var parsed = Parse(text);
            return string.Join("", parsed.Select(s => s.Text));
        }
        
        /// <summary>
        /// Colorizes text using keyword coloring
        /// </summary>
        public static List<ColorDefinitions.ColoredSegment> Colorize(string text)
        {
            var coloredTexts = KeywordColorSystem.ColorText(text);
            return coloredTexts.Select(ct => new ColorDefinitions.ColoredSegment(ct.Text, ct.Color)).ToList();
        }
    }
}
