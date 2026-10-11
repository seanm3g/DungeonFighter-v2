using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Canvas;
using RPGGame.UI.Avalonia.Effects;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.Tests.Unit.UI
{
    public static class ActionTextEmphasisWordLocatorTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionTextEmphasisWordLocator Tests ===\n");
            _run = _passed = _failed = 0;

            // Ensure center-panel hit tests have a sane grid.
            LayoutConstants.UpdateGridDimensions(100, 40);

            TestLocatesWordInCenterPanel();
            TestExcludePadsFlankingCells();
            TestIgnoresOverlayAndMissingWord();

            TestBase.PrintSummary("ActionTextEmphasisWordLocator Tests", _run, _passed, _failed);
        }

        private static void TestLocatesWordInCenterPanel()
        {
            Console.WriteLine("--- Locates SLAM in center panel ---");
            int x = LayoutConstants.CENTER_PANEL_X + 2;
            int y = LayoutConstants.CENTER_PANEL_Y + 5;
            var texts = new List<CanvasText>
            {
                new CanvasText { X = x, Y = y, Content = "answers with SLAM.", Color = Colors.White }
            };

            bool ok = ActionTextEmphasisWordLocator.TryLocate(
                texts, "SLAM", 10, 16,
                out double originX, out double originY,
                out double minX, out double maxX, out double minY, out double maxY);

            TestBase.AssertTrue(ok, "found SLAM", ref _run, ref _passed, ref _failed);
            int wordStart = "answers with ".Length;
            int pad = ActionTextEmphasisWordLocator.FlankingExcludeCells;
            double expectedMinX = (x + wordStart - pad) * 10.0;
            double expectedMaxX = (x + wordStart + 4 + pad) * 10.0;
            double expectedOriginX = (x + wordStart + 2) * 10.0; // center of 4-letter word
            TestBase.AssertTrue(Math.Abs(minX - expectedMinX) < 0.01, $"minX={minX}", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(Math.Abs(maxX - expectedMaxX) < 0.01, $"maxX={maxX}", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(Math.Abs(originX - expectedOriginX) < 0.01, $"originX={originX}", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(originX > minX && originX < maxX, "origin inside exclude X", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(originY > minY && originY < maxY, "origin inside exclude Y", ref _run, ref _passed, ref _failed);
        }

        private static void TestExcludePadsFlankingCells()
        {
            Console.WriteLine("\n--- Exclude pads flanking cells (colored SLAM run) ---");
            int x = LayoutConstants.CENTER_PANEL_X + 2;
            int y = LayoutConstants.CENTER_PANEL_Y + 6;
            // Mimic narrative paint: space and green action are separate CanvasText runs.
            var texts = new List<CanvasText>
            {
                new CanvasText { X = x, Y = y, Content = "A decisive ", Color = Colors.White },
                new CanvasText { X = x + "A decisive ".Length, Y = y, Content = "SLAM", Color = Colors.Lime },
                new CanvasText { X = x + "A decisive SLAM".Length, Y = y, Content = " cuts.", Color = Colors.White }
            };

            bool ok = ActionTextEmphasisWordLocator.TryLocate(
                texts, "SLAM", 10, 16,
                out _, out _,
                out double minX, out double maxX, out _, out _);

            TestBase.AssertTrue(ok, "found SLAM in split runs", ref _run, ref _passed, ref _failed);
            int wordGridX = x + "A decisive ".Length;
            int pad = ActionTextEmphasisWordLocator.FlankingExcludeCells;
            double expectedMinX = (wordGridX - pad) * 10.0;
            double expectedMaxX = (wordGridX + 4 + pad) * 10.0;
            TestBase.AssertTrue(Math.Abs(minX - expectedMinX) < 0.01,
                $"flanking pad left minX={minX} expected={expectedMinX}",
                ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(Math.Abs(maxX - expectedMaxX) < 0.01,
                $"flanking pad right maxX={maxX} expected={expectedMaxX}",
                ref _run, ref _passed, ref _failed);
        }

        private static void TestIgnoresOverlayAndMissingWord()
        {
            Console.WriteLine("\n--- Ignores overlay / missing ---");
            int x = LayoutConstants.CENTER_PANEL_X + 2;
            int y = LayoutConstants.CENTER_PANEL_Y + 5;
            var texts = new List<CanvasText>
            {
                new CanvasText
                {
                    X = x, Y = y, Content = "answers with SLAM.", Color = Colors.White, IsOverlay = true
                }
            };

            bool overlay = ActionTextEmphasisWordLocator.TryLocate(
                texts, "SLAM", 10, 16,
                out _, out _, out _, out _, out _, out _);
            TestBase.AssertTrue(!overlay, "overlay ignored", ref _run, ref _passed, ref _failed);

            texts[0].IsOverlay = false;
            texts[0].Content = "answers with STRIKE.";
            bool missing = ActionTextEmphasisWordLocator.TryLocate(
                texts, "SLAM", 10, 16,
                out _, out _, out _, out _, out _, out _);
            TestBase.AssertTrue(!missing, "missing word returns false", ref _run, ref _passed, ref _failed);
        }
    }
}
