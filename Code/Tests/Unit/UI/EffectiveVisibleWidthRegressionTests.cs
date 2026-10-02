using RPGGame.Tests;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Regression tests: font scale fills height (panel rows + bottom pad); column count shrinks with width (right pad).
    /// UI zoom keeps left/top/right chrome near design pixel size; center absorbs leftover cells.
    /// </summary>
    public static class EffectiveVisibleWidthRegressionTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;

            // Tests assume 100% UI zoom unless a case sets otherwise.
            LayoutConstants.UpdateUiZoom(1.0);

            TestBase.AssertEqual(209, EffectiveColumnsFromPixels(210, 2100, 10), "nominal layout leaves one right outer-pad column", ref run, ref passed, ref failed);
            TestBase.AssertEqual(249, EffectiveColumnsFromPixels(250, 2500, 10), "wider grid still reserves right outer pad", ref run, ref passed, ref failed);
            TestBase.AssertEqual(209, EffectiveColumnsFromPixels(210, 2100 - 1e-9, 10), "float slop under full logical width stays at layout width", ref run, ref passed, ref failed);

            LayoutConstants.UpdateUiZoom(1.0);
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
            TestContentOriginCentersZoomedContent(ref run, ref passed, ref failed);
            TestFontScaleSnapsToWholePixels(ref run, ref passed, ref failed);
            TestUiZoomKeepsSideChromePixelFootprint(ref run, ref passed, ref failed);
            TestUiZoomShrinksActionInfoStrip(ref run, ref passed, ref failed);

            LayoutConstants.UpdateUiZoom(1.0);
            LayoutConstants.UpdateGridDimensions(210, 52);
            LayoutConstants.UpdateEffectiveVisibleWidth(2100, 10);

            TestBase.PrintSummary("EffectiveVisibleWidthRegressionTests", run, passed, failed);
        }

        private static void TestNarrowerWindowShrinksCenter(ref int run, ref int passed, ref int failed)
        {
            LayoutConstants.UpdateUiZoom(1.0);
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
                "left panel stays fixed character width on horizontal shrink at 100% zoom",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(30, LayoutConstants.RIGHT_PANEL_WIDTH,
                "right panel stays fixed character width on horizontal shrink at 100% zoom",
                ref run, ref passed, ref failed);

            LayoutConstants.UpdateGridDimensions(210, 52);
            LayoutConstants.UpdateEffectiveVisibleWidth(2100, 10);
        }

        private static void TestUiZoomKeepsSideChromePixelFootprint(ref int run, ref int passed, ref int failed)
        {
            const double charW = 10;
            LayoutConstants.UpdateGridDimensions(210, 52);
            LayoutConstants.UpdateUiZoom(1.0);
            LayoutConstants.UpdateEffectiveVisibleWidth(2090, charW);
            int baseLeft = LayoutConstants.LEFT_PANEL_WIDTH;
            int baseRight = LayoutConstants.RIGHT_PANEL_WIDTH;
            int baseCenter = LayoutConstants.CENTER_PANEL_WIDTH;

            // Zoom in: fewer total columns, but chrome character widths shrink so pixel footprint ~matches 100%.
            LayoutConstants.UpdateGridDimensions(187, 52); // ~210 / 1.12
            LayoutConstants.UpdateUiZoom(1.12);
            LayoutConstants.UpdateEffectiveVisibleWidth(1870, charW);

            TestBase.AssertTrue(LayoutConstants.LEFT_PANEL_WIDTH < baseLeft,
                $"left chrome columns shrink when zoomed in (base={baseLeft}, zoomed={LayoutConstants.LEFT_PANEL_WIDTH})",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(LayoutConstants.RIGHT_PANEL_WIDTH < baseRight,
                $"right chrome columns shrink when zoomed in (base={baseRight}, zoomed={LayoutConstants.RIGHT_PANEL_WIDTH})",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                System.Math.Abs(LayoutConstants.LEFT_PANEL_WIDTH * 1.12 - baseLeft) < 1.5,
                $"left chrome pixel footprint stays near design (cols*zoom≈{LayoutConstants.LEFT_PANEL_WIDTH * 1.12}, base={baseLeft})",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                System.Math.Abs(LayoutConstants.RIGHT_PANEL_WIDTH * 1.12 - baseRight) < 1.5,
                $"right chrome pixel footprint stays near design (cols*zoom≈{LayoutConstants.RIGHT_PANEL_WIDTH * 1.12}, base={baseRight})",
                ref run, ref passed, ref failed);

            // Zoom out: chrome columns grow; center shrinks to accommodate.
            LayoutConstants.UpdateGridDimensions(262, 52); // ~210 / 0.8
            LayoutConstants.UpdateUiZoom(0.8);
            LayoutConstants.UpdateEffectiveVisibleWidth(2620, charW);
            TestBase.AssertTrue(LayoutConstants.LEFT_PANEL_WIDTH > baseLeft,
                "left chrome columns grow when zoomed out",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(LayoutConstants.CENTER_PANEL_WIDTH < baseCenter
                || LayoutConstants.LEFT_PANEL_WIDTH + LayoutConstants.RIGHT_PANEL_WIDTH > baseLeft + baseRight,
                "zoomed-out chrome takes more character columns (center yields)",
                ref run, ref passed, ref failed);

            LayoutConstants.UpdateUiZoom(1.0);
            LayoutConstants.UpdateGridDimensions(210, 52);
            LayoutConstants.UpdateEffectiveVisibleWidth(2100, charW);
        }

        private static void TestUiZoomShrinksActionInfoStrip(ref int run, ref int passed, ref int failed)
        {
            LayoutConstants.UpdateUiZoom(1.0);
            LayoutConstants.UpdateGridDimensions(210, 52);
            LayoutConstants.UpdateEffectiveVisibleWidth(2100, 10);
            int baseStrip = LayoutConstants.ACTION_INFO_STRIP_HEIGHT;
            int baseCenterH = LayoutConstants.CENTER_PANEL_HEIGHT;

            LayoutConstants.UpdateUiZoom(1.5);
            TestBase.AssertTrue(LayoutConstants.ACTION_INFO_STRIP_HEIGHT < baseStrip,
                $"action-info strip rows shrink when zoomed in (base={baseStrip}, zoomed={LayoutConstants.ACTION_INFO_STRIP_HEIGHT})",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(LayoutConstants.CENTER_PANEL_HEIGHT > baseCenterH,
                "center panel height grows when the top strip shrinks for zoom",
                ref run, ref passed, ref failed);

            LayoutConstants.UpdateUiZoom(1.0);
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

            // UI zoom multiplies fill-height scale inside a fixed window (does not resize the window).
            var zoomed = CanvasGridSizer.Calculate(wideWidth, height, baseCharW, baseCharH, uiZoom: 1.2);
            TestBase.AssertTrue(System.Math.Abs(zoomed.ScaleFactor - 1.2) < 1e-9,
                "120% UI zoom scales glyphs inside the same window height",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(zoomed.GridWidth < wide.GridWidth,
                "larger UI zoom fits fewer columns in the same pixel width",
                ref run, ref passed, ref failed);

            var zoomedOut = CanvasGridSizer.Calculate(wideWidth, height, baseCharW, baseCharH, uiZoom: 0.8);
            TestBase.AssertTrue(System.Math.Abs(zoomedOut.ScaleFactor - 0.8) < 1e-9,
                "80% UI zoom shrinks glyphs inside the same window height",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(zoomedOut.GridWidth > wide.GridWidth,
                "smaller UI zoom fits more columns in the same pixel width",
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
            LayoutConstants.UpdateUiZoom(1.0);
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

        private static void TestContentOriginCentersZoomedContent(ref int run, ref int passed, ref int failed)
        {
            // Zoom-in: content taller than viewport → negative Y offset keeps the middle on screen.
            var zoomedIn = CanvasGridSizer.CalculateContentOrigin(
                viewportWidth: 1000,
                viewportHeight: 800,
                contentWidth: 1000,
                contentHeight: 800 * 1.74);
            TestBase.AssertTrue(zoomedIn.OffsetX == 0,
                "width-matched content has no X origin shift",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(zoomedIn.OffsetY < 0,
                "taller zoomed content centers with a negative Y origin (crop equally)",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(
                (int)System.Math.Round((800 - 800 * 1.74) / 2.0),
                (int)zoomedIn.OffsetY,
                "zoomed-in Y origin is the rounded half overflow",
                ref run, ref passed, ref failed);

            // Zoom-out: content shorter than viewport → positive Y letterbox.
            var zoomedOut = CanvasGridSizer.CalculateContentOrigin(1000, 800, 1000, 640);
            TestBase.AssertEqual(80, (int)zoomedOut.OffsetY,
                "shorter content letterboxes equally top/bottom",
                ref run, ref passed, ref failed);

            var matched = CanvasGridSizer.CalculateContentOrigin(1000, 800, 1000, 800);
            TestBase.AssertTrue(matched.OffsetX == 0 && matched.OffsetY == 0,
                "exact fit keeps origin at (0,0)",
                ref run, ref passed, ref failed);
        }

        private static void TestFontScaleSnapsToWholePixels(ref int run, ref int passed, ref int failed)
        {
            var converter = new RPGGame.UI.Avalonia.Canvas.CanvasCoordinateConverter();
            converter.SetScaleFactor(1.234); // 16 * 1.234 = 19.744 → 20
            TestBase.AssertTrue(System.Math.Abs(converter.GetFontSize() - 20.0) < 1e-9,
                "font size snaps to whole pixels for pixel-font alignment",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(System.Math.Abs(converter.GetScaleFactor() - (20.0 / 16.0)) < 1e-9,
                "scale factor stays in sync with snapped font size",
                ref run, ref passed, ref failed);
        }

        private static int EffectiveColumnsFromPixels(int gridWidth, double canvasPixelWidth, double charWidth)
        {
            LayoutConstants.UpdateUiZoom(1.0);
            LayoutConstants.UpdateGridDimensions(gridWidth, 52);
            LayoutConstants.UpdateEffectiveVisibleWidth(canvasPixelWidth, charWidth);
            return LayoutConstants.RIGHT_PANEL_X + LayoutConstants.RIGHT_PANEL_WIDTH;
        }
    }
}
