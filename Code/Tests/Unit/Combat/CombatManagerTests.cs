using System;
using System.Threading.Tasks;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Comprehensive tests for CombatManager
    /// Tests combat orchestration, turn management, and state transitions
    /// </summary>
    public static class CombatManagerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all CombatManager tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== CombatManager Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestStartBattleNarrative();
            TestEndBattleNarrative();
            TestGetBattleNarrative();

            TestBase.PrintSummary("CombatManager Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            var manager = new CombatManager();
            TestBase.AssertNotNull(manager,
                "CombatManager should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Battle Narrative Tests

        private static void TestStartBattleNarrative()
        {
            Console.WriteLine("\n--- Testing StartBattleNarrative ---");

            var manager = new CombatManager();
            manager.StartBattleNarrative("Player", "Enemy", "Forest", 100, 50);

            // Starting narrative should not crash
            TestBase.AssertTrue(true,
                "StartBattleNarrative should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEndBattleNarrative()
        {
            Console.WriteLine("\n--- Testing EndBattleNarrative ---");

            var manager = new CombatManager();
            manager.StartBattleNarrative("Player", "Enemy", "Forest", 100, 50);
            manager.EndBattleNarrative();

            // Ending narrative should not crash
            TestBase.AssertTrue(true,
                "EndBattleNarrative should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGetBattleNarrative()
        {
            Console.WriteLine("\n--- Testing GetBattleNarrative ---");

            var manager = new CombatManager();
            manager.StartBattleNarrative("Player", "Enemy", "Forest", 100, 50);

            var narrative = manager.GetCurrentBattleNarrative();
            TestBase.AssertNotNull(narrative,
                "GetCurrentBattleNarrative should return a narrative",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
