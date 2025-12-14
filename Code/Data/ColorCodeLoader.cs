using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Data
{
    /// <summary>
    /// Data structure for a single color code definition
    /// </summary>
    public class ColorCodeData
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = "";
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        
        [JsonPropertyName("rgb")]
        public int[] Rgb { get; set; } = new int[3];
        
        [JsonPropertyName("hex")]
        public string? Hex { get; set; }
    }
    
    /// <summary>
    /// Root configuration structure for ColorCodes.json
    /// </summary>
    public class ColorCodeConfig
    {
        [JsonPropertyName("$schema")]
        public string? Schema { get; set; }
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("version")]
        public string? Version { get; set; }
        
        [JsonPropertyName("colorCodes")]
        public List<ColorCodeData> ColorCodes { get; set; } = new List<ColorCodeData>();
    }
    
    /// <summary>
    /// Loads color code definitions from GameData/ColorCodes.json
    /// Provides efficient dictionary lookup for color code to RGB conversion
    /// </summary>
    public static class ColorCodeLoader
    {
        private static readonly object _loadLock = new object();
        private static ColorCodeConfig? _cachedConfig;
        private static Dictionary<string, Color>? _colorCodeCache;
        private static bool _isLoaded = false;
        
        /// <summary>
        /// Loads color code configuration from GameData/ColorCodes.json
        /// First tries unified ColorConfiguration.json, then falls back to individual file
        /// </summary>
        private static ColorCodeConfig LoadColorCodes()
        {
            if (_isLoaded && _cachedConfig != null)
            {
                return _cachedConfig;
            }

            lock (_loadLock)
            {
                // Double-check after acquiring lock
                if (_isLoaded && _cachedConfig != null)
                {
                    return _cachedConfig;
                }

                // Try to load from unified configuration first
                var unifiedConfig = ColorConfigurationLoader.LoadColorConfiguration();
                if (unifiedConfig.ColorCodes != null && unifiedConfig.ColorCodes.Count > 0)
                {
                    // Convert unified config to ColorCodeConfig format
                    _cachedConfig = new ColorCodeConfig
                    {
                        ColorCodes = unifiedConfig.ColorCodes.Select(c => new ColorCodeData
                        {
                            Code = c.Code,
                            Name = c.Name,
                            Rgb = c.Rgb,
                            Hex = c.Hex
                        }).ToList()
                    };
                    BuildColorCache();
                    _isLoaded = true;
                    return _cachedConfig;
                }

                // Fallback to individual JSON file
                var filePath = JsonLoader.FindGameDataFile("ColorCodes.json");
                if (filePath == null)
                {
                    ErrorHandler.LogWarning("ColorCodes.json not found. Using fallback color codes.", "ColorCodeLoader");
                    _cachedConfig = CreateFallbackConfig();
                    BuildColorCache();
                    _isLoaded = true;
                    return _cachedConfig;
                }

                _cachedConfig = JsonLoader.LoadJson<ColorCodeConfig>(filePath, true, CreateFallbackConfig());
                BuildColorCache();
                _isLoaded = true;
                return _cachedConfig;
            }
        }
        
        /// <summary>
        /// Creates fallback configuration with default color codes
        /// Used when ColorCodes.json is not found
        /// </summary>
        private static ColorCodeConfig CreateFallbackConfig()
        {
            return new ColorCodeConfig
            {
                ColorCodes = new List<ColorCodeData>
                {
                    new ColorCodeData { Code = "r", Name = "dark red", Rgb = new[] { 166, 74, 46 } },
                    new ColorCodeData { Code = "R", Name = "red", Rgb = new[] { 215, 66, 0 } },
                    new ColorCodeData { Code = "g", Name = "dark green", Rgb = new[] { 0, 148, 3 } },
                    new ColorCodeData { Code = "G", Name = "green", Rgb = new[] { 0, 196, 32 } },
                    new ColorCodeData { Code = "b", Name = "dark blue", Rgb = new[] { 0, 72, 189 } },
                    new ColorCodeData { Code = "B", Name = "blue", Rgb = new[] { 0, 150, 255 } },
                    new ColorCodeData { Code = "c", Name = "dark cyan", Rgb = new[] { 64, 164, 185 } },
                    new ColorCodeData { Code = "C", Name = "cyan", Rgb = new[] { 119, 191, 207 } },
                    new ColorCodeData { Code = "m", Name = "dark magenta", Rgb = new[] { 177, 84, 207 } },
                    new ColorCodeData { Code = "M", Name = "magenta", Rgb = new[] { 218, 91, 214 } },
                    new ColorCodeData { Code = "o", Name = "dark orange", Rgb = new[] { 241, 95, 34 } },
                    new ColorCodeData { Code = "O", Name = "orange", Rgb = new[] { 233, 159, 16 } },
                    new ColorCodeData { Code = "w", Name = "brown", Rgb = new[] { 152, 135, 95 } },
                    new ColorCodeData { Code = "W", Name = "gold", Rgb = new[] { 207, 192, 65 } },
                    new ColorCodeData { Code = "k", Name = "very dark", Rgb = new[] { 15, 59, 58 } },
                    new ColorCodeData { Code = "K", Name = "dark grey", Rgb = new[] { 21, 83, 82 } },
                    new ColorCodeData { Code = "y", Name = "grey", Rgb = new[] { 177, 201, 195 } },
                    new ColorCodeData { Code = "Y", Name = "white", Rgb = new[] { 255, 255, 255 } }
                }
            };
        }
        
        /// <summary>
        /// Builds the color cache dictionary from loaded configuration
        /// </summary>
        private static void BuildColorCache()
        {
            _colorCodeCache = new Dictionary<string, Color>(StringComparer.Ordinal);
            
            if (_cachedConfig?.ColorCodes == null)
                return;
            
            foreach (var colorCode in _cachedConfig.ColorCodes)
            {
                if (string.IsNullOrEmpty(colorCode.Code) || colorCode.Rgb == null || colorCode.Rgb.Length != 3)
                    continue;
                
                // Clamp RGB values to valid range [0, 255]
                int r = Math.Clamp(colorCode.Rgb[0], 0, 255);
                int g = Math.Clamp(colorCode.Rgb[1], 0, 255);
                int b = Math.Clamp(colorCode.Rgb[2], 0, 255);
                
                _colorCodeCache[colorCode.Code] = Color.FromRgb((byte)r, (byte)g, (byte)b);
            }
        }
        
        /// <summary>
        /// Gets a color for a color code (e.g., "R", "r", "G")
        /// Returns Colors.White if code not found
        /// This is called as a fallback from ColorConfigurationLoader, so we don't call back to it
        /// </summary>
        public static Color GetColor(string colorCode)
        {
            if (string.IsNullOrEmpty(colorCode))
                return Colors.White;
            
            // Load from individual file (don't call back to ColorConfigurationLoader to avoid circular dependency)
            // ColorConfigurationLoader.GetColorCode() already tries unified config first before calling this
            LoadColorCodes(); // Ensure codes are loaded
            
            if (_colorCodeCache == null)
                return Colors.White;
            
            return _colorCodeCache.TryGetValue(colorCode, out var color) ? color : Colors.White;
        }
        
        /// <summary>
        /// Checks if a color code exists
        /// </summary>
        public static bool HasColorCode(string colorCode)
        {
            if (string.IsNullOrEmpty(colorCode))
                return false;
            
            LoadColorCodes(); // Ensure codes are loaded
            
            return _colorCodeCache?.ContainsKey(colorCode) ?? false;
        }
        
        /// <summary>
        /// Gets all available color codes
        /// </summary>
        public static IEnumerable<string> GetAllColorCodes()
        {
            LoadColorCodes(); // Ensure codes are loaded
            
            if (_colorCodeCache == null)
                return new List<string>();
            
            return _colorCodeCache.Keys;
        }
        
        /// <summary>
        /// Reloads the color code configuration from disk
        /// </summary>
        public static void Reload()
        {
            _isLoaded = false;
            _cachedConfig = null;
            _colorCodeCache = null;
            JsonLoader.ClearCacheForFile("ColorCodes.json");
        }
    }
}

