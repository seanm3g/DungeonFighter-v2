using RPGGame.Tests;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Hit tests for <see cref="LayoutConstants.ContainsCenterPanelContent"/> (combat log vs action strip).
    /// Main-window mouse wheel scrolling uses the same predicate so the wheel only moves the log, not when the pointer is over the action strip or side panels.
    /// </summary>
    public static class LayoutConstantsCenterPanelHitTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;

            LayoutConstants.UpdateGridDimensions(210, 52);
            LayoutConstants.UpdateEffectiveVisibleWidth(2100, 10);

            int stripTop = LayoutConstants.ACTION_INFO_Y;
            int centerTop = LayoutConstants.CENTER_PANEL_Y;

            TestBase.AssertTrue(
                !LayoutConstants.ContainsCenterPanelContent(LayoutConstants.CENTER_PANEL_X + 1, stripTop),
                "action-info strip row is not center panel content",
                ref run, ref passed, ref failed);

            TestBase.AssertTrue(
                LayoutConstants.ContainsCenterPanelContent(LayoutConstants.CENTER_PANEL_X + 1, centerTop),
                "first row of framed center panel counts as center panel content",
                ref run, ref passed, ref failed);

            TestBase.AssertTrue(
                !LayoutConstants.ContainsCenterPanelContent(LayoutConstants.LEFT_PANEL_X, centerTop),
                "left panel column is not center panel content",
                ref run, ref passed, ref failed);

            double cw = 10;
            double ch = 18;
            double insideX = (LayoutConstants.CENTER_PANEL_X + 1) * cw + 0.5 * cw;
            double insideY = (LayoutConstants.CENTER_PANEL_Y + 1) * ch + 0.5 * ch;
            TestBase.AssertTrue(
                LayoutConstants.ContainsCenterPanelPixelHit(insideX, insideY, cw, ch),
                "pixel hit inside center panel matches grid interior",
                ref run, ref passed, ref failed);

            double leftX = (LayoutConstants.LEFT_PANEL_X + 1) * cw;
            TestBase.AssertTrue(
                !LayoutConstants.ContainsCenterPanelPixelHit(leftX, insideY, cw, ch),
                "pixel hit over left panel is not center panel",
                ref run, ref passed, ref failed);

            TestBase.PrintSummary("LayoutConstantsCenterPanelHitTests", run, passed, failed);
        }
    }
}
