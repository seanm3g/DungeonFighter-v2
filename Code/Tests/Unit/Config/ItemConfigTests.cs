using System;
using System.Collections.Generic;
using System.Text.Json;
using RPGGame;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Config
{
    /// <summary>
    /// Comprehensive tests for ItemConfig
    /// Tests item scaling, rarity tables, and tier distributions
    /// </summary>
    public static class ItemConfigTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all ItemConfig tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== ItemConfig Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestItemScalingConfig();
            TestItemScalingSanitizer();
            TestWeaponScalingConfig();
            TestWeaponScalingSanitizer();
            TestEquipmentScalingConfig();
            TestEquipmentScalingSanitizer();
            TestRarityScalingConfig();
            TestLootSystemConfig();
            TestLootSystemSanitizer();
            TestItemAffixByRaritySettingsJson();
            TestRollUpRule();

            TestBase.PrintSummary("ItemConfig Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region ItemScalingConfig Tests

        private static void TestItemScalingConfig()
        {
            Console.WriteLine("--- Testing ItemScalingConfig ---");

            var config = new ItemScalingConfig
            {
                GlobalDamageMultiplier = 1.0,
                WeaponDamagePerTier = 5,
                ArmorValuePerTier = 3,
                MaxTier = 5
            };

            TestBase.AssertNotNull(config.StartingWeaponDamage,
                "StartingWeaponDamage should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(config.TierDamageRanges,
                "TierDamageRanges should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(5, config.MaxTier,
                "MaxTier should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        private static void TestItemScalingSanitizer()
        {
            Console.WriteLine("\n--- Testing ItemScalingConfig.EnsureSanitizedWeaponScalingDefaults ---");

            var config = new ItemScalingConfig { GlobalDamageMultiplier = 0, WeaponDamagePerTier = 0, MaxTier = 0 };
            config.EnsureSanitizedWeaponScalingDefaults();
            TestBase.AssertEqual(1.0, config.GlobalDamageMultiplier,
                "GlobalDamageMultiplier should become 1.0",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(config.MaxTier > 0,
                "MaxTier should be positive",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #region WeaponScalingConfig Tests

        private static void TestWeaponScalingConfig()
        {
            Console.WriteLine("\n--- Testing WeaponScalingConfig ---");

            var config = new WeaponScalingConfig
            {
                GlobalDamageMultiplier = 1.0
            };

            TestBase.AssertNotNull(config.StartingWeaponDamage,
                "StartingWeaponDamage should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(config.TierDamageRanges,
                "TierDamageRanges should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestWeaponScalingSanitizer()
        {
            Console.WriteLine("\n--- Testing WeaponScalingConfig.EnsureSanitizedDefaults ---");

            var config = new WeaponScalingConfig { GlobalDamageMultiplier = 0 };
            config.EnsureSanitizedDefaults();
            TestBase.AssertEqual(1.0, config.GlobalDamageMultiplier,
                "Weapon global damage multiplier should become 1.0",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region EquipmentScalingConfig Tests

        private static void TestEquipmentScalingConfig()
        {
            Console.WriteLine("\n--- Testing EquipmentScalingConfig ---");

            var config = new EquipmentScalingConfig
            {
                WeaponDamagePerTier = 5,
                ArmorValuePerTier = 3,
                SpeedBonusPerTier = 0.1,
                MaxTier = 5
            };

            TestBase.AssertEqual(5, config.WeaponDamagePerTier,
                "WeaponDamagePerTier should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(3, config.ArmorValuePerTier,
                "ArmorValuePerTier should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(0.1, config.SpeedBonusPerTier,
                "SpeedBonusPerTier should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEquipmentScalingSanitizer()
        {
            Console.WriteLine("\n--- Testing EquipmentScalingConfig.EnsureSensibleDefaults ---");

            var config = new EquipmentScalingConfig { WeaponDamagePerTier = 0, MaxTier = 0 };
            config.EnsureSensibleDefaults();
            TestBase.AssertTrue(config.WeaponDamagePerTier > 0,
                "WeaponDamagePerTier should be repaired",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(config.MaxTier > 0,
                "MaxTier should be repaired",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region RarityScalingConfig Tests

        private static void TestRarityScalingConfig()
        {
            Console.WriteLine("\n--- Testing RarityScalingConfig ---");

            var config = new RarityScalingConfig();

            TestBase.AssertNotNull(config.StatBonusMultipliers,
                "StatBonusMultipliers should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(config.RollChanceFormulas,
                "RollChanceFormulas should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(config.MagicFindScaling,
                "MagicFindScaling should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region ItemAffixByRaritySettings Tests

        private static void TestItemAffixByRaritySettingsJson()
        {
            Console.WriteLine("\n--- Testing itemAffixByRarity.rarityWeights + lootSystem.itemGeneration JSON ---");

            const string json = """
{
  "itemAffixByRarity": { "rarityWeights": { "epic": 7 } },
  "lootSystem": { "itemGeneration": { "heroClassShare": 0.6, "tierRollUp": { "chance": 0.2, "decay": 0.25, "max": 3 } } }
}
""";

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var root = JsonSerializer.Deserialize<GameConfiguration>(json, options);

            TestBase.AssertEqual(7.0, root!.ItemAffixByRarity.ResolveRarityWeight(new RarityData { Name = "Epic", Weight = 250 }),
                "rarityWeights override the table weight (case-insensitive)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1200.0, root.ItemAffixByRarity.ResolveRarityWeight(new RarityData { Name = "Rare", Weight = 1200 }),
                "rarities missing from rarityWeights keep the table weight",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            var gen = root.LootSystem.ItemGeneration;
            TestBase.AssertEqual(0.6, gen.HeroClassShare, "itemGeneration.heroClassShare reads", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(3, gen.TierRollUp.Max, "itemGeneration.tierRollUp.max reads", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0.9, gen.WeaponClassMaterialShare, "unset itemGeneration keys keep their defaults", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRollUpRule()
        {
            Console.WriteLine("\n--- Testing RollUpRule ---");
            var never = new RollUpRule(0, 0.5, 5);
            var always = new RollUpRule(1, 1, 3);
            var rng = new Random(1);
            TestBase.AssertEqual(0, never.Roll(rng), "chance 0 never rolls up", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(3, always.Roll(rng), "chance 1 rolls up to the cap", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var rule = new RollUpRule(0.5, 0.5, 4);
            int total = 0, over = 0;
            for (int i = 0; i < 20000; i++)
            {
                int steps = rule.Roll(rng);
                total += steps > 0 ? 1 : 0;
                over += steps > 4 ? 1 : 0;
            }
            double share = total / 20000.0;
            TestBase.AssertTrue(share > 0.47 && share < 0.53, $"first step lands at the base chance (got {share:0.000})", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, over, "never exceeds max", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region LootSystemConfig Tests

        private static void TestLootSystemConfig()
        {
            Console.WriteLine("\n--- Testing LootSystemConfig ---");

            var config = new LootSystemConfig();
            TestBase.AssertNotNull(config.ItemGeneration, "ItemGeneration should be initialized", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(4, config.ItemGeneration.PoolWeights.Count, "pool weights cover the four pools", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestLootSystemSanitizer()
        {
            Console.WriteLine("\n--- Testing LootSystemConfig.EnsureSensibleLootDefaults ---");

            var config = new LootSystemConfig
            {
                GoldDropMultiplier = 0,
                ItemGeneration = null!
            };
            config.EnsureSensibleLootDefaults();
            TestBase.AssertEqual(1.0, config.GoldDropMultiplier,
                "GoldDropMultiplier should become 1.0",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertNotNull(config.ItemGeneration,
                "missing itemGeneration is rebuilt",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
