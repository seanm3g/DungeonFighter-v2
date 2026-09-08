using RPGGame.Tests;
using RPGGame.UI.Avalonia.CombatVisuals;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.Tests.Unit.UI;

public static class CombatSceneGeometryTests
{
    public static void RunAllTests()
    {
        int run = 0, passed = 0, failed = 0;
        TestBase.AssertTrue(!CombatSceneGeometry.CanSplit(69, 30, true), "narrow arena falls back to text", ref run, ref passed, ref failed);
        TestBase.AssertTrue(!CombatSceneGeometry.CanSplit(100, 16, true), "short arena preserves cards", ref run, ref passed, ref failed);
        TestBase.AssertTrue(!CombatSceneGeometry.CanSplit(100, 30, false), "text mode keeps full resolve band", ref run, ref passed, ref failed);
        TestBase.AssertTrue(CombatSceneGeometry.CanSplit(70, 17, true), "minimum supported arena fits visual mode", ref run, ref passed, ref failed);
        bool previous = CombatArenaHudLayout.IllustratedSceneVisible;
        bool priorIllustrated = GameConfiguration.Instance.UICustomization.IllustratedCombat;
        try
        {
            CombatArenaHudLayout.IllustratedSceneVisible = true;
            GameConfiguration.Instance.UICustomization.IllustratedCombat = true;
            if (CombatArenaHudLayout.UseCinematic)
            {
                CombatArenaHudLayout.GetSceneRect(out int cx,out int cinematicY,out int cw,out int chh);
                CombatArenaHudLayout.GetResolutionRect(out int dx,out int dy,out int dw,out int dh);
                TestBase.AssertTrue(cinematicY+chh<=dy && cx==dx && cw==dw,"cinematic resolution stays below full width scene",ref run,ref passed,ref failed);
            }
            GameConfiguration.Instance.UICustomization.IllustratedCombat = false;
            CombatArenaHudLayout.GetSceneRect(out int sx, out int sy, out int sw, out int sh);
            CombatArenaHudLayout.GetResolveStackBand(out int rx, out int ry, out _, out int rh);
            CombatArenaHudLayout.GetFighterHud(out _, out int fy, out _, out _);
            TestBase.AssertTrue(sh == 0 || sx + sw < rx, "scene does not overlap action cards", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ry + rh <= fy, "action cards remain above fighter HUD", ref run, ref passed, ref failed);
            CombatArenaHudLayout.GetResolveCurrentCardRect(out _, out int cy, out _, out int ch);
            CombatArenaHudLayout.GetResolvePreviousCardRect(out _, out int py, out _, out int ph);
            CombatArenaHudLayout.GetResolveEnemyPreviousCardRect(out _, out int ey, out _, out int eh);
            TestBase.AssertTrue(sh == 0 || (cy + ch <= py && py + ph <= ey && ey + eh <= ry + rh),
                "vertical cards remain separate and inside resolve band", ref run, ref passed, ref failed);
        }
        finally { CombatArenaHudLayout.IllustratedSceneVisible = previous; GameConfiguration.Instance.UICustomization.IllustratedCombat = priorIllustrated; }
        TestBase.PrintSummary("CombatSceneGeometryTests", run, passed, failed);
    }
}
