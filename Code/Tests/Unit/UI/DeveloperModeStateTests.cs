using System;
using System.Diagnostics;
using RPGGame;
using RPGGame.Tests;
using RPGGame.UI;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Verifies developer mode zeros UIDelayManager pacing (room/system lines use message-type delays).
    /// </summary>
    public static class DeveloperModeStateTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== DeveloperModeState Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;

            TestUidelaySkipsWhenDeveloperModeOn();

            TestBase.PrintSummary("DeveloperModeState Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestUidelaySkipsWhenDeveloperModeOn()
        {
            Console.WriteLine("--- UIDelayManager skips delay when developer mode on ---");

            bool prev = DeveloperModeState.IsCombatLogInstant;
            bool prevEnable = UIManager.EnableDelays;
            try
            {
                UIManager.EnableDelays = true;
                DeveloperModeState.SetCombatLogInstant(true);

                var delayManager = new UIDelayManager();
                var sw = Stopwatch.StartNew();
                delayManager.ApplyDelayAsync(UIMessageType.System).GetAwaiter().GetResult();
                sw.Stop();

                TestBase.AssertTrue(sw.ElapsedMilliseconds < 80,
                    "ApplyDelayAsync(System) should not wait when developer mode is on",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(prev);
                UIManager.EnableDelays = prevEnable;
            }
        }
    }
}
