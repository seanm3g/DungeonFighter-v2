using System;
using RPGGame;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Verifies runtime combat speed state and the center-panel fast-pacing tint.
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

            TestCombatSpeedLadder();
            TestDelayScaling();
            TestCenterPanelTintChangesWithCombatSpeed();

            TestBase.PrintSummary("DeveloperModeState Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestCombatSpeedLadder()
        {
            Console.WriteLine("--- Combat speed ladder steps through supported values ---");

            int prevSpeed = DeveloperModeState.CombatSpeedMultiplier;
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            try
            {
                DeveloperModeState.SetCombatLogInstant(false);
                DeveloperModeState.SetCombatSpeedMultiplier(1);

                TestBase.AssertEqual(1, DeveloperModeState.CombatSpeedMultiplier,
                    "combat speed starts at 1x when set to the first ladder step",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(2, DeveloperModeState.IncreaseCombatSpeed(),
                    "Page Up step moves 1x to 2x",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(5, DeveloperModeState.IncreaseCombatSpeed(),
                    "Page Up step moves 2x to 5x",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(20, DeveloperModeState.IncreaseCombatSpeed(),
                    "Page Up step moves 5x to 20x",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(20, DeveloperModeState.IncreaseCombatSpeed(),
                    "Page Up stays capped at 20x",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(5, DeveloperModeState.DecreaseCombatSpeed(),
                    "Page Down step moves 20x to 5x",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                DeveloperModeState.SetCombatSpeedMultiplier(prevSpeed);
            }
        }

        private static void TestDelayScaling()
        {
            Console.WriteLine("--- Combat speed scales delays without making them instant ---");

            int prevSpeed = DeveloperModeState.CombatSpeedMultiplier;
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            try
            {
                DeveloperModeState.SetCombatLogInstant(false);
                DeveloperModeState.SetCombatSpeedMultiplier(5);

                TestBase.AssertEqual(200, DeveloperModeState.ScaleDelayMs(1000),
                    "5x speed scales 1000 ms to 200 ms",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(1, DeveloperModeState.ScaleDelayMs(1),
                    "non-zero scaled delays keep a one millisecond floor",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                DeveloperModeState.SetCombatLogInstant(true);
                TestBase.AssertEqual(0, DeveloperModeState.ScaleDelayMs(1000),
                    "legacy instant mode still zeroes delays when explicitly enabled",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                DeveloperModeState.SetCombatSpeedMultiplier(prevSpeed);
            }
        }

        private static void TestCenterPanelTintChangesWithCombatSpeed()
        {
            Console.WriteLine("--- Center panel tint changes when combat speed is accelerated ---");

            int prevSpeed = DeveloperModeState.CombatSpeedMultiplier;
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            try
            {
                DeveloperModeState.SetCombatLogInstant(false);
                DeveloperModeState.SetCombatSpeedMultiplier(1);
                var normal = CenterPanelModeTint.GetBackgroundColor();

                DeveloperModeState.SetCombatSpeedMultiplier(2);
                var fast = CenterPanelModeTint.GetBackgroundColor();

                TestBase.AssertTrue(!normal.Equals(fast),
                    "center panel background should change when combat speed is above 1x",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(fast.B > fast.R,
                    "accelerated combat tint should shift the dark panel toward blue/purple",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                DeveloperModeState.SetCombatSpeedMultiplier(prevSpeed);
            }
        }
    }
}
