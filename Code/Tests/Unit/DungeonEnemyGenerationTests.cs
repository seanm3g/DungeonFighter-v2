using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for dungeon and enemy generation
    /// Tests dungeon creation, room generation, enemy spawning, and scaling
    /// </summary>
    public static class DungeonEnemyGenerationTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Dungeon and Enemy Generation Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestDungeonCreation();
            TestDungeonGeneration();
            TestRoomGeneration();
            TestEnemyGeneration();
            TestEnemyScaling();
            TestEnemyLevelDistribution();
            TestDungeonThemeConsistency();
            TestMultipleDungeonGeneration();

            TestBase.PrintSummary("Dungeon and Enemy Generation Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestDungeonCreation()
        {
            Console.WriteLine("--- Testing Dungeon Creation ---");

            var dungeon = new Dungeon("Test Dungeon", 1, 5, "Forest");
            
            TestBase.AssertNotNull(dungeon, 
                "Dungeon should be created", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual("Test Dungeon", dungeon.Name, 
                "Dungeon should have correct name", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(dungeon.MinLevel == 1 && dungeon.MaxLevel == 5, 
                "Dungeon should have correct level range", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual("Forest", dungeon.Theme, 
                "Dungeon should have correct theme", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDungeonGeneration()
        {
            Console.WriteLine("\n--- Testing Dungeon Generation ---");

            var dungeon = new Dungeon("Test Dungeon", 1, 5, "Forest");
            dungeon.Generate();

            TestBase.AssertTrue(dungeon.Rooms.Count > 0, 
                $"Dungeon should generate rooms, got {dungeon.Rooms.Count}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Verify all rooms are non-null
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
            int totalRooms = dungeon.Rooms.Count;

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

            // At least one room should be hostile
            TestBase.AssertTrue(hasHostileRoom, 
                "Dungeon should have at least one hostile room", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(totalRooms > 0, 
                "Dungeon should have at least one room", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEnemyGeneration()
        {
            Console.WriteLine("\n--- Testing Enemy Generation ---");

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
                        
                        foreach (var enemy in enemies)
                        {
                            TestBase.AssertNotNull(enemy, 
                                "Enemy should not be null", 
                                ref _testsRun, ref _testsPassed, ref _testsFailed);

                            TestBase.AssertTrue(enemy.Level >= 1, 
                                $"Enemy level should be >= 1, got {enemy.Level}", 
                                ref _testsRun, ref _testsPassed, ref _testsFailed);

                            TestBase.AssertTrue(enemy.MaxHealth > 0, 
                                $"Enemy max health should be > 0, got {enemy.MaxHealth}", 
                                ref _testsRun, ref _testsPassed, ref _testsFailed);
                        }
                    }
                }
            }

            // At least one enemy should spawn in hostile rooms
            TestBase.AssertTrue(totalEnemies > 0, 
                $"Dungeon should spawn enemies, got {totalEnemies}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEnemyScaling()
        {
            Console.WriteLine("\n--- Testing Enemy Scaling ---");

            // Test low level dungeon
            var lowDungeon = new Dungeon("Low Level", 1, 3, "Forest");
            lowDungeon.Generate();

            // Test high level dungeon
            var highDungeon = new Dungeon("High Level", 15, 20, "Forest");
            highDungeon.Generate();

            int lowLevelEnemies = 0;
            int highLevelEnemies = 0;

            foreach (var room in lowDungeon.Rooms)
            {
                if (room != null)
                {
                    var enemies = room.GetEnemies();
                    if (enemies != null)
                    {
                        lowLevelEnemies += enemies.Count;
                    }
                }
            }

            foreach (var room in highDungeon.Rooms)
            {
                if (room != null)
                {
                    var enemies = room.GetEnemies();
                    if (enemies != null)
                    {
                        highLevelEnemies += enemies.Count;
                    }
                }
            }

            TestBase.AssertTrue(lowLevelEnemies > 0, 
                "Low level dungeon should have enemies", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(highLevelEnemies > 0, 
                "High level dungeon should have enemies", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEnemyLevelDistribution()
        {
            Console.WriteLine("\n--- Testing Enemy Level Distribution ---");

            var dungeon = new Dungeon("Test Dungeon", 5, 10, "Forest");
            dungeon.Generate();

            var enemyLevels = new List<int>();
            foreach (var room in dungeon.Rooms)
            {
                if (room != null)
                {
                    var enemies = room.GetEnemies();
                    if (enemies != null)
                    {
                        foreach (var enemy in enemies)
                        {
                            enemyLevels.Add(enemy.Level);
                        }
                    }
                }
            }

            if (enemyLevels.Count > 0)
            {
                int minLevel = enemyLevels.Min();
                int maxLevel = enemyLevels.Max();

                TestBase.AssertTrue(minLevel >= 5, 
                    $"Minimum enemy level should be >= 5, got {minLevel}", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertTrue(maxLevel <= 10, 
                    $"Maximum enemy level should be <= 10, got {maxLevel}", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestDungeonThemeConsistency()
        {
            Console.WriteLine("\n--- Testing Dungeon Theme Consistency ---");

            var themes = new[] { "Forest", "Lava", "Crypt", "Crystal", "Temple" };

            foreach (var theme in themes)
            {
                var dungeon = new Dungeon($"Test {theme}", 1, 5, theme);
                dungeon.Generate();

                TestBase.AssertTrue(dungeon.Rooms.Count > 0, 
                    $"{theme} dungeon should generate rooms", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertEqual(theme, dungeon.Theme, 
                    $"Dungeon should maintain {theme} theme", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestMultipleDungeonGeneration()
        {
            Console.WriteLine("\n--- Testing Multiple Dungeon Generation ---");

            var dungeons = new List<Dungeon>();
            for (int i = 0; i < 5; i++)
            {
                var dungeon = new Dungeon($"Dungeon {i}", 1, 5, "Forest");
                dungeon.Generate();
                dungeons.Add(dungeon);
            }

            TestBase.AssertTrue(dungeons.Count == 5, 
                "Should create 5 dungeons", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            foreach (var dungeon in dungeons)
            {
                TestBase.AssertTrue(dungeon.Rooms.Count > 0, 
                    "Each dungeon should have rooms", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }
    }
}

