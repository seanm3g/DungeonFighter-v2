using System;
using System.Linq;
using RPGGame;
using RPGGame.Config;
using RPGGame.Tests;
using RPGGame.Tuning;

namespace RPGGame.Tests.Unit.Tuning
{
    public static class LevelWinRateCurveTests
    {
        private static int _run;
        private static int _pass;
        private static int _fail;

        public static void RunAllTests()
        {
            Console.WriteLine("=== LevelWinRateCurve Tests ===\n");
            _run = _pass = _fail = 0;

            EnsureCurveEnabled();
            TestAnchorsExact();
            TestLevel50Midpoint();
            TestClampHighLevel();
            TestConformanceOnTarget();
            TestDefaultAnchorLevels();
            TestIsWinRateInBandUsesCurve();

            TestBase.PrintSummary("LevelWinRateCurve Tests", _run, _pass, _fail);
        }

        private static void EnsureCurveEnabled()
        {
            var cfg = GameConfiguration.Instance;
            cfg.BalanceTuningGoals.LevelWinRateCurve ??= new LevelWinRateCurveConfig();
            cfg.BalanceTuningGoals.LevelWinRateCurve.Enabled = true;
            cfg.BalanceTuningGoals.LevelWinRateCurve.TolerancePercent = 3.0;
            cfg.BalanceTuningGoals.LevelWinRateCurve.Anchors = new LevelWinRateCurveConfig().Anchors;
        }

        private static void TestAnchorsExact()
        {
            Console.WriteLine("--- Anchor levels match targets ---");
            TestBase.AssertTrue(Math.Abs(LevelWinRateCurve.GetTargetWinRate(1) - 100) < 0.01,
                "L1 = 100%", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(Math.Abs(LevelWinRateCurve.GetTargetWinRate(10) - 90) < 0.01,
                "L10 = 90%", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(Math.Abs(LevelWinRateCurve.GetTargetWinRate(100) - 1) < 0.01,
                "L100 = 1%", ref _run, ref _pass, ref _fail);
        }

        private static void TestLevel50Midpoint()
        {
            Console.WriteLine("--- L50 interpolates near 50% ---");
            double target = LevelWinRateCurve.GetTargetWinRate(50);
            TestBase.AssertTrue(target > 45 && target < 55,
                $"L50 target ~50% (actual {target:F1})", ref _run, ref _pass, ref _fail);
        }

        private static void TestClampHighLevel()
        {
            Console.WriteLine("--- Levels above max clamp to last anchor ---");
            TestBase.AssertTrue(Math.Abs(LevelWinRateCurve.GetTargetWinRate(150) - 1) < 0.01,
                "L150 clamps to 1%", ref _run, ref _pass, ref _fail);
        }

        private static void TestConformanceOnTarget()
        {
            Console.WriteLine("--- Conformance 100 when on target ---");
            double score = LevelWinRateCurve.GetConformanceScore(90, 10);
            TestBase.AssertTrue(score >= 99,
                $"On-target conformance {score}", ref _run, ref _pass, ref _fail);
        }

        private static void TestDefaultAnchorLevels()
        {
            Console.WriteLine("--- Default sweep includes 7 levels ---");
            TestBase.AssertEqual(7, LevelWinRateCurve.GetDefaultAnchorLevels().Count,
                "Default anchor count", ref _run, ref _pass, ref _fail);
        }

        private static void TestIsWinRateInBandUsesCurve()
        {
            Console.WriteLine("--- In-band uses curve tolerance ---");
            TestBase.AssertTrue(LevelWinRateCurve.IsWinRateInBand(1.0, 1),
                "L1 100% on target", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(LevelWinRateCurve.IsWinRateInBand(0.89, 10),
                "L10 89% within 90±3", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(!LevelWinRateCurve.IsWinRateInBand(0.13, 1),
                "L1 13% far below 100% target", ref _run, ref _pass, ref _fail);
        }

    }
}
