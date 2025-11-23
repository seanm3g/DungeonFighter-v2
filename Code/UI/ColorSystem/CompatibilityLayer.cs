using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
            
            // Merge adjacent segments with the same color to prevent extra spacing
            return MergeAdjacentSegments(result);
        }
        
        /// <summary>
        /// Merges adjacent segments with the same color to prevent extra spacing issues
        /// Normalizes spaces to ensure only single spaces between segments
        /// </summary>
        private static List<ColoredText> MergeAdjacentSegments(List<ColoredText> segments)
        {
            if (segments == null || segments.Count <= 1)
                return segments ?? new List<ColoredText>();
            
            var merged = new List<ColoredText>();
            ColoredText? currentSegment = null;
            
            foreach (var segment in segments)
            {
                // Skip empty segments
                if (string.IsNullOrEmpty(segment.Text))
                    continue;
                
                if (currentSegment == null)
                {
                    // First segment
                    currentSegment = new ColoredText(segment.Text, segment.Color);
                }
                else if (AreColorsEqual(currentSegment.Color, segment.Color))
                {
                    // Same color - merge with current segment
                    // Normalize spaces: if current ends with space and segment starts with space, use only one
                    string currentText = currentSegment.Text;
                    string segmentText = segment.Text;
                    
                    // Check if we need to normalize spaces at the boundary
                    if (currentText.EndsWith(" ") && segmentText.StartsWith(" "))
                    {
                        // Remove one space to avoid double spacing
                        currentText = currentText.TrimEnd() + " " + segmentText.TrimStart();
                    }
                    else
                    {
                        currentText = currentText + segmentText;
                    }
                    
                    currentSegment = new ColoredText(currentText, currentSegment.Color);
                }
                else
                {
                    // Different color - add current segment and start new one
                    merged.Add(currentSegment);
                    currentSegment = new ColoredText(segment.Text, segment.Color);
                }
            }
            
            // Add the last segment
            if (currentSegment != null)
            {
                merged.Add(currentSegment);
            }
            
            // Normalize spaces between adjacent segments of different colors FIRST
            // This prevents double spaces from being created when segments are merged
            for (int i = 0; i < merged.Count - 1; i++)
            {
                var current = merged[i];
                var next = merged[i + 1];
                
                // If both segments are just spaces, merge them into one
                if (current.Text.Trim().Length == 0 && next.Text.Trim().Length == 0)
                {
                    // Both are space-only segments - merge into one white space segment
                    merged[i] = new ColoredText(" ", Colors.White);
                    merged.RemoveAt(i + 1);
                    i--; // Adjust index after removal
                    continue;
                }
                
                // If current ends with space and next starts with space, remove one
                if (current.Text.EndsWith(" ") && next.Text.StartsWith(" "))
                {
                    // Remove trailing space from current segment
                    var trimmedCurrent = current.Text.TrimEnd();
                    if (trimmedCurrent.Length > 0)
                    {
                        merged[i] = new ColoredText(trimmedCurrent, current.Color);
                    }
                    else
                    {
                        // If current becomes empty after trimming, check if we can merge with next
                        // If next is also a space segment, merge them
                        if (next.Text.Trim().Length == 0)
                        {
                            merged[i] = new ColoredText(" ", Colors.White);
                            merged.RemoveAt(i + 1);
                            i--; // Adjust index after removal
                        }
                        else
                        {
                            // Current is empty space, next is not - remove current
                            merged.RemoveAt(i);
                            i--; // Adjust index after removal
                        }
                        continue;
                    }
                }
            }
            
            // Final pass: normalize any remaining double spaces within segments
            for (int i = 0; i < merged.Count; i++)
            {
                var segment = merged[i];
                // Replace multiple consecutive spaces with single space
                var normalizedText = Regex.Replace(segment.Text, @"\s+", " ");
                if (normalizedText != segment.Text)
                {
                    merged[i] = new ColoredText(normalizedText, segment.Color);
                }
            }
            
            // Final pass: remove any empty segments that might have been created
            merged.RemoveAll(s => string.IsNullOrEmpty(s.Text));
            
            return merged;
        }
        
        /// <summary>
        /// Checks if two colors are equal (comparing RGB values)
        /// </summary>
        private static bool AreColorsEqual(Color color1, Color color2)
        {
            return color1.R == color2.R && 
                   color1.G == color2.G && 
                   color1.B == color2.B && 
                   color1.A == color2.A;
        }
        
        /// <summary>
        /// Converts old color codes to new colors
        /// </summary>
        private static Color ConvertOldColorCode(char colorCode)
        {
            Color color = colorCode switch
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
                'W' => ColorPalette.Gold.GetColor(), // Gold/yellow per template rules
                'w' => ColorPalette.Brown.GetColor(),
                'O' => ColorPalette.Orange.GetColor(),
                'o' => ColorPalette.Orange.GetColor(),
                'K' => ColorPalette.Black.GetColor(), // Will be lightened by ColorValidator
                'k' => ColorPalette.DarkGray.GetColor(),
                'y' => ColorPalette.Gray.GetColor(), // Grey per template rules
                _ => ColorPalette.White.GetColor()
            };
            
            // Ensure color is visible on black background
            return ColorValidator.EnsureVisible(color);
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
            
            // Check for template syntax: {{template|text}}
            if (text.Contains("{{") && text.Contains("}}"))
                return true;
            
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
                'W' => Colors.Gold, // Gold/yellow per template rules
                'O' => Colors.Orange,
                'K' => Colors.Black,
                'r' => Colors.DarkRed,
                'g' => Colors.DarkGreen,
                'b' => Colors.DarkBlue,
                'y' => Colors.Gray, // Grey per template rules
                'c' => Colors.DarkCyan,
                'm' => Colors.DarkMagenta,
                'w' => Colors.Brown,
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
