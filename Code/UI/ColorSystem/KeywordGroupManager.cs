using Avalonia.Media;
using System;
using System.Collections.Generic;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// Manages keyword groups for the keyword color system.
    /// Extracted from KeywordColorSystem to reduce size and improve Single Responsibility Principle compliance.
    /// </summary>
    public static class KeywordGroupManager
    {
        private static readonly Dictionary<string, KeywordGroup> _keywordGroups = new Dictionary<string, KeywordGroup>();
        
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
        /// Gets all keyword groups
        /// </summary>
        public static IEnumerable<KeywordGroup> GetAllGroups()
        {
            return _keywordGroups.Values;
        }
        
        /// <summary>
        /// Initializes default keyword groups
        /// </summary>
        public static void InitializeDefaultGroups()
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
        
        private static Color GetColorFromPattern(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return Colors.White;
            
            // First, check if this is a template name in ColorTemplateLibrary
            // This allows KeywordColorGroups.json to reference templates from ColorTemplates.json
            if (ColorTemplateLibrary.HasTemplate(pattern))
            {
                return ColorTemplateLibrary.GetRepresentativeColorFromTemplate(pattern);
            }
            // Fall back to hardcoded mappings for backward compatibility
            else
            {
                return pattern.ToLower() switch
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
        }
    }
}
