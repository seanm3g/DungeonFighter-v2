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

            var manager6 = new CanvasElementManager();
            manager6.AddLine(new CanvasLine { X1 = 10, Y1 = 10, X2 = 20, Y2 = 18, Color = Colors.Red, ThicknessPixels = 4 });
            manager6.AddLine(new CanvasLine { X1 = 80, Y1 = 2, X2 = 90, Y2 = 4, Color = Colors.White, ThicknessPixels = 2 });
            manager6.ClearLinesInArea(8, 8, 16, 12);
            TestBase.AssertEqual(1, manager6.LineElements.Count, "ClearLinesInArea removes strokes in the rect", ref run, ref passed, ref failed);
            TestBase.AssertEqual(80, manager6.LineElements[0].X1, "ClearLinesInArea keeps strokes outside the rect", ref run, ref passed, ref failed);
            manager6.Clear();
            TestBase.AssertEqual(0, manager6.LineElements.Count, "Clear removes leftover line strokes", ref run, ref passed, ref failed);

            TestBase.PrintSummary("CanvasPrimitiveStackingTests", run, passed, failed);
        }
    }
}
