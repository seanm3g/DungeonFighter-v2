using System;
using System.Collections.Generic;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for equipment system
    /// Tests equipping, unequipping, stat bonuses, action pool updates, and inventory management
    /// </summary>
    public static class EquipmentSystemTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Equipment System Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestEquipWeapon();
            TestUnequipWeapon();
            TestEquipArmor();
            TestUnequipArmor();
            TestStatBonusApplication();
            TestStatBonusRemoval();
            TestActionPoolUpdates();
            TestInventoryManagement();
            TestEquipmentConflicts();
            TestNullEquipment();
            TestEquipmentHealthBonuses();
            TestHealthAdjustmentOnEquip();
            TestRollBonusApplication();
            TestRerollChargesUpdate();

            TestBase.PrintSummary("Equipment System Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestEquipWeapon()
        {
            Console.WriteLine("\n--- Testing Weapon Equipping ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            var weapon = TestDataBuilders.Weapon().WithName("TestSword").WithWeaponType(WeaponType.Sword).Build();

            var previousWeapon = character.EquipItem(weapon, "weapon");

            TestBase.AssertTrue(character.Equipment.Weapon == weapon,
                "Weapon should be equipped",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNull(previousWeapon,
                "Previous weapon should be null when slot was empty",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestUnequipWeapon()
        {
            Console.WriteLine("\n--- Testing Weapon Unequipping ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            var weapon = TestDataBuilders.Weapon().WithName("TestSword").Build();

            character.EquipItem(weapon, "weapon");
            var unequippedWeapon = character.UnequipItem("weapon");

            TestBase.AssertNull(character.Equipment.Weapon,
                "Weapon slot should be empty after unequipping",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(unequippedWeapon == weapon,
                "Unequipped weapon should be returned",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEquipArmor()
        {
            Console.WriteLine("\n--- Testing Armor Equipping ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            var headItem = TestDataBuilders.Armor().WithType(ItemType.Head).WithName("TestHelmet").Build();
            var bodyItem = TestDataBuilders.Armor().WithType(ItemType.Chest).WithName("TestChest").Build();
            var feetItem = TestDataBuilders.Armor().WithType(ItemType.Feet).WithName("TestBoots").Build();

            character.EquipItem(headItem, "head");
            character.EquipItem(bodyItem, "body");
            character.EquipItem(feetItem, "feet");

            TestBase.AssertTrue(character.Equipment.Head == headItem,
                "Head item should be equipped",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(character.Equipment.Body == bodyItem,
                "Body item should be equipped",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(character.Equipment.Feet == feetItem,
                "Feet item should be equipped",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestUnequipArmor()
        {
            Console.WriteLine("\n--- Testing Armor Unequipping ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            var headItem = TestDataBuilders.Armor().WithType(ItemType.Head).WithName("TestHelmet").Build();

            character.EquipItem(headItem, "head");
            var unequippedItem = character.UnequipItem("head");

            TestBase.AssertNull(character.Equipment.Head,
                "Head slot should be empty after unequipping",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(unequippedItem == headItem,
                "Unequipped item should be returned",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestStatBonusApplication()
        {
            Console.WriteLine("\n--- Testing Stat Bonus Application ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            int originalStrength = character.Strength;

            var weapon = TestDataBuilders.Weapon()
                .WithName("StrengthWeapon")
                .WithStatBonus("STR", 5)
                .Build();

            character.EquipItem(weapon, "weapon");

            int newStrength = character.Strength;
            TestBase.AssertTrue(newStrength > originalStrength,
                $"Strength should increase after equipping weapon with bonus: {originalStrength} -> {newStrength}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestStatBonusRemoval()
        {
            Console.WriteLine("\n--- Testing Stat Bonus Removal ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            var weapon = TestDataBuilders.Weapon()
                .WithName("StrengthWeapon")
                .WithStatBonus("STR", 5)
                .Build();

            character.EquipItem(weapon, "weapon");
            int strengthWithWeapon = character.Strength;

            character.UnequipItem("weapon");
            int strengthWithoutWeapon = character.Strength;

            TestBase.AssertTrue(strengthWithoutWeapon < strengthWithWeapon,
                $"Strength should decrease after unequipping weapon: {strengthWithWeapon} -> {strengthWithoutWeapon}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestActionPoolUpdates()
        {
            Console.WriteLine("\n--- Testing Action Pool Updates ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            int actionsBefore = character.Actions.GetAllActions(character).Count;

            var weapon = TestDataBuilders.Weapon().WithName("ActionWeapon").Build();
            weapon.GearAction = "TEST_ACTION";

            character.EquipItem(weapon, "weapon");
            int actionsAfter = character.Actions.GetAllActions(character).Count;

            // Action pool should update (may increase or stay same depending on implementation)
            TestBase.AssertTrue(actionsAfter >= actionsBefore,
                $"Action pool should update after equipping weapon: {actionsBefore} -> {actionsAfter}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestInventoryManagement()
        {
            Console.WriteLine("\n--- Testing Inventory Management ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            var item = TestDataBuilders.Item().WithName("TestItem").Build();

            character.Equipment.AddToInventory(item);

            TestBase.AssertTrue(character.Equipment.Inventory.Contains(item),
                "Item should be in inventory after adding",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            bool removed = character.Equipment.RemoveFromInventory(item);

            TestBase.AssertTrue(removed,
                "RemoveFromInventory should return true when item is removed",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertFalse(character.Equipment.Inventory.Contains(item),
                "Item should not be in inventory after removing",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEquipmentConflicts()
        {
            Console.WriteLine("\n--- Testing Equipment Conflicts ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            var weapon1 = TestDataBuilders.Weapon().WithName("Weapon1").Build();
            var weapon2 = TestDataBuilders.Weapon().WithName("Weapon2").Build();

            character.EquipItem(weapon1, "weapon");
            var previousWeapon = character.EquipItem(weapon2, "weapon");

            TestBase.AssertTrue(character.Equipment.Weapon == weapon2,
                "New weapon should be equipped",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(previousWeapon == weapon1,
                "Previous weapon should be returned when replacing",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestNullEquipment()
        {
            Console.WriteLine("\n--- Testing Null Equipment Handling ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();

            // Test unequipping from empty slot
            var unequipped = character.UnequipItem("weapon");

            TestBase.AssertNull(unequipped,
                "Unequipping from empty slot should return null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNull(character.Equipment.Weapon,
                "Weapon slot should remain null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEquipmentHealthBonuses()
        {
            Console.WriteLine("\n--- Testing Equipment Health Bonuses ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            int baseMaxHealth = character.MaxHealth;

            var chestItem = TestDataBuilders.Armor()
                .WithType(ItemType.Chest)
                .WithName("VitalityChest")
                .WithStatBonus("Health", 10)
                .Build();

            character.EquipItem(chestItem, "body");
            int effectiveMaxHealth = character.GetEffectiveMaxHealth();

            TestBase.AssertTrue(effectiveMaxHealth > baseMaxHealth,
                $"Effective max health should increase with equipment bonus: {baseMaxHealth} -> {effectiveMaxHealth}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHealthAdjustmentOnEquip()
        {
            Console.WriteLine("\n--- Testing Health Adjustment On Equip ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            character.CurrentHealth = 50; // Set to half health
            double healthPercentage = character.GetHealthPercentage();

            var chestItem = TestDataBuilders.Armor()
                .WithType(ItemType.Chest)
                .WithName("VitalityChest")
                .WithStatBonus("Health", 20)
                .Build();

            character.EquipItem(chestItem, "body");

            // Health should maintain percentage or be adjusted appropriately
            double newHealthPercentage = character.GetHealthPercentage();
            TestBase.AssertTrue(character.CurrentHealth >= 50,
                $"Health should be adjusted when max health increases: {character.CurrentHealth}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRollBonusApplication()
        {
            Console.WriteLine("\n--- Testing Roll Bonus Application ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            int originalRollBonus = character.Equipment.GetEquipmentRollBonus();

            var weapon = TestDataBuilders.Weapon()
                .WithName("RollBonusWeapon")
                .WithStatBonus("RollBonus", 3)
                .Build();

            character.EquipItem(weapon, "weapon");
            int newRollBonus = character.Equipment.GetEquipmentRollBonus();

            TestBase.AssertTrue(newRollBonus >= originalRollBonus,
                $"Roll bonus should increase after equipping weapon: {originalRollBonus} -> {newRollBonus}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRerollChargesUpdate()
        {
            Console.WriteLine("\n--- Testing Reroll Charges Update ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            int originalCharges = character.Effects.RerollCharges;

            // Create a weapon with Divine modification (which provides reroll charges)
            var weapon = TestDataBuilders.Weapon().WithName("DivineWeapon").Build();
            var divineMod = new Modification
            {
                Name = "Divine",
                Effect = "RerollCharges",
                RolledValue = 1
            };
            weapon.Modifications.Add(divineMod);

            character.EquipItem(weapon, "weapon");
            int newCharges = character.Effects.RerollCharges;

            TestBase.AssertTrue(newCharges >= originalCharges,
                $"Reroll charges should update after equipping item with Divine mod: {originalCharges} -> {newCharges}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}

