using System;
using System.Linq;
using RPGGame;
using RPGGame.Tests;
using RPGGame.Tuning;
using RPGGame.UI.Avalonia.Settings.ViewModels;

namespace RPGGame.Tests.Unit.UI
{
    public static class CombatTuningPanelViewModelTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== CombatTuningPanelViewModel Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestFromRegistry_ProducesAllParameters();
            TestHeroSubGroups_FollowRegistryOrder();
            TestReloadFromConfig_RoundTripsPlayerBaseHealth();
            TestGetById_FindsKnownParameter();

            TestBase.PrintSummary("CombatTuningPanelViewModel Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestFromRegistry_ProducesAllParameters()
        {
            Console.WriteLine("--- FromRegistry builds all registry parameters ---");
            var vm = CombatTuningPanelViewModel.FromRegistry();
            TestBase.AssertEqual(CombatTuningParameterRegistry.All.Count, vm.TotalParameterCount,
                "Panel view model exposes every registry parameter",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(vm.CoreSubGroups.Count >= 5,
                "Core tab has multiple subgroups",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(vm.CountForTab(CombatTuningTab.HeroClasses) >= 15,
                "Hero tab has class multipliers",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(vm.ArchetypeTuning.ArchetypeNames.Count >= 6,
                "Archetype selector lists all archetypes",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(vm.StatusEffectTuning.EffectNames.Count >= 5,
                "Status effect selector lists all effects",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            int coreLayerTotal = vm.DurationParameters.Count
                                 + vm.WinRateParameters.Count
                                 + vm.RollFeelParameters.Count
                                 + vm.ComboAffordanceParameters.Count
                                 + vm.GoalsParameters.Count;
            TestBase.AssertTrue(coreLayerTotal > 0 && coreLayerTotal < vm.TotalParameterCount,
                "Core layer collections are a subset of all parameters",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHeroSubGroups_FollowRegistryOrder()
        {
            Console.WriteLine("--- Hero subgroups follow registry order ---");
            var vm = CombatTuningPanelViewModel.FromRegistry();
            var expected = CombatTuningParameterRegistry.GetSubGroupsForTab(CombatTuningTab.HeroClasses);
            var actual = vm.HeroSubGroups.Select(g => g.Name).ToList();
            TestBase.AssertEqual(expected.Count, actual.Count,
                "Hero tab subgroup count matches registry",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Level-Up Growth", actual[0],
                "Level-Up Growth is the first hero subgroup",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            for (int i = 0; i < expected.Count; i++)
            {
                TestBase.AssertEqual(expected[i], actual[i],
                    $"Hero subgroup {i} matches registry order",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestReloadFromConfig_RoundTripsPlayerBaseHealth()
        {
            Console.WriteLine("--- ReloadFromConfig reflects playerBaseHealth ---");
            var cfg = GameConfiguration.Instance;
            int saved = cfg.Character.PlayerBaseHealth;
            try
            {
                cfg.Character.PlayerBaseHealth = 123;
                var vm = CombatTuningPanelViewModel.FromRegistry();
                vm.ReloadFromConfig();
                var row = vm.GetById("playerBaseHealth");
                TestBase.AssertTrue(row != null, "playerBaseHealth row exists", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(123, (int)row!.Value,
                    "ReloadFromConfig loads config into view model",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual("123", row.ValueText,
                    "ValueText matches loaded config value",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                cfg.Character.PlayerBaseHealth = saved;
            }
        }

        private static void TestGetById_FindsKnownParameter()
        {
            Console.WriteLine("--- GetById resolves registry id ---");
            var vm = CombatTuningPanelViewModel.FromRegistry();
            TestBase.AssertTrue(vm.GetById("enemyBaselineHealth") != null,
                "GetById finds enemyBaselineHealth",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
