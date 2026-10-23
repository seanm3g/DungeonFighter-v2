using Avalonia.Media;
using System.Collections.Generic;
using RPGGame.UI.ColorSystem;
using RPGGame.Data;

namespace RPGGame.UI.Avalonia
{
    /// <summary>
    /// Maps dungeon themes to their thematic color palettes for UI display
    /// Loads colors from ColorConfiguration.json with fallback to hard-coded values
    /// </summary>
    public static class DungeonThemeColors
    {
        // Fallback theme-specific colors (used if ColorConfiguration.json not found)
        // These are kept as fallback for backward compatibility
        private static readonly Dictionary<string, Color> fallbackThemeColorMap = new()
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

            // Region biome dungeons (Regions.json linkedDungeonThemes)
            { "Den", Color.FromRgb(139, 110, 60) },          // Earthy brown - animal den
            { "Labyrinth", Color.FromRgb(46, 160, 87) },     // Hedge green
            { "Dense Brush", Color.FromRgb(0, 170, 40) },    // Thicket green
            { "Mirror Pond", Color.FromRgb(120, 170, 200) }, // Misty blue
            { "Spring", Color.FromRgb(64, 200, 210) },       // Spring cyan
            { "Water Temple", Color.FromRgb(30, 110, 190) }, // Deep temple blue
            { "Ashen Land", Color.FromRgb(150, 140, 130) },  // Ash gray
            { "Fire Temple", Color.FromRgb(255, 120, 20) },  // Flame orange
            { "Lava Cave", Color.FromRgb(215, 66, 0) },      // Molten red
            { "Pyramids", Color.FromRgb(214, 180, 90) },     // Sandstone gold
            { "Mountains", Color.FromRgb(192, 192, 210) },   // Snowy silver
            
            // Generic/Default
            { "Generic", Color.FromRgb(192, 192, 192) },     // Silver - neutral
        };

        /// <summary>
        /// Gets the theme color for a dungeon. Returns ColorPalette.White if theme not found.
        /// Loads from ColorConfiguration.json, falls back to hard-coded values if not found.
        /// </summary>
        public static Color GetThemeColor(string theme)
        {
            if (string.IsNullOrEmpty(theme))
                return ColorPalette.White.GetColor();

            // Try to load from unified configuration
            var color = ColorConfigurationLoader.GetDungeonThemeColor(theme);
            if (color != ColorPalette.White.GetColor())
            {
                return color;
            }

            // Fallback to hard-coded values
            return fallbackThemeColorMap.TryGetValue(theme, out Color fallbackColor) 
                ? fallbackColor 
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
        /// Loads from ColorConfiguration.json, falls back to hard-coded values
        /// </summary>
        public static Dictionary<string, Color> GetAllThemeColors()
        {
            var result = new Dictionary<string, Color>();
            
            // Load from unified configuration
            var config = ColorConfigurationLoader.LoadColorConfiguration();
            if (config.DungeonThemes != null)
            {
                foreach (var theme in config.DungeonThemes)
                {
                    if (!string.IsNullOrEmpty(theme.Name) && theme.Rgb != null && theme.Rgb.Length == 3)
                    {
                        int r = System.Math.Clamp(theme.Rgb[0], 0, 255);
                        int g = System.Math.Clamp(theme.Rgb[1], 0, 255);
                        int b = System.Math.Clamp(theme.Rgb[2], 0, 255);
                        result[theme.Name] = Color.FromRgb((byte)r, (byte)g, (byte)b);
                    }
                }
            }
            
            // Add any missing themes from fallback
            foreach (var kvp in fallbackThemeColorMap)
            {
                if (!result.ContainsKey(kvp.Key))
                {
                    result[kvp.Key] = kvp.Value;
                }
            }
            
            return result;
        }

        // Fallback color code map (used if ColorConfiguration.json not found)
        private static readonly Dictionary<string, char> fallbackThemeColorCodeMap = new()
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
        /// Loads from ColorConfiguration.json, falls back to hard-coded values if not found.
        /// </summary>
        public static char GetThemeColorCode(string theme)
        {
            if (string.IsNullOrEmpty(theme))
                return 'Y';

            // Try to load from unified configuration
            var themeData = ColorConfigurationLoader.GetDungeonTheme(theme);
            if (themeData != null && !string.IsNullOrEmpty(themeData.ColorCode))
            {
                return themeData.ColorCode[0]; // Take first character
            }

            // Fallback to hard-coded values
            return fallbackThemeColorCodeMap.TryGetValue(theme, out char code)
                ? code
                : 'Y';
        }

        /// <summary>
        /// ColorTemplates.json template for a dungeon theme ("Fire Temple" → "fire_temple"), or null when none exists.
        /// Rooms carry their dungeon's theme, so this also themes room names.
        /// </summary>
        public static string? GetThemeTemplate(string? theme)
        {
            if (string.IsNullOrWhiteSpace(theme))
                return null;
            string slug = ToTemplateSlug(theme);
            return ColorTemplateLibrary.HasTemplate(slug) ? slug : null;
        }

        /// <summary>
        /// Template for a travel region's element theme (Forest / Water / Fire / Air → "region_fire"), or null.
        /// </summary>
        public static string? GetRegionThemeTemplate(string? regionTheme)
        {
            if (string.IsNullOrWhiteSpace(regionTheme))
                return null;
            string slug = "region_" + ToTemplateSlug(regionTheme);
            return ColorTemplateLibrary.HasTemplate(slug) ? slug : null;
        }

        /// <summary>
        /// Name painted with the theme's color template; single theme color when the theme has no template.
        /// Segments keep <see cref="ColoredText.SourceTemplate"/> so markup and animation preserve the palette.
        /// </summary>
        public static List<ColoredText> BuildThemedName(string name, string? theme)
        {
            if (string.IsNullOrEmpty(name))
                return new List<ColoredText>();
            string? template = GetThemeTemplate(theme);
            if (template != null)
            {
                var segments = ColoredText.FromTemplate(template, name);
                if (segments.Count > 0)
                    return segments;
            }
            return new List<ColoredText> { new ColoredText(name, GetThemeColor(theme ?? "")) };
        }

        private static Dictionary<string, string>? dungeonThemeByName;

        /// <summary>
        /// Theme for a dungeon display name from Dungeons.json (for UI that only has the name, e.g. the LOCATION panel).
        /// </summary>
        public static string? GetThemeForDungeonName(string? dungeonName)
        {
            if (string.IsNullOrWhiteSpace(dungeonName))
                return null;
            if (dungeonThemeByName == null)
            {
                var map = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
                try
                {
                    foreach (var d in JsonLoader.LoadJsonList<DungeonData>("Dungeons.json"))
                    {
                        if (!string.IsNullOrWhiteSpace(d.name) && !string.IsNullOrWhiteSpace(d.theme))
                            map[d.name.Trim()] = d.theme.Trim();
                    }
                }
                catch
                {
                    // Missing data file: names fall back to their plain colors
                }
                dungeonThemeByName = map;
            }
            return dungeonThemeByName.TryGetValue(dungeonName.Trim(), out var theme) ? theme : null;
        }

        private static string ToTemplateSlug(string theme)
        {
            var sb = new System.Text.StringBuilder(theme.Length);
            foreach (char c in theme.Trim().ToLowerInvariant())
                sb.Append(char.IsLetterOrDigit(c) ? c : '_');
            return sb.ToString();
        }
    }
}

