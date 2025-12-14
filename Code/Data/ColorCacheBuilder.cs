using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Data
{
    public static class ColorCacheBuilder
    {
        public static Dictionary<string, Color> BuildColorCodeCache(ColorConfigurationData? config)
        {
            var cache = new Dictionary<string, Color>(StringComparer.Ordinal);
            
            if (config?.ColorCodes == null)
                return cache;
            
            foreach (var colorCode in config.ColorCodes)
            {
                if (string.IsNullOrEmpty(colorCode.Code) || colorCode.Rgb == null || colorCode.Rgb.Length != 3)
                    continue;
                
                int r = Math.Clamp(colorCode.Rgb[0], 0, 255);
                int g = Math.Clamp(colorCode.Rgb[1], 0, 255);
                int b = Math.Clamp(colorCode.Rgb[2], 0, 255);
                
                cache[colorCode.Code] = Color.FromRgb((byte)r, (byte)g, (byte)b);
            }

            return cache;
        }

        public static Dictionary<ColorPalette, Color> BuildPaletteColorCache(
            ColorConfigurationData? config,
            Dictionary<string, Color>? colorCodeCache)
        {
            var cache = new Dictionary<ColorPalette, Color>();
            
            if (config?.ColorPalette?.PaletteColors == null)
                return cache;
            
            // First pass: Build initial cache with direct RGB values and color code references
            foreach (var paletteColor in config.ColorPalette.PaletteColors)
            {
                if (string.IsNullOrEmpty(paletteColor.Name))
                    continue;
                
                if (!Enum.TryParse<ColorPalette>(paletteColor.Name, true, out var paletteEnum))
                {
                    ErrorHandler.LogWarning($"Unknown ColorPalette enum value: {paletteColor.Name}", "ColorCacheBuilder");
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
                    if (colorCodeCache != null && colorCodeCache.TryGetValue(paletteColor.ColorCode, out var cachedColor))
                    {
                        color = cachedColor;
                    }
                    else
                    {
                        var colorCodeData = config?.ColorCodes?.FirstOrDefault(c => 
                            string.Equals(c.Code, paletteColor.ColorCode, StringComparison.OrdinalIgnoreCase));
                        if (colorCodeData != null && colorCodeData.Rgb != null && colorCodeData.Rgb.Length == 3)
                        {
                            int r = Math.Clamp(colorCodeData.Rgb[0], 0, 255);
                            int g = Math.Clamp(colorCodeData.Rgb[1], 0, 255);
                            int b = Math.Clamp(colorCodeData.Rgb[2], 0, 255);
                            color = Color.FromRgb((byte)r, (byte)g, (byte)b);
                        }
                        else
                        {
                            color = Colors.White;
                        }
                    }
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
                
                cache[paletteEnum] = color;
            }
            
            // Second pass: Resolve references to other palette colors
            if (config?.ColorPalette?.PaletteColors == null)
                return cache;
            
            foreach (var paletteColor in config.ColorPalette.PaletteColors)
            {
                if (string.IsNullOrEmpty(paletteColor.Name) || string.IsNullOrEmpty(paletteColor.Reference))
                    continue;
                
                if (!Enum.TryParse<ColorPalette>(paletteColor.Name, true, out var paletteEnum))
                    continue;
                
                if (Enum.TryParse<ColorPalette>(paletteColor.Reference, true, out var referencedEnum))
                {
                    if (cache.TryGetValue(referencedEnum, out var referencedColor))
                    {
                        cache[paletteEnum] = referencedColor;
                    }
                    else
                    {
                        ErrorHandler.LogWarning($"ColorPalette '{paletteColor.Name}' references '{paletteColor.Reference}' which is not yet defined or resolved.", "ColorCacheBuilder");
                    }
                }
                else
                {
                    ErrorHandler.LogWarning($"ColorPalette '{paletteColor.Name}' references unknown palette color '{paletteColor.Reference}'.", "ColorCacheBuilder");
                }
            }

            return cache;
        }

        public static Dictionary<string, ColorTemplateData> BuildTemplateCache(ColorConfigurationData? config)
        {
            var cache = new Dictionary<string, ColorTemplateData>(StringComparer.OrdinalIgnoreCase);
            
            if (config?.ColorTemplates == null)
                return cache;
            
            foreach (var template in config.ColorTemplates)
            {
                if (!string.IsNullOrEmpty(template.Name))
                {
                    cache[template.Name] = template;
                }
            }

            return cache;
        }

        public static Dictionary<string, DungeonThemeData> BuildDungeonThemeCache(ColorConfigurationData? config)
        {
            var cache = new Dictionary<string, DungeonThemeData>(StringComparer.OrdinalIgnoreCase);
            
            if (config?.DungeonThemes == null)
                return cache;
            
            foreach (var theme in config.DungeonThemes)
            {
                if (!string.IsNullOrEmpty(theme.Name))
                {
                    cache[theme.Name] = theme;
                }
            }

            return cache;
        }

        public static Dictionary<string, ColorPalette> BuildColorPatternCache(ColorConfigurationData? config)
        {
            var cache = new Dictionary<string, ColorPalette>(StringComparer.OrdinalIgnoreCase);
            
            if (config?.ColorPatterns == null)
                return cache;
            
            foreach (var pattern in config.ColorPatterns)
            {
                if (string.IsNullOrEmpty(pattern.Name) || string.IsNullOrEmpty(pattern.ColorPalette))
                    continue;
                
                if (Enum.TryParse<ColorPalette>(pattern.ColorPalette, true, out var palette))
                {
                    cache[pattern.Name] = palette;
                }
                else
                {
                    ErrorHandler.LogWarning($"ColorPattern '{pattern.Name}' references unknown ColorPalette '{pattern.ColorPalette}'.", "ColorCacheBuilder");
                }
            }

            return cache;
        }
    }
}

