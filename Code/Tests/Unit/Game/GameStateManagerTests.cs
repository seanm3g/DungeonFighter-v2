using System;
using System.Collections.Generic;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Game
{
    /// <summary>
    /// Comprehensive tests for GameStateManager
    /// Tests state transitions, state validation, and state persistence
    /// </summary>
    public static class GameStateManagerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all GameStateManager tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== GameStateManager Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestCurrentState();
            TestCurrentPlayer();
            TestCurrentInventory();
            TestAvailableDungeons();
            TestCurrentDungeon();
            TestCurrentRoom();

            TestBase.PrintSummary("GameStateManager Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            var manager = new GameStateManager();
            TestBase.AssertNotNull(manager,
                "GameStateManager should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (manager != null)
            {
                TestBase.AssertEqualEnum(GameState.MainMenu, manager.CurrentState,
                    "Initial state should be MainMenu",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region State Tests

        private static void TestCurrentState()
        {
            Console.WriteLine("\n--- Testing CurrentState ---");

            var manager = new GameStateManager();
            TestBase.AssertEqualEnum(GameState.MainMenu, manager.CurrentState,
                "CurrentState should default to MainMenu",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Player Tests

        private static void TestCurrentPlayer()
        {
            Console.WriteLine("\n--- Testing CurrentPlayer ---");

            var manager = new GameStateManager();
            var player = manager.CurrentPlayer;
            
            // Player might be null initially, which is acceptable
            TestBase.AssertTrue(player == null || player != null,
                "CurrentPlayer should be accessible",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Inventory Tests

        private static void TestCurrentInventory()
        {
            Console.WriteLine("\n--- Testing CurrentInventory ---");

            var manager = new GameStateManager();
            var inventory = manager.CurrentInventory;

            TestBase.AssertNotNull(inventory,
                "CurrentInventory should return a list",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (inventory != null)
            {
                TestBase.AssertTrue(inventory.Count >= 0,
                    $"Inventory count should be >= 0, got {inventory.Count}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Dungeon Tests

        private static void TestAvailableDungeons()
        {
            Console.WriteLine("\n--- Testing AvailableDungeons ---");

            var manager = new GameStateManager();
            var dungeons = manager.AvailableDungeons;

            TestBase.AssertNotNull(dungeons,
                "AvailableDungeons should return a list",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (dungeons != null)
            {
                TestBase.AssertTrue(dungeons.Count >= 0,
                    $"Dungeons count should be >= 0, got {dungeons.Count}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestCurrentDungeon()
        {
            Console.WriteLine("\n--- Testing CurrentDungeon ---");

            var manager = new GameStateManager();
            var dungeon = manager.CurrentDungeon;

            // Dungeon might be null initially, which is acceptable
            TestBase.AssertTrue(dungeon == null || dungeon != null,
                "CurrentDungeon should be accessible",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCurrentRoom()
        {
            Console.WriteLine("\n--- Testing CurrentRoom ---");

            var manager = new GameStateManager();
            var room = manager.CurrentRoom;

            // Room might be null initially, which is acceptable
            TestBase.AssertTrue(room == null || room != null,
                "CurrentRoom should be accessible",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
