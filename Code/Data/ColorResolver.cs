using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Data
{
    public static class ColorResolver
    {
        public static Color GetColorCode(
            string colorCode,
            Dictionary<string, Color>? colorCodeCache,
            bool isBuilding,
            bool inFallback,
            ref bool fallbackFlag)
        {
            if (string.IsNullOrEmpty(colorCode))
                return Colors.White;
            
            if (isBuilding)
            {
                if (colorCodeCache != null && colorCodeCache.TryGetValue(colorCode, out var cachedColor))
                {
                    return cachedColor;
                }
                return Colors.White;
            }
            
            if (inFallback)
            {
                if (colorCodeCache != null && colorCodeCache.TryGetValue(colorCode, out var fallbackColor))
                {
                    return fallbackColor;
                }
                return Colors.White;
            }
            
            if (colorCodeCache != null && colorCodeCache.TryGetValue(colorCode, out var resultColor))
            {
                return resultColor;
            }
            
            fallbackFlag = true;
            try
            {
                return ColorCodeLoader.GetColor(colorCode);
            }
            finally
            {
                fallbackFlag = false;
            }
        }

        public static Color GetPaletteColor(
            ColorPalette palette,
            Dictionary<ColorPalette, Color>? paletteColorCache)
        {
            if (paletteColorCache != null && paletteColorCache.TryGetValue(palette, out var color))
            {
                return color;
            }
            
            return ColorPaletteLoader.GetColor(palette);
        }

        public static ColorTemplateData? GetTemplate(
            string templateName,
            Dictionary<string, ColorTemplateData>? templateCache)
        {
            if (string.IsNullOrEmpty(templateName))
                return null;
            
            if (templateCache != null && templateCache.TryGetValue(templateName, out var template))
            {
                return template;
            }
            
            return ColorTemplateLoader.GetTemplate(templateName);
        }

        public static DungeonThemeData? GetDungeonTheme(
            string theme,
            Dictionary<string, DungeonThemeData>? dungeonThemeCache)
        {
            if (string.IsNullOrEmpty(theme))
                return null;
            
            if (dungeonThemeCache != null && dungeonThemeCache.TryGetValue(theme, out var themeData))
            {
                return themeData;
            }
            
            return null;
        }

        public static Color GetDungeonThemeColor(
            string theme,
            Dictionary<string, DungeonThemeData>? dungeonThemeCache)
        {
            var themeData = GetDungeonTheme(theme, dungeonThemeCache);
            if (themeData != null && themeData.Rgb != null && themeData.Rgb.Length == 3)
            {
                int r = Math.Clamp(themeData.Rgb[0], 0, 255);
                int g = Math.Clamp(themeData.Rgb[1], 0, 255);
                int b = Math.Clamp(themeData.Rgb[2], 0, 255);
                return Color.FromRgb((byte)r, (byte)g, (byte)b);
            }
            
            return ColorPalette.White.GetColor();
        }

        public static ColorPalette GetColorPattern(
            string pattern,
            Dictionary<string, ColorPalette>? colorPatternCache)
        {
            if (string.IsNullOrEmpty(pattern))
                return ColorPalette.White;
            
            var normalizedPattern = pattern.ToLowerInvariant().Trim();
            
            if (colorPatternCache != null && colorPatternCache.TryGetValue(normalizedPattern, out var palette))
            {
                return palette;
            }
            
            return ColorPalette.White;
        }

        public static string GetEntityDefault(
            string entityType,
            ColorConfigurationData? config)
        {
            if (config?.EntityDefaults == null)
                return "White";
            
            return entityType.ToLowerInvariant() switch
            {
                "player" => config.EntityDefaults.Player,
                "enemy" => config.EntityDefaults.Enemy,
                "boss" => config.EntityDefaults.Boss,
                "npc" => config.EntityDefaults.NPC,
                _ => "White"
            };
        }

        public static List<KeywordGroupData> GetKeywordGroups(ColorConfigurationData? config)
        {
            if (config?.KeywordGroups != null)
            {
                return config.KeywordGroups;
            }
            
            var fallbackConfig = KeywordColorLoader.LoadKeywordColors();
            if (fallbackConfig.Groups != null)
            {
                return fallbackConfig.Groups.Select(g => new KeywordGroupData
                {
                    Name = g.Name,
                    ColorPattern = g.ColorPattern,
                    CaseSensitive = g.CaseSensitive,
                    Keywords = g.Keywords
                }).ToList();
            }
            
            return new List<KeywordGroupData>();
        }
    }
}

