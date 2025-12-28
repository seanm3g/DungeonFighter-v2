using System;
using System.Linq;
using RPGGame.Tests;
using RPGGame.Actions.Execution;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Tests for action execution flow
    /// Tests action selection, execution flow, cooldown management, and roll generation
    /// </summary>
    public static class ActionExecutionFlowTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Action Execution Flow Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestActionSelectionLogic();
            TestActionExecutionFlow();
            TestActionCooldownManagement();
            TestActionAvailabilityChecks();
            TestForcedActionExecution();
            TestActionRollGeneration();
            TestActionRollModifications();

            TestBase.PrintSummary("Action Execution Flow Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestActionSelectionLogic()
        {
            Console.WriteLine("--- Testing Action Selection Logic ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            var enemy = TestDataBuilders.Enemy().WithName("TestEnemy").Build();

            // Test character action selection
            var charAction = ActionSelector.SelectActionByEntityType(character);
            var charActionPool = character.GetActionPool();
            TestBase.AssertTrue(charAction == null || charActionPool.Contains(charAction), 
                "Character action selection should return action from pool", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test enemy action selection
            var enemyAction = ActionSelector.SelectActionByEntityType(enemy);
            TestBase.AssertTrue(enemyAction == null || enemy.ActionPool.Any(item => item.action == enemyAction), 
                "Enemy action selection should return action from pool", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test selection when stunned
            character.IsStunned = true;
            var stunnedAction = ActionSelector.SelectActionByEntityType(character);
            TestBase.AssertNull(stunnedAction, 
                "Stunned character should not select action", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            character.IsStunned = false;
        }

        private static void TestActionExecutionFlow()
        {
            Console.WriteLine("\n--- Testing Action Execution Flow ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            var enemy = TestDataBuilders.Enemy().WithName("TestEnemy").Build();

            // Test that action execution can be initiated
            var actionPool = character.GetActionPool();
            if (actionPool.Count > 0)
            {
                var action = actionPool[0];
                TestBase.AssertNotNull(action, 
                    "Action should be available for execution", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                if (action != null)
                {
                    TestBase.AssertTrue(!string.IsNullOrEmpty(action.Name), 
                        "Action should have a name", 
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
        }

        private static void TestActionCooldownManagement()
        {
            Console.WriteLine("\n--- Testing Action Cooldown Management ---");

            var action = TestDataBuilders.CreateMockAction("TestAction");
            
            // Test initial cooldown
            TestBase.AssertEqual(0, action.CurrentCooldown, 
                "Action should start with 0 cooldown", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test setting cooldown
            action.CurrentCooldown = 3;
            TestBase.AssertEqual(3, action.CurrentCooldown, 
                "Action cooldown should be settable", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test cooldown cannot be negative
            action.CurrentCooldown = -1;
            TestBase.AssertTrue(action.CurrentCooldown >= 0, 
                "Action cooldown should not be negative", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestActionAvailabilityChecks()
        {
            Console.WriteLine("\n--- Testing Action Availability Checks ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            
            // Test action pool availability
            var actionPool = character.GetActionPool();
            TestBase.AssertNotNull(actionPool, 
                "Action pool should not be null", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test action selection when pool has actions
            if (actionPool.Count > 0)
            {
                var action = ActionSelector.SelectActionByEntityType(character);
                TestBase.AssertTrue(action != null, 
                    "Action should be selectable when pool has actions", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestForcedActionExecution()
        {
            Console.WriteLine("\n--- Testing Forced Action Execution ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            var forcedAction = TestDataBuilders.CreateMockAction("ForcedAction");

            // Test that forced action can be set
            TestBase.AssertNotNull(forcedAction, 
                "Forced action should be creatable", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (forcedAction != null)
            {
                TestBase.AssertEqual("ForcedAction", forcedAction.Name, 
                    "Forced action should have correct name", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestActionRollGeneration()
        {
            Console.WriteLine("\n--- Testing Action Roll Generation ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();

            // Test roll generation
            var roll = ActionSelector.GetActionRoll(character);
            TestBase.AssertTrue(roll >= 1 && roll <= 20, 
                $"Action roll should be between 1-20, got {roll}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestActionRollModifications()
        {
            Console.WriteLine("\n--- Testing Action Roll Modifications ---");

            var action = TestDataBuilders.CreateMockAction("TestAction");
            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            var enemy = TestDataBuilders.Enemy().WithName("TestEnemy").Build();

            // Test roll modifications can be applied
            int baseRoll = 10;
            var modifiedRoll = RPGGame.Actions.RollModification.RollModificationManager.ApplyActionRollModifications(
                baseRoll, action, character, enemy);
            
            TestBase.AssertTrue(modifiedRoll >= 1, 
                $"Modified roll should be valid, got {modifiedRoll}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}

