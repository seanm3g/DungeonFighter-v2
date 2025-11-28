using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// System for managing color layers, brightness, saturation, and depth-based coloring
    /// </summary>
    public static class ColorLayerSystem
    {
        /// <summary>
        /// Creates a colored text segment with significance-based brightness and saturation
        /// </summary>
        public static List<ColoredText> CreateSignificantSegment(string text, Color baseColor, EventSignificance significance)
        {
            var adjustedColor = AdjustColorForSignificance(baseColor, significance);
            return new List<ColoredText> { new ColoredText(text, adjustedColor) };
        }
        
        /// <summary>
        /// Creates a colored text segment with depth-based white coloring
        /// </summary>
        public static List<ColoredText> CreateDepthWhiteSegment(string text, int depth, WhiteTemperature temperature = WhiteTemperature.Neutral)
        {
            var color = GetWhiteByDepth(depth, temperature);
            return new List<ColoredText> { new ColoredText(text, color) };
        }
        
        /// <summary>
        /// Gets a white color with specified temperature
        /// </summary>
        public static Color GetWhite(WhiteTemperature temperature)
        {
            return temperature switch
            {
                WhiteTemperature.Warm => Color.FromRgb(255, 248, 240), // Warm white
                WhiteTemperature.Cool => Color.FromRgb(240, 248, 255), // Cool white
                WhiteTemperature.Neutral => Color.FromRgb(255, 255, 255), // Pure white
                _ => Color.FromRgb(255, 255, 255)
            };
        }
        
        /// <summary>
        /// Gets a white color with specified temperature and brightness
        /// </summary>
        public static Color GetWhite(WhiteTemperature temperature, double brightness)
        {
            var baseColor = GetWhite(temperature);
            return AdjustBrightness(baseColor, brightness);
        }
        
        /// <summary>
        /// Gets a white color based on depth (darker for deeper levels)
        /// </summary>
        public static Color GetWhiteByDepth(int depth, WhiteTemperature temperature = WhiteTemperature.Neutral)
        {
            var baseColor = GetWhite(temperature);
            
            // Darker colors for deeper levels (0 = surface, higher = deeper)
            var depthFactor = Math.Max(0.3, 1.0 - (depth * 0.1)); // Minimum 30% brightness
            return AdjustBrightness(baseColor, depthFactor);
        }
        
        /// <summary>
        /// Creates layered colored text with multiple significance levels
        /// </summary>
        public static List<ColoredText> CreateLayeredText(string text, Color baseColor, params EventSignificance[] layers)
        {
            var result = new List<ColoredText>();
            
            foreach (var layer in layers)
            {
                var adjustedColor = AdjustColorForSignificance(baseColor, layer);
                result.Add(new ColoredText(text, adjustedColor));
            }
            
            return result;
        }
        
        /// <summary>
        /// Adjusts color brightness and saturation based on event significance
        /// </summary>
        private static Color AdjustColorForSignificance(Color baseColor, EventSignificance significance)
        {
            return significance switch
            {
                EventSignificance.Trivial => AdjustBrightness(baseColor, 0.3), // Very dim
                EventSignificance.Minor => AdjustBrightness(baseColor, 0.6), // Dim
                EventSignificance.Normal => baseColor, // Normal
                EventSignificance.Important => AdjustSaturation(baseColor, 1.2), // More saturated
                EventSignificance.Critical => AdjustBrightness(AdjustSaturation(baseColor, 1.3), 1.2), // Bright and saturated
                _ => baseColor
            };
        }
        
        /// <summary>
        /// Adjusts the brightness of a color
        /// </summary>
        private static Color AdjustBrightness(Color color, double factor)
        {
            factor = Math.Max(0.0, Math.Min(2.0, factor)); // Clamp between 0 and 2
            
            var r = (byte)Math.Min(255, color.R * factor);
            var g = (byte)Math.Min(255, color.G * factor);
            var b = (byte)Math.Min(255, color.B * factor);
            
            return Color.FromRgb(r, g, b);
        }
        
        /// <summary>
        /// Adjusts the saturation of a color
        /// </summary>
        private static Color AdjustSaturation(Color color, double factor)
        {
            factor = Math.Max(0.0, Math.Min(2.0, factor)); // Clamp between 0 and 2
            
            // Convert to HSL, adjust saturation, convert back
            var (h, s, l) = RgbToHsl(color.R, color.G, color.B);
            s = Math.Min(1.0, s * factor);
            var (r, g, b) = HslToRgb(h, s, l);
            
            return Color.FromRgb(r, g, b);
        }
        
        /// <summary>
        /// Converts RGB to HSL
        /// </summary>
        private static (double h, double s, double l) RgbToHsl(byte r, byte g, byte b)
        {
            var rf = r / 255.0;
            var gf = g / 255.0;
            var bf = b / 255.0;
            
            var max = Math.Max(rf, Math.Max(gf, bf));
            var min = Math.Min(rf, Math.Min(gf, bf));
            var delta = max - min;
            
            var l = (max + min) / 2.0;
            var s = delta == 0 ? 0 : delta / (1 - Math.Abs(2 * l - 1));
            
            double h = 0;
            if (delta != 0)
            {
                if (max == rf)
                    h = 60 * (((gf - bf) / delta) % 6);
                else if (max == gf)
                    h = 60 * ((bf - rf) / delta + 2);
                else if (max == bf)
                    h = 60 * ((rf - gf) / delta + 4);
            }
            
            if (h < 0) h += 360;
            
            return (h, s, l);
        }
        
        /// <summary>
        /// Converts HSL to RGB
        /// </summary>
        private static (byte r, byte g, byte b) HslToRgb(double h, double s, double l)
        {
            var c = (1 - Math.Abs(2 * l - 1)) * s;
            var x = c * (1 - Math.Abs((h / 60) % 2 - 1));
            var m = l - c / 2;
            
            double rf, gf, bf;
            
            if (h < 60)
            {
                rf = c; gf = x; bf = 0;
            }
            else if (h < 120)
            {
                rf = x; gf = c; bf = 0;
            }
            else if (h < 180)
            {
                rf = 0; gf = c; bf = x;
            }
            else if (h < 240)
            {
                rf = 0; gf = x; bf = c;
            }
            else if (h < 300)
            {
                rf = x; gf = 0; bf = c;
            }
            else
            {
                rf = c; gf = 0; bf = x;
            }
            
            var r = (byte)Math.Min(255, Math.Max(0, (rf + m) * 255));
            var g = (byte)Math.Min(255, Math.Max(0, (gf + m) * 255));
            var b = (byte)Math.Min(255, Math.Max(0, (bf + m) * 255));
            
            return (r, g, b);
        }
    }
    
    /// <summary>
    /// Represents the significance level of an event for color adjustment
    /// </summary>
    public enum EventSignificance
    {
        Trivial,
        Minor,
        Normal,
        Important,
        Critical
    }
    
    /// <summary>
    /// Represents the temperature of white color
    /// </summary>
    public enum WhiteTemperature
    {
        Warm,
        Neutral,
        Cool
    }
}
