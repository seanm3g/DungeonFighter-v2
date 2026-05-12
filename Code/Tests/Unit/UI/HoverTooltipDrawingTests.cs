using System;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.Tests.Unit.UI
{
    public static class HoverTooltipDrawingTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== HoverTooltipDrawing Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            var r = HoverTooltipDrawing.GetPaddedClearRegion(10, 5, 20, 4, 8, 2, 100, 50, pad: 1);
            TestBase.AssertEqual(9, r.gx, "pad left when allowed", ref run, ref passed, ref failed);
            TestBase.AssertEqual(4, r.gy, "pad top when allowed", ref run, ref passed, ref failed);
            TestBase.AssertEqual(22, r.gw, "width includes box + pad both sides", ref run, ref passed, ref failed);
            TestBase.AssertEqual(6, r.gh, "height includes box + pad both sides", ref run, ref passed, ref failed);

            var clamped = HoverTooltipDrawing.GetPaddedClearRegion(10, 5, 8, 3, 10, 5, 15, 20, pad: 1);
            TestBase.AssertEqual(10, clamped.gx, "clamp left to innerLeft", ref run, ref passed, ref failed);
            TestBase.AssertEqual(5, clamped.gy, "clamp top to innerTop", ref run, ref passed, ref failed);
            TestBase.AssertEqual(6, clamped.gw, "clamp right edge", ref run, ref passed, ref failed);
            TestBase.AssertEqual(4, clamped.gh, "clamp bottom edge", ref run, ref passed, ref failed);

            int leftOfRightColumn = HoverTooltipDrawing.GetHorizontalPositionAvoidingTarget(
                defaultX: 50,
                boxW: 20,
                innerLeft: 10,
                innerRightInclusive: 99,
                targetX: 60,
                targetWidth: 30);
            TestBase.AssertEqual(38, leftOfRightColumn, "right-side target opens tooltip to the left", ref run, ref passed, ref failed);

            int rightOfLeftColumn = HoverTooltipDrawing.GetHorizontalPositionAvoidingTarget(
                defaultX: 50,
                boxW: 20,
                innerLeft: 10,
                innerRightInclusive: 99,
                targetX: 20,
                targetWidth: 30);
            TestBase.AssertEqual(52, rightOfLeftColumn, "left-side target opens tooltip to the right", ref run, ref passed, ref failed);

            TestBase.PrintSummary("HoverTooltipDrawing Tests", run, passed, failed);
        }
    }
}
