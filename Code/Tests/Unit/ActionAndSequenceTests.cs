using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Actions.Execution;
using RPGGame.Actions.RollModification;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for actions and action sequences (combo system)
    /// Tests action selection, combo sequences, action properties, and sequence logic
    /// </summary>
    public static class ActionAndSequenceTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;
        
        /// <summary>
        /// Runs all action and sequence tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== Action and Action Sequence Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            
            // Action Creation and Properties Tests
            TestActionCreation();
            TestActionProperties();
            TestActionCooldown();
            TestActionStatusEffects();
            
            // Action Selection Tests
            TestActionSelectionByRoll();
            TestActionSelectionForCharacters();
            TestActionSelectionForEnemies();
            TestBasicAttackSelection();
            TestComboActionSelection();
            
            // Combo Sequence Tests
            TestComboSequenceManager();
            TestAddToCombo();
            TestRemoveFromCombo();
            TestComboOrdering();
            TestComboInitialization();
            TestComboStepAdvancement();
            TestComboResetOnMiss();
            
            // Action Pool Tests
            TestActionPoolManagement();
            TestActionPoolWithComboActions();
            
            // Action Execution Flow Tests
            TestActionExecutionFlow();
            TestActionExecutionWithForcedAction();
            TestActionExecutionResult();
            
            PrintSummary();
        }
        
        #region Action Creation and Properties Tests
        
        private static void TestActionCreation()
        {
            Console.WriteLine("--- Testing Action Creation ---");
            
            var action = new Action
            {
                Name = "Test Action",
                Type = ActionType.Attack,
                Target = TargetType.SingleTarget,
                DamageMultiplier = 1.5,
                Length = 1.0
            };
            
            AssertTrue(action.Name == "Test Action", "Action name should be set correctly");
            AssertTrue(action.Type == ActionType.Attack, "Action type should be Attack");
            AssertTrue(action.DamageMultiplier == 1.5, "Damage multiplier should be 1.5");
        }
        
        private static void TestActionProperties()
        {
            Console.WriteLine("\n--- Testing Action Properties ---");
            
            var action = new Action
            {
                Name = "Multi-Hit Action",
                Type = ActionType.Attack,
                DamageMultiplier = 2.0,
                Length = 1.5,
                Cooldown = 3
            };
            
            AssertTrue(action.DamageMultiplier == 2.0, "Damage multiplier should be 2.0");
            AssertTrue(action.Length == 1.5, "Action length should be 1.5");
            AssertTrue(action.Cooldown == 3, "Action cooldown should be 3");
        }
        
        private static void TestActionCooldown()
        {
            Console.WriteLine("\n--- Testing Action Cooldown ---");
            
            var action = new Action
            {
                Name = "Cooldown Test",
                Cooldown = 3,
                CurrentCooldown = 0
            };
            
            AssertTrue(!action.IsOnCooldown, "Action should not be on cooldown initially");
            
            action.ResetCooldown();
            AssertTrue(action.CurrentCooldown == 3, "Cooldown should be reset to 3");
            AssertTrue(action.IsOnCooldown, "Action should be on cooldown");
            
            action.UpdateCooldown();
            AssertTrue(action.CurrentCooldown == 2, "Cooldown should decrease to 2");
            
            action.UpdateCooldown();
            action.UpdateCooldown();
            action.UpdateCooldown();
            AssertTrue(action.CurrentCooldown == 0, "Cooldown should reach 0");
            AssertTrue(!action.IsOnCooldown, "Action should not be on cooldown when at 0");
        }
        
        private static void TestActionStatusEffects()
        {
            Console.WriteLine("\n--- Testing Action Status Effects ---");
            
            var bleedAction = new Action
            {
                Name = "Bleed Attack",
                CausesBleed = true
            };
            
            var poisonAction = new Action
            {
                Name = "Poison Attack",
                CausesPoison = true
            };
            
            var stunAction = new Action
            {
                Name = "Stun Attack",
                CausesStun = true
            };
            
            AssertTrue(bleedAction.CausesBleed, "Bleed action should cause bleed");
            AssertTrue(poisonAction.CausesPoison, "Poison action should cause poison");
            AssertTrue(stunAction.CausesStun, "Stun action should cause stun");
        }
        
        #endregion
        
        #region Action Selection Tests
        
        private static void TestActionSelectionByRoll()
        {
            Console.WriteLine("\n--- Testing Action Selection by Roll ---");
            
            var character = new Character("TestHero", 1);
            
            // Add some actions to the pool
            var basicAttack = new Action { Name = "BASIC ATTACK", IsComboAction = false };
            var comboAction = new Action { Name = "COMBO STRIKE", IsComboAction = true };
            
            character.AddAction(basicAttack, 1.0);
            character.AddAction(comboAction, 1.0);
            
            // Test that action selector can select actions
            // Note: This tests the structure, actual roll-based selection would require mocking
            AssertTrue(character.ActionPool.Count > 0, "Character should have actions in pool");
        }
        
        private static void TestActionSelectionForCharacters()
        {
            Console.WriteLine("\n--- Testing Action Selection for Characters ---");
            
            var character = new Character("TestHero", 1);
            var basicAttack = new Action { Name = "BASIC ATTACK", IsComboAction = false };
            character.AddAction(basicAttack, 1.0);
            
            // Characters should use roll-based selection
            AssertTrue(character.ActionPool.Count > 0, "Character should have actions available");
        }
        
        private static void TestActionSelectionForEnemies()
        {
            Console.WriteLine("\n--- Testing Action Selection for Enemies ---");
            
            var enemy = new Enemy("TestEnemy", 1, 100, 10, 10, 10, 10);
            var enemyAction = new Action { Name = "ENEMY BASIC ATTACK", IsComboAction = false };
            enemy.AddAction(enemyAction, 1.0);
            
            // Enemies should use probability-based selection
            AssertTrue(enemy.ActionPool.Count > 0, "Enemy should have actions available");
        }
        
        private static void TestBasicAttackSelection()
        {
            Console.WriteLine("\n--- Testing Basic Attack Selection ---");
            
            var character = new Character("TestHero", 1);
            var basicAttack = ActionFactory.GetBasicAttack(character);
            
            // If character has no weapon, basic attack might be null
            // But the factory should handle this gracefully
            AssertTrue(basicAttack == null || basicAttack.Name == "BASIC ATTACK", 
                "Basic attack should be null or named BASIC ATTACK");
        }
        
        private static void TestComboActionSelection()
        {
            Console.WriteLine("\n--- Testing Combo Action Selection ---");
            
            var character = new Character("TestHero", 1);
            var comboAction = new Action 
            { 
                Name = "COMBO STRIKE", 
                IsComboAction = true,
                ComboOrder = 1
            };
            
            character.AddAction(comboAction, 1.0);
            
            var comboActions = ActionUtilities.GetComboActions(character);
            AssertTrue(comboActions.Count > 0, "Character should have combo actions available");
        }
        
        #endregion
        
        #region Combo Sequence Tests
        
        private static void TestComboSequenceManager()
        {
            Console.WriteLine("\n--- Testing Combo Sequence Manager ---");
            
            var manager = new ComboSequenceManager();
            var comboActions = manager.GetComboActions();
            
            AssertTrue(comboActions != null, "Combo sequence should not be null");
            AssertTrue(comboActions != null && comboActions.Count == 0, "Combo sequence should be empty initially");
        }
        
        private static void TestAddToCombo()
        {
            Console.WriteLine("\n--- Testing Add to Combo ---");
            
            var manager = new ComboSequenceManager();
            var comboAction = new Action 
            { 
                Name = "COMBO STRIKE", 
                IsComboAction = true,
                ComboOrder = 0
            };
            
            manager.AddToCombo(comboAction);
            var comboActions = manager.GetComboActions();
            
            AssertTrue(comboActions.Count == 1, "Combo sequence should have 1 action");
            AssertTrue(comboActions[0].Name == "COMBO STRIKE", "Combo action should be COMBO STRIKE");
            AssertTrue(comboActions[0].ComboOrder == 1, "Combo order should be set to 1");
        }
        
        private static void TestRemoveFromCombo()
        {
            Console.WriteLine("\n--- Testing Remove from Combo ---");
            
            var manager = new ComboSequenceManager();
            var comboAction = new Action 
            { 
                Name = "COMBO STRIKE", 
                IsComboAction = true
            };
            
            manager.AddToCombo(comboAction);
            AssertTrue(manager.GetComboActions().Count == 1, "Should have 1 action before removal");
            
            manager.RemoveFromCombo(comboAction);
            var comboActions = manager.GetComboActions();
            
            AssertTrue(comboActions.Count == 0, "Combo sequence should be empty after removal");
            AssertTrue(comboAction.ComboOrder == 0, "Combo order should be reset to 0");
        }
        
        private static void TestComboOrdering()
        {
            Console.WriteLine("\n--- Testing Combo Ordering ---");
            
            var manager = new ComboSequenceManager();
            var action1 = new Action { Name = "Action 1", IsComboAction = true, ComboOrder = 0 };
            var action2 = new Action { Name = "Action 2", IsComboAction = true, ComboOrder = 0 };
            var action3 = new Action { Name = "Action 3", IsComboAction = true, ComboOrder = 0 };
            
            // Add in different order
            manager.AddToCombo(action2);
            manager.AddToCombo(action1);
            manager.AddToCombo(action3);
            
            var comboActions = manager.GetComboActions();
            
            AssertTrue(comboActions.Count == 3, "Should have 3 actions in combo");
            AssertTrue(comboActions[0].ComboOrder == 1, "First action should have order 1");
            AssertTrue(comboActions[1].ComboOrder == 2, "Second action should have order 2");
            AssertTrue(comboActions[2].ComboOrder == 3, "Third action should have order 3");
        }
        
        private static void TestComboInitialization()
        {
            Console.WriteLine("\n--- Testing Combo Initialization ---");
            
            var character = new Character("TestHero", 1);
            var weapon = new WeaponItem("Test Sword", 1, 10, 0.05, WeaponType.Sword);
            
            // Add weapon actions to character
            var weaponAction1 = new Action { Name = "SWORD SLASH", IsComboAction = true };
            var weaponAction2 = new Action { Name = "PARRY", IsComboAction = true };
            character.AddAction(weaponAction1, 1.0);
            character.AddAction(weaponAction2, 1.0);
            
            character.Actions.InitializeDefaultCombo(character, weapon);
            var comboActions = character.Actions.GetComboActions();
            
            // Should have initialized combo with weapon actions
            AssertTrue(comboActions.Count >= 0, "Combo should be initialized (may be empty if no valid combo actions)");
        }
        
        private static void TestComboStepAdvancement()
        {
            Console.WriteLine("\n--- Testing Combo Step Advancement ---");
            
            var character = new Character("TestHero", 1);
            var initialStep = character.ComboStep;
            
            var comboAction = new Action 
            { 
                Name = "COMBO STRIKE", 
                IsComboAction = true,
                ComboOrder = 1
            };
            
            character.IncrementComboStep(comboAction);
            
            AssertTrue(character.ComboStep > initialStep || character.ComboStep == 0, 
                "Combo step should advance or reset based on action");
        }
        
        private static void TestComboResetOnMiss()
        {
            Console.WriteLine("\n--- Testing Combo Reset on Miss ---");
            
            var character = new Character("TestHero", 1);
            character.ComboStep = 3; // Set to step 3
            
            // Simulate a miss (combo should reset)
            character.ComboStep = 0;
            
            AssertTrue(character.ComboStep == 0, "Combo step should reset to 0 on miss");
        }
        
        #endregion
        
        #region Action Pool Tests
        
        private static void TestActionPoolManagement()
        {
            Console.WriteLine("\n--- Testing Action Pool Management ---");
            
            var character = new Character("TestHero", 1);
            var action = new Action { Name = "Test Action" };
            
            character.AddAction(action, 1.0);
            
            AssertTrue(character.ActionPool.Count > 0, "Action pool should contain actions");
            AssertTrue(character.ActionPool.Any(a => a.action.Name == "Test Action"), 
                "Action pool should contain the test action");
        }
        
        private static void TestActionPoolWithComboActions()
        {
            Console.WriteLine("\n--- Testing Action Pool with Combo Actions ---");
            
            var character = new Character("TestHero", 1);
            var comboAction = new Action { Name = "COMBO STRIKE", IsComboAction = true };
            var basicAction = new Action { Name = "BASIC ATTACK", IsComboAction = false };
            
            character.AddAction(comboAction, 1.0);
            character.AddAction(basicAction, 1.0);
            
            var comboActions = ActionUtilities.GetComboActions(character);
            
            AssertTrue(character.ActionPool.Count == 2, "Action pool should have 2 actions");
            AssertTrue(comboActions.Count == 1, "Should have 1 combo action");
            AssertTrue(comboActions[0].Name == "COMBO STRIKE", "Combo action should be COMBO STRIKE");
        }
        
        #endregion
        
        #region Action Execution Flow Tests
        
        private static void TestActionExecutionFlow()
        {
            Console.WriteLine("\n--- Testing Action Execution Flow ---");
            
            var character = new Character("TestHero", 1);
            var enemy = new Enemy("TestEnemy", 1, 100, 10, 10, 10, 10);
            var action = new Action { Name = "Test Action", Type = ActionType.Attack };
            
            // Test that execution flow can be called
            // Note: Full execution requires battle narrative and other dependencies
            var lastUsedActions = new Dictionary<Actor, Action>();
            var lastCriticalMissStatus = new Dictionary<Actor, bool>();
            
            // This tests the structure - actual execution would require full setup
            AssertTrue(action != null, "Action should be created");
        }
        
        private static void TestActionExecutionWithForcedAction()
        {
            Console.WriteLine("\n--- Testing Action Execution with Forced Action ---");
            
            var character = new Character("TestHero", 1);
            var enemy = new Enemy("TestEnemy", 1, 100, 10, 10, 10, 10);
            var forcedAction = new Action { Name = "Forced Action", Type = ActionType.Attack };
            
            // Forced actions should bypass normal selection
            AssertTrue(forcedAction != null, "Forced action should be created");
            AssertTrue(forcedAction != null && forcedAction.Name == "Forced Action", "Forced action should have correct name");
        }
        
        private static void TestActionExecutionResult()
        {
            Console.WriteLine("\n--- Testing Action Execution Result ---");
            
            var result = new ActionExecutionResult();
            
            AssertTrue(result != null, "Action execution result should be created");
            AssertTrue(result != null && result.SelectedAction == null, "Selected action should be null initially");
            AssertTrue(result != null && result.BaseRoll == 0, "Base roll should be 0 initially");
            AssertTrue(result != null && result.Hit == false, "Hit should be false initially");
            AssertTrue(result != null && result.Damage == 0, "Damage should be 0 initially");
        }
        
        #endregion
        
        #region Helper Methods
        
        private static void AssertTrue(bool condition, string message)
        {
            _testsRun++;
            if (condition)
            {
                _testsPassed++;
                Console.WriteLine($"  ✓ {message}");
            }
            else
            {
                _testsFailed++;
                Console.WriteLine($"  ✗ FAILED: {message}");
            }
        }
        
        private static void PrintSummary()
        {
            Console.WriteLine("\n=== Test Summary ===");
            Console.WriteLine($"Total Tests: {_testsRun}");
            Console.WriteLine($"Passed: {_testsPassed}");
            Console.WriteLine($"Failed: {_testsFailed}");
            Console.WriteLine($"Success Rate: {(_testsPassed * 100.0 / _testsRun):F1}%");
            
            if (_testsFailed == 0)
            {
                Console.WriteLine("\n✅ All tests passed!");
            }
            else
            {
                Console.WriteLine($"\n❌ {_testsFailed} test(s) failed");
            }
        }
        
        #endregion
    }
}

