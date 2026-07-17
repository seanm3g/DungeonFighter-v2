using System;
using Avalonia;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Helpers;

namespace RPGGame.Tests.Unit.UI
{
    public static class FlavorTextWindowPlacementTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;

            Compute_CentersMainAndUsesSideColumns(ref run, ref passed, ref failed);
            Compute_FitsWithinWorkArea(ref run, ref passed, ref failed);
            Compute_ScalesWithLargerMonitor(ref run, ref passed, ref failed);
            GetPlacement_MapsHostNames(ref run, ref passed, ref failed);

            TestBase.PrintSummary("FlavorTextWindowPlacementTests", run, passed, failed);
        }

        private static void Compute_CentersMainAndUsesSideColumns(ref int run, ref int passed, ref int failed)
        {
            var wa = new PixelRect(new PixelPoint(0, 0), new PixelSize(1920, 1080));
            var layout = FlavorTextWindowPlacement.Compute(wa);

            TestBase.AssertTrue(layout.Forms.Position.X < layout.MainPosition.X, "Forms left of main", ref run, ref passed, ref failed);
            TestBase.AssertTrue(layout.Template.Position.X > layout.MainPosition.X, "Template right of main", ref run, ref passed, ref failed);
            TestBase.AssertTrue(layout.Categories.Position.Y > layout.Forms.Position.Y, "Categories below Forms", ref run, ref passed, ref failed);
            TestBase.AssertTrue(layout.Legacy.Position.Y > layout.Template.Position.Y, "Legacy below Template", ref run, ref passed, ref failed);

            int mainCenterX = layout.MainPosition.X + layout.MainSize.Width / 2;
            int workCenterX = wa.X + wa.Width / 2;
            TestBase.AssertTrue(Math.Abs(mainCenterX - workCenterX) <= FlavorTextWindowPlacement.EdgeMarginPixels + 2,
                "main roughly horizontally centered", ref run, ref passed, ref failed);
        }

        private static void Compute_FitsWithinWorkArea(ref int run, ref int passed, ref int failed)
        {
            var wa = new PixelRect(new PixelPoint(100, 50), new PixelSize(1280, 720));
            var layout = FlavorTextWindowPlacement.Compute(wa);

            AssertInside(wa, layout.MainPosition, layout.MainSize, "main", ref run, ref passed, ref failed);
            AssertInside(wa, layout.Forms.Position, layout.Forms.Size, "forms", ref run, ref passed, ref failed);
            AssertInside(wa, layout.Template.Position, layout.Template.Size, "template", ref run, ref passed, ref failed);
            AssertInside(wa, layout.Categories.Position, layout.Categories.Size, "categories", ref run, ref passed, ref failed);
            AssertInside(wa, layout.Legacy.Position, layout.Legacy.Size, "legacy", ref run, ref passed, ref failed);
        }

        private static void Compute_ScalesWithLargerMonitor(ref int run, ref int passed, ref int failed)
        {
            var small = FlavorTextWindowPlacement.Compute(new PixelRect(new PixelPoint(0, 0), new PixelSize(1280, 720)));
            var large = FlavorTextWindowPlacement.Compute(new PixelRect(new PixelPoint(0, 0), new PixelSize(2560, 1440)));

            TestBase.AssertTrue(large.MainSize.Width > small.MainSize.Width, "main wider on larger monitor", ref run, ref passed, ref failed);
            TestBase.AssertTrue(large.Forms.Size.Width > small.Forms.Size.Width, "side wider on larger monitor", ref run, ref passed, ref failed);
            TestBase.AssertTrue(large.Forms.Size.Height > small.Forms.Size.Height, "side taller on larger monitor", ref run, ref passed, ref failed);
        }

        private static void GetPlacement_MapsHostNames(ref int run, ref int passed, ref int failed)
        {
            var layout = FlavorTextWindowPlacement.Compute(new PixelRect(new PixelPoint(0, 0), new PixelSize(1920, 1080)));
            TestBase.AssertEqual(layout.Forms.Position.X, FlavorTextWindowPlacement.GetPlacement(layout, "FlavorTextFormsHost").Position.X, "forms host", ref run, ref passed, ref failed);
            TestBase.AssertEqual(layout.Template.Position.X, FlavorTextWindowPlacement.GetPlacement(layout, "FlavorTextTemplateHost").Position.X, "template host", ref run, ref passed, ref failed);
            TestBase.AssertEqual(layout.Categories.Position.Y, FlavorTextWindowPlacement.GetPlacement(layout, "FlavorTextCategoriesHost").Position.Y, "categories host", ref run, ref passed, ref failed);
            TestBase.AssertEqual(layout.Legacy.Position.Y, FlavorTextWindowPlacement.GetPlacement(layout, "FlavorTextLegacyHost").Position.Y, "legacy host", ref run, ref passed, ref failed);
        }

        private static void AssertInside(
            PixelRect wa,
            PixelPoint pos,
            PixelSize size,
            string label,
            ref int run,
            ref int passed,
            ref int failed)
        {
            TestBase.AssertTrue(pos.X >= wa.X, $"{label} X >= work left", ref run, ref passed, ref failed);
            TestBase.AssertTrue(pos.Y >= wa.Y, $"{label} Y >= work top", ref run, ref passed, ref failed);
            TestBase.AssertTrue(pos.X + size.Width <= wa.X + wa.Width + 1, $"{label} right within work", ref run, ref passed, ref failed);
            TestBase.AssertTrue(pos.Y + size.Height <= wa.Y + wa.Height + 1, $"{label} bottom within work", ref run, ref passed, ref failed);
        }
    }
}
