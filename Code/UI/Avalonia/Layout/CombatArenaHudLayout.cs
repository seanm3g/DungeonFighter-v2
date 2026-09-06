using System;
using RPGGame.UI.Avalonia;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Character-grid regions for the combat center arena: enemy HUD (top of the log frame), resolve row (middle), fighter HUD (bottom of the log frame, above the combo strip).
    /// </summary>
    public static class CombatArenaHudLayout
    {
        public static bool IllustratedSceneVisible { get; set; }

        public static void GetSceneRect(out int x, out int y, out int width, out int height)
        {
            var (ax, ay, aw, _) = GetArenaInnerRect();
            GetFighterHud(out _, out int fighterY, out _, out _);
            x = ax;
            y = ay + EnemyHudHeight + 1;
            int available = fighterY - y - 1;
            bool split = CombatVisuals.CombatSceneGeometry.CanSplit(aw, available, IllustratedSceneVisible);
            width = split ? aw * 2 / 3 : 0;
            height = split ? available : 0;
        }
        /// <summary>
        /// Side-panel HP/armor/d20 bars duplicate the center arena HUD. Hide them only while that HUD is on screen
        /// (active fight). Encounter intro / result / room-cleared screens keep the side bars.
        /// </summary>
        public static bool HideSidePanelHealthBars(bool arenaHudVisible) => arenaHudVisible;

        public const int EnemyHudHeight = 5;
        public const int FighterBarsHeight = 4;
        public const int FighterLogHeight = 4;
        public const int EnemyLogHeight = 3;
        public const double BarsWidthFraction = 0.55;

        public static (int x, int y, int width, int height) GetArenaInnerRect()
        {
            int x = LayoutConstants.CENTER_PANEL_X + 1;
            int y = LayoutConstants.CENTER_PANEL_Y + 1;
            int w = Math.Max(1, LayoutConstants.CENTER_PANEL_WIDTH - 2);
            int h = Math.Max(1, LayoutConstants.CENTER_PANEL_HEIGHT - 2);
            return (x, y, w, h);
        }

        public static void GetEnemyHud(out int x, out int y, out int width, out int height)
        {
            var (ax, ay, aw, _) = GetArenaInnerRect();
            x = ax;
            y = ay;
            width = aw;
            height = EnemyHudHeight;
        }

        public static void GetEnemyBars(out int x, out int y, out int width)
        {
            GetEnemyHud(out int hx, out int hy, out int hw, out _);
            width = Math.Max(8, (int)(hw * BarsWidthFraction));
            x = hx + Math.Max(0, hw - width);
            y = hy + 1;
        }

        public static void GetEnemyLog(out int x, out int y, out int width, out int height)
        {
            GetEnemyHud(out int hx, out int hy, out _, out int hh);
            GetEnemyBars(out int barsX, out _, out _);
            x = hx;
            y = hy;
            width = Math.Max(8, barsX - hx - 1);
            height = Math.Min(EnemyLogHeight, hh);
        }

        public static int GetEnemyNameX(int nameWidth)
        {
            GetEnemyBars(out int barsX, out _, out int barsW);
            return Math.Max(barsX, barsX + barsW - Math.Max(1, nameWidth));
        }

        public static void GetFighterHud(out int x, out int y, out int width, out int height)
        {
            var (ax, ay, aw, ah) = GetArenaInnerRect();
            height = FighterBarsHeight + FighterLogHeight;
            x = ax;
            y = ay + ah - height;
            width = aw;
        }

        public static void GetFighterBars(out int x, out int y, out int width)
        {
            GetFighterHud(out int hx, out int hy, out int hw, out _);
            x = hx;
            y = hy;
            width = Math.Max(8, (int)(hw * BarsWidthFraction));
        }

        public static void GetFighterLog(out int x, out int y, out int width, out int height)
        {
            GetFighterHud(out int hx, out int hy, out int hw, out int hh);
            x = hx;
            y = hy + FighterBarsHeight;
            width = hw;
            height = Math.Min(FighterLogHeight, Math.Max(1, hh - FighterBarsHeight));
        }

        public static void GetResolveStackBand(out int x, out int y, out int width, out int height)
        {
            var (ax, ay, aw, _) = GetArenaInnerRect();
            GetEnemyHud(out _, out _, out _, out int enemyH);
            GetFighterHud(out _, out int fighterY, out _, out _);
            x = ax;
            y = ay + enemyH + 1;
            width = aw;
            height = Math.Max(3, fighterY - y - 1);
            if (CombatVisuals.CombatSceneGeometry.CanSplit(width, height, IllustratedSceneVisible))
            {
                int sceneWidth = width * 2 / 3;
                x += sceneWidth + 1;
                width -= sceneWidth + 1;
            }
        }

        /// <summary>Resolving action card, centered in the mid-band.</summary>
        public static void GetResolveCurrentCardRect(out int x, out int y, out int width, out int height)
        {
            GetResolveStackBand(out int bx, out int by, out int bw, out int bh);
            if (TryGetVisualCardRect(0, out x, out y, out width, out height)) return;
            height = Math.Max(5, Math.Min(bh, Math.Max(7, bh - 1)));
            width = Math.Max(10, (int)Math.Round(height * 5.0 / 3.0));
            if (width > bw)
            {
                width = Math.Max(8, bw);
                height = Math.Max(4, (int)Math.Round(width * 3.0 / 5.0));
                height = Math.Min(height, bh);
            }
            x = bx + Math.Max(0, (bw - width) / 2);
            y = by + Math.Max(0, (bh - height) / 2);
        }

        /// <summary>Fighter past-action card, left of the centered current card.</summary>
        public static void GetResolvePreviousCardRect(out int x, out int y, out int width, out int height)
        {
            if (TryGetVisualCardRect(1, out x, out y, out width, out height)) return;
            GetResolveStackBand(out int bx, out int by, out _, out int bh);
            GetResolveCurrentCardRect(out int cx, out _, out _, out int ch);
            height = Math.Max(4, (int)Math.Round(ch * 0.82));
            width = Math.Max(8, (int)Math.Round(height * 5.0 / 3.0));
            const int gap = 2;
            x = Math.Max(bx, cx - gap - width);
            y = by + Math.Max(0, (bh - height) / 2);
        }

        /// <summary>Enemy past-action card, right of the centered current card.</summary>
        public static void GetResolveEnemyPreviousCardRect(out int x, out int y, out int width, out int height)
        {
            if (TryGetVisualCardRect(2, out x, out y, out width, out height)) return;
            GetResolveStackBand(out int bx, out int by, out int bw, out int bh);
            GetResolveCurrentCardRect(out int cx, out _, out int cw, out int ch);
            height = Math.Max(4, (int)Math.Round(ch * 0.82));
            width = Math.Max(8, (int)Math.Round(height * 5.0 / 3.0));
            const int gap = 2;
            int rightEdge = bx + bw;
            x = Math.Min(rightEdge - width, cx + cw + gap);
            y = by + Math.Max(0, (bh - height) / 2);
        }

        private static bool TryGetVisualCardRect(int index, out int x, out int y, out int width, out int height)
        {
            GetSceneRect(out _, out _, out int sceneWidth, out _);
            x = y = width = height = 0;
            if (sceneWidth == 0) return false;
            GetResolveStackBand(out int bx, out int by, out int bw, out int bh);
            height = (bh - 2) / 3;
            width = bw;
            x = bx;
            y = by + index * (height + 1);
            return true;
        }

        public static void GetNarrativeBand(out int x, out int y, out int width, out int height) =>
            GetResolveStackBand(out x, out y, out width, out height);

        /// <summary>
        /// Erase arena chrome (HUD bars, resolve cards, and unused-action X strokes) before a new paint.
        /// </summary>
        public static void ClearArenaInner(GameCanvasControl canvas)
        {
            if (canvas == null)
                return;
            var (x, y, w, h) = GetArenaInnerRect();
            canvas.ClearTextInArea(x, y, w, h);
            canvas.ClearProgressBarsInArea(x, y, w, h);
            canvas.ClearSegmentedBarsInArea(x, y, w, h);
            canvas.ClearBoxesInArea(x, y, w, h);
            canvas.ClearLinesInArea(x, y, w, h);
        }
    }
}
