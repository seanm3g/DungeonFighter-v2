using System;
using Avalonia.Media;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Canvas;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Ensures the ASCII canvas font presets cycle correctly.
    /// </summary>
    public static class GameFontsTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("\n=== GameFonts Tests ===");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestVt323FamilyUri();
            TestNoplatoMonoFamilyUri();
            TestPixelzoneFamilyUri();
            TestBytesizedFamilyUri();
            TestFontCycleOrderAndWeights();
            TestCanvasTypefaceFollowsActivePreset();
            TestPerFontZoomIsIndependentAndClamped();
            TestCaptureAndApplyPreferencesRoundTrip();
            TestResetAndSetDefaultZoomPerFont();

            Console.WriteLine($"\nGameFonts: {_testsPassed}/{_testsRun} passed, {_testsFailed} failed");
        }

        private static void TestVt323FamilyUri()
        {
            Console.WriteLine("\n--- VT323 family URI targets embedded Regular ---");

            TestBase.AssertTrue(
                GameFonts.Vt323FamilyName.Contains("VT323-Regular.ttf", StringComparison.Ordinal),
                "Vt323FamilyName should reference the embedded VT323-Regular.ttf asset",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                GameFonts.Vt323FamilyName.Contains("#VT323", StringComparison.Ordinal),
                "Vt323FamilyName should select the VT323 family from the TTF",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                GameFonts.PrimaryFamilyName == GameFonts.Vt323FamilyName,
                "PrimaryFamilyName remains the VT323 URI alias",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestNoplatoMonoFamilyUri()
        {
            Console.WriteLine("\n--- Noplato Mono family URI targets embedded Regular ---");

            TestBase.AssertTrue(
                GameFonts.NoplatoMonoFamilyName.Contains(
                    "NoplatoMono.ttf", StringComparison.Ordinal),
                "NoplatoMonoFamilyName should reference the embedded TTF",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                GameFonts.NoplatoMonoFamilyName.Contains(
                    "#Noplato Demo Mono", StringComparison.Ordinal),
                "URI fragment should use the TTF family name (Noplato Demo Mono)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestPixelzoneFamilyUri()
        {
            Console.WriteLine("\n--- Pixelzone family URI targets embedded Regular ---");

            TestBase.AssertTrue(
                GameFonts.PixelzoneFamilyName.Contains(
                    "Pixelzone.ttf", StringComparison.Ordinal),
                "PixelzoneFamilyName should reference the embedded TTF",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                GameFonts.PixelzoneFamilyName.Contains(
                    "#Pixelzone", StringComparison.Ordinal),
                "URI fragment should select the Pixelzone family",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestBytesizedFamilyUri()
        {
            Console.WriteLine("\n--- Bytesized family URI targets embedded Regular ---");

            TestBase.AssertTrue(
                GameFonts.BytesizedFamilyName.Contains("Bytesized-Regular.ttf", StringComparison.Ordinal),
                "BytesizedFamilyName should reference the embedded TTF",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                GameFonts.BytesizedFamilyName.Contains("#Bytesized", StringComparison.Ordinal),
                "URI fragment should select the Bytesized family",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestFontCycleOrderAndWeights()
        {
            Console.WriteLine("\n--- F3 cycle order and bold-when-available weights ---");

            var previous = GameFonts.ActivePreset;
            try
            {
                GameFonts.SetPreset(GameFonts.Preset.Vt323);
                TestBase.AssertEqualEnum(GameFonts.Preset.Vt323, GameFonts.ActivePreset,
                    "start at VT323", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(GameFonts.ActiveWeight == FontWeight.Normal,
                    "VT323 has no bold face", ref _testsRun, ref _testsPassed, ref _testsFailed);

                var next = GameFonts.Cycle();
                TestBase.AssertEqualEnum(GameFonts.Preset.NoplatoMono, next.Id,
                    "first cycle → Noplato Mono", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(GameFonts.ActiveWeight == FontWeight.Normal,
                    "Noplato Mono has no bold face", ref _testsRun, ref _testsPassed, ref _testsFailed);

                next = GameFonts.Cycle();
                TestBase.AssertEqualEnum(GameFonts.Preset.Pixelzone, next.Id,
                    "second cycle → Pixelzone", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(GameFonts.ActiveWeight == FontWeight.Normal,
                    "Pixelzone has no bold face", ref _testsRun, ref _testsPassed, ref _testsFailed);

                next = GameFonts.Cycle();
                TestBase.AssertEqualEnum(GameFonts.Preset.Bytesized, next.Id,
                    "third cycle → Bytesized", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(GameFonts.ActiveWeight == FontWeight.Normal,
                    "Bytesized has no bold face", ref _testsRun, ref _testsPassed, ref _testsFailed);

                next = GameFonts.Cycle();
                TestBase.AssertEqualEnum(GameFonts.Preset.CourierNew, next.Id,
                    "fourth cycle → Courier New", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(GameFonts.ActiveWeight == FontWeight.Bold,
                    "Courier New uses Bold", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(
                    GameFonts.ActiveFamilyName.Contains("Courier New", StringComparison.Ordinal),
                    "Courier New family name", ref _testsRun, ref _testsPassed, ref _testsFailed);

                next = GameFonts.Cycle();
                TestBase.AssertEqualEnum(GameFonts.Preset.Vt323, next.Id,
                    "fifth cycle wraps to VT323", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                GameFonts.SetPreset(previous);
            }
        }

        private static void TestCanvasTypefaceFollowsActivePreset()
        {
            Console.WriteLine("\n--- Canvas coordinate converter uses GameFonts.ActiveTypeface ---");

            var previous = GameFonts.ActivePreset;
            try
            {
                GameFonts.SetPreset(GameFonts.Preset.Vt323);
                var converter = new CanvasCoordinateConverter();
                var familyName = converter.GetTypeface().FontFamily.Name;
                TestBase.AssertTrue(
                    familyName.Contains("VT323", StringComparison.OrdinalIgnoreCase),
                    $"Expected VT323 in canvas FontFamily, got: {familyName}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                GameFonts.SetPreset(GameFonts.Preset.Bytesized);
                converter.InvalidateCharMetrics();
                var bytesized = converter.GetTypeface();
                TestBase.AssertTrue(
                    bytesized.FontFamily.Name.Contains("Bytesized", StringComparison.OrdinalIgnoreCase),
                    $"Expected Bytesized after preset change, got: {bytesized.FontFamily.Name}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                GameFonts.SetPreset(GameFonts.Preset.CourierNew);
                converter.InvalidateCharMetrics();
                var courier = converter.GetTypeface();
                TestBase.AssertTrue(
                    courier.FontFamily.Name.Contains("Courier New", StringComparison.OrdinalIgnoreCase),
                    $"Expected Courier New after preset change, got: {courier.FontFamily.Name}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(courier.Weight == FontWeight.Bold,
                    "Courier New typeface weight is Bold",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                GameFonts.SetPreset(previous);
            }
        }

        private static void TestPerFontZoomIsIndependentAndClamped()
        {
            Console.WriteLine("\n--- Per-font UI zoom is independent and clamped ---");

            var previous = GameFonts.ActivePreset;
            try
            {
                GameFonts.ResetZoomsForTests();
                GameFonts.SetPreset(GameFonts.Preset.Vt323);
                TestBase.AssertTrue(Math.Abs(GameFonts.ActiveZoom - 1.0) < 1e-9,
                    "default zoom is 1.0", ref _testsRun, ref _testsPassed, ref _testsFailed);

                double vtZoom = GameFonts.AdjustActiveZoom(1);
                TestBase.AssertTrue(Math.Abs(vtZoom - 1.02) < 1e-9,
                    "Ctrl+ steps VT323 to 1.02", ref _testsRun, ref _testsPassed, ref _testsFailed);

                GameFonts.SetPreset(GameFonts.Preset.Bytesized);
                TestBase.AssertTrue(Math.Abs(GameFonts.ActiveZoom - 1.0) < 1e-9,
                    "Bytesized keeps its own default zoom", ref _testsRun, ref _testsPassed, ref _testsFailed);

                double byteZoom = GameFonts.AdjustActiveZoom(-2);
                TestBase.AssertTrue(Math.Abs(byteZoom - 0.96) < 1e-9,
                    "Ctrl- steps Bytesized to 0.96", ref _testsRun, ref _testsPassed, ref _testsFailed);

                GameFonts.SetPreset(GameFonts.Preset.Vt323);
                TestBase.AssertTrue(Math.Abs(GameFonts.ActiveZoom - 1.02) < 1e-9,
                    "VT323 restores its saved zoom after font switch",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                GameFonts.SetZoom(GameFonts.Preset.Vt323, 99);
                TestBase.AssertTrue(Math.Abs(GameFonts.GetZoom(GameFonts.Preset.Vt323) - GameFonts.MaxZoom) < 1e-9,
                    "zoom clamps to max", ref _testsRun, ref _testsPassed, ref _testsFailed);

                GameFonts.SetZoom(GameFonts.Preset.Vt323, 0.01);
                TestBase.AssertTrue(Math.Abs(GameFonts.GetZoom(GameFonts.Preset.Vt323) - GameFonts.MinZoom) < 1e-9,
                    "zoom clamps to min", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                GameFonts.ResetZoomsForTests();
                GameFonts.SetPreset(previous);
            }
        }

        private static void TestCaptureAndApplyPreferencesRoundTrip()
        {
            Console.WriteLine("\n--- Capture/Apply preferences round-trip ---");

            var previous = GameFonts.ActivePreset;
            try
            {
                GameFonts.ResetZoomsForTests();
                GameFonts.SetPreset(GameFonts.Preset.NoplatoMono);
                GameFonts.SetZoom(GameFonts.Preset.NoplatoMono, 1.3);
                GameFonts.SetZoom(GameFonts.Preset.CourierNew, 0.7);

                var prefs = GameFonts.CapturePreferences();
                TestBase.AssertEqual("NoplatoMono", prefs.ActivePreset,
                    "captures active preset name", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(Math.Abs(prefs.GetZoom("NoplatoMono") - 1.3) < 1e-9,
                    "captures Noplato Mono zoom", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(Math.Abs(prefs.GetZoom("CourierNew") - 0.7) < 1e-9,
                    "captures Courier New zoom", ref _testsRun, ref _testsPassed, ref _testsFailed);

                GameFonts.ResetZoomsForTests();
                GameFonts.SetPreset(GameFonts.Preset.Vt323);
                GameFonts.ApplyPreferences(prefs);

                TestBase.AssertEqualEnum(GameFonts.Preset.NoplatoMono, GameFonts.ActivePreset,
                    "apply restores active preset", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(Math.Abs(GameFonts.ActiveZoom - 1.3) < 1e-9,
                    "apply restores active zoom", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(Math.Abs(GameFonts.GetZoom(GameFonts.Preset.CourierNew) - 0.7) < 1e-9,
                    "apply restores other font zoom", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                GameFonts.ResetZoomsForTests();
                GameFonts.SetPreset(previous);
            }
        }

        private static void TestResetAndSetDefaultZoomPerFont()
        {
            Console.WriteLine("\n--- Ctrl+0 reset / Ctrl+Shift+0 set-default per font ---");

            var previous = GameFonts.ActivePreset;
            try
            {
                GameFonts.ResetZoomsForTests();
                GameFonts.SetPreset(GameFonts.Preset.Vt323);
                GameFonts.SetZoom(GameFonts.Preset.Vt323, 1.2);
                double savedDefault = GameFonts.SetActiveZoomAsDefault();
                TestBase.AssertTrue(Math.Abs(savedDefault - 1.2) < 1e-9,
                    "set-default captures 1.2", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(Math.Abs(GameFonts.GetDefaultZoom(GameFonts.Preset.Vt323) - 1.2) < 1e-9,
                    "VT323 default is 1.2", ref _testsRun, ref _testsPassed, ref _testsFailed);

                GameFonts.AdjustActiveZoom(5);
                TestBase.AssertTrue(Math.Abs(GameFonts.ActiveZoom - 1.3) < 1e-9,
                    "current zoom stepped away from default", ref _testsRun, ref _testsPassed, ref _testsFailed);

                double reset = GameFonts.ResetActiveZoomToDefault();
                TestBase.AssertTrue(Math.Abs(reset - 1.2) < 1e-9,
                    "reset restores VT323 default", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(Math.Abs(GameFonts.ActiveZoom - 1.2) < 1e-9,
                    "active zoom matches reset", ref _testsRun, ref _testsPassed, ref _testsFailed);

                GameFonts.SetPreset(GameFonts.Preset.Bytesized);
                TestBase.AssertTrue(Math.Abs(GameFonts.ActiveDefaultZoom - 1.0) < 1e-9,
                    "Bytesized keeps factory default", ref _testsRun, ref _testsPassed, ref _testsFailed);
                GameFonts.SetZoom(GameFonts.Preset.Bytesized, 0.8);
                GameFonts.ResetActiveZoomToDefault();
                TestBase.AssertTrue(Math.Abs(GameFonts.ActiveZoom - 1.0) < 1e-9,
                    "Bytesized reset ignores VT323 default", ref _testsRun, ref _testsPassed, ref _testsFailed);

                var prefs = GameFonts.CapturePreferences();
                TestBase.AssertTrue(Math.Abs(prefs.GetDefaultZoom("Vt323") - 1.2) < 1e-9,
                    "capture includes VT323 default", ref _testsRun, ref _testsPassed, ref _testsFailed);

                GameFonts.ResetZoomsForTests();
                GameFonts.ApplyPreferences(prefs);
                TestBase.AssertTrue(Math.Abs(GameFonts.GetDefaultZoom(GameFonts.Preset.Vt323) - 1.2) < 1e-9,
                    "apply restores VT323 default", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                GameFonts.ResetZoomsForTests();
                GameFonts.SetPreset(previous);
            }
        }
    }
}
