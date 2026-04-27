using System.Linq;
using Avalonia.Media;
using RPGGame;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Renderers.Helpers;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Tests for <see cref="ItemStatFormatter.FormatStatLine"/> weapon speed coloring.
    /// </summary>
    public static class ItemStatFormatterTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== ItemStatFormatter Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestSpeedFasterThanBaselineGreen();
            TestSpeedSlowerThanBaselineRed();
            TestSpeedEqualBaselineWhite();
            TestSpeedNoBaselineWhite();
            TestSpeedNonWeaponDisplayedStaysWhite();

            TestBase.PrintSummary("ItemStatFormatter Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static Color ExpectedPaletteColor(ColorPalette palette) =>
            new ColoredText("x", palette.GetColor()).Color;

        private static ColoredText? FindSpeedValueSegment(System.Collections.Generic.List<ColoredText> segments)
        {
            return segments.FirstOrDefault(s => s.Text.Contains('×'));
        }

        private static void TestSpeedFasterThanBaselineGreen()
        {
            _testsRun++;
            Console.WriteLine("--- TestSpeedFasterThanBaselineGreen ---");
            var faster = new WeaponItem("Fast", 1, 10, 0.5, WeaponType.Sword);
            var slower = new WeaponItem("Slow", 1, 10, 1.0, WeaponType.Sword);
            string stat = $"Speed: {faster.GetTotalAttackSpeed():F2}×";
            var segments = ItemStatFormatter.FormatStatLine(stat, faster, slower);
            var valueSeg = FindSpeedValueSegment(segments);
            var ok = valueSeg != null && valueSeg.Color == ExpectedPaletteColor(ColorPalette.Success);
            if (ok) _testsPassed++; else { _testsFailed++; Console.WriteLine("  FAIL: expected success color for faster weapon"); }
        }

        private static void TestSpeedSlowerThanBaselineRed()
        {
            _testsRun++;
            Console.WriteLine("--- TestSpeedSlowerThanBaselineRed ---");
            var faster = new WeaponItem("Fast", 1, 10, 0.5, WeaponType.Sword);
            var slower = new WeaponItem("Slow", 1, 10, 1.0, WeaponType.Sword);
            string stat = $"Speed: {slower.GetTotalAttackSpeed():F2}×";
            var segments = ItemStatFormatter.FormatStatLine(stat, slower, faster);
            var valueSeg = FindSpeedValueSegment(segments);
            var ok = valueSeg != null && valueSeg.Color == ExpectedPaletteColor(ColorPalette.Error);
            if (ok) _testsPassed++; else { _testsFailed++; Console.WriteLine("  FAIL: expected error color for slower weapon"); }
        }

        private static void TestSpeedEqualBaselineWhite()
        {
            _testsRun++;
            Console.WriteLine("--- TestSpeedEqualBaselineWhite ---");
            var a = new WeaponItem("A", 1, 10, 0.75, WeaponType.Sword);
            var b = new WeaponItem("B", 1, 8, 0.75, WeaponType.Dagger);
            string stat = $"Speed: {a.GetTotalAttackSpeed():F2}×";
            var segments = ItemStatFormatter.FormatStatLine(stat, a, b);
            var valueSeg = FindSpeedValueSegment(segments);
            var ok = valueSeg != null && valueSeg.Color == new ColoredText("x", Colors.White).Color;
            if (ok) _testsPassed++; else { _testsFailed++; Console.WriteLine("  FAIL: expected white when speeds match"); }
        }

        private static void TestSpeedNoBaselineWhite()
        {
            _testsRun++;
            Console.WriteLine("--- TestSpeedNoBaselineWhite ---");
            var w = new WeaponItem("W", 1, 10, 0.6, WeaponType.Sword);
            string stat = $"Speed: {w.GetTotalAttackSpeed():F2}×";
            var segments = ItemStatFormatter.FormatStatLine(stat, w, weaponSpeedBaseline: null);
            var valueSeg = FindSpeedValueSegment(segments);
            var ok = valueSeg != null && valueSeg.Color == new ColoredText("x", Colors.White).Color;
            if (ok) _testsPassed++; else { _testsFailed++; Console.WriteLine("  FAIL: expected white with no baseline"); }
        }

        private static void TestSpeedNonWeaponDisplayedStaysWhite()
        {
            _testsRun++;
            Console.WriteLine("--- TestSpeedNonWeaponDisplayedStaysWhite ---");
            var baseline = new WeaponItem("B", 1, 10, 1.0, WeaponType.Sword);
            var head = new HeadItem("H", 1, armor: 2);
            string stat = "Speed: 9.99×";
            var segments = ItemStatFormatter.FormatStatLine(stat, head, baseline);
            var valueSeg = FindSpeedValueSegment(segments);
            var ok = valueSeg != null && valueSeg.Color == new ColoredText("x", Colors.White).Color;
            if (ok) _testsPassed++; else { _testsFailed++; Console.WriteLine("  FAIL: non-weapon line should not apply weapon compare"); }
        }
    }
}
