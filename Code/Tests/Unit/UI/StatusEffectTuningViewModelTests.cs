using System.Linq;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Settings.ViewModels;

namespace RPGGame.Tests.Unit.UI
{
    public static class StatusEffectTuningViewModelTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            System.Console.WriteLine("=== StatusEffectTuningViewModel Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestStatusEffectSelector_FiltersParameters();

            TestBase.PrintSummary("StatusEffectTuningViewModel Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestStatusEffectSelector_FiltersParameters()
        {
            System.Console.WriteLine("--- Status effect selector filters per-effect sliders ---");
            var panel = CombatTuningPanelViewModel.FromRegistry();
            var fx = panel.StatusEffectTuning;
            TestBase.AssertTrue(fx.GlobalParameters.Count >= 6,
                "Has global status scaling params", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(fx.EffectNames.Count >= 5,
                "Has effect names", ref _testsRun, ref _testsPassed, ref _testsFailed);
            fx.SelectedEffect = "Bleed";
            TestBase.AssertEqual(4, fx.SelectedEffectParameters.Count,
                "Bleed shows 4 key fields",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(fx.SelectedEffectParameters.Any(p => p.Id.Contains("damagePerTick")),
                "Includes damage per tick", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
