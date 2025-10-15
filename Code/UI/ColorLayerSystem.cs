using System;
using Avalonia.Media;

namespace RPGGame.UI
{
    /// <summary>
    /// Significance levels for events - affects brightness and saturation
    /// </summary>
    public enum EventSignificance
    {
        Trivial,      // Very dim, low saturation (minor events)
        Minor,        // Dim, reduced saturation (routine events)
        Normal,       // Standard brightness and saturation
        Important,    // Bright, full saturation (key events)
        Critical      // Very bright, high saturation (major events)
    }

    /// <summary>
    /// White temperature types for atmospheric progression
    /// </summary>
    public enum WhiteTemperature
    {
        Warm,      // Warm white (yellowish tint) - early dungeon
        Neutral,   // Pure white - mid dungeon
        Cool       // Cool white (bluish tint) - deep dungeon
    }

    /// <summary>
    /// Layer system that adjusts color brightness and saturation based on event significance
    /// Also provides atmospheric white color progression for dungeon depth
    /// </summary>
    public static class ColorLayerSystem
    {
        // Brightness multipliers for different significance levels
        private static readonly float[] brightnessMultipliers = new float[]
        {
            0.4f,  // Trivial
            0.6f,  // Minor
            1.0f,  // Normal
            1.2f,  // Important
            1.5f   // Critical
        };

        // Saturation multipliers for different significance levels
        private static readonly float[] saturationMultipliers = new float[]
        {
            0.3f,  // Trivial
            0.5f,  // Minor
            1.0f,  // Normal
            1.2f,  // Important
            1.4f   // Critical
        };

        // Cached temperature intensity value
        private static float? _cachedIntensity = null;
        private static object _intensityLock = new object();
        
        // Force reload flag for testing
        private static bool _forceReload = true;

        /// <summary>
        /// Gets the white temperature intensity from configuration (cached)
        /// </summary>
        private static float GetTemperatureIntensity()
        {
            // Force reload for first call to ensure we get latest config
            if (_forceReload || !_cachedIntensity.HasValue)
            {
                lock (_intensityLock)
                {
                    _forceReload = false;
                    
                    try
                    {
                        var config = RPGGame.UIConfiguration.LoadFromFile();
                        _cachedIntensity = (float)config.WhiteTemperatureIntensity;
                        System.Diagnostics.Debug.WriteLine($"[ColorLayerSystem] Loaded intensity: {_cachedIntensity.Value}");
                        return _cachedIntensity.Value;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ColorLayerSystem] Error loading intensity: {ex.Message}");
                        _cachedIntensity = 1.0f; // Default intensity if config can't be loaded
                        return _cachedIntensity.Value;
                    }
                }
            }

            return _cachedIntensity.Value;
        }

        /// <summary>
        /// Clears the cached temperature intensity, forcing a reload from config on next access
        /// Call this after updating UIConfiguration.json
        /// </summary>
        public static void RefreshTemperatureIntensity()
        {
            lock (_intensityLock)
            {
                _cachedIntensity = null;
            }
        }

        /// <summary>
        /// Applies significance-based brightness and saturation to a color
        /// </summary>
        public static ColorDefinitions.ColorRGB ApplySignificance(
            ColorDefinitions.ColorRGB baseColor, 
            EventSignificance significance)
        {
            float brightnessMult = brightnessMultipliers[(int)significance];
            float saturationMult = saturationMultipliers[(int)significance];

            return AdjustColor(baseColor, brightnessMult, saturationMult);
        }

