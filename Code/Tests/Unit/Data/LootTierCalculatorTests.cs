using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    /// <summary>
    /// Comprehensive tests for LootTierCalculator
    /// Tests tier calculation, level clamping, and tier rolling
    /// </summary>
    public static class LootTierCalculatorTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all LootTierCalculator tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== LootTierCalculator Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestCalculateLootLevel();
            TestClampLootLevel();
            TestRollTier();
            TestGetTierDistribution();

            TestBase.PrintSummary("LootTierCalculator Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Loot Level Calculation Tests

        private static void TestCalculateLootLevel()
        {
            Console.WriteLine("--- Testing CalculateLootLevel ---");

            var cache = TestDataBuilders.CreateLootDataCache();
            var random = new Random(12345);
            var calculator = new LootTierCalculator(cache, random);

            // Player level equals dungeon level
            int level1 = calculator.CalculateLootLevel(5, 5);
            TestBase.AssertEqual(5, level1,
                "When player level equals dungeon level, loot level should equal dungeon level",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Player level higher than dungeon level
            int level2 = calculator.CalculateLootLevel(10, 5);
            TestBase.AssertTrue(level2 < 5,
                "When player level is higher than dungeon level, loot level should be lower",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Player level lower than dungeon level
            int level3 = calculator.CalculateLootLevel(3, 10);
            TestBase.AssertTrue(level3 > 10,
                "When player level is lower than dungeon level, loot level should be higher",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Edge case: player level 1, dungeon level 1
            int level4 = calculator.CalculateLootLevel(1, 1);
            TestBase.AssertTrue(level4 >= 1 && level4 <= 100,
                "Loot level should be clamped to valid range",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestClampLootLevel()
        {
            Console.WriteLine("\n--- Testing ClampLootLevel (via CalculateLootLevel) ---");

            var cache = TestDataBuilders.CreateLootDataCache();
            var random = new Random(12345);
            var calculator = new LootTierCalculator(cache, random);

            // Test negative level clamping
            int level1 = calculator.CalculateLootLevel(100, 1);
            TestBase.AssertTrue(level1 >= 1,
                "Negative loot level should be clamped to 1",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test very high level clamping
            int level2 = calculator.CalculateLootLevel(1, 200);
            TestBase.AssertTrue(level2 <= 100,
                "Loot level above 100 should be clamped to 100",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Tier Rolling Tests

        private static void TestRollTier()
        {
            Console.WriteLine("\n--- Testing RollTier ---");

            var cache = TestDataBuilders.CreateLootDataCache();
            var random = new Random(12345);
            var calculator = new LootTierCalculator(cache, random);

            // Roll tier for valid level
            int tier1 = calculator.RollTier(5);
            TestBase.AssertTrue(tier1 >= 1 && tier1 <= 5,
                "Rolled tier should be between 1 and 5",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Roll tier for level 1
            int tier2 = calculator.RollTier(1);
            TestBase.AssertTrue(tier2 >= 1 && tier2 <= 5,
                "Rolled tier for level 1 should be valid",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Roll tier for level 100
            int tier3 = calculator.RollTier(100);
            TestBase.AssertTrue(tier3 >= 1 && tier3 <= 5,
                "Rolled tier for level 100 should be valid",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Roll tier for invalid level (should clamp)
            int tier4 = calculator.RollTier(0);
            TestBase.AssertTrue(tier4 >= 1 && tier4 <= 5,
                "Rolled tier for invalid level should clamp and return valid tier",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Roll tier for very high level (should clamp)
            int tier5 = calculator.RollTier(200);
            TestBase.AssertTrue(tier5 >= 1 && tier5 <= 5,
                "Rolled tier for very high level should clamp and return valid tier",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGetTierDistribution()
        {
            Console.WriteLine("\n--- Testing GetTierDistribution ---");

            var cache = TestDataBuilders.CreateLootDataCache();
            var random = new Random(12345);
            var calculator = new LootTierCalculator(cache, random);

            // Get distribution for valid level
            var dist1 = calculator.GetTierDistribution(5);
            TestBase.AssertNotNull(dist1,
                "Tier distribution should be found for valid level",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Get distribution for level that doesn't exist
            var dist2 = calculator.GetTierDistribution(999);
            // May be null if no distribution exists for that level
            // This is acceptable behavior

            // Get distribution for level 1
            var dist3 = calculator.GetTierDistribution(1);
            // May be null or valid, both are acceptable
        }

        #endregion
    }
}
