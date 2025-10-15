using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RPGGame.Data;

namespace RPGGame.UI
{
    /// <summary>
    /// Manages keyword groups and their associated color patterns
    /// Automatically applies colors to keywords in text using Caves of Qud-style markup
    /// Loads configuration from GameData/KeywordColorGroups.json
    /// </summary>
    public static class KeywordColorSystem
    {
        // Keyword groups with their associated color patterns
        private static readonly Dictionary<string, KeywordGroup> keywordGroups = new();
        private static bool _isInitialized = false;

        static KeywordColorSystem()
        {
            Initialize();
        }
        
        /// <summary>
        /// Initializes the keyword color system by loading from config file
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }
            
            try
            {
                // Load keyword groups from JSON config
                KeywordColorLoader.LoadAndRegisterKeywordGroups();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex, "KeywordColorSystem.Initialize", "Failed to load keyword groups from config, using hardcoded defaults");
                // Fall back to hardcoded defaults if loading fails
                InitializeDefaultGroups();
                _isInitialized = true;
            }
        }

        /// <summary>
        /// Represents a group of keywords with an associated color pattern
        /// </summary>
        public class KeywordGroup
        {
            public string Name { get; set; } = "";
            public HashSet<string> Keywords { get; set; } = new();
            public string ColorPattern { get; set; } = ""; // Color code or template name
            public bool CaseSensitive { get; set; } = false;

            public KeywordGroup(string name, string colorPattern, bool caseSensitive = false)
            {
                Name = name;
                ColorPattern = colorPattern;
                CaseSensitive = caseSensitive;
            }

            public void AddKeywords(params string[] words)
            {
                foreach (var word in words)
                {
                    Keywords.Add(CaseSensitive ? word : word.ToLower());
                }
            }
        }

        /// <summary>
        /// Initializes hardcoded default keyword groups for the game
        /// Only used as a fallback if JSON loading fails
        /// </summary>
        private static void InitializeDefaultGroups()
        {
            // Damage and combat keywords
            var damageGroup = new KeywordGroup("damage", "damage");
            damageGroup.AddKeywords("damage", "hit", "strike", "attack", "slash", "pierce", "crush", "wound", "injure");
            keywordGroups["damage"] = damageGroup;

            // Critical hit keywords
            var criticalGroup = new KeywordGroup("critical", "critical");
            criticalGroup.AddKeywords("critical", "crit", "devastating", "crushing blow", "massive hit");
            keywordGroups["critical"] = criticalGroup;

            // Healing keywords
            var healGroup = new KeywordGroup("heal", "heal");
            healGroup.AddKeywords("heal", "restore", "regenerate", "recover", "mend", "revive");
            keywordGroups["heal"] = healGroup;

            // Status effect keywords - Positive
            var buffGroup = new KeywordGroup("buff", "holy");
            buffGroup.AddKeywords("strengthen", "fortify", "enhance", "boost", "empower", "blessed");
            keywordGroups["buff"] = buffGroup;

            // Status effect keywords - Negative
            var debuffGroup = new KeywordGroup("debuff", "corrupted");
            debuffGroup.AddKeywords("weaken", "curse", "hex", "poison", "disease", "corrupt");
            keywordGroups["debuff"] = debuffGroup;

            // Poison keywords
            var poisonGroup = new KeywordGroup("poison", "poisoned");
            poisonGroup.AddKeywords("poison", "venom", "toxic", "poisoned", "toxin");
            keywordGroups["poison"] = poisonGroup;

            // Fire keywords
            var fireGroup = new KeywordGroup("fire", "fiery");
            fireGroup.AddKeywords("fire", "flame", "burn", "burning", "blaze", "inferno", "ignite", "scorch");
            keywordGroups["fire"] = fireGroup;

            // Ice keywords
            var iceGroup = new KeywordGroup("ice", "icy");
            iceGroup.AddKeywords("ice", "frost", "frozen", "freeze", "chill", "cold", "glacial");
            keywordGroups["ice"] = iceGroup;

            // Lightning keywords
            var lightningGroup = new KeywordGroup("lightning", "electric");
            lightningGroup.AddKeywords("lightning", "thunder", "electric", "shock", "zap", "bolt");
            keywordGroups["lightning"] = lightningGroup;

            // Shadow/Dark keywords
            var shadowGroup = new KeywordGroup("shadow", "shadow");
            shadowGroup.AddKeywords("shadow", "darkness", "dark", "void", "abyss", "umbral");
            keywordGroups["shadow"] = shadowGroup;

            // Holy/Light keywords
            var holyGroup = new KeywordGroup("holy", "holy");
            holyGroup.AddKeywords("holy", "divine", "sacred", "blessed", "light", "radiant", "celestial");
            keywordGroups["holy"] = holyGroup;

            // Death keywords
            var deathGroup = new KeywordGroup("death", "bloodied");
            deathGroup.AddKeywords("death", "die", "dies", "died", "killed", "slain", "defeated", "destroyed");
            keywordGroups["death"] = deathGroup;

            // Item rarity keywords
            var commonGroup = new KeywordGroup("common", "common");
            commonGroup.AddKeywords("common");
            keywordGroups["common"] = commonGroup;

            var uncommonGroup = new KeywordGroup("uncommon", "uncommon");
            uncommonGroup.AddKeywords("uncommon");
            keywordGroups["uncommon"] = uncommonGroup;

            var rareGroup = new KeywordGroup("rare", "rare");
            rareGroup.AddKeywords("rare");
            keywordGroups["rare"] = rareGroup;

            var epicGroup = new KeywordGroup("epic", "epic");
            epicGroup.AddKeywords("epic");
            keywordGroups["epic"] = epicGroup;

            var legendaryGroup = new KeywordGroup("legendary", "legendary");
            legendaryGroup.AddKeywords("legendary", "mythic", "artifact");
            keywordGroups["legendary"] = legendaryGroup;

            // Magic keywords
            var magicGroup = new KeywordGroup("magic", "arcane");
            magicGroup.AddKeywords("magic", "spell", "arcane", "mystical", "enchanted", "magical");
            keywordGroups["magic"] = magicGroup;

            // Nature keywords
            var natureGroup = new KeywordGroup("nature", "natural");
            natureGroup.AddKeywords("nature", "natural", "earth", "growth", "plant", "forest");
            keywordGroups["nature"] = natureGroup;

            // Blood keywords
            var bloodGroup = new KeywordGroup("blood", "bleeding");
            bloodGroup.AddKeywords("blood", "bleed", "bleeding", "hemorrhage", "bloodied");
            keywordGroups["blood"] = bloodGroup;

            // Stun keywords
            var stunGroup = new KeywordGroup("stun", "stunned");
            stunGroup.AddKeywords("stun", "stunned", "daze", "dazed", "paralyzed", "immobilize");
            keywordGroups["stun"] = stunGroup;

            // Gold/Currency keywords
            var goldGroup = new KeywordGroup("gold", "golden");
            goldGroup.AddKeywords("gold", "coins", "currency", "treasure", "wealth", "riches");
            keywordGroups["gold"] = goldGroup;

            // Experience/Level keywords
            var xpGroup = new KeywordGroup("experience", "white");
            xpGroup.AddKeywords("experience", "xp", "level", "level up", "leveled");
            keywordGroups["experience"] = xpGroup;

            // Enemy types
            var enemyGroup = new KeywordGroup("enemy", "red");
            enemyGroup.AddKeywords("goblin", "orc", "skeleton", "zombie", "dragon", "demon", "wraith", 
                                   "spider", "bat", "slime", "cultist", "bandit", "boss", "wolf", "bear",
                                   "treant", "elemental", "golem", "salamander", "lich", "warden", "guardian",
                                   "kobold", "soldier", "beast", "sprite", "wyrm", "sentinel", "priest",
                                   "yeti", "ghoul", "vampire", "werewolf", "minotaur", "hydra", "chimera");
            keywordGroups["enemy"] = enemyGroup;

            // Player class keywords
            var classGroup = new KeywordGroup("class", "cyan");
            classGroup.AddKeywords("warrior", "mage", "rogue", "wizard", "barbarian", "paladin", "ranger");
            keywordGroups["class"] = classGroup;

            // Combat action keywords
            var actionGroup = new KeywordGroup("action", "electric");
            actionGroup.AddKeywords("jab", "taunt", "flurry", "cleave", "shield bash", "precision strike",
                                   "momentum bash", "lucky strike", "overkill", "dance", "opening volley",
                                   "focus", "sharp edge", "blood frenzy", "berzerk", "swing for the fences",
                                   "true strike", "last grasp", "second wind", "quick reflexes", "first blood",
                                   "brutal strike", "war cry", "web strike", "poison bite", "devastating blow",
                                   "power attack", "rending slash", "execute", "backstab", "sneak attack");
            keywordGroups["action"] = actionGroup;

            // Character/Player keywords
            var characterGroup = new KeywordGroup("character", "golden");
            characterGroup.AddKeywords("you", "your", "yourself", "hero", "champion", "adventurer");
            keywordGroups["character"] = characterGroup;

            // Environment/Room keywords
            var environmentGroup = new KeywordGroup("environment", "natural");
            environmentGroup.AddKeywords("entrance", "chamber", "room", "hall", "cavern", "tunnel", "passage",
                                        "dungeon", "crypt", "tomb", "vault", "sanctuary", "grove", "clearing",
                                        "lair", "den", "nest", "pool", "lake", "cave", "cathedral", "temple",
                                        "altar", "shrine", "library", "armory", "treasury", "prison", "barracks");
            keywordGroups["environment"] = environmentGroup;

            // Location theme keywords
            var themeGroup = new KeywordGroup("theme", "crystalline");
            themeGroup.AddKeywords("forest", "lava", "frozen", "ice", "shadow", "crystal", "astral",
                                  "underground", "swamp", "storm", "volcanic", "glacial", "void",
                                  "steampunk", "mechanical", "nature", "cosmic");
            keywordGroups["theme"] = themeGroup;
        }

        /// <summary>
        /// Applies keyword coloring to text
        /// </summary>
        public static string ApplyKeywordColors(string text, params string[] groupNames)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            // If no groups specified, use all groups
            var groups = groupNames.Length > 0 
                ? groupNames.Select(n => keywordGroups.ContainsKey(n) ? keywordGroups[n] : null).Where(g => g != null)
                : keywordGroups.Values;

            string result = text;

            foreach (var group in groups)
            {
                if (group == null) continue;

                foreach (var keyword in group.Keywords)
                {
                    // Create a regex pattern for whole word matching
                    string pattern = group.CaseSensitive 
                        ? $@"\b{Regex.Escape(keyword)}\b"
                        : $@"\b{Regex.Escape(keyword)}\b";

                    RegexOptions options = group.CaseSensitive 
                        ? RegexOptions.None 
                        : RegexOptions.IgnoreCase;

                    // Apply color pattern to matched keywords
                    result = Regex.Replace(result, pattern, match =>
                    {
                        // Check if this match is already inside color markup
                        if (IsInsideColorMarkup(result, match.Index))
                        {
                            return match.Value;
                        }

                        // Apply the color pattern
                        return ColorParser.Colorize(match.Value, group.ColorPattern);
                    }, options);
                }
            }

            return result;
        }

        /// <summary>
        /// Checks if a position in text is already inside color markup
        /// </summary>
        private static bool IsInsideColorMarkup(string text, int position)
        {
            // Check for {{template|
            int lastTemplateOpen = text.LastIndexOf("{{", position);
            if (lastTemplateOpen >= 0)
            {
                int templateClose = text.IndexOf("}}", lastTemplateOpen);
                if (templateClose > position)
                {
                    return true;
                }
            }

            // Check for &X or ^X immediately before
            if (position >= 2 && (text[position - 2] == '&' || text[position - 2] == '^'))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Registers a custom keyword group
        /// </summary>
        public static void RegisterKeywordGroup(KeywordGroup group)
        {
            keywordGroups[group.Name] = group;
        }

        /// <summary>
        /// Gets a keyword group by name
        /// </summary>
        public static KeywordGroup? GetKeywordGroup(string name)
        {
            return keywordGroups.TryGetValue(name, out var group) ? group : null;
        }

        /// <summary>
        /// Lists all registered keyword groups
        /// </summary>
        public static IEnumerable<string> GetAllGroupNames()
        {
            return keywordGroups.Keys;
        }

        /// <summary>
        /// Adds keywords to an existing group
        /// </summary>
        public static void AddKeywordsToGroup(string groupName, params string[] keywords)
        {
            if (keywordGroups.TryGetValue(groupName, out var group))
            {
                group.AddKeywords(keywords);
            }
        }

        /// <summary>
        /// Creates and registers a new keyword group
        /// </summary>
        public static void CreateGroup(string name, string colorPattern, bool caseSensitive, params string[] keywords)
        {
            var group = new KeywordGroup(name, colorPattern, caseSensitive);
            group.AddKeywords(keywords);
            RegisterKeywordGroup(group);
        }

        /// <summary>
        /// Removes a keyword group
        /// </summary>
        public static void RemoveGroup(string name)
        {
            keywordGroups.Remove(name);
        }

        /// <summary>
        /// Clears all keyword groups
        /// </summary>
        public static void ClearAllGroups()
        {
            keywordGroups.Clear();
        }

        /// <summary>
        /// Resets to default groups by reloading from config
        /// </summary>
        public static void ResetToDefaults()
        {
            ClearAllGroups();
            _isInitialized = false;
            Initialize();
        }
        
        /// <summary>
        /// Reloads keyword groups from the config file
        /// </summary>
        public static void ReloadFromConfig()
        {
            KeywordColorLoader.Reload();
            ClearAllGroups();
            _isInitialized = false;
            Initialize();
        }

        /// <summary>
        /// Helper method for common use case: applying all keyword colors
        /// </summary>
        public static string Colorize(string text)
        {
            return ApplyKeywordColors(text);
        }

        /// <summary>
        /// Helper method: colorize with specific groups
        /// </summary>
        public static string ColorizeWithGroups(string text, params string[] groupNames)
        {
            return ApplyKeywordColors(text, groupNames);
        }
    }
}

