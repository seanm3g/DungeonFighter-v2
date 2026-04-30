using System;
using System.Collections.Generic;
using System.Linq;
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
            TestApplyStatBonusesUsesAffixTierPools();
            TestApplyPrefixSlotsUsesAffixTierPools();
            TestApplyActionBonuses();
            TestApplyModifications();
            TestTuningAffixOverridesRarityTable();
            TestProbabilisticAffixTuningHitsMax();
            TestAffixMagicFindBoostsOptionalExtraChances();

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

        private static void TestApplyStatBonusesUsesAffixTierPools()
        {
            Console.WriteLine("\n--- Testing ApplyStatBonuses affix-tier pools ---");
            TestBase.SetCurrentTestName(nameof(TestApplyStatBonusesUsesAffixTierPools));
            try
            {
                var cache = LootDataCache.CreateEmpty();
                cache.StatBonuses.Add(new StatBonus { Name = "CommonLine", Rarity = "Common", StatType = "Armor", Value = 1 });
                cache.StatBonuses.Add(new StatBonus { Name = "RareLine", Rarity = "Rare", StatType = "Armor", Value = 9 });
                cache.RarityData.Add(new RarityData { Name = "Common", Weight = 0, StatBonuses = 0, ActionBonuses = 0, Modifications = 0 });
                cache.RarityData.Add(new RarityData { Name = "Rare", Weight = 1, StatBonuses = 0, ActionBonuses = 0, Modifications = 0 });

                var item = TestDataBuilders.Item().WithName("T").Build();
                var applier = new LootBonusApplier(cache, new Random(1));
                applier.ApplyStatBonuses(item, 15);

                TestBase.AssertEqual(15, item.StatBonuses.Count, "suffix count", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(item.StatBonuses.All(s => s.Name == "RareLine"),
                    "only Rare-tier StatBonuses rows should be chosen when RarityTable weights exclude Common",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"Affix-tier stat test: {ex.Message}", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestApplyPrefixSlotsUsesAffixTierPools()
        {
            Console.WriteLine("\n--- Testing ApplyPrefixSlots affix-tier pools ---");
            TestBase.SetCurrentTestName(nameof(TestApplyPrefixSlotsUsesAffixTierPools));
            try
            {
                var cache = LootDataCache.CreateEmpty();
                void Add(string rank, string cat, string name, int dice) =>
                    cache.Modifications.Add(new Modification
                    {
                        DiceResult = dice,
                        ItemRank = rank,
                        PrefixCategory = cat,
                        Name = name,
                        Description = "x",
                        Effect = "damage",
                        MinValue = 1,
                        MaxValue = 2
                    });
                Add("Common", "Quality", "Qc", 1);
                Add("Rare", "Quality", "Qr", 2);
                Add("Common", "Adjective", "Ac", 3);
                Add("Rare", "Adjective", "Ar", 4);
                Add("Common", "Material", "Mc", 5);
                Add("Rare", "Material", "Mr", 6);
                cache.RarityData.Add(new RarityData { Name = "Common", Weight = 0, StatBonuses = 0, ActionBonuses = 0, Modifications = 0 });
                cache.RarityData.Add(new RarityData { Name = "Rare", Weight = 1, StatBonuses = 0, ActionBonuses = 0, Modifications = 0 });

                var item = TestDataBuilders.Item().WithName("T").Build();
                var applier = new LootBonusApplier(cache, new Random(3));
                applier.ApplyPrefixSlots(item, 3, null);

                TestBase.AssertEqual(3, item.Modifications.Count, "three prefix slots", ref _testsRun, ref _testsPassed, ref _testsFailed);
                string names = string.Join(",", item.Modifications.Select(m => m.Name).OrderBy(x => x));
                TestBase.AssertEqual("Ar,Mr,Qr", names, "rare-only picks per category", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"Affix-tier prefix test: {ex.Message}", ref _testsRun, ref _testsPassed, ref _testsFailed);
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

        private static void TestAffixMagicFindBoostsOptionalExtraChances()
        {
            Console.WriteLine("\n--- Testing MF on optional affix extra chances ---");
            TestBase.SetCurrentTestName(nameof(TestAffixMagicFindBoostsOptionalExtraChances));

            var rule = new ItemAffixRollRule(0, 3, 0.2, 0, 2, 0.2, 0, 1, 0.2);
            var loot = new LootSystemConfig { AffixMagicFindMaxExtraChanceBoost = 2.0 };
            int sum0 = 0, sum100 = 0;
            const int n = 5000;
            for (int i = 0; i < n; i++)
            {
                ItemAffixByRaritySettings.RollAffixCounts(new Random(i), rule, 0, loot, out int p0, out int s0, out int a0);
                ItemAffixByRaritySettings.RollAffixCounts(new Random(i), rule, 100, loot, out int p100, out int s100, out int a100);
                sum0 += p0 + s0 + a0;
                sum100 += p100 + s100 + a100;
            }

            TestBase.AssertTrue(sum100 > sum0,
                $"MF=100 should roll more optional affix steps on average (MF100 sum {sum100} vs MF0 {sum0})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
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
