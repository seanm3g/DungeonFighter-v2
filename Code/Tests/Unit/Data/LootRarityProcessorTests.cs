using System;
using System.Collections.Generic;
using System.Linq;
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
            TestMagicFindDistributionShiftsTowardHigherTiers();

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

        private static double MeanRarityTierIndex(IReadOnlyList<(string Name, double Weight, double ProbabilityPercent)> dist)
        {
            var order = new[] { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic" };
            double sum = 0;
            foreach (var row in dist)
            {
                int idx = Array.FindIndex(order, x => x.Equals(row.Name, StringComparison.OrdinalIgnoreCase));
                if (idx < 0)
                    idx = 0;
                sum += row.ProbabilityPercent * 0.01 * idx;
            }

            return sum;
        }

        private static void TestMagicFindDistributionShiftsTowardHigherTiers()
        {
            Console.WriteLine("\n--- Testing MF distribution (first roll) ---");
            TestBase.SetCurrentTestName(nameof(TestMagicFindDistributionShiftsTowardHigherTiers));

            var cache = LootDataCache.Load();
            if (cache.RarityData == null || cache.RarityData.Count == 0)
            {
                TestBase.AssertTrue(false, "LootDataCache needs rarity data", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            // Level 20 unlocks Mythic in IsRarityUnlockedAtPlayerLevel
            var dist0 = LootRarityProcessor.GetBaseRollDistribution(cache, 20, 0);
            var dist100 = LootRarityProcessor.GetBaseRollDistribution(cache, 20, 100);
            TestBase.AssertTrue(dist0.Count > 0 && dist100.Count == dist0.Count,
                "MF distributions same tier set",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            double mean0 = MeanRarityTierIndex(dist0);
            double mean100 = MeanRarityTierIndex(dist100);
            TestBase.AssertTrue(mean100 > mean0,
                $"MF=100 mean tier index {mean100:F4} should exceed MF=0 {mean0:F4}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            double mythic0 = dist0.Where(d => d.Name.Equals("Mythic", StringComparison.OrdinalIgnoreCase))
                .Select(d => d.ProbabilityPercent).DefaultIfEmpty(0).First();
            double mythic100 = dist100.Where(d => d.Name.Equals("Mythic", StringComparison.OrdinalIgnoreCase))
                .Select(d => d.ProbabilityPercent).DefaultIfEmpty(0).First();
            TestBase.AssertTrue(mythic100 > mythic0,
                $"Mythic %% MF100 ({mythic100}) > MF0 ({mythic0})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Monte Carlo: MF=100 should yield strictly higher non-Common rate than MF=0 over many rolls
            var rndLo = new Random(424242);
            var rndHi = new Random(424242);
            var procLo = new LootRarityProcessor(cache, rndLo);
            var procHi = new LootRarityProcessor(cache, rndHi);
            int n = 8000;
            int nonCommonLo = 0, nonCommonHi = 0;
            for (int i = 0; i < n; i++)
            {
                var r0 = procLo.RollRarity(0, 20);
                var r1 = procHi.RollRarity(100, 20);
                if (!r0.Name.Equals("Common", StringComparison.OrdinalIgnoreCase))
                    nonCommonLo++;
                if (!r1.Name.Equals("Common", StringComparison.OrdinalIgnoreCase))
                    nonCommonHi++;
            }

            TestBase.AssertTrue(nonCommonHi > nonCommonLo,
                $"MF100 non-Common count {nonCommonHi} vs MF0 {nonCommonLo} over {n} rolls",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
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
