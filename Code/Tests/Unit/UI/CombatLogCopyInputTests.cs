using RPGGame.Tests;
using RPGGame.UI.Avalonia.Layout;
using RPGGame.UI.Avalonia.Utils;

namespace RPGGame.Tests.Unit.UI
{
    public static class CombatLogCopyInputTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;

            LayoutConstants.UpdateGridDimensions(210, 52);
            LayoutConstants.UpdateEffectiveVisibleWidth(2100, 10);

            int logX = LayoutConstants.CENTER_PANEL_X + 1;
            int logY = LayoutConstants.CENTER_PANEL_Y + 1;

            TestBase.AssertTrue(
                CombatLogCopyInput.ShouldCopyOnRightClick(
                    isRightButtonPressed: true,
                    isOverlayOpen: false,
                    isCombatDisplayActive: true,
                    gridX: logX,
                    gridY: logY),
                "right-click inside active combat log copies",
                ref run, ref passed, ref failed);

            TestBase.AssertTrue(
                !CombatLogCopyInput.ShouldCopyOnRightClick(
                    isRightButtonPressed: false,
                    isOverlayOpen: false,
                    isCombatDisplayActive: true,
                    gridX: logX,
                    gridY: logY),
                "left-click inside combat log does not copy",
                ref run, ref passed, ref failed);

            TestBase.AssertTrue(
                !CombatLogCopyInput.ShouldCopyOnRightClick(
                    isRightButtonPressed: true,
                    isOverlayOpen: true,
                    isCombatDisplayActive: true,
                    gridX: logX,
                    gridY: logY),
                "right-click is ignored while overlay is open",
                ref run, ref passed, ref failed);

            TestBase.AssertTrue(
                !CombatLogCopyInput.ShouldCopyOnRightClick(
                    isRightButtonPressed: true,
                    isOverlayOpen: false,
                    isCombatDisplayActive: false,
                    gridX: logX,
                    gridY: logY),
                "right-click is ignored when combat display is inactive",
                ref run, ref passed, ref failed);

            TestBase.AssertTrue(
                !CombatLogCopyInput.ShouldCopyOnRightClick(
                    isRightButtonPressed: true,
                    isOverlayOpen: false,
                    isCombatDisplayActive: true,
                    gridX: LayoutConstants.ACTION_INFO_X + 1,
                    gridY: LayoutConstants.ACTION_INFO_Y),
                "right-click on action strip does not copy",
                ref run, ref passed, ref failed);

            TestBase.AssertTrue(
                !CombatLogCopyInput.ShouldCopyOnRightClick(
                    isRightButtonPressed: true,
                    isOverlayOpen: false,
                    isCombatDisplayActive: true,
                    gridX: LayoutConstants.LEFT_PANEL_X,
                    gridY: logY),
                "right-click on side panel does not copy",
                ref run, ref passed, ref failed);

            TestBase.PrintSummary("CombatLogCopyInputTests", run, passed, failed);
        }
    }
}
