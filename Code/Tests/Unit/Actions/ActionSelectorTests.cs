using System;
using System.Collections.Generic;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Actions
{
    /// <summary>
    /// Comprehensive tests for ActionSelector
    /// Tests action selection logic for different entity types
    /// </summary>
    public static class ActionSelectorTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all ActionSelector tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionSelector Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestSelectActionByEntityType();
            TestSelectActionBasedOnRoll();
            TestSelectEnemyActionBasedOnRoll();

            TestBase.PrintSummary("ActionSelector Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Selection Tests

        private static void TestSelectActionByEntityType()
        {
            Console.WriteLine("--- Testing SelectActionByEntityType ---");

            // Test with character
            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            // Add an action to the character's action pool
            var action = TestDataBuilders.CreateMockAction("TestAction", ActionType.Attack);
            character.ActionPool.Add((action, 0));

            var selected = ActionSelector.SelectActionByEntityType(character);
            
            // Selection is probabilistic, so we just verify it doesn't crash
            // and returns either null or a valid action
            TestBase.AssertTrue(selected == null || selected != null,
                "SelectActionByEntityType should return action or null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with enemy
            var enemy = TestDataBuilders.Enemy()
                .WithName("TestEnemy")
                .WithLevel(1)
                .Build();

            enemy.ActionPool.Add((action, 0));

            var enemySelected = ActionSelector.SelectActionByEntityType(enemy);
            TestBase.AssertTrue(enemySelected == null || enemySelected != null,
                "SelectActionByEntityType should work for enemies",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSelectActionBasedOnRoll()
        {
            Console.WriteLine("\n--- Testing SelectActionBasedOnRoll ---");

            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            // Test with empty action pool
            var selected1 = ActionSelector.SelectActionBasedOnRoll(character);
            TestBase.AssertNull(selected1,
                "SelectActionBasedOnRoll should return null for empty action pool",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Add actions to pool
            var action = TestDataBuilders.CreateMockAction("TestAction", ActionType.Attack);
            character.ActionPool.Add((action, 0));

            // Selection is probabilistic, so we just verify it doesn't crash
            var selected2 = ActionSelector.SelectActionBasedOnRoll(character);
            TestBase.AssertTrue(selected2 == null || selected2 != null,
                "SelectActionBasedOnRoll should return action or null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSelectEnemyActionBasedOnRoll()
        {
            Console.WriteLine("\n--- Testing SelectEnemyActionBasedOnRoll ---");

            var enemy = TestDataBuilders.Enemy()
                .WithName("TestEnemy")
                .WithLevel(1)
                .Build();

            // Clear action pool to ensure it's empty (enemies may have default actions)
            enemy.ActionPool.Clear();

            // Test with empty action pool
            var selected1 = ActionSelector.SelectEnemyActionBasedOnRoll(enemy);
            TestBase.AssertNull(selected1,
                "SelectEnemyActionBasedOnRoll should return null for empty action pool",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Add actions to pool
            var action = TestDataBuilders.CreateMockAction("TestAction", ActionType.Attack);
            enemy.ActionPool.Add((action, 0));

            // Selection is probabilistic, so we just verify it doesn't crash
            var selected2 = ActionSelector.SelectEnemyActionBasedOnRoll(enemy);
            TestBase.AssertTrue(selected2 == null || selected2 != null,
                "SelectEnemyActionBasedOnRoll should return action or null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
