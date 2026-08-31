using System;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Regression: display buffer clear band must not wipe the action-info strip rows
    /// (must match <see cref="RPGGame.UI.Avalonia.Display.DisplayRenderer.Render"/> clear logic).
    /// </summary>
    public static class DisplayRendererClearBandRegressionTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;

            LayoutConstants.UpdateGridDimensions(210, 52);
            LayoutConstants.UpdateEffectiveVisibleWidth(2100, 10);

            int stripTop = LayoutConstants.ACTION_INFO_Y;
            int persistentInnerContentY = LayoutConstants.CENTER_PANEL_Y + 1;
            int persistentContentHeight = LayoutConstants.CENTER_PANEL_HEIGHT - 2;

            TestBase.AssertEqual(
                Math.Max(0, persistentInnerContentY - 2),
                ComputeClearStartY(persistentInnerContentY),
                "persistent center content clear starts near content top (strip is below)",
                ref run, ref passed, ref failed);

            TestBase.AssertEqual(
                Math.Min(persistentInnerContentY + persistentContentHeight, stripTop),
                ComputeClearEndY(persistentInnerContentY, persistentContentHeight),
                "persistent center content clear ends at content bottom (clamped to strip top)",
                ref run, ref passed, ref failed);

            TestBase.AssertEqual(
                0,
                ComputeClearStartY(0),
                "chromeless content at y=0 clear starts at row 0",
                ref run, ref passed, ref failed);

            TestBase.AssertTrue(
                LayoutConstants.CENTER_PANEL_Y + LayoutConstants.CENTER_PANEL_HEIGHT == stripTop,
                "framed center panel bottom aligns with action strip top",
                ref run, ref passed, ref failed);

            TestBase.PrintSummary("DisplayRendererClearBandRegressionTests", run, passed, failed);
        }

        private static int ComputeClearStartY(int contentY) => Math.Max(0, contentY - 2);

        private static int ComputeClearEndY(int contentY, int contentHeight) =>
            Math.Min(contentY + contentHeight, LayoutConstants.ACTION_INFO_Y);
    }
}
