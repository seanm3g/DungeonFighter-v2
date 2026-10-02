using System;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Tests for left character panel viewport clip/scroll math.
    /// </summary>
    public static class LeftPanelViewportTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== LeftPanelViewport Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            LayoutConstants.UpdateGridDimensions(210, 52);
            LayoutConstants.UpdateEffectiveVisibleWidth(2100, 10);

            TestContentBandInsideBorder(ref run, ref passed, ref failed);
            TestRowVisibility(ref run, ref passed, ref failed);
            TestClampScroll(ref run, ref passed, ref failed);

            TestBase.PrintSummary("LeftPanelViewport Tests", run, passed, failed);
        }

        private static void TestContentBandInsideBorder(ref int run, ref int passed, ref int failed)
        {
            TestBase.AssertEqual(
                LayoutConstants.LEFT_PANEL_Y + 1,
                LeftPanelViewport.ContentTop,
                "content top is below top border",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(
                LayoutConstants.LEFT_PANEL_Y + LayoutConstants.LEFT_PANEL_HEIGHT - 1,
                LeftPanelViewport.ContentBottomExclusive,
                "content bottom exclusive is top of bottom border",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                LeftPanelViewport.ContentHeight == LeftPanelViewport.ContentBottomExclusive - LeftPanelViewport.ContentTop,
                "content height matches exclusive band",
                ref run, ref passed, ref failed);
        }

        private static void TestRowVisibility(ref int run, ref int passed, ref int failed)
        {
            int top = LeftPanelViewport.ContentTop;
            int bottomEx = LeftPanelViewport.ContentBottomExclusive;
            TestBase.AssertTrue(LeftPanelViewport.IsRowVisible(top), "first body row visible", ref run, ref passed, ref failed);
            TestBase.AssertTrue(LeftPanelViewport.IsRowVisible(bottomEx - 1), "last body row visible", ref run, ref passed, ref failed);
            TestBase.AssertFalse(LeftPanelViewport.IsRowVisible(top - 1), "top border row not body-visible", ref run, ref passed, ref failed);
            TestBase.AssertFalse(LeftPanelViewport.IsRowVisible(bottomEx), "bottom border row not body-visible", ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                LeftPanelViewport.IsRangeVisible(bottomEx - 1, 2),
                "range overlapping last body row is visible",
                ref run, ref passed, ref failed);
            TestBase.AssertFalse(
                LeftPanelViewport.IsRangeVisible(bottomEx, 2),
                "range entirely on/after bottom border is not visible",
                ref run, ref passed, ref failed);
        }

        private static void TestClampScroll(ref int run, ref int passed, ref int failed)
        {
            int viewport = LeftPanelViewport.ContentHeight;
            TestBase.AssertEqual(0, LeftPanelViewport.MaxScrollOffset(viewport - 5, viewport),
                "content shorter than viewport: max scroll 0", ref run, ref passed, ref failed);
            TestBase.AssertEqual(12, LeftPanelViewport.MaxScrollOffset(viewport + 12, viewport),
                "overflow rows become max scroll", ref run, ref passed, ref failed);
            TestBase.AssertEqual(0, LeftPanelViewport.ClampScrollOffset(-3, viewport + 12, viewport),
                "negative scroll clamps to 0", ref run, ref passed, ref failed);
            TestBase.AssertEqual(12, LeftPanelViewport.ClampScrollOffset(99, viewport + 12, viewport),
                "large scroll clamps to max", ref run, ref passed, ref failed);
        }
    }
}
