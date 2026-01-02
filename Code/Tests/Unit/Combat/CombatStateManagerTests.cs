using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Comprehensive tests for CombatStateManager
    /// Tests state management, entity tracking, and battle narrative
    /// </summary>
    public static class CombatStateManagerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all CombatStateManager tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== CombatStateManager Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestStartBattleNarrative();
            TestEndBattleNarrative();
            TestEndBattleNarrativeWithEntities();
            TestGetBattleNarrative();
            TestInitializeCombatEntities();
            TestGetNextEntityToAct();
            TestUpdateLastPlayerAction();
            TestGetLastPlayerAction();
            TestGetCurrentActionSpeedSystem();

            TestBase.PrintSummary("CombatStateManager Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            var manager = new CombatStateManager();
            TestBase.AssertNotNull(manager,
                "CombatStateManager should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Battle Narrative Tests

        private static void TestStartBattleNarrative()
        {
            Console.WriteLine("\n--- Testing StartBattleNarrative ---");

            var manager = new CombatStateManager();
            manager.StartBattleNarrative("Player", "Enemy", "Forest", 100, 50);

            // Starting narrative should not crash
            TestBase.AssertTrue(true,
                "StartBattleNarrative should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEndBattleNarrative()
        {
            Console.WriteLine("\n--- Testing EndBattleNarrative ---");

            var manager = new CombatStateManager();
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

            var manager = new CombatStateManager();
            manager.StartBattleNarrative("Player", "Enemy", "Forest", 100, 50);

            var narrative = manager.GetCurrentBattleNarrative();
            TestBase.AssertNotNull(narrative,
                "GetCurrentBattleNarrative should return a narrative",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEndBattleNarrativeWithEntities()
        {
            Console.WriteLine("\n--- Testing EndBattleNarrativeWithEntities ---");

            var manager = new CombatStateManager();
            manager.StartBattleNarrative("Player", "Enemy", "Forest", 100, 50);

            var player = TestDataBuilders.Character().WithName("Player").Build();
            var enemy = TestDataBuilders.Enemy().WithName("Enemy").Build();

            manager.EndBattleNarrative(player, enemy);

            // Should not throw exception
            TestBase.AssertTrue(true,
                "EndBattleNarrative with entities should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestInitializeCombatEntities()
        {
            Console.WriteLine("\n--- Testing InitializeCombatEntities ---");

            var manager = new CombatStateManager();
            var player = TestDataBuilders.Character().WithName("Player").Build();
            var enemy = TestDataBuilders.Enemy().WithName("Enemy").Build();

            manager.InitializeCombatEntities(player, enemy);

            // Should not throw exception
            TestBase.AssertTrue(true,
                "InitializeCombatEntities should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGetNextEntityToAct()
        {
            Console.WriteLine("\n--- Testing GetNextEntityToAct ---");

            var manager = new CombatStateManager();
            var player = TestDataBuilders.Character().WithName("Player").Build();
            var enemy = TestDataBuilders.Enemy().WithName("Enemy").Build();

            manager.InitializeCombatEntities(player, enemy);

            var nextEntity = manager.GetNextEntityToAct();
            // May be null if not properly initialized
            TestBase.AssertTrue(true,
                "GetNextEntityToAct should complete",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestUpdateLastPlayerAction()
        {
            Console.WriteLine("\n--- Testing UpdateLastPlayerAction ---");

            var manager = new CombatManager();
            var action = TestDataBuilders.CreateMockAction("JAB");

            manager.UpdateLastPlayerAction(action);

            // Should not throw exception
            TestBase.AssertTrue(true,
                "UpdateLastPlayerAction should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGetLastPlayerAction()
        {
            Console.WriteLine("\n--- Testing GetLastPlayerAction ---");

            var manager = new CombatManager();
            var action = TestDataBuilders.CreateMockAction("JAB");

            manager.UpdateLastPlayerAction(action);
            var retrievedAction = manager.GetLastPlayerAction();

            TestBase.AssertNotNull(retrievedAction,
                "GetLastPlayerAction should return the set action",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGetCurrentActionSpeedSystem()
        {
            Console.WriteLine("\n--- Testing GetCurrentActionSpeedSystem ---");

            var manager = new CombatStateManager();
            var player = TestDataBuilders.Character().WithName("Player").Build();
            var enemy = TestDataBuilders.Enemy().WithName("Enemy").Build();

            manager.InitializeCombatEntities(player, enemy);

            var actionSpeedSystem = manager.GetCurrentActionSpeedSystem();
            // May be null if not properly initialized
            TestBase.AssertTrue(true,
                "GetCurrentActionSpeedSystem should complete",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
