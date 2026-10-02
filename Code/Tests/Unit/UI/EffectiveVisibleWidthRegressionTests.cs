using RPGGame.Tests;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Regression tests: font scale fills height (panel rows + bottom pad); column count shrinks with width (right pad).
    /// </summary>
    public static class EffectiveVisibleWidthRegressionTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;

            TestBase.AssertEqual(209, EffectiveColumnsFromPixels(210, 2100, 10), "nominal layout leaves one right outer-pad column", ref run, ref passed, ref failed);
            TestBase.AssertEqual(249, EffectiveColumnsFromPixels(250, 2500, 10), "wider grid still reserves right outer pad", ref run, ref passed, ref failed);
            TestBase.AssertEqual(209, EffectiveColumnsFromPixels(210, 2100 - 1e-9, 10), "float slop under full logical width stays at layout width", ref run, ref passed, ref failed);

            LayoutConstants.UpdateGridDimensions(210, 52);
            LayoutConstants.UpdateEffectiveVisibleWidth(5, 10);
            TestBase.AssertTrue(LayoutConstants.CENTER_PANEL_WIDTH >= 1,
                "center column width stays positive when effective width is tiny (strip/layout math)",
                ref run, ref passed, ref failed);

            TestNarrowerWindowShrinksCenter(ref run, ref passed, ref failed);
            TestCanvasGridSizerFillsHeightShrinksWidth(ref run, ref passed, ref failed);
            TestCanvasGridSizerNarrowWidthKeepsVerticalScale(ref run, ref passed, ref failed);
            TestCanvasGridSizerReservesBottomPanelRow(ref run, ref passed, ref failed);
            TestOuterPaddingRightAndBottom(ref run, ref passed, ref failed);

            TestBase.PrintSummary("EffectiveVisibleWidthRegressionTests", run, passed, failed);
        }

        private static void TestNarrowerWindowShrinksCenter(ref int run, ref int passed, ref int failed)
        {
            LayoutConstants.UpdateGridDimensions(210, 52);
            LayoutConstants.UpdateEffectiveVisibleWidth(2100, 10);
            int wideCenter = LayoutConstants.CENTER_PANEL_WIDTH;

            LayoutConstants.UpdateGridDimensions(160, 52);
            LayoutConstants.UpdateEffectiveVisibleWidth(1600, 10);
            int narrowCenter = LayoutConstants.CENTER_PANEL_WIDTH;

            TestBase.AssertTrue(narrowCenter < wideCenter,
                $"center panel shrinks when effective width drops (wide={wideCenter}, narrow={narrowCenter})",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(32, LayoutConstants.LEFT_PANEL_WIDTH,
                "left panel stays fixed character width on horizontal shrink",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(30, LayoutConstants.RIGHT_PANEL_WIDTH,
                "right panel stays fixed character width on horizontal shrink",
                ref run, ref passed, ref failed);

            LayoutConstants.UpdateGridDimensions(210, 52);
            LayoutConstants.UpdateEffectiveVisibleWidth(2100, 10);
        }

        private static void TestCanvasGridSizerFillsHeightShrinksWidth(ref int run, ref int passed, ref int failed)
        {
            const double baseCharW = 10;
            const double baseCharH = 18;
            double height = CanvasGridSizer.DesignCanvasRowCount * baseCharH; // scaleY = 1
            double wideWidth = 300 * baseCharW;
            var wide = CanvasGridSizer.Calculate(wideWidth, height, baseCharW, baseCharH);
            TestBase.AssertTrue(System.Math.Abs(wide.ScaleFactor - 1.0) < 1e-9, "fills height at scale 1 when height matches canvas rows", ref run, ref passed, ref failed);
            TestBase.AssertEqual(300, wide.GridWidth, "wide window expands column count", ref run, ref passed, ref failed);
            TestBase.AssertEqual(CanvasGridSizer.DesignCanvasRowCount, CanvasGridSizer.CanvasRowCount(wide.GridHeight),
                "canvas row count includes panel rows plus bottom outer pad",
                ref run, ref passed, ref failed);

            double midWidth = 180 * baseCharW;
            var mid = CanvasGridSizer.Calculate(midWidth, height, baseCharW, baseCharH);
            TestBase.AssertTrue(System.Math.Abs(mid.ScaleFactor - 1.0) < 1e-9, "horizontal shrink keeps the same vertical fill scale", ref run, ref passed, ref failed);
            TestBase.AssertEqual(180, mid.GridWidth, "narrower width reduces column count", ref run, ref passed, ref failed);
            TestBase.AssertTrue(mid.GridWidth < wide.GridWidth, "horizontal shrink reduces grid width", ref run, ref passed, ref failed);

            // UI zoom resizes the window (MainWindowStartupSizing); canvas scale always fills height.
            var stillFills = CanvasGridSizer.Calculate(wideWidth, height * 1.2, baseCharW, baseCharH);
            TestBase.AssertTrue(System.Math.Abs(stillFills.ScaleFactor - 1.2) < 1e-9,
                "taller window yields proportionally larger fill-height scale (zoom via window size)",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(stillFills.GridWidth < wide.GridWidth,
                "taller/larger scale fits fewer columns in the same pixel width",
                ref run, ref passed, ref failed);
        }

        private static void TestCanvasGridSizerNarrowWidthKeepsVerticalScale(ref int run, ref int passed, ref int failed)
        {
            const double baseCharW = 10;
            const double baseCharH = 18;
            double height = CanvasGridSizer.DesignCanvasRowCount * baseCharH;
            double tinyWidth = 40 * baseCharW;
            var tiny = CanvasGridSizer.Calculate(tinyWidth, height, baseCharW, baseCharH);
            TestBase.AssertTrue(System.Math.Abs(tiny.ScaleFactor - 1.0) < 1e-9,
                "very narrow window still fills vertically (does not scale font down for width)",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(40, tiny.GridWidth,
                "very narrow window shrinks column count to fit width",
                ref run, ref passed, ref failed);
        }

        private static void TestCanvasGridSizerReservesBottomPanelRow(ref int run, ref int passed, ref int failed)
        {
            const double baseCharW = 10;
            const double baseCharH = 18;
            double heightForLayoutRowsOnly = CanvasGridSizer.DesignLayoutRowCount * baseCharH;
            var sized = CanvasGridSizer.Calculate(2100, heightForLayoutRowsOnly, baseCharW, baseCharH);
            TestBase.AssertTrue(sized.ScaleFactor < 1.0,
                "layout-only pixel budget yields scale < 1 so canvas rows (with bottom pad) still fit",
                ref run, ref passed, ref failed);
            double paintedHeight = CanvasGridSizer.CanvasRowCount(sized.GridHeight) * baseCharH * sized.ScaleFactor;
            TestBase.AssertTrue(paintedHeight <= heightForLayoutRowsOnly + 1e-6,
                "canvas rows including bottom pad fit inside the available height",
                ref run, ref passed, ref failed);
        }

        private static void TestOuterPaddingRightAndBottom(ref int run, ref int passed, ref int failed)
        {
            LayoutConstants.UpdateGridDimensions(210, 52);
            LayoutConstants.UpdateEffectiveVisibleWidth(2100, 10);
            int rightEdge = LayoutConstants.RIGHT_PANEL_X + LayoutConstants.RIGHT_PANEL_WIDTH;
            TestBase.AssertEqual(209, rightEdge,
                "right panel ends one column before the grid edge (outer right pad)",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(
                LayoutConstants.LEFT_PANEL_HEIGHT + CanvasGridSizer.OuterPaddingBottom,
                CanvasGridSizer.CanvasRowCount(52),
                "canvas includes one row below the panel bottom border",
                ref run, ref passed, ref failed);
        }

        private static int EffectiveColumnsFromPixels(int gridWidth, double canvasPixelWidth, double charWidth)
        {
            LayoutConstants.UpdateGridDimensions(gridWidth, 52);
            LayoutConstants.UpdateEffectiveVisibleWidth(canvasPixelWidth, charWidth);
            return LayoutConstants.RIGHT_PANEL_X + LayoutConstants.RIGHT_PANEL_WIDTH;
        }
    }
}
