using RPGGame.Tests;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Regression: display buffer clear band must not start inside the action-info strip rows
    /// (must match <see cref="RPGGame.UI.Avalonia.Display.DisplayRenderer.Render"/> clear logic).
    /// </summary>
    public static class DisplayRendererClearBandRegressionTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;

            LayoutConstants.UpdateGridDimensions(210, 52);
            LayoutConstants.UpdateEffectiveVisibleWidth(2100, 10);

            int firstRowBelowStrip = LayoutConstants.ACTION_INFO_Y + LayoutConstants.ACTION_INFO_HEIGHT;
            int persistentInnerContentY = LayoutConstants.CENTER_PANEL_Y + 1;

            TestBase.AssertEqual(
                firstRowBelowStrip,
                ComputeClearStartY(persistentInnerContentY),
                "persistent center content clear starts at first row below action strip",
                ref run, ref passed, ref failed);

            TestBase.AssertEqual(
                0,
                ComputeClearStartY(0),
                "chromeless content at y=0 clear starts at row 0",
                ref run, ref passed, ref failed);

            TestBase.AssertEqual(
                LayoutConstants.CENTER_PANEL_Y,
                firstRowBelowStrip,
                "first row below strip aligns with framed center panel top",
                ref run, ref passed, ref failed);

            TestBase.PrintSummary("DisplayRendererClearBandRegressionTests", run, passed, failed);
        }

        /// <summary>Mirrors DisplayRenderer scroll-area clear start calculation.</summary>
        private static int ComputeClearStartY(int contentY)
        {
            int scrollOverflowPad = System.Math.Max(0, contentY - 2);
            int firstRowBelowActionStrip = LayoutConstants.ACTION_INFO_Y + LayoutConstants.ACTION_INFO_HEIGHT;
            return contentY >= firstRowBelowActionStrip
                ? System.Math.Max(scrollOverflowPad, firstRowBelowActionStrip)
                : scrollOverflowPad;
        }
    }
}
