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
            TestStartingSubGroups_FollowRegistryOrder();
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
            TestBase.AssertTrue(vm.StartingSubGroups.Count >= 4,
                "Starting tab has multiple subgroups",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(vm.CountForTab(CombatTuningTab.Scaling) >= 15,
                "Scaling tab has growth and class multipliers",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(vm.CountForTab(CombatTuningTab.Outfit) >= 20,
                "Outfit tab has gear/feel/status/archetype knobs",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(vm.ArchetypeTuning.ArchetypeNames.Count >= 6,
                "Archetype selector lists all archetypes",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(vm.StatusEffectTuning.EffectNames.Count >= 5,
                "Status effect selector lists all effects",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestStartingSubGroups_FollowRegistryOrder()
        {
            Console.WriteLine("--- Starting subgroups follow registry order ---");
            var vm = CombatTuningPanelViewModel.FromRegistry();
            var expected = CombatTuningParameterRegistry.GetSubGroupsForTab(CombatTuningTab.Starting);
            var actual = vm.StartingSubGroups.Select(g => g.Name).ToList();
            TestBase.AssertEqual(expected.Count, actual.Count,
                "Starting tab subgroup count matches registry",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Hero bases", actual[0],
                "Hero bases is the first Starting subgroup",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            for (int i = 0; i < expected.Count; i++)
            {
                TestBase.AssertEqual(expected[i], actual[i],
                    $"Starting subgroup {i} matches registry order",
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
            Console.WriteLine("--- GetById finds attributeDamageScale ---");
            var vm = CombatTuningPanelViewModel.FromRegistry();
            var row = vm.GetById("attributeDamageScale");
            TestBase.AssertTrue(row != null, "attributeDamageScale row exists", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
