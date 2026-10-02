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

            int leftSidebarDock = HoverTooltipDrawing.GetHorizontalPositionAvoidingTarget(
                defaultX: 50,
                boxW: 20,
                innerLeft: 35,
                innerRightInclusive: 99,
                targetX: 3,
                targetWidth: 28);
            TestBase.AssertEqual(35, leftSidebarDock, "left-panel target docks tooltip to inner left", ref run, ref passed, ref failed);

            int rightSidebarDock = HoverTooltipDrawing.GetHorizontalPositionAvoidingTarget(
                defaultX: 50,
                boxW: 20,
                innerLeft: 10,
                innerRightInclusive: 40,
                targetX: 50,
                targetWidth: 20);
            TestBase.AssertEqual(21, rightSidebarDock, "right-of-band target docks tooltip to inner right", ref run, ref passed, ref failed);

            TestBase.AssertEqual(40, HoverTooltipDrawing.GetVerticalPositionNearTarget(40, 6, 12, 50),
                "tooltip top follows hovered row", ref run, ref passed, ref failed);
            TestBase.AssertEqual(12, HoverTooltipDrawing.GetVerticalPositionNearTarget(5, 6, 12, 50),
                "tooltip clamps up to inner top", ref run, ref passed, ref failed);
            TestBase.AssertEqual(41, HoverTooltipDrawing.GetVerticalPositionNearTarget(48, 10, 12, 50),
                "tooltip clamps down so the box stays in band", ref run, ref passed, ref failed);

            var untouched = HoverTooltipDrawing.MaskTextRunOutsideRange(0, "ABCDEF", 10, 20);
            TestBase.AssertEqual(1, untouched.Count, "run fully left of mask stays", ref run, ref passed, ref failed);
            TestBase.AssertEqual("ABCDEF", untouched[0].content, "left-of-mask content unchanged", ref run, ref passed, ref failed);

            var covered = HoverTooltipDrawing.MaskTextRunOutsideRange(10, "HIJK", 10, 20);
            TestBase.AssertEqual(0, covered.Count, "run fully inside mask is dropped", ref run, ref passed, ref failed);

            var spanning = HoverTooltipDrawing.MaskTextRunOutsideRange(8, "0123456789", 10, 14);
            TestBase.AssertEqual(2, spanning.Count, "spanning run splits into left+right", ref run, ref passed, ref failed);
            TestBase.AssertEqual(8, spanning[0].x, "left remnant keeps origin", ref run, ref passed, ref failed);
            TestBase.AssertEqual("01", spanning[0].content, "left remnant is prefix before mask", ref run, ref passed, ref failed);
            TestBase.AssertEqual(14, spanning[1].x, "right remnant starts at mask end", ref run, ref passed, ref failed);
            TestBase.AssertEqual("6789", spanning[1].content, "right remnant is suffix after mask", ref run, ref passed, ref failed);

            TestBase.PrintSummary("HoverTooltipDrawing Tests", run, passed, failed);
        }
    }
}
