using Avalonia.Media;
using System.Collections.Generic;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia
{
    /// <summary>
    /// Maps dungeon themes to their thematic color palettes for UI display
    /// Uses custom theme-specific colors while delegating fallback to ColorPalette
    /// </summary>
    public static class DungeonThemeColors
    {
        // Theme-specific colors (custom RGB values for thematic accuracy)
        // These are kept separate from ColorPalette as they are theme-specific
        private static readonly Dictionary<string, Color> themeColorMap = new()
        {
            // Natural themes
            { "Forest", Color.FromRgb(0, 196, 32) },         // Green - vibrant forest
            { "Nature", Color.FromRgb(0, 148, 3) },          // Dark green - mystical nature
            { "Swamp", Color.FromRgb(88, 129, 87) },         // Murky green - poisonous swamp
            
            // Fire/Heat themes
            { "Lava", Color.FromRgb(215, 66, 0) },           // Red - molten lava
            { "Volcano", Color.FromRgb(255, 140, 0) },       // Orange - volcanic heat
            
            // Ice/Cold themes
            { "Ice", Color.FromRgb(119, 191, 207) },         // Cyan - frozen ice
            { "Mountain", Color.FromRgb(192, 192, 192) },    // Silver - snowy peaks
            
            // Dark themes
            { "Crypt", Color.FromRgb(177, 84, 207) },        // Purple - undead magic
            { "Shadow", Color.FromRgb(75, 0, 130) },         // Indigo - deep shadow
            { "Void", Color.FromRgb(64, 64, 64) },           // Dark gray - emptiness
            { "Dream", Color.FromRgb(147, 112, 219) },       // Medium purple - dreamlike
            
            // Magical themes
            { "Crystal", Color.FromRgb(0, 255, 255) },       // Bright cyan - crystalline
            { "Arcane", Color.FromRgb(0, 150, 255) },        // Blue - arcane magic
            { "Astral", Color.FromRgb(218, 91, 214) },       // Magenta - cosmic
            { "Dimensional", Color.FromRgb(138, 43, 226) },  // Blue violet - reality bending
            { "Temporal", Color.FromRgb(100, 149, 237) },    // Cornflower blue - time distortion
            
            // Holy/Divine themes
            { "Temple", Color.FromRgb(255, 215, 0) },        // Gold - sacred temple
            { "Divine", Color.FromRgb(255, 255, 224) },      // Light yellow - holy light
            
            // Weather themes
            { "Storm", Color.FromRgb(70, 130, 180) },        // Steel blue - stormy
            { "Ocean", Color.FromRgb(0, 105, 148) },         // Deep blue - ocean depths
            
            // Industrial/Artificial themes
            { "Steampunk", Color.FromRgb(205, 127, 50) },    // Bronze - mechanical
            { "Underground", Color.FromRgb(139, 90, 43) },   // Brown - earthy depths
            
            // Desert/Arid themes
            { "Desert", Color.FromRgb(237, 201, 175) },      // Sandy beige
            { "Ruins", Color.FromRgb(160, 144, 119) },       // Stone gray-brown
            
            // Generic/Default
            { "Generic", Color.FromRgb(192, 192, 192) },     // Silver - neutral
        };

        /// <summary>
        /// Gets the theme color for a dungeon. Returns ColorPalette.White if theme not found.
        /// </summary>
        public static Color GetThemeColor(string theme)
        {
            if (string.IsNullOrEmpty(theme))
                return ColorPalette.White.GetColor();

            return themeColorMap.TryGetValue(theme, out Color color) 
                ? color 
                : ColorPalette.White.GetColor();
        }

        /// <summary>
        /// Gets a dimmed version of the theme color for hover effects
        /// </summary>
        public static Color GetDimmedThemeColor(string theme)
        {
            Color baseColor = GetThemeColor(theme);
            return Color.FromRgb(
                (byte)(baseColor.R * 0.7),
                (byte)(baseColor.G * 0.7),
                (byte)(baseColor.B * 0.7)
            );
        }

        /// <summary>
        /// Gets a brightened version of the theme color for highlights
        /// </summary>
        public static Color GetBrightenedThemeColor(string theme)
        {
            Color baseColor = GetThemeColor(theme);
            return Color.FromRgb(
                (byte)System.Math.Min(255, baseColor.R + 50),
                (byte)System.Math.Min(255, baseColor.G + 50),
                (byte)System.Math.Min(255, baseColor.B + 50)
            );
        }

        /// <summary>
        /// Gets a list of all available themes and their colors for reference
        /// </summary>
        public static Dictionary<string, Color> GetAllThemeColors()
        {
            return new Dictionary<string, Color>(themeColorMap);
        }

        /// <summary>
        /// Maps dungeon themes to their closest color code character for text coloring
        /// </summary>
        private static readonly Dictionary<string, char> themeColorCodeMap = new()
        {
            // Natural themes
            { "Forest", 'G' },         // Green
            { "Nature", 'G' },         // Green
            { "Swamp", 'G' },          // Green
            
            // Fire/Heat themes
            { "Lava", 'R' },           // Red
            { "Volcano", 'O' },        // Orange
            
            // Ice/Cold themes
            { "Ice", 'C' },            // Cyan
            { "Mountain", 'Y' },       // White/Silver
            
            // Dark themes
            { "Crypt", 'M' },          // Magenta/Purple
            { "Shadow", 'M' },         // Magenta/Purple
            { "Void", 'y' },           // Grey
            { "Dream", 'M' },          // Magenta/Purple
            
            // Magical themes
            { "Crystal", 'C' },        // Cyan
            { "Arcane", 'B' },         // Blue
            { "Astral", 'M' },         // Magenta
            { "Dimensional", 'M' },    // Magenta
            { "Temporal", 'B' },       // Blue
            
            // Holy/Divine themes
            { "Temple", 'W' },         // Yellow/Gold
            { "Divine", 'W' },         // Yellow/Gold
            
            // Weather themes
            { "Storm", 'B' },          // Blue
            { "Ocean", 'B' },          // Blue
            
            // Industrial/Artificial themes
            { "Steampunk", 'O' },      // Orange/Bronze
            { "Underground", 'o' },    // Dark orange/Brown
            
            // Desert/Arid themes
            { "Desert", 'W' },         // Yellow/Sandy
            { "Ruins", 'y' },          // Grey
            
            // Generic/Default
            { "Generic", 'Y' },        // White
        };

        /// <summary>
        /// Gets the color code character for a dungeon theme. Returns 'Y' (white) if theme not found.
        /// </summary>
        public static char GetThemeColorCode(string theme)
        {
            if (string.IsNullOrEmpty(theme))
                return 'Y';

            return themeColorCodeMap.TryGetValue(theme, out char code) 
                ? code 
                : 'Y';
        }
    }
}

