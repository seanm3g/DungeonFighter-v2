using System;
using System.Collections.Generic;
using RPGGame.Tests;

namespace RPGGame.Tests.Integration
{
    /// <summary>
    /// Comprehensive integration tests for dungeon system
    /// Tests dungeon generation, room creation, enemy spawning, and dungeon flow
    /// </summary>
    public static class DungeonIntegrationTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Dungeon Integration Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestDungeonCreation();
            TestDungeonGeneration();
            TestRoomGeneration();
            TestEnemySpawningInRooms();
            TestDungeonScaling();
            TestDungeonThemeConsistency();

            TestBase.PrintSummary("Dungeon Integration Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestDungeonCreation()
        {
            Console.WriteLine("--- Testing Dungeon Creation ---");

            var dungeon = new Dungeon("Test Dungeon", 1, 5, "Forest");

            TestBase.AssertNotNull(dungeon, 
                "Dungeon should be created", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(dungeon.Name == "Test Dungeon", 
                "Dungeon should have correct name", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(dungeon.MinLevel == 1 && dungeon.MaxLevel == 5, 
                "Dungeon should have correct level range", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(dungeon.Theme == "Forest", 
                "Dungeon should have correct theme", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDungeonGeneration()
        {
            Console.WriteLine("\n--- Testing Dungeon Generation ---");

            var dungeon = new Dungeon("Test Dungeon", 1, 5, "Forest");
            dungeon.Generate();

            TestBase.AssertTrue(dungeon.Rooms.Count > 0, 
                "Dungeon should generate at least one room", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Verify rooms are properly initialized
            foreach (var room in dungeon.Rooms)
            {
                TestBase.AssertNotNull(room, 
                    "Room should not be null", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestRoomGeneration()
        {
            Console.WriteLine("\n--- Testing Room Generation ---");

            var dungeon = new Dungeon("Test Dungeon", 1, 5, "Forest");
            dungeon.Generate();

            bool hasHostileRoom = false;
            foreach (var room in dungeon.Rooms)
            {
                if (room != null)
                {
                    var enemies = room.GetEnemies();
                    if (enemies != null && enemies.Count > 0)
                    {
                        hasHostileRoom = true;
                    }
                }
            }

            // At least one room should be hostile (dungeon generation ensures this)
            TestBase.AssertTrue(hasHostileRoom, 
                "Dungeon should have at least one hostile room", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEnemySpawningInRooms()
        {
            Console.WriteLine("\n--- Testing Enemy Spawning in Rooms ---");

            var dungeon = new Dungeon("Test Dungeon", 1, 5, "Forest");
            dungeon.Generate();

            int totalEnemies = 0;
            foreach (var room in dungeon.Rooms)
            {
                if (room != null)
                {
                    var enemies = room.GetEnemies();
                    if (enemies != null)
                    {
                        totalEnemies += enemies.Count;
                    }
                }
            }

            // At least one enemy should spawn in hostile rooms
            TestBase.AssertTrue(totalEnemies > 0, 
                "Dungeon should spawn at least one enemy", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDungeonScaling()
        {
            Console.WriteLine("\n--- Testing Dungeon Scaling ---");

            // Test low level dungeon
            var lowLevelDungeon = new Dungeon("Low Level", 1, 3, "Forest");
            lowLevelDungeon.Generate();

            // Test mid level dungeon
            var midLevelDungeon = new Dungeon("Mid Level", 10, 15, "Forest");
            midLevelDungeon.Generate();

            // Test high level dungeon
            var highLevelDungeon = new Dungeon("High Level", 20, 25, "Forest");
            highLevelDungeon.Generate();

            TestBase.AssertTrue(lowLevelDungeon.Rooms.Count > 0, 
                "Low level dungeon should generate rooms", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(midLevelDungeon.Rooms.Count > 0, 
                "Mid level dungeon should generate rooms", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(highLevelDungeon.Rooms.Count > 0, 
                "High level dungeon should generate rooms", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDungeonThemeConsistency()
        {
            Console.WriteLine("\n--- Testing Dungeon Theme Consistency ---");

            var themes = new List<string> { "Forest", "Cave", "Desert", "Dungeon" };
            
            foreach (var theme in themes)
            {
                var dungeon = new Dungeon($"Test {theme}", 1, 5, theme);
                dungeon.Generate();

                TestBase.AssertTrue(dungeon.Theme == theme, 
                    $"Dungeon should maintain theme '{theme}'", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertTrue(dungeon.Rooms.Count > 0, 
                    $"Dungeon with theme '{theme}' should generate rooms", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }
    }
}

