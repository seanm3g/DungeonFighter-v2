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
        
        // Pattern for legacy color codes: &X where X is a color code character
        private static readonly Regex LegacyColorCodeRegex = new Regex(
            @"&([RGYBCMWKrygbocmw])",
            RegexOptions.Compiled
        );
        
        /// <summary>
        /// Parses text with color markup into ColoredText segments
        /// Supports both new markup format and legacy &X format codes
        /// </summary>
        public static List<ColoredText> Parse(string text, string? characterName = null)
        {
            if (string.IsNullOrEmpty(text))
                return new List<ColoredText>();
            
            // Check if text contains legacy color codes - if so, parse them first
            if (LegacyColorCodeRegex.IsMatch(text))
            {
                return ParseLegacyColorCodes(text);
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
            // Only match if the pattern inside brackets is a valid color pattern
            // This prevents entity names like [Joren Blackthorn] from being treated as markup
            matches.AddRange(SimpleColorRegex.Matches(text).Cast<Match>()
                .Where(m => ColorPatterns.HasPattern(m.Groups[1].Value))
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
            return ColoredTextMerger.MergeAdjacentSegments(segments);
        }
        
        /// <summary>
        /// Parses text with simple pattern-based coloring
        /// Preserves original spacing in the text
        /// </summary>
        public static List<ColoredText> ParseWithPatterns(string text, string? characterName = null)
        {
            if (string.IsNullOrEmpty(text))
                return new List<ColoredText>();
            
            var segments = new List<ColoredText>();
            var words = text.Split(new[] { ' ' }, StringSplitOptions.None);
            
            for (int i = 0; i < words.Length; i++)
            {
                var word = words[i];
                
                // Skip empty words (they represent spaces between words)
                if (string.IsNullOrEmpty(word))
                {
                    // Only add space if this isn't the last empty segment (which would be trailing)
                    if (i < words.Length - 1)
                    {
                        segments.Add(new ColoredText(" ", Colors.White));
                    }
                    continue;
                }
                
                // Check if this word matches any patterns
                var color = GetColorForWord(word, characterName);
                segments.Add(new ColoredText(word, color));
                
                // Add space after word if not the last word
                if (i < words.Length - 1)
                {
                    segments.Add(new ColoredText(" ", Colors.White));
                }
            }
            
            // Merge adjacent segments with the same color to prevent extra spacing
            return ColoredTextMerger.MergeAdjacentSegments(segments);
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
        
        // Merging logic has been moved to ColoredTextMerger for centralized maintenance
        
        /// <summary>
        /// Parses legacy &X format color codes
        /// </summary>
        private static List<ColoredText> ParseLegacyColorCodes(string text)
        {
            var segments = new List<ColoredText>();
            var currentIndex = 0;
            Color currentColor = Colors.White;
            
            var matches = LegacyColorCodeRegex.Matches(text);
            
            foreach (Match match in matches)
            {
                // Add text before the color code (with current color)
                if (match.Index > currentIndex)
                {
                    var beforeText = text.Substring(currentIndex, match.Index - currentIndex);
                    if (!string.IsNullOrEmpty(beforeText))
                    {
                        segments.Add(new ColoredText(beforeText, currentColor));
                    }
                }
                
                // Update current color based on the color code
                char colorCode = match.Groups[1].Value[0];
                currentColor = GetColorFromLegacyCode(colorCode);
                
                // Move past the color code
                currentIndex = match.Index + match.Length;
            }
            
            // Add remaining text after the last color code
            if (currentIndex < text.Length)
            {
                var remainingText = text.Substring(currentIndex);
                if (!string.IsNullOrEmpty(remainingText))
                {
                    segments.Add(new ColoredText(remainingText, currentColor));
                }
            }
            
            // If no color codes were found, return the whole text as white
            if (segments.Count == 0 && !string.IsNullOrEmpty(text))
            {
                segments.Add(new ColoredText(text, Colors.White));
            }
            
            // Merge adjacent segments with the same color
            return ColoredTextMerger.MergeAdjacentSegments(segments);
        }
        
        /// <summary>
        /// Converts a legacy color code character to a Color
        /// </summary>
        private static Color GetColorFromLegacyCode(char colorCode)
        {
            return colorCode switch
            {
                'R' => ColorPalette.Error.GetColor(),
                'r' => ColorPalette.DarkRed.GetColor(),
                'G' => ColorPalette.Success.GetColor(),
                'g' => ColorPalette.DarkGreen.GetColor(),
                'B' => ColorPalette.Info.GetColor(),
                'b' => ColorPalette.DarkBlue.GetColor(),
                'Y' => ColorPalette.Warning.GetColor(),
                'y' => Colors.White, // White/default
                'C' => ColorPalette.Cyan.GetColor(),
                'c' => ColorPalette.DarkCyan.GetColor(),
                'M' => ColorPalette.Magenta.GetColor(),
                'm' => ColorPalette.DarkMagenta.GetColor(),
                'W' => ColorPalette.Gold.GetColor(),
                'w' => ColorPalette.Brown.GetColor(),
                'O' => ColorPalette.Orange.GetColor(),
                'o' => ColorPalette.DarkYellow.GetColor(),
                'K' => ColorPalette.DarkGray.GetColor(),
                'k' => Colors.Black,
                _ => Colors.White
            };
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
