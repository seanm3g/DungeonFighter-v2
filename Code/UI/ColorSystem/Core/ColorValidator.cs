using System;
using Avalonia.Media;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// Utility class for validating and ensuring colors are visible on black background.
    /// Also provides color comparison utilities.
    /// </summary>
    public static class ColorValidator
    {
        /// <summary>
        /// Minimum brightness threshold (0-255) to ensure visibility on black background.
        /// Colors below this threshold will be lightened.
        /// </summary>
        private const int MIN_BRIGHTNESS = 50;
        
        /// <summary>
        /// Checks if two colors are equal by comparing RGB and alpha values.
        /// Centralized utility method to avoid duplication across color system files.
        /// </summary>
        public static bool AreColorsEqual(Color a, Color b)
        {
            return a.R == b.R && a.G == b.G && a.B == b.B && a.A == b.A;
        }
        
        /// <summary>
        /// Calculates the perceived brightness (luminance) of a color.
        /// Uses the standard formula: 0.299*R + 0.587*G + 0.114*B
        /// </summary>
        public static double GetBrightness(Color color)
        {
            return 0.299 * color.R + 0.587 * color.G + 0.114 * color.B;
        }

        /// <summary>
        /// Converts sRGB bytes to HSV. H is degrees [0,360), S and V are in [0,1] (Value = max channel, standard HS model).
        /// </summary>
        public static (double H, double S, double V) RgbToHsv(byte r, byte g, byte b)
        {
            double rn = r / 255.0, gn = g / 255.0, bn = b / 255.0;
            double max = Math.Max(rn, Math.Max(gn, bn));
            double min = Math.Min(rn, Math.Min(gn, bn));
            double delta = max - min;

            double H = 0;
            if (delta > 1e-12)
            {
                if (Math.Abs(max - rn) < 1e-12)
                {
                    double x = ((gn - bn) / delta) % 6;
                    if (x < 0) x += 6;
                    H = 60 * x;
                }
                else if (Math.Abs(max - gn) < 1e-12)
                    H = 60 * (((bn - rn) / delta) + 2);
                else
                    H = 60 * (((rn - gn) / delta) + 4);
            }

            if (H < 0) H += 360;
            if (H >= 360) H -= 360;

            double S = max <= 1e-12 ? 0 : delta / max;
            double V = max;
            return (H, S, V);
        }

        /// <summary>
        /// Converts HSV to sRGB. H in degrees, S and V in [0,1].
        /// </summary>
        public static Color HsvToColor(double H, double S, double V)
        {
            H %= 360;
            if (H < 0) H += 360;
            S = Math.Clamp(S, 0, 1);
            V = Math.Clamp(V, 0, 1);

            double C = V * S;
            double h60 = H / 60.0;
            double X = C * (1 - Math.Abs(h60 % 2 - 1));
            double m = V - C;

            double r1 = 0, g1 = 0, b1 = 0;
            if (H < 60) { r1 = C; g1 = X; b1 = 0; }
            else if (H < 120) { r1 = X; g1 = C; b1 = 0; }
            else if (H < 180) { r1 = 0; g1 = C; b1 = X; }
            else if (H < 240) { r1 = 0; g1 = X; b1 = C; }
            else if (H < 300) { r1 = X; g1 = 0; b1 = C; }
            else { r1 = C; g1 = 0; b1 = X; }

            int ri = (int)Math.Round((r1 + m) * 255);
            int gi = (int)Math.Round((g1 + m) * 255);
            int bi = (int)Math.Round((b1 + m) * 255);
            return Color.FromRgb(
                (byte)Math.Clamp(ri, 0, 255),
                (byte)Math.Clamp(gi, 0, 255),
                (byte)Math.Clamp(bi, 0, 255));
        }

        /// <summary>
        /// HSV Value (0–255 scale), i.e. max RGB channel relative to white.
        /// </summary>
        public static double GetHsvValue255(Color color)
        {
            var (_, _, V) = RgbToHsv(color.R, color.G, color.B);
            return V * 255.0;
        }
        
        /// <summary>
        /// Checks if a color is too dark to be visible on a black background.
        /// </summary>
        public static bool IsTooDark(Color color)
        {
            return GetBrightness(color) < MIN_BRIGHTNESS;
        }
        
        /// <summary>
        /// Ensures a color is visible on black background by lightening it if necessary.
        /// Preserves the color's hue while increasing brightness.
        /// </summary>
        public static Color EnsureVisible(Color color)
        {
            if (!IsTooDark(color))
                return color;
            
            // Calculate current brightness
            double brightness = GetBrightness(color);
            
            // Lighten the color while preserving hue
            // Use a blend with white to lighten while maintaining color character
            double blendFactor = 0.5; // Blend 50% with white for very dark colors
            
            byte newR = (byte)Math.Min(255, color.R + (255 - color.R) * blendFactor);
            byte newG = (byte)Math.Min(255, color.G + (255 - color.G) * blendFactor);
            byte newB = (byte)Math.Min(255, color.B + (255 - color.B) * blendFactor);
            
            // If still too dark, use a minimum brightness approach
            Color lightened = Color.FromRgb(newR, newG, newB);
            if (IsTooDark(lightened))
            {
                // For extremely dark colors, use a gray that's definitely visible
                // But try to preserve some color character
                int minComponent = Math.Max(newR, Math.Max(newG, newB));
                if (minComponent < MIN_BRIGHTNESS)
                {
                    // Scale up the brightest component to at least MIN_BRIGHTNESS
                    double scale = MIN_BRIGHTNESS / (double)minComponent;
                    newR = (byte)Math.Min(255, (int)(color.R * scale));
                    newG = (byte)Math.Min(255, (int)(color.G * scale));
                    newB = (byte)Math.Min(255, (int)(color.B * scale));
                }
            }
            
            return Color.FromRgb(newR, newG, newB);
        }

        /// <summary>
        /// Scales HSV Value (brightness) by <paramref name="factor"/> (clamped 0–2). Preserves hue and saturation.
        /// </summary>
        public static Color ScaleBrightnessHsv(Color color, double factor)
        {
            factor = Math.Clamp(factor, 0.0, 2.0);
            var (H, S, V) = RgbToHsv(color.R, color.G, color.B);
            double v2 = Math.Clamp(V * factor, 0, 1);
            return HsvToColor(H, S, v2);
        }

        /// <summary>
        /// Clamps HSV Value into <paramref name="minBrightness"/>..<paramref name="maxBrightness"/> on a 0–255 scale
        /// (mapped to V = x/255). Preserves hue and saturation; does not wash colors toward white except when S=0.
        /// </summary>
        public static Color ClampAnimatedTextBrightness(Color color, double minBrightness, double maxBrightness)
        {
            minBrightness = Math.Clamp(minBrightness, 0, 255);
            maxBrightness = Math.Clamp(maxBrightness, 0, 255);
            if (minBrightness > maxBrightness)
                (minBrightness, maxBrightness) = (maxBrightness, minBrightness);

            if (minBrightness <= 0 && maxBrightness >= 255)
                return color;

            double vmin = minBrightness / 255.0;
            double vmax = maxBrightness / 255.0;

            var (H, S, V) = RgbToHsv(color.R, color.G, color.B);
            double v2 = Math.Clamp(V, vmin, vmax);
            return HsvToColor(H, S, v2);
        }
    }
}

