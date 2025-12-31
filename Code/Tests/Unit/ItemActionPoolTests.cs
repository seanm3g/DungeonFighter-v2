using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Tests;
using RPGGame.Data;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests to verify that items with actions actually make it into the action pool
    /// Tests both GearAction and ActionBonuses for weapons and armor
    /// </summary>
    public static class ItemActionPoolTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all ItemActionPool tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== Item Action Pool Tests ===\n");

            // Ensure actions are loaded before testing
            ActionLoader.LoadActions();

            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestWeaponGearActionInPool();
            TestWeaponActionBonusesInPool();
            TestWeaponMultipleActionsInPool();
            TestArmorGearActionInPool();
            TestArmorActionBonusesInPool();
            TestArmorMultipleActionsInPool();
            TestActionPoolViaGetActionPool();
            TestActionPoolViaActionPoolProperty();
            TestActionsRemovedOnUnequip();
            TestActionsReplacedOnEquipNewItem();

            TestBase.PrintSummary("Item Action Pool Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Weapon Action Tests

        /// <summary>
        /// Tests that a weapon with a GearAction appears in the action pool
        /// </summary>
        private static void TestWeaponGearActionInPool()
        {
            Console.WriteLine("--- Testing Weapon GearAction in Action Pool ---");

            var character = TestDataBuilders.Character().WithName("WeaponGearActionTest").Build();
            var weapon = TestDataBuilders.Weapon()
                .WithName("TestSword")
                .WithWeaponType(WeaponType.Sword)
                .Build();
            
            weapon.GearAction = "JAB";

            // Get initial action count
            var initialActions = character.GetActionPool();
            int initialCount = initialActions.Count;

            // Equip the weapon
            character.EquipItem(weapon, "weapon");

            // Verify action is in the pool
            var actionPool = character.GetActionPool();
            bool hasJab = actionPool.Any(a => a.Name == "JAB");

            TestBase.AssertTrue(hasJab,
                $"JAB action should be in action pool after equipping weapon with GearAction. Pool size: {actionPool.Count}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Also verify via ActionPool property
            bool hasJabInProperty = character.ActionPool.Any(a => a.action.Name == "JAB");
            TestBase.AssertTrue(hasJabInProperty,
                "JAB action should be in ActionPool property",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Tests that a weapon with ActionBonuses appears in the action pool
        /// </summary>
        private static void TestWeaponActionBonusesInPool()
        {
            Console.WriteLine("\n--- Testing Weapon ActionBonuses in Action Pool ---");

            var character = TestDataBuilders.Character().WithName("WeaponActionBonusesTest").Build();
            var weapon = TestDataBuilders.Weapon()
                .WithName("TestSword")
                .WithWeaponType(WeaponType.Sword)
                .Build();
            
            weapon.ActionBonuses = new List<ActionBonus>
            {
                new ActionBonus { Name = "FOLLOW THROUGH" },
                new ActionBonus { Name = "TAUNT" }
            };

            // Equip the weapon
            character.EquipItem(weapon, "weapon");

            // Verify both actions are in the pool
            var actionPool = character.GetActionPool();
            bool hasFollowThrough = actionPool.Any(a => a.Name == "FOLLOW THROUGH");
            bool hasTaunt = actionPool.Any(a => a.Name == "TAUNT");

            TestBase.AssertTrue(hasFollowThrough,
                $"FOLLOW THROUGH action should be in action pool after equipping weapon with ActionBonuses. Pool size: {actionPool.Count}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(hasTaunt,
                $"TAUNT action should be in action pool after equipping weapon with ActionBonuses. Pool size: {actionPool.Count}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Tests that a weapon with both GearAction and ActionBonuses has all actions in the pool
        /// </summary>
        private static void TestWeaponMultipleActionsInPool()
        {
            Console.WriteLine("\n--- Testing Weapon Multiple Actions in Action Pool ---");

            var character = TestDataBuilders.Character().WithName("WeaponMultipleActionsTest").Build();
            var weapon = TestDataBuilders.Weapon()
                .WithName("TestSword")
                .WithWeaponType(WeaponType.Sword)
                .Build();
            
            weapon.GearAction = "JAB";
            weapon.ActionBonuses = new List<ActionBonus>
            {
                new ActionBonus { Name = "FOLLOW THROUGH" },
                new ActionBonus { Name = "TAUNT" }
            };

            // Equip the weapon
            character.EquipItem(weapon, "weapon");

            // Verify all actions are in the pool
            var actionPool = character.GetActionPool();
            bool hasJab = actionPool.Any(a => a.Name == "JAB");
            bool hasFollowThrough = actionPool.Any(a => a.Name == "FOLLOW THROUGH");
            bool hasTaunt = actionPool.Any(a => a.Name == "TAUNT");

            TestBase.AssertTrue(hasJab,
                "JAB action should be in action pool",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(hasFollowThrough,
                "FOLLOW THROUGH action should be in action pool",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(hasTaunt,
                "TAUNT action should be in action pool",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Verify we have at least 3 actions
            TestBase.AssertTrue(actionPool.Count >= 3,
                $"Should have at least 3 actions in pool (GearAction + 2 ActionBonuses), got {actionPool.Count}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Armor Action Tests

        /// <summary>
        /// Tests that armor with a GearAction appears in the action pool
        /// </summary>
        private static void TestArmorGearActionInPool()
        {
            Console.WriteLine("\n--- Testing Armor GearAction in Action Pool ---");

            var character = TestDataBuilders.Character().WithName("ArmorGearActionTest").Build();
            var armor = TestDataBuilders.Armor()
                .WithType(ItemType.Head)
                .WithName("TestHelmet")
                .Build();
            
            armor.GearAction = "TAUNT";

            // Equip the armor
            character.EquipItem(armor, "head");

            // Verify action is in the pool
            var actionPool = character.GetActionPool();
            bool hasTaunt = actionPool.Any(a => a.Name == "TAUNT");

            TestBase.AssertTrue(hasTaunt,
                $"TAUNT action should be in action pool after equipping armor with GearAction. Pool size: {actionPool.Count}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Tests that armor with ActionBonuses appears in the action pool
        /// </summary>
        private static void TestArmorActionBonusesInPool()
        {
            Console.WriteLine("\n--- Testing Armor ActionBonuses in Action Pool ---");

            var character = TestDataBuilders.Character().WithName("ArmorActionBonusesTest").Build();
            var armor = TestDataBuilders.Armor()
                .WithType(ItemType.Chest)
                .WithName("TestChest")
                .Build();
            
            armor.ActionBonuses = new List<ActionBonus>
            {
                new ActionBonus { Name = "FOLLOW THROUGH" },
                new ActionBonus { Name = "MISDIRECT" }
            };

            // Equip the armor
            character.EquipItem(armor, "body");

            // Verify both actions are in the pool
            var actionPool = character.GetActionPool();
            bool hasFollowThrough = actionPool.Any(a => a.Name == "FOLLOW THROUGH");
            bool hasMisdirect = actionPool.Any(a => a.Name == "MISDIRECT");

            TestBase.AssertTrue(hasFollowThrough,
                $"FOLLOW THROUGH action should be in action pool after equipping armor with ActionBonuses. Pool size: {actionPool.Count}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(hasMisdirect,
                $"MISDIRECT action should be in action pool after equipping armor with ActionBonuses. Pool size: {actionPool.Count}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Tests that armor with both GearAction and ActionBonuses has all actions in the pool
        /// </summary>
        private static void TestArmorMultipleActionsInPool()
        {
            Console.WriteLine("\n--- Testing Armor Multiple Actions in Action Pool ---");

            var character = TestDataBuilders.Character().WithName("ArmorMultipleActionsTest").Build();
            var armor = TestDataBuilders.Armor()
                .WithType(ItemType.Feet)
                .WithName("TestBoots")
                .Build();
            
            armor.GearAction = "CHANNEL";
            armor.ActionBonuses = new List<ActionBonus>
            {
                new ActionBonus { Name = "TAUNT" }
            };

            // Equip the armor
            character.EquipItem(armor, "feet");

            // Verify all actions are in the pool
            var actionPool = character.GetActionPool();
            bool hasChannel = actionPool.Any(a => a.Name == "CHANNEL");
            bool hasTaunt = actionPool.Any(a => a.Name == "TAUNT");

            TestBase.AssertTrue(hasChannel,
                "CHANNEL action should be in action pool",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(hasTaunt,
                "TAUNT action should be in action pool",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Action Pool Access Tests

        /// <summary>
        /// Tests that actions are accessible via GetActionPool() method
        /// </summary>
        private static void TestActionPoolViaGetActionPool()
        {
            Console.WriteLine("\n--- Testing Action Pool via GetActionPool() ---");

            var character = TestDataBuilders.Character().WithName("GetActionPoolTest").Build();
            var weapon = TestDataBuilders.Weapon()
                .WithName("TestSword")
                .WithWeaponType(WeaponType.Sword)
                .Build();
            
            weapon.GearAction = "JAB";

            // Equip the weapon
            character.EquipItem(weapon, "weapon");

            // Get action pool via method
            var actionPool = character.GetActionPool();

            TestBase.AssertNotNull(actionPool,
                "GetActionPool() should not return null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(actionPool.Count > 0,
                $"GetActionPool() should return actions, got {actionPool.Count}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Verify we can find the action
            var jabAction = actionPool.FirstOrDefault(a => a.Name == "JAB");
            TestBase.AssertNotNull(jabAction,
                "JAB action should be accessible via GetActionPool()",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Tests that actions are accessible via ActionPool property
        /// </summary>
        private static void TestActionPoolViaActionPoolProperty()
        {
            Console.WriteLine("\n--- Testing Action Pool via ActionPool Property ---");

            var character = TestDataBuilders.Character().WithName("ActionPoolPropertyTest").Build();
            var weapon = TestDataBuilders.Weapon()
                .WithName("TestSword")
                .WithWeaponType(WeaponType.Sword)
                .Build();
            
            weapon.GearAction = "JAB";

            // Equip the weapon
            character.EquipItem(weapon, "weapon");

            // Get action pool via property
            var actionPool = character.ActionPool;

            TestBase.AssertNotNull(actionPool,
                "ActionPool property should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(actionPool.Count > 0,
                $"ActionPool property should contain actions, got {actionPool.Count}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Verify we can find the action
            var jabEntry = actionPool.FirstOrDefault(a => a.action.Name == "JAB");
            TestBase.AssertNotNull(jabEntry.action,
                "JAB action should be accessible via ActionPool property",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Action Removal Tests

        /// <summary>
        /// Tests that actions are removed from the pool when item is unequipped
        /// </summary>
        private static void TestActionsRemovedOnUnequip()
        {
            Console.WriteLine("\n--- Testing Actions Removed on Unequip ---");

            var character = TestDataBuilders.Character().WithName("ActionRemovalTest").Build();
            var weapon = TestDataBuilders.Weapon()
                .WithName("TestSword")
                .WithWeaponType(WeaponType.Sword)
                .Build();
            
            weapon.GearAction = "JAB";
            weapon.ActionBonuses = new List<ActionBonus>
            {
                new ActionBonus { Name = "FOLLOW THROUGH" }
            };

            // Equip the weapon
            character.EquipItem(weapon, "weapon");

            // Verify actions are in pool
            var actionPoolBefore = character.GetActionPool();
            bool hasJabBefore = actionPoolBefore.Any(a => a.Name == "JAB");
            bool hasFollowThroughBefore = actionPoolBefore.Any(a => a.Name == "FOLLOW THROUGH");

            TestBase.AssertTrue(hasJabBefore,
                "JAB should be in pool before unequip",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(hasFollowThroughBefore,
                "FOLLOW THROUGH should be in pool before unequip",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Unequip the weapon
            character.UnequipItem("weapon");

            // Verify actions are removed from pool
            var actionPoolAfter = character.GetActionPool();
            bool hasJabAfter = actionPoolAfter.Any(a => a.Name == "JAB");
            bool hasFollowThroughAfter = actionPoolAfter.Any(a => a.Name == "FOLLOW THROUGH");

            TestBase.AssertFalse(hasJabAfter,
                "JAB should not be in pool after unequipping weapon",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertFalse(hasFollowThroughAfter,
                "FOLLOW THROUGH should not be in pool after unequipping weapon",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Tests that actions are replaced when a new item is equipped
        /// </summary>
        private static void TestActionsReplacedOnEquipNewItem()
        {
            Console.WriteLine("\n--- Testing Actions Replaced on Equip New Item ---");

            var character = TestDataBuilders.Character().WithName("ActionReplacementTest").Build();
            
            // First weapon
            var weapon1 = TestDataBuilders.Weapon()
                .WithName("TestSword1")
                .WithWeaponType(WeaponType.Sword)
                .Build();
            weapon1.GearAction = "JAB";

            // Second weapon
            var weapon2 = TestDataBuilders.Weapon()
                .WithName("TestSword2")
                .WithWeaponType(WeaponType.Sword)
                .Build();
            weapon2.GearAction = "TAUNT";

            // Equip first weapon
            character.EquipItem(weapon1, "weapon");
            var actionPool1 = character.GetActionPool();
            bool hasJab1 = actionPool1.Any(a => a.Name == "JAB");

            TestBase.AssertTrue(hasJab1,
                "JAB should be in pool after equipping first weapon",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Equip second weapon (should replace first)
            character.EquipItem(weapon2, "weapon");
            var actionPool2 = character.GetActionPool();
            bool hasJab2 = actionPool2.Any(a => a.Name == "JAB");
            bool hasTaunt2 = actionPool2.Any(a => a.Name == "TAUNT");

            // JAB should be removed, TAUNT should be added
            TestBase.AssertFalse(hasJab2,
                "JAB should not be in pool after equipping new weapon",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(hasTaunt2,
                "TAUNT should be in pool after equipping new weapon",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}

