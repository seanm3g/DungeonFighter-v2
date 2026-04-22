using System;
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

        /// <summary>
        /// Floors displayed d20 ladder numbers (crit / combo / hit / crit-miss) so large queued threshold shifts
        /// (e.g. deferred <c>ACCURACY</c> from CAST) never show impossible negatives; matches
        /// <see cref="RPGGame.ActionSelector.GetEffectiveComboThresholdForSelection"/>.
        /// </summary>
        public static int ClampDiceLadderDisplayValue(int computedEffective) => Math.Max(1, computedEffective);

        /// <summary>Suffix like " (+2)" or " (-1)" when current differs from default; empty when equal.</summary>
        public static string FormatDeltaSuffix(int current, int defaultValue)
        {
            if (current == defaultValue)
                return "";
            return (defaultValue - current) > 0 ? $" (+{defaultValue - current})" : $" ({defaultValue - current})";
        }

        /// <summary>
        /// Color for the roll-accuracy parenthetical from <see cref="FormatThresholdValueWithAccuracy"/>.
        /// Positive delta (e.g. penalty <c>(+2)</c>) is worse for the player → red; negative (<c>(-3)</c> bonus) → green.
        /// </summary>
        public static Color GetAccuracyDeltaParenColor(int delta)
        {
            if (delta == 0)
                return AsciiArtAssets.Colors.White;
            return delta > 0 ? AsciiArtAssets.Colors.Red : AsciiArtAssets.Colors.Green;
        }

        /// <summary>
        /// Shows the effective threshold after applying roll accuracy (positive bonus lowers the number needed)
        /// and the change from base in parentheses, e.g. <c>19 (-1)</c> for base 20 with +1 bonus, <c>8 (+2)</c> for a penalty.
        /// </summary>
        public static string FormatThresholdValueWithAccuracy(int baseThreshold, int accuracy)
        {
            if (accuracy == 0)
                return baseThreshold.ToString();
            int effective = baseThreshold - accuracy;
            int delta = -accuracy;
            return $"{effective} ({delta:+0;-0;0})";
        }

        /// <summary>Effective value and parenthetical string (including leading space) for split rendering; empty suffix when accuracy is 0.</summary>
        public static void GetThresholdValueWithAccuracyParts(int baseThreshold, int accuracy, out int effective, out string parenSuffix, out int accuracyDelta)
        {
            accuracyDelta = -accuracy;
            if (accuracy == 0)
            {
                effective = baseThreshold;
                parenSuffix = "";
                return;
            }

            effective = baseThreshold - accuracy;
            parenSuffix = $" ({accuracyDelta:+0;-0;0})";
        }
    }
}
