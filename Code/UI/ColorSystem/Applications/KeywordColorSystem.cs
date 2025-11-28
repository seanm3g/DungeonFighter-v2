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
        private static readonly Dictionary<string, KeywordGroup> _keywordGroups = new Dictionary<string, KeywordGroup>();
        private static readonly Dictionary<string, Color> _colorPatterns = new Dictionary<string, Color>();
        
        static KeywordColorSystem()
        {
            InitializeDefaultGroups();
        }
        
        /// <summary>
        /// Colors text by applying keyword coloring to all registered groups
        /// </summary>
        public static List<ColoredText> Colorize(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new List<ColoredText>();
            
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
                
                // Check if this word matches any keyword groups
                var color = GetColorForWord(cleanWord);
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
        /// Colors text by applying only the specified group's keywords
        /// </summary>
        public static List<ColoredText> ColorizeWithGroups(string text, string groupName)
        {
            if (string.IsNullOrEmpty(text) || !_keywordGroups.ContainsKey(groupName))
                return new List<ColoredText> { new ColoredText(text) };
            
            var group = _keywordGroups[groupName];
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
            var color = GetColorFromPattern(colorPattern);
            var group = new KeywordGroup
            {
                Name = name,
                ColorPattern = colorPattern,
                Color = color,
                CaseSensitive = caseSensitive,
                Keywords = new HashSet<string>(keywords, caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase)
            };
            
            _keywordGroups[name] = group;
        }
        
        /// <summary>
        /// Removes a keyword group
        /// </summary>
        public static void RemoveGroup(string name)
        {
            _keywordGroups.Remove(name);
        }
        
        /// <summary>
        /// Gets all registered group names
        /// </summary>
        public static IEnumerable<string> GetAllGroupNames()
        {
            return _keywordGroups.Keys;
        }
        
        /// <summary>
        /// Gets a keyword group by name
        /// </summary>
        public static KeywordGroup? GetKeywordGroup(string name)
        {
            return _keywordGroups.TryGetValue(name, out var group) ? group : null;
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
        /// Colors text using keyword coloring (alias for Colorize)
        /// </summary>
        public static List<ColoredText> ColorText(string text)
        {
            return Colorize(text);
        }
        
        private static void InitializeDefaultGroups()
        {
            // Damage-related keywords
            CreateGroup("damage", "damage", false,
                "damage", "hit", "strike", "attack", "critical", "crit", "wound", "bleed", "bleeding",
                "slash", "stab", "pierce", "crush", "smash", "punch", "kick", "claw", "bite");
            
            // Healing-related keywords
            CreateGroup("healing", "healing", false,
                "heal", "healing", "cure", "restore", "recover", "regenerate", "revive", "resurrect");
            
            // Enemy-related keywords
            CreateGroup("enemy", "enemy", false,
                "goblin", "orc", "troll", "dragon", "demon", "undead", "skeleton", "zombie", "ghost",
                "bandit", "thief", "assassin", "cultist", "wizard", "mage", "sorcerer", "necromancer");
            
            // Fire-related keywords
            CreateGroup("fire", "fire", false,
                "fire", "flame", "burn", "burning", "blaze", "inferno", "ember", "spark", "ignite");
            
            // Ice-related keywords
            CreateGroup("ice", "ice", false,
                "ice", "frost", "freeze", "frozen", "chill", "cold", "blizzard", "snow", "crystal");
            
            // Poison-related keywords
            CreateGroup("poison", "poison", false,
                "poison", "toxic", "venom", "acid", "corrupt", "taint", "disease", "plague");
            
            // Class-related keywords
            CreateGroup("class", "class", false,
                "warrior", "fighter", "knight", "paladin", "barbarian", "rogue", "thief", "assassin",
                "wizard", "mage", "sorcerer", "cleric", "priest", "druid", "ranger", "archer");
            
            // Status effect keywords
            CreateGroup("status", "status", false,
                "stun", "stunned", "paralyze", "paralyzed", "charm", "charmed", "fear", "feared",
                "confuse", "confused", "blind", "blinded", "silence", "silenced", "slow", "slowed");
            
            // Experience and progression keywords
            CreateGroup("progression", "progression", false,
                "experience", "xp", "level", "leveled", "skill", "ability", "talent", "perk",
                "upgrade", "enhance", "improve", "advance", "progress");
            
            // Currency and loot keywords
            CreateGroup("loot", "loot", false,
                "gold", "coin", "money", "treasure", "loot", "reward", "prize", "gem", "jewel",
                "artifact", "relic", "magic", "enchanted", "legendary", "epic", "rare");
        }
        
        private static Color GetColorForWord(string word)
        {
            foreach (var group in _keywordGroups.Values)
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
