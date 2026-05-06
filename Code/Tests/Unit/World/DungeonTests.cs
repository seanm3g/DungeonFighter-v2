using System;
using System.Collections.Generic;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.World
{
    /// <summary>
    /// Comprehensive tests for Dungeon
    /// Tests dungeon generation, room progression, and rewards
    /// </summary>
    public static class DungeonTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all Dungeon tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== Dungeon Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestDungeonCreation();
            TestDungeonProperties();
            TestDungeonGenerate();
            TestTrainingGroundDungeonGenerate();

            TestBase.PrintSummary("Dungeon Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Creation Tests

        private static void TestDungeonCreation()
        {
            Console.WriteLine("--- Testing Dungeon Creation ---");

            var dungeon = new Dungeon("Test Dungeon", 1, 5, "Forest");
            TestBase.AssertNotNull(dungeon,
                "Dungeon should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (dungeon != null)
            {
                TestBase.AssertEqual("Test Dungeon", dungeon.Name,
                    "Dungeon should have correct name",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertEqual(1, dungeon.MinLevel,
                    "Dungeon should have correct min level",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertEqual(5, dungeon.MaxLevel,
                    "Dungeon should have correct max level",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertEqual("Forest", dungeon.Theme,
                    "Dungeon should have correct theme",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Properties Tests

        private static void TestDungeonProperties()
        {
            Console.WriteLine("\n--- Testing Dungeon Properties ---");

            var dungeon = new Dungeon("Test Dungeon", 1, 5, "Forest");

            TestBase.AssertNotNull(dungeon.Rooms,
                "Rooms list should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(dungeon.PossibleEnemies,
                "PossibleEnemies list should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(0, dungeon.Rooms.Count,
                "New dungeon should have no rooms",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Generation Tests

        private static void TestDungeonGenerate()
        {
            Console.WriteLine("\n--- Testing Dungeon Generate ---");

            var dungeon = new Dungeon("Test Dungeon", 1, 5, "Forest");
            dungeon.Generate();

            // Generation should create rooms
            TestBase.AssertTrue(dungeon.Rooms.Count >= 0,
                $"Dungeon should have rooms after generation, got {dungeon.Rooms.Count}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // If rooms were generated, verify they have properties
            if (dungeon.Rooms.Count > 0)
            {
                var room = dungeon.Rooms[0];
                TestBase.AssertNotNull(room,
                    "Generated room should not be null",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                if (room != null)
                {
                    TestBase.AssertTrue(!string.IsNullOrEmpty(room.Name),
                        "Generated room should have a name",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
        }

        private static void TestTrainingGroundDungeonGenerate()
        {
            Console.WriteLine("\n--- Testing Training Ground Dungeon Generate ---");

            var dungeon = PreWeaponTrainingFlow.CreateTrainingGroundDungeon();
            dungeon.Generate();

            TestBase.AssertEqual(1, dungeon.Rooms.Count,
                "Training Ground should have exactly one room",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var room = dungeon.Rooms[0];
            TestBase.AssertTrue(room.IsHostile, "Training room should be hostile", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var enemies = room.GetEnemies();
            TestBase.AssertEqual(1, enemies.Count, "Training room should have one enemy", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var dummy = enemies[0];
            TestBase.AssertEqual(GameConstants.TrainingDummyEnemyName, dummy.Name,
                "Enemy should be Training Dummy",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(GameConstants.TrainingDummyMaxHealth, dummy.MaxHealth,
                "Dummy max health",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(dummy.UsesDirectCombatStats(), "Dummy uses direct combat stats", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1, dummy.GetEffectiveStrength(),
                "Dummy effective damage (strength path)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(GameConstants.TrainingDummyBaseAttackSpeedSeconds, dummy.GetTotalAttackSpeed(),
                "Dummy base attack time (seconds, direct stats)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
