using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Actions.RollModification;
using RPGGame.Combat;
using RPGGame.UI.Avalonia;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Renders dice roll threshold rows (Crit, Combo, Hit, Crit Miss; in chance mode percents sorted high→low) for any <see cref="Actor"/>.
    /// Used by the left panel (player hero) and the right panel (active enemy).
    /// </summary>
    public static class DiceRollThresholdRowsRenderer
    {
        private const int DefaultCritMiss = 1;
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
            var tm = RollModificationManager.GetThresholdManager();
            var config = GameConfiguration.Instance;
            int hit = tm.GetHitThreshold(actor);
            int combo = tm.GetComboThreshold(actor);
            int crit = tm.GetCriticalHitThreshold(actor);
            int critMiss = tm.GetCriticalMissThreshold(actor);
            int defaultHit = config.RollSystem.MissThreshold.Max > 0 ? config.RollSystem.MissThreshold.Max : 5;
            int defaultCombo = config.RollSystem.ComboThreshold.Min > 0 ? config.RollSystem.ComboThreshold.Min : 14;
            int defaultCrit = config.Combat.CriticalHitThreshold > 0 ? config.Combat.CriticalHitThreshold : 20;
            // Queued FIFO/slot shifts (ACCURACY shared; COMBO/HIT/CRIT per row), INT milestone steps, and literal
            // gear HIT/COMBO/CRIT (catalog + prefixes like Swift + suffixes). Sheet/temp roll totals stay in
            // <see cref="CombatCalculator.CalculateRollBonus"/> — do not merge deferred ACCURACY into roll bonus here
            // (that produced wildly negative HUD numbers, e.g. CAST +20 with a two-card strip).
            var pendingHud = actor is Character chHud ? ActionSelector.PeekPendingThresholdHudShifts(chHud) : default;
            var intSteps = actor is Character chInt
                ? IntelligenceMilestoneThresholdBonuses.GetSteps(chInt.GetEffectiveIntelligence())
                : default;
            // Literal HIT/COMBO/CRIT from gear (catalog, rolled prefixes like Swift, suffixes) — same “easier ladder” sense as INT steps.
            int eqHit = 0, eqCombo = 0, eqCrit = 0;
            if (actor is Character chEq && chEq is not Enemy)
            {
                eqHit = chEq.Equipment.GetEquipmentStatBonus("HIT", chEq);
                eqCombo = chEq.Equipment.GetEquipmentStatBonus("COMBO", chEq);
                eqCrit = chEq.Equipment.GetEquipmentStatBonus("CRIT", chEq);
            }

            int comboRowShift = pendingHud.SharedAccuracy + pendingHud.ComboDelta;
            int hitRowShift = pendingHud.SharedAccuracy + pendingHud.HitDelta;
            int critRowShift = pendingHud.CritDelta;
            // CRIT_MISS bonuses raise the stored threshold; GetThresholdValueWithAccuracyParts subtracts shift → negate.
            int critMissRowShift = -pendingHud.CritMissDelta;

            comboRowShift += intSteps.ComboSteps + eqCombo;
            hitRowShift += intSteps.HitSteps + eqHit;
            critRowShift += intSteps.CritSteps + eqCrit;

            // One parenthetical: final displayed value minus configured default in the same units as the main number
            // (queued shifts + sheet/stored deltas combined). Negative = easier on the d20 ladder for crit/combo/hit.
            int ResolveEffectiveThreshold(int baseThreshold, int rowAccuracy)
            {
                ThresholdDisplayFormatting.GetThresholdValueWithAccuracyParts(baseThreshold, rowAccuracy, out int effective, out _, out _);
                return ThresholdDisplayFormatting.ClampDiceLadderDisplayValue(effective);
            }

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

            int effectiveCrit = ResolveEffectiveThreshold(crit, critRowShift);
            int effectiveCombo = ResolveEffectiveThreshold(combo, comboRowShift);
            int effectiveHit = ResolveEffectiveThreshold(hit + 1, hitRowShift);
            int effectiveCritMiss = ResolveEffectiveThreshold(critMiss, critMissRowShift);

            int cy = y;
            int defaultMinRollToHit = defaultHit + 1;

            if (showChances)
            {
                var chances = ThresholdDisplayFormatting.CalculateExclusiveD20OutcomeChances(
                    effectiveCrit,
                    effectiveCombo,
                    effectiveHit,
                    effectiveCritMiss);
                var defaultChances = ThresholdDisplayFormatting.CalculateExclusiveD20OutcomeChances(
                    defaultCrit,
                    defaultCombo,
                    defaultMinRollToHit,
                    DefaultCritMiss);
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
                // Queued HIT/COMBO/CRIT deltas shift their respective rows; ACCURACY shifts both hit and combo (matches combat resolution).
                RenderThresholdRow(cy++, "Crit", crit, defaultCrit, effectiveCrit, defaultCrit);
                RenderThresholdRow(cy++, "Combo", combo, defaultCombo, effectiveCombo, defaultCombo);
                // Main number is minimum d20 to hit; baseline is default max-miss + 1 so delta matches that scale.
                RenderThresholdRow(cy++, "Hit", hit, defaultHit, effectiveHit, defaultMinRollToHit);
                RenderThresholdRow(cy++, "Crit Miss", critMiss, DefaultCritMiss, effectiveCritMiss, DefaultCritMiss);
            }
            return cy;
        }
    }
}
