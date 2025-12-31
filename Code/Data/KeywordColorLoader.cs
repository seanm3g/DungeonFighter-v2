using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using RPGGame.UI;

namespace RPGGame.Data
{
    /// <summary>
    /// Data structure for loading keyword color groups from JSON
    /// </summary>
    public class KeywordColorConfig
    {
        [JsonPropertyName("$schema")]
        public string? Schema { get; set; }
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("groups")]
        public List<KeywordGroupData>? Groups { get; set; }
        
        [JsonPropertyName("usage")]
        public Dictionary<string, string>? Usage { get; set; }
        
        [JsonPropertyName("notes")]
        public List<string>? Notes { get; set; }
    }
    
    public class KeywordGroupData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        
        [JsonPropertyName("colorPattern")]
        public string ColorPattern { get; set; } = "";
        
        [JsonPropertyName("caseSensitive")]
        public bool CaseSensitive { get; set; } = false;
        
        [JsonPropertyName("keywords")]
        public List<string>? Keywords { get; set; }
        
        public override string ToString() => Name ?? "Unnamed";
    }
    
    /// <summary>
    /// Loads keyword color groups from KeywordColorGroups.json
    /// </summary>
    public static class KeywordColorLoader
    {
        private static KeywordColorConfig? _cachedConfig;
        private static bool _isLoaded = false;
        
        /// <summary>
        /// Loads keyword color configuration from GameData/KeywordColorGroups.json
        /// First tries unified ColorConfiguration.json, then falls back to individual file
        /// </summary>
        public static KeywordColorConfig LoadKeywordColors()
        {
            if (_isLoaded && _cachedConfig != null)
            {
                return _cachedConfig;
            }
            
            // Try to load from unified configuration first
            var unifiedGroups = ColorConfigurationLoader.GetKeywordGroups();
            if (unifiedGroups != null && unifiedGroups.Count > 0)
            {
                // Convert unified config to KeywordColorConfig format
                _cachedConfig = new KeywordColorConfig
                {
                    Groups = unifiedGroups.Select(g => new KeywordGroupData
                    {
                        Name = g.Name,
                        ColorPattern = g.ColorPattern,
                        CaseSensitive = g.CaseSensitive,
                        Keywords = g.Keywords
                    }).ToList()
                };
                _isLoaded = true;
                return _cachedConfig;
            }
            
            // Fallback to individual JSON file
            var filePath = JsonLoader.FindGameDataFile("KeywordColorGroups.json");
            if (filePath == null)
            {
                ErrorHandler.LogWarning("KeywordColorGroups.json not found. Using empty configuration.", "KeywordColorLoader");
                _cachedConfig = new KeywordColorConfig
                {
                    Groups = new List<KeywordGroupData>()
                };
                _isLoaded = true;
                return _cachedConfig;
            }
            
            _cachedConfig = JsonLoader.LoadJson<KeywordColorConfig>(filePath, true, new KeywordColorConfig
            {
                Groups = new List<KeywordGroupData>()
            });
            
            _isLoaded = true;
            return _cachedConfig;
        }
        
        /// <summary>
        /// Loads keyword groups and registers them with KeywordColorSystem
        /// </summary>
        public static void LoadAndRegisterKeywordGroups()
        {
            var config = LoadKeywordColors();
            
            if (config.Groups == null)
            {
                ErrorHandler.LogWarning("No keyword groups found in KeywordColorGroups.json", "KeywordColorLoader");
                return;
            }
            
            foreach (var groupData in config.Groups)
            {
                try
                {
                    // Add each keyword to the group using the simplified API
                    if (groupData.Keywords != null && groupData.Keywords.Count > 0)
                    {
                        foreach (var keyword in groupData.Keywords)
                        {
                            KeywordColorSystem.AddKeyword(keyword, groupData.Name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorHandler.LogError(ex, "KeywordColorLoader", $"Failed to load keyword group: {groupData.Name}");
                }
            }
        }
        
        /// <summary>
        /// Reloads the keyword color configuration from disk
        /// </summary>
        public static void Reload()
        {
            _isLoaded = false;
            _cachedConfig = null;
            JsonLoader.ClearCacheForFile("KeywordColorGroups.json");
        }
    }
}

