using System;
using System.Collections.Generic;
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

        public readonly struct D20ChanceDisplayRow
        {
            public D20ChanceDisplayRow(string label, int percent)
            {
                Label = label;
                Percent = percent;
            }

            public string Label { get; }
            public int Percent { get; }
        }

        public readonly struct D20OutcomeSegment
        {
            public D20OutcomeSegment(string label, int faceCount, Color color)
            {
                Label = label;
                FaceCount = faceCount;
                Percent = faceCount * 5;
                Color = color;
            }

            public string Label { get; }
            public int FaceCount { get; }
            public int Percent { get; }
            public Color Color { get; }
        }

        /// <summary>Palette for d20 threshold bar segments (Crit Miss → Crit, left to right).</summary>
        public static class D20ThresholdBarColors
        {
            public static readonly Color CritMiss = Color.Parse("#C0392B");
            public static readonly Color Miss = Color.Parse("#E07B54");
            public static readonly Color Hit = Color.Parse("#5B8DB8");
            public static readonly Color Combo = Color.Parse("#9B59B6");
            public static readonly Color Crit = Color.Parse("#F0C040");
        }

        /// <summary>Bar segment color for a d20 outcome label (Crit Miss → Crit, left to right on the bar).</summary>
        public static Color GetD20OutcomeColor(string label) =>
            label switch
            {
                "Crit Miss" => D20ThresholdBarColors.CritMiss,
                "Miss" => D20ThresholdBarColors.Miss,
                "Hit" => D20ThresholdBarColors.Hit,
                "Combo" => D20ThresholdBarColors.Combo,
                "Crit" => D20ThresholdBarColors.Crit,
                _ => Colors.Gray
            };

        /// <summary>
        /// Builds left-to-right d20 bar segments (roll 1 → 20) using the same exclusive outcome priority as combat.
        /// </summary>
        public static D20OutcomeSegment[] BuildD20OutcomeSegments(
            int critMinRoll,
            int comboMinRoll,
            int hitMinRoll,
            int critMissMaxRoll)
        {
            var segments = new List<D20OutcomeSegment>(5);
            string? currentLabel = null;
            int currentFaceCount = 0;

            for (int roll = 1; roll <= 20; roll++)
            {
                string label = ClassifyExclusiveD20Outcome(roll, critMinRoll, comboMinRoll, hitMinRoll, critMissMaxRoll);
                if (label == currentLabel)
                {
                    currentFaceCount++;
                    continue;
                }

                if (currentLabel != null)
                    segments.Add(new D20OutcomeSegment(currentLabel, currentFaceCount, GetD20OutcomeColor(currentLabel)));

                currentLabel = label;
                currentFaceCount = 1;
            }

            if (currentLabel != null)
                segments.Add(new D20OutcomeSegment(currentLabel, currentFaceCount, GetD20OutcomeColor(currentLabel)));

            return segments.ToArray();
        }

        /// <summary>
        /// Character-column offset from the bar's left edge to the center of a d20 face (1-based roll).
        /// Matches segmented-bar face geometry: width * (roll - 0.5) / totalFaces.
        /// </summary>
        public static double GetRollFaceCenterCharOffset(int roll, int barWidth, int totalFaces = 20)
        {
            if (barWidth <= 0 || totalFaces <= 0)
                return 0;
            roll = Math.Clamp(roll, 1, totalFaces);
            return (roll - 0.5) * barWidth / (double)totalFaces;
        }

        /// <summary>
        /// Returns the left-to-right bar segment index (0-based) for a d20 roll using the same exclusive outcome priority as combat.
        /// </summary>
        public static int FindSegmentIndexForRoll(
            int roll,
            int critMinRoll,
            int comboMinRoll,
            int hitMinRoll,
            int critMissMaxRoll)
        {
            roll = Math.Clamp(roll, 1, 20);
            string label = ClassifyExclusiveD20Outcome(roll, critMinRoll, comboMinRoll, hitMinRoll, critMissMaxRoll);
            var segments = BuildD20OutcomeSegments(critMinRoll, comboMinRoll, hitMinRoll, critMissMaxRoll);
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i].Label == label)
                    return i;
            }

            return -1;
        }

        private static string ClassifyExclusiveD20Outcome(
            int roll,
            int critMinRoll,
            int comboMinRoll,
            int hitMinRoll,
            int critMissMaxRoll)
        {
            if (roll >= critMinRoll)
                return "Crit";
            if (roll >= comboMinRoll)
                return "Combo";
            if (roll >= hitMinRoll)
                return "Hit";
            if (roll <= critMissMaxRoll)
                return "Crit Miss";
            return "Miss";
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
        /// Canonical row order for CHANCES data (Crit → Combo → Hit → Miss → Crit Miss). The HUD sorts by percent for display.
        /// </summary>
        public static D20ChanceDisplayRow[] GetExclusiveD20ChanceRows(ExclusiveD20OutcomeChances chances) =>
            new[]
            {
                new D20ChanceDisplayRow("Crit", chances.CritPercent),
                new D20ChanceDisplayRow("Combo", chances.ComboPercent),
                new D20ChanceDisplayRow("Hit", chances.HitPercent),
                new D20ChanceDisplayRow("Miss", chances.MissPercent),
                new D20ChanceDisplayRow("Crit Miss", chances.CritMissPercent)
            };

        /// <summary>
        /// CHANCES panel display order: highest percent first; ties break by label for stable layout and hover rows.
        /// </summary>
        public static D20ChanceDisplayRow[] SortChanceRowsByPercentDescending(D20ChanceDisplayRow[] rows)
        {
            var copy = (D20ChanceDisplayRow[])rows.Clone();
            Array.Sort(copy, (a, b) =>
            {
                int c = b.Percent.CompareTo(a.Percent);
                return c != 0 ? c : string.CompareOrdinal(a.Label, b.Label);
            });
            return copy;
        }

        /// <summary>Maps default-scenario percents by row label for delta coloring when display order is sorted.</summary>
        public static Dictionary<string, int> BuildChancePercentByLabel(D20ChanceDisplayRow[] canonicalDefaultRows)
        {
            var d = new Dictionary<string, int>(canonicalDefaultRows.Length);
            for (int i = 0; i < canonicalDefaultRows.Length; i++)
                d[canonicalDefaultRows[i].Label] = canonicalDefaultRows[i].Percent;
            return d;
        }

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
