using System;
using RPGGame.ActionInteractionLab;
using RPGGame.BattleStatistics;
using RPGGame.Config;
using RPGGame.Tests;
using RPGGame.Tuning;
using RPGGame.Tuning.Profiles;

namespace RPGGame.Tests.Unit.Tuning
{
    public static class FundamentalsSimulationTests
    {
        private static int _run;
        private static int _pass;
        private static int _fail;

        public static void RunAllTests()
        {
            Console.WriteLine("=== FundamentalsSimulation Tests ===\n");
            _run = _pass = _fail = 0;

            TestFundamentalsUsesLoaderEnemyWithHealthMultiplier();

            TestBase.PrintSummary("FundamentalsSimulation Tests", _run, _pass, _fail);
        }

        private static void TestFundamentalsUsesLoaderEnemyWithHealthMultiplier()
        {
            Console.WriteLine("--- Fundamentals uses loader enemy with health multiplier ---");
            var cfg = GameConfiguration.Instance;
            EnemyLoader.LoadEnemies();
            double savedMult = cfg.EnemySystem.HealthCurve.BaseMultiplier;
            try
            {
                var snapshot = FundamentalsCombatSetup.BuildSnapshot(new SimulationProfileConfig
                {
                    PlayerLevel = 10,
                    EnemyLevel = 10
                });

                TestBase.AssertTrue(!string.IsNullOrEmpty(snapshot.SessionEnemyLoaderType),
                    "Snapshot uses loader enemy", ref _run, ref _pass, ref _fail);
                TestBase.AssertTrue(snapshot.LabEnemyBattleConfig == null,
                    "Snapshot has no hardcoded battle config", ref _run, ref _pass, ref _fail);

                cfg.EnemySystem.HealthCurve.BaseMultiplier = 1.0;
                var enemy1 = EnemyLoader.CreateEnemy(snapshot.SessionEnemyLoaderType!, snapshot.EnemyLevel);
                TestBase.AssertTrue(enemy1 != null, "Loader enemy created", ref _run, ref _pass, ref _fail);
                int hp1 = enemy1!.MaxHealth;

                cfg.EnemySystem.HealthCurve.BaseMultiplier = 2.0;
                var enemy2 = EnemyLoader.CreateEnemy(snapshot.SessionEnemyLoaderType!, snapshot.EnemyLevel);
                TestBase.AssertTrue(enemy2 != null, "Loader enemy created at 2×", ref _run, ref _pass, ref _fail);
                TestBase.AssertTrue(enemy2!.MaxHealth >= hp1 * 1.9,
                    $"HP scales with multiplier ({hp1} → {enemy2.MaxHealth})", ref _run, ref _pass, ref _fail);
            }
            finally
            {
                cfg.EnemySystem.HealthCurve.BaseMultiplier = savedMult;
            }
        }

    }
}
