using System;
using RPGGame;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Unit tests for <see cref="ComboAmplifierFromTechnique"/> (technique → base combo amp).
    /// </summary>
    public static class ComboAmplifierFromTechniqueTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== ComboAmplifierFromTechnique Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestFlatRegionOnePointZero();
            TestMaxTechReachesMaxAmp();
            TestMonotonicIncrease();
            TestDefaultExponentWhenZero();

            TestBase.PrintSummary("ComboAmplifierFromTechnique Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static ComboSystemConfig StandardConfig() => new ComboSystemConfig
        {
            ComboAmplifierMax = 2.0,
            ComboAmplifierMaxTech = 100,
            ComboAmplifierCurveExponent = 2.5
        };

        private static void TestFlatRegionOnePointZero()
        {
            var c = StandardConfig();
            for (int tech = 0; tech < ComboAmplifierFromTechnique.FlatAmpBelowTech; tech++)
            {
                double amp = ComboAmplifierFromTechnique.Compute(tech, c);
                TestBase.AssertTrue(Math.Abs(amp - 1.0) < 1e-9,
                    $"TECH {tech} should yield amp 1.0, got {amp}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestMaxTechReachesMaxAmp()
        {
            var c = StandardConfig();
            double amp = ComboAmplifierFromTechnique.Compute(100, c);
            TestBase.AssertTrue(Math.Abs(amp - 2.0) < 1e-9,
                $"TECH 100 should yield amp 2.0, got {amp}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            double atFive = ComboAmplifierFromTechnique.Compute(5, c);
            TestBase.AssertTrue(Math.Abs(atFive - 1.0) < 1e-9,
                $"TECH 5 should start curved region at 1.0, got {atFive}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMonotonicIncrease()
        {
            var c = StandardConfig();
            double prev = ComboAmplifierFromTechnique.Compute(4, c);
            for (int tech = 5; tech <= 100; tech++)
            {
                double amp = ComboAmplifierFromTechnique.Compute(tech, c);
                TestBase.AssertTrue(amp >= prev - 1e-12,
                    $"Amp should not decrease from TECH {tech - 1} to {tech} ({prev} -> {amp})",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                prev = amp;
            }
        }

        private static void TestDefaultExponentWhenZero()
        {
            var c = StandardConfig();
            c.ComboAmplifierCurveExponent = 0;
            double withDefault = ComboAmplifierFromTechnique.Compute(50, c);
            c.ComboAmplifierCurveExponent = 2.5;
            double explicitExp = ComboAmplifierFromTechnique.Compute(50, c);
            TestBase.AssertTrue(Math.Abs(explicitExp - withDefault) < 1e-12,
                "Exponent 0 should fall back to 2.5",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
