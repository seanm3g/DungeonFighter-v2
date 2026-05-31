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
            TestApplyAndPersistAfterEdit_UpdatesCurrentPlayerMaxHealth();

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

        private static void TestApplyAndPersistAfterEdit_UpdatesCurrentPlayerMaxHealth()
        {
            Console.WriteLine("--- ApplyAndPersistAfterEdit updates current player max health ---");
            var cfg = GameConfiguration.Instance;
            int savedBase = cfg.Character.PlayerBaseHealth;
            try
            {
                cfg.Character.PlayerBaseHealth = 60;
                var character = new Character("PersistTest", level: 1);
                TestBase.AssertEqual(60, character.MaxHealth, "baseline max health is 60", ref _testsRun, ref _testsPassed, ref _testsFailed);

                cfg.Character.PlayerBaseHealth = 100;
                var editor = new VariableEditor();
                editor.ApplyAndPersistAfterEdit(character);

                TestBase.AssertEqual(100, character.MaxHealth, "player max health updated after apply", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                cfg.Character.PlayerBaseHealth = savedBase;
            }
        }
    }
}
