using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Media;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// Renders colored text to different output formats
    /// </summary>
    public static class ColoredTextRenderer
    {
        /// <summary>
        /// Renders colored text as plain text (strips all color information)
        /// </summary>
        public static string RenderAsPlainText(IEnumerable<ColoredText> segments)
        {
            if (segments == null)
                return "";
                
            return string.Join("", segments.Select(s => s.Text));
        }
        
        /// <summary>
        /// Renders colored text as markup string (old-style &X format) for backward compatibility
        /// This allows ColoredText to be stored in string buffers and parsed back
        /// </summary>
        public static string RenderAsMarkup(IEnumerable<ColoredText> segments)
        {
            if (segments == null)
                return "";
            
            var markup = new StringBuilder();
            Color? currentColor = null;
            
            foreach (var segment in segments)
            {
                if (string.IsNullOrEmpty(segment.Text))
                    continue;
                
                // Only add color code if color changed
                if (!currentColor.HasValue || !AreColorsEqual(currentColor.Value, segment.Color))
                {
                    // Add color code
                    char colorCode = GetColorCode(segment.Color);
                    markup.Append($"&{colorCode}");
                    currentColor = segment.Color;
                }
                
                markup.Append(segment.Text);
            }
            
            // Reset to white at the end
            if (currentColor.HasValue && !AreColorsEqual(currentColor.Value, Colors.White))
            {
                markup.Append("&y");
            }
            
            return markup.ToString();
        }
        
        /// <summary>
        /// Checks if two colors are equal
        /// </summary>
        private static bool AreColorsEqual(Color color1, Color color2)
        {
            return color1.R == color2.R && color1.G == color2.G && color1.B == color2.B && color1.A == color2.A;
        }
        
        /// <summary>
        /// Gets the old-style color code for a color
        /// Uses reverse mapping from CompatibilityLayer's color code system
        /// </summary>
        private static char GetColorCode(Color color)
        {
            // Use reverse mapping from CompatibilityLayer
            // Try to match by RGB values to ColorPalette colors
            var colorPalette = GetColorPaletteForColor(color);
            return GetColorCodeForPalette(colorPalette);
        }
        
        /// <summary>
        /// Gets the ColorPalette enum value that best matches a color
        /// </summary>
        private static ColorPalette GetColorPaletteForColor(Color color)
        {
            // Try to match by RGB values - this is approximate
            // Check common ColorPalette colors
            if (AreColorsEqual(color, ColorPalette.Damage.GetColor()) || 
                AreColorsEqual(color, ColorPalette.Error.GetColor()) ||
                AreColorsEqual(color, ColorPalette.Red.GetColor()))
                return ColorPalette.Damage;
            
            if (AreColorsEqual(color, ColorPalette.Success.GetColor()) ||
                AreColorsEqual(color, ColorPalette.Green.GetColor()) ||
                AreColorsEqual(color, ColorPalette.Healing.GetColor()))
                return ColorPalette.Success;
            
            if (AreColorsEqual(color, ColorPalette.Info.GetColor()) ||
                AreColorsEqual(color, ColorPalette.Cyan.GetColor()) ||
                AreColorsEqual(color, ColorPalette.Blue.GetColor()))
                return ColorPalette.Info;
            
            if (AreColorsEqual(color, ColorPalette.Warning.GetColor()) ||
                AreColorsEqual(color, ColorPalette.Yellow.GetColor()) ||
                AreColorsEqual(color, ColorPalette.Orange.GetColor()))
                return ColorPalette.Warning;
            
            if (AreColorsEqual(color, ColorPalette.Gold.GetColor()))
                return ColorPalette.Gold;
            
            if (AreColorsEqual(color, ColorPalette.Gray.GetColor()) ||
                AreColorsEqual(color, ColorPalette.Brown.GetColor()))
                return ColorPalette.Gray;
            
            // Default to white
            return ColorPalette.White;
        }
        
        /// <summary>
        /// Gets the old-style color code for a ColorPalette
        /// </summary>
        private static char GetColorCodeForPalette(ColorPalette palette)
        {
            // Reverse mapping from CompatibilityLayer.ConvertOldColorCode
            return palette switch
            {
                ColorPalette.Damage or ColorPalette.Error or ColorPalette.Red => 'R',
                ColorPalette.DarkRed => 'r',
                ColorPalette.Success or ColorPalette.Green or ColorPalette.Healing => 'G',
                ColorPalette.DarkGreen => 'g',
                ColorPalette.Info or ColorPalette.Cyan or ColorPalette.Blue => 'B',
                ColorPalette.DarkBlue => 'b',
                ColorPalette.Warning or ColorPalette.Yellow => 'Y',
                ColorPalette.Orange => 'O',
                ColorPalette.Gold => 'W',
                ColorPalette.Brown => 'w',
                ColorPalette.Gray => 'y',
                ColorPalette.DarkGray => 'k',
                ColorPalette.Magenta => 'M',
                ColorPalette.DarkMagenta => 'm',
                ColorPalette.DarkCyan => 'c',
                ColorPalette.Black => 'K',
                _ => 'y' // Default to white/gray
            };
        }
        
        /// <summary>
        /// Renders colored text as HTML
        /// </summary>
        public static string RenderAsHtml(IEnumerable<ColoredText> segments)
        {
            if (segments == null)
                return "";
                
            var html = new StringBuilder();
            
            foreach (var segment in segments)
            {
                if (string.IsNullOrEmpty(segment.Text))
                    continue;
                    
                var colorHex = ColorToHex(segment.Color);
                var escapedText = EscapeHtml(segment.Text);
                
                html.Append($"<span style=\"color: {colorHex}\">{escapedText}</span>");
            }
            
            return html.ToString();
        }
        
        /// <summary>
        /// Renders colored text as ANSI escape codes (for console output)
        /// </summary>
        public static string RenderAsAnsi(IEnumerable<ColoredText> segments)
        {
            if (segments == null)
                return "";
                
            var ansi = new StringBuilder();
            
            foreach (var segment in segments)
            {
                if (string.IsNullOrEmpty(segment.Text))
                    continue;
                    
                var ansiCode = ColorToAnsi(segment.Color);
                var escapedText = EscapeAnsi(segment.Text);
                
                ansi.Append($"\x1b[{ansiCode}m{escapedText}\x1b[0m");
            }
            
            return ansi.ToString();
        }
        
        /// <summary>
        /// Renders colored text as a simple format string (for debugging)
        /// </summary>
        public static string RenderAsDebug(IEnumerable<ColoredText> segments)
        {
            if (segments == null)
                return "";
                
            var debug = new StringBuilder();
            
            foreach (var segment in segments)
            {
                if (string.IsNullOrEmpty(segment.Text))
                    continue;
                    
                var colorName = GetColorName(segment.Color);
                debug.Append($"[{colorName}]{segment.Text}[/{colorName}]");
            }
            
            return debug.ToString();
        }
        
        /// <summary>
        /// Gets the display length of colored text (ignoring color markup)
        /// </summary>
        public static int GetDisplayLength(IEnumerable<ColoredText> segments)
        {
            if (segments == null)
                return 0;
                
            return segments.Sum(s => s.Text.Length);
        }
        
        /// <summary>
        /// Truncates colored text to a maximum length
        /// </summary>
        public static List<ColoredText> Truncate(IEnumerable<ColoredText> segments, int maxLength)
        {
            if (segments == null)
                return new List<ColoredText>();
                
            var result = new List<ColoredText>();
            var currentLength = 0;
            
            foreach (var segment in segments)
            {
                if (currentLength >= maxLength)
                    break;
                    
                var remainingLength = maxLength - currentLength;
                var segmentLength = segment.Text.Length;
                
                if (segmentLength <= remainingLength)
                {
                    result.Add(segment);
                    currentLength += segmentLength;
                }
                else
                {
                    // Truncate this segment
                    var truncatedText = segment.Text.Substring(0, remainingLength);
                    result.Add(new ColoredText(truncatedText, segment.Color));
                    break;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Pads colored text to a minimum length
        /// </summary>
        public static List<ColoredText> PadRight(IEnumerable<ColoredText> segments, int minLength, char paddingChar = ' ')
        {
            if (segments == null)
                return new List<ColoredText>();
                
            var result = new List<ColoredText>(segments);
            var currentLength = GetDisplayLength(result);
            
            if (currentLength < minLength)
            {
                var paddingLength = minLength - currentLength;
                var padding = new string(paddingChar, paddingLength);
                result.Add(new ColoredText(padding, Colors.White));
            }
            
            return result;
        }
        
        /// <summary>
        /// Centers colored text within a given width
        /// </summary>
        public static List<ColoredText> Center(IEnumerable<ColoredText> segments, int width, char paddingChar = ' ')
        {
            if (segments == null)
                return new List<ColoredText>();
                
            var result = new List<ColoredText>();
            var currentLength = GetDisplayLength(segments);
            
            if (currentLength >= width)
            {
                // Text is already wider than target width
                return new List<ColoredText>(segments);
            }
            
            var paddingLength = (width - currentLength) / 2;
            var leftPadding = new string(paddingChar, paddingLength);
            var rightPadding = new string(paddingChar, width - currentLength - paddingLength);
            
            result.Add(new ColoredText(leftPadding, Colors.White));
            result.AddRange(segments);
            result.Add(new ColoredText(rightPadding, Colors.White));
            
            return result;
        }
        
        private static string ColorToHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
        
        private static string ColorToAnsi(Color color)
        {
            // Simple ANSI color mapping
            if (color == Colors.Red) return "31";
            if (color == Colors.Green) return "32";
            if (color == Colors.Blue) return "34";
            if (color == Colors.Yellow) return "33";
            if (color == Colors.Cyan) return "36";
            if (color == Colors.Magenta) return "35";
            if (color == Colors.White) return "37";
            if (color == Colors.Black) return "30";
            if (color == Colors.Gray) return "90";
            
            // Default to white
            return "37";
        }
        
        private static string EscapeHtml(string text)
        {
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }
        
        private static string EscapeAnsi(string text)
        {
            // ANSI doesn't need much escaping, but we'll handle basic cases
            return text.Replace("\x1b", "\\x1b");
        }
        
        private static string GetColorName(Color color)
        {
            // Simple color name mapping for debug output
            if (color == Colors.Red) return "Red";
            if (color == Colors.Green) return "Green";
            if (color == Colors.Blue) return "Blue";
            if (color == Colors.Yellow) return "Yellow";
            if (color == Colors.Cyan) return "Cyan";
            if (color == Colors.Magenta) return "Magenta";
            if (color == Colors.White) return "White";
            if (color == Colors.Black) return "Black";
            if (color == Colors.Gray) return "Gray";
            
            return $"RGB({color.R},{color.G},{color.B})";
        }
    }
}
