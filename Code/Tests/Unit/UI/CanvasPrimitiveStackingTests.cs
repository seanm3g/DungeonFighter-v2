using System;
using Avalonia;
using Avalonia.Media;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Canvas;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Documents fullscreen ghosting: a second layout pass without <see cref="GameCanvasControl.Clear"/>
    /// leaves two text elements when grid coordinates differ between passes.
    /// </summary>
    public static class CanvasPrimitiveStackingTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;

            var manager = new CanvasElementManager();
            manager.AddTextWithMerging(5, 10, "a", Colors.White);
            manager.AddTextWithMerging(5, 10, "b", Colors.White);
            TestBase.AssertEqual(1, manager.TextElements.Count, "same cell merge/replace keeps one element", ref run, ref passed, ref failed);

            var manager2 = new CanvasElementManager();
            manager2.AddTextWithMerging(5, 10, "a", Colors.White);
            // Not adjacent to x=5 (merger would combine); simulates second layout pass at shifted column
            manager2.AddTextWithMerging(20, 10, "b", Colors.White);
            TestBase.AssertEqual(2, manager2.TextElements.Count, "non-adjacent cells stack (ghosting if both drawn)", ref run, ref passed, ref failed);

            var manager3 = new CanvasElementManager();
            manager3.AddOverlayText(3, 4, "overlay", Colors.Yellow);
            TestBase.AssertEqual(1, manager3.TextElements.Count, "AddOverlayText adds one element", ref run, ref passed, ref failed);
            TestBase.AssertTrue(manager3.TextElements[0].IsOverlay, "AddOverlayText sets IsOverlay", ref run, ref passed, ref failed);
            manager3.AddOverlayText(3, 4, "replaced", Colors.White);
            TestBase.AssertEqual(1, manager3.TextElements.Count, "AddOverlayText replaces prior overlay at same cell", ref run, ref passed, ref failed);
            TestBase.AssertEqual("replaced", manager3.TextElements[0].Content, "overlay content updated", ref run, ref passed, ref failed);

            var manager4 = new CanvasElementManager();
            manager4.AddOverlayText(10, 5, "a", Colors.White);
            manager4.AddOverlayText(10, 12, "b", Colors.White);
            manager4.AddTextWithMerging(10, 8, "body", Colors.Gray);
            manager4.ClearOverlayTextInArea(9, 4, 5, 12);
            TestBase.AssertEqual(1, manager4.TextElements.Count, "ClearOverlayTextInArea removes overlay rows, keeps body", ref run, ref passed, ref failed);
            TestBase.AssertEqual("body", manager4.TextElements[0].Content, "non-overlay text remains", ref run, ref passed, ref failed);

            var manager5 = new CanvasElementManager();
            manager5.AddBox(new CanvasBox { X = 1, Y = 2, Width = 3, Height = 4, BorderColor = Colors.Cyan, BackgroundColor = Colors.Black });
            bool boxUpdated = manager5.TryUpdateBox(1, 2, 3, 4, Colors.Cyan, Colors.DarkBlue);
            TestBase.AssertTrue(boxUpdated, "TryUpdateBox updates an existing box", ref run, ref passed, ref failed);
            TestBase.AssertEqual(1, manager5.BoxElements.Count, "TryUpdateBox does not add duplicate boxes", ref run, ref passed, ref failed);
            TestBase.AssertEqual(Colors.DarkBlue, manager5.BoxElements[0].BackgroundColor, "TryUpdateBox changes background color", ref run, ref passed, ref failed);

            TestRotateAboutPointKeepsCenter(ref run, ref passed, ref failed);

            TestBase.PrintSummary("CanvasPrimitiveStackingTests", run, passed, failed);
        }

        /// <summary>
        /// Burst glyph spin must use T(-C)*R*T(C). The column-vector order T(C)*R*T(-C)
        /// rotates around world (0,0) and flings letters off-screen.
        /// </summary>
        private static void TestRotateAboutPointKeepsCenter(ref int run, ref int passed, ref int failed)
        {
            Console.WriteLine("--- Rotate-about-point keeps glyph center (Avalonia row-vector) ---");
            const double cx = 420;
            const double cy = 260;
            double rotation = Math.PI * 0.75;

            var wrong = Matrix.CreateTranslation(cx, cy)
                        * Matrix.CreateRotation(rotation)
                        * Matrix.CreateTranslation(-cx, -cy);
            var right = CanvasPrimitivesRenderer.CreateRotateAboutPointTransform(cx, cy, rotation);

            var centerWrong = wrong.Transform(new Point(cx, cy));
            var centerRight = right.Transform(new Point(cx, cy));
            TestBase.AssertTrue(
                Math.Abs(centerWrong.X - cx) > 1.0 || Math.Abs(centerWrong.Y - cy) > 1.0,
                $"legacy T(C)*R*T(-C) must move the glyph center (got {centerWrong})",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                Math.Abs(centerRight.X - cx) < 1e-6 && Math.Abs(centerRight.Y - cy) < 1e-6,
                $"T(-C)*R*T(C) must keep glyph center fixed (got {centerRight})",
                ref run, ref passed, ref failed);

            // A point one unit right of center should stay one unit from center after rotate.
            var tip = right.Transform(new Point(cx + 10, cy));
            double dist = Math.Sqrt((tip.X - cx) * (tip.X - cx) + (tip.Y - cy) * (tip.Y - cy));
            TestBase.AssertTrue(Math.Abs(dist - 10) < 1e-6,
                $"rotated tip must stay 10px from center (got dist={dist}, tip={tip})",
                ref run, ref passed, ref failed);
        }
    }
}
