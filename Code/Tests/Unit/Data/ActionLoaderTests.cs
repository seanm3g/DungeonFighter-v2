using System;
using System.Collections.Generic;
using System.IO;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    /// <summary>
    /// Comprehensive tests for ActionLoader
    /// Tests action loading from JSON, action retrieval, and error handling
    /// </summary>
    public static class ActionLoaderTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all ActionLoader tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionLoader Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestActionLoading();
            TestGetAction();
            TestGetActions();
            TestHasAction();
            TestGetAllActionNames();
            TestGetAllActions();
            TestActionProperties();

            TestBase.PrintSummary("ActionLoader Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Action Loading Tests

        private static void TestActionLoading()
        {
            Console.WriteLine("--- Testing Action Loading ---");

            // Test that actions can be loaded
            ActionLoader.LoadActions();
            var allActions = ActionLoader.GetAllActions();

            TestBase.AssertTrue(allActions.Count > 0, 
                $"Actions should be loaded, got {allActions.Count} actions", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test that loading multiple times doesn't break
            ActionLoader.LoadActions();
            var allActions2 = ActionLoader.GetAllActions();
            TestBase.AssertEqual(allActions.Count, allActions2.Count,
                "Loading actions multiple times should return same count",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Action Retrieval Tests

        private static void TestGetAction()
        {
            Console.WriteLine("\n--- Testing GetAction ---");

            // Test getting a known action
            var jabAction = ActionLoader.GetAction("JAB");
            TestBase.AssertNotNull(jabAction,
                "JAB action should be retrievable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (jabAction != null)
            {
                TestBase.AssertEqual("JAB", jabAction.Name,
                    "JAB action should have correct name",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test getting non-existent action
            var nonExistent = ActionLoader.GetAction("NONEXISTENT_ACTION_XYZ");
            TestBase.AssertNull(nonExistent,
                "Non-existent action should return null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test getting action with null/empty name
            var nullAction = ActionLoader.GetAction("");
            TestBase.AssertNull(nullAction,
                "Empty action name should return null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGetActions()
        {
            Console.WriteLine("\n--- Testing GetActions (multiple) ---");

            // Test getting multiple actions
            var actions = ActionLoader.GetActions("JAB", "SLASH", "CRUSH");
            TestBase.AssertTrue(actions.Count > 0,
                $"GetActions should return actions, got {actions.Count}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with mix of existing and non-existing
            var mixedActions = ActionLoader.GetActions("JAB", "NONEXISTENT", "SLASH");
            TestBase.AssertTrue(mixedActions.Count >= 2,
                $"GetActions should return existing actions, got {mixedActions.Count}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Action Query Tests

        private static void TestHasAction()
        {
            Console.WriteLine("\n--- Testing HasAction ---");

            // Test with existing action
            TestBase.AssertTrue(ActionLoader.HasAction("JAB"),
                "HasAction should return true for JAB",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with non-existent action
            TestBase.AssertFalse(ActionLoader.HasAction("NONEXISTENT_ACTION_XYZ"),
                "HasAction should return false for non-existent action",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with empty string
            TestBase.AssertFalse(ActionLoader.HasAction(""),
                "HasAction should return false for empty string",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGetAllActionNames()
        {
            Console.WriteLine("\n--- Testing GetAllActionNames ---");

            var actionNames = ActionLoader.GetAllActionNames();
            TestBase.AssertTrue(actionNames.Count > 0,
                $"GetAllActionNames should return action names, got {actionNames.Count}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test that names are not empty
            foreach (var name in actionNames)
            {
                TestBase.AssertTrue(!string.IsNullOrEmpty(name),
                    $"Action name should not be empty: '{name}'",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test that JAB is in the list
            TestBase.AssertTrue(actionNames.Contains("JAB"),
                "GetAllActionNames should include JAB",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGetAllActions()
        {
            Console.WriteLine("\n--- Testing GetAllActions ---");

            var allActions = ActionLoader.GetAllActions();
            TestBase.AssertTrue(allActions.Count > 0,
                $"GetAllActions should return actions, got {allActions.Count}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test that all actions have names
            foreach (var action in allActions)
            {
                TestBase.AssertNotNull(action,
                    "Action should not be null",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                
                if (action != null)
                {
                    TestBase.AssertTrue(!string.IsNullOrEmpty(action.Name),
                        "Action should have a name",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
        }

        #endregion

        #region Action Properties Tests

        private static void TestActionProperties()
        {
            Console.WriteLine("\n--- Testing Action Properties ---");

            var jabAction = ActionLoader.GetAction("JAB");
            if (jabAction == null)
            {
                TestBase.AssertTrue(false,
                    "JAB action should exist for property tests",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            // Test basic properties
            TestBase.AssertTrue(!string.IsNullOrEmpty(jabAction.Name),
                "Action should have a name",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(!string.IsNullOrEmpty(jabAction.Description),
                "Action should have a description",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test damage multiplier is non-negative
            TestBase.AssertTrue(jabAction.DamageMultiplier >= 0,
                $"Damage multiplier should be non-negative, got {jabAction.DamageMultiplier}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test length is positive
            TestBase.AssertTrue(jabAction.Length > 0,
                $"Length should be positive, got {jabAction.Length}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test cooldown is non-negative
            TestBase.AssertTrue(jabAction.Cooldown >= 0,
                $"Cooldown should be non-negative, got {jabAction.Cooldown}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
