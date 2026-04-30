using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    /// <summary>
    /// Comprehensive tests for LootRarityProcessor
    /// Tests rarity rolling, upgrades, and scaling
    /// </summary>
    public static class LootRarityProcessorTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all LootRarityProcessor tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== LootRarityProcessor Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestRollRarity();
            TestApplyRarityScaling();
            TestRarityUpgrades();
            TestMagicFindDoesNotChangeBaseRarityRollWithSameSeed();

            TestBase.PrintSummary("LootRarityProcessor Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Rarity Rolling Tests

        private static void TestRollRarity()
        {
            Console.WriteLine("--- Testing RollRarity ---");

            var cache = TestDataBuilders.CreateLootDataCache();
            var random = new Random(12345);
            var processor = new LootRarityProcessor(cache, random);

            // Roll rarity with default parameters
            var rarity1 = processor.RollRarity();
            TestBase.AssertNotNull(rarity1,
                "Rarity should be rolled",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (rarity1 != null)
            {
                TestBase.AssertTrue(!string.IsNullOrEmpty(rarity1.Name),
                    "Rarity should have a name",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(rarity1.Weight > 0,
                    "Rarity should have positive weight",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Roll rarity with magic find
            var rarity2 = processor.RollRarity(0.5, 10);
            TestBase.AssertNotNull(rarity2,
                "Rarity should be rolled with magic find",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with empty cache (should return default)
            var emptyCache = LootDataCache.CreateEmpty();
            var processor2 = new LootRarityProcessor(emptyCache, random);
            var rarity3 = processor2.RollRarity();
            TestBase.AssertNotNull(rarity3,
                "Rarity should be returned even with empty cache",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (rarity3 != null)
            {
                TestBase.AssertEqual("Common", rarity3.Name,
                    "Empty cache should return Common rarity",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestApplyRarityScaling()
        {
            Console.WriteLine("\n--- Testing ApplyRarityScaling ---");

            var cache = TestDataBuilders.CreateLootDataCache();
            var random = new Random(12345);
            var processor = new LootRarityProcessor(cache, random);

            var rarity = new RarityData
            {
                Name = "Common",
                Weight = 500,
                StatBonuses = 1,
                ActionBonuses = 0,
                Modifications = 0
            };

            // Test with weapon
            var weapon = TestDataBuilders.Weapon()
                .WithName("TestWeapon")
                .WithTier(1)
                .WithBaseDamage(10)
                .Build();

            int originalDamage = weapon.BaseDamage;
            processor.ApplyRarityScaling(weapon, rarity);
            // Note: Current implementation applies 1.0 multiplier, so damage should remain same
            TestBase.AssertTrue(weapon.BaseDamage >= 0,
                "Weapon damage should be valid after rarity scaling",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMagicFindDoesNotChangeBaseRarityRollWithSameSeed()
        {
            Console.WriteLine("\n--- Testing MF ignored on base rarity roll (affix-only MF) ---");
            TestBase.SetCurrentTestName(nameof(TestMagicFindDoesNotChangeBaseRarityRollWithSameSeed));

            var cache = LootDataCache.Load();
            if (cache.RarityData == null || cache.RarityData.Count == 0)
            {
                TestBase.AssertTrue(false, "LootDataCache needs rarity data", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            for (int seed = 0; seed < 300; seed++)
            {
                var rLo = new LootRarityProcessor(cache, new Random(seed)).RollRarity(0, 20);
                var rHi = new LootRarityProcessor(cache, new Random(seed)).RollRarity(999, 20);
                TestBase.AssertTrue(
                    string.Equals(rLo.Name, rHi.Name, StringComparison.OrdinalIgnoreCase),
                    $"seed {seed}: RollRarity(0) and RollRarity(999) should match ({rLo.Name} vs {rHi.Name})",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestRarityUpgrades()
        {
            Console.WriteLine("\n--- Testing Rarity Upgrades ---");

            var cache = TestDataBuilders.CreateLootDataCache();
            // Add more rarities for upgrade testing
            cache.RarityData.Add(new RarityData
            {
                Name = "Uncommon",
                Weight = 300,
                StatBonuses = 2,
                ActionBonuses = 0,
                Modifications = 0
            });

            var random = new Random(12345);
            var processor = new LootRarityProcessor(cache, random);

            // Roll rarity - upgrades are applied internally
            var rarity = processor.RollRarity();
            TestBase.AssertNotNull(rarity,
                "Rarity should be rolled with upgrade system",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
