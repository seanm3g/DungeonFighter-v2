using System;
using RPGGame.UI.Avalonia;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Character-grid regions for the combat center arena: enemy HUD (top), resolve row (middle), fighter HUD (bottom above combo strip).
    /// </summary>
    public static class CombatArenaHudLayout
    {
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
        }

        public static void GetNarrativeBand(out int x, out int y, out int width, out int height) =>
            GetResolveStackBand(out x, out y, out width, out height);

        public static void ClearArenaInner(GameCanvasControl canvas)
        {
            if (canvas == null)
                return;
            var (x, y, w, h) = GetArenaInnerRect();
            canvas.ClearTextInArea(x, y, w, h);
            canvas.ClearProgressBarsInArea(x, y, w, h);
            canvas.ClearSegmentedBarsInArea(x, y, w, h);
            canvas.ClearBoxesInArea(x, y, w, h);
        }
    }
}
