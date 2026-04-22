using Avalonia;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Helpers;

namespace RPGGame.Tests.Unit.UI
{
    public static class ActionLabWindowPlacementTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;

            RightAnchor_UsesWorkingAreaRightAndMargin(ref run, ref passed, ref failed);
            RightAnchor_ClampsWhenWiderThanWorkArea(ref run, ref passed, ref failed);
            Centered_FitsWithinWorkArea(ref run, ref passed, ref failed);
            LeftAnchor_RespectsMargin(ref run, ref passed, ref failed);

            TestBase.PrintSummary("ActionLabWindowPlacementTests", run, passed, failed);
        }

        private static void RightAnchor_UsesWorkingAreaRightAndMargin(ref int run, ref int passed, ref int failed)
        {
            var wa = new PixelRect(new PixelPoint(100, 50), new PixelSize(800, 600));
            var p = ActionLabWindowPlacement.ComputeRightAnchoredTopLeft(wa, 560, 1200, 10);
            // x = 100 + 800 - 560 - 10 = 330
            TestBase.AssertEqual(330, p.X, "right-anchored X", ref run, ref passed, ref failed);
            // y: window taller than work area -> top of work area
            TestBase.AssertEqual(50, p.Y, "right-anchored Y when window taller than work area", ref run, ref passed, ref failed);
        }

        private static void RightAnchor_ClampsWhenWiderThanWorkArea(ref int run, ref int passed, ref int failed)
        {
            var wa = new PixelRect(new PixelPoint(0, 0), new PixelSize(400, 300));
            var p = ActionLabWindowPlacement.ComputeRightAnchoredTopLeft(wa, 900, 200, 10);
            TestBase.AssertEqual(0, p.X, "right-anchored X clamps to work area left", ref run, ref passed, ref failed);
        }

        private static void Centered_FitsWithinWorkArea(ref int run, ref int passed, ref int failed)
        {
            var wa = new PixelRect(new PixelPoint(10, 20), new PixelSize(1000, 800));
            var p = ActionLabWindowPlacement.ComputeCenteredTopLeft(wa, 400, 300);
            TestBase.AssertEqual(10 + (1000 - 400) / 2, p.X, "centered X", ref run, ref passed, ref failed);
            TestBase.AssertEqual(20 + (800 - 300) / 2, p.Y, "centered Y", ref run, ref passed, ref failed);
        }

        private static void LeftAnchor_RespectsMargin(ref int run, ref int passed, ref int failed)
        {
            var wa = new PixelRect(new PixelPoint(5, 5), new PixelSize(1920, 1080));
            var p = ActionLabWindowPlacement.ComputeLeftAnchoredTopLeft(wa, 800, 400, 12);
            TestBase.AssertEqual(5 + 12, p.X, "left-anchored X", ref run, ref passed, ref failed);
            TestBase.AssertEqual(5 + (1080 - 400) / 2, p.Y, "left-anchored Y", ref run, ref passed, ref failed);
        }
    }
}
