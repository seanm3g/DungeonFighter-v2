using System;
using RPGGame.Tests;
using RPGGame.Actions.RollModification;

namespace RPGGame.Tests.Unit.Actions
{
    /// <summary>
    /// Comprehensive tests for RollModificationManager
    /// Tests roll modifications, threshold management, and modifier application
    /// </summary>
    public static class RollModificationManagerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all RollModificationManager tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== RollModificationManager Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestApplyActionRollModifications();
            TestGetThresholdManager();
            TestApplyThresholdOverrides();

            TestBase.PrintSummary("RollModificationManager Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Roll Modification Tests

        private static void TestApplyActionRollModifications()
        {
            Console.WriteLine("--- Testing ApplyActionRollModifications ---");

            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            var enemy = TestDataBuilders.Enemy()
                .WithName("TestEnemy")
                .WithLevel(1)
                .Build();

            var action = TestDataBuilders.CreateMockAction("TestAction", ActionType.Attack);
            var baseRoll = 10;

            var modifiedRoll = RollModificationManager.ApplyActionRollModifications(
                baseRoll, action, character, enemy);

            TestBase.AssertTrue(modifiedRoll >= 1 && modifiedRoll <= 20,
                $"Modified roll should be in valid range (1-20), got {modifiedRoll}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Threshold Management Tests

        private static void TestGetThresholdManager()
        {
            Console.WriteLine("\n--- Testing GetThresholdManager ---");

            var manager = RollModificationManager.GetThresholdManager();
            TestBase.AssertNotNull(manager,
                "GetThresholdManager should return a threshold manager",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestApplyThresholdOverrides()
        {
            Console.WriteLine("\n--- Testing ApplyThresholdOverrides ---");

            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            var enemy = TestDataBuilders.Enemy()
                .WithName("TestEnemy")
                .WithLevel(1)
                .Build();

            var action = TestDataBuilders.CreateMockAction("TestAction", ActionType.Attack);
            
            // Test applying threshold overrides (should not crash)
            RollModificationManager.ApplyThresholdOverrides(action, character, enemy);
            TestBase.AssertTrue(true,
                "ApplyThresholdOverrides should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with null target
            RollModificationManager.ApplyThresholdOverrides(action, character, null);
            TestBase.AssertTrue(true,
                "ApplyThresholdOverrides should work with null target",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
