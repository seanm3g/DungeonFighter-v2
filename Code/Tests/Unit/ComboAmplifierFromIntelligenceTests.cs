using System;
using RPGGame;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Unit tests for <see cref="ComboAmplifierFromIntelligence"/> (intelligence → base combo amp).
    /// </summary>
    public static class ComboAmplifierFromIntelligenceTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== ComboAmplifierFromIntelligence Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestZeroAndNegativeIntelligenceYieldOnePointZero();
            TestDesignerLogarithmicFormula();
            TestMonotonicIncrease();
            TestIgnoresLegacyTuningFields();

            TestBase.PrintSummary("ComboAmplifierFromIntelligence Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static ComboSystemConfig StandardConfig() => new ComboSystemConfig
        {
            ComboAmplifierMax = 2.0,
            ComboAmplifierMaxTech = 100,
            ComboAmplifierCurveExponent = 2.5
        };

        private static void TestZeroAndNegativeIntelligenceYieldOnePointZero()
        {
            var c = StandardConfig();
            double zero = ComboAmplifierFromIntelligence.Compute(0, c);
            TestBase.AssertTrue(Math.Abs(zero - 1.0) < 1e-9,
                $"INT 0 should yield amp 1.0, got {zero}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            double negative = ComboAmplifierFromIntelligence.Compute(-10, c);
            TestBase.AssertTrue(Math.Abs(negative - 1.0) < 1e-9,
                $"Negative INT should clamp to amp 1.0, got {negative}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDesignerLogarithmicFormula()
        {
            var c = StandardConfig();
            double atOne = ComboAmplifierFromIntelligence.Compute(1, c);
            double expectedAtOne = 1.0 + 0.5 * Math.Log10(2.0);
            TestBase.AssertTrue(Math.Abs(atOne - expectedAtOne) < 1e-12,
                $"INT 1 should follow 1 + 0.5*log10(INT+1), got {atOne}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            double atNine = ComboAmplifierFromIntelligence.Compute(9, c);
            TestBase.AssertTrue(Math.Abs(atNine - 1.5) < 1e-12,
                $"INT 9 should yield amp 1.5 from log10(10), got {atNine}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            double atNinetyNine = ComboAmplifierFromIntelligence.Compute(99, c);
            TestBase.AssertTrue(Math.Abs(atNinetyNine - 2.0) < 1e-12,
                $"INT 99 should yield amp 2.0 from log10(100), got {atNinetyNine}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMonotonicIncrease()
        {
            var c = StandardConfig();
            double prev = ComboAmplifierFromIntelligence.Compute(0, c);
            for (int intel = 1; intel <= 200; intel++)
            {
                double amp = ComboAmplifierFromIntelligence.Compute(intel, c);
                TestBase.AssertTrue(amp >= prev - 1e-12,
                    $"Amp should not decrease from INT {intel - 1} to {intel} ({prev} -> {amp})",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                prev = amp;
            }
        }

        private static void TestIgnoresLegacyTuningFields()
        {
            var c = StandardConfig();
            double standard = ComboAmplifierFromIntelligence.Compute(20, c);

            c.ComboAmplifierMax = 10.0;
            c.ComboAmplifierMaxTech = 1;
            c.ComboAmplifierCurveExponent = 10.0;
            double withLegacyTuningChanged = ComboAmplifierFromIntelligence.Compute(20, c);

            TestBase.AssertTrue(Math.Abs(standard - withLegacyTuningChanged) < 1e-12,
                "Legacy combo AMP tuning fields should not alter the designer log formula",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
