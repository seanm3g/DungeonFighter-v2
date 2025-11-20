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
                var color = GetColorForMatch(match, characterName);
                segments.Add(new ColoredText(match.Text, color));
                
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
            
            return segments;
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
            
            return segments;
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
