using System;
using System.Collections.Generic;
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

        #region LootSystemConfig Tests

        private static void TestLootSystemConfig()
        {
            Console.WriteLine("\n--- Testing LootSystemConfig ---");

            var config = new LootSystemConfig
            {
                BaseDropChance = 0.1,
                DropChancePerLevel = 0.01,
                MaxDropChance = 0.5,
                GuaranteedLootChance = 1.0
            };

            TestBase.AssertEqual(0.1, config.BaseDropChance,
                "BaseDropChance should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(config.RarityUpgrade,
                "RarityUpgrade should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestLootSystemSanitizer()
        {
            Console.WriteLine("\n--- Testing LootSystemConfig.EnsureSensibleLootDefaults ---");

            var config = new LootSystemConfig
            {
                BaseDropChance = 0,
                MaxDropChance = 0,
                GuaranteedLootChance = 0,
                GoldDropMultiplier = 0
            };
            config.EnsureSensibleLootDefaults();
            TestBase.AssertTrue(config.BaseDropChance > 0,
                "BaseDropChance should be repaired",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(config.MaxDropChance > 0,
                "MaxDropChance should be repaired",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1.0, config.GuaranteedLootChance,
                "GuaranteedLootChance should become 1.0 when zero",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1.0, config.GoldDropMultiplier,
                "GoldDropMultiplier should become 1.0",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
