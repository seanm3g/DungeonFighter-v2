using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Data
{
    /// <summary>
    /// Data structures for unified color configuration
    /// </summary>
    
    public class DungeonThemeData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        
        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }
        
        [JsonPropertyName("colorTemplate")]
        public string? ColorTemplate { get; set; }
        
        [JsonPropertyName("rgb")]
        public int[]? Rgb { get; set; }
        
        [JsonPropertyName("hex")]
        public string? Hex { get; set; }
        
        [JsonPropertyName("colorCode")]
        public string? ColorCode { get; set; }
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
    
    public class ColorPatternData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        
        [JsonPropertyName("colorPalette")]
        public string ColorPalette { get; set; } = "";
        
        [JsonPropertyName("category")]
        public string? Category { get; set; }
    }
    
    public class EntityDefaultsData
    {
        [JsonPropertyName("player")]
        public string Player { get; set; } = "Player";
        
        [JsonPropertyName("enemy")]
        public string Enemy { get; set; } = "Enemy";
        
        [JsonPropertyName("boss")]
        public string Boss { get; set; } = "Boss";
        
        [JsonPropertyName("npc")]
        public string NPC { get; set; } = "NPC";
    }
    
    public class ColorConfigurationData
    {
        [JsonPropertyName("$schema")]
        public string? Schema { get; set; }
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("version")]
        public string? Version { get; set; }
        
        [JsonPropertyName("colorCodes")]
        public List<ColorCodeData>? ColorCodes { get; set; }
        
        [JsonPropertyName("colorPalette")]
        public ColorPaletteSection? ColorPalette { get; set; }
        
        [JsonPropertyName("colorTemplates")]
        public List<ColorTemplateData>? ColorTemplates { get; set; }
        
        [JsonPropertyName("keywordGroups")]
        public List<KeywordGroupData>? KeywordGroups { get; set; }
        
        [JsonPropertyName("dungeonThemes")]
        public List<DungeonThemeData>? DungeonThemes { get; set; }
        
        [JsonPropertyName("colorPatterns")]
        public List<ColorPatternData>? ColorPatterns { get; set; }
        
        [JsonPropertyName("entityDefaults")]
        public EntityDefaultsData? EntityDefaults { get; set; }
    }
    
    public class ColorPaletteSection
    {
        [JsonPropertyName("paletteColors")]
        public List<ColorPaletteData>? PaletteColors { get; set; }
    }
    
    /// <summary>
    /// Loads unified color configuration from GameData/ColorConfiguration.json
    /// Provides access to all color subsystems with backward compatibility
    /// Facade coordinating ColorCacheBuilder and ColorResolver
    /// </summary>
    public static class ColorConfigurationLoader
    {
        private static readonly object _loadLock = new object();
        private static ColorConfigurationData? _cachedConfig;
        private static bool _isLoaded = false;
        private static bool _isBuilding = false;
        private static bool _inFallback = false;
        
        // Caches for fast lookup
        private static Dictionary<string, Color>? _colorCodeCache;
        private static Dictionary<ColorPalette, Color>? _paletteColorCache;
        private static Dictionary<string, ColorTemplateData>? _templateCache;
        private static Dictionary<string, DungeonThemeData>? _dungeonThemeCache;
        private static Dictionary<string, ColorPalette>? _colorPatternCache;
        
        /// <summary>
        /// Loads unified color configuration from GameData/ColorConfiguration.json
        /// Falls back to individual JSON files if unified config not found
        /// </summary>
        public static ColorConfigurationData LoadColorConfiguration()
        {
            if (_isLoaded && _cachedConfig != null)
            {
                return _cachedConfig;
            }

            lock (_loadLock)
            {
                if (_isLoaded && _cachedConfig != null)
                {
                    return _cachedConfig;
                }

                if (_isBuilding)
                {
                    return _cachedConfig ?? new ColorConfigurationData();
                }

                var filePath = JsonLoader.FindGameDataFile("ColorConfiguration.json");
                if (filePath == null)
                {
                    ErrorHandler.LogWarning("ColorConfiguration.json not found. Using individual JSON files as fallback.", "ColorConfigurationLoader");
                    _cachedConfig = new ColorConfigurationData();
                    _isLoaded = true;
                    return _cachedConfig;
                }

                _isBuilding = true;
                try
                {
                    _cachedConfig = JsonLoader.LoadJson<ColorConfigurationData>(filePath, true, new ColorConfigurationData());

                    // Build caches for fast lookup
                    _colorCodeCache = ColorCacheBuilder.BuildColorCodeCache(_cachedConfig);
                    _paletteColorCache = ColorCacheBuilder.BuildPaletteColorCache(_cachedConfig, _colorCodeCache);
                    _templateCache = ColorCacheBuilder.BuildTemplateCache(_cachedConfig);
                    _dungeonThemeCache = ColorCacheBuilder.BuildDungeonThemeCache(_cachedConfig);
                    _colorPatternCache = ColorCacheBuilder.BuildColorPatternCache(_cachedConfig);

                    _isLoaded = true;
                    return _cachedConfig;
                }
                finally
                {
                    _isBuilding = false;
                }
            }
        }
        
        public static Color GetColorCode(string colorCode)
        {
            LoadColorConfiguration();
            return ColorResolver.GetColorCode(colorCode, _colorCodeCache, _isBuilding, _inFallback, ref _inFallback);
        }
        
        public static Color GetPaletteColor(ColorPalette palette)
        {
            LoadColorConfiguration();
            return ColorResolver.GetPaletteColor(palette, _paletteColorCache);
        }
        
        public static ColorTemplateData? GetTemplate(string templateName)
        {
            LoadColorConfiguration();
            return ColorResolver.GetTemplate(templateName, _templateCache);
        }
        
        public static DungeonThemeData? GetDungeonTheme(string theme)
        {
            LoadColorConfiguration();
            return ColorResolver.GetDungeonTheme(theme, _dungeonThemeCache);
        }
        
        public static Color GetDungeonThemeColor(string theme)
        {
            LoadColorConfiguration();
            return ColorResolver.GetDungeonThemeColor(theme, _dungeonThemeCache);
        }
        
        public static ColorPalette GetColorPattern(string pattern)
        {
            LoadColorConfiguration();
            return ColorResolver.GetColorPattern(pattern, _colorPatternCache);
        }
        
        public static string GetEntityDefault(string entityType)
        {
            LoadColorConfiguration();
            return ColorResolver.GetEntityDefault(entityType, _cachedConfig);
        }
        
        public static List<KeywordGroupData> GetKeywordGroups()
        {
            LoadColorConfiguration();
            return ColorResolver.GetKeywordGroups(_cachedConfig);
        }
        
        /// <summary>
        /// Reloads the unified color configuration from disk
        /// </summary>
        public static void Reload()
        {
            _isLoaded = false;
            _cachedConfig = null;
            _colorCodeCache = null;
            _paletteColorCache = null;
            _templateCache = null;
            _dungeonThemeCache = null;
            _colorPatternCache = null;
        }
    }
}
