using System;
using RPGGame.Tests;
using RPGGame.Config;
using RPGGame.Tuning;

namespace RPGGame.Tests.Unit.Game
{
    /// <summary>
    /// Tests for AdjustmentExecutor
    /// Tests configuration application and multiplier adjustments
    /// </summary>
    public static class AdjustmentExecutorTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all AdjustmentExecutor tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== AdjustmentExecutor Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestApplyConfiguration();
            TestAdjustGlobalEnemyMultiplier();
            TestAdjustGlobalEnemyMultiplierWithInvalidName();

            TestBase.PrintSummary("AdjustmentExecutor Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Configuration Tests

        private static void TestApplyConfiguration()
        {
            Console.WriteLine("--- Testing ApplyConfiguration ---");

            try
            {
                var config = new GameConfiguration();
                AdjustmentExecutor.ApplyConfiguration(config);
                
                TestBase.AssertTrue(true,
                    "ApplyConfiguration should complete without errors",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"ApplyConfiguration failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestAdjustGlobalEnemyMultiplier()
        {
            Console.WriteLine("\n--- Testing AdjustGlobalEnemyMultiplier ---");

            try
            {
                var result = AdjustmentExecutor.AdjustGlobalEnemyMultiplier("health", 1.5);
                
                TestBase.AssertTrue(true,
                    "AdjustGlobalEnemyMultiplier should complete",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"AdjustGlobalEnemyMultiplier failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestAdjustGlobalEnemyMultiplierWithInvalidName()
        {
            Console.WriteLine("\n--- Testing AdjustGlobalEnemyMultiplier with invalid name ---");

            try
            {
                var result = AdjustmentExecutor.AdjustGlobalEnemyMultiplier("invalid", 1.0);
                
                TestBase.AssertTrue(result == false,
                    "AdjustGlobalEnemyMultiplier should return false for invalid multiplier name",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"AdjustGlobalEnemyMultiplier with invalid name failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion
    }
}
