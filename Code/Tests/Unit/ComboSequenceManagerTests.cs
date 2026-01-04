using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for ComboSequenceManager
    /// Tests combo sequence management, ordering, and weapon action initialization
    /// </summary>
    public static class ComboSequenceManagerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all ComboSequenceManager tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== ComboSequenceManager Tests ===\n");

            TestGetComboActions();
            TestAddToCombo();
            TestRemoveFromCombo();
            TestReorderComboSequence();
            TestInitializeDefaultCombo();
            TestUpdateComboSequenceAfterGearChange();
            TestClearCombo();
            TestDuplicateActionPrevention();
            TestDuplicateActionsAllowed();
            TestMultipleDuplicateActionsInCombo();
            TestDuplicateActionsRemoval();
            TestNonComboActionRejection();
            TestComboOrdering();

            TestBase.PrintSummary("ComboSequenceManager Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Basic Functionality Tests

        private static void TestGetComboActions()
        {
            Console.WriteLine("--- Testing GetComboActions ---");

            var manager = new ComboSequenceManager();
            var character = TestDataBuilders.Character().WithName("ComboTest").Build();
            
            // Add some actions to action pool first
            var action1 = TestDataBuilders.CreateMockAction("ACTION1");
            action1.IsComboAction = true;
            action1.ComboOrder = 1;
            character.AddAction(action1, 1.0);
            
            manager.AddToCombo(action1);
            
            var comboActions = manager.GetComboActions();
            
            TestBase.AssertNotNull(comboActions, "GetComboActions should return non-null list", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(comboActions.Count > 0, "GetComboActions should return actions", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestAddToCombo()
        {
            Console.WriteLine("\n--- Testing AddToCombo ---");

            var manager = new ComboSequenceManager();
            var character = TestDataBuilders.Character().WithName("AddComboTest").Build();
            
            var action = TestDataBuilders.CreateMockAction("COMBO_ACTION");
            action.IsComboAction = true;
            character.AddAction(action, 1.0);
            
            manager.AddToCombo(action);
            
            var comboActions = manager.GetComboActions();
            var hasAction = comboActions.Any(a => a.Name == "COMBO_ACTION");
            
            TestBase.AssertTrue(hasAction, "Action should be added to combo", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRemoveFromCombo()
        {
            Console.WriteLine("\n--- Testing RemoveFromCombo ---");

            var manager = new ComboSequenceManager();
            var character = TestDataBuilders.Character().WithName("RemoveComboTest").Build();
            
            var action = TestDataBuilders.CreateMockAction("REMOVE_ACTION");
            action.IsComboAction = true;
            action.ComboOrder = 1;
            character.AddAction(action, 1.0);
            
            manager.AddToCombo(action);
            var beforeCount = manager.GetComboActions().Count;
            
            manager.RemoveFromCombo(action);
            var afterCount = manager.GetComboActions().Count;
            
            TestBase.AssertTrue(afterCount < beforeCount, 
                $"Combo should have fewer actions after removal (before: {beforeCount}, after: {afterCount})", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            
            var hasAction = manager.GetComboActions().Any(a => a.Name == "REMOVE_ACTION");
            TestBase.AssertFalse(hasAction, "Action should be removed from combo", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Ordering Tests

        private static void TestReorderComboSequence()
        {
            Console.WriteLine("\n--- Testing ReorderComboSequence ---");

            var manager = new ComboSequenceManager();
            var character = TestDataBuilders.Character().WithName("ReorderTest").Build();
            
            // Add actions in random order
            var action1 = TestDataBuilders.CreateMockAction("ACTION1");
            action1.IsComboAction = true;
            action1.ComboOrder = 3;
            character.AddAction(action1, 1.0);
            
            var action2 = TestDataBuilders.CreateMockAction("ACTION2");
            action2.IsComboAction = true;
            action2.ComboOrder = 1;
            character.AddAction(action2, 1.0);
            
            var action3 = TestDataBuilders.CreateMockAction("ACTION3");
            action3.IsComboAction = true;
            action3.ComboOrder = 2;
            character.AddAction(action3, 1.0);
            
            manager.AddToCombo(action1);
            manager.AddToCombo(action2);
            manager.AddToCombo(action3);
            
            var comboActions = manager.GetComboActions();
            
            // Check that actions are sorted by ComboOrder
            for (int i = 0; i < comboActions.Count - 1; i++)
            {
                TestBase.AssertTrue(comboActions[i].ComboOrder <= comboActions[i + 1].ComboOrder,
                    $"Combo actions should be sorted (action {i} order: {comboActions[i].ComboOrder}, action {i+1} order: {comboActions[i+1].ComboOrder})",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestComboOrdering()
        {
            Console.WriteLine("\n--- Testing Combo Ordering ---");

            var manager = new ComboSequenceManager();
            var character = TestDataBuilders.Character().WithName("OrderingTest").Build();
            
            var action1 = TestDataBuilders.CreateMockAction("FIRST");
            action1.IsComboAction = true;
            action1.ComboOrder = 1;
            character.AddAction(action1, 1.0);
            
            var action2 = TestDataBuilders.CreateMockAction("SECOND");
            action2.IsComboAction = true;
            action2.ComboOrder = 2;
            character.AddAction(action2, 1.0);
            
            manager.AddToCombo(action1);
            manager.AddToCombo(action2);
            
            var comboActions = manager.GetComboActions();
            
            TestBase.AssertEqual(1, comboActions[0].ComboOrder, 
                "First action should have ComboOrder 1", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(2, comboActions[1].ComboOrder, 
                "Second action should have ComboOrder 2", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Initialization Tests

        private static void TestInitializeDefaultCombo()
        {
            Console.WriteLine("\n--- Testing InitializeDefaultCombo ---");

            var manager = new ComboSequenceManager();
            var character = TestDataBuilders.Character().WithName("InitTest").Build();
            
            // Create a mock weapon with GearAction
            var weapon = new WeaponItem
            {
                Name = "TestWeapon",
                GearAction = "JAB",
                WeaponType = WeaponType.Sword
            };
            
            // Add the weapon action to character's action pool
            var weaponAction = ActionLoader.GetAction("JAB");
            if (weaponAction != null)
            {
                weaponAction.IsComboAction = true;
                character.AddAction(weaponAction, 1.0);
            }
            
            manager.InitializeDefaultCombo(character, weapon);
            
            var comboActions = manager.GetComboActions();
            
            // Should have at least one action in combo
            TestBase.AssertTrue(comboActions.Count > 0, 
                $"Default combo should be initialized with actions (got {comboActions.Count})", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Update Tests

        private static void TestUpdateComboSequenceAfterGearChange()
        {
            Console.WriteLine("\n--- Testing UpdateComboSequenceAfterGearChange ---");

            var manager = new ComboSequenceManager();
            var character = TestDataBuilders.Character().WithName("UpdateTest").Build();
            
            // Add an action to combo
            var action = TestDataBuilders.CreateMockAction("GEAR_ACTION");
            action.IsComboAction = true;
            action.ComboOrder = 1;
            character.AddAction(action, 1.0);
            
            manager.AddToCombo(action);
            var beforeCount = manager.GetComboActions().Count;
            
            // Remove action from action pool (simulating gear change)
            character.RemoveAction(action);
            
            // Update combo sequence
            manager.UpdateComboSequenceAfterGearChange(character);
            
            var afterCount = manager.GetComboActions().Count;
            
            TestBase.AssertTrue(afterCount < beforeCount, 
                "Actions not in action pool should be removed from combo", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Edge Case Tests

        private static void TestClearCombo()
        {
            Console.WriteLine("\n--- Testing ClearCombo ---");

            var manager = new ComboSequenceManager();
            var character = TestDataBuilders.Character().WithName("ClearTest").Build();
            
            var action = TestDataBuilders.CreateMockAction("CLEAR_ACTION");
            action.IsComboAction = true;
            character.AddAction(action, 1.0);
            
            manager.AddToCombo(action);
            TestBase.AssertTrue(manager.GetComboActions().Count > 0, 
                "Combo should have actions before clear", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            
            manager.ClearCombo();
            
            TestBase.AssertEqual(0, manager.GetComboActions().Count, 
                "Combo should be empty after clear", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDuplicateActionPrevention()
        {
            Console.WriteLine("\n--- Testing Duplicate Action Prevention (Same Instance) ---");

            var manager = new ComboSequenceManager();
            var character = TestDataBuilders.Character().WithName("DuplicateTest").Build();
            
            var action = TestDataBuilders.CreateMockAction("DUPLICATE_ACTION");
            action.IsComboAction = true;
            character.AddAction(action, 1.0);
            
            manager.AddToCombo(action);
            var firstCount = manager.GetComboActions().Count;
            
            // Try to add same Action object instance again (should be prevented)
            manager.AddToCombo(action);
            var secondCount = manager.GetComboActions().Count;
            
            TestBase.AssertEqual(firstCount, secondCount, 
                "Should not add the same Action object instance twice to combo", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDuplicateActionsAllowed()
        {
            Console.WriteLine("\n--- Testing Duplicate Actions Allowed (Different Instances) ---");

            var manager = new ComboSequenceManager();
            var character = TestDataBuilders.Character().WithName("DuplicateAllowedTest").Build();
            
            // Create two different Action instances with the same name (simulating duplicate from item)
            var action1 = ActionLoader.GetAction("JAB");
            if (action1 == null)
            {
                TestBase.AssertTrue(false, "JAB action not found in ActionLoader", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }
            action1.IsComboAction = true;
            action1.ComboOrder = 1;
            character.AddActionAllowDuplicates(action1, 1.0);
            
            var action2 = ActionLoader.GetAction("JAB");
            if (action2 == null)
            {
                TestBase.AssertTrue(false, "JAB action not found in ActionLoader (second instance)", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }
            action2.IsComboAction = true;
            action2.ComboOrder = 2;
            character.AddActionAllowDuplicates(action2, 1.0);
            
            // Add both instances to combo
            manager.AddToCombo(action1);
            manager.AddToCombo(action2);
            
            var comboActions = manager.GetComboActions();
            int jabCount = comboActions.Count(a => a.Name == "JAB");
            
            TestBase.AssertTrue(jabCount >= 2,
                $"Should allow multiple instances of same action name in combo, got {jabCount}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMultipleDuplicateActionsInCombo()
        {
            Console.WriteLine("\n--- Testing Multiple Duplicate Actions in Combo ---");

            var manager = new ComboSequenceManager();
            var character = TestDataBuilders.Character().WithName("MultipleDuplicateTest").Build();
            
            // Create three instances of the same action (simulating item with 3x same action)
            var action1 = ActionLoader.GetAction("ARCANE ECHO");
            var action2 = ActionLoader.GetAction("ARCANE ECHO");
            var action3 = ActionLoader.GetAction("ARCANE ECHO");
            
            if (action1 == null || action2 == null || action3 == null)
            {
                TestBase.AssertTrue(false, "ARCANE ECHO action not found in ActionLoader", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }
            
            action1.IsComboAction = true;
            action1.ComboOrder = 1;
            action2.IsComboAction = true;
            action2.ComboOrder = 2;
            action3.IsComboAction = true;
            action3.ComboOrder = 3;
            
            character.AddActionAllowDuplicates(action1, 1.0);
            character.AddActionAllowDuplicates(action2, 1.0);
            character.AddActionAllowDuplicates(action3, 1.0);
            
            // Add all three to combo
            manager.AddToCombo(action1);
            manager.AddToCombo(action2);
            manager.AddToCombo(action3);
            
            var comboActions = manager.GetComboActions();
            int arcaneEchoCount = comboActions.Count(a => a.Name == "ARCANE ECHO");
            
            TestBase.AssertTrue(arcaneEchoCount >= 3,
                $"Should allow 3 instances of same action in combo, got {arcaneEchoCount}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDuplicateActionsRemoval()
        {
            Console.WriteLine("\n--- Testing Duplicate Actions Removal ---");

            var manager = new ComboSequenceManager();
            var character = TestDataBuilders.Character().WithName("DuplicateRemovalTest").Build();
            
            // Create two instances of the same action
            var action1 = ActionLoader.GetAction("TAUNT");
            var action2 = ActionLoader.GetAction("TAUNT");
            
            if (action1 == null || action2 == null)
            {
                TestBase.AssertTrue(false, "TAUNT action not found in ActionLoader", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }
            
            action1.IsComboAction = true;
            action1.ComboOrder = 1;
            action2.IsComboAction = true;
            action2.ComboOrder = 2;
            
            character.AddActionAllowDuplicates(action1, 1.0);
            character.AddActionAllowDuplicates(action2, 1.0);
            
            // Add both to combo
            manager.AddToCombo(action1);
            manager.AddToCombo(action2);
            
            var beforeCount = manager.GetComboActions().Count(a => a.Name == "TAUNT");
            TestBase.AssertTrue(beforeCount >= 2,
                $"Should have 2 instances of TAUNT in combo before removal, got {beforeCount}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            
            // Remove one instance
            manager.RemoveFromCombo(action1);
            var afterCount = manager.GetComboActions().Count(a => a.Name == "TAUNT");
            
            TestBase.AssertTrue(afterCount == beforeCount - 1,
                $"Should remove only the specific instance, got {afterCount} instances remaining",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestNonComboActionRejection()
        {
            Console.WriteLine("\n--- Testing Non-Combo Action Rejection ---");

            var manager = new ComboSequenceManager();
            var character = TestDataBuilders.Character().WithName("RejectTest").Build();
            
            var action = TestDataBuilders.CreateMockAction("NON_COMBO_ACTION");
            action.IsComboAction = false; // Not a combo action
            character.AddAction(action, 1.0);
            
            var beforeCount = manager.GetComboActions().Count;
            
            manager.AddToCombo(action);
            
            var afterCount = manager.GetComboActions().Count;
            
            TestBase.AssertEqual(beforeCount, afterCount, 
                "Non-combo actions should not be added to combo", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}

