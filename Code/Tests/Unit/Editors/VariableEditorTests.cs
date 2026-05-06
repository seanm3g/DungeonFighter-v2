using System;
using RPGGame;
using RPGGame.Editors;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Editors
{
    /// <summary>
    /// Ensures game-variable bindings read/write the current <see cref="GameConfiguration.Instance"/>,
    /// not a stale reference (regression: Save + ResetInstance must not orphan tuning edits from other tabs).
    /// </summary>
    public static class VariableEditorTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== VariableEditor Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestGameVariableBindingUsesLiveConfigurationInstance();

            TestBase.PrintSummary("VariableEditor Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestGameVariableBindingUsesLiveConfigurationInstance()
        {
            Console.WriteLine("--- VariableEditor getters use live GameConfiguration.Instance ---");
            var cfg = GameConfiguration.Instance;
            int saved = cfg.Combat.CriticalHitThreshold;
            try
            {
                cfg.Combat.CriticalHitThreshold = saved + 91;
                var editor = new VariableEditor();
                var v = editor.GetVariable("Combat.CriticalHitThreshold");
                TestBase.AssertNotNull(v, "Combat.CriticalHitThreshold variable", ref _testsRun, ref _testsPassed, ref _testsFailed);
                if (v == null)
                    return;
                object val = v.GetValue();
                TestBase.AssertEqual(saved + 91, Convert.ToInt32(val), "getter follows in-memory singleton", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                cfg.Combat.CriticalHitThreshold = saved;
            }
        }
    }
}
