using System;
using System.Collections.Generic;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Layout
{
    public enum CombatLocalLogLane
    {
        Fighter,
        Enemy
    }

    /// <summary>
    /// Renders a short near-entity combat log by filtering the shared display buffer into fighter vs enemy lanes.
    /// </summary>
    public static class CombatLocalLogView
    {
        public static void RenderLane(
            GameCanvasControl canvas,
            ColoredTextWriter textWriter,
            DisplayBuffer? buffer,
            CombatLocalLogLane lane,
            int x,
            int y,
            int width,
            int height,
            IReadOnlyList<string>? combatEnemyNames,
            string? heroName)
        {
            if (canvas == null || textWriter == null || width < 1 || height < 1)
                return;

            canvas.ClearTextInArea(x, y, width, height);
            if (buffer == null || buffer.Count == 0)
                return;

            try
            {
                RenderLaneCore(canvas, textWriter, buffer, lane, x, y, width, height, combatEnemyNames, heroName);
            }
            catch (Exception)
            {
                // Combat still appends to the same buffer on another thread; never let log paint kill the process.
            }
        }

        private static void RenderLaneCore(
            GameCanvasControl canvas,
            ColoredTextWriter textWriter,
            DisplayBuffer buffer,
            CombatLocalLogLane lane,
            int x,
            int y,
            int width,
            int height,
            IReadOnlyList<string>? combatEnemyNames,
            string? heroName)
        {
            var lines = CombatCenterPanelEnemyLineAlignment.CopyLinesStable(buffer.Messages);
            if (lines.Count == 0)
                return;

            bool[] enemyFlags = CombatCenterPanelEnemyLineAlignment.ResolveRightAlignFlags(
                lines,
                combatEnemyNames,
                heroName);

            var selected = new List<List<ColoredText>>();
            for (int i = 0; i < lines.Count; i++)
            {
                var segs = lines[i];
                if (segs == null || segs.Count == 0)
                    continue;
                string plain = ColoredTextRenderer.RenderAsPlainText(segs);
                if (string.IsNullOrWhiteSpace(plain))
                    continue;

                bool isEnemy = i < enemyFlags.Length && enemyFlags[i];
                if (lane == CombatLocalLogLane.Enemy)
                {
                    if (isEnemy)
                        selected.Add(segs);
                }
                else if (!isEnemy)
                {
                    selected.Add(segs);
                }
            }

            int take = Math.Min(selected.Count, height);
            int start = selected.Count - take;
            int row = y;
            for (int i = start; i < selected.Count && row < y + height; i++)
            {
                var segs = selected[i];
                string plain = ColoredTextRenderer.RenderAsPlainText(segs);
                if (plain.Length > width)
                {
                    var truncated = new List<ColoredText>();
                    int used = 0;
                    foreach (var s in segs)
                    {
                        if (s?.Text == null)
                            continue;
                        int remain = width - used;
                        if (remain <= 0)
                            break;
                        string t = s.Text.Length <= remain ? s.Text : s.Text.Substring(0, remain);
                        truncated.Add(new ColoredText(t, s.Color));
                        used += t.Length;
                    }
                    textWriter.RenderSegments(truncated, x, row);
                }
                else
                {
                    textWriter.RenderSegments(segs, x, row);
                }
                row++;
            }
        }
    }
}