        /// <summary>
        /// Gets a white color based on temperature and dungeon depth
        /// </summary>
        /// <param name="temperature">Warm, Neutral, or Cool</param>
        /// <param name="brightness">Brightness level (0.0 to 1.5+)</param>
        public static ColorDefinitions.ColorRGB GetWhite(WhiteTemperature temperature, float brightness = 1.0f)
        {
            float intensity = GetTemperatureIntensity();
            ColorDefinitions.ColorRGB baseWhite;

            switch (temperature)
            {
                case WhiteTemperature.Warm:
                    // Warm white - yellowish tint (intensity controls how yellow)
                    // At intensity=1.0: RGB(255, 250, 220)
                    // At intensity=0.0: RGB(255, 255, 255) (pure white)
                    // At intensity=2.0: RGB(255, 245, 185) (more yellow)
                    byte warmR = 255;
                    byte warmG = (byte)Math.Max(0, Math.Min(255, 255 - (5 * intensity)));
                    byte warmB = (byte)Math.Max(0, Math.Min(255, 255 - (35 * intensity)));
                    baseWhite = new ColorDefinitions.ColorRGB(warmR, warmG, warmB);
                    break;

                case WhiteTemperature.Cool:
                    // Cool white - bluish tint (intensity controls how blue)
                    // At intensity=1.0: RGB(220, 230, 255)
                    // At intensity=0.0: RGB(255, 255, 255) (pure white)
                    // At intensity=2.0: RGB(185, 205, 255) (more blue)
                    byte coolR = (byte)Math.Max(0, Math.Min(255, 255 - (35 * intensity)));
                    byte coolG = (byte)Math.Max(0, Math.Min(255, 255 - (25 * intensity)));
                    byte coolB = 255;
                    baseWhite = new ColorDefinitions.ColorRGB(coolR, coolG, coolB);
                    break;

                case WhiteTemperature.Neutral:
                default:
                    // Pure white (unaffected by intensity)
                    baseWhite = new ColorDefinitions.ColorRGB(255, 255, 255);
                    break;
            }

            return AdjustBrightness(baseWhite, brightness);
        }

        /// <summary>
        /// Calculates white temperature based on dungeon depth
        /// </summary>
        /// <param name="currentRoom">Current room number</param>
        /// <param name="totalRooms">Total rooms in dungeon</param>
        /// <returns>Interpolated white color from warm to cool</returns>
        public static ColorDefinitions.ColorRGB GetWhiteByDepth(int currentRoom, int totalRooms, float brightness = 1.0f)
        {
            if (totalRooms <= 1)
            {
                return GetWhite(WhiteTemperature.Neutral, brightness);
            }

            // Calculate progression (0.0 = start, 1.0 = end)
            float progression = (float)(currentRoom - 1) / (totalRooms - 1);

            // Interpolate between warm (start) and cool (end)
            return InterpolateWhiteTemperature(progression, brightness);
        }

        /// <summary>
        /// Interpolates between warm and cool white based on progression
        /// </summary>
        private static ColorDefinitions.ColorRGB InterpolateWhiteTemperature(float progression, float brightness)
        {
            // Clamp progression to 0-1
            progression = Math.Max(0f, Math.Min(1f, progression));
            
            float intensity = GetTemperatureIntensity();

            // Calculate warm white endpoint (start): 
            // At intensity=1.0: RGB(255, 250, 220)
            // At intensity=0.0: RGB(255, 255, 255)
            float warmR = 255;
            float warmG = 255 - (5 * intensity);
            float warmB = 255 - (35 * intensity);
            
            // Calculate cool white endpoint (end):
            // At intensity=1.0: RGB(220, 230, 255)
            // At intensity=0.0: RGB(255, 255, 255)
            float coolR = 255 - (35 * intensity);
            float coolG = 255 - (25 * intensity);
            float coolB = 255;
            
            // Interpolate between warm and cool based on progression
            byte r = (byte)Math.Max(0, Math.Min(255, warmR + ((coolR - warmR) * progression)));
            byte g = (byte)Math.Max(0, Math.Min(255, warmG + ((coolG - warmG) * progression)));
            byte b = (byte)Math.Max(0, Math.Min(255, warmB + ((coolB - warmB) * progression)));

            var baseColor = new ColorDefinitions.ColorRGB(r, g, b);
            return AdjustBrightness(baseColor, brightness);
        }

        /// <summary>
        /// Adjusts color brightness and saturation
        /// </summary>
        private static ColorDefinitions.ColorRGB AdjustColor(
            ColorDefinitions.ColorRGB color, 
            float brightnessMult, 
            float saturationMult)
        {
            // Convert RGB to HSL
            var (h, s, l) = RGBtoHSL(color.R, color.G, color.B);

            // Adjust saturation and lightness
            s = Math.Max(0f, Math.Min(1f, s * saturationMult));
            l = Math.Max(0f, Math.Min(1f, l * brightnessMult));

            // Convert back to RGB
            var (r, g, b) = HSLtoRGB(h, s, l);

            return new ColorDefinitions.ColorRGB(r, g, b);
        }

