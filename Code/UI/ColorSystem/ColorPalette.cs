using System;
using System.Collections.Generic;
using Avalonia.Media;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// Standard color palette for the game
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
    /// </summary>
    public static class ColorPaletteExtensions
    {
        private static readonly Dictionary<ColorPalette, Color> _colors = new Dictionary<ColorPalette, Color>
        {
            // Basic colors
            [ColorPalette.White] = Colors.White,
            [ColorPalette.Black] = Colors.Black,
            [ColorPalette.Gray] = Colors.Gray,
            [ColorPalette.DarkGray] = Colors.DarkGray,
            [ColorPalette.LightGray] = Colors.LightGray,
            
            // Primary colors
            [ColorPalette.Red] = Colors.Red,
            [ColorPalette.Green] = Colors.Green,
            [ColorPalette.Blue] = Colors.Blue,
            [ColorPalette.Yellow] = Colors.Yellow,
            [ColorPalette.Cyan] = Colors.Cyan,
            [ColorPalette.Magenta] = Colors.Magenta,
            
            // Dark variants
            [ColorPalette.DarkRed] = Color.FromRgb(139, 0, 0),
            [ColorPalette.DarkGreen] = Color.FromRgb(0, 100, 0),
            [ColorPalette.DarkBlue] = Color.FromRgb(0, 0, 139),
            [ColorPalette.DarkYellow] = Color.FromRgb(184, 134, 11),
            [ColorPalette.DarkCyan] = Color.FromRgb(0, 139, 139),
            [ColorPalette.DarkMagenta] = Color.FromRgb(139, 0, 139),
            
            // Game-specific colors
            [ColorPalette.Gold] = Color.FromRgb(255, 215, 0),
            [ColorPalette.Silver] = Color.FromRgb(192, 192, 192),
            [ColorPalette.Bronze] = Color.FromRgb(205, 127, 50),
            [ColorPalette.Orange] = Color.FromRgb(255, 165, 0),
            [ColorPalette.Purple] = Color.FromRgb(128, 0, 128),
            [ColorPalette.Pink] = Color.FromRgb(255, 192, 203),
            [ColorPalette.Brown] = Color.FromRgb(165, 42, 42),
            [ColorPalette.Lime] = Color.FromRgb(0, 255, 0),
            [ColorPalette.Navy] = Color.FromRgb(0, 0, 128),
            [ColorPalette.Teal] = Color.FromRgb(0, 128, 128),
            
            // Status colors
            [ColorPalette.Success] = Color.FromRgb(0, 128, 0),
            [ColorPalette.Warning] = Color.FromRgb(255, 165, 0),
            [ColorPalette.Error] = Color.FromRgb(220, 20, 60),
            [ColorPalette.Info] = Color.FromRgb(0, 191, 255),
            
            // Combat colors
            [ColorPalette.Damage] = Color.FromRgb(220, 20, 60),
            [ColorPalette.Healing] = Color.FromRgb(0, 255, 127),
            [ColorPalette.Critical] = Color.FromRgb(255, 0, 0),
            [ColorPalette.Miss] = Color.FromRgb(128, 128, 128),
            [ColorPalette.Block] = Color.FromRgb(0, 191, 255),
            [ColorPalette.Dodge] = Color.FromRgb(255, 255, 0),
            
            // Rarity colors
            [ColorPalette.Common] = Colors.White,
            [ColorPalette.Uncommon] = Color.FromRgb(0, 255, 0),
            [ColorPalette.Rare] = Color.FromRgb(0, 191, 255),
            [ColorPalette.Epic] = Color.FromRgb(128, 0, 128),
            [ColorPalette.Legendary] = Color.FromRgb(255, 215, 0),
            
            // UI colors
            [ColorPalette.Background] = Color.FromRgb(25, 25, 25),
            [ColorPalette.Foreground] = Colors.White,
            [ColorPalette.Border] = Color.FromRgb(64, 64, 64),
            [ColorPalette.Highlight] = Color.FromRgb(255, 255, 0),
            [ColorPalette.Disabled] = Color.FromRgb(128, 128, 128),
            
            // Actor colors (semantic)
            [ColorPalette.Player] = Colors.Cyan,
            [ColorPalette.Enemy] = Colors.Red,
            [ColorPalette.NPC] = Color.FromRgb(0, 255, 0),
            [ColorPalette.Boss] = Color.FromRgb(128, 0, 128),
            [ColorPalette.Minion] = Color.FromRgb(255, 165, 0),
            
            // Item type colors
            [ColorPalette.Weapon] = Color.FromRgb(192, 192, 192),
            [ColorPalette.Armor] = Colors.Blue,
            [ColorPalette.Potion] = Colors.Green,
            [ColorPalette.Scroll] = Colors.Magenta
        };
        
        /// <summary>
        /// Gets the color value for a palette color
        /// </summary>
        public static Color GetColor(this ColorPalette palette)
        {
            return _colors.TryGetValue(palette, out var color) ? color : Colors.White;
        }
        
        /// <summary>
        /// Gets all available colors in the palette
        /// </summary>
        public static IEnumerable<ColorPalette> GetAllColors()
        {
            return _colors.Keys;
        }
        
        /// <summary>
        /// Checks if a color exists in the palette
        /// </summary>
        public static bool IsValidColor(ColorPalette palette)
        {
            return _colors.ContainsKey(palette);
        }
    }
}


