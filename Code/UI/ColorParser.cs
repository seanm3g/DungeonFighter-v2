using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace RPGGame.UI
{
    /// <summary>
    /// Parses Caves of Qud-style color markup:
    /// - &X for foreground color (e.g., &R for red)
    /// - ^X for background color (e.g., ^g for dark green background)
    /// - {{template|text}} for color templates (e.g., {{fiery|Blazing Sword}})
    /// </summary>
    public static class ColorParser
    {
        // Regex patterns
        private static readonly Regex templatePattern = new Regex(@"\{\{([^|]+)\|([^}]+)\}\}", RegexOptions.Compiled);
        private static readonly Regex colorCodePattern = new Regex(@"[&^]([rgbcmywkoRGBCMYWKO])", RegexOptions.Compiled);

        /// <summary>
        /// Parses text with color markup into colored segments
        /// </summary>
        public static List<ColorDefinitions.ColoredSegment> Parse(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new List<ColorDefinitions.ColoredSegment>();
            }

            // First, expand all template markup
            text = ExpandTemplates(text);

            // Then parse color codes
            return ParseColorCodes(text);
        }

        /// <summary>
        /// Expands {{template|text}} markup into color code sequences
        /// </summary>
        private static string ExpandTemplates(string text)
        {
            return templatePattern.Replace(text, match =>
            {
                string templateName = match.Groups[1].Value.Trim();
                string content = match.Groups[2].Value;

                var template = ColorTemplateLibrary.GetTemplate(templateName);
                if (template == null)
                {
                    // If template doesn't exist, return the content unchanged
                    return content;
                }

                // Apply template and convert to color code sequence
                var segments = template.Apply(content);
                var result = new StringBuilder();

                foreach (var segment in segments)
                {
                    if (segment.Foreground.HasValue)
                    {
                        // Find the color code for this color
                        char? code = FindColorCode(segment.Foreground.Value);
                        if (code.HasValue)
                        {
                            result.Append('&');
                            result.Append(code.Value);
                        }
                    }

                    if (segment.Background.HasValue)
                    {
                        char? code = FindColorCode(segment.Background.Value);
                        if (code.HasValue)
                        {
                            result.Append('^');
                            result.Append(code.Value);
                        }
                    }

                    result.Append(segment.Text);
                }

                return result.ToString();
            });
        }

        /// <summary>
        /// Finds the color code that matches the given RGB color
        /// </summary>
        private static char? FindColorCode(ColorDefinitions.ColorRGB color)
        {
            foreach (var code in ColorDefinitions.GetAllColorCodes())
            {
                var testColor = ColorDefinitions.GetColor(code);
                if (testColor.HasValue && 
                    testColor.Value.R == color.R && 
                    testColor.Value.G == color.G && 
                    testColor.Value.B == color.B)
                {
                    return code;
                }
            }
            return null;
        }

        /// <summary>
        /// Parses text with &X and ^X color codes into colored segments
        /// </summary>
        private static List<ColorDefinitions.ColoredSegment> ParseColorCodes(string text)
        {
            var segments = new List<ColorDefinitions.ColoredSegment>();
            var currentText = new StringBuilder();
            ColorDefinitions.ColorRGB? currentForeground = null;
            ColorDefinitions.ColorRGB? currentBackground = null;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                // Check for color codes
                if ((c == '&' || c == '^') && i + 1 < text.Length)
                {
                    char nextChar = text[i + 1];
                    
                    if (ColorDefinitions.IsValidColorCode(nextChar))
                    {
                        // Save current segment if there's text
                        if (currentText.Length > 0)
                        {
                            segments.Add(new ColorDefinitions.ColoredSegment(
                                currentText.ToString(),
                                currentForeground,
                                currentBackground
                            ));
                            currentText.Clear();
                        }

                        // Update color
                        var color = ColorDefinitions.GetColor(nextChar);
                        if (color.HasValue)
                        {
                            if (c == '&')
                            {
                                currentForeground = color.Value;
                            }
                            else // c == '^'
                            {
                                currentBackground = color.Value;
                            }
                        }

                        // Skip the next character (the color code)
                        i++;
                        continue;
                    }
                }

                // Regular character - add to current text
                currentText.Append(c);
            }

            // Add final segment
            if (currentText.Length > 0)
            {
                segments.Add(new ColorDefinitions.ColoredSegment(
                    currentText.ToString(),
                    currentForeground,
                    currentBackground
                ));
            }

            return segments;
        }

        /// <summary>
        /// Strips all color markup from text, returning plain text
        /// </summary>
        public static string StripColorMarkup(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            // Remove templates
            text = templatePattern.Replace(text, match => match.Groups[2].Value);

            // Remove color codes
            text = colorCodePattern.Replace(text, "");

            return text;
        }

        /// <summary>
        /// Converts colored segments back to markup string (for debugging/testing)
        /// </summary>
        public static string SegmentsToMarkup(List<ColorDefinitions.ColoredSegment> segments)
        {
            var result = new StringBuilder();

            foreach (var segment in segments)
            {
                if (segment.Foreground.HasValue)
                {
                    var code = FindColorCode(segment.Foreground.Value);
                    if (code.HasValue)
                    {
                        result.Append('&');
                        result.Append(code.Value);
                    }
                }

                if (segment.Background.HasValue)
                {
                    var code = FindColorCode(segment.Background.Value);
                    if (code.HasValue)
                    {
                        result.Append('^');
                        result.Append(code.Value);
                    }
                }

                result.Append(segment.Text);
            }

            return result.ToString();
        }

        /// <summary>
        /// Helper method to wrap text in a color template
        /// </summary>
        public static string Colorize(string text, string templateOrColorCode)
        {
            // Check if it's a template
            if (ColorTemplateLibrary.HasTemplate(templateOrColorCode))
            {
                return $"{{{{{templateOrColorCode}|{text}}}}}";
            }

            // Check if it's a single color code
            if (templateOrColorCode.Length == 1 && ColorDefinitions.IsValidColorCode(templateOrColorCode[0]))
            {
                return $"&{templateOrColorCode}{text}";
            }

            // Unknown template/code, return unchanged
            return text;
        }

        /// <summary>
        /// Applies a foreground and optional background color to text
        /// </summary>
        public static string ColorizeRaw(string text, char foreground, char? background = null)
        {
            var result = new StringBuilder();
            
            if (ColorDefinitions.IsValidColorCode(foreground))
            {
                result.Append('&');
                result.Append(foreground);
            }

            if (background.HasValue && ColorDefinitions.IsValidColorCode(background.Value))
            {
                result.Append('^');
                result.Append(background.Value);
            }

            result.Append(text);

            return result.ToString();
        }

        /// <summary>
        /// Gets the length of text without color markup
        /// </summary>
        public static int GetDisplayLength(string text)
        {
            return StripColorMarkup(text).Length;
        }

        /// <summary>
        /// Tests if text contains color markup
        /// </summary>
        public static bool HasColorMarkup(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            return templatePattern.IsMatch(text) || colorCodePattern.IsMatch(text);
        }
    }
}

