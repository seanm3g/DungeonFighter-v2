using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia.Media;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// Parses text with color markup into ColoredText segments
    /// </summary>
    public static class ColoredTextParser
    {
        // Pattern for color markup: [color:pattern]text[/color] or [color:pattern]text[/]
        private static readonly Regex ColorMarkupRegex = new Regex(
            @"\[color:([^\]]+)\](.*?)\[/color\]|\[color:([^\]]+)\](.*?)\[/\]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );
        
        // Pattern for simple color markup: [pattern]text[/pattern]
        private static readonly Regex SimpleColorRegex = new Regex(
            @"\[([^\]]+)\](.*?)\[/\1\]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );
        
        // Pattern for character-specific markup: [char:name:pattern]text[/char]
        private static readonly Regex CharacterColorRegex = new Regex(
            @"\[char:([^:]+):([^\]]+)\](.*?)\[/char\]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );
        
        // Pattern for template syntax: {{template|text}}
        // Matches template name and text, ensuring we match up to the closing }}
        private static readonly Regex TemplateRegex = new Regex(
            @"\{\{([^|]+)\|(.*?)\}\}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline
        );
        
        /// <summary>
        /// Parses text with color markup into ColoredText segments
        /// </summary>
        public static List<ColoredText> Parse(string text, string? characterName = null)
        {
            if (string.IsNullOrEmpty(text))
                return new List<ColoredText>();
            
            // Check if text contains old-style color codes (&X format)
            // If so, use the compatibility layer to convert them
            if (CompatibilityLayer.HasColorMarkup(text) && ContainsOldStyleColorCodes(text))
            {
                return CompatibilityLayer.ConvertOldMarkup(text);
            }
            
            var segments = new List<ColoredText>();
            var currentIndex = 0;
            
            // Find all color markup patterns
            var matches = new List<ColorMatch>();
            
            // Add template syntax matches ({{template|text}}) - process first to handle nested cases
            matches.AddRange(TemplateRegex.Matches(text).Cast<Match>()
                .Select(m => new ColorMatch
                {
                    Start = m.Index,
                    End = m.Index + m.Length,
                    ColorPattern = m.Groups[1].Value.Trim(),
                    Text = m.Groups[2].Value,
                    Type = ColorMatchType.Template
                }));
            
            // Add explicit color markup matches
            matches.AddRange(ColorMarkupRegex.Matches(text).Cast<Match>()
                .Select(m => new ColorMatch
                {
                    Start = m.Index,
                    End = m.Index + m.Length,
                    ColorPattern = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[3].Value,
                    Text = m.Groups[2].Success ? m.Groups[2].Value : m.Groups[4].Value,
                    Type = ColorMatchType.Explicit
                }));
            
            // Add simple color markup matches
            matches.AddRange(SimpleColorRegex.Matches(text).Cast<Match>()
                .Select(m => new ColorMatch
                {
                    Start = m.Index,
                    End = m.Index + m.Length,
                    ColorPattern = m.Groups[1].Value,
                    Text = m.Groups[2].Value,
                    Type = ColorMatchType.Simple
                }));
            
            // Add character-specific markup matches
            matches.AddRange(CharacterColorRegex.Matches(text).Cast<Match>()
                .Select(m => new ColorMatch
                {
                    Start = m.Index,
                    End = m.Index + m.Length,
                    ColorPattern = m.Groups[2].Value,
                    Text = m.Groups[3].Value,
                    CharacterName = m.Groups[1].Value,
                    Type = ColorMatchType.Character
                }));
            
            // Sort matches by start position
            matches.Sort((a, b) => a.Start.CompareTo(b.Start));
            
            // Process matches and build segments
            foreach (var match in matches)
            {
                // Add any text before this match
                if (match.Start > currentIndex)
                {
                    var beforeText = text.Substring(currentIndex, match.Start - currentIndex);
                    if (!string.IsNullOrEmpty(beforeText))
                    {
                        segments.Add(new ColoredText(beforeText, Colors.White));
                    }
                }
                
                // Add the colored text
                // For templates, check ColorTemplateLibrary first (supports multi-color sequences)
                if (match.Type == ColorMatchType.Template && ColorTemplateLibrary.HasTemplate(match.ColorPattern))
                {
                    var templateSegments = ColorTemplateLibrary.GetTemplate(match.ColorPattern, match.Text);
                    segments.AddRange(templateSegments);
                }
                else
                {
                    var color = GetColorForMatch(match, characterName);
                    segments.Add(new ColoredText(match.Text, color));
                }
                
                currentIndex = match.End;
            }
            
            // Add any remaining text
            if (currentIndex < text.Length)
            {
                var remainingText = text.Substring(currentIndex);
                if (!string.IsNullOrEmpty(remainingText))
                {
                    segments.Add(new ColoredText(remainingText, Colors.White));
                }
            }
            
            // Merge adjacent segments with the same color to prevent extra spacing
            return MergeAdjacentSegments(segments);
        }
        
        /// <summary>
        /// Parses text with simple pattern-based coloring
        /// </summary>
        public static List<ColoredText> ParseWithPatterns(string text, string? characterName = null)
        {
            if (string.IsNullOrEmpty(text))
                return new List<ColoredText>();
            
            var segments = new List<ColoredText>();
            var words = text.Split(' ');
            
            foreach (var word in words)
            {
                var cleanWord = word.Trim();
                if (string.IsNullOrEmpty(cleanWord))
                {
                    segments.Add(new ColoredText(" ", Colors.White));
                    continue;
                }
                
                // Check if this word matches any patterns
                var color = GetColorForWord(cleanWord, characterName);
                segments.Add(new ColoredText(cleanWord, color));
                segments.Add(new ColoredText(" ", Colors.White));
            }
            
            // Remove trailing space
            if (segments.Count > 0 && segments.Last().Text == " ")
            {
                segments.RemoveAt(segments.Count - 1);
            }
            
            // Merge adjacent segments with the same color to prevent extra spacing
            return MergeAdjacentSegments(segments);
        }
        
        /// <summary>
        /// Creates a simple colored text from a pattern
        /// </summary>
        public static List<ColoredText> CreateFromPattern(string text, string pattern, string? characterName = null)
        {
            var color = GetColorForPattern(pattern, characterName);
            return new List<ColoredText> { new ColoredText(text, color) };
        }
        
        private static Color GetColorForMatch(ColorMatch match, string? characterName)
        {
            switch (match.Type)
            {
                case ColorMatchType.Template:
                case ColorMatchType.Explicit:
                case ColorMatchType.Simple:
                    // Use pattern-based color
                    return GetColorForPattern(match.ColorPattern, characterName);
                
                case ColorMatchType.Character:
                    // Use character-specific color
                    var profile = CharacterColorManager.GetProfile(match.CharacterName);
                    return profile.GetColorForPattern(match.ColorPattern);
                
                default:
                    // Use pattern-based color
                    return GetColorForPattern(match.ColorPattern, characterName);
            }
        }
        
        private static Color GetColorForPattern(string pattern, string? characterName)
        {
            if (string.IsNullOrEmpty(pattern))
                return Colors.White;
            
            // If we have a character name, try to get character-specific color first
            if (!string.IsNullOrEmpty(characterName))
            {
                var profile = CharacterColorManager.GetProfile(characterName);
                var characterColor = profile.GetColorForPattern(pattern);
                
                // If character has a custom color for this pattern, use it
                if (characterColor != ColorPatterns.GetColorForPattern(pattern))
                {
                    return characterColor;
                }
            }
            
            // Fall back to global pattern color
            return ColorPatterns.GetColorForPattern(pattern);
        }
        
        private static Color GetColorForWord(string word, string? characterName)
        {
            // Check if the word matches any patterns
            if (ColorPatterns.HasPattern(word))
            {
                return GetColorForPattern(word, characterName);
            }
            
            // Check for partial matches (e.g., "damage" in "damaged")
            var lowerWord = word.ToLowerInvariant();
            foreach (var pattern in ColorPatterns.GetAllPatterns())
            {
                if (lowerWord.Contains(pattern) || pattern.Contains(lowerWord))
                {
                    return GetColorForPattern(pattern, characterName);
                }
            }
            
            // Default to white
            return Colors.White;
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
                var normalizedText = System.Text.RegularExpressions.Regex.Replace(segment.Text, @"\s+", " ");
                if (normalizedText != segment.Text)
                {
                    merged[i] = new ColoredText(normalizedText, segment.Color);
                }
            }
            
            // Final pass: normalize spaces between adjacent segments again (after internal normalization)
            // This catches any remaining boundary issues after normalizing within segments
            for (int i = 0; i < merged.Count - 1; i++)
            {
                var current = merged[i];
                var next = merged[i + 1];
                
                string currentText = current.Text;
                string nextText = next.Text;
                
                // If one segment ends with space and next starts with space, remove one
                if (currentText.EndsWith(" ") && nextText.StartsWith(" "))
                {
                    var trimmedCurrent = currentText.TrimEnd();
                    if (trimmedCurrent.Length > 0)
                    {
                        merged[i] = new ColoredText(trimmedCurrent, current.Color);
                    }
                    else
                    {
                        merged.RemoveAt(i);
                        i--;
                        continue;
                    }
                }
            }
            
            // Remove any empty segments that might have been created
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
        /// Checks if text contains old-style color codes (&X format)
        /// </summary>
        private static bool ContainsOldStyleColorCodes(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
            
            // Check for old-style color codes (both uppercase and lowercase)
            return text.Contains("&R") || text.Contains("&G") || text.Contains("&B") || 
                   text.Contains("&Y") || text.Contains("&C") || text.Contains("&M") || 
                   text.Contains("&W") || text.Contains("&K") || text.Contains("&y") ||
                   text.Contains("&r") || text.Contains("&g") || text.Contains("&b") || 
                   text.Contains("&o") || text.Contains("&O") || text.Contains("&w") ||
                   text.Contains("&c") || text.Contains("&m") || text.Contains("&k");
        }
        
        private class ColorMatch
        {
            public int Start { get; set; }
            public int End { get; set; }
            public string ColorPattern { get; set; } = "";
            public string Text { get; set; } = "";
            public string CharacterName { get; set; } = "";
            public ColorMatchType Type { get; set; }
        }
        
        private enum ColorMatchType
        {
            Template,
            Explicit,
            Simple,
            Character
        }
    }
}
