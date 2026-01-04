using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia.Media;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// System for automatically coloring keywords in text based on predefined groups
    /// </summary>
    public static class KeywordColorSystem
    {
        private static readonly Dictionary<string, Color> _colorPatterns = new Dictionary<string, Color>();
        private static readonly Dictionary<string, Color> _characterNames = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
        
        static KeywordColorSystem()
        {
            KeywordGroupManager.InitializeDefaultGroups();
        }
        
        /// <summary>
        /// Colors text by applying keyword coloring to all registered groups
        /// Also checks for character names and colors them appropriately
        /// </summary>
        public static List<ColoredText> Colorize(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new List<ColoredText>();
            
            var result = new List<ColoredText>();
            var words = text.Split(' ');
            
            // Check for multi-word character names first
            int i = 0;
            while (i < words.Length)
            {
                var word = words[i];
                var cleanWord = word.Trim();
                
                if (string.IsNullOrEmpty(cleanWord))
                {
                    result.Add(new ColoredText(" ", Colors.White));
                    i++;
                    continue;
                }
                
                // Try to match character names (check current word and next word if available)
                // Strip punctuation for matching but preserve it in the output
                var wordWithoutPunctuation = cleanWord.TrimEnd('.', ',', '!', '?', ';', ':');
                Color? characterColor = null;
                int wordsToConsume = 1;
                
                // Check single word character name
                if (_characterNames.TryGetValue(wordWithoutPunctuation, out var singleWordColor))
                {
                    characterColor = singleWordColor;
                }
                // Check two-word character name (e.g., "Joren Blackthorn")
                else if (i + 1 < words.Length)
                {
                    var nextWord = words[i + 1].Trim();
                    var nextWordWithoutPunctuation = nextWord.TrimEnd('.', ',', '!', '?', ';', ':');
                    var twoWordName = $"{wordWithoutPunctuation} {nextWordWithoutPunctuation}";
                    if (_characterNames.TryGetValue(twoWordName, out var twoWordColor))
                    {
                        characterColor = twoWordColor;
                        wordsToConsume = 2;
                    }
                }
                
                if (characterColor.HasValue)
                {
                    // Found a character name - use character color
                    if (wordsToConsume == 2)
                    {
                        result.Add(new ColoredText($"{cleanWord} {words[i + 1].Trim()}", characterColor.Value));
                        i += 2;
                    }
                    else
                    {
                        result.Add(new ColoredText(cleanWord, characterColor.Value));
                        i++;
                    }
                    
                    if (i < words.Length)
                    {
                        result.Add(new ColoredText(" ", Colors.White));
                    }
                }
                else
                {
                    // Not a character name - check if this word matches any keyword groups
                    var color = GetColorForWord(cleanWord);
                    result.Add(new ColoredText(cleanWord, color));
                    result.Add(new ColoredText(" ", Colors.White));
                    i++;
                }
            }
            
            // Remove trailing space
            if (result.Count > 0 && result.Last().Text == " ")
            {
                result.RemoveAt(result.Count - 1);
            }
            
            return result;
        }
        
        /// <summary>
        /// Colors text by applying only the specified group's keywords
        /// </summary>
        public static List<ColoredText> ColorizeWithGroups(string text, string groupName)
        {
            if (string.IsNullOrEmpty(text))
                return new List<ColoredText> { new ColoredText(text) };
            
            var group = KeywordGroupManager.GetKeywordGroup(groupName);
            if (group == null)
                return new List<ColoredText> { new ColoredText(text) };
            var result = new List<ColoredText>();
            var words = text.Split(' ');
            
            foreach (var word in words)
            {
                var cleanWord = word.Trim();
                if (string.IsNullOrEmpty(cleanWord))
                {
                    result.Add(new ColoredText(" ", Colors.White));
                    continue;
                }
                
                // Check if this word matches the specified group
                var color = group.ContainsKeyword(cleanWord) ? group.Color : Colors.White;
                result.Add(new ColoredText(cleanWord, color));
                result.Add(new ColoredText(" ", Colors.White));
            }
            
            // Remove trailing space
            if (result.Count > 0 && result.Last().Text == " ")
            {
                result.RemoveAt(result.Count - 1);
            }
            
            return result;
        }
        
        /// <summary>
        /// Creates a new keyword group
        /// </summary>
        public static void CreateGroup(string name, string colorPattern, bool caseSensitive, params string[] keywords)
        {
            KeywordGroupManager.CreateGroup(name, colorPattern, caseSensitive, keywords);
        }
        
        /// <summary>
        /// Removes a keyword group
        /// </summary>
        public static void RemoveGroup(string name)
        {
            KeywordGroupManager.RemoveGroup(name);
        }
        
        /// <summary>
        /// Gets all registered group names
        /// </summary>
        public static IEnumerable<string> GetAllGroupNames()
        {
            return KeywordGroupManager.GetAllGroupNames();
        }
        
        /// <summary>
        /// Gets a keyword group by name
        /// </summary>
        public static KeywordGroup? GetKeywordGroup(string name)
        {
            return KeywordGroupManager.GetKeywordGroup(name);
        }
        
        /// <summary>
        /// Clears the color pattern cache
        /// Call this after reloading ColorTemplates.json to pick up new template colors
        /// </summary>
        public static void ClearColorPatternCache()
        {
            _colorPatterns.Clear();
        }
        
        /// <summary>
        /// Registers a character name for automatic coloring
        /// Character names will be colored using the player color palette
        /// </summary>
        public static void RegisterCharacterName(string characterName, Color? color = null)
        {
            if (string.IsNullOrEmpty(characterName))
                return;
            
            var colorToUse = color ?? ColorPalette.Player.GetColor();
            _characterNames[characterName] = colorToUse;
        }
        
        /// <summary>
        /// Registers multiple character names for automatic coloring
        /// </summary>
        public static void RegisterCharacterNames(IEnumerable<string> characterNames, Color? color = null)
        {
            if (characterNames == null)
                return;
            
            var colorToUse = color ?? ColorPalette.Player.GetColor();
            foreach (var name in characterNames)
            {
                if (!string.IsNullOrEmpty(name))
                {
                    _characterNames[name] = colorToUse;
                }
            }
        }
        
        /// <summary>
        /// Unregisters a character name
        /// </summary>
        public static void UnregisterCharacterName(string characterName)
        {
            if (string.IsNullOrEmpty(characterName))
                return;
            
            _characterNames.Remove(characterName);
        }
        
        /// <summary>
        /// Clears all registered character names
        /// </summary>
        public static void ClearCharacterNames()
        {
            _characterNames.Clear();
        }
        
        /// <summary>
        /// Colors text using keyword coloring (alias for Colorize)
        /// </summary>
        public static List<ColoredText> ColorText(string text)
        {
            return Colorize(text);
        }
        
        
        private static Color GetColorForWord(string word)
        {
            // Exclude common articles from being colored
            var lowerWord = word.ToLowerInvariant().TrimEnd('.', ',', '!', '?', ';', ':');
            var commonArticles = new HashSet<string> { "the", "a", "an", "and", "or", "but", "for", "with", "to", "of", "in", "on", "at", "by" };
            if (commonArticles.Contains(lowerWord))
            {
                return Colors.White;
            }
            
            foreach (var group in KeywordGroupManager.GetAllGroups())
            {
                if (group.ContainsKeyword(word))
                {
                    return group.Color;
                }
            }
            
            return Colors.White;
        }
        
        private static Color GetColorFromPattern(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return Colors.White;
            
            // Check cache first
            if (_colorPatterns.TryGetValue(pattern, out var existingColor))
                return existingColor;
            
            Color color;
            
            // First, check if this is a template name in ColorTemplateLibrary
            // This allows KeywordColorGroups.json to reference templates from ColorTemplates.json
            if (ColorTemplateLibrary.HasTemplate(pattern))
            {
                color = ColorTemplateLibrary.GetRepresentativeColorFromTemplate(pattern);
            }
            // Fall back to hardcoded mappings for backward compatibility
            else
            {
                color = pattern.ToLower() switch
                {
                    "damage" => ColorPalette.Damage.GetColor(),
                    "healing" => ColorPalette.Healing.GetColor(),
                    "enemy" => ColorPalette.Enemy.GetColor(),
                    "fire" => ColorPalette.Orange.GetColor(),
                    "ice" => ColorPalette.Cyan.GetColor(),
                    "poison" => ColorPalette.Green.GetColor(),
                    "class" => ColorPalette.Purple.GetColor(),
                    "status" => ColorPalette.Yellow.GetColor(),
                    "progression" => ColorPalette.Gold.GetColor(),
                    "loot" => ColorPalette.Gold.GetColor(),
                    "cyan" => ColorPalette.Cyan.GetColor(),
                    "golden" => ColorPalette.Gold.GetColor(),
                    _ => ColorPalette.White.GetColor()
                };
            }
            
            // Cache the result
            _colorPatterns[pattern] = color;
            return color;
        }
    }
    
    /// <summary>
    /// Represents a group of keywords with associated coloring
    /// </summary>
    public class KeywordGroup
    {
        public string Name { get; set; } = "";
        public string ColorPattern { get; set; } = "";
        public Color Color { get; set; } = Colors.White;
        public bool CaseSensitive { get; set; } = false;
        public HashSet<string> Keywords { get; set; } = new HashSet<string>();
        
        /// <summary>
        /// Checks if a word matches any keyword in this group
        /// </summary>
        public bool ContainsKeyword(string word)
        {
            if (string.IsNullOrEmpty(word))
                return false;
            
            var searchWord = CaseSensitive ? word : word.ToLowerInvariant();
            
            // Check exact matches
            if (Keywords.Contains(searchWord))
                return true;
            
            // Check partial matches (word contains keyword or keyword contains word)
            foreach (var keyword in Keywords)
            {
                var searchKeyword = CaseSensitive ? keyword : keyword.ToLowerInvariant();
                if (searchWord.Contains(searchKeyword) || searchKeyword.Contains(searchWord))
                    return true;
            }
            
            return false;
        }
    }
}
