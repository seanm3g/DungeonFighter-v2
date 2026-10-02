using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.Tests;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Effects;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Tests for wake-proximity brighten of clickable canvas text.
    /// </summary>
    public static class InteractiveTextHighlightTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== InteractiveTextHighlight Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestBuildInteractiveCells();
            TestTextOverlapsInteractive();
            TestFingerprintClickableRegions();
            TestApplyBrightenIdle();
            TestApplyBrightenFull();
            TestShouldApplyProximityGlowSkipsSaturated();
            TestProximityInfluenceNearStrongerThanFar();
            TestProximityInfluenceZeroOutsideWake();
            TestProximityInfluenceZeroWithoutMouse();
            TestProximityDisabledByConfig();

            TestBase.PrintSummary("InteractiveTextHighlight Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestBuildInteractiveCells()
        {
            Console.WriteLine("--- BuildInteractiveCells covers width×height ---");
            var elements = new List<ClickableElement>
            {
                new ClickableElement { X = 10, Y = 5, Width = 3, Height = 2, Value = "1" }
            };
            var cells = InteractiveTextHighlight.BuildInteractiveCells(elements);
            TestBase.AssertTrue(cells.Count == 6, "3×2 cells", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(cells.Contains(InteractiveTextHighlight.PackCell(10, 5)), "includes (10,5)", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(cells.Contains(InteractiveTextHighlight.PackCell(12, 6)), "includes (12,6)", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!cells.Contains(InteractiveTextHighlight.PackCell(13, 5)), "excludes (13,5)", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestTextOverlapsInteractive()
        {
            Console.WriteLine("--- TextOverlapsInteractive ---");
            var cells = new HashSet<long> { InteractiveTextHighlight.PackCell(4, 2) };
            TestBase.AssertTrue(
                InteractiveTextHighlight.TextOverlapsInteractive(2, 2, 4, cells),
                "run covering cell overlaps",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                !InteractiveTextHighlight.TextOverlapsInteractive(0, 2, 3, cells),
                "run missing cell does not overlap",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                !InteractiveTextHighlight.TextOverlapsInteractive(4, 2, 1, null),
                "null cells never overlap",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestFingerprintClickableRegions()
        {
            Console.WriteLine("--- FingerprintClickableRegions ---");
            var a = new List<ClickableElement>
            {
                new ClickableElement { X = 1, Y = 2, Width = 3, Height = 1, Value = "a" }
            };
            var b = new List<ClickableElement>
            {
                new ClickableElement { X = 1, Y = 2, Width = 3, Height = 1, Value = "different" }
            };
            var c = new List<ClickableElement>
            {
                new ClickableElement { X = 1, Y = 2, Width = 4, Height = 1, Value = "a" }
            };
            int ha = InteractiveTextHighlight.FingerprintClickableRegions(a);
            int hb = InteractiveTextHighlight.FingerprintClickableRegions(b);
            int hc = InteractiveTextHighlight.FingerprintClickableRegions(c);
            TestBase.AssertEqual(ha, hb, "same bounds ignore Value", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(ha != hc, "width change changes fingerprint", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, InteractiveTextHighlight.FingerprintClickableRegions(null),
                "null → 0", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestApplyBrightenIdle()
        {
            Console.WriteLine("--- ApplyBrighten influence 0 returns same color ---");
            var c = Color.FromRgb(80, 80, 80);
            var outC = InteractiveTextHighlight.ApplyBrighten(c, 0, 0.55);
            TestBase.AssertTrue(outC == c, "unchanged at 0 influence", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestApplyBrightenFull()
        {
            Console.WriteLine("--- ApplyBrighten moves toward cool-white ---");
            var c = Color.FromRgb(40, 40, 40);
            var outC = InteractiveTextHighlight.ApplyBrighten(c, 1.0, 1.0);
            TestBase.AssertTrue(outC.R > c.R && outC.G > c.G && outC.B > c.B,
                "brighter channels", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(outC.B >= outC.R, "cool bias (B >= R)", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestShouldApplyProximityGlowSkipsSaturated()
        {
            Console.WriteLine("--- ShouldApplyProximityGlow skips saturated hues ---");
            TestBase.AssertTrue(
                InteractiveTextHighlight.ShouldApplyProximityGlow(Color.FromRgb(240, 240, 240)),
                "near-white allows glow",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                !InteractiveTextHighlight.ShouldApplyProximityGlow(Color.FromRgb(220, 40, 40)),
                "primary STR red skips glow",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                !InteractiveTextHighlight.ShouldApplyProximityGlow(Color.FromRgb(220, 180, 40)),
                "gold header skips glow",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static WindSwayConfig ProximityCfg() => new WindSwayConfig
        {
            Enabled = true,
            InteractiveHighlightEnabled = true,
            WakeRadiusCells = 10,
            NearInfluence = 1.0,
            FarInfluence = 0.2,
            WakeFalloffPower = 1.0,
            WakeRearBias = 0
        };

        private static void TestProximityInfluenceNearStrongerThanFar()
        {
            Console.WriteLine("--- SampleProximityInfluence near > far ---");
            var field = new WindSwayField();
            field.ApplyConfig(ProximityCfg());
            const double cw = 8, ch = 12;
            // Glyph at (5,5) cell center ≈ (44, 66)
            field.TrackMousePosition(44, 66);
            double near = field.SampleProximityInfluence(5, 5, 0, cw, ch);
            field.TrackMousePosition(44 + 7 * cw, 66);
            double far = field.SampleProximityInfluence(5, 5, 0, cw, ch);
            TestBase.AssertTrue(near > 0.5, $"near strong ({near:F2})", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(far > 0 && far < near, $"far weaker ({far:F2} < {near:F2})", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestProximityInfluenceZeroOutsideWake()
        {
            Console.WriteLine("--- SampleProximityInfluence outside wake is 0 ---");
            var field = new WindSwayField();
            field.ApplyConfig(ProximityCfg());
            const double cw = 8, ch = 12;
            field.TrackMousePosition(44, 66);
            double outside = field.SampleProximityInfluence(5 + 20, 5, 0, cw, ch);
            TestBase.AssertTrue(outside <= 1e-6, $"outside zero ({outside})", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestProximityInfluenceZeroWithoutMouse()
        {
            Console.WriteLine("--- SampleProximityInfluence without mouse is 0 ---");
            var field = new WindSwayField();
            field.ApplyConfig(ProximityCfg());
            field.Reset();
            double v = field.SampleProximityInfluence(5, 5, 0, 8, 12);
            TestBase.AssertTrue(v <= 1e-6, "no mouse → 0", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestProximityDisabledByConfig()
        {
            Console.WriteLine("--- SampleProximityInfluence respects InteractiveHighlightEnabled ---");
            var field = new WindSwayField();
            var cfg = ProximityCfg();
            cfg.InteractiveHighlightEnabled = false;
            field.ApplyConfig(cfg);
            field.TrackMousePosition(44, 66);
            double v = field.SampleProximityInfluence(5, 5, 0, 8, 12);
            TestBase.AssertTrue(v <= 1e-6, "disabled → 0", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
