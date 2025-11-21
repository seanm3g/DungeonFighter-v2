using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// Manages color patterns for different types of content
    /// </summary>
    public static class ColorPatterns
    {
        private static readonly Dictionary<string, ColorPalette> _patternColors = new Dictionary<string, ColorPalette>
        {
            // Combat patterns
            ["damage"] = ColorPalette.Damage,
            ["healing"] = ColorPalette.Healing,
            ["critical"] = ColorPalette.Critical,
            ["miss"] = ColorPalette.Miss,
            ["block"] = ColorPalette.Block,
            ["dodge"] = ColorPalette.Dodge,
            ["attack"] = ColorPalette.Damage,
            ["hit"] = ColorPalette.Damage,
            ["strike"] = ColorPalette.Damage,
            ["slash"] = ColorPalette.Damage,
            ["pierce"] = ColorPalette.Damage,
            ["crush"] = ColorPalette.Damage,
            
            // Status patterns
            ["success"] = ColorPalette.Success,
            ["warning"] = ColorPalette.Warning,
            ["error"] = ColorPalette.Error,
            ["info"] = ColorPalette.Info,
            ["victory"] = ColorPalette.Success,
            ["defeat"] = ColorPalette.Error,
            ["level_up"] = ColorPalette.Success,
            ["experience"] = ColorPalette.Info,
            
            // Rarity patterns
            ["common"] = ColorPalette.Common,
            ["uncommon"] = ColorPalette.Uncommon,
            ["rare"] = ColorPalette.Rare,
            ["epic"] = ColorPalette.Epic,
            ["legendary"] = ColorPalette.Legendary,
            
            // Element patterns
            ["fire"] = ColorPalette.Red,
            ["ice"] = ColorPalette.Cyan,
            ["lightning"] = ColorPalette.Yellow,
            ["poison"] = ColorPalette.Green,
            ["dark"] = ColorPalette.Purple,
            ["light"] = ColorPalette.Yellow,
            ["arcane"] = ColorPalette.Magenta,
            ["nature"] = ColorPalette.Green,
            
            // UI patterns
            ["title"] = ColorPalette.Gold,
            ["subtitle"] = ColorPalette.Yellow,
            ["header"] = ColorPalette.Cyan,
            ["label"] = ColorPalette.White,
            ["value"] = ColorPalette.LightGray,
            ["button"] = ColorPalette.Cyan,
            ["selected"] = ColorPalette.Highlight,
            ["disabled"] = ColorPalette.Disabled,
            
            // Character patterns
            ["player"] = ColorPalette.Cyan,
            ["enemy"] = ColorPalette.Red,
            ["npc"] = ColorPalette.Green,
            ["boss"] = ColorPalette.Purple,
            ["minion"] = ColorPalette.Orange,
            
            // Item patterns
            ["weapon"] = ColorPalette.Silver,
            ["armor"] = ColorPalette.Blue,
            ["potion"] = ColorPalette.Green,
            ["scroll"] = ColorPalette.Magenta,
            ["gem"] = ColorPalette.Cyan,
            ["coin"] = ColorPalette.Gold,
            ["loot"] = ColorPalette.Gold,
            
            // Location patterns
            ["dungeon"] = ColorPalette.Brown,
            ["room"] = ColorPalette.Gray,
            ["corridor"] = ColorPalette.DarkGray,
            ["chamber"] = ColorPalette.Purple,
            ["treasure"] = ColorPalette.Gold,
            ["exit"] = ColorPalette.Green,
            ["entrance"] = ColorPalette.Cyan,
            
            // Environmental patterns
            ["natural"] = ColorPalette.Green
        };
        
        /// <summary>
        /// Gets the color for a specific pattern
        /// Ensures the color is visible on black background
        /// </summary>
        public static Color GetColorForPattern(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return ColorPalette.White.GetColor();
                
            var normalizedPattern = pattern.ToLowerInvariant().Trim();
            
            Color color;
            if (_patternColors.TryGetValue(normalizedPattern, out var palette))
            {
                color = palette.GetColor();
            }
            else
            {
                // Default to white if pattern not found
                color = ColorPalette.White.GetColor();
            }
            
            // Ensure color is visible on black background
            return ColorValidator.EnsureVisible(color);
        }
        
        /// <summary>
        /// Gets the color palette for a specific pattern
        /// </summary>
        public static ColorPalette GetPaletteForPattern(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return ColorPalette.White;
                
            var normalizedPattern = pattern.ToLowerInvariant().Trim();
            
            return _patternColors.TryGetValue(normalizedPattern, out var palette) 
                ? palette 
                : ColorPalette.White;
        }
        
        /// <summary>
        /// Checks if a pattern exists
        /// </summary>
        public static bool HasPattern(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return false;
                
            var normalizedPattern = pattern.ToLowerInvariant().Trim();
            return _patternColors.ContainsKey(normalizedPattern);
        }
        
        /// <summary>
        /// Gets all available patterns
        /// </summary>
        public static IEnumerable<string> GetAllPatterns()
        {
            return _patternColors.Keys;
        }
        
        /// <summary>
        /// Gets patterns by category
        /// </summary>
        public static IEnumerable<string> GetPatternsByCategory(string category)
        {
            if (string.IsNullOrEmpty(category))
                return GetAllPatterns();
                
            var normalizedCategory = category.ToLowerInvariant().Trim();
            
            return _patternColors.Keys
                .Where(pattern => pattern.StartsWith(normalizedCategory, StringComparison.OrdinalIgnoreCase));
        }
        
        /// <summary>
        /// Adds a new pattern
        /// </summary>
        public static void AddPattern(string pattern, ColorPalette color)
        {
            if (string.IsNullOrEmpty(pattern))
                return;
                
            var normalizedPattern = pattern.ToLowerInvariant().Trim();
            _patternColors[normalizedPattern] = color;
        }
        
        /// <summary>
        /// Removes a pattern
        /// </summary>
        public static bool RemovePattern(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return false;
                
            var normalizedPattern = pattern.ToLowerInvariant().Trim();
            return _patternColors.Remove(normalizedPattern);
        }
        
        /// <summary>
        /// Gets pattern categories
        /// </summary>
        public static IEnumerable<string> GetCategories()
        {
            return _patternColors.Keys
                .Select(pattern => pattern.Split('_')[0])
                .Distinct()
                .OrderBy(category => category);
        }
    }
}
