using System;
using RPGGame.Tests;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Ctrl+/- zooms center-column glyphs only; helpers drive wrap width, line stride, and X gating.
    /// </summary>
    public static class CenterColumnTextZoomTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;
            var preset = GameFonts.ActivePreset;
            double savedZoom = GameFonts.GetZoom(preset);

            try
            {
                LayoutConstants.UpdateUiZoom(1.0);
                LayoutConstants.UpdateGridDimensions(210, 52);
                LayoutConstants.UpdateEffectiveVisibleWidth(2090, 10);

                GameFonts.SetZoom(preset, 1.0);
                TestBase.AssertEqual(1, CenterColumnTextZoom.LineStride,
                    "100% zoom uses line stride 1",
                    ref run, ref passed, ref failed);
                TestBase.AssertEqual(40, CenterColumnTextZoom.ScaleWrapWidth(40),
                    "100% zoom keeps wrap width",
                    ref run, ref passed, ref failed);
                TestBase.AssertEqual(20, CenterColumnTextZoom.ScaleVisibleRows(20),
                    "100% zoom keeps visible rows",
                    ref run, ref passed, ref failed);
                TestBase.AssertTrue(Math.Abs(CenterColumnTextZoom.ScaledFontSize(16) - 16) < 1e-9,
                    "100% zoom keeps base font size",
                    ref run, ref passed, ref failed);

                GameFonts.SetZoom(preset, 1.5);
                TestBase.AssertEqual(2, CenterColumnTextZoom.LineStride,
                    "150% zoom uses line stride ceil(1.5)=2",
                    ref run, ref passed, ref failed);
                TestBase.AssertEqual(26, CenterColumnTextZoom.ScaleWrapWidth(40),
                    "150% zoom shrinks wrap width (floor(40/1.5)=26)",
                    ref run, ref passed, ref failed);
                TestBase.AssertEqual(10, CenterColumnTextZoom.ScaleVisibleRows(20),
                    "150% zoom halves visible logical rows (20/2)",
                    ref run, ref passed, ref failed);
                TestBase.AssertTrue(Math.Abs(CenterColumnTextZoom.ScaledFontSize(16) - 24) < 1e-9,
                    "150% zoom scales font size (16*1.5=24)",
                    ref run, ref passed, ref failed);

                GameFonts.SetZoom(preset, 0.8);
                TestBase.AssertEqual(1, CenterColumnTextZoom.LineStride,
                    "80% zoom keeps line stride 1",
                    ref run, ref passed, ref failed);
                TestBase.AssertEqual(40, CenterColumnTextZoom.ScaleWrapWidth(40),
                    "zoom-out does not expand wrap width (sides stay 100%)",
                    ref run, ref passed, ref failed);

                int leftInner = LayoutConstants.LEFT_PANEL_X + 1;
                int centerX = LayoutConstants.CENTER_PANEL_X + 2;
                int rightInner = LayoutConstants.RIGHT_PANEL_X + 1;
                TestBase.AssertFalse(CenterColumnTextZoom.IsCenterColumnX(leftInner),
                    "left panel X is not center column",
                    ref run, ref passed, ref failed);
                TestBase.AssertTrue(CenterColumnTextZoom.IsCenterColumnX(centerX),
                    "framed center panel X is center column",
                    ref run, ref passed, ref failed);
                TestBase.AssertFalse(CenterColumnTextZoom.IsCenterColumnX(rightInner),
                    "right panel X is not center column",
                    ref run, ref passed, ref failed);
            }
            finally
            {
                GameFonts.SetZoom(preset, savedZoom);
                LayoutConstants.UpdateUiZoom(1.0);
                LayoutConstants.UpdateGridDimensions(210, 52);
                LayoutConstants.UpdateEffectiveVisibleWidth(2100, 10);
            }

            TestBase.PrintSummary("CenterColumnTextZoomTests", run, passed, failed);
        }
    }
}
