using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Actions.Conditional;
using RPGGame.Combat.Events;
using RPGGame.Tests; // For TestBase

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Tests for Conditional Trigger Tests
    /// </summary>
    public static class ComboDiceRollTestsConditionalTriggers
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            Console.WriteLine("--- Conditional Trigger Tests ---");
            TestOnComboTrigger();
            TestOnHitTrigger();
            TestOnNormalHitTrigger();
            TestOnCriticalHitTrigger();
            TestConditionalTriggerWithWrongCondition();
            TestBase.PrintSummary("Conditional Trigger Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestOnComboTrigger()
        {
            Console.WriteLine("\n--- Testing OnCombo Trigger ---");
            
            var evaluator = new ConditionalTriggerEvaluator();
            var character = CreateTestCharacter();
            var enemy = new Enemy("TestEnemy", 1, 100, 10, 10, 10, 10);
            var action = new Action { Name = "Test Action" };
            
            // Create a combo hit event
            var comboEvent = new CombatEvent(CombatEventType.ActionHit, character)
            {
                Target = enemy,
                Action = action,
                IsCombo = true,
                IsCritical = false,
                RollValue = 15
            };
            
            var condition = new TriggerCondition(TriggerConditionType.OnComboHit);
            
            bool shouldTrigger = evaluator.EvaluateConditions(
                new List<TriggerCondition> { condition }, 
                comboEvent, character, enemy, action);
            
            TestBase.AssertTrue(shouldTrigger, "OnCombo trigger should fire on combo hit", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestOnHitTrigger()
        {
            Console.WriteLine("\n--- Testing OnHit Trigger ---");
            
            var evaluator = new ConditionalTriggerEvaluator();
            var character = CreateTestCharacter();
            var enemy = new Enemy("TestEnemy", 1, 100, 10, 10, 10, 10);
            var action = new Action { Name = "Test Action" };
            
            // Create a normal hit event (not combo, not critical)
            var normalHitEvent = new CombatEvent(CombatEventType.ActionHit, character)
            {
                Target = enemy,
                Action = action,
                IsCombo = false,
                IsCritical = false,
                RollValue = 10
            };
            
            var condition = new TriggerCondition(TriggerConditionType.OnNormalHit);
            
            bool shouldTrigger = evaluator.EvaluateConditions(
                new List<TriggerCondition> { condition }, 
                normalHitEvent, character, enemy, action);
            
            TestBase.AssertTrue(shouldTrigger, "OnNormalHit trigger should fire on normal hit", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestOnNormalHitTrigger()
        {
            Console.WriteLine("\n--- Testing OnNormalHit Trigger (Not on Combo) ---");
            
            var evaluator = new ConditionalTriggerEvaluator();
            var character = CreateTestCharacter();
            var enemy = new Enemy("TestEnemy", 1, 100, 10, 10, 10, 10);
            var action = new Action { Name = "Test Action" };
            
            // Create a combo hit event
            var comboEvent = new CombatEvent(CombatEventType.ActionHit, character)
            {
                Target = enemy,
                Action = action,
                IsCombo = true,  // This is a combo, so OnNormalHit should NOT trigger
                IsCritical = false,
                RollValue = 15
            };
            
            var condition = new TriggerCondition(TriggerConditionType.OnNormalHit);
            
            bool shouldTrigger = evaluator.EvaluateConditions(
                new List<TriggerCondition> { condition }, 
                comboEvent, character, enemy, action);
            
            TestBase.AssertTrue(!shouldTrigger, "OnNormalHit trigger should NOT fire on combo hit", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestOnCriticalHitTrigger()
        {
            Console.WriteLine("\n--- Testing OnCriticalHit Trigger ---");
            
            var evaluator = new ConditionalTriggerEvaluator();
            var character = CreateTestCharacter();
            var enemy = new Enemy("TestEnemy", 1, 100, 10, 10, 10, 10);
            var action = new Action { Name = "Test Action" };
            
            // Create a critical hit event
            var criticalEvent = new CombatEvent(CombatEventType.ActionHit, character)
            {
                Target = enemy,
                Action = action,
                IsCombo = false,
                IsCritical = true,
                RollValue = 20
            };
            
            var condition = new TriggerCondition(TriggerConditionType.OnCriticalHit);
            
            bool shouldTrigger = evaluator.EvaluateConditions(
                new List<TriggerCondition> { condition }, 
                criticalEvent, character, enemy, action);
            
            TestBase.AssertTrue(shouldTrigger, "OnCriticalHit trigger should fire on critical hit", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestConditionalTriggerWithWrongCondition()
        {
            Console.WriteLine("\n--- Testing Conditional Trigger with Wrong Condition ---");
            
            var evaluator = new ConditionalTriggerEvaluator();
            var character = CreateTestCharacter();
            var enemy = new Enemy("TestEnemy", 1, 100, 10, 10, 10, 10);
            var action = new Action { Name = "Test Action" };
            
            // Create a normal hit event
            var normalHitEvent = new CombatEvent(CombatEventType.ActionHit, character)
            {
                Target = enemy,
                Action = action,
                IsCombo = false,
                IsCritical = false,
                RollValue = 10
            };
            
            // But require OnCombo trigger
            var condition = new TriggerCondition(TriggerConditionType.OnComboHit);
            
            bool shouldTrigger = evaluator.EvaluateConditions(
                new List<TriggerCondition> { condition }, 
                normalHitEvent, character, enemy, action);
            
            TestBase.AssertTrue(!shouldTrigger, "OnCombo trigger should NOT fire on normal hit", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static Character CreateTestCharacter()
        {
            var character = new Character("TestHero", 1);
            // BASIC ATTACK has been removed from the game
            return character;
        }
    }
}

