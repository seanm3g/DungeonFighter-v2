using System;
using RPGGame;
using RPGGame.UI.BlockDisplay;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.UI.BlockDisplay
{
    /// <summary>
    /// Comprehensive tests for BlockDelayManager
    /// Tests delay calculation and application
    /// </summary>
    public static class BlockDelayManagerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all BlockDelayManager tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== BlockDelayManager Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestApplyBlockDelay();
            TestCalculateActionBlockDelay();
            TestCalculateActionBlockDelay_DeveloperModeZero();

            TestBase.PrintSummary("BlockDelayManager Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Delay Tests

        private static void TestApplyBlockDelay()
        {
            Console.WriteLine("--- Testing ApplyBlockDelay ---");

            // Test that ApplyBlockDelay doesn't crash
            BlockDelayManager.ApplyBlockDelay();
            
            TestBase.AssertTrue(true,
                "ApplyBlockDelay should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCalculateActionBlockDelay()
        {
            Console.WriteLine("\n--- Testing CalculateActionBlockDelay ---");

            int delay = BlockDelayManager.CalculateActionBlockDelay();
            
            TestBase.AssertTrue(delay >= 0,
                "CalculateActionBlockDelay should return a non-negative delay",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCalculateActionBlockDelay_DeveloperModeZero()
        {
            Console.WriteLine("\n--- Testing CalculateActionBlockDelay (developer mode) ---");

            bool previous = DeveloperModeState.IsCombatLogInstant;
            try
            {
                DeveloperModeState.SetCombatLogInstant(true);
                int delay = BlockDelayManager.CalculateActionBlockDelay();
                TestBase.AssertEqual(0, delay,
                    "CalculateActionBlockDelay should be 0 when developer mode is on",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(previous);
            }
        }

        #endregion
    }
}
