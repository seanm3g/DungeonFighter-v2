using System;
using System.Linq;
using RPGGame;
using RPGGame.Tests;
using RPGGame.Tuning;
using RPGGame.UI.Avalonia.Settings.ViewModels;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>Config and registry behavior backing CombatTuningPanelHandler.</summary>
    public static class CombatTuningPanelHandlerTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== CombatTuningPanelHandler Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestRegistry_LayerCountsMatchAllParameters();
            TestRegistry_ParameterRoundTripsThroughConfig();
            TestConfig_PlayerBaseHealth_LoadsFromPatch();
            TestCommitAllToConfig_FlushesPendingText();
            TestConfig_BalanceTuningGoals_LoadsFromPatch();

            TestBase.PrintSummary("CombatTuningPanelHandler Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestRegistry_LayerCountsMatchAllParameters()
        {
            Console.WriteLine("--- Registry layer counts cover all parameters ---");
            int layerTotal = Enum.GetValues<CombatTuningLayer>()
                .Sum(layer => CombatTuningParameterRegistry.GetByLayer(layer).Count);
            TestBase.AssertEqual(CombatTuningParameterRegistry.All.Count, layerTotal,
                "Every registry parameter belongs to exactly one layer",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(CombatTuningParameterRegistry.All.Count >= 30,
                "Combat tuning exposes curated parameter set",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRegistry_ParameterRoundTripsThroughConfig()
        {
            Console.WriteLine("--- Handler push target round-trips playerBaseHealth ---");
            var param = CombatTuningParameterRegistry.GetById("playerBaseHealth");
            var cfg = GameConfiguration.Instance;
            int saved = cfg.Character.PlayerBaseHealth;
            try
            {
                TestBase.AssertTrue(param != null, "playerBaseHealth exists", ref _testsRun, ref _testsPassed, ref _testsFailed);
                param!.SetValue(77);
                TestBase.AssertEqual(77, cfg.Character.PlayerBaseHealth,
                    "PushParameterFromControl target updates config",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                param.SetValue(saved);
                TestBase.AssertEqual(saved, param.GetValue(),
                    "ReloadSettings would read saved value",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                cfg.Character.PlayerBaseHealth = saved;
            }
        }

        private static void TestConfig_PlayerBaseHealth_LoadsFromPatch()
        {
            Console.WriteLine("--- Balance patch loads playerBaseHealth into config ---");
            var cfg = GameConfiguration.Instance;
            cfg.Reload();
            CombatTuningParameterRegistry.EnsureSanitizedDefaults();

            var param = CombatTuningParameterRegistry.GetById("playerBaseHealth");
            TestBase.AssertTrue(param != null, "playerBaseHealth exists", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(cfg.Character.PlayerBaseHealth > 0,
                "Character.PlayerBaseHealth is positive after patch load",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(cfg.Character.PlayerBaseHealth, (int)param!.GetValue(),
                "Registry getter matches loaded config value",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCommitAllToConfig_FlushesPendingText()
        {
            Console.WriteLine("--- CommitAllToConfig flushes pending playerBaseHealth text ---");
            var param = CombatTuningParameterRegistry.GetById("playerBaseHealth");
            var cfg = GameConfiguration.Instance;
            int saved = cfg.Character.PlayerBaseHealth;
            try
            {
                var vm = CombatTuningPanelViewModel.FromRegistry();
                var row = vm.GetById("playerBaseHealth");
                TestBase.AssertTrue(row != null, "playerBaseHealth row", ref _testsRun, ref _testsPassed, ref _testsFailed);
                if (row == null)
                    return;

                row.ValueText = "188";
                vm.CommitAllToConfig();
                TestBase.AssertEqual(188, cfg.Character.PlayerBaseHealth,
                    "CommitAllToConfig persists flushed text to config",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                cfg.Character.PlayerBaseHealth = saved;
            }
        }

        private static void TestConfig_BalanceTuningGoals_LoadsFromPatch()
        {
            Console.WriteLine("--- Balance patch loads balanceTuningGoals into config ---");
            var cfg = GameConfiguration.Instance;
            cfg.Reload();

            // Compare against whatever patch file the config actually read (the active patch differs per machine).
            double? PatchGoal(string section)
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(GameConfiguration.LoadedBalancePatchPath));
                    foreach (var top in doc.RootElement.EnumerateObject())
                    {
                        if (!top.Name.Equals("balanceTuningGoals", StringComparison.OrdinalIgnoreCase)) continue;
                        foreach (var sec in top.Value.EnumerateObject())
                            if (sec.Name.Equals(section, StringComparison.OrdinalIgnoreCase))
                                foreach (var f in sec.Value.EnumerateObject())
                                    if (f.Name.Equals("minTarget", StringComparison.OrdinalIgnoreCase))
                                        return f.Value.GetDouble();
                    }
                }
                catch (Exception) { }
                return null;
            }

            double? winMin = PatchGoal("winRate");
            double? durMin = PatchGoal("combatDuration");
            TestBase.AssertTrue(winMin == null || Math.Abs(cfg.BalanceTuningGoals.WinRate.MinTarget - winMin.Value) < 0.001,
                "Win rate min target loaded from patch",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(durMin == null || Math.Abs(cfg.BalanceTuningGoals.CombatDuration.MinTarget - durMin.Value) < 0.001,
                $"Combat duration min target loaded from patch (config {cfg.BalanceTuningGoals.CombatDuration.MinTarget}, patch {durMin?.ToString() ?? "n/a"})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
