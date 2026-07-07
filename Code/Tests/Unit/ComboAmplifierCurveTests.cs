using System;
using RPGGame;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Unit tests for <see cref="ComboAmplifierCurve"/> (technique → base combo amp).
    /// </summary>
    public static class ComboAmplifierCurveTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== ComboAmplifierCurve Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestZeroAndNegativeTechniqueYieldOnePointZero();
            TestDesignerAnchorPoints();
            TestMonotonicIncrease();
            TestRespectsConfiguredMaxTechBreakpoint();

            TestBase.PrintSummary("ComboAmplifierCurve Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static ComboSystemConfig StandardConfig() => new ComboSystemConfig
        {
            ComboAmplifierMax = 2.0,
            ComboAmplifierMaxTech = 110,
            ComboAmplifierCurveExponent = 1.1
        };

        private static void TestZeroAndNegativeTechniqueYieldOnePointZero()
        {
            var c = StandardConfig();
            double zero = ComboAmplifierCurve.Compute(0, c);
            TestBase.AssertTrue(Math.Abs(zero - 1.0) < 1e-9,
                $"TECH 0 should yield amp 1.0, got {zero}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            double negative = ComboAmplifierCurve.Compute(-10, c);
            TestBase.AssertTrue(Math.Abs(negative - 1.0) < 1e-9,
                $"Negative TECH should clamp to amp 1.0, got {negative}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDesignerAnchorPoints()
        {
            var c = StandardConfig();

            AssertNear(ComboAmplifierCurve.Compute(110, c), 2.00, 0.001, "TECH 110");
            AssertNear(ComboAmplifierCurve.Compute(99, c), 1.98, 0.001, "TECH 99");
            AssertNear(ComboAmplifierCurve.Compute(20, c), 1.20, 0.02, "TECH 20");
            AssertNear(ComboAmplifierCurve.Compute(50, c), 1.44, 0.02, "TECH 50");
        }

        private static void AssertNear(double actual, double expected, double tolerance, string label)
        {
            TestBase.AssertTrue(Math.Abs(actual - expected) <= tolerance,
                $"{label} should be ~{expected:F2}× (±{tolerance}), got {actual:F3}×",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMonotonicIncrease()
        {
            var c = StandardConfig();
            double prev = ComboAmplifierCurve.Compute(0, c);
            for (int tech = 1; tech <= 200; tech++)
            {
                double amp = ComboAmplifierCurve.Compute(tech, c);
                TestBase.AssertTrue(amp >= prev - 1e-12,
                    $"Amp should not decrease from TECH {tech - 1} to {tech} ({prev} -> {amp})",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                prev = amp;
            }
        }

        private static void TestRespectsConfiguredMaxTechBreakpoint()
        {
            var c = StandardConfig();
            double atTwenty = ComboAmplifierCurve.Compute(20, c);

            c.ComboAmplifierMaxTech = 20;
            double atTwentyFastCurve = ComboAmplifierCurve.Compute(20, c);
            TestBase.AssertTrue(Math.Abs(atTwentyFastCurve - 2.0) < 1e-9,
                $"TECH 20 should reach amp 2.0 when maxTech is 20, got {atTwentyFastCurve}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(atTwenty < atTwentyFastCurve - 0.01,
                "Default TECH 110 cap should scale slower than a TECH 20 cap",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
