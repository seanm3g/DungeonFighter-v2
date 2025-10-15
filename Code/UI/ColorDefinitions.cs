using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using Avalonia.Media;

namespace RPGGame.UI
{
    /// <summary>
    /// Caves of Qud-inspired color definitions with single-letter codes
    /// & = foreground color, ^ = background color
    /// 
    /// Colors are loaded from GameData/ColorTemplates.json at startup.
    /// This ensures the color values are always in sync with the JSON configuration.
    /// </summary>
    public static class ColorDefinitions
    {
        // Color code to RGB mapping - loaded from ColorTemplates.json
        private static readonly Dictionary<char, ColorRGB> colorMap = LoadColorsFromJson();

        /// <summary>
        /// Loads color definitions from ColorTemplates.json
        /// </summary>
        private static Dictionary<char, ColorRGB> LoadColorsFromJson()
        {
            try
            {
                string? configPath = JsonLoader.FindGameDataFile("ColorTemplates.json");
                
                if (configPath != null && System.IO.File.Exists(configPath))
                {
                    string json = System.IO.File.ReadAllText(configPath);
                    using JsonDocument doc = JsonDocument.Parse(json);
                    
                    var colors = new Dictionary<char, ColorRGB>();
                    
                    // Navigate to colorCodes.codes in the JSON
                    if (doc.RootElement.TryGetProperty("colorCodes", out JsonElement colorCodesElement) &&
                        colorCodesElement.TryGetProperty("codes", out JsonElement codesElement))
                    {
                        foreach (JsonProperty property in codesElement.EnumerateObject())
                        {
                            if (property.Name.Length == 1)
                            {
                                char code = property.Name[0];
                                string colorString = property.Value.GetString() ?? "";
                                
                                // Extract hex code from format like "orange (#D04200)"
                                ColorRGB? rgb = ParseColorFromString(colorString);
                                if (rgb.HasValue)
                                {
                                    colors[code] = rgb.Value;
                                }
                            }
                        }
                    }
                    
                    if (colors.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ColorDefinitions] Loaded {colors.Count} colors from ColorTemplates.json");
                        return colors;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogWarning("ColorDefinitions.LoadColorsFromJson", 
                    $"Failed to load colors from ColorTemplates.json: {ex.Message}. Using fallback colors.");
            }
            
            // Fallback to hardcoded colors if JSON loading fails
            return GetFallbackColors();
        }

        /// <summary>
        /// Parses a color string like "orange (#D04200)" or just "#D04200" to RGB
        /// </summary>
        private static ColorRGB? ParseColorFromString(string colorString)
        {
            if (string.IsNullOrEmpty(colorString))
                return null;
            
            // Extract hex code using regex (matches #RRGGBB)
            Match match = Regex.Match(colorString, @"#([0-9A-Fa-f]{6})");
            if (match.Success)
            {
                string hex = match.Groups[1].Value;
                return ParseHexColor(hex);
            }
            
            return null;
        }

        /// <summary>
        /// Converts a hex color string (RRGGBB) to ColorRGB
        /// </summary>
        private static ColorRGB? ParseHexColor(string hex)
        {
            if (hex.Length != 6)
                return null;
            
            try
            {
                byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                return new ColorRGB(r, g, b);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Fallback hardcoded colors in case JSON loading fails
        /// </summary>
        private static Dictionary<char, ColorRGB> GetFallbackColors()
        {
            return new Dictionary<char, ColorRGB>
            {
                // Reds
                { 'r', new ColorRGB(166, 74, 46) },    // dark red / crimson
                { 'R', new ColorRGB(255, 50, 50) },    // bright red
                
                // Oranges
                { 'o', new ColorRGB(255, 140, 0) },    // vibrant orange
                { 'O', new ColorRGB(208, 66, 0) },     // orange
                
                // Browns & Yellows
                { 'w', new ColorRGB(152, 135, 95) },   // brown
                { 'W', new ColorRGB(255, 255, 0) },    // bright sun yellow
                
                // Greens
                { 'g', new ColorRGB(0, 148, 3) },      // dark green
                { 'G', new ColorRGB(0, 196, 32) },     // green
                
                // Blues
                { 'b', new ColorRGB(0, 72, 189) },     // dark blue
                { 'B', new ColorRGB(0, 150, 255) },    // blue / azure
                
                // Cyans
                { 'c', new ColorRGB(64, 164, 185) },   // dark cyan / teal
                { 'C', new ColorRGB(119, 191, 207) },  // cyan
                
                // Magentas
                { 'm', new ColorRGB(177, 84, 207) },   // dark magenta / purple
                { 'M', new ColorRGB(218, 91, 214) },   // magenta
                
                // Grays & Blacks
                { 'k', new ColorRGB(15, 59, 58) },     // very dark
                { 'K', new ColorRGB(21, 83, 82) },     // dark grey / black
                { 'y', new ColorRGB(230, 230, 230) },  // light grey (default text color)
                { 'Y', new ColorRGB(255, 255, 255) },  // white
            };
        }

        /// <summary>
        /// RGB color structure
        /// </summary>
        public struct ColorRGB
        {
            public byte R { get; set; }
            public byte G { get; set; }
            public byte B { get; set; }

            public ColorRGB(byte r, byte g, byte b)
            {
                R = r;
                G = g;
                B = b;
            }

            public Color ToAvaloniaColor() => Color.FromRgb(R, G, B);
            
            public System.ConsoleColor ToConsoleColor()
            {
                // Map RGB to closest console color
                int luminance = (R + G + B) / 3;
                
                // Check for specific color patterns
                if (R > 200 && G < 100 && B < 100) return System.ConsoleColor.Red;
                if (R > 200 && G > 150 && B < 100) return System.ConsoleColor.Yellow;
                if (R < 100 && G > 150 && B < 100) return System.ConsoleColor.Green;
                if (R < 100 && G > 150 && B > 150) return System.ConsoleColor.Cyan;
                if (R < 100 && G < 100 && B > 150) return System.ConsoleColor.Blue;
                if (R > 150 && G < 100 && B > 150) return System.ConsoleColor.Magenta;
                
                // Grayscale mapping
                if (luminance < 50) return System.ConsoleColor.Black;
                if (luminance < 100) return System.ConsoleColor.DarkGray;
                if (luminance < 150) return System.ConsoleColor.Gray;
                return System.ConsoleColor.White;
            }

            public override string ToString() => $"#{R:X2}{G:X2}{B:X2}";
        }

        /// <summary>
        /// Represents a colored text segment with foreground and optional background
        /// </summary>
        public class ColoredSegment
        {
            public string Text { get; set; } = "";
            public ColorRGB? Foreground { get; set; }
            public ColorRGB? Background { get; set; }
            
            public ColoredSegment(string text, ColorRGB? fg = null, ColorRGB? bg = null)
            {
                Text = text;
                Foreground = fg;
                Background = bg;
            }
            
            /// <summary>
            /// Applies a brightness adjustment to this segment's foreground color
            /// </summary>
            /// <param name="brightnessPercent">Percentage adjustment (-100 to +100, where -5 = 5% darker, +5 = 5% brighter)</param>
            public void ApplyBrightness(float brightnessPercent)
            {
                if (Foreground.HasValue)
                {
                    Foreground = AdjustBrightness(Foreground.Value, brightnessPercent);
                }
            }
        }

        /// <summary>
        /// Gets the RGB color for a given color code
        /// </summary>
        public static ColorRGB? GetColor(char code)
        {
            return colorMap.TryGetValue(code, out var color) ? color : null;
        }

        /// <summary>
        /// Checks if a character is a valid color code
        /// </summary>
        public static bool IsValidColorCode(char code)
        {
            return colorMap.ContainsKey(code);
        }

        /// <summary>
        /// Gets all available color codes
        /// </summary>
        public static IEnumerable<char> GetAllColorCodes()
        {
            return colorMap.Keys;
        }

        /// <summary>
        /// Gets the default text color (grey)
        /// </summary>
        public static ColorRGB DefaultTextColor => colorMap['y'];

        /// <summary>
        /// Gets the default background color (black/dark)
        /// </summary>
        public static ColorRGB DefaultBackgroundColor => colorMap['K'];
        
        /// <summary>
        /// Adjusts the brightness of a color
        /// </summary>
        /// <param name="color">The color to adjust</param>
        /// <param name="brightnessPercent">Percentage adjustment (-100 to +100, where -5 = 5% darker, +5 = 5% brighter)</param>
        /// <returns>The adjusted color</returns>
        public static ColorRGB AdjustBrightness(ColorRGB color, float brightnessPercent)
        {
            float factor = 1.0f + (brightnessPercent / 100.0f);
            
            byte newR = (byte)Math.Clamp((int)(color.R * factor), 0, 255);
            byte newG = (byte)Math.Clamp((int)(color.G * factor), 0, 255);
            byte newB = (byte)Math.Clamp((int)(color.B * factor), 0, 255);
            
            return new ColorRGB(newR, newG, newB);
        }
    }
}

