using System;
using System.Linq;
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
            TestRegistry_HasExpandedParameterCount();
            TestRegistry_HasAllTabs();
            TestRegistry_HasHeroAndEnemyBaseHealth();
            TestRegistry_HasProgressionCurveKnobs();
            TestPlayerBaseHealth_RoundTrips();
            TestGlobalEnemyHealthMult_RoundTrips();
            TestPlayerBaseHealth_AffectsComputeMaxHealth();
            TestEnemyHealthMultiplier_AppliesInStatCalculator();
            TestRuntimeDifficulty_UsesGameSettings();
            TestClassDamageMultiplier_WiredInRegistry();
            TestDifficultyPresets_MarkedUnimplemented();

            TestBase.PrintSummary("CombatTuningParameterRegistry Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestRegistry_HasExpandedParameterCount()
        {
            Console.WriteLine("--- Registry has expanded parameter catalog ---");
            TestBase.AssertTrue(CombatTuningParameterRegistry.All.Count >= 150,
                $"Registry has 150+ parameters (actual {CombatTuningParameterRegistry.All.Count})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRegistry_HasAllTabs()
        {
            Console.WriteLine("--- Registry assigns parameters to all tabs ---");
            foreach (CombatTuningTab tab in Enum.GetValues(typeof(CombatTuningTab)))
            {
                TestBase.AssertTrue(CombatTuningParameterRegistry.GetByTab(tab).Count > 0,
                    $"Tab {tab} has parameters",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestClassDamageMultiplier_WiredInRegistry()
        {
            Console.WriteLine("--- barbarianDamageMult round-trips ---");
            var param = CombatTuningParameterRegistry.GetById("barbarianDamageMult");
            var cfg = GameConfiguration.Instance;
            double saved = cfg.ClassBalance.Barbarian.DamageMultiplier;
            try
            {
                TestBase.AssertTrue(param != null, "barbarianDamageMult exists", ref _testsRun, ref _testsPassed, ref _testsFailed);
                param!.SetValue(1.2);
                TestBase.AssertTrue(Math.Abs(ClassBalanceHelper.GetDamageMultiplier(WeaponType.Mace) - 1.2) < 0.001,
                    "ClassBalanceHelper reads updated damage mult",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                cfg.ClassBalance.Barbarian.DamageMultiplier = saved;
            }
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

        private static void TestRegistry_HasHeroAndEnemyBaseHealth()
        {
            Console.WriteLine("--- Registry exposes hero and enemy base health ---");
            var hero = CombatTuningParameterRegistry.GetById("playerBaseHealth");
            var enemy = CombatTuningParameterRegistry.GetById("enemyBaselineHealth");
            TestBase.AssertTrue(hero != null, "playerBaseHealth (hero base health) exists",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(enemy != null, "enemyBaselineHealth (enemy base health) exists",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(hero!.Layer == CombatTuningLayer.Duration,
                "Hero base health is in Duration layer", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(enemy!.Layer == CombatTuningLayer.Duration,
                "Enemy base health is in Duration layer", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Hero base health", hero.Label,
                "Hero base health label", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Enemy base health", enemy.Label,
                "Enemy base health label", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRegistry_HasProgressionCurveKnobs()
        {
            Console.WriteLine("--- Registry exposes progression curve knobs ---");
            foreach (var id in new[]
                     {
                         "combatTempoScale", "progressionShape", "playerEnemyParity", "progressionPivotLevel",
                         "baseHealthScale", "healthGrowthScale", "playerHealthPerLevel"
                     })
            {
                var param = CombatTuningParameterRegistry.GetById(id);
                TestBase.AssertTrue(param != null, $"{id} exists", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(param!.Tab == CombatTuningTab.ProgressionCurve,
                    $"{id} on Progression Curve tab", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            var tempo = CombatTuningParameterRegistry.GetById("combatTempoScale");
            var cfg = GameConfiguration.Instance;
            double saved = cfg.EnemySystem.ProgressionScales.CombatTempoScale;
            try
            {
                tempo!.SetValue(1.5);
                TestBase.AssertTrue(Math.Abs(cfg.EnemySystem.ProgressionScales.CombatTempoScale - 1.5) < 0.001,
                    "combatTempoScale round-trips", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                cfg.EnemySystem.ProgressionScales.CombatTempoScale = saved;
            }
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
                HealthPercent = 57.14,
                HealthGrowthPercent = 2.0 / 70.0 * 100.0
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

        private static void TestDifficultyPresets_MarkedUnimplemented()
        {
            Console.WriteLine("--- Difficulty presets marked unimplemented ---");
            foreach (var id in new[] { "easyEnemyHealthMult", "normalXpMult", "hardLootMult" })
            {
                var param = CombatTuningParameterRegistry.GetById(id);
                TestBase.AssertTrue(param != null && !param.IsImplemented,
                    $"{id} is unimplemented",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            var runtime = CombatTuningParameterRegistry.GetById("runtimeEnemyHealthMult");
            TestBase.AssertTrue(runtime != null && runtime.IsImplemented,
                "runtimeEnemyHealthMult remains implemented",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
