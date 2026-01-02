using System;
using System.Collections.Generic;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    /// <summary>
    /// Comprehensive tests for ItemGenerator
    /// Tests item generation, tier selection, and name generation
    /// </summary>
    public static class ItemGeneratorTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all ItemGenerator tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== ItemGenerator Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestGenerateWeaponItem();
            TestGenerateArmorItem();
            TestSelectRandomItemByTier();
            TestGenerateItemNameWithBonuses();

            TestBase.PrintSummary("ItemGenerator Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Weapon Generation Tests

        private static void TestGenerateWeaponItem()
        {
            Console.WriteLine("--- Testing GenerateWeaponItem ---");

            var weaponData = new WeaponData
            {
                Name = "Test Sword",
                Type = "Sword",
                Tier = 1,
                BaseDamage = 10,
                AttackSpeed = 0.05
            };

            var weapon = ItemGenerator.GenerateWeaponItem(weaponData);

            TestBase.AssertNotNull(weapon,
                "Should generate weapon item",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (weapon != null)
            {
                TestBase.AssertEqual("Test Sword", weapon.Name,
                    "Weapon should have correct name",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertEqual(1, weapon.Tier,
                    "Weapon should have correct tier",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertEqual(10, weapon.BaseDamage,
                    "Weapon should have correct base damage",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertEqual(0.05, weapon.BaseAttackSpeed,
                    "Weapon should have correct attack speed",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Armor Generation Tests

        private static void TestGenerateArmorItem()
        {
            Console.WriteLine("\n--- Testing GenerateArmorItem ---");

            // Test Head item
            var headData = new ArmorData
            {
                Name = "Test Helmet",
                Slot = "head",
                Tier = 1,
                Armor = 5
            };

            var headItem = ItemGenerator.GenerateArmorItem(headData);
            TestBase.AssertNotNull(headItem,
                "Should generate head item",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (headItem != null)
            {
                TestBase.AssertEqual("Test Helmet", headItem.Name,
                    "Armor should have correct name",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test Chest item
            var chestData = new ArmorData
            {
                Name = "Test Chestplate",
                Slot = "chest",
                Tier = 2,
                Armor = 10
            };

            var chestItem = ItemGenerator.GenerateArmorItem(chestData);
            TestBase.AssertNotNull(chestItem,
                "Should generate chest item",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test Feet item
            var feetData = new ArmorData
            {
                Name = "Test Boots",
                Slot = "feet",
                Tier = 1,
                Armor = 3
            };

            var feetItem = ItemGenerator.GenerateArmorItem(feetData);
            TestBase.AssertNotNull(feetItem,
                "Should generate feet item",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Tier Selection Tests

        private static void TestSelectRandomItemByTier()
        {
            Console.WriteLine("\n--- Testing SelectRandomItemByTier ---");

            var items = new List<WeaponData>
            {
                new WeaponData { Name = "Tier1-1", Tier = 1 },
                new WeaponData { Name = "Tier1-2", Tier = 1 },
                new WeaponData { Name = "Tier2-1", Tier = 2 },
                new WeaponData { Name = "Tier2-2", Tier = 2 }
            };

            // Test selecting tier 1
            var tier1Item = ItemGenerator.SelectRandomItemByTier(items, 1);
            TestBase.AssertNotNull(tier1Item,
                "Should select item from tier 1",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (tier1Item != null)
            {
                TestBase.AssertEqual(1, tier1Item.Tier,
                    "Selected item should be tier 1",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test selecting tier 2
            var tier2Item = ItemGenerator.SelectRandomItemByTier(items, 2);
            TestBase.AssertNotNull(tier2Item,
                "Should select item from tier 2",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test selecting non-existent tier
            var tier3Item = ItemGenerator.SelectRandomItemByTier(items, 3);
            TestBase.AssertNull(tier3Item,
                "Should return null for non-existent tier",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Name Generation Tests

        private static void TestGenerateItemNameWithBonuses()
        {
            Console.WriteLine("\n--- Testing GenerateItemNameWithBonuses ---");

            // Create a test item with bonuses
            var weapon = TestDataBuilders.Weapon()
                .WithName("Test Sword")
                .WithTier(1)
                .Build();

            // Add some stat bonuses
            weapon.StatBonuses.Add(new StatBonus { StatType = "Strength", Value = 5 });
            weapon.StatBonuses.Add(new StatBonus { StatType = "Agility", Value = 3 });

            var name = ItemGenerator.GenerateItemNameWithBonuses(weapon);

            TestBase.AssertTrue(!string.IsNullOrEmpty(name),
                "Generated name should not be empty",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(name.Contains("Test Sword"),
                "Generated name should contain base name",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
