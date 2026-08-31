using System;
using Avalonia.Media;
using RPGGame;
using RPGGame.UI.Avalonia;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Draws horizontal Previous | Current | Next resolve cards in the center arena (fighter only).
    /// Cards target a 5:3 width:height aspect and stay centered in the resolve band.
    /// </summary>
    public static class FighterResolveActionStackRenderer
    {
        public const int AspectWidth = 5;
        public const int AspectHeight = 3;

        public static void Render(GameCanvasControl canvas)
        {
            if (canvas == null)
                return;

            CombatArenaHudLayout.GetResolveStackBand(out int bx, out int by, out int bw, out int bh);
            canvas.ClearTextInArea(bx, by, bw, bh);
            canvas.ClearBoxesInArea(bx, by, bw, bh);

            var previous = FighterResolveActionStackState.Previous;
            var current = FighterResolveActionStackState.Current;
            var next = FighterResolveActionStackState.Next;
            if (!previous.HasValue && !current.HasValue && !next.HasValue)
                return;

            int gap = 2;
            // Prefer using the mid-band height; derive 5:3 widths from that height.
            int currentH = Math.Max(5, Math.Min(bh, Math.Max(7, bh - 1)));
            int currentW = Math.Max(10, (int)Math.Round(currentH * (double)AspectWidth / AspectHeight));
            int sideH = Math.Max(4, (int)Math.Round(currentH * 0.82));
            int sideW = Math.Max(8, (int)Math.Round(sideH * (double)AspectWidth / AspectHeight));

            int totalW = sideW + gap + currentW + gap + sideW;
            if (totalW > bw)
            {
                double scale = (double)bw / totalW;
                currentW = Math.Max(8, (int)Math.Floor(currentW * scale));
                sideW = Math.Max(6, (int)Math.Floor(sideW * scale));
                currentH = Math.Max(4, (int)Math.Round(currentW * (double)AspectHeight / AspectWidth));
                sideH = Math.Max(3, (int)Math.Round(sideW * (double)AspectHeight / AspectWidth));
                currentH = Math.Min(currentH, bh);
                sideH = Math.Min(sideH, bh);
                totalW = sideW + gap + currentW + gap + sideW;
            }

            int startX = bx + Math.Max(0, (bw - totalW) / 2);
            int currentY = by + Math.Max(0, (bh - currentH) / 2);
            int sideY = by + Math.Max(0, (bh - sideH) / 2);

            int prevX = startX;
            int curX = startX + sideW + gap;
            int nextX = curX + currentW + gap;

            if (previous.HasValue)
                DrawCard(canvas, previous.Value, prevX, sideY, sideW, sideH, AsciiArtAssets.Colors.DarkGray, 0.5);
            if (current.HasValue)
                DrawCard(canvas, current.Value, curX, currentY, currentW, currentH, AsciiArtAssets.Colors.White, 1.0);
            if (next.HasValue)
                DrawCard(canvas, next.Value, nextX, sideY, sideW, sideH, AsciiArtAssets.Colors.NeutralGray70, 0.7);
        }

        private static void DrawCard(
            GameCanvasControl canvas,
            ActionPanelInfo info,
            int x,
            int y,
            int w,
            int h,
            Color border,
            double fade)
        {
            if (w < 8 || h < 3)
                return;

            canvas.AddBorder(x, y, w, h, fade < 0.99 ? Darken(border, fade) : border);
            int tx = x + 1;
            int ty = y + 1;
            int textW = w - 2;

            string name = info.Name ?? "";
            if (name.Length > textW)
                name = name.Substring(0, textW - 3) + "...";
            canvas.AddText(tx, ty, name.ToUpperInvariant(), Darken(AsciiArtAssets.Colors.Red, fade));

            if (ty + 1 < y + h - 1)
            {
                string dmgSpd = $"Dmg {info.DamageModified:0.#}% | Spd {info.SpeedModified:0.#}%";
                if (dmgSpd.Length > textW)
                    dmgSpd = dmgSpd.Substring(0, textW);
                canvas.AddText(tx, ty + 1, dmgSpd, Darken(AsciiArtAssets.Colors.White, fade));
            }

            if (!string.IsNullOrWhiteSpace(info.ThresholdText) && ty + 3 < y + h - 1)
            {
                string thr = info.ThresholdText!;
                if (thr.Length > textW)
                    thr = thr.Substring(0, textW);
                canvas.AddText(tx, ty + 3, thr, Darken(AsciiArtAssets.Colors.Cyan, fade));
            }
        }

        private static Color Darken(Color c, double factor)
        {
            byte Scale(byte ch) => (byte)Math.Clamp((int)Math.Round(ch * factor), 0, 255);
            return Color.FromArgb(c.A, Scale(c.R), Scale(c.G), Scale(c.B));
        }
    }
}
