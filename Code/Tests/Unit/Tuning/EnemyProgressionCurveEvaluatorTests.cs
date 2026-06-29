using System;
using RPGGame.Config;
using RPGGame.Tests;
using RPGGame.Tuning;

namespace RPGGame.Tests.Unit.Tuning
{
    public static class EnemyProgressionCurveEvaluatorTests
    {
        private static int _run;
        private static int _pass;
        private static int _fail;

        public static void RunAllTests()
        {
            Console.WriteLine("=== EnemyProgressionCurveEvaluator Tests ===\n");
            _run = _pass = _fail = 0;

            TestCombatTempoScalesAllLevelsEqually();
            TestProgressionShapeChangesMidVsLateHp();
            TestParityScalesPlayerHp();
            TestGrowthWeightIncreasesWithLevel();

            TestBase.PrintSummary("EnemyProgressionCurveEvaluator Tests", _run, _pass, _fail);
        }

        private static void TestCombatTempoScalesAllLevelsEqually()
        {
            Console.WriteLine("--- Combat tempo scales all levels equally ---");
            var cfg = GameConfiguration.Instance;
            var prog = cfg.EnemySystem.ProgressionScales;
            double savedTempo = prog.CombatTempoScale;
            try
            {
                prog.CombatTempoScale = 1.0;
                int l10Base = EnemyProgressionCurveEvaluator.GetDerivedSample(10).EnemyHealth;
                int l50Base = EnemyProgressionCurveEvaluator.GetDerivedSample(50).EnemyHealth;

                prog.CombatTempoScale = 2.0;
                int l10Double = EnemyProgressionCurveEvaluator.GetDerivedSample(10).EnemyHealth;
                int l50Double = EnemyProgressionCurveEvaluator.GetDerivedSample(50).EnemyHealth;

                TestBase.AssertTrue(l10Double >= l10Base * 2 - 1 && l10Double <= l10Base * 2 + 1,
                    $"L10 tempo 2× ({l10Base} → {l10Double})", ref _run, ref _pass, ref _fail);
                TestBase.AssertTrue(l50Double >= l50Base * 2 - 1 && l50Double <= l50Base * 2 + 1,
                    $"L50 tempo 2× ({l50Base} → {l50Double})", ref _run, ref _pass, ref _fail);
            }
            finally
            {
                prog.CombatTempoScale = savedTempo;
            }
        }

        private static void TestProgressionShapeChangesMidVsLateHp()
        {
            Console.WriteLine("--- Progression shape shifts HP accumulation ---");
            var prog = GameConfiguration.Instance.EnemySystem.ProgressionScales;
            double savedShape = prog.ProgressionShape;
            try
            {
                prog.ProgressionShape = 0.0;
                int earlyShapeL10 = EnemyProgressionCurveEvaluator.GetDerivedSample(10).EnemyHealth;
                int earlyShapeL100 = EnemyProgressionCurveEvaluator.GetDerivedSample(100).EnemyHealth;
                double earlyRatio = earlyShapeL100 / (double)Math.Max(1, earlyShapeL10);

                prog.ProgressionShape = 1.0;
                int lateShapeL10 = EnemyProgressionCurveEvaluator.GetDerivedSample(10).EnemyHealth;
                int lateShapeL100 = EnemyProgressionCurveEvaluator.GetDerivedSample(100).EnemyHealth;
                double lateRatio = lateShapeL100 / (double)Math.Max(1, lateShapeL10);

                TestBase.AssertTrue(lateShapeL10 > earlyShapeL10,
                    $"Shape 1 raises L10 HP ({earlyShapeL10} → {lateShapeL10})", ref _run, ref _pass, ref _fail);
                TestBase.AssertTrue(lateRatio < earlyRatio,
                    $"Shape 0 steepens L100 vs L10 ({earlyRatio:F2} vs {lateRatio:F2})", ref _run, ref _pass, ref _fail);
            }
            finally
            {
                prog.ProgressionShape = savedShape;
            }
        }

        private static void TestParityScalesPlayerHp()
        {
            Console.WriteLine("--- Player/enemy parity scales player HP ---");
            var prog = GameConfiguration.Instance.EnemySystem.ProgressionScales;
            double savedParity = prog.PlayerEnemyParity;
            try
            {
                prog.PlayerEnemyParity = 0;
                int neutral = EnemyProgressionCurveEvaluator.EstimatePlayerMaxHealth(20);

                prog.PlayerEnemyParity = 1.0;
                int buffed = EnemyProgressionCurveEvaluator.EstimatePlayerMaxHealth(20);

                prog.PlayerEnemyParity = -1.0;
                int nerfed = EnemyProgressionCurveEvaluator.EstimatePlayerMaxHealth(20);

                TestBase.AssertTrue(buffed > neutral,
                    $"Parity +1 buffs player ({neutral} → {buffed})", ref _run, ref _pass, ref _fail);
                TestBase.AssertTrue(nerfed < neutral,
                    $"Parity -1 nerfs player ({neutral} → {nerfed})", ref _run, ref _pass, ref _fail);
            }
            finally
            {
                prog.PlayerEnemyParity = savedParity;
            }
        }

        private static void TestGrowthWeightIncreasesWithLevel()
        {
            Console.WriteLine("--- Growth weight rises with level ---");
            var prog = GameConfiguration.Instance.EnemySystem.ProgressionScales;
            double w10 = EnemyProgressionCurveEvaluator.ComputeGrowthWeight(10, prog);
            double w30 = EnemyProgressionCurveEvaluator.ComputeGrowthWeight(30, prog);
            double w50 = EnemyProgressionCurveEvaluator.ComputeGrowthWeight(50, prog);

            TestBase.AssertTrue(w10 < w30 && w30 <= w50,
                $"Growth weight rises toward pivot ({w10:F3} < {w30:F3} ≤ {w50:F3})", ref _run, ref _pass, ref _fail);
        }
    }
}
