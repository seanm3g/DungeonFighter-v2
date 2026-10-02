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
            // Keep combat-log keyword paint sparse: only high-signal outcome / effect words.
            // Common verbs (hit/strike/attack) and atmospheric prose (stance/fortune/steel) stay white.
            // Identity spans (names, action) are handled by CombatSequenceNarrativeEmphasis, not here.

            // Damage severity / decisive combat nouns (red)
            CreateGroup("damage", "damage", false,
                "critical", "crit", "wound", "wounds", "injury", "scratch", "harm",
                "bleed", "bleeding");

            // Healing outcomes
            CreateGroup("healing", "healing", false,
                "heal", "healed", "healing", "cure", "restore", "restored", "recover",
                "regenerate", "revive", "resurrect", "mend");

            // Element / status — DoT and effect prose only
            CreateGroup("fire", "fire", false,
                "fire", "flame", "burn", "burning", "blaze", "inferno", "ignite");

            CreateGroup("ice", "ice", false,
                "ice", "frost", "freeze", "frozen", "blizzard");

            CreateGroup("poison", "poison", false,
                "poison", "toxic", "venom", "acid", "plague");

            CreateGroup("status", "status", false,
                "stun", "stunned", "paralyze", "paralyzed", "charm", "charmed", "fear", "feared",
                "confuse", "confused", "blind", "blinded", "silence", "silenced", "slow", "slowed");

            // Miss outcomes (keep rare; identity "miss" action name is skipped by narrative emphasis)
            CreateGroup("miss", "damage", false,
                "miss", "misses", "missed");
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
