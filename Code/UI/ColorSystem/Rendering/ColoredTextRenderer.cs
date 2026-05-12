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
            
            for (int i = 0; i < segmentList.Count; i++)
            {
                var segment = segmentList[i];
                
                if (string.IsNullOrEmpty(segment.Text))
                    continue;

                string textToRender = segment.Text;
                string? sourceTemplate = GetRenderableSourceTemplate(segment);
                int groupEndIndex = i;

                if (sourceTemplate != null)
                {
                    while (groupEndIndex + 1 < segmentList.Count)
                    {
                        var nextInGroup = segmentList[groupEndIndex + 1];
                        if (nextInGroup == null
                            || string.IsNullOrEmpty(nextInGroup.Text)
                            || !string.Equals(GetRenderableSourceTemplate(nextInGroup), sourceTemplate, StringComparison.OrdinalIgnoreCase))
                        {
                            break;
                        }

                        textToRender += nextInGroup.Text;
                        groupEndIndex++;
                    }
                }
                
                bool isWhite = ColorValidator.AreColorsEqual(segment.Color, Colors.White);
                
                if (sourceTemplate != null)
                {
                    markup.Append($"{{{{{sourceTemplate}|");
                    markup.Append(textToRender);
                    markup.Append("}}");
                }
                else if (!isWhite)
                {
                    markup.Append($"[color:{ColorToHex(segment.Color)}]");
                    markup.Append(textToRender);
                    markup.Append("[/color]");
                }
                else
                {
                    markup.Append(textToRender);
                }
                
                // Ensure space after segment (unless it's the last segment, ends with punctuation/newline, or next starts with punctuation/newline)
                // This ensures each item has a space after it leading to the next item
                // BUT: skip ALL spacing if all segments form a single word (prevents spacing in multi-color templates like room names)
                // IMPORTANT: Don't add space if current segment IS whitespace or already ends with whitespace
                if (!isSingleWord)
                {
                    bool currentIsWhitespace = textToRender.Trim().Length == 0 && textToRender.Length > 0;
                    bool currentEndsWithSpace = textToRender.Length > 0 && char.IsWhiteSpace(textToRender[textToRender.Length - 1]);
                    
                    // CRITICAL: Never add space if current segment ends with whitespace (prevents double spacing)
                    // This fixes issues like "Room: " + "Magma Chamber" where "Room: " already has a trailing space
                    if (groupEndIndex < segmentList.Count - 1 && !currentIsWhitespace && !currentEndsWithSpace)
                    {
                        var nextSegment = segmentList[groupEndIndex + 1];
                        if (!string.IsNullOrEmpty(nextSegment.Text))
                        {
                            // Also check if next segment starts with whitespace
                            bool nextStartsWithSpace = char.IsWhiteSpace(nextSegment.Text[0]);
                            
                            // Only add space if neither segment has whitespace at the boundary
                            if (!nextStartsWithSpace)
                            {
                                // Check if these two segments are part of the same word
                                // This prevents spacing within words in multi-color templates like room names
                                bool areSameWord = AreAdjacentSegmentsSameWord(textToRender, nextSegment.Text);
                                
                                if (!areSameWord)
                                {
                                    // Use centralized spacing manager with word boundary detection for multi-color templates
                                    bool needsSpace = CombatLogSpacingManager.ShouldAddSpaceBetween(textToRender, nextSegment.Text, checkWordBoundary: true);
                                    if (needsSpace)
                                    {
                                        // Add space as plain text (white)
                                        markup.Append(CombatLogSpacingManager.SingleSpace);
                                    }
                                }
                            }
                        }
                    }
                }

                i = groupEndIndex;
            }
            
            return markup.ToString();
        }

        private static string? GetRenderableSourceTemplate(ColoredText segment)
        {
            if (segment == null || string.IsNullOrWhiteSpace(segment.SourceTemplate))
                return null;

            return ColorTemplateLibrary.HasTemplate(segment.SourceTemplate)
                ? segment.SourceTemplate
                : null;
        }

        private static string ColorToHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
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
        
        /// <summary>
        /// Checks if two adjacent segments are part of the same word.
        /// This prevents spacing within words in multi-color templates like room names.
        /// For example, "Q" + "ua" should be treated as part of the same word "Quantum".
        /// </summary>
        private static bool AreAdjacentSegmentsSameWord(string currentText, string nextText)
        {
            if (string.IsNullOrEmpty(currentText) || string.IsNullOrEmpty(nextText))
                return false;
            
            // If either segment contains whitespace, they're not part of the same word
            if (currentText.Any(char.IsWhiteSpace) || nextText.Any(char.IsWhiteSpace))
                return false;
            
            // Get boundary characters
            char lastChar = currentText[currentText.Length - 1];
            char firstChar = nextText[0];
            
            // If both boundary characters are letters/digits, they're part of the same word
            // This handles cases like "Q" + "ua" (both are letters, forming "Qua" part of "Quantum")
            if (char.IsLetterOrDigit(lastChar) && char.IsLetterOrDigit(firstChar))
            {
                return true;
            }
            
            // Check for punctuation that's part of the word (like apostrophe or hyphen)
            if ((lastChar == '\'' || lastChar == '-') && char.IsLetter(firstChar))
            {
                return true;
            }
            
            if (char.IsLetter(lastChar) && (firstChar == '\'' || firstChar == '-'))
            {
                return true;
            }
            
            // Otherwise, they're separate words or have a word boundary
            return false;
        }
        
        // Spacing logic has been moved to CombatLogSpacingManager for centralized management.
        // Use CombatLogSpacingManager.ShouldAddSpaceBetween() with checkWordBoundary: true
        // for renderer-specific spacing that handles multi-color templates.
        
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
                    result.Add(new ColoredText(truncatedText, segment.Color, segment.SourceTemplate, segment.ColorReadyForCanvas));
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
