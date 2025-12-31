using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI
{
    /// <summary>
    /// Automatic keyword coloring system
    /// Colors specific keywords based on their semantic meaning
    /// </summary>
    public static class KeywordColorSystem
    {
        private static Dictionary<string, string> _keywordGroups = new Dictionary<string, string>();
        private static bool _initialized = false;
        
        /// <summary>
        /// Initializes the keyword color system
        /// </summary>
        public static void Initialize()
        {
            if (_initialized)
                return;
                
            // Load keyword groups from JSON or use defaults
            LoadDefaultKeywords();
            _initialized = true;
        }
        
        /// <summary>
        /// Loads default keyword groups
        /// </summary>
        private static void LoadDefaultKeywords()
        {
            // Damage keywords
            AddKeyword("damage", "damage");
            AddKeyword("hit", "damage");
            AddKeyword("attack", "damage");
            AddKeyword("attacks", "damage");
            AddKeyword("critical", "critical");
            
            // Healing keywords
            AddKeyword("heal", "healing");
            AddKeyword("heals", "healing");
            AddKeyword("healing", "healing");
            AddKeyword("restore", "healing");
            AddKeyword("restores", "healing");
            
            // Status keywords
            AddKeyword("poison", "poison");
            AddKeyword("poisoned", "poison");
            AddKeyword("stun", "stun");
            AddKeyword("stunned", "stun");
            AddKeyword("bleed", "bleed");
            AddKeyword("bleeding", "bleed");
            
            // Rarity keywords
            AddKeyword("common", "common");
            AddKeyword("uncommon", "uncommon");
            AddKeyword("rare", "rare");
            AddKeyword("epic", "epic");
            AddKeyword("legendary", "legendary");
        }
        
        /// <summary>
        /// Adds a keyword to a group
        /// </summary>
        public static void AddKeyword(string keyword, string group)
        {
            _keywordGroups[keyword.ToLower()] = group.ToLower();
        }
        
        /// <summary>
        /// Colors text by applying keyword coloring
        /// </summary>
        public static List<ColoredText> ColorText(string text)
        {
            if (!_initialized)
                Initialize();
                
            var result = new List<ColoredText>();
            var words = text.Split(' ');
            
            for (int i = 0; i < words.Length; i++)
            {
                var word = words[i];
                var cleanWord = word.Trim('.', ',', '!', '?').ToLower();
                
                if (_keywordGroups.TryGetValue(cleanWord, out var group))
                {
                    // Apply color based on group
                    var color = GetColorForGroup(group);
                    result.Add(new ColoredText(word, color));
                }
                else
                {
                    // No keyword match, use default color
                    result.Add(new ColoredText(word, Colors.White));
                }
                
                // Add space between words (except last word)
                if (i < words.Length - 1)
                {
                    result.Add(new ColoredText(" ", Colors.White));
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Gets the color for a keyword group
        /// </summary>
        private static Color GetColorForGroup(string group)
        {
            return group switch
            {
                "damage" => ColorPalette.DarkRed.GetColor(),
                "critical" => ColorPalette.Red.GetColor(),
                "healing" => ColorPalette.Green.GetColor(),
                "poison" => ColorPalette.DarkGreen.GetColor(),
                "stun" => ColorPalette.Yellow.GetColor(),
                "bleed" => ColorPalette.Red.GetColor(),
                "common" => ColorPalette.Gray.GetColor(),
                "uncommon" => ColorPalette.Green.GetColor(),
                "rare" => ColorPalette.Blue.GetColor(),
                "epic" => ColorPalette.Purple.GetColor(),
                "legendary" => ColorPalette.Orange.GetColor(),
                _ => Colors.White
            };
        }
        
        /// <summary>
        /// Colorizes text using keyword coloring (alias for ColorText)
        /// </summary>
        public static List<ColoredText> Colorize(string text)
        {
            return ColorText(text);
        }
        
        /// <summary>
        /// Creates a new keyword group
        /// </summary>
        public static void CreateGroup(string groupName, Color color)
        {
            // For now, groups are managed internally
            // This is a placeholder for future extensibility
        }
        
        /// <summary>
        /// Removes a keyword group
        /// </summary>
        public static void RemoveGroup(string groupName)
        {
            // For now, groups are managed internally
            // This is a placeholder for future extensibility
        }
        
        /// <summary>
        /// Gets all group names
        /// </summary>
        public static List<string> GetAllGroupNames()
        {
            return new List<string> 
            { 
                "damage", "critical", "healing", "poison", "stun", "bleed",
                "common", "uncommon", "rare", "epic", "legendary"
            };
        }
        
        /// <summary>
        /// Gets the group for a specific keyword
        /// </summary>
        public static string GetKeywordGroup(string keyword)
        {
            if (_keywordGroups.TryGetValue(keyword.ToLower(), out var group))
            {
                return group;
            }
            return "default";
        }
    }
}

