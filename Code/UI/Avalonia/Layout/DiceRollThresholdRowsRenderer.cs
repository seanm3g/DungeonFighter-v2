using System;
using RPGGame;
using RPGGame.UI.Avalonia;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Renders dice roll threshold rows (Crit, Combo, Hit, Crit Miss; in chance mode percents sorted high→low) for any <see cref="Actor"/>.
    /// Used by the left panel (player hero) and the right panel (active enemy).
    /// </summary>
    public static class DiceRollThresholdRowsRenderer
    {
        private const int ThresholdLabelWidth = 11;

        /// <summary>
        /// Draws threshold ladder rows (enemy / default path).
        /// </summary>
        /// <returns>Y coordinate immediately below the last row.</returns>
        public static int RenderRows(GameCanvasControl canvas, int x, int y, Actor actor) =>
            RenderRows(canvas, x, y, actor, false, false, out _);

        /// <summary>
        /// Draws four threshold rows or five CHANCES rows starting at <paramref name="y"/>.
        /// </summary>
        /// <param name="chanceHoverOrder">When <paramref name="showChances"/> is true, rows in on-screen order (for hover ids).</param>
        /// <returns>Y coordinate immediately below the last row.</returns>
        public static int RenderRows(
            GameCanvasControl canvas,
            int x,
            int y,
            Actor actor,
            bool showChances,
            bool chancesFlashHighlight,
            out ThresholdDisplayFormatting.D20ChanceDisplayRow[]? chanceHoverOrder)
        {
            chanceHoverOrder = null;
            var snapshot = DiceRollThresholdResolver.Resolve(actor);

            void RenderThresholdRow(int rowY, string labelName, int current, int def, int effective, int defaultDisplayedBaseline)
            {
                string labelPart = $"{labelName}:".PadRight(ThresholdLabelWidth);
                var valueColor = ThresholdDisplayFormatting.GetValueColor(current, def);
                int combinedDelta = effective - defaultDisplayedBaseline;
                string modStr = ThresholdDisplayFormatting.FormatSignedDeltaSuffix(combinedDelta);

                canvas.AddText(x, rowY, labelPart, AsciiArtAssets.Colors.Cyan);
                int colX = x + ThresholdLabelWidth;
                string mainPart = effective.ToString();
                canvas.AddText(colX, rowY, mainPart, valueColor);
                colX += mainPart.Length;
                if (modStr.Length > 0)
                {
                    var modColor = ThresholdDisplayFormatting.GetAccuracyDeltaParenColor(combinedDelta);
                    canvas.AddText(colX, rowY, modStr, modColor);
                }
            }

            void RenderChanceRow(int rowY, string labelName, int percent, int defaultPercent)
            {
                string labelPart = $"{labelName}:".PadRight(ThresholdLabelWidth);
                if (chancesFlashHighlight)
                {
                    canvas.AddText(x, rowY, labelPart, AsciiArtAssets.Colors.Gold);
                    canvas.AddText(
                        x + ThresholdLabelWidth,
                        rowY,
                        ThresholdDisplayFormatting.FormatD20ChancePercent(percent),
                        AsciiArtAssets.Colors.Gold);
                    return;
                }

                canvas.AddText(x, rowY, labelPart, AsciiArtAssets.Colors.Cyan);
                var valueColor = ThresholdDisplayFormatting.GetChanceDeltaColor(percent, defaultPercent);
                canvas.AddText(x + ThresholdLabelWidth, rowY, ThresholdDisplayFormatting.FormatD20ChancePercent(percent), valueColor);
            }

            int cy = y;

            if (showChances)
            {
                var chances = ThresholdDisplayFormatting.CalculateExclusiveD20OutcomeChances(
                    snapshot.EffectiveCrit,
                    snapshot.EffectiveCombo,
                    snapshot.EffectiveHit,
                    snapshot.EffectiveCritMiss);
                var defaultChances = ThresholdDisplayFormatting.CalculateExclusiveD20OutcomeChances(
                    snapshot.DefaultCrit,
                    snapshot.DefaultCombo,
                    snapshot.DefaultMinRollToHit,
                    DiceRollThresholdSnapshot.DefaultCritMiss);
                var canonicalRows = ThresholdDisplayFormatting.GetExclusiveD20ChanceRows(chances);
                var defaultCanonical = ThresholdDisplayFormatting.GetExclusiveD20ChanceRows(defaultChances);
                var defaultByLabel = ThresholdDisplayFormatting.BuildChancePercentByLabel(defaultCanonical);
                var chanceRows = ThresholdDisplayFormatting.SortChanceRowsByPercentDescending(canonicalRows);
                chanceHoverOrder = chanceRows;
                for (int i = 0; i < chanceRows.Length; i++)
                {
                    defaultByLabel.TryGetValue(chanceRows[i].Label, out int defaultPct);
                    RenderChanceRow(cy++, chanceRows[i].Label, chanceRows[i].Percent, defaultPct);
                }
            }
            else
            {
                RenderThresholdRow(cy++, "Crit", snapshot.Crit, snapshot.DefaultCrit, snapshot.EffectiveCrit, snapshot.DefaultCrit);
                RenderThresholdRow(cy++, "Combo", snapshot.Combo, snapshot.DefaultCombo, snapshot.EffectiveCombo, snapshot.DefaultCombo);
                RenderThresholdRow(cy++, "Hit", snapshot.Hit, snapshot.DefaultHit, snapshot.EffectiveHit, snapshot.DefaultMinRollToHit);
                RenderThresholdRow(cy++, "Crit Miss", snapshot.CritMiss, DiceRollThresholdSnapshot.DefaultCritMiss, snapshot.EffectiveCritMiss, DiceRollThresholdSnapshot.DefaultCritMiss);
            }
            return cy;
        }
    }
}
