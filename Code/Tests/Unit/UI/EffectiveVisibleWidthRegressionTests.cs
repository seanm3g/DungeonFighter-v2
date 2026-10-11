using RPGGame.Tests;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Regression tests: font scale fills height (panel rows + bottom pad); column count shrinks with width (right pad).
    /// Ctrl+/- UI zoom scales center-column glyphs only; left/right chrome stay at design character sizes.
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
            TestUiZoomLeavesSideChromeAtBaseSizes(ref run, ref passed, ref failed);
            TestUiZoomLeavesActionInfoStripAtBaseSize(ref run, ref passed, ref failed);
            TestChromelessMenuStaysCenteredWhenColumnsChange(ref run, ref passed, ref failed);
            TestCanvasGridSizerIgnoresUiZoom(ref run, ref passed, ref failed);

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

        private static void TestUiZoomLeavesSideChromeAtBaseSizes(ref int run, ref int passed, ref int failed)
        {
            const double charW = 10;
            LayoutConstants.UpdateGridDimensions(210, 52);
            LayoutConstants.UpdateUiZoom(1.0);
            LayoutConstants.UpdateEffectiveVisibleWidth(2090, charW);
            int baseLeft = LayoutConstants.LEFT_PANEL_WIDTH;
            int baseRight = LayoutConstants.RIGHT_PANEL_WIDTH;
            int baseCenter = LayoutConstants.CENTER_PANEL_WIDTH;

            // Ctrl+/- no longer changes chrome column counts — grid stays window-fit 100%.
            LayoutConstants.UpdateUiZoom(1.5);
            LayoutConstants.UpdateEffectiveVisibleWidth(2090, charW);

            TestBase.AssertEqual(baseLeft, LayoutConstants.LEFT_PANEL_WIDTH,
                "left chrome columns stay at design size when UI zoom changes",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(baseRight, LayoutConstants.RIGHT_PANEL_WIDTH,
                "right chrome columns stay at design size when UI zoom changes",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(baseCenter, LayoutConstants.CENTER_PANEL_WIDTH,
                "center column width stays unchanged when only UI zoom changes",
                ref run, ref passed, ref failed);

            LayoutConstants.UpdateUiZoom(0.8);
            LayoutConstants.UpdateEffectiveVisibleWidth(2090, charW);
            TestBase.AssertEqual(baseLeft, LayoutConstants.LEFT_PANEL_WIDTH,
                "left chrome columns stay at design size when zoomed out",
                ref run, ref passed, ref failed);

            LayoutConstants.UpdateUiZoom(1.0);
            LayoutConstants.UpdateGridDimensions(210, 52);
            LayoutConstants.UpdateEffectiveVisibleWidth(2100, charW);
        }

        private static void TestUiZoomLeavesActionInfoStripAtBaseSize(ref int run, ref int passed, ref int failed)
        {
            LayoutConstants.UpdateUiZoom(1.0);
            LayoutConstants.UpdateGridDimensions(210, 52);
            LayoutConstants.UpdateEffectiveVisibleWidth(2100, 10);
            int baseStrip = LayoutConstants.ACTION_INFO_STRIP_HEIGHT;
            int baseCenterH = LayoutConstants.CENTER_PANEL_HEIGHT;

            LayoutConstants.UpdateUiZoom(1.5);
            TestBase.AssertEqual(baseStrip, LayoutConstants.ACTION_INFO_STRIP_HEIGHT,
                "action-info strip rows stay at design size when UI zoom changes",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(baseCenterH, LayoutConstants.CENTER_PANEL_HEIGHT,
                "center panel height stays unchanged when only UI zoom changes",
                ref run, ref passed, ref failed);

            LayoutConstants.UpdateUiZoom(1.0);
        }

        /// <summary>
        /// Main-menu options stay centered in the chromeless content rect when the window
        /// (and thus column count) changes. UI zoom itself no longer shrinks columns.
        /// </summary>
        private static void TestChromelessMenuStaysCenteredWhenColumnsChange(ref int run, ref int passed, ref int failed)
        {
            const double charW = 10;
            const int menuWidth = 48;
            const int menuLines = 4;

            LayoutConstants.UpdateUiZoom(1.0);
            LayoutConstants.UpdateGridDimensions(210, 52);
            LayoutConstants.UpdateEffectiveVisibleWidth(2090, charW);
            var (x100, y100, w100, h100) = LayoutConstants.GetChromelessContentRect();
            var (menuX100, _) = RPGGame.UI.Avalonia.Renderers.Menu.MenuLayoutCalculator.CalculateCenteredMenu(
                x100, y100, w100, h100, menuLines, menuWidth);
            double mid100 = menuX100 + menuWidth / 2.0;
            double gridMid100 = LayoutConstants.SCREEN_WIDTH / 2.0;
            TestBase.AssertTrue(System.Math.Abs(mid100 - gridMid100) <= 1.5,
                $"100% chromeless menu mid stays near grid mid (menuMid={mid100}, gridMid={gridMid100})",
                ref run, ref passed, ref failed);

            // Narrower window: fewer columns; menu must re-center on the new grid mid.
            LayoutConstants.UpdateGridDimensions(140, 52);
            LayoutConstants.UpdateEffectiveVisibleWidth(1390, charW);
            var (xNarrow, yNarrow, wNarrow, hNarrow) = LayoutConstants.GetChromelessContentRect();
            var (menuXNarrow, _) = RPGGame.UI.Avalonia.Renderers.Menu.MenuLayoutCalculator.CalculateCenteredMenu(
                xNarrow, yNarrow, wNarrow, hNarrow, menuLines, menuWidth);
            double midNarrow = menuXNarrow + menuWidth / 2.0;
            double gridMidNarrow = LayoutConstants.SCREEN_WIDTH / 2.0;
            TestBase.AssertTrue(System.Math.Abs(midNarrow - gridMidNarrow) <= 1.5,
                $"narrow chromeless menu mid stays near grid mid (menuMid={midNarrow}, gridMid={gridMidNarrow})",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(menuXNarrow < menuX100,
                $"menu start X moves left when columns shrink (wide={menuX100}, narrow={menuXNarrow})",
                ref run, ref passed, ref failed);

            LayoutConstants.UpdateUiZoom(1.0);
            LayoutConstants.UpdateGridDimensions(210, 52);
            LayoutConstants.UpdateEffectiveVisibleWidth(2100, charW);
        }

        private static void TestCanvasGridSizerIgnoresUiZoom(ref int run, ref int passed, ref int failed)
        {
            const double baseCharW = 10;
            const double baseCharH = 18;
            double height = CanvasGridSizer.DesignCanvasRowCount * baseCharH;
            double wideWidth = 300 * baseCharW;

            var at100 = CanvasGridSizer.Calculate(wideWidth, height, baseCharW, baseCharH, uiZoom: 1.0);
            var at120 = CanvasGridSizer.Calculate(wideWidth, height, baseCharW, baseCharH, uiZoom: 1.2);
            var at80 = CanvasGridSizer.Calculate(wideWidth, height, baseCharW, baseCharH, uiZoom: 0.8);

            TestBase.AssertTrue(System.Math.Abs(at100.ScaleFactor - at120.ScaleFactor) < 1e-9,
                "grid scale ignores UI zoom (120%)",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(System.Math.Abs(at100.ScaleFactor - at80.ScaleFactor) < 1e-9,
                "grid scale ignores UI zoom (80%)",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(at100.GridWidth, at120.GridWidth,
                "column count ignores UI zoom",
                ref run, ref passed, ref failed);
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

            // UI zoom is ignored by the grid sizer (center-column glyphs scale separately).
            var zoomed = CanvasGridSizer.Calculate(wideWidth, height, baseCharW, baseCharH, uiZoom: 1.2);
            TestBase.AssertTrue(System.Math.Abs(zoomed.ScaleFactor - wide.ScaleFactor) < 1e-9,
                "120% UI zoom does not change fill-height grid scale",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(wide.GridWidth, zoomed.GridWidth,
                "120% UI zoom does not change column count",
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
