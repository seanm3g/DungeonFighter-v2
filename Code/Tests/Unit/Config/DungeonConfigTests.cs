using System;
using RPGGame;
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
            TestDungeonScalingSanitizer();
            TestDungeonGenerationConfig();
            TestRoomCountRoll();

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

        private static void TestRoomCountRoll()
        {
            Console.WriteLine("--- Testing room count roll ---");
            var cfg = new DungeonScalingConfig { RoomCountBase = 2, RoomCountPerLevel = 0.5, MaxRoomCount = 4 };

            var l4 = cfg.GetRoomCountRange(4);
            TestBase.AssertTrue(l4.Low == 2 && l4.High == 2 && l4.HighChance == 0, "L4 always 2 rooms", ref _testsRun, ref _testsPassed, ref _testsFailed);
            var l5 = cfg.GetRoomCountRange(5);
            TestBase.AssertTrue(l5.Low == 2 && l5.High == 3 && Math.Abs(l5.HighChance - 0.5) < 1e-9, "L5 rolls 2 or 3 rooms at 50%", ref _testsRun, ref _testsPassed, ref _testsFailed);
            var l6 = cfg.GetRoomCountRange(6);
            TestBase.AssertTrue(l6.Low == 3 && l6.HighChance == 0, "L6 always 3 rooms", ref _testsRun, ref _testsPassed, ref _testsFailed);
            var l7 = cfg.GetRoomCountRange(7);
            TestBase.AssertTrue(l7.Low == 3 && l7.High == 4, "L7 rolls 3 or 4 rooms", ref _testsRun, ref _testsPassed, ref _testsFailed);
            var l20 = cfg.GetRoomCountRange(20);
            TestBase.AssertTrue(l20.Low == 4 && l20.High == 4, "high levels capped at MaxRoomCount", ref _testsRun, ref _testsPassed, ref _testsFailed);
            var l1 = cfg.GetRoomCountRange(1);
            TestBase.AssertTrue(l1.Low == 2 && l1.High == 2, "low levels floored at RoomCountBase", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var rng = new Random(7);
            int threes = 0;
            for (int i = 0; i < 2000; i++)
                if (cfg.RollRoomCount(5, rng) == 3) threes++;
            TestBase.AssertTrue(threes > 850 && threes < 1150, $"L5 roll is about half 3-room ({threes}/2000)", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDungeonScalingSanitizer()
        {
            Console.WriteLine("\n--- Testing DungeonScalingConfig.EnsureSensibleDefaults ---");

            var config = new DungeonScalingConfig { RoomCountBase = 0, EnemyCountPerRoom = 0, RoomCountPerLevel = 0 };
            config.EnsureSensibleDefaults();
            TestBase.AssertTrue(config.RoomCountBase > 0,
                "RoomCountBase should be repaired",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(config.EnemyCountPerRoom > 0,
                "EnemyCountPerRoom should be repaired",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(config.RoomCountPerLevel > 0,
                "RoomCountPerLevel should be repaired",
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
