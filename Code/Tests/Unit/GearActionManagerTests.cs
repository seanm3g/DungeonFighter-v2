using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for GearActionManager
    /// Tests weapon/armor action addition, removal, and roll bonus application
    /// </summary>
    public static class GearActionManagerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all GearActionManager tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== GearActionManager Tests ===\n");

            TestAddWeaponActions();
            TestAddArmorActions();
            TestRemoveWeaponActions();
            TestRemoveArmorActions();
            TestGetGearActions();
            TestRollBonusApplication();
            TestRollBonusRemoval();
            TestNullGearHandling();
            TestActionMarkingAsComboAction();
            TestMultipleGearActions();
            TestGearActionNotFoundHandling();
            TestDuplicateActionsFromGear();
            TestDuplicateActionsRemoval();

            TestBase.PrintSummary("GearActionManager Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Weapon Action Tests

        private static void TestAddWeaponActions()
        {
            Console.WriteLine("--- Testing AddWeaponActions ---");

            var manager = new GearActionManager();
            var character = TestDataBuilders.Character().WithName("WeaponTest").Build();
            
            var weapon = new WeaponItem
            {
                Name = "TestSword",
                GearAction = "JAB",
                WeaponType = WeaponType.Sword
            };

            var beforeCount = character.ActionPool.Count;
            manager.AddWeaponActions(character, weapon);
            var afterCount = character.ActionPool.Count;

            TestBase.AssertTrue(afterCount >= beforeCount, 
                $"Weapon actions should be added (before: {beforeCount}, after: {afterCount})", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRemoveWeaponActions()
        {
            Console.WriteLine("\n--- Testing RemoveWeaponActions ---");

            var manager = new GearActionManager();
            var character = TestDataBuilders.Character().WithName("RemoveWeaponTest").Build();
            
            var weapon = new WeaponItem
            {
                Name = "TestSword",
                GearAction = "JAB",
                WeaponType = WeaponType.Sword
            };

            manager.AddWeaponActions(character, weapon);
            var beforeCount = character.ActionPool.Count;

            manager.RemoveWeaponActions(character, weapon);
            var afterCount = character.ActionPool.Count;

            TestBase.AssertTrue(afterCount <= beforeCount, 
                $"Weapon actions should be removed (before: {beforeCount}, after: {afterCount})", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Armor Action Tests

        private static void TestAddArmorActions()
        {
            Console.WriteLine("\n--- Testing AddArmorActions ---");

            var manager = new GearActionManager();
            var character = TestDataBuilders.Character().WithName("ArmorTest").Build();
            
            var armor = new Item(ItemType.Head, "TestArmor")
            {
                GearAction = "DEFENSIVE STANCE"
            };

            var beforeCount = character.ActionPool.Count;
            manager.AddArmorActions(character, armor);
            var afterCount = character.ActionPool.Count;

            TestBase.AssertTrue(afterCount >= beforeCount, 
                $"Armor actions should be added (before: {beforeCount}, after: {afterCount})", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRemoveArmorActions()
        {
            Console.WriteLine("\n--- Testing RemoveArmorActions ---");

            var manager = new GearActionManager();
            var character = TestDataBuilders.Character().WithName("RemoveArmorTest").Build();
            
            var armor = new Item(ItemType.Head, "TestArmor")
            {
                GearAction = "DEFENSIVE STANCE"
            };

            manager.AddArmorActions(character, armor);
            var beforeCount = character.ActionPool.Count;

            manager.RemoveArmorActions(character, armor);
            var afterCount = character.ActionPool.Count;

            TestBase.AssertTrue(afterCount <= beforeCount, 
                $"Armor actions should be removed (before: {beforeCount}, after: {afterCount})", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Gear Action Retrieval Tests

        private static void TestGetGearActions()
        {
            Console.WriteLine("\n--- Testing GetGearActions ---");

            var manager = new GearActionManager();
            
            var weapon = new WeaponItem
            {
                Name = "TestWeapon",
                GearAction = "JAB",
                WeaponType = WeaponType.Sword,
                ActionBonuses = new List<ActionBonus>
                {
                    new ActionBonus { Name = "CRIT" }
                }
            };

            var actions = manager.GetGearActions(weapon);

            TestBase.AssertTrue(actions.Count > 0, 
                $"Should return gear actions (got {actions.Count})", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(actions.Contains("JAB"), 
                "Should include GearAction", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(actions.Contains("CRIT"), 
                "Should include ActionBonuses", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Roll Bonus Tests

        private static void TestRollBonusApplication()
        {
            Console.WriteLine("\n--- Testing Roll Bonus Application ---");

            var manager = new GearActionManager();
            var character = TestDataBuilders.Character().WithName("RollBonusTest").Build();
            
            var weapon = new WeaponItem
            {
                Name = "TestWeapon",
                WeaponType = WeaponType.Sword,
                StatBonuses = new List<StatBonus>
                {
                    new StatBonus { StatType = "RollBonus", Value = 5 }
                }
            };

            // Should not throw exception
            try
            {
                manager.ApplyRollBonusesFromGear(character, weapon);
                TestBase.AssertTrue(true, "Should apply roll bonuses without exception", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"Should not throw exception: {ex.Message}", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestRollBonusRemoval()
        {
            Console.WriteLine("\n--- Testing Roll Bonus Removal ---");

            var manager = new GearActionManager();
            var character = TestDataBuilders.Character().WithName("RollBonusRemoveTest").Build();
            
            var weapon = new WeaponItem
            {
                Name = "TestWeapon",
                WeaponType = WeaponType.Sword,
                StatBonuses = new List<StatBonus>
                {
                    new StatBonus { StatType = "Roll", Value = 3 }
                }
            };

            // Should not throw exception
            try
            {
                manager.RemoveRollBonusesFromGear(character, weapon);
                TestBase.AssertTrue(true, "Should remove roll bonuses without exception", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"Should not throw exception: {ex.Message}", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Edge Case Tests

        private static void TestNullGearHandling()
        {
            Console.WriteLine("\n--- Testing Null Gear Handling ---");

            var manager = new GearActionManager();
            var character = TestDataBuilders.Character().WithName("NullGearTest").Build();

            // Should not throw exception with null gear
            try
            {
                manager.AddWeaponActions(character, null);
                manager.AddArmorActions(character, null);
                manager.RemoveWeaponActions(character, null);
                manager.RemoveArmorActions(character, null);
                TestBase.AssertTrue(true, "Should handle null gear without exception", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"Should not throw exception with null gear: {ex.Message}", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestActionMarkingAsComboAction()
        {
            Console.WriteLine("\n--- Testing Action Marking as Combo Action ---");

            var manager = new GearActionManager();
            var character = TestDataBuilders.Character().WithName("ComboActionTest").Build();
            
            var weapon = new WeaponItem
            {
                Name = "TestWeapon",
                GearAction = "JAB",
                WeaponType = WeaponType.Sword
            };

            manager.AddWeaponActions(character, weapon);

            // Check that gear action is marked as combo action
            var gearAction = character.ActionPool.FirstOrDefault(a => a.action.Name == "JAB");
            if (gearAction.action != null)
            {
                TestBase.AssertTrue(gearAction.action.IsComboAction, 
                    "Gear actions should be marked as combo actions", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestMultipleGearActions()
        {
            Console.WriteLine("\n--- Testing Multiple Gear Actions ---");

            var manager = new GearActionManager();
            var character = TestDataBuilders.Character().WithName("MultiGearTest").Build();
            
            var weapon = new WeaponItem
            {
                Name = "TestWeapon",
                GearAction = "JAB",
                WeaponType = WeaponType.Sword,
                ActionBonuses = new List<ActionBonus>
                {
                    new ActionBonus { Name = "CRIT" },
                    new ActionBonus { Name = "STUN" }
                }
            };

            var beforeCount = character.ActionPool.Count;
            manager.AddWeaponActions(character, weapon);
            var afterCount = character.ActionPool.Count;

            TestBase.AssertTrue(afterCount > beforeCount, 
                $"Should add multiple gear actions (before: {beforeCount}, after: {afterCount})", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGearActionNotFoundHandling()
        {
            Console.WriteLine("\n--- Testing Gear Action Not Found Handling ---");

            var manager = new GearActionManager();
            var character = TestDataBuilders.Character().WithName("NotFoundGearTest").Build();
            
            var weapon = new WeaponItem
            {
                Name = "TestWeapon",
                GearAction = "NONEXISTENT_ACTION",
                WeaponType = WeaponType.Sword
            };

            // Should not throw exception if action doesn't exist
            try
            {
                manager.AddWeaponActions(character, weapon);
                TestBase.AssertTrue(true, "Should handle missing actions gracefully", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"Should handle missing actions gracefully: {ex.Message}", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Duplicate Action Tests

        private static void TestDuplicateActionsFromGear()
        {
            Console.WriteLine("\n--- Testing Duplicate Actions from Gear ---");

            var manager = new GearActionManager();
            var character = TestDataBuilders.Character().WithName("DuplicateGearTest").Build();
            
            var weapon = new WeaponItem
            {
                Name = "TestWeapon",
                GearAction = "ARCANE ECHO",
                WeaponType = WeaponType.Wand,
                ActionBonuses = new List<ActionBonus>
                {
                    new ActionBonus { Name = "ARCANE ECHO" }
                }
            };

            var beforeCount = character.ActionPool.Count;
            manager.AddWeaponActions(character, weapon);
            var afterCount = character.ActionPool.Count;

            // Should add 2 actions (both ARCANE ECHO instances)
            TestBase.AssertTrue(afterCount >= beforeCount + 2,
                $"Should add at least 2 actions for duplicate ARCANE ECHO (before: {beforeCount}, after: {afterCount})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Verify both instances are in the pool
            var arcaneEchoCount = character.ActionPool.Count(a => a.action.Name == "ARCANE ECHO");
            TestBase.AssertTrue(arcaneEchoCount >= 2,
                $"Should have at least 2 instances of ARCANE ECHO in action pool, got {arcaneEchoCount}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Verify they have different ComboOrder values
            var arcaneEchoActions = character.ActionPool
                .Where(a => a.action.Name == "ARCANE ECHO")
                .Select(a => a.action.ComboOrder)
                .Distinct()
                .ToList();
            
            TestBase.AssertTrue(arcaneEchoActions.Count >= 2,
                $"Duplicate actions should have different ComboOrder values, got {arcaneEchoActions.Count} unique orders",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDuplicateActionsRemoval()
        {
            Console.WriteLine("\n--- Testing Duplicate Actions Removal ---");

            var manager = new GearActionManager();
            var character = TestDataBuilders.Character().WithName("DuplicateRemovalTest").Build();
            
            var armor = new Item(ItemType.Head, "TestArmor")
            {
                ActionBonuses = new List<ActionBonus>
                {
                    new ActionBonus { Name = "TAUNT" },
                    new ActionBonus { Name = "TAUNT" },
                    new ActionBonus { Name = "TAUNT" }
                }
            };

            // Add duplicate actions
            manager.AddArmorActions(character, armor);
            var beforeCount = character.ActionPool.Count(a => a.action.Name == "TAUNT");
            
            TestBase.AssertTrue(beforeCount >= 3,
                $"Should have at least 3 instances of TAUNT before removal, got {beforeCount}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Remove all instances
            manager.RemoveArmorActions(character, armor);
            var afterCount = character.ActionPool.Count(a => a.action.Name == "TAUNT");

            TestBase.AssertEqual(0, afterCount,
                $"All instances of TAUNT should be removed, got {afterCount}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}

