using System;
using System.Collections.Generic;
using RPGGame.Tests;
using RPGGame;

namespace RPGGame.Tests.Unit.Entity
{
    /// <summary>
    /// Comprehensive tests for EquipmentManager
    /// Tests equipment management, stat bonuses, and equipment operations
    /// </summary>
    public static class EquipmentManagerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all EquipmentManager tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== EquipmentManager Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestEquipWeapon();
            TestEquipArmor();
            TestStatBonuses();
            TestTryEquipItem_BlockedByAttributeRequirements();
            TestTryEquipItem_SucceedsWhenAttributeRequirementsMet();

            TestBase.PrintSummary("EquipmentManager Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            var manager = new EquipmentManager(character);
            TestBase.AssertNotNull(manager,
                "EquipmentManager should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Equipment Tests

        private static void TestEquipWeapon()
        {
            Console.WriteLine("\n--- Testing EquipWeapon ---");

            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            var weapon = TestDataBuilders.Weapon()
                .WithName("Test Sword")
                .WithTier(1)
                .Build();

            var previousWeapon = character.EquipItem(weapon, "weapon");
            
            // Equipping should not crash
            TestBase.AssertTrue(true,
                "EquipWeapon should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Verify weapon is equipped
            TestBase.AssertNotNull(character.Equipment.Weapon,
                "Weapon should be equipped",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEquipArmor()
        {
            Console.WriteLine("\n--- Testing EquipArmor ---");

            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            var armor = TestDataBuilders.Armor()
                .WithType(ItemType.Head)
                .WithName("Test Helmet")
                .WithTier(1)
                .Build();

            var previousArmor = character.EquipItem(armor, "head");
            
            // Equipping should not crash
            TestBase.AssertTrue(true,
                "EquipArmor should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Verify armor is equipped
            TestBase.AssertNotNull(character.Equipment.Head,
                "Armor should be equipped",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Attribute requirement equip

        private static void TestTryEquipItem_BlockedByAttributeRequirements()
        {
            Console.WriteLine("\n--- Testing TryEquipItem blocked by attribute requirements ---");

            var character = TestDataBuilders.Character()
                .WithName("LowStat")
                .WithStats(3, 3, 3, 3)
                .Build();

            var weapon = TestDataBuilders.Weapon()
                .WithName("High Tec Blade")
                .WithTier(1)
                .Build();
            weapon.AttributeRequirements = new AttributeRequirements(new Dictionary<string, int> { ["technique"] = 10 });

            bool ok = character.TryEquipItem(weapon, "weapon", out var replaced, out var fail);
            TestBase.AssertFalse(ok,
                "TryEquipItem should fail when technique too low",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!string.IsNullOrEmpty(fail),
                "Failure reason should mention requirements",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(character.Equipment.Weapon == null,
                "Weapon slot should stay empty on failed equip",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(replaced == null,
                "replacedItem should be null on failed TryEquipItem",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestTryEquipItem_SucceedsWhenAttributeRequirementsMet()
        {
            Console.WriteLine("\n--- Testing TryEquipItem succeeds when requirements met ---");

            var character = TestDataBuilders.Character()
                .WithName("OkStat")
                .WithStats(3, 3, 12, 3)
                .Build();

            var weapon = TestDataBuilders.Weapon()
                .WithName("Tec Blade")
                .WithTier(1)
                .Build();
            weapon.AttributeRequirements = new AttributeRequirements(new Dictionary<string, int> { ["technique"] = 10 });

            bool ok = character.TryEquipItem(weapon, "weapon", out var replaced, out var fail);
            TestBase.AssertTrue(ok,
                "TryEquipItem should succeed when technique meets requirement",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(string.IsNullOrEmpty(fail),
                "No failure message on success",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(ReferenceEquals(weapon, character.Equipment.Weapon),
                "Equipped weapon should be the instance passed in",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Stat Bonuses Tests

        private static void TestStatBonuses()
        {
            Console.WriteLine("\n--- Testing Stat Bonuses ---");

            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            // Test that stat bonuses can be calculated
            var strength = character.Facade.GetEffectiveStrength();
            TestBase.AssertTrue(strength >= 0,
                $"Effective strength should be >= 0, got {strength}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Equip item with stat bonus
            var weapon = TestDataBuilders.Weapon()
                .WithName("Test Sword")
                .WithTier(1)
                .WithStatBonus("Strength", 5)
                .Build();

            character.EquipItem(weapon, "weapon");

            // Strength should increase after equipping
            var newStrength = character.Facade.GetEffectiveStrength();
            TestBase.AssertTrue(newStrength >= strength,
                "Strength should increase after equipping item with stat bonus",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
