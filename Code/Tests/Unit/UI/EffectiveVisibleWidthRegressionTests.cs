using RPGGame.Tests;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Regression tests for fullscreen ghosting: effective column count must stay tied to the logical grid
    /// when pixel bounds are wider than GridWidth * charWidth (letterboxing).
    /// </summary>
    public static class EffectiveVisibleWidthRegressionTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;

            TestBase.AssertEqual(210, EffectiveColumnsFromPixels(2100, 10), "nominal grid width", ref run, ref passed, ref failed);
            TestBase.AssertEqual(210, EffectiveColumnsCappedLikeGameCanvas(5000, 10, 210), "capped wide bounds matches grid", ref run, ref passed, ref failed);
            TestBase.AssertEqual(210, EffectiveColumnsFromPixels(2500, 10), "uncapped pixel width clamps to grid width", ref run, ref passed, ref failed);
            TestBase.AssertEqual(210, EffectiveColumnsFromPixels(2100 - 1e-9, 10), "float slop under full logical width stays at grid", ref run, ref passed, ref failed);

            LayoutConstants.UpdateGridDimensions(210, 52);
            LayoutConstants.UpdateEffectiveVisibleWidth(5, 10);
            TestBase.AssertTrue(LayoutConstants.CENTER_PANEL_WIDTH >= 1,
                "center column width stays positive when effective width is tiny (strip/layout math)",
                ref run, ref passed, ref failed);

            TestBase.PrintSummary("EffectiveVisibleWidthRegressionTests", run, passed, failed);
        }

        private static int EffectiveColumnsFromPixels(double canvasPixelWidth, double charWidth)
        {
            LayoutConstants.UpdateGridDimensions(210, 52);
            LayoutConstants.UpdateEffectiveVisibleWidth(canvasPixelWidth, charWidth);
            return LayoutConstants.RIGHT_PANEL_X + LayoutConstants.RIGHT_PANEL_WIDTH;
        }

        private static int EffectiveColumnsCappedLikeGameCanvas(double boundsWidth, double charWidth, int gridWidth)
        {
            LayoutConstants.UpdateGridDimensions(gridWidth, 52);
            double logicalPixelWidth = gridWidth * charWidth;
            double widthForLayout = boundsWidth > 0 ? System.Math.Min(boundsWidth, logicalPixelWidth) : logicalPixelWidth;
            LayoutConstants.UpdateEffectiveVisibleWidth(widthForLayout, charWidth);
            return LayoutConstants.RIGHT_PANEL_X + LayoutConstants.RIGHT_PANEL_WIDTH;
        }
    }
}
