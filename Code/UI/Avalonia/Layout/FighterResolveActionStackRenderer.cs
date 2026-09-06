using System;
using Avalonia.Media;
using RPGGame;
using RPGGame.UI.Avalonia;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Draws independent fighter and enemy piles, newest card first, with readable descriptions.
    /// </summary>
    public static class FighterResolveActionStackRenderer
    {
        public const int AspectWidth = 5;
        public const int AspectHeight = 3;
        public static global::Avalonia.Rect ActiveCardBounds { get; private set; }

        public static void Render(GameCanvasControl canvas)
        {
            if (canvas == null)
                return;

            CombatArenaHudLayout.GetResolveStackBand(out int bx, out int by, out int bw, out int bh);
            ActiveCardBounds = default;
            canvas.ClearTextInArea(bx, by, bw, bh);
            canvas.ClearBoxesInArea(bx, by, bw, bh);
            canvas.ClearLinesInArea(bx, by, bw, bh);

            // Keep full-width piles in the narrow illustrated sidebar; use columns in a wide text arena.
            bool columns = bw >= 64;
            int width = columns ? (bw - 2) / 2 : bw;
            int height = columns ? bh : (bh - 1) / 2;
            DrawPile(canvas, false, bx, by, width, height);
            DrawPile(canvas, true, columns ? bx + width + 2 : bx,
                columns ? by : by + height + 1, width, height);
        }

        private static void DrawPile(GameCanvasControl canvas, bool enemy, int x, int y, int w, int h)
        {
            if (w < 8 || h < 6) return;
            var history = enemy ? FighterResolveActionStackState.EnemyHistory : FighterResolveActionStackState.FighterHistory;
            var current = FighterResolveActionStackState.Current;
            bool active = current.HasValue && FighterResolveActionStackState.CurrentIsEnemy == enemy;
            var top = active ? current : history.Count > 0 ? history[0].Info : (ActionPanelInfo?)null;
            var color = enemy ? CombatVisuals.BattleOverlay.Crimson : CombatVisuals.BattleOverlay.Sulfur;
            canvas.AddText(x, y, Fit((enemy ? "ENEMY" : "FIGHTER") + (active ? " / RESOLVING" : " / LAST PLAYED"), w), color);
            if (!top.HasValue)
            {
                canvas.AddText(x + 1, y + 2, "Awaiting action", AsciiArtAssets.Colors.Gray);
                return;
            }
            int offset = active ? 0 : 1;
            int tails = Math.Min(2, Math.Min(history.Count - offset, Math.Max(0, h - 9)));
            int cardHeight = h - 1 - tails;
            if (active) ActiveCardBounds = new global::Avalonia.Rect(x, y + 1, w, cardHeight);
            DrawCard(canvas, top.Value, x, y + 1, w, cardHeight, active ? color : Color.Parse("#58615D"), 1,
                active ? FighterResolveActionStackState.CurrentMissed : history[0].Unused,
                active ? FighterResolveActionStackState.CurrentResult : history[0].Result);
            for (int i = 0; i < tails; i++)
            {
                var past = history[i + offset];
                int edgeY = y + 1 + cardHeight + i;
                int inset = i + 1;
                var edgeColor = Darken(color, 0.5);
                canvas.AddLine(x + inset, edgeY, x + inset, edgeY + 1, edgeColor, 1);
                canvas.AddLine(x + w - inset, edgeY, x + w - inset, edgeY + 1, edgeColor, 1);
                canvas.AddLine(x + inset, edgeY + 1, x + w - inset, edgeY + 1, edgeColor, 1);
                string label = $"  {past.Info.Name} {(past.Unused ? "[unused]" : "[played]")}";
                canvas.AddText(x + inset, edgeY, Fit(label, w - inset * 2), AsciiArtAssets.Colors.Gray);
            }
        }

        internal static string Fit(string text, int width) => text.Length <= width ? text : text.Substring(0, Math.Max(0, width - 3)) + "...";
        private static void DrawCard(
            GameCanvasControl canvas,
            ActionPanelInfo info,
            int x,
            int y,
            int w,
            int h,
            Color border,
            double fade,
            bool missed, string result)
        {
            if (w < 8 || h < 3)
                return;

            canvas.AddBox(
                x, y, w, h,
                fade < 0.99 ? Darken(border, fade) : border,
                Color.Parse("#171D20"), borderThicknessPixels: LayoutConstants.ACTION_SQUARE_BORDER_THICKNESS_PIXELS);
            int tx = x + 1;
            int ty = y + 1;
            int textW = w - 2;

            string name = info.Name ?? "";
            if (name.Length > textW)
                name = name.Substring(0, textW - 3) + "...";
            canvas.AddText(tx, ty, name.ToUpperInvariant(), Darken(CombatArenaHudLayout.IllustratedSceneVisible ? CombatVisuals.BattleOverlay.Bone : AsciiArtAssets.Colors.Red, fade));

            if (ty + 1 < y + h - 1)
            {
                string dmgSpd = $"Dmg {info.DamageModified:0.#}% | Spd {info.SpeedModified:0.#}%";
                if (dmgSpd.Length > textW)
                    dmgSpd = dmgSpd.Substring(0, textW);
                canvas.AddText(tx, ty + 1, dmgSpd, Darken(AsciiArtAssets.Colors.White, fade));
            }

            int row = ty + 2;
            int bottom = y + h - 1;
            if (!string.IsNullOrWhiteSpace(result) && row < bottom)
                canvas.AddText(tx, row++, Fit(result, textW), CombatVisuals.BattleOverlay.Sulfur);
            else if (missed && row < bottom)
                canvas.AddText(tx, row++, "Combo action unused", CombatVisuals.BattleOverlay.Crimson);
            if (!string.IsNullOrWhiteSpace(info.ThresholdText) && row < bottom)
                canvas.AddText(tx, row++, Fit(info.ThresholdText, textW), Darken(AsciiArtAssets.Colors.Cyan, fade));
            string description = string.IsNullOrWhiteSpace(info.Description) ? "No description provided." : info.Description;
            if (bottom - row >= 4) row++;
            var lines = TextWrapper.WrapText(description.Replace("\r", " ").Replace("\n", " "), textW);
            for (int i = 0; i < lines.Count && row < bottom; i++, row++)
            {
                string line = lines[i];
                if (row == bottom - 1 && i + 1 < lines.Count)
                    line = Fit(line + "...", textW);
                canvas.AddText(tx, row, line, CombatVisuals.BattleOverlay.Bone);
            }
        }
        private static Color Darken(Color c, double factor)
        {
            byte Scale(byte ch) => (byte)Math.Clamp((int)Math.Round(ch * factor), 0, 255);
            return Color.FromArgb(c.A, Scale(c.R), Scale(c.G), Scale(c.B));
        }
    }
}


