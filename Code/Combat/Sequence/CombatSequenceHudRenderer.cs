using System;
using Avalonia.Media;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Layout;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Combat.Sequence
{
    /// <summary>
    /// Draws the two-row combat sequence HUD in its own framed panel (top of the center column, above the combat log).
    /// Column headers stay visible in a dungeon even with no swing in progress.
    /// </summary>
    public static class CombatSequenceHudRenderer
    {
        public static void Render(GameCanvasControl canvas)
        {
            if (canvas == null || !CombatSequenceHudState.IsBandReserved)
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
            int current = CombatSequenceHudState.CurrentIndex;
            bool revealed = CombatSequenceHudState.ResultRevealed;
            bool idle = steps.Count == 0 || current < 0;

            for (int i = 0; i < columns.Length; i++)
            {
                var col = columns[i];
                if (col.Width <= 0)
                    continue;

                string header = CombatSequenceHudLayout.ColumnTitles[i];
                int stepIndex = CombatSequenceHudLayout.FindStepIndex(steps, i);
                CombatSequenceHudLayout.Phase phase;
                if (idle)
                    phase = CombatSequenceHudLayout.Phase.Pending;
                else if (stepIndex >= 0)
                    phase = CombatSequenceHudLayout.GetPhase(stepIndex, current);
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
    }
}
