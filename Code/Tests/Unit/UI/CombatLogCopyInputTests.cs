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
                    allowCombatLogCopy: true,
                    gridX: logX,
                    gridY: logY),
                "right-click in center when battle-log copy is allowed (combat display and/or combat state)",
                ref run, ref passed, ref failed);

            TestBase.AssertTrue(
                !CombatLogCopyInput.ShouldCopyOnRightClick(
                    isRightButtonPressed: false,
                    isOverlayOpen: false,
                    allowCombatLogCopy: true,
                    gridX: logX,
                    gridY: logY),
                "left-click inside combat log does not copy",
                ref run, ref passed, ref failed);

            TestBase.AssertTrue(
                !CombatLogCopyInput.ShouldCopyOnRightClick(
                    isRightButtonPressed: true,
                    isOverlayOpen: true,
                    allowCombatLogCopy: true,
                    gridX: logX,
                    gridY: logY),
                "right-click is ignored while overlay is open",
                ref run, ref passed, ref failed);

            TestBase.AssertTrue(
                !CombatLogCopyInput.ShouldCopyOnRightClick(
                    isRightButtonPressed: true,
                    isOverlayOpen: false,
                    allowCombatLogCopy: false,
                    gridX: logX,
                    gridY: logY),
                "right-click is ignored when battle-log copy context is false",
                ref run, ref passed, ref failed);

            TestBase.AssertTrue(
                !CombatLogCopyInput.ShouldCopyOnRightClick(
                    isRightButtonPressed: true,
                    isOverlayOpen: false,
                    allowCombatLogCopy: true,
                    gridX: LayoutConstants.ACTION_INFO_X + 1,
                    gridY: LayoutConstants.ACTION_INFO_Y),
                "right-click on action strip does not copy",
                ref run, ref passed, ref failed);

            TestBase.AssertTrue(
                !CombatLogCopyInput.ShouldCopyOnRightClick(
                    isRightButtonPressed: true,
                    isOverlayOpen: false,
                    allowCombatLogCopy: true,
                    gridX: LayoutConstants.LEFT_PANEL_X,
                    gridY: logY),
                "right-click on side panel does not copy",
                ref run, ref passed, ref failed);

            double cw = 10;
            double ch = 18;
            double px = (LayoutConstants.CENTER_PANEL_X + 2) * cw + 3;
            double py = (LayoutConstants.CENTER_PANEL_Y + 2) * ch + 3;
            TestBase.AssertTrue(
                CombatLogCopyInput.ShouldCopyOnRightClick(
                    isRightButtonPressed: true,
                    isOverlayOpen: false,
                    allowCombatLogCopy: true,
                    gridX: 0,
                    gridY: 0,
                    pointerCanvasLocalX: px,
                    pointerCanvasLocalY: py,
                    charWidth: cw,
                    charHeight: ch),
                "pixel hit-test allows copy when grid cell is wrong but pointer is in center panel",
                ref run, ref passed, ref failed);

            TestBase.PrintSummary("CombatLogCopyInputTests", run, passed, failed);
        }
    }
}
