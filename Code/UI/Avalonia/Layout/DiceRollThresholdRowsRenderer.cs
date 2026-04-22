using RPGGame;
using RPGGame.Actions.RollModification;
using RPGGame.Combat;
using RPGGame.UI.Avalonia;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Renders the four dice roll threshold rows (Crit, Combo, Hit, Crit Miss) for any <see cref="Actor"/>.
    /// Used by the left panel (player hero) and the right panel (active enemy).
    /// </summary>
    public static class DiceRollThresholdRowsRenderer
    {
        private const int DefaultCritMiss = 1;
        private const int ThresholdLabelWidth = 11;

        /// <summary>
        /// Draws four threshold rows starting at <paramref name="y"/>.
        /// </summary>
        /// <returns>Y coordinate immediately below the last row.</returns>
        public static int RenderRows(GameCanvasControl canvas, int x, int y, Actor actor)
        {
            var tm = RollModificationManager.GetThresholdManager();
            var config = GameConfiguration.Instance;
            int hit = tm.GetHitThreshold(actor);
            int combo = tm.GetComboThreshold(actor);
            int crit = tm.GetCriticalHitThreshold(actor);
            int critMiss = tm.GetCriticalMissThreshold(actor);
            int defaultHit = config.RollSystem.MissThreshold.Max > 0 ? config.RollSystem.MissThreshold.Max : 5;
            int defaultCombo = config.RollSystem.ComboThreshold.Min > 0 ? config.RollSystem.ComboThreshold.Min : 14;
            int defaultCrit = config.Combat.CriticalHitThreshold > 0 ? config.Combat.CriticalHitThreshold : 20;
            // Queued threshold shifts only (ACCURACY shared; COMBO/HIT/CRIT per row). Stat/equipment/temp roll bonus
            // must not be folded here: combat adds those to attack total (see HitCalculator / combo check), while FIFO
            // ACCURACY and sheet HIT/COMBO deltas shift thresholds (see ActionExecutionFlow). Mixing them caused
            // wildly negative HUD numbers (e.g. CAST +20 with a two-card strip).
            var pendingHud = actor is Character chHud ? ActionSelector.PeekPendingThresholdHudShifts(chHud) : default;
            int comboRowShift = pendingHud.SharedAccuracy + pendingHud.ComboDelta;
            int hitRowShift = pendingHud.SharedAccuracy + pendingHud.HitDelta;
            int critRowShift = pendingHud.CritDelta;
            // CRIT_MISS bonuses raise the stored threshold; GetThresholdValueWithAccuracyParts subtracts shift → negate.
            int critMissRowShift = -pendingHud.CritMissDelta;

            void RenderThresholdRow(int rowY, string labelName, int current, int def, int baseThreshold, int rowAccuracy)
            {
                string labelPart = $"{labelName}:".PadRight(ThresholdLabelWidth);
                var valueColor = ThresholdDisplayFormatting.GetValueColor(current, def);
                string modStr = ThresholdDisplayFormatting.FormatDeltaSuffix(current, def);
                ThresholdDisplayFormatting.GetThresholdValueWithAccuracyParts(baseThreshold, rowAccuracy, out int effective, out string accuracyParen, out int accuracyDelta);
                effective = ThresholdDisplayFormatting.ClampDiceLadderDisplayValue(effective);

                canvas.AddText(x, rowY, labelPart, AsciiArtAssets.Colors.Cyan);
                int colX = x + ThresholdLabelWidth;
                string mainPart = effective.ToString();
                canvas.AddText(colX, rowY, mainPart, valueColor);
                colX += mainPart.Length;
                if (accuracyParen.Length > 0)
                {
                    var parenColor = ThresholdDisplayFormatting.GetAccuracyDeltaParenColor(accuracyDelta);
                    canvas.AddText(colX, rowY, accuracyParen, parenColor);
                    colX += accuracyParen.Length;
                }

                if (modStr.Length > 0)
                    canvas.AddText(colX, rowY, modStr, valueColor);
            }

            int cy = y;
            // Queued HIT/COMBO/CRIT deltas shift their respective rows; ACCURACY shifts both hit and combo (matches combat resolution).
            RenderThresholdRow(cy, "Crit", crit, defaultCrit, crit, critRowShift);
            cy++;
            RenderThresholdRow(cy, "Combo", combo, defaultCombo, combo, comboRowShift);
            cy++;
            RenderThresholdRow(cy, "Hit", hit, defaultHit, hit + 1, hitRowShift);
            cy++;
            RenderThresholdRow(cy, "Crit Miss", critMiss, DefaultCritMiss, critMiss, critMissRowShift);
            cy++;
            return cy;
        }
    }
}
