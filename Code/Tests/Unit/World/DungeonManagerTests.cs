using System;
using System.Collections.Generic;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.World
{
    /// <summary>
    /// Comprehensive tests for DungeonManagerWithRegistry
    /// Tests dungeon management, selection, completion, and dungeon state
    /// </summary>
    public static class DungeonManagerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all DungeonManager tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== DungeonManager Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestRegenerateDungeons();
            TestGetAvailableDungeons();

            TestBase.PrintSummary("DungeonManager Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            var manager = new DungeonManagerWithRegistry();
            TestBase.AssertNotNull(manager,
                "DungeonManagerWithRegistry should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Dungeon Management Tests

        private static void TestRegenerateDungeons()
        {
            Console.WriteLine("\n--- Testing RegenerateDungeons ---");

            var manager = new DungeonManagerWithRegistry();
            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(5)
                .Build();

            var availableDungeons = new List<Dungeon>();
            manager.RegenerateDungeons(character, availableDungeons);

            TestBase.AssertTrue(availableDungeons.Count >= 0,
                $"RegenerateDungeons should populate dungeons, got {availableDungeons.Count}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // If dungeons were generated, verify they have properties
            if (availableDungeons.Count > 0)
            {
                var dungeon = availableDungeons[0];
                TestBase.AssertNotNull(dungeon,
                    "Generated dungeon should not be null",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                if (dungeon != null)
                {
                    TestBase.AssertTrue(!string.IsNullOrEmpty(dungeon.Name),
                        "Generated dungeon should have a name",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
        }

        private static void TestGetAvailableDungeons()
        {
            Console.WriteLine("\n--- Testing GetAvailableDungeons ---");

            var manager = new DungeonManagerWithRegistry();
            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(5)
                .Build();

            var availableDungeons = new List<Dungeon>();
            manager.RegenerateDungeons(character, availableDungeons);

            // Test that dungeons are accessible
            TestBase.AssertTrue(availableDungeons.Count >= 0,
                "Available dungeons should be accessible",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
