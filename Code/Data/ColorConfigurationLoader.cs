using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Linq;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Data
{
    /// <summary>
    /// Data structures for unified color configuration
    /// Note: ColorCodeData, ColorPaletteData, ColorTemplateData, and KeywordGroupData
    /// are defined in their respective loader files and reused here.
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
    /// </summary>
    public static class ColorConfigurationLoader
    {
        private static ColorConfigurationData? _cachedConfig;
        private static bool _isLoaded = false;
        private static bool _isBuilding = false; // Flag to prevent recursion during cache building
        
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
            
            // Prevent recursion if we're already building
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
                BuildColorCodeCache();
                BuildPaletteColorCache();
                BuildTemplateCache();
                BuildDungeonThemeCache();
                BuildColorPatternCache();
                
                _isLoaded = true;
                return _cachedConfig;
            }
            finally
            {
                _isBuilding = false;
            }
        }
        
        private static void BuildColorCodeCache()
        {
            _colorCodeCache = new Dictionary<string, Color>(StringComparer.Ordinal);
            
            if (_cachedConfig?.ColorCodes == null)
                return;
            
            foreach (var colorCode in _cachedConfig.ColorCodes)
            {
                if (string.IsNullOrEmpty(colorCode.Code) || colorCode.Rgb == null || colorCode.Rgb.Length != 3)
                    continue;
                
                int r = Math.Clamp(colorCode.Rgb[0], 0, 255);
                int g = Math.Clamp(colorCode.Rgb[1], 0, 255);
                int b = Math.Clamp(colorCode.Rgb[2], 0, 255);
                
                _colorCodeCache[colorCode.Code] = Color.FromRgb((byte)r, (byte)g, (byte)b);
            }
        }
        
        private static void BuildPaletteColorCache()
        {
            _paletteColorCache = new Dictionary<ColorPalette, Color>();
            
            if (_cachedConfig?.ColorPalette?.PaletteColors == null)
                return;
            
            // First pass: Build initial cache with direct RGB values and color code references
            foreach (var paletteColor in _cachedConfig.ColorPalette.PaletteColors)
            {
                if (string.IsNullOrEmpty(paletteColor.Name))
                    continue;
                
                if (!Enum.TryParse<ColorPalette>(paletteColor.Name, true, out var paletteEnum))
                {
                    ErrorHandler.LogWarning($"Unknown ColorPalette enum value: {paletteColor.Name}", "ColorConfigurationLoader");
                    continue;
                }
                
                Color color = Colors.White;
                
                // Priority 1: Reference to another palette color (will resolve in second pass)
                if (!string.IsNullOrEmpty(paletteColor.Reference))
                {
                    continue; // Skip for now, will resolve in second pass
                }
                
                // Priority 2: Reference to a color code
                if (!string.IsNullOrEmpty(paletteColor.ColorCode))
                {
                    color = GetColorCode(paletteColor.ColorCode);
                }
                // Priority 3: Direct RGB values
                else if (paletteColor.Rgb != null && paletteColor.Rgb.Length == 3)
                {
                    int r = Math.Clamp(paletteColor.Rgb[0], 0, 255);
                    int g = Math.Clamp(paletteColor.Rgb[1], 0, 255);
                    int b = Math.Clamp(paletteColor.Rgb[2], 0, 255);
                    color = Color.FromRgb((byte)r, (byte)g, (byte)b);
                }
                else
                {
                    continue;
                }
                
                _paletteColorCache[paletteEnum] = color;
            }
            
            // Second pass: Resolve references to other palette colors
            foreach (var paletteColor in _cachedConfig.ColorPalette.PaletteColors)
            {
                if (string.IsNullOrEmpty(paletteColor.Name) || string.IsNullOrEmpty(paletteColor.Reference))
                    continue;
                
                if (!Enum.TryParse<ColorPalette>(paletteColor.Name, true, out var paletteEnum))
                    continue;
                
                if (Enum.TryParse<ColorPalette>(paletteColor.Reference, true, out var referencedEnum))
                {
                    if (_paletteColorCache.TryGetValue(referencedEnum, out var referencedColor))
                    {
                        _paletteColorCache[paletteEnum] = referencedColor;
                    }
                    else
                    {
                        ErrorHandler.LogWarning($"ColorPalette '{paletteColor.Name}' references '{paletteColor.Reference}' which is not yet defined or resolved.", "ColorConfigurationLoader");
                    }
                }
                else
                {
                    ErrorHandler.LogWarning($"ColorPalette '{paletteColor.Name}' references unknown palette color '{paletteColor.Reference}'.", "ColorConfigurationLoader");
                }
            }
        }
        
        private static void BuildTemplateCache()
        {
            _templateCache = new Dictionary<string, ColorTemplateData>(StringComparer.OrdinalIgnoreCase);
            
            if (_cachedConfig?.ColorTemplates == null)
                return;
            
            foreach (var template in _cachedConfig.ColorTemplates)
            {
                if (!string.IsNullOrEmpty(template.Name))
                {
                    _templateCache[template.Name] = template;
                }
            }
        }
        
        private static void BuildDungeonThemeCache()
        {
            _dungeonThemeCache = new Dictionary<string, DungeonThemeData>(StringComparer.OrdinalIgnoreCase);
            
            if (_cachedConfig?.DungeonThemes == null)
                return;
            
            foreach (var theme in _cachedConfig.DungeonThemes)
            {
                if (!string.IsNullOrEmpty(theme.Name))
                {
                    _dungeonThemeCache[theme.Name] = theme;
                }
            }
        }
        
        private static void BuildColorPatternCache()
        {
            _colorPatternCache = new Dictionary<string, ColorPalette>(StringComparer.OrdinalIgnoreCase);
            
            if (_cachedConfig?.ColorPatterns == null)
                return;
            
            foreach (var pattern in _cachedConfig.ColorPatterns)
            {
                if (string.IsNullOrEmpty(pattern.Name) || string.IsNullOrEmpty(pattern.ColorPalette))
                    continue;
                
                if (Enum.TryParse<ColorPalette>(pattern.ColorPalette, true, out var palette))
                {
                    _colorPatternCache[pattern.Name] = palette;
                }
                else
                {
                    ErrorHandler.LogWarning($"ColorPattern '{pattern.Name}' references unknown ColorPalette '{pattern.ColorPalette}'.", "ColorConfigurationLoader");
                }
            }
        }
        
        /// <summary>
        /// Gets a color for a color code (e.g., "R", "r", "G")
        /// Falls back to ColorCodeLoader if not in unified config
        /// </summary>
        public static Color GetColorCode(string colorCode)
        {
            if (string.IsNullOrEmpty(colorCode))
                return Colors.White;
            
            // If we're building, just check the cache directly without triggering a reload
            if (_isBuilding)
            {
                if (_colorCodeCache != null && _colorCodeCache.TryGetValue(colorCode, out var cachedColor))
                {
                    return cachedColor;
                }
                // During building, if not in cache yet, return white to avoid recursion
                return Colors.White;
            }
            
            LoadColorConfiguration();
            
            if (_colorCodeCache != null && _colorCodeCache.TryGetValue(colorCode, out var resultColor))
            {
                return resultColor;
            }
            
            // Fallback to individual loader
            return ColorCodeLoader.GetColor(colorCode);
        }
        
        /// <summary>
        /// Gets a color for a ColorPalette enum value
        /// Falls back to ColorPaletteLoader if not in unified config
        /// </summary>
        public static Color GetPaletteColor(ColorPalette palette)
        {
            LoadColorConfiguration();
            
            if (_paletteColorCache != null && _paletteColorCache.TryGetValue(palette, out var color))
            {
                return color;
            }
            
            // Fallback to individual loader
            return ColorPaletteLoader.GetColor(palette);
        }
        
        /// <summary>
        /// Gets a color template by name (case-insensitive)
        /// Falls back to ColorTemplateLoader if not in unified config
        /// </summary>
        public static ColorTemplateData? GetTemplate(string templateName)
        {
            if (string.IsNullOrEmpty(templateName))
                return null;
            
            LoadColorConfiguration();
            
            if (_templateCache != null && _templateCache.TryGetValue(templateName, out var template))
            {
                return template;
            }
            
            // Fallback to individual loader
            return ColorTemplateLoader.GetTemplate(templateName);
        }
        
        /// <summary>
        /// Gets dungeon theme color data by theme name
        /// Returns null if theme not found
        /// </summary>
        public static DungeonThemeData? GetDungeonTheme(string theme)
        {
            if (string.IsNullOrEmpty(theme))
                return null;
            
            LoadColorConfiguration();
            
            if (_dungeonThemeCache != null && _dungeonThemeCache.TryGetValue(theme, out var themeData))
            {
                return themeData;
            }
            
            return null;
        }
        
        /// <summary>
        /// Gets dungeon theme color (RGB) by theme name
        /// Falls back to hard-coded values if not in unified config
        /// </summary>
        public static Color GetDungeonThemeColor(string theme)
        {
            var themeData = GetDungeonTheme(theme);
            if (themeData != null && themeData.Rgb != null && themeData.Rgb.Length == 3)
            {
                int r = Math.Clamp(themeData.Rgb[0], 0, 255);
                int g = Math.Clamp(themeData.Rgb[1], 0, 255);
                int b = Math.Clamp(themeData.Rgb[2], 0, 255);
                return Color.FromRgb((byte)r, (byte)g, (byte)b);
            }
            
            // Fallback to hard-coded values (will be replaced when DungeonThemeColors.cs is updated)
            return ColorPalette.White.GetColor();
        }
        
        /// <summary>
        /// Gets color pattern mapping (pattern name to ColorPalette)
        /// Falls back to hard-coded values if not in unified config
        /// </summary>
        public static ColorPalette GetColorPattern(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return ColorPalette.White;
            
            LoadColorConfiguration();
            
            var normalizedPattern = pattern.ToLowerInvariant().Trim();
            
            if (_colorPatternCache != null && _colorPatternCache.TryGetValue(normalizedPattern, out var palette))
            {
                return palette;
            }
            
            // Fallback to hard-coded values (will be replaced when ColorPatterns.cs is updated)
            return ColorPalette.White;
        }
        
        /// <summary>
        /// Gets entity default color palette name
        /// </summary>
        public static string GetEntityDefault(string entityType)
        {
            LoadColorConfiguration();
            
            if (_cachedConfig?.EntityDefaults == null)
                return "White";
            
            return entityType.ToLowerInvariant() switch
            {
                "player" => _cachedConfig.EntityDefaults.Player,
                "enemy" => _cachedConfig.EntityDefaults.Enemy,
                "boss" => _cachedConfig.EntityDefaults.Boss,
                "npc" => _cachedConfig.EntityDefaults.NPC,
                _ => "White"
            };
        }
        
        /// <summary>
        /// Gets all keyword groups from unified config
        /// Falls back to KeywordColorLoader if not in unified config
        /// </summary>
        public static List<KeywordGroupData> GetKeywordGroups()
        {
            LoadColorConfiguration();
            
            if (_cachedConfig?.KeywordGroups != null)
            {
                return _cachedConfig.KeywordGroups;
            }
            
            // Fallback to individual loader
            var config = KeywordColorLoader.LoadKeywordColors();
            if (config.Groups != null)
            {
                return config.Groups.Select(g => new KeywordGroupData
                {
                    Name = g.Name,
                    ColorPattern = g.ColorPattern,
                    CaseSensitive = g.CaseSensitive,
                    Keywords = g.Keywords
                }).ToList();
            }
            
            return new List<KeywordGroupData>();
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
            JsonLoader.ClearCacheForFile("ColorConfiguration.json");
        }
    }
}

