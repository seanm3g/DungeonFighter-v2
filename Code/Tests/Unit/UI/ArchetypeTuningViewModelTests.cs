using System.Linq;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Settings.ViewModels;

namespace RPGGame.Tests.Unit.UI
{
    public static class ArchetypeTuningViewModelTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            System.Console.WriteLine("=== ArchetypeTuningViewModel Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestArchetypeSelector_FiltersParameters();

            TestBase.PrintSummary("ArchetypeTuningViewModel Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestArchetypeSelector_FiltersParameters()
        {
            System.Console.WriteLine("--- Archetype selector filters to 6 sliders ---");
            var panel = CombatTuningPanelViewModel.FromRegistry();
            var arch = panel.ArchetypeTuning;
            TestBase.AssertTrue(arch.ArchetypeNames.Count >= 6,
                "Has archetype names", ref _testsRun, ref _testsPassed, ref _testsFailed);
            arch.SelectedArchetype = "Berserker";
            TestBase.AssertEqual(6, arch.SelectedParameters.Count,
                "Berserker shows 6 stat multipliers",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(arch.SelectedParameters.Any(p => p.Id.Contains("health")),
                "Includes health multiplier", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
