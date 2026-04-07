using Avalonia.Media;
using RPGGame.UI.Avalonia;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Display helpers for the character panel THRESHOLDS section.
    /// Lower numeric threshold than default means an easier requirement (benefit); higher means harder (penalty).
    /// </summary>
    public static class ThresholdDisplayFormatting
    {
        public static Color GetValueColor(int current, int defaultValue)
        {
            if (current == defaultValue)
                return AsciiArtAssets.Colors.White;
            return (defaultValue - current) > 0 ? AsciiArtAssets.Colors.Green : AsciiArtAssets.Colors.Red;
        }

        /// <summary>Suffix like " (+2)" or " (-1)" when current differs from default; empty when equal.</summary>
        public static string FormatDeltaSuffix(int current, int defaultValue)
        {
            if (current == defaultValue)
                return "";
            return (defaultValue - current) > 0 ? $" (+{defaultValue - current})" : $" ({defaultValue - current})";
        }
    }
}
