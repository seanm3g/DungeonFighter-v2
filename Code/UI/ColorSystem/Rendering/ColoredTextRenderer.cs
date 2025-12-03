using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Media;
using RPGGame.UI;

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
        /// Renders colored text as template syntax markup string ({{pattern|text}} format)
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
            
            // Check if all segments form a single continuous word (no spaces between them)
            // This is important for multi-color templates like room names where each character
            // or character group is a separate segment but they form one word
            bool isSingleWord = IsSingleWord(segmentList);
            
            var markup = new StringBuilder();
            Color? currentColor = null;
            
            for (int i = 0; i < segmentList.Count; i++)
            {
                var segment = segmentList[i];
                
                if (string.IsNullOrEmpty(segment.Text))
                    continue;
                
                // Only add template markup if color changed or is not white
                bool needsColor = !currentColor.HasValue || !ColorValidator.AreColorsEqual(currentColor.Value, segment.Color);
                bool isWhite = ColorValidator.AreColorsEqual(segment.Color, Colors.White);
                
                if (needsColor && !isWhite)
                {
                    // Get pattern name for this color
                    string pattern = GetPatternForColor(segment.Color);
                    markup.Append($"{{{{{pattern}|");
                    currentColor = segment.Color;
                }
                
                // Add the segment text
                markup.Append(segment.Text);
                
                // Close template if we opened one
                if (needsColor && !isWhite)
                {
                    markup.Append("}}");
                }
                
                // Ensure space after segment (unless it's the last segment, ends with punctuation/newline, or next starts with punctuation/newline)
                // This ensures each item has a space after it leading to the next item
                // BUT: skip spacing if all segments form a single word (prevents spacing in multi-color templates)
                // IMPORTANT: Don't add space if current segment IS whitespace or already ends with whitespace
                bool currentIsWhitespace = segment.Text.Trim().Length == 0 && segment.Text.Length > 0;
                bool currentEndsWithSpace = segment.Text.Length > 0 && char.IsWhiteSpace(segment.Text[segment.Text.Length - 1]);
                
                if (i < segmentList.Count - 1 && !isSingleWord && !currentIsWhitespace && !currentEndsWithSpace)
                {
                    var nextSegment = segmentList[i + 1];
                    if (!string.IsNullOrEmpty(nextSegment.Text))
                    {
                        // Also check if next segment starts with whitespace
                        bool nextStartsWithSpace = char.IsWhiteSpace(nextSegment.Text[0]);
                        
                        // Only add space if neither segment has whitespace at the boundary
                        if (!nextStartsWithSpace)
                        {
                            // Use centralized spacing manager with word boundary detection for multi-color templates
                            bool needsSpace = CombatLogSpacingManager.ShouldAddSpaceBetween(segment.Text, nextSegment.Text, checkWordBoundary: true);
                            if (needsSpace)
                            {
                                // Add space as plain text (white)
                                markup.Append(CombatLogSpacingManager.SingleSpace);
                            }
                        }
                    }
                }
            }
            
            return markup.ToString();
        }
        
        /// <summary>
        /// Checks if all segments form a single continuous word (no spaces between segments).
        /// This is used to prevent spacing issues in multi-color templates like room names.
        /// </summary>
        private static bool IsSingleWord(List<ColoredText> segments)
        {
            if (segments == null || segments.Count <= 1)
                return false;
            
            // Check if all non-empty segments are letters/digits with no whitespace
            // and adjacent segments have letter/digit boundaries (forming a continuous word)
            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                if (string.IsNullOrEmpty(segment.Text))
                    continue;
                
                // If any segment contains whitespace, it's not a single word
                if (segment.Text.Any(char.IsWhiteSpace))
                    return false;
                
                // Check boundary with next segment
                if (i < segments.Count - 1)
                {
                    var nextSegment = segments[i + 1];
                    if (!string.IsNullOrEmpty(nextSegment.Text))
                    {
                        char lastChar = segment.Text[segment.Text.Length - 1];
                        char firstChar = nextSegment.Text[0];
                        
                        // If both boundary characters are letters/digits, they're part of the same word
                        // If either is not a letter/digit, there might be a word boundary
                        if (!char.IsLetterOrDigit(lastChar) || !char.IsLetterOrDigit(firstChar))
                        {
                            // Check if it's punctuation that's part of the word (like apostrophe or hyphen)
                            if (lastChar != '\'' && lastChar != '-' && firstChar != '\'' && firstChar != '-')
                                return false;
                        }
                    }
                }
            }
            
            // All segments form a continuous word
            return true;
        }
        
        // Spacing logic has been moved to CombatLogSpacingManager for centralized management.
        // Use CombatLogSpacingManager.ShouldAddSpaceBetween() with checkWordBoundary: true
        // for renderer-specific spacing that handles multi-color templates.
        
        /// <summary>
        /// Gets a pattern name for a color by finding the closest matching ColorPalette
        /// </summary>
        private static string GetPatternForColor(Color color)
        {
            // Find the closest ColorPalette match
            ColorPalette closestPalette = ColorPalette.White;
            double minDistance = double.MaxValue;
            
            foreach (ColorPalette palette in Enum.GetValues(typeof(ColorPalette)))
            {
                var paletteColor = palette.GetColor();
                double distance = ColorDistance(color, paletteColor);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestPalette = palette;
                }
            }
            
            // Find a pattern that maps to this palette
            foreach (var pattern in ColorPatterns.GetAllPatterns())
            {
                if (ColorPatterns.GetPaletteForPattern(pattern) == closestPalette)
                {
                    return pattern;
                }
            }
            
            // Fallback to "info" if no pattern found
            return "info";
        }
        
        /// <summary>
        /// Calculates color distance using RGB values
        /// </summary>
        private static double ColorDistance(Color a, Color b)
        {
            double rDiff = a.R - b.R;
            double gDiff = a.G - b.G;
            double bDiff = a.B - b.B;
            return Math.Sqrt(rDiff * rDiff + gDiff * gDiff + bDiff * bDiff);
        }
        
        /// <summary>
        /// Renders colored text as HTML
        /// </summary>
        public static string RenderAsHtml(IEnumerable<ColoredText> segments)
        {
            return FormatRenderer.RenderAsHtml(segments);
        }
        
        /// <summary>
        /// Renders colored text as ANSI escape codes (for console output)
        /// </summary>
        public static string RenderAsAnsi(IEnumerable<ColoredText> segments)
        {
            return FormatRenderer.RenderAsAnsi(segments);
        }
        
        /// <summary>
        /// Renders colored text as a simple format string (for debugging)
        /// </summary>
        public static string RenderAsDebug(IEnumerable<ColoredText> segments)
        {
            return FormatRenderer.RenderAsDebug(segments);
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
        
    }
}
