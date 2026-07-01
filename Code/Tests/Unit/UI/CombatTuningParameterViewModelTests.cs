using System;
using System.Linq;
using RPGGame.Tests;
using RPGGame.Tuning;
using RPGGame.UI.Avalonia.Settings.ViewModels;

namespace RPGGame.Tests.Unit.UI
{
    public static class CombatTuningParameterViewModelTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== CombatTuningParameterViewModel Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestValue_ClampsToMaximum();
            TestValueText_ReflectsIntegerValue();
            TestValueText_ParseUpdatesValue();
            TestFlushPendingText_AppliesUncommittedText();
            TestValueChange_DoesNotInvokeCommitUntilCommitToConfig();
            TestReloadFromConfig_DoesNotPushToBackingParameter();
            TestUnimplementedParameter_ShowsDisplayAffectsPrefix();
            TestDifficultyPresetSubgroup_ShowsUnimplementedHeader();

            TestBase.PrintSummary("CombatTuningParameterViewModel Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestValue_ClampsToMaximum()
        {
            Console.WriteLine("--- Value clamps to registry maximum ---");
            var param = CombatTuningParameterRegistry.GetById("playerBaseHealth");
            var row = new CombatTuningParameterViewModel(param!);
            row.Value = 2000000;
            TestBase.AssertEqual(param!.Maximum, row.Value,
                "Value cannot exceed registry maximum",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestValueText_ReflectsIntegerValue()
        {
            Console.WriteLine("--- ValueText reflects integer Value ---");
            var param = CombatTuningParameterRegistry.GetById("playerBaseHealth");
            var row = new CombatTuningParameterViewModel(param!);
            row.Value = 150;
            TestBase.AssertEqual("150", row.ValueText,
                "ValueText shows whole number for health parameter",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestValueText_ParseUpdatesValue()
        {
            Console.WriteLine("--- ValueText parse updates Value ---");
            var param = CombatTuningParameterRegistry.GetById("globalEnemyHealthMult");
            var row = new CombatTuningParameterViewModel(param!);
            row.ValueText = "1.50";
            TestBase.AssertTrue(Math.Abs(row.Value - 1.5) < 0.001,
                "Parsing ValueText updates decimal Value",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestFlushPendingText_AppliesUncommittedText()
        {
            Console.WriteLine("--- FlushPendingText applies typed text without focus loss ---");
            var param = CombatTuningParameterRegistry.GetById("playerBaseHealth");
            int saved = (int)param!.GetValue();
            try
            {
                var row = new CombatTuningParameterViewModel(param);
                row.ReloadFromConfig();
                row.ValueText = "175";
                TestBase.AssertEqual(saved, (int)param.GetValue(),
                    "pending text does not commit to config until flush",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                row.FlushPendingText();
                row.CommitToConfig();
                TestBase.AssertEqual(175, (int)param.GetValue(),
                    "FlushPendingText + CommitToConfig writes typed value",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                param.SetValue(saved);
            }
        }

        private static void TestValueChange_DoesNotInvokeCommitUntilCommitToConfig()
        {
            Console.WriteLine("--- Value change does not live-commit to config ---");
            var param = CombatTuningParameterRegistry.GetById("playerBaseHealth");
            int saved = (int)param!.GetValue();
            try
            {
                var row = new CombatTuningParameterViewModel(param);
                row.ReloadFromConfig();
                row.Value = 142;
                TestBase.AssertEqual(saved, (int)param.GetValue(),
                    "slider move updates VM only until CommitToConfig",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                row.CommitToConfig();
                TestBase.AssertEqual(142, (int)param.GetValue(),
                    "CommitToConfig pushes VM value to config",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                param.SetValue(saved);
            }
        }

        private static void TestReloadFromConfig_DoesNotPushToBackingParameter()
        {
            Console.WriteLine("--- ReloadFromConfig reads without writing ---");
            var param = CombatTuningParameterRegistry.GetById("playerBaseHealth");
            int saved = (int)param!.GetValue();
            try
            {
                param.SetValue(88);
                int pushed = 0;
                var row = new CombatTuningParameterViewModel(param, v => pushed = (int)v);
                row.ReloadFromConfig();
                TestBase.AssertEqual(88, (int)row.Value,
                    "ReloadFromConfig reads current config value",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(0, pushed,
                    "ReloadFromConfig does not invoke commit callback",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                param.SetValue(saved);
            }
        }

        private static void TestUnimplementedParameter_ShowsDisplayAffectsPrefix()
        {
            Console.WriteLine("--- Unimplemented parameter prefixes DisplayAffects ---");
            var param = CombatTuningParameterRegistry.GetById("easyEnemyHealthMult");
            var row = new CombatTuningParameterViewModel(param!);
            TestBase.AssertTrue(!row.IsImplemented,
                "easyEnemyHealthMult flagged unimplemented",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(row.DisplayAffects.StartsWith("[Unimplemented]", StringComparison.Ordinal),
                "DisplayAffects shows [Unimplemented] prefix",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDifficultyPresetSubgroup_ShowsUnimplementedHeader()
        {
            Console.WriteLine("--- Difficulty subgroup header notes unimplemented ---");
            var easyParams = CombatTuningParameterRegistry.GetByTab(CombatTuningTab.EnemyStats)
                .Where(p => p.SubGroup == "Difficulty — Easy")
                .Select(p => new CombatTuningParameterViewModel(p))
                .ToList();
            var subgroup = new CombatTuningSubGroupViewModel("Difficulty — Easy",
                new System.Collections.ObjectModel.ObservableCollection<CombatTuningParameterViewModel>(easyParams));
            TestBase.AssertEqual("Difficulty — Easy (unimplemented)", subgroup.DisplayName,
                "All-unimplemented subgroup shows suffix",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
