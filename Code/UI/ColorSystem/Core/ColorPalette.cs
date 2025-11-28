using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.Data;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// Standard color palette for the game
    /// Color values are loaded from GameData/ColorPalette.json
    /// </summary>
    public enum ColorPalette
    {
        // Basic colors
        White,
        Black,
        Gray,
        DarkGray,
        LightGray,
        
        // Primary colors
        Red,
        Green,
        Blue,
        Yellow,
        Cyan,
        Magenta,
        
        // Dark variants
        DarkRed,
        DarkGreen,
        DarkBlue,
        DarkYellow,
        DarkCyan,
        DarkMagenta,
        
        // Game-specific colors
        Gold,
        Silver,
        Bronze,
        Orange,
        Purple,
        Pink,
        Brown,
        Lime,
        Navy,
        Teal,
        
        // Status colors
        Success,
        Warning,
        Error,
        Info,
        
        // Combat colors
        Damage,
        Healing,
        Critical,
        Miss,
        Block,
        Dodge,
        
        // Rarity colors
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        
        // UI colors
        Background,
        Foreground,
        Border,
        Highlight,
        Disabled,
        
        // Actor colors (semantic)
        Player,
        Enemy,
        NPC,
        Boss,
        Minion,
        
        // Item type colors
        Weapon,
        Armor,
        Potion,
        Scroll
    }
    
    /// <summary>
    /// Provides color values for the color palette
    /// Colors are loaded from GameData/ColorPalette.json via ColorPaletteLoader
    /// </summary>
    public static class ColorPaletteExtensions
    {
        /// <summary>
        /// Gets the color value for a palette color
        /// </summary>
        public static Color GetColor(this ColorPalette palette)
        {
            return ColorPaletteLoader.GetColor(palette);
        }
        
        /// <summary>
        /// Gets all available colors in the palette
        /// </summary>
        public static IEnumerable<ColorPalette> GetAllColors()
        {
            return ColorPaletteLoader.GetAllColors();
        }
        
        /// <summary>
        /// Checks if a color exists in the palette
        /// </summary>
        public static bool IsValidColor(ColorPalette palette)
        {
            return ColorPaletteLoader.HasColor(palette);
        }
    }
}


