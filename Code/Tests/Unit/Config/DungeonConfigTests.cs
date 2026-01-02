using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Config
{
    /// <summary>
    /// Comprehensive tests for DungeonConfig
    /// Tests dungeon scaling and generation configuration
    /// </summary>
    public static class DungeonConfigTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all DungeonConfig tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== DungeonConfig Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestDungeonScalingConfig();
            TestDungeonGenerationConfig();

            TestBase.PrintSummary("DungeonConfig Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Configuration Tests

        private static void TestDungeonScalingConfig()
        {
            Console.WriteLine("--- Testing DungeonScalingConfig ---");

            var config = new DungeonScalingConfig
            {
                RoomCountBase = 3,
                RoomCountPerLevel = 0.5,
                EnemyCountPerRoom = 1,
                BossRoomChance = 0.1,
                TrapRoomChance = 0.2,
                TreasureRoomChance = 0.15
            };

            TestBase.AssertEqual(3, config.RoomCountBase,
                "Room count base should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(config.RoomCountPerLevel > 0,
                "Room count per level should be positive",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(config.BossRoomChance >= 0 && config.BossRoomChance <= 1,
                "Boss room chance should be between 0 and 1",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDungeonGenerationConfig()
        {
            Console.WriteLine("\n--- Testing DungeonGenerationConfig ---");

            var config = new DungeonGenerationConfig();

            TestBase.AssertTrue(config.minRooms >= 2,
                "Min rooms should be at least 2",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(config.roomCountScaling > 0,
                "Room count scaling should be positive",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(config.hostileRoomChance >= 0 && config.hostileRoomChance <= 1,
                "Hostile room chance should be between 0 and 1",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertNotNull(config.EquipmentSlots,
                "Equipment slots list should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(config.EquipmentSlots.Count > 0,
                "Equipment slots should have items",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
