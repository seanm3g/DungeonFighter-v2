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

            // Panel chrome clears must not wipe overlay boxes (hover tips).
            var manager6 = new CanvasElementManager();
            manager6.AddBox(new CanvasBox { X = 10, Y = 10, Width = 5, Height = 5, BorderColor = Colors.Purple, BackgroundColor = Colors.Black });
            manager6.AddBox(new CanvasBox
            {
                X = 12, Y = 12, Width = 1, Height = 1,
                BorderColor = Colors.White, BackgroundColor = Colors.White, IsOverlay = true
            });
            manager6.ClearBoxesInArea(8, 8, 20, 20);
            TestBase.AssertEqual(1, manager6.BoxElements.Count, "ClearBoxesInArea keeps overlay boxes", ref run, ref passed, ref failed);
            TestBase.AssertTrue(manager6.BoxElements[0].IsOverlay, "surviving box is the overlay", ref run, ref passed, ref failed);
            manager6.ClearOverlayBoxesInArea(8, 8, 20, 20);
            TestBase.AssertEqual(0, manager6.BoxElements.Count, "ClearOverlayBoxesInArea removes overlay boxes", ref run, ref passed, ref failed);

            TestRotateAboutPointKeepsCenter(ref run, ref passed, ref failed);
            TestSnapPixelFontOffset(ref run, ref passed, ref failed);
            TestPixelFontsForceMonospaceGlyphPlacement(ref run, ref passed, ref failed);
            TestGlyphStringReusesAsciiInstances(ref run, ref passed, ref failed);
            TestDistortionAppliesWhenPulseActiveWithoutWind(ref run, ref passed, ref failed);
            TestLengthOneCellsDoNotMerge(ref run, ref passed, ref failed);

            TestBase.PrintSummary("CanvasPrimitiveStackingTests", run, passed, failed);
        }

        /// <summary>
        /// Hover-recolored dungeon rows add one glyph per cell in the same color. Merging those
        /// into one run used center-zoom glyphAdvance and jumped letter spacing vs idle.
        /// </summary>
        private static void TestLengthOneCellsDoNotMerge(ref int run, ref int passed, ref int failed)
        {
            Console.WriteLine("--- Length-1 cells do not adjacent-merge ---");

            TestBase.AssertTrue(
                CanvasElementManager.ShouldKeepAsOwnCell("H"),
                "single char stays its own cell",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                !CanvasElementManager.ShouldKeepAsOwnCell("Hi"),
                "multi-char may still merge",
                ref run, ref passed, ref failed);

            var yellowRow = new CanvasElementManager();
            // Length-1 adds must not collapse when the whole hover row shares one color.
            yellowRow.AddTextWithMerging(4, 8, "A", Colors.Yellow);
            yellowRow.AddTextWithMerging(5, 8, "B", Colors.Yellow);
            yellowRow.AddTextWithMerging(6, 8, "C", Colors.Yellow);
            yellowRow.AddTextWithMerging(7, 8, "D", Colors.Yellow);
            yellowRow.AddTextWithMerging(8, 8, "E", Colors.Yellow);
            TestBase.AssertEqual(5, yellowRow.TextElements.Count,
                "same-color length-1 neighbors stay separate cells (hover yellow row)",
                ref run, ref passed, ref failed);

            var multi = new CanvasElementManager();
            multi.AddTextWithMerging(4, 8, "Hi", Colors.White);
            multi.AddTextWithMerging(6, 8, "ya", Colors.White);
            TestBase.AssertEqual(1, multi.TextElements.Count,
                "multi-char same-color neighbors still merge",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual("Hiya", multi.TextElements[0].Content,
                "merged multi-char content",
                ref run, ref passed, ref failed);
        }

        /// <summary>
        /// VT323 idle paint must use the same grid advances as the mouse-wake path;
        /// otherwise FormattedText's tighter natural advances look like a kerning jump.
        /// </summary>
        private static void TestPixelFontsForceMonospaceGlyphPlacement(
            ref int run, ref int passed, ref int failed)
        {
            Console.WriteLine("--- Pixel fonts force monospace glyph placement ---");

            TestBase.AssertTrue(
                CanvasPrimitivesRenderer.ShouldForceMonospaceGlyphPlacement(usesPixelGrid: true),
                "pixel fonts force grid advances",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                !CanvasPrimitivesRenderer.ShouldForceMonospaceGlyphPlacement(usesPixelGrid: false),
                "Courier keeps whole-string FormattedText advances",
                ref run, ref passed, ref failed);

            const double baseX = 100;
            const double advance = 10;
            TestBase.AssertTrue(
                Math.Abs(CanvasPrimitivesRenderer.MonospaceGlyphOriginX(baseX, 0, advance) - 100) < 1e-9,
                "glyph 0 stays at base X",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                Math.Abs(CanvasPrimitivesRenderer.MonospaceGlyphOriginX(baseX, 3, advance) - 130) < 1e-9,
                "glyph 3 is base + 3*advance (same idle and wake)",
                ref run, ref passed, ref failed);
        }

        private static void TestGlyphStringReusesAsciiInstances(ref int run, ref int passed, ref int failed)
        {
            Console.WriteLine("--- GlyphString reuses ASCII instances ---");
            string a = CanvasPrimitivesRenderer.GlyphString('A');
            string b = CanvasPrimitivesRenderer.GlyphString('A');
            TestBase.AssertTrue(ReferenceEquals(a, b),
                "ASCII glyph strings are stable for FormattedText cache keys",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual("A", a, "GlyphString content", ref run, ref passed, ref failed);
        }

        /// <summary>
        /// VT323 / pixel fonts must snap sway offsets to whole pixels; half-pixel values
        /// flip Math.Round on draw origins and make letters look like they are jumping.
        /// </summary>
        private static void TestSnapPixelFontOffset(ref int run, ref int passed, ref int failed)
        {
            Console.WriteLine("--- Pixel-font offset snap keeps whole pixels ---");

            var (px, py) = CanvasPrimitivesRenderer.SnapPixelFontOffset(10.4, 20.6, usesPixelGrid: true);
            TestBase.AssertTrue(Math.Abs(px - 10) < 1e-9 && Math.Abs(py - 21) < 1e-9,
                $"pixel grid snaps (10.4,20.6) → (10,21) got ({px},{py})",
                ref run, ref passed, ref failed);

            var (cx, cy) = CanvasPrimitivesRenderer.SnapPixelFontOffset(10.4, 20.6, usesPixelGrid: false);
            TestBase.AssertTrue(Math.Abs(cx - 10.4) < 1e-9 && Math.Abs(cy - 20.6) < 1e-9,
                $"non-pixel fonts keep fractional offsets (got ({cx},{cy}))",
                ref run, ref passed, ref failed);

            var (zx, zy) = CanvasPrimitivesRenderer.SnapPixelFontOffset(0.49, -0.49, usesPixelGrid: true);
            TestBase.AssertTrue(Math.Abs(zx) < 1e-9 && Math.Abs(zy) < 1e-9,
                $"sub-half-pixel sway snaps to rest (got ({zx},{zy}))",
                ref run, ref passed, ref failed);
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

        private static void TestDistortionAppliesWhenPulseActiveWithoutWind(
            ref int run, ref int passed, ref int failed)
        {
            Console.WriteLine("--- Distortion applies for emphasis pulse without wind ---");
            TestBase.AssertTrue(
                !CanvasPrimitivesRenderer.DistortionAppliesToBodyText(
                    isOverlay: false, burstActive: false, pulseActive: false, windApplies: false),
                "idle body text is not distorted",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                CanvasPrimitivesRenderer.DistortionAppliesToBodyText(
                    isOverlay: false, burstActive: false, pulseActive: true, windApplies: false),
                "pulse distorts combat-log body text while wind is idle",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                !CanvasPrimitivesRenderer.DistortionAppliesToBodyText(
                    isOverlay: true, burstActive: false, pulseActive: true, windApplies: true),
                "overlay text skips distortion",
                ref run, ref passed, ref failed);
        }
    }
}
