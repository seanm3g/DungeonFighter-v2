using System;
using RPGGame;
using RPGGame.Tests;
using RPGGame.Tuning;

namespace RPGGame.Tests.Unit.Tuning
{
    public static class CombatTuningParameterRegistryTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== CombatTuningParameterRegistry Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestRegistry_HasAllLayers();
            TestPlayerBaseHealth_RoundTrips();
            TestGlobalEnemyHealthMult_RoundTrips();
            TestPlayerBaseHealth_AffectsComputeMaxHealth();
            TestEnemyHealthMultiplier_AppliesInStatCalculator();
            TestRuntimeDifficulty_UsesGameSettings();

            TestBase.PrintSummary("CombatTuningParameterRegistry Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestRegistry_HasAllLayers()
        {
            Console.WriteLine("--- Registry exposes all balance layers ---");
            TestBase.AssertTrue(CombatTuningParameterRegistry.GetByLayer(CombatTuningLayer.Duration).Count >= 5,
                "Duration layer has parameters", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(CombatTuningParameterRegistry.GetByLayer(CombatTuningLayer.WinRate).Count >= 10,
                "WinRate layer has parameters", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(CombatTuningParameterRegistry.GetByLayer(CombatTuningLayer.Goals).Count >= 4,
                "Goals layer has parameters", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestPlayerBaseHealth_RoundTrips()
        {
            Console.WriteLine("--- playerBaseHealth round-trips ---");
            var param = CombatTuningParameterRegistry.GetById("playerBaseHealth");
            var cfg = GameConfiguration.Instance;
            int saved = cfg.Character.PlayerBaseHealth;
            try
            {
                TestBase.AssertTrue(param != null, "playerBaseHealth exists", ref _testsRun, ref _testsPassed, ref _testsFailed);
                param!.SetValue(88);
                TestBase.AssertEqual(88, cfg.Character.PlayerBaseHealth, "config updated", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(88, param.GetValue(), "getter matches", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                cfg.Character.PlayerBaseHealth = saved;
            }
        }

        private static void TestGlobalEnemyHealthMult_RoundTrips()
        {
            Console.WriteLine("--- globalEnemyHealthMult round-trips ---");
            var param = CombatTuningParameterRegistry.GetById("globalEnemyHealthMult");
            var cfg = GameConfiguration.Instance;
            double saved = cfg.EnemySystem.GlobalMultipliers.HealthMultiplier;
            try
            {
                param!.SetValue(1.35);
                TestBase.AssertTrue(Math.Abs(cfg.EnemySystem.GlobalMultipliers.HealthMultiplier - 1.35) < 0.001,
                    "global health mult updated", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                cfg.EnemySystem.GlobalMultipliers.HealthMultiplier = saved;
            }
        }

        private static void TestPlayerBaseHealth_AffectsComputeMaxHealth()
        {
            Console.WriteLine("--- playerBaseHealth affects ComputeMaxHealthFromTuning ---");
            var cfg = GameConfiguration.Instance;
            int saved = cfg.Character.PlayerBaseHealth;
            try
            {
                cfg.Character.PlayerBaseHealth = 95;
                var character = new Character("RegistryHero", level: 1);
                int max = PlayerTuningApplier.ComputeMaxHealthFromTuning(character);
                TestBase.AssertEqual(95, max, "computed max follows registry path", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                cfg.Character.PlayerBaseHealth = saved;
            }
        }

        private static void TestEnemyHealthMultiplier_AppliesInStatCalculator()
        {
            Console.WriteLine("--- GameSettings.EnemyHealthMultiplier applies in EnemyStatCalculator ---");
            var gs = GameSettings.Instance;
            double savedGs = gs.EnemyHealthMultiplier;
            var enemySystem = GameConfiguration.Instance.EnemySystem;
            double savedGlobal = enemySystem.GlobalMultipliers.HealthMultiplier;

            var data = new EnemyData
            {
                Name = "TuningGoblin",
                Archetype = "Berserker",
                BaseHealth = 40,
                HealthGrowthPerLevel = 2
            };

            try
            {
                enemySystem.GlobalMultipliers.HealthMultiplier = 1.0;
                gs.EnemyHealthMultiplier = 1.0;
                var baseline = EnemyStatCalculator.CalculateStats(data, 1, enemySystem);

                gs.EnemyHealthMultiplier = 2.0;
                var doubled = EnemyStatCalculator.CalculateStats(data, 1, enemySystem);

                TestBase.AssertTrue(doubled.Health >= baseline.Health * 2 - 1 && doubled.Health <= baseline.Health * 2 + 1,
                    $"runtime enemy HP doubles ({baseline.Health} -> {doubled.Health})", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                gs.EnemyHealthMultiplier = savedGs;
                enemySystem.GlobalMultipliers.HealthMultiplier = savedGlobal;
            }
        }

        private static void TestRuntimeDifficulty_UsesGameSettings()
        {
            Console.WriteLine("--- runtime params use GameSettings ---");
            var param = CombatTuningParameterRegistry.GetById("runtimeEnemyHealthMult");
            TestBase.AssertTrue(param != null && param.UsesGameSettings,
                "runtimeEnemyHealthMult flagged as GameSettings", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
