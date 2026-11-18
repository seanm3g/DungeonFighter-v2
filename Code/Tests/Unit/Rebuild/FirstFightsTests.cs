using System;
using RPGGame.Tuning;

namespace RPGGame.Tests.Unit.Rebuild
{
    /// <summary>Rebuild 1.8 / 1.9: the first 60 seconds switches and the level 1 enemy HP floor (D0.4).</summary>
    public static class FirstFightsTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== FirstFights Tests ===\n");
            _run = _passed = _failed = 0;

            TestBase.AssertFalse(FirstSixtySeconds.ShowPathIntro, "The path intro is cut (D1.1)", ref _run, ref _passed, ref _failed);
            TestBase.AssertFalse(FirstSixtySeconds.RandomStartingArmor, "No random starting armor", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(FirstSixtySeconds.StraightIntoFirstDungeon, "Weapon choice goes straight into the first dungeon", ref _run, ref _passed, ref _failed);

            var es = GameConfiguration.Instance.EnemySystem;
            double old = es.LevelOneHealthMultiplierFloor;
            try
            {
                es.LevelOneHealthMultiplierFloor = 0;
                int off1 = EnemyProgressionCurveEvaluator.ApplyFinalHealthMultipliers(1000, es, 1);
                int off2 = EnemyProgressionCurveEvaluator.ApplyFinalHealthMultipliers(1000, es, 2);
                es.LevelOneHealthMultiplierFloor = 0.2;
                int on1 = EnemyProgressionCurveEvaluator.ApplyFinalHealthMultipliers(1000, es, 1);
                int on2 = EnemyProgressionCurveEvaluator.ApplyFinalHealthMultipliers(1000, es, 2);
                TestBase.AssertTrue(on1 >= off1, $"The floor never lowers level 1 HP ({off1} → {on1})", ref _run, ref _passed, ref _failed);
                TestBase.AssertEqual(off2, on2, "Level 2 keeps the patch curve", ref _run, ref _passed, ref _failed);
                double runtime = GameSettings.Instance.EnemyHealthMultiplier > 0 ? GameSettings.Instance.EnemyHealthMultiplier : 1.0;
                double tempo = es.ProgressionScales?.CombatTempoScale ?? 1.0;
                TestBase.AssertTrue(on1 >= (int)Math.Floor(1000 * 0.2 * runtime * tempo) - 1, $"Level 1 HP is at least the floor ({on1})", ref _run, ref _passed, ref _failed);
            }
            finally
            {
                es.LevelOneHealthMultiplierFloor = old;
            }
            TestBase.PrintSummary("FirstFights Tests", _run, _passed, _failed);
        }
    }
}
