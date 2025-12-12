using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame.Data;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// Manages color patterns for different types of content
    /// Loads from ColorConfiguration.json with fallback to hard-coded values
    /// </summary>
    public static class ColorPatterns
    {
        // Fallback pattern colors (used if ColorConfiguration.json not found)
        private static readonly Dictionary<string, ColorPalette> _fallbackPatternColors = new Dictionary<string, ColorPalette>
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
        /// Loads from ColorConfiguration.json, falls back to hard-coded values
        /// Ensures the color is visible on black background
        /// </summary>
        public static Color GetColorForPattern(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return ColorPalette.White.GetColor();
                
            var normalizedPattern = pattern.ToLowerInvariant().Trim();
            
            // Try to load from unified configuration
            var palette = ColorConfigurationLoader.GetColorPattern(normalizedPattern);
            if (palette != ColorPalette.White)
            {
                var color = palette.GetColor();
                return ColorValidator.EnsureVisible(color);
            }
            
            // Fallback to hard-coded values
            Color color2;
            if (_fallbackPatternColors.TryGetValue(normalizedPattern, out var fallbackPalette))
            {
                color2 = fallbackPalette.GetColor();
            }
            else
            {
                // Default to white if pattern not found
                color2 = ColorPalette.White.GetColor();
            }
            
            // Ensure color is visible on black background
            return ColorValidator.EnsureVisible(color2);
        }
        
        /// <summary>
        /// Gets the color palette for a specific pattern
        /// Loads from ColorConfiguration.json, falls back to hard-coded values
        /// </summary>
        public static ColorPalette GetPaletteForPattern(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return ColorPalette.White;
                
            var normalizedPattern = pattern.ToLowerInvariant().Trim();
            
            // Try to load from unified configuration
            var palette = ColorConfigurationLoader.GetColorPattern(normalizedPattern);
            if (palette != ColorPalette.White)
            {
                return palette;
            }
            
            // Fallback to hard-coded values
            return _fallbackPatternColors.TryGetValue(normalizedPattern, out var fallbackPalette) 
                ? fallbackPalette 
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
            
            // Check unified configuration first
            var palette = ColorConfigurationLoader.GetColorPattern(normalizedPattern);
            if (palette != ColorPalette.White)
            {
                return true;
            }
            
            // Check fallback
            return _fallbackPatternColors.ContainsKey(normalizedPattern);
        }
        
        /// <summary>
        /// Gets all available patterns
        /// </summary>
        public static IEnumerable<string> GetAllPatterns()
        {
            var patterns = new HashSet<string>();
            
            // Load from unified configuration
            var config = ColorConfigurationLoader.LoadColorConfiguration();
            if (config.ColorPatterns != null)
            {
                foreach (var pattern in config.ColorPatterns)
                {
                    if (!string.IsNullOrEmpty(pattern.Name))
                    {
                        patterns.Add(pattern.Name.ToLowerInvariant());
                    }
                }
            }
            
            // Add fallback patterns
            foreach (var pattern in _fallbackPatternColors.Keys)
            {
                patterns.Add(pattern);
            }
            
            return patterns;
        }
        
        /// <summary>
        /// Gets patterns by category
        /// </summary>
        public static IEnumerable<string> GetPatternsByCategory(string category)
        {
            if (string.IsNullOrEmpty(category))
                return GetAllPatterns();
                
            var normalizedCategory = category.ToLowerInvariant().Trim();
            var allPatterns = GetAllPatterns();
            
            return allPatterns
                .Where(pattern => pattern.StartsWith(normalizedCategory, StringComparison.OrdinalIgnoreCase));
        }
        
        /// <summary>
        /// Adds a new pattern (runtime modification - not persisted)
        /// Note: This only affects the fallback cache, not the JSON configuration
        /// </summary>
        public static void AddPattern(string pattern, ColorPalette color)
        {
            if (string.IsNullOrEmpty(pattern))
                return;
                
            var normalizedPattern = pattern.ToLowerInvariant().Trim();
            _fallbackPatternColors[normalizedPattern] = color;
        }
        
        /// <summary>
        /// Removes a pattern (runtime modification - not persisted)
        /// Note: This only affects the fallback cache, not the JSON configuration
        /// </summary>
        public static bool RemovePattern(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return false;
                
            var normalizedPattern = pattern.ToLowerInvariant().Trim();
            return _fallbackPatternColors.Remove(normalizedPattern);
        }
        
        /// <summary>
        /// Gets pattern categories
        /// </summary>
        public static IEnumerable<string> GetCategories()
        {
            var config = ColorConfigurationLoader.LoadColorConfiguration();
            var categories = new HashSet<string>();
            
            // Load categories from unified configuration
            if (config.ColorPatterns != null)
            {
                foreach (var pattern in config.ColorPatterns)
                {
                    if (!string.IsNullOrEmpty(pattern.Category))
                    {
                        categories.Add(pattern.Category.ToLowerInvariant());
                    }
                }
            }
            
            // Add categories from fallback patterns
            foreach (var pattern in _fallbackPatternColors.Keys)
            {
                var parts = pattern.Split('_');
                if (parts.Length > 0)
                {
                    categories.Add(parts[0].ToLowerInvariant());
                }
            }
            
            return categories.OrderBy(c => c);
        }
    }
}
