using System;
using Avalonia.Media;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Layout;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Combat.Sequence
{
    /// <summary>
    /// Draws the two-row combat sequence HUD in its own framed panel (below the action strip, above the combat log).
    /// Column headers stay visible during Dungeon / Combat / Action Lab even with no swing in progress.
    /// </summary>
    public static class CombatSequenceHudRenderer
    {
        public static void Render(GameCanvasControl canvas)
        {
            if (canvas == null || !CombatSequenceHudState.IsBandReserved || LayoutConstants.CombatSequencePanelHeight == 0)
                return;

            int x = LayoutConstants.CENTER_PANEL_X + 1;
            int y = LayoutConstants.CombatSequenceHudY;
            int w = Math.Max(1, LayoutConstants.CENTER_PANEL_WIDTH - 2);
            int h = LayoutConstants.COMBAT_SEQUENCE_HUD_HEIGHT;
            canvas.ClearTextInArea(x, y, w, h);

            int colCount = CombatSequenceHudLayout.ColumnTitles.Length;
            var columns = CombatSequenceHudLayout.MeasureColumns(x, w, colCount);
            var writer = new ColoredTextWriter(canvas);
            Color separatorColor = ColorPalette.DarkGray.GetColor();
            var steps = CombatSequenceHudState.Steps;
            bool revealed = CombatSequenceHudState.ResultRevealed;
            // Empty steps = dungeon chrome idle. Selective F7 may have CurrentIndex < 0 until first reveal.
            bool idle = steps.Count == 0;

            for (int i = 0; i < columns.Length; i++)
            {
                var col = columns[i];
                if (col.Width <= 0)
                    continue;

                string header = CombatSequenceHudLayout.ColumnTitles[i];
                int stepIndex = CombatSequenceHudLayout.FindDisplayStepIndex(steps, i, CombatSequenceHudState.GetStepPhase);
                CombatSequenceHudLayout.Phase phase;
                if (idle)
                    phase = CombatSequenceHudLayout.Phase.Pending;
                else if (stepIndex >= 0)
                    phase = CombatSequenceHudState.GetStepPhase(stepIndex);
                else
                    phase = CombatSequenceHudLayout.Phase.Pending;

                string title = CombatSequenceHudLayout.FormatTitle(header, idle ? CombatSequenceHudLayout.Phase.Pending : phase, col.Width);
                Color titleColor = idle
                    ? ColorPalette.Cyan.GetColor()
                    : CombatSequenceHudLayout.TitlePalette(phase).GetColor();
                canvas.AddText(col.X, y, title, titleColor);

                if (!idle && stepIndex >= 0)
                {
                    var step = steps[stepIndex];
                    var cell = phase == CombatSequenceHudLayout.Phase.Active && revealed
                        ? CombatSequenceHudState.VisibleResult
                        : step.Result;
                    var result = CombatSequenceHudLayout.FormatResult(cell, phase, revealed, col.Width);
                    if (result.Count > 0)
                        writer.RenderSegments(result, col.X, y + 1);
                }

                if (i < columns.Length - 1)
                {
                    int sepX = col.X + col.Width;
                    if (sepX < x + w)
                    {
                        canvas.AddText(sepX, y, AsciiArtAssets.UIElements.BorderVertical, separatorColor);
                        canvas.AddText(sepX, y + 1, AsciiArtAssets.UIElements.BorderVertical, separatorColor);
                    }
                }
            }
        }

        /// <summary>Rows <see cref="RenderVertical"/> needs for <paramref name="rowsPerEntry"/> (3 = title, result, gap; 2 = title, result; 1 = both on one row).</summary>
        public static int VerticalContentHeight(int rowsPerEntry)
        {
            int n = CombatSequenceHudLayout.ColumnTitles.Length;
            return rowsPerEntry >= 3 ? n * 3 - 1 : n * Math.Max(1, rowsPerEntry);
        }

        /// <summary>Most rows per entry that fit <paramref name="innerHeight"/> rows (1 when even that is short).</summary>
        public static int VerticalRowsPerEntry(int innerHeight) =>
            innerHeight >= VerticalContentHeight(3) ? 3 : innerHeight >= VerticalContentHeight(2) ? 2 : 1;

        /// <summary>
        /// Board combat (<see cref="CombatBoardLayout"/>) has no HUD band, so the same columns stack down a narrow panel
        /// on the left of the board: each title over its result, or both on one row when the panel is short.
        /// The caller draws the frame; this fills the inner rect.
        /// </summary>
        public static void RenderVertical(GameCanvasControl canvas, int x, int y, int w, int h)
        {
            if (canvas == null || w <= 0 || h <= 0)
                return;
            canvas.ClearTextInArea(x, y, w, h);

            int rowsPer = VerticalRowsPerEntry(h);
            var writer = new ColoredTextWriter(canvas);
            var steps = CombatSequenceHudState.Steps;
            bool revealed = CombatSequenceHudState.ResultRevealed;
            bool idle = steps.Count == 0;
            var titles = CombatSequenceHudLayout.ColumnTitles;

            for (int i = 0; i < titles.Length; i++)
            {
                int rowY = y + i * Math.Min(rowsPer, 3);
                if (rowY >= y + h)
                    break;

                int stepIndex = idle ? -1 : CombatSequenceHudLayout.FindDisplayStepIndex(steps, i, CombatSequenceHudState.GetStepPhase);
                var phase = stepIndex >= 0 ? CombatSequenceHudState.GetStepPhase(stepIndex) : CombatSequenceHudLayout.Phase.Pending;
                string title = CombatSequenceHudLayout.FormatTitle(titles[i], phase, w);
                Color titleColor = idle ? ColorPalette.Cyan.GetColor() : CombatSequenceHudLayout.TitlePalette(phase).GetColor();
                canvas.AddText(x, rowY, title, titleColor);

                if (stepIndex < 0)
                    continue;
                var step = steps[stepIndex];
                var cell = phase == CombatSequenceHudLayout.Phase.Active && revealed
                    ? CombatSequenceHudState.VisibleResult
                    : step.Result;
                int resultX = rowsPer == 1 ? x + title.Length + 1 : x + 1;
                int resultY = rowsPer == 1 ? rowY : rowY + 1;
                var result = CombatSequenceHudLayout.FormatResult(cell, phase, revealed, x + w - resultX);
                if (result.Count > 0 && resultY < y + h)
                    writer.RenderSegments(result, resultX, resultY);
            }
        }
    }
}
