using System;
using Avalonia.Media;
using RPGGame;
using RPGGame.UI.Avalonia;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Draws the resolving action card in the center of the arena band.
    /// Fighter past-actions sit to the left; enemy past-actions sit to the right with a red border.
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

            var fighterPast = FighterResolveActionStackState.Previous;
            var enemyPast = FighterResolveActionStackState.EnemyPrevious;
            var current = FighterResolveActionStackState.Current;
            if (!fighterPast.HasValue && !enemyPast.HasValue && !current.HasValue)
                return;

            CombatArenaHudLayout.GetResolveCurrentCardRect(out int curX, out int curY, out int curW, out int curH);
            CombatArenaHudLayout.GetResolvePreviousCardRect(out int prevX, out int prevY, out int prevW, out int prevH);
            CombatArenaHudLayout.GetResolveEnemyPreviousCardRect(out int enemyX, out int enemyY, out int enemyW, out int enemyH);

            if (fighterPast.HasValue)
                DrawCard(canvas, fighterPast.Value, prevX, prevY, prevW, prevH, AsciiArtAssets.Colors.DarkGray, 0.5);
            if (enemyPast.HasValue)
                DrawCard(canvas, enemyPast.Value, enemyX, enemyY, enemyW, enemyH, AsciiArtAssets.Colors.Red, 0.55);
            if (current.HasValue)
            {
                var border = FighterResolveActionStackState.CurrentIsEnemy
                    ? AsciiArtAssets.Colors.Red
                    : AsciiArtAssets.Colors.White;
                DrawCard(canvas, current.Value, curX, curY, curW, curH, border, 1.0);
            }
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

            canvas.AddBorder(
                x, y, w, h,
                fade < 0.99 ? Darken(border, fade) : border,
                LayoutConstants.ACTION_SQUARE_BORDER_THICKNESS_PIXELS);
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