        /// <summary>
        /// Adjusts only brightness
        /// </summary>
        private static ColorDefinitions.ColorRGB AdjustBrightness(
            ColorDefinitions.ColorRGB color, 
            float brightnessMult)
        {
            byte r = (byte)Math.Max(0, Math.Min(255, color.R * brightnessMult));
            byte g = (byte)Math.Max(0, Math.Min(255, color.G * brightnessMult));
            byte b = (byte)Math.Max(0, Math.Min(255, color.B * brightnessMult));

            return new ColorDefinitions.ColorRGB(r, g, b);
        }

        /// <summary>
        /// Converts RGB to HSL
        /// </summary>
        private static (float h, float s, float l) RGBtoHSL(byte r, byte g, byte b)
        {
            float rNorm = r / 255f;
            float gNorm = g / 255f;
            float bNorm = b / 255f;

            float max = Math.Max(rNorm, Math.Max(gNorm, bNorm));
            float min = Math.Min(rNorm, Math.Min(gNorm, bNorm));
            float delta = max - min;

            float h = 0f, s = 0f, l = (max + min) / 2f;

            if (delta != 0f)
            {
                s = l < 0.5f ? delta / (max + min) : delta / (2f - max - min);

                if (max == rNorm)
                {
                    h = ((gNorm - bNorm) / delta) % 6f;
                }
                else if (max == gNorm)
                {
                    h = ((bNorm - rNorm) / delta) + 2f;
                }
                else
                {
                    h = ((rNorm - gNorm) / delta) + 4f;
                }

                h *= 60f;
                if (h < 0f) h += 360f;
            }

            return (h, s, l);
        }

        /// <summary>
        /// Converts HSL to RGB
        /// </summary>
        private static (byte r, byte g, byte b) HSLtoRGB(float h, float s, float l)
        {
            float c = (1f - Math.Abs(2f * l - 1f)) * s;
            float x = c * (1f - Math.Abs((h / 60f) % 2f - 1f));
            float m = l - c / 2f;

            float r1, g1, b1;

            if (h < 60f)
            {
                r1 = c; g1 = x; b1 = 0f;
            }
            else if (h < 120f)
            {
                r1 = x; g1 = c; b1 = 0f;
            }
            else if (h < 180f)
            {
                r1 = 0f; g1 = c; b1 = x;
            }
            else if (h < 240f)
            {
                r1 = 0f; g1 = x; b1 = c;
            }
            else if (h < 300f)
            {
                r1 = x; g1 = 0f; b1 = c;
            }
            else
            {
                r1 = c; g1 = 0f; b1 = x;
            }

            byte r = (byte)Math.Round((r1 + m) * 255f);
            byte g = (byte)Math.Round((g1 + m) * 255f);
            byte b = (byte)Math.Round((b1 + m) * 255f);

            return (r, g, b);
        }

        /// <summary>
        /// Applies significance layer to parsed color segments
        /// </summary>
        public static void ApplySignificanceToSegments(
            System.Collections.Generic.List<ColorDefinitions.ColoredSegment> segments,
            EventSignificance significance)
        {
            foreach (var segment in segments)
            {
                if (segment.Foreground.HasValue)
                {
                    segment.Foreground = ApplySignificance(segment.Foreground.Value, significance);
                }

                if (segment.Background.HasValue)
                {
                    segment.Background = ApplySignificance(segment.Background.Value, significance);
                }
            }
        }

        /// <summary>
        /// Creates a colored segment with significance-adjusted color
        /// </summary>
        public static ColorDefinitions.ColoredSegment CreateSignificantSegment(
            string text,
            ColorDefinitions.ColorRGB baseColor,
            EventSignificance significance)
        {
            var adjustedColor = ApplySignificance(baseColor, significance);
            return new ColorDefinitions.ColoredSegment(text, adjustedColor);
        }

        /// <summary>
        /// Creates a white text segment based on dungeon depth
        /// </summary>
        public static ColorDefinitions.ColoredSegment CreateDepthWhiteSegment(
            string text,
            int currentRoom,
            int totalRooms,
            float brightness = 1.0f)
        {
            var whiteColor = GetWhiteByDepth(currentRoom, totalRooms, brightness);
            return new ColorDefinitions.ColoredSegment(text, whiteColor);
        }
    }
}

