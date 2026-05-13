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
        public readonly struct ExclusiveD20OutcomeChances
        {
            public ExclusiveD20OutcomeChances(int critPercent, int comboPercent, int hitPercent, int critMissPercent)
            {
                CritPercent = critPercent;
                ComboPercent = comboPercent;
                HitPercent = hitPercent;
                CritMissPercent = critMissPercent;
            }

            public int CritPercent { get; }
            public int ComboPercent { get; }
            public int HitPercent { get; }
            public int CritMissPercent { get; }

            /// <summary>
            /// d20 faces that are not crit, combo, normal hit, or crit miss (plain miss band between crit miss and hit).
            /// </summary>
            public int MissPercent => Math.Clamp(100 - CritPercent - ComboPercent - HitPercent - CritMissPercent, 0, 100);
        }

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
        /// Signed delta in parentheses, e.g. <c> (-4)</c> or <c> (+1)</c>; empty when <paramref name="delta"/> is 0.
        /// Used for dice threshold rows: displayed value minus configured baseline (queued shifts and stored
        /// deltas combined). Negative = easier on the crit/combo/hit ladder.
        /// </summary>
        public static string FormatSignedDeltaSuffix(int delta) =>
            delta == 0 ? "" : $" ({delta:+0;-0;0})";

        /// <summary>
        /// Converts effective d20 ladder rows into exclusive outcome percentages.
        /// Priority follows the combat ladder shown in the HUD: Crit, then Combo, then normal Hit, then Crit Miss.
        /// </summary>
        public static ExclusiveD20OutcomeChances CalculateExclusiveD20OutcomeChances(
            int critMinRoll,
            int comboMinRoll,
            int hitMinRoll,
            int critMissMaxRoll)
        {
            int crit = 0;
            int combo = 0;
            int hit = 0;
            int critMiss = 0;

            for (int roll = 1; roll <= 20; roll++)
            {
                if (roll >= critMinRoll)
                    crit++;
                else if (roll >= comboMinRoll)
                    combo++;
                else if (roll >= hitMinRoll)
                    hit++;
                else if (roll <= critMissMaxRoll)
                    critMiss++;
            }

            return new ExclusiveD20OutcomeChances(
                crit * 5,
                combo * 5,
                hit * 5,
                critMiss * 5);
        }

        public static string FormatD20ChancePercent(int percent) => $"{Math.Clamp(percent, 0, 100)}%";

        /// <summary>
        /// Color for percent chance rows when toggled from ladder thresholds.
        /// Positive chance movement is green; negative movement is red.
        /// </summary>
        public static Color GetChanceDeltaColor(int currentPercent, int defaultPercent)
        {
            if (currentPercent == defaultPercent)
                return AsciiArtAssets.Colors.White;
            return currentPercent > defaultPercent ? AsciiArtAssets.Colors.Green : AsciiArtAssets.Colors.Red;
        }

        /// <summary>
        /// Color for combined threshold delta parenthetical (same sign sense as former roll-accuracy parens).
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
