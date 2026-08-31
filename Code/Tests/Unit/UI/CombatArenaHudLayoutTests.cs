using RPGGame.Tests;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Geometry sanity checks for the combat center arena HUD bands.
    /// </summary>
    public static class CombatArenaHudLayoutTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;

            LayoutConstants.UpdateGridDimensions(210, 52);
            LayoutConstants.UpdateEffectiveVisibleWidth(2100, 10);

            CombatArenaHudLayout.GetEnemyHud(out int ex, out int ey, out int ew, out int eh);
            CombatArenaHudLayout.GetFighterHud(out int fx, out int fy, out int fw, out int fh);
            CombatArenaHudLayout.GetResolveStackBand(out int sx, out int sy, out int sw, out int sh);
            CombatArenaHudLayout.GetEnemyBars(out int enemyBarsX, out _, out int enemyBarsW);
            CombatArenaHudLayout.GetFighterBars(out int fighterBarsX, out _, out _);

            TestBase.AssertTrue(ey < sy && sy + sh <= fy,
                "resolve stack band sits between enemy and fighter HUDs",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(fy + fh <= LayoutConstants.ACTION_INFO_Y,
                "fighter HUD ends at or above the bottom combo strip",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(ex, fx, "enemy and fighter HUDs share left edge", ref run, ref passed, ref failed);
            TestBase.AssertEqual(ew, fw, "enemy and fighter HUDs share width", ref run, ref passed, ref failed);
            TestBase.AssertTrue(eh >= 3 && fh >= 4 && sh >= 3,
                "HUD bands have usable height",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(fx, fighterBarsX,
                "fighter bars are left-aligned in the arena",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(ex + ew, enemyBarsX + enemyBarsW,
                "enemy bars are right-aligned in the arena",
                ref run, ref passed, ref failed);

            CombatArenaHudLayout.GetNarrativeBand(out int nx, out int ny, out int nw, out int nh);
            TestBase.AssertTrue(ny >= ey + eh,
                "narrative band starts below enemy HUD",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(ny + nh <= fy,
                "narrative band ends above fighter HUD",
                ref run, ref passed, ref failed);

            TestBase.PrintSummary("CombatArenaHudLayoutTests", run, passed, failed);
        }
    }
}
