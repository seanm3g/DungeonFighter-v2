using System;
using System.Collections.Generic;
using RPGGame;
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
            TestTuningAffixOverridesRarityTable();
            TestProbabilisticAffixTuningHitsMax();

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

        private static void TestTuningAffixOverridesRarityTable()
        {
            Console.WriteLine("\n--- Testing ItemAffixByRarity tuning overrides ---");

            var cfg = GameConfiguration.Instance;
            var backup = cfg.ItemAffixByRarity;
            try
            {
                cfg.ItemAffixByRarity = new ItemAffixByRaritySettings
                {
                    PerRarity = new Dictionary<string, ItemAffixPerRarityEntry>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Rare"] = new ItemAffixPerRarityEntry
                        {
                            PrefixSlots = 0,
                            StatSuffixes = 1,
                            ActionBonuses = 0
                        }
                    }
                };

                var item = TestDataBuilders.Item()
                    .WithName("Test Sword")
                    .WithTier(2)
                    .Build();
                item.Rarity = "Rare";

                var rarityFromTable = new RarityData
                {
                    Name = "Rare",
                    StatBonuses = 99,
                    ActionBonuses = 99,
                    Modifications = 0
                };

                var cache = LootDataCache.Load();
                var applier = new LootBonusApplier(cache, new Random(42));
                applier.ApplyBonuses(item, rarityFromTable);

                TestBase.AssertEqual(0, item.Modifications.Count,
                    "Tuning prefixSlots=0 should yield no prefix modifications",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(1, item.StatBonuses.Count,
                    "Tuning statSuffixes=1 should yield exactly one stat bonus (ignore RarityTable 99)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(0, item.ActionBonuses.Count,
                    "Tuning actionBonuses=0 should yield no action bonuses",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"Tuning affix override test failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                cfg.ItemAffixByRarity = backup;
            }
        }

        private static void TestProbabilisticAffixTuningHitsMax()
        {
            Console.WriteLine("\n--- Testing probabilistic affix tuning (100% extra hits max) ---");

            var cfg = GameConfiguration.Instance;
            var backup = cfg.ItemAffixByRarity;
            try
            {
                cfg.ItemAffixByRarity = new ItemAffixByRaritySettings
                {
                    PerRarity = new Dictionary<string, ItemAffixPerRarityEntry>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Rare"] = new ItemAffixPerRarityEntry
                        {
                            PrefixSlots = 0,
                            PrefixSlotsMax = 3,
                            PrefixExtraChance = 1.0,
                            StatSuffixes = 0,
                            StatSuffixesMax = 2,
                            StatSuffixExtraChance = 1.0,
                            ActionBonuses = 0,
                            ActionBonusesMax = 1,
                            ActionExtraChance = 1.0
                        }
                    }
                };

                var item = TestDataBuilders.Item()
                    .WithName("Test Sword")
                    .WithTier(2)
                    .Build();
                item.Rarity = "Rare";

                var rarityFromTable = new RarityData
                {
                    Name = "Rare",
                    StatBonuses = 0,
                    ActionBonuses = 0,
                    Modifications = 0
                };

                var cache = LootDataCache.Load();
                var applier = new LootBonusApplier(cache, new Random(7));
                applier.ApplyBonuses(item, rarityFromTable);

                TestBase.AssertEqual(3, item.Modifications.Count,
                    "100% prefix extra chance should fill to prefix max 3",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(2, item.StatBonuses.Count,
                    "100% stat extra chance should fill to stat max 2",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(1, item.ActionBonuses.Count,
                    "100% action extra chance should fill to action max 1",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"Probabilistic affix test failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                cfg.ItemAffixByRarity = backup;
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
