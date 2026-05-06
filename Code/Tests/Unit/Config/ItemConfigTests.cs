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
            TestItemAffixExtraFieldsJson();
            TestItemAffixExtraComboSlotsJson();
            TestItemAffixOmittedActionMaxDefaultsToOneWhenMinZero();
            TestItemAffixRollAxis();
            TestItemAffixPerItemTypeOverridesPerRarity();

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
            Console.WriteLine("\n--- Testing ItemAffixByRarity JSON deserialization ---");

            const string json = """
{
  "itemAffixByRarity": {
    "perRarity": {
      "Epic": { "prefixSlots": 2, "statSuffixes": 1, "actionBonuses": 0 }
    }
  }
}
""";

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var root = JsonSerializer.Deserialize<GameConfiguration>(json, options);

            TestBase.AssertNotNull(root?.ItemAffixByRarity,
                "ItemAffixByRarity should deserialize",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                root!.ItemAffixByRarity.TryGetForRarity("epic", out var entry),
                "TryGetForRarity should match case-insensitively",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(2, entry.PrefixSlots,
                "Epic prefixSlots",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1, entry.StatSuffixes,
                "Epic statSuffixes",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, entry.ActionBonuses,
                "Epic actionBonuses",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestItemAffixExtraFieldsJson()
        {
            Console.WriteLine("\n--- Testing ItemAffix extra chance / max JSON ---");

            const string json = """
{
  "itemAffixByRarity": {
    "perRarity": {
      "Rare": {
        "prefixSlots": 1,
        "prefixSlotsMax": 3,
        "prefixExtraChance": 0.25,
        "statSuffixes": 0,
        "statSuffixesMax": 4,
        "statSuffixExtraChance": 0.5,
        "actionBonuses": 0,
        "actionBonusesMax": 2,
        "actionExtraChance": 0.1
      }
    }
  }
}
""";

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var root = JsonSerializer.Deserialize<GameConfiguration>(json, options);

            TestBase.AssertTrue(
                root!.ItemAffixByRarity.TryGetForRarity("Rare", out var entry),
                "TryGetForRarity Rare",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var rule = ItemAffixByRaritySettings.BuildRuleFromTuningEntry(entry!, null);
            TestBase.AssertEqual(1, rule.PrefixMin, "prefix min", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(3, rule.PrefixMax, "prefix max", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0.25, rule.PrefixExtraChance, "prefix chance", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(4, rule.StatMax, "stat max", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(2, rule.ActionMax, "action max", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestItemAffixExtraComboSlotsJson()
        {
            Console.WriteLine("\n--- Testing ItemAffix extraComboSlots JSON ---");

            const string json = """
{
  "itemAffixByRarity": {
    "perRarity": {
      "Epic": {
        "prefixSlots": 0,
        "statSuffixes": 0,
        "actionBonuses": 0,
        "extraComboSlots": 1,
        "extraComboSlotsMax": 4,
        "extraComboSlotsExtraChance": 0.4
      }
    }
  }
}
""";

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var root = JsonSerializer.Deserialize<GameConfiguration>(json, options);

            TestBase.AssertTrue(
                root!.ItemAffixByRarity.TryGetForRarity("Epic", out var entry),
                "TryGetForRarity Epic",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var rule = ItemAffixByRaritySettings.BuildRuleFromTuningEntry(entry!, null);
            TestBase.AssertEqual(1, rule.ExtraComboSlotsMin, "extra combo min", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(4, rule.ExtraComboSlotsMax, "extra combo max", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0.4, rule.ExtraComboSlotsExtraChance, "extra combo chance", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// JSON/UI often omit actionBonusesMax when "0 or 1 action" is intended; legacy code used max 5.
        /// </summary>
        private static void TestItemAffixOmittedActionMaxDefaultsToOneWhenMinZero()
        {
            Console.WriteLine("\n--- Testing omitted actionBonusesMax (min 0 + extra chance) ---");

            var entry = new ItemAffixPerRarityEntry
            {
                ActionBonuses = 0,
                ActionExtraChance = 0.25,
                ActionBonusesMax = null
            };
            var commonRow = new RarityData { Name = "Common", ActionBonuses = 0 };
            var rule = ItemAffixByRaritySettings.BuildRuleFromTuningEntry(entry, commonRow);
            TestBase.AssertEqual(0, rule.ActionMin, "action min", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1, rule.ActionMax, "implicit action max should be 1 for optional single action", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestItemAffixRollAxis()
        {
            Console.WriteLine("\n--- Testing ItemAffixByRaritySettings.RollAxis ---");

            var rnd = new Random(123);
            int a = ItemAffixByRaritySettings.RollAxis(rnd, 2, 2, 1.0);
            TestBase.AssertEqual(2, a, "max<min clamp path: min 2 max 2 stays 2", ref _testsRun, ref _testsPassed, ref _testsFailed);

            int b = ItemAffixByRaritySettings.RollAxis(new Random(1), 3, 1, 1.0);
            TestBase.AssertEqual(3, b, "when max below min, max clamps up to min", ref _testsRun, ref _testsPassed, ref _testsFailed);

            int c = ItemAffixByRaritySettings.RollAxis(new Random(99), 1, 5, 0);
            TestBase.AssertEqual(1, c, "zero extra chance stays at min", ref _testsRun, ref _testsPassed, ref _testsFailed);

            int d = ItemAffixByRaritySettings.RollAxis(new Random(2), 0, 4, 1.0);
            TestBase.AssertEqual(4, d, "100% chance fills to max", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestItemAffixPerItemTypeOverridesPerRarity()
        {
            Console.WriteLine("\n--- Testing PerItemType affix overrides ---");

            var commonRow = new RarityData { Name = "Common", StatBonuses = 5, ActionBonuses = 5, Modifications = 0 };
            var tuning = new ItemAffixByRaritySettings
            {
                PerRarity = new Dictionary<string, ItemAffixPerRarityEntry>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Common"] = new ItemAffixPerRarityEntry { PrefixSlots = 1, StatSuffixes = 1, ActionBonuses = 1 }
                },
                PerItemType = new Dictionary<string, Dictionary<string, ItemAffixPerRarityEntry>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Head"] = new Dictionary<string, ItemAffixPerRarityEntry>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Common"] = new ItemAffixPerRarityEntry
                        {
                            PrefixSlots = 0,
                            StatSuffixes = 2,
                            ActionBonuses = 0
                        }
                    }
                }
            };

            var headRule = ItemAffixByRaritySettings.GetResolvedAffixRule("Common", commonRow, tuning, ItemType.Head);
            TestBase.AssertEqual(0, headRule.PrefixMin, "head prefix min", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(2, headRule.StatMin, "head stat min from PerItemType", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var chestRule = ItemAffixByRaritySettings.GetResolvedAffixRule("Common", commonRow, tuning, ItemType.Chest);
            TestBase.AssertEqual(1, chestRule.PrefixMin, "chest uses PerRarity", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1, chestRule.StatMin, "chest stat", ref _testsRun, ref _testsPassed, ref _testsFailed);
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
