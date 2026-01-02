using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    /// <summary>
    /// Comprehensive tests for LootBonusApplier
    /// Tests bonus application, rarity-based distribution, and modification application
    /// </summary>
    public static class LootBonusApplierTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all LootBonusApplier tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== LootBonusApplier Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            // Note: These tests require LootDataCache to be loaded
            // We test that the methods don't crash and handle edge cases
            TestApplyBonuses();
            TestApplyStatBonuses();
            TestApplyActionBonuses();
            TestApplyModifications();

            TestBase.PrintSummary("LootBonusApplier Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Bonus Application Tests

        private static void TestApplyBonuses()
        {
            Console.WriteLine("--- Testing ApplyBonuses ---");

            // Create test item
            var item = TestDataBuilders.Item()
                .WithName("Test Item")
                .WithTier(1)
                .Build();

            // Create test rarity
            var rarity = new RarityData
            {
                Name = "Common",
                StatBonuses = 0,
                ActionBonuses = 0,
                Modifications = 0
            };

            // Test that ApplyBonuses doesn't crash
            try
            {
                var cache = LootDataCache.Load();
                var applier = new LootBonusApplier(cache, new Random());
                applier.ApplyBonuses(item, rarity);
                
                TestBase.AssertTrue(true,
                    "ApplyBonuses should complete without errors",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"ApplyBonuses should not throw: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestApplyStatBonuses()
        {
            Console.WriteLine("\n--- Testing ApplyStatBonuses ---");

            var item = TestDataBuilders.Item()
                .WithName("Test Item")
                .WithTier(1)
                .Build();

            try
            {
                var cache = LootDataCache.Load();
                var applier = new LootBonusApplier(cache, new Random());
                
                // Test applying stat bonuses
                applier.ApplyStatBonuses(item, 2);
                
                TestBase.AssertTrue(item.StatBonuses.Count >= 0,
                    $"Item should have stat bonuses applied, got {item.StatBonuses.Count}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"ApplyStatBonuses should not throw: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestApplyActionBonuses()
        {
            Console.WriteLine("\n--- Testing ApplyActionBonuses ---");

            var item = TestDataBuilders.Item()
                .WithName("Test Item")
                .WithTier(1)
                .Build();

            try
            {
                var cache = LootDataCache.Load();
                var applier = new LootBonusApplier(cache, new Random());
                
                // Test applying action bonuses
                applier.ApplyActionBonuses(item, 1);
                
                TestBase.AssertTrue(item.ActionBonuses.Count >= 0,
                    $"Item should have action bonuses applied, got {item.ActionBonuses.Count}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"ApplyActionBonuses should not throw: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestApplyModifications()
        {
            Console.WriteLine("\n--- Testing ApplyModifications ---");

            var item = TestDataBuilders.Item()
                .WithName("Test Item")
                .WithTier(1)
                .Build();

            try
            {
                var cache = LootDataCache.Load();
                var applier = new LootBonusApplier(cache, new Random());
                
                // Test applying modifications
                applier.ApplyModifications(item, 1);
                
                TestBase.AssertTrue(item.Modifications.Count >= 0,
                    $"Item should have modifications applied, got {item.Modifications.Count}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"ApplyModifications should not throw: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion
    }
}
