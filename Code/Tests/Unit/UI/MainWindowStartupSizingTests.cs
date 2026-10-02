using Avalonia;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Helpers;

namespace RPGGame.Tests.Unit.UI
{
    public static class MainWindowStartupSizingTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;

            MacRetina_2560x1600_At2x_ScalesDown(ref run, ref passed, ref failed);
            MacScaled_1728x1117_FitsWithinWorkingArea(ref run, ref passed, ref failed);
            LargeWorkingArea_KeepsDesignSize(ref run, ref passed, ref failed);
            ClampedCenter_StaysInsideWorkArea(ref run, ref passed, ref failed);
            ScaledOverlayDimensions_AppliesRatio(ref run, ref passed, ref failed);

            TestBase.PrintSummary("MainWindowStartupSizingTests", run, passed, failed);
        }

        /// <summary>
        /// 2560×1600 Retina Mac at 2× scaling: logical ~1280×800 with menu bar and dock.
        /// </summary>
        private static void MacRetina_2560x1600_At2x_ScalesDown(ref int run, ref int passed, ref int failed)
        {
            const double logicalWidth = 1280;
            const double logicalHeight = 705;

            var (width, height, scale) = MainWindowStartupSizing.ComputeMacStartupSize(
                logicalWidth,
                logicalHeight,
                extraBottomMargin: MainWindowStartupSizing.MacDockSafetyMarginLogical,
                extraTopMargin: MainWindowStartupSizing.MacTitleBarSafetyMarginLogical);

            const double totalVerticalMargin =
                (MainWindowStartupSizing.MacEdgeMarginLogical * 2)
                + MainWindowStartupSizing.MacDockSafetyMarginLogical
                + MainWindowStartupSizing.MacTitleBarSafetyMarginLogical;

            TestBase.AssertTrue(scale < 1.0, "2560×1600 Retina scales below design size", ref run, ref passed, ref failed);
            TestBase.AssertTrue(width <= logicalWidth - 32, "scaled width fits working area with margins", ref run, ref passed, ref failed);
            TestBase.AssertTrue(height <= logicalHeight - totalVerticalMargin, "scaled height fits dock-safe working area", ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                System.Math.Abs(width / height - MainWindowStartupSizing.DesignWidth / MainWindowStartupSizing.DesignHeight) < 0.01,
                "aspect ratio preserved",
                ref run, ref passed, ref failed);
        }

        /// <summary>
        /// Default scaled resolution on many 2560×1600 MacBook Pro displays (~1728×1117 logical).
        /// </summary>
        private static void MacScaled_1728x1117_FitsWithinWorkingArea(ref int run, ref int passed, ref int failed)
        {
            const double logicalWidth = 1728;
            const double logicalHeight = 1050;

            var (width, height, scale) = MainWindowStartupSizing.ComputeMacStartupSize(
                logicalWidth,
                logicalHeight,
                extraBottomMargin: MainWindowStartupSizing.MacDockSafetyMarginLogical,
                extraTopMargin: MainWindowStartupSizing.MacTitleBarSafetyMarginLogical);

            TestBase.AssertTrue(scale < 1.0, "1728×1117 logical display scales down slightly", ref run, ref passed, ref failed);
            TestBase.AssertTrue(width <= logicalWidth, "width within working area", ref run, ref passed, ref failed);
            TestBase.AssertTrue(height <= logicalHeight, "height within working area", ref run, ref passed, ref failed);
            TestBase.AssertTrue(width >= MainWindowStartupSizing.MinStartupWidth, "width respects minimum", ref run, ref passed, ref failed);
            TestBase.AssertTrue(height >= MainWindowStartupSizing.MinStartupHeight, "height respects minimum", ref run, ref passed, ref failed);
        }

        private static void LargeWorkingArea_KeepsDesignSize(ref int run, ref int passed, ref int failed)
        {
            var (width, height, scale) = MainWindowStartupSizing.ComputeMacStartupSize(2560, 1600);

            TestBase.AssertEqual(MainWindowStartupSizing.DesignWidth, width, "large monitor keeps design width", ref run, ref passed, ref failed);
            TestBase.AssertEqual(MainWindowStartupSizing.DesignHeight, height, "large monitor keeps design height", ref run, ref passed, ref failed);
            TestBase.AssertEqual(1.0, scale, "large monitor scale is 1.0", ref run, ref passed, ref failed);
        }

        private static void ClampedCenter_StaysInsideWorkArea(ref int run, ref int passed, ref int failed)
        {
            var wa = new PixelRect(new PixelPoint(0, 37), new PixelSize(2560, 1410));
            var p = MainWindowStartupSizing.ComputeClampedCenteredTopLeft(wa, 2200, 1300, edgeMarginPixels: 16);

            TestBase.AssertTrue(p.X >= wa.X, "clamped X not left of work area", ref run, ref passed, ref failed);
            TestBase.AssertTrue(p.Y >= wa.Y, "clamped Y not above work area", ref run, ref passed, ref failed);
            TestBase.AssertTrue(p.X + 2200 <= wa.X + wa.Width, "clamped right edge inside work area", ref run, ref passed, ref failed);
            TestBase.AssertTrue(p.Y + 1300 <= wa.Y + wa.Height, "clamped bottom edge inside work area", ref run, ref passed, ref failed);
        }

        private static void ScaledOverlayDimensions_AppliesRatio(ref int run, ref int passed, ref int failed)
        {
            const double ratio = 0.75;
            var dims = MainWindowStartupSizing.ComputeScaledOverlayDimensions(1000, 650, 900, 650, ratio);

            TestBase.AssertEqual(750.0, dims.Width, "overlay width scaled", ref run, ref passed, ref failed);
            TestBase.AssertEqual(487.5, dims.Height, "overlay height scaled", ref run, ref passed, ref failed);
            TestBase.AssertEqual(675.0, dims.MinWidth, "overlay min width scaled", ref run, ref passed, ref failed);
            TestBase.AssertEqual(487.5, dims.MinHeight, "overlay min height scaled", ref run, ref passed, ref failed);
        }
    }
}
