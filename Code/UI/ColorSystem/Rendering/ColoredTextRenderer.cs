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
        /// Ensures proper spacing between segments - each segment gets a space after it (unless punctuation/newline)
        /// </summary>
        public static string RenderAsMarkup(IEnumerable<ColoredText> segments)
        {
            if (segments == null)
                return "";
            
            var segmentList = segments.ToList();
            if (segmentList.Count == 0)
                return "";
            
            var markup = new StringBuilder();
            Color? currentColor = null;
            
            for (int i = 0; i < segmentList.Count; i++)
            {
                var segment = segmentList[i];
                
                if (string.IsNullOrEmpty(segment.Text))
                    continue;
                
                // Only add color code if color changed
                if (!currentColor.HasValue || !ColorValidator.AreColorsEqual(currentColor.Value, segment.Color))
                {
                    // Add color code
                    char colorCode = GetColorCode(segment.Color);
                    markup.Append($"&{colorCode}");
                    currentColor = segment.Color;
                }
                
                // Add the segment text
                markup.Append(segment.Text);
                
                // Ensure space after segment (unless it's the last segment, ends with punctuation/newline, or next starts with punctuation/newline)
                // This ensures each item has a space after it leading to the next item
                if (i < segmentList.Count - 1)
                {
                    var nextSegment = segmentList[i + 1];
                    if (!string.IsNullOrEmpty(nextSegment.Text))
                    {
                        bool needsSpace = ShouldAddSpaceBetweenSegments(segment.Text, nextSegment.Text);
                        if (needsSpace)
                        {
                            // Add space - use current color to maintain color context
                            // The space will be part of the current color segment
                            markup.Append(" ");
                        }
                    }
                }
            }
            
            // Reset to white at the end
            if (currentColor.HasValue && !ColorValidator.AreColorsEqual(currentColor.Value, Colors.White))
            {
                markup.Append("&y");
            }
            
            return markup.ToString();
        }
        
        /// <summary>
        /// Determines if a space should be added between two text segments.
        /// Returns false for punctuation boundaries, newlines, or if either segment is empty.
        /// Also returns false for segments that are part of the same word (prevents spacing issues with multi-color templates).
        /// </summary>
        private static bool ShouldAddSpaceBetweenSegments(string currentText, string nextText)
        {
            if (string.IsNullOrEmpty(currentText) || string.IsNullOrEmpty(nextText))
                return false;
            
            // Don't add space if both segments are single characters that are part of the same word
            // This prevents spacing issues when multi-color templates split words into character-by-character segments
            if (currentText.Length == 1 && nextText.Length == 1)
            {
                char currentChar = currentText[0];
                char nextChar = nextText[0];
                
                // If both are letters or alphanumeric, they're part of the same word - don't add space
                if (char.IsLetterOrDigit(currentChar) && char.IsLetterOrDigit(nextChar))
                    return false;
            }
            
            // Don't add space if both segments contain only letters/digits (part of the same word)
            // This handles merged segments from multi-color templates (e.g., "ua" + "n" should not have space)
            // Check if current ends with a letter/digit and next starts with a letter/digit
            char currentLast = currentText[currentText.Length - 1];
            char nextFirst = nextText[0];
            
            // If both boundary characters are letters/digits and neither segment contains whitespace,
            // they're part of the same word - don't add space
            if (char.IsLetterOrDigit(currentLast) && char.IsLetterOrDigit(nextFirst))
            {
                // Check that neither segment contains whitespace (which would indicate word boundary)
                bool currentHasNoWhitespace = !currentText.Any(char.IsWhiteSpace);
                bool nextHasNoWhitespace = !nextText.Any(char.IsWhiteSpace);
                
                // If both segments have no whitespace, they're part of the same word
                if (currentHasNoWhitespace && nextHasNoWhitespace)
                    return false;
            }
            
            // Don't add space if current ends with punctuation that shouldn't have space after
            if (currentLast == '!' || currentLast == '?' || currentLast == '.' || currentLast == ',' || 
                currentLast == ':' || currentLast == ';' || currentLast == '\n' || currentLast == '\r')
                return false;
            
            // Don't add space if current ends with space (already has spacing)
            if (char.IsWhiteSpace(currentLast))
                return false;
            
            // Don't add space if next starts with punctuation that shouldn't have space before
            if (nextFirst == '!' || nextFirst == '?' || nextFirst == '.' || 
                nextFirst == ',' || nextFirst == ':' || nextFirst == ';' ||
                nextFirst == '\n' || nextFirst == '\r')
                return false;
            
            // Don't add space if next starts with space (already has spacing)
            if (char.IsWhiteSpace(nextFirst))
                return false;
            
            // Add space for all other cases
            return true;
        }
        
        
        /// <summary>
        /// Gets the old-style color code for a color
        /// Uses reverse mapping from LegacyColorConverter's color code system
        /// </summary>
        private static char GetColorCode(Color color)
        {
            // Use reverse mapping from LegacyColorConverter
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
            if (ColorValidator.AreColorsEqual(color, ColorPalette.Damage.GetColor()) || 
                ColorValidator.AreColorsEqual(color, ColorPalette.Error.GetColor()) ||
                ColorValidator.AreColorsEqual(color, ColorPalette.Red.GetColor()))
                return ColorPalette.Damage;
            
            if (ColorValidator.AreColorsEqual(color, ColorPalette.Success.GetColor()) ||
                ColorValidator.AreColorsEqual(color, ColorPalette.Green.GetColor()) ||
                ColorValidator.AreColorsEqual(color, ColorPalette.Healing.GetColor()))
                return ColorPalette.Success;
            
            if (ColorValidator.AreColorsEqual(color, ColorPalette.Info.GetColor()) ||
                ColorValidator.AreColorsEqual(color, ColorPalette.Cyan.GetColor()) ||
                ColorValidator.AreColorsEqual(color, ColorPalette.Blue.GetColor()))
                return ColorPalette.Info;
            
            if (ColorValidator.AreColorsEqual(color, ColorPalette.Warning.GetColor()) ||
                ColorValidator.AreColorsEqual(color, ColorPalette.Yellow.GetColor()) ||
                ColorValidator.AreColorsEqual(color, ColorPalette.Orange.GetColor()))
                return ColorPalette.Warning;
            
            if (ColorValidator.AreColorsEqual(color, ColorPalette.Gold.GetColor()))
                return ColorPalette.Gold;
            
            if (ColorValidator.AreColorsEqual(color, ColorPalette.Gray.GetColor()) ||
                ColorValidator.AreColorsEqual(color, ColorPalette.Brown.GetColor()))
                return ColorPalette.Gray;
            
            // Default to white
            return ColorPalette.White;
        }
        
        /// <summary>
        /// Gets the old-style color code for a ColorPalette
        /// </summary>
        private static char GetColorCodeForPalette(ColorPalette palette)
        {
            // Reverse mapping from LegacyColorConverter.ConvertOldColorCode
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
