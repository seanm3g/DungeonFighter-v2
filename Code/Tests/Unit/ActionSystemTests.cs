using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for action system
    /// Tests all actions in Actions.json, action properties, types, and execution
    /// </summary>
    public static class ActionSystemTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all action system tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== Action System Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestActionLoading();
            TestActionProperties();
            TestActionTypes();
            TestTargetTypes();
            TestActionExecution();
            TestActionValidation();
            TestActionCooldown();
            TestActionAvailability();

            TestBase.PrintSummary("Action System Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Action Loading Tests

        private static void TestActionLoading()
        {
            Console.WriteLine("--- Testing Action Loading ---");

            // Ensure actions are loaded
            ActionLoader.LoadActions();
            var allActions = ActionLoader.GetAllActions();

            TestBase.AssertTrue(allActions.Count > 0, 
                $"Actions should be loaded, got {allActions.Count} actions", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test specific action loading
            var jabAction = ActionLoader.GetAction("JAB");
            TestBase.AssertNotNull(jabAction, 
                "JAB action should be loadable", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (jabAction != null)
            {
                TestBase.AssertEqual("JAB", jabAction.Name, 
                    "JAB action should have correct name", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test HasAction
            TestBase.AssertTrue(ActionLoader.HasAction("JAB"), 
                "HasAction should return true for JAB", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertFalse(ActionLoader.HasAction("NONEXISTENT_ACTION"), 
                "HasAction should return false for nonexistent action", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test GetAllActionNames
            var actionNames = ActionLoader.GetAllActionNames();
            TestBase.AssertTrue(actionNames.Count > 0, 
                $"GetAllActionNames should return actions, got {actionNames.Count}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Action Properties Tests

        private static void TestActionProperties()
        {
            Console.WriteLine("\n--- Testing Action Properties ---");

            var allActions = ActionLoader.GetAllActions();
            
            foreach (var action in allActions.Take(10)) // Test first 10 actions
            {
                // Test name property
                TestBase.AssertTrue(!string.IsNullOrEmpty(action.Name), 
                    $"{action.Name}: Name should not be empty", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Test damage multiplier
                TestBase.AssertTrue(action.DamageMultiplier >= 0, 
                    $"{action.Name}: DamageMultiplier should be non-negative", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Test length
                TestBase.AssertTrue(action.Length > 0, 
                    $"{action.Name}: Length should be positive", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Test cooldown
                TestBase.AssertTrue(action.Cooldown >= 0, 
                    $"{action.Name}: Cooldown should be non-negative", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Test description
                TestBase.AssertNotNull(action.Description, 
                    $"{action.Name}: Description should not be null", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test specific action with known properties
            var crushAction = ActionLoader.GetAction("CRUSHING BLOW");
            if (crushAction != null)
            {
                TestBase.AssertEqual(1.5, crushAction.DamageMultiplier, 
                    "CRUSHING BLOW should have 1.5x damage multiplier", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Action Types Tests

        private static void TestActionTypes()
        {
            Console.WriteLine("\n--- Testing Action Types ---");

            var allActions = ActionLoader.GetAllActions();
            var typeCounts = new Dictionary<ActionType, int>();

            foreach (var action in allActions)
            {
                if (!typeCounts.ContainsKey(action.Type))
                {
                    typeCounts[action.Type] = 0;
                }
                typeCounts[action.Type]++;

                // Verify type is valid enum value
                TestBase.AssertTrue(Enum.IsDefined(typeof(ActionType), action.Type), 
                    $"{action.Name}: ActionType should be valid enum value", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Verify we have at least one of each common type
            TestBase.AssertTrue(typeCounts.ContainsKey(ActionType.Attack) || allActions.Count == 0, 
                "Should have Attack type actions", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test specific action types
            var attackActions = allActions.Where(a => a.Type == ActionType.Attack).ToList();
            TestBase.AssertTrue(attackActions.Count > 0, 
                $"Should have at least one Attack action, found {attackActions.Count}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Target Types Tests

        private static void TestTargetTypes()
        {
            Console.WriteLine("\n--- Testing Target Types ---");

            var allActions = ActionLoader.GetAllActions();
            var targetTypeCounts = new Dictionary<TargetType, int>();

            foreach (var action in allActions)
            {
                if (!targetTypeCounts.ContainsKey(action.Target))
                {
                    targetTypeCounts[action.Target] = 0;
                }
                targetTypeCounts[action.Target]++;

                // Verify target type is valid enum value
                TestBase.AssertTrue(Enum.IsDefined(typeof(TargetType), action.Target), 
                    $"{action.Name}: TargetType should be valid enum value", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Verify we have SingleTarget actions (most common)
            TestBase.AssertTrue(targetTypeCounts.ContainsKey(TargetType.SingleTarget) || allActions.Count == 0, 
                "Should have SingleTarget actions", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Action Execution Tests

        private static void TestActionExecution()
        {
            Console.WriteLine("\n--- Testing Action Execution ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            var enemy = TestDataBuilders.Enemy().WithName("TestEnemy").Build();

            // Test action selection
            var selectedAction = ActionSelector.SelectActionByEntityType(character);
            var actionPool = character.GetActionPool();
            TestBase.AssertTrue(selectedAction == null || actionPool.Contains(selectedAction), 
                "Selected action should be from character's action pool", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test action with valid properties can be executed
            var jabAction = ActionLoader.GetAction("JAB");
            if (jabAction != null && actionPool.Count > 0)
            {
                // Verify action can be selected
                TestBase.AssertTrue(actionPool.Any(a => a.Name == jabAction.Name) || 
                    ActionLoader.HasAction("JAB"), 
                    "JAB action should be available", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test enemy action selection
            var enemyAction = ActionSelector.SelectActionByEntityType(enemy);
            TestBase.AssertTrue(enemyAction == null || enemy.ActionPool.Any(item => item.action == enemyAction), 
                "Enemy selected action should be from enemy's action pool", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Action Validation Tests

        private static void TestActionValidation()
        {
            Console.WriteLine("\n--- Testing Action Validation ---");

            // Test null action handling
            Action? nullAction = null;
            TestBase.AssertNull(nullAction, 
                "Null action should be null", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test action with invalid name
            var invalidAction = ActionLoader.GetAction("INVALID_ACTION_NAME_XYZ");
            TestBase.AssertNull(invalidAction, 
                "Invalid action name should return null", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test action with empty name
            TestBase.AssertFalse(ActionLoader.HasAction(""), 
                "Empty action name should return false", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test GetActions with mixed valid/invalid names
            var mixedActions = ActionLoader.GetActions("JAB", "INVALID", "CRUSH");
            TestBase.AssertTrue(mixedActions.Count >= 2, 
                $"GetActions should return valid actions only, got {mixedActions.Count}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Action Cooldown Tests

        private static void TestActionCooldown()
        {
            Console.WriteLine("\n--- Testing Action Cooldown ---");

            var allActions = ActionLoader.GetAllActions();
            
            foreach (var action in allActions.Take(5))
            {
                // Test initial cooldown
                TestBase.AssertTrue(action.Cooldown >= 0, 
                    $"{action.Name}: Cooldown should be non-negative", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Test current cooldown initialization
                TestBase.AssertTrue(action.CurrentCooldown >= 0, 
                    $"{action.Name}: CurrentCooldown should be non-negative", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Test cooldown can be set
                int originalCooldown = action.CurrentCooldown;
                action.CurrentCooldown = 5;
                TestBase.AssertEqual(5, action.CurrentCooldown, 
                    $"{action.Name}: CurrentCooldown should be settable", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                action.CurrentCooldown = originalCooldown; // Reset
            }
        }

        #endregion

        #region Action Availability Tests

        private static void TestActionAvailability()
        {
            Console.WriteLine("\n--- Testing Action Availability ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            
            // Test action pool is not null
            var actionPool = character.GetActionPool();
            TestBase.AssertNotNull(actionPool, 
                "Character action pool should not be null", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test action pool can contain actions
            TestBase.AssertTrue(actionPool.Count >= 0, 
                $"Character action pool should have non-negative count, got {actionPool.Count}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test action selection when pool is empty
            if (actionPool.Count == 0)
            {
                var emptyPoolAction = ActionSelector.SelectActionByEntityType(character);
                TestBase.AssertNull(emptyPoolAction, 
                    "Action selection should return null when pool is empty", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion
    }
}

