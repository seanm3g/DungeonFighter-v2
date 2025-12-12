using System;
using Avalonia.Media;
using RPGGame;
using RPGGame.Data;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.Avalonia;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// Helper class for getting entity colors with override support
    /// </summary>
    public static class EntityColorHelper
    {
        /// <summary>
        /// Gets the color for an enemy, checking for color override first, then falling back to defaults
        /// </summary>
        public static Color GetEnemyColor(Enemy enemy)
        {
            if (enemy == null)
                return ColorPalette.White.GetColor();
            
            // Check for color override
            var colorOverride = GetColorOverride(enemy);
            if (colorOverride != null)
            {
                var overrideColor = ResolveColorOverride(colorOverride);
                if (overrideColor.HasValue)
                {
                    return overrideColor.Value;
                }
            }
            
            // Fallback to default enemy color
            var defaultPaletteName = ColorConfigurationLoader.GetEntityDefault("enemy");
            if (Enum.TryParse<ColorPalette>(defaultPaletteName, true, out var defaultPalette))
            {
                return defaultPalette.GetColor();
            }
            
            return ColorPalette.Enemy.GetColor();
        }
        
        /// <summary>
        /// Gets the color for a dungeon, checking for color override first, then falling back to theme color
        /// </summary>
        public static Color GetDungeonColor(Dungeon dungeon)
        {
            if (dungeon == null)
                return ColorPalette.White.GetColor();
            
            // Check for color override
            var colorOverride = GetDungeonColorOverride(dungeon);
            if (colorOverride != null)
            {
                var overrideColor = ResolveColorOverride(colorOverride);
                if (overrideColor.HasValue)
                {
                    return overrideColor.Value;
                }
            }
            
            // Fallback to theme color
            return DungeonThemeColors.GetThemeColor(dungeon.Theme);
        }
        
        /// <summary>
        /// Gets the color override from an enemy using reflection
        /// </summary>
        private static ColorOverride? GetColorOverride(Enemy enemy)
        {
            var colorOverrideProperty = typeof(Enemy).GetProperty("ColorOverride", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (colorOverrideProperty != null)
            {
                return colorOverrideProperty.GetValue(enemy) as ColorOverride;
            }
            return null;
        }
        
        /// <summary>
        /// Gets the color override from a dungeon
        /// </summary>
        private static ColorOverride? GetDungeonColorOverride(Dungeon dungeon)
        {
            return dungeon.ColorOverride;
        }
        
        /// <summary>
        /// Resolves a color override to an actual Color value
        /// </summary>
        private static Color? ResolveColorOverride(ColorOverride? colorOverride)
        {
            if (colorOverride == null || string.IsNullOrEmpty(colorOverride.Type))
                return null;
            
            var type = colorOverride.Type.ToLowerInvariant();
            
            switch (type)
            {
                case "template":
                    // Template name - return first color from template
                    if (!string.IsNullOrEmpty(colorOverride.Value))
                    {
                        var template = ColorConfigurationLoader.GetTemplate(colorOverride.Value);
                        if (template != null && template.Colors != null && template.Colors.Count > 0)
                        {
                            return ColorCodeLoader.GetColor(template.Colors[0]);
                        }
                    }
                    break;
                    
                case "palette":
                    // Palette name - return palette color
                    if (!string.IsNullOrEmpty(colorOverride.Value))
                    {
                        if (Enum.TryParse<ColorPalette>(colorOverride.Value, true, out var palette))
                        {
                            return palette.GetColor();
                        }
                    }
                    break;
                    
                case "colorcode":
                case "color_code":
                    // Color code - return color code color
                    if (!string.IsNullOrEmpty(colorOverride.Value))
                    {
                        return ColorCodeLoader.GetColor(colorOverride.Value);
                    }
                    break;
                    
                case "rgb":
                    // Direct RGB values
                    if (colorOverride.Rgb != null && colorOverride.Rgb.Length == 3)
                    {
                        int r = Math.Clamp(colorOverride.Rgb[0], 0, 255);
                        int g = Math.Clamp(colorOverride.Rgb[1], 0, 255);
                        int b = Math.Clamp(colorOverride.Rgb[2], 0, 255);
                        return Color.FromRgb((byte)r, (byte)g, (byte)b);
                    }
                    break;
            }
            
            return null;
        }
    }
}

