using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Data
{
    /// <summary>
    /// Data structure for a single color palette entry
    /// Can reference a color code (e.g., "R", "G"), another palette color, or direct RGB values
    /// </summary>
    public class ColorPaletteData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        
        /// <summary>
        /// Direct RGB values [r, g, b]. Used if colorCode and reference are not specified.
        /// </summary>
        [JsonPropertyName("rgb")]
        public int[]? Rgb { get; set; }
        
        /// <summary>
        /// Hex color value (for documentation/reference). Not used for color resolution.
        /// </summary>
        [JsonPropertyName("hex")]
        public string? Hex { get; set; }
        
        /// <summary>
        /// Category for organization (e.g., "combat", "rarity", "ui"). Not used for color resolution.
        /// </summary>
        [JsonPropertyName("category")]
        public string? Category { get; set; }
        
        /// <summary>
        /// Reference to a color code from ColorCodes.json (e.g., "R", "G", "r", "g").
        /// Takes precedence over rgb if specified.
        /// </summary>
        [JsonPropertyName("colorCode")]
        public string? ColorCode { get; set; }
        
        /// <summary>
        /// Reference to another ColorPalette enum name (e.g., "Red", "Damage").
        /// Takes precedence over rgb and colorCode if specified.
        /// </summary>
        [JsonPropertyName("reference")]
        public string? Reference { get; set; }
    }
    
    /// <summary>
    /// Root configuration structure for ColorPalette.json
    /// </summary>
    public class ColorPaletteConfig
    {
        [JsonPropertyName("$schema")]
        public string? Schema { get; set; }
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("version")]
        public string? Version { get; set; }
        
        [JsonPropertyName("paletteColors")]
        public List<ColorPaletteData> PaletteColors { get; set; } = new List<ColorPaletteData>();
    }
    
    /// <summary>
    /// Loads color palette definitions from GameData/ColorPalette.json
    /// Provides efficient dictionary lookup for ColorPalette enum to Color conversion
    /// </summary>
    public static class ColorPaletteLoader
    {
        private static ColorPaletteConfig? _cachedConfig;
        private static Dictionary<ColorPalette, Color>? _colorCache;
        private static bool _isLoaded = false;
        
        /// <summary>
        /// Loads color palette configuration from GameData/ColorPalette.json
        /// First tries unified ColorConfiguration.json, then falls back to individual file
        /// </summary>
        private static ColorPaletteConfig LoadColorPalette()
        {
            if (_isLoaded && _cachedConfig != null)
            {
                return _cachedConfig;
            }
            
            // Try to load from unified configuration first
            var unifiedConfig = ColorConfigurationLoader.LoadColorConfiguration();
            if (unifiedConfig.ColorPalette?.PaletteColors != null && unifiedConfig.ColorPalette.PaletteColors.Count > 0)
            {
                // Convert unified config to ColorPaletteConfig format
                _cachedConfig = new ColorPaletteConfig
                {
                    PaletteColors = unifiedConfig.ColorPalette.PaletteColors.Select(p => new ColorPaletteData
                    {
                        Name = p.Name,
                        Rgb = p.Rgb,
                        Hex = p.Hex,
                        Category = p.Category,
                        ColorCode = p.ColorCode,
                        Reference = p.Reference
                    }).ToList()
                };
                BuildColorCache();
                _isLoaded = true;
                return _cachedConfig;
            }
            
            // Fallback to individual JSON file
            var filePath = JsonLoader.FindGameDataFile("ColorPalette.json");
            if (filePath == null)
            {
                ErrorHandler.LogWarning("ColorPalette.json not found. Using fallback color palette.", "ColorPaletteLoader");
                _cachedConfig = CreateFallbackConfig();
                BuildColorCache();
                _isLoaded = true;
                return _cachedConfig;
            }
            
            _cachedConfig = JsonLoader.LoadJson<ColorPaletteConfig>(filePath, true, CreateFallbackConfig());
            BuildColorCache();
            _isLoaded = true;
            return _cachedConfig;
        }
        
        /// <summary>
        /// Creates fallback configuration with default color palette values
        /// Used when ColorPalette.json is not found
        /// </summary>
        private static ColorPaletteConfig CreateFallbackConfig()
        {
            return new ColorPaletteConfig
            {
                PaletteColors = new List<ColorPaletteData>
                {
                    // Basic colors
                    new ColorPaletteData { Name = "White", Rgb = new[] { 255, 255, 255 } },
                    new ColorPaletteData { Name = "Black", Rgb = new[] { 0, 0, 0 } },
                    new ColorPaletteData { Name = "Gray", Rgb = new[] { 128, 128, 128 } },
                    new ColorPaletteData { Name = "DarkGray", Rgb = new[] { 64, 64, 64 } },
                    new ColorPaletteData { Name = "LightGray", Rgb = new[] { 211, 211, 211 } },
                    
                    // Primary colors
                    new ColorPaletteData { Name = "Red", Rgb = new[] { 255, 0, 0 } },
                    new ColorPaletteData { Name = "Green", Rgb = new[] { 0, 128, 0 } },
                    new ColorPaletteData { Name = "Blue", Rgb = new[] { 0, 0, 255 } },
                    new ColorPaletteData { Name = "Yellow", Rgb = new[] { 255, 255, 0 } },
                    new ColorPaletteData { Name = "Cyan", Rgb = new[] { 0, 255, 255 } },
                    new ColorPaletteData { Name = "Magenta", Rgb = new[] { 255, 0, 255 } },
                    
                    // Dark variants
                    new ColorPaletteData { Name = "DarkRed", Rgb = new[] { 139, 0, 0 } },
                    new ColorPaletteData { Name = "DarkGreen", Rgb = new[] { 0, 100, 0 } },
                    new ColorPaletteData { Name = "DarkBlue", Rgb = new[] { 0, 0, 139 } },
                    new ColorPaletteData { Name = "DarkYellow", Rgb = new[] { 184, 134, 11 } },
                    new ColorPaletteData { Name = "DarkCyan", Rgb = new[] { 0, 139, 139 } },
                    new ColorPaletteData { Name = "DarkMagenta", Rgb = new[] { 139, 0, 139 } },
                    
                    // Game-specific colors
                    new ColorPaletteData { Name = "Gold", Rgb = new[] { 207, 192, 65 } },
                    new ColorPaletteData { Name = "Silver", Rgb = new[] { 192, 192, 192 } },
                    new ColorPaletteData { Name = "Bronze", Rgb = new[] { 205, 127, 50 } },
                    new ColorPaletteData { Name = "Orange", Rgb = new[] { 255, 165, 0 } },
                    new ColorPaletteData { Name = "Purple", Rgb = new[] { 128, 0, 128 } },
                    new ColorPaletteData { Name = "Pink", Rgb = new[] { 255, 192, 203 } },
                    new ColorPaletteData { Name = "Brown", Rgb = new[] { 165, 42, 42 } },
                    new ColorPaletteData { Name = "Lime", Rgb = new[] { 0, 255, 0 } },
                    new ColorPaletteData { Name = "Navy", Rgb = new[] { 0, 0, 128 } },
                    new ColorPaletteData { Name = "Teal", Rgb = new[] { 0, 128, 128 } },
                    
                    // Status colors
                    new ColorPaletteData { Name = "Success", Rgb = new[] { 0, 128, 0 } },
                    new ColorPaletteData { Name = "Warning", Rgb = new[] { 255, 165, 0 } },
                    new ColorPaletteData { Name = "Error", Rgb = new[] { 220, 20, 60 } },
                    new ColorPaletteData { Name = "Info", Rgb = new[] { 0, 191, 255 } },
                    
                    // Combat colors
                    new ColorPaletteData { Name = "Damage", Rgb = new[] { 220, 20, 60 } },
                    new ColorPaletteData { Name = "Healing", Rgb = new[] { 0, 255, 127 } },
                    new ColorPaletteData { Name = "Critical", Rgb = new[] { 255, 0, 0 } },
                    new ColorPaletteData { Name = "Miss", Rgb = new[] { 128, 128, 128 } },
                    new ColorPaletteData { Name = "Block", Rgb = new[] { 0, 191, 255 } },
                    new ColorPaletteData { Name = "Dodge", Rgb = new[] { 255, 255, 0 } },
                    
                    // Rarity colors
                    new ColorPaletteData { Name = "Common", Rgb = new[] { 255, 255, 255 } },
                    new ColorPaletteData { Name = "Uncommon", Rgb = new[] { 0, 255, 0 } },
                    new ColorPaletteData { Name = "Rare", Rgb = new[] { 0, 191, 255 } },
                    new ColorPaletteData { Name = "Epic", Rgb = new[] { 128, 0, 128 } },
                    new ColorPaletteData { Name = "Legendary", Rgb = new[] { 255, 215, 0 } },
                    
                    // UI colors
                    new ColorPaletteData { Name = "Background", Rgb = new[] { 25, 25, 25 } },
                    new ColorPaletteData { Name = "Foreground", Rgb = new[] { 255, 255, 255 } },
                    new ColorPaletteData { Name = "Border", Rgb = new[] { 64, 64, 64 } },
                    new ColorPaletteData { Name = "Highlight", Rgb = new[] { 255, 255, 0 } },
                    new ColorPaletteData { Name = "Disabled", Rgb = new[] { 128, 128, 128 } },
                    
                    // Actor colors
                    new ColorPaletteData { Name = "Player", Rgb = new[] { 0, 255, 255 } },
                    new ColorPaletteData { Name = "Enemy", Rgb = new[] { 255, 0, 0 } },
                    new ColorPaletteData { Name = "NPC", Rgb = new[] { 0, 255, 0 } },
                    new ColorPaletteData { Name = "Boss", Rgb = new[] { 128, 0, 128 } },
                    new ColorPaletteData { Name = "Minion", Rgb = new[] { 255, 165, 0 } },
                    
                    // Item type colors
                    new ColorPaletteData { Name = "Weapon", Rgb = new[] { 192, 192, 192 } },
                    new ColorPaletteData { Name = "Armor", Rgb = new[] { 0, 0, 255 } },
                    new ColorPaletteData { Name = "Potion", Rgb = new[] { 0, 128, 0 } },
                    new ColorPaletteData { Name = "Scroll", Rgb = new[] { 255, 0, 255 } }
                }
            };
        }
        
        /// <summary>
        /// Builds the color cache dictionary from loaded configuration
        /// Resolves references to color codes and other palette colors
        /// </summary>
        private static void BuildColorCache()
        {
            _colorCache = new Dictionary<ColorPalette, Color>();
            
            if (_cachedConfig?.PaletteColors == null)
                return;
            
            // First pass: Build initial cache with direct RGB values and color code references
            foreach (var paletteColor in _cachedConfig.PaletteColors)
            {
                if (string.IsNullOrEmpty(paletteColor.Name))
                    continue;
                
                // Try to parse the enum name
                if (!Enum.TryParse<ColorPalette>(paletteColor.Name, true, out var paletteEnum))
                {
                    ErrorHandler.LogWarning($"Unknown ColorPalette enum value: {paletteColor.Name}", "ColorPaletteLoader");
                    continue;
                }
                
                Color color = Colors.White;
                
                // Priority 1: Reference to another palette color (will resolve in second pass)
                if (!string.IsNullOrEmpty(paletteColor.Reference))
                {
                    // Skip for now, will resolve in second pass
                    continue;
                }
                
                // Priority 2: Reference to a color code
                if (!string.IsNullOrEmpty(paletteColor.ColorCode))
                {
                    color = ColorCodeLoader.GetColor(paletteColor.ColorCode);
                }
                // Priority 3: Direct RGB values
                else if (paletteColor.Rgb != null && paletteColor.Rgb.Length == 3)
                {
                    // Clamp RGB values to valid range [0, 255]
                    int r = Math.Clamp(paletteColor.Rgb[0], 0, 255);
                    int g = Math.Clamp(paletteColor.Rgb[1], 0, 255);
                    int b = Math.Clamp(paletteColor.Rgb[2], 0, 255);
                    color = Color.FromRgb((byte)r, (byte)g, (byte)b);
                }
                else
                {
                    // No valid color definition, skip
                    continue;
                }
                
                _colorCache[paletteEnum] = color;
            }
            
            // Second pass: Resolve references to other palette colors
            foreach (var paletteColor in _cachedConfig.PaletteColors)
            {
                if (string.IsNullOrEmpty(paletteColor.Name) || string.IsNullOrEmpty(paletteColor.Reference))
                    continue;
                
                if (!Enum.TryParse<ColorPalette>(paletteColor.Name, true, out var paletteEnum))
                    continue;
                
                // Try to resolve the reference
                if (Enum.TryParse<ColorPalette>(paletteColor.Reference, true, out var referencedEnum))
                {
                    // Check if the referenced color is already in cache
                    if (_colorCache.TryGetValue(referencedEnum, out var referencedColor))
                    {
                        _colorCache[paletteEnum] = referencedColor;
                    }
                    else
                    {
                        ErrorHandler.LogWarning($"ColorPalette '{paletteColor.Name}' references '{paletteColor.Reference}' which is not yet defined or resolved.", "ColorPaletteLoader");
                    }
                }
                else
                {
                    ErrorHandler.LogWarning($"ColorPalette '{paletteColor.Name}' references unknown palette color '{paletteColor.Reference}'.", "ColorPaletteLoader");
                }
            }
        }
        
        /// <summary>
        /// Gets a color for a ColorPalette enum value
        /// Returns Colors.White if palette not found
        /// First tries unified ColorConfiguration.json, then falls back to individual file
        /// </summary>
        public static Color GetColor(ColorPalette palette)
        {
            // Try unified configuration first
            var unifiedColor = ColorConfigurationLoader.GetPaletteColor(palette);
            if (unifiedColor != Colors.White)
            {
                return unifiedColor;
            }
            
            // Fallback to individual file loading
            LoadColorPalette(); // Ensure palette is loaded
            
            if (_colorCache == null)
                return Colors.White;
            
            return _colorCache.TryGetValue(palette, out var color) ? color : Colors.White;
        }
        
        /// <summary>
        /// Checks if a palette color exists in the loaded configuration
        /// </summary>
        public static bool HasColor(ColorPalette palette)
        {
            LoadColorPalette(); // Ensure palette is loaded
            
            return _colorCache?.ContainsKey(palette) ?? false;
        }
        
        /// <summary>
        /// Gets all available palette colors
        /// </summary>
        public static IEnumerable<ColorPalette> GetAllColors()
        {
            LoadColorPalette(); // Ensure palette is loaded
            
            if (_colorCache == null)
                return new List<ColorPalette>();
            
            return _colorCache.Keys;
        }
        
        /// <summary>
        /// Reloads the color palette configuration from disk
        /// </summary>
        public static void Reload()
        {
            _isLoaded = false;
            _cachedConfig = null;
            _colorCache = null;
            JsonLoader.ClearCacheForFile("ColorPalette.json");
        }
    }
}

