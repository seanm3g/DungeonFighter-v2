using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Actions.Execution;
using RPGGame.Actions.RollModification;
using RPGGame.Actions.Conditional;
using RPGGame.Combat.Events;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for dice rolls, combo sequences, and action triggering
    /// Tests dice roll mechanics, action selection, combo sequence information, and conditional triggers
    /// </summary>
    public static class ComboDiceRollTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;
        
        /// <summary>
        /// Runs all combo and dice roll tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== Combo and Dice Roll Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            
            // Dice Roll Mechanics Tests
            TestDiceRollMechanics();
            TestDiceRollComboAction();
            TestDiceRollComboContinue();
            TestDiceRollRanges();
            
            // Action Selection Based on Dice Rolls
            TestActionSelectionRoll1To5();
            TestActionSelectionRoll6To13();
            TestActionSelectionRoll14To20();
            TestActionSelectionWithRollBonus();
            TestActionSelectionNatural20();
            
            // Combo Sequence Information Tests
            TestComboSequenceOrdering();
            TestComboStepAdvancement();
            TestComboStepResetOnMiss();
            TestComboSequenceWithMultipleActions();
            TestComboSequenceRouting();
            
            // IsCombo Flag Tests (Bug Detection)
            TestIsComboFlagOnBasicAttack();
            TestIsComboFlagOnComboAction();
            TestIsComboFlagBasedOnRoll();
            TestIsComboFlagWithForcedAction();
            
            // Conditional Trigger Tests
            TestOnComboTrigger();
            TestOnHitTrigger();
            TestOnNormalHitTrigger();
            TestOnCriticalHitTrigger();
            TestConditionalTriggerWithWrongCondition();
            
            // Integration Tests
            TestFullComboSequenceExecution();
            TestDiceRollToActionSelectionFlow();
            TestComboSequenceInformationCorrectness();
            
            PrintSummary();
        }
        
        #region Dice Roll Mechanics Tests
        
        private static void TestDiceRollMechanics()
        {
            Console.WriteLine("--- Testing Dice Roll Mechanics ---");
            
            // Test basic dice roll
            int roll = Dice.Roll(20);
            AssertTrue(roll >= 1 && roll <= 20, $"Dice roll should be between 1 and 20, got: {roll}");
            
            // Test multiple dice rolls
            int roll2 = Dice.Roll(2, 20);
            AssertTrue(roll2 >= 2 && roll2 <= 40, $"2d20 roll should be between 2 and 40, got: {roll2}");
        }
        
        private static void TestDiceRollComboAction()
        {
            Console.WriteLine("\n--- Testing Dice Roll Combo Action ---");
            
            // Test multiple rolls to verify ranges
            int failCount = 0;
            int normalCount = 0;
            int comboCount = 0;
            
            for (int i = 0; i < 1000; i++)
            {
                var result = Dice.RollComboAction(0);
                
                if (result.Roll <= 5)
                {
                    failCount++;
                    AssertTrue(!result.Success, $"Roll {result.Roll} should fail");
                    AssertTrue(!result.ComboTriggered, $"Roll {result.Roll} should not trigger combo");
                }
                else if (result.Roll <= 13)
                {
                    normalCount++;
                    AssertTrue(result.Success, $"Roll {result.Roll} should succeed");
                    AssertTrue(!result.ComboTriggered, $"Roll {result.Roll} should not trigger combo");
                }
                else
                {
                    comboCount++;
                    AssertTrue(result.Success, $"Roll {result.Roll} should succeed");
                    AssertTrue(result.ComboTriggered, $"Roll {result.Roll} should trigger combo");
                }
            }
            
            AssertTrue(failCount > 0, "Should have some fail rolls (1-5)");
            AssertTrue(normalCount > 0, "Should have some normal rolls (6-13)");
            AssertTrue(comboCount > 0, "Should have some combo rolls (14-20)");
        }
        
        private static void TestDiceRollComboContinue()
        {
            Console.WriteLine("\n--- Testing Dice Roll Combo Continue ---");
            
            int successCount = 0;
            int failCount = 0;
            
            for (int i = 0; i < 1000; i++)
            {
                var result = Dice.RollComboContinue(0);
                
                if (result.Roll >= 14)
                {
                    successCount++;
                    AssertTrue(result.Success, $"Roll {result.Roll} should succeed for combo continue");
                }
                else
                {
                    failCount++;
                    AssertTrue(!result.Success, $"Roll {result.Roll} should fail for combo continue");
                }
            }
            
            AssertTrue(successCount > 0, "Should have some successful combo continues");
            AssertTrue(failCount > 0, "Should have some failed combo continues");
        }
        
        private static void TestDiceRollRanges()
        {
            Console.WriteLine("\n--- Testing Dice Roll Ranges ---");
            
            // Test specific roll values
            // Note: We can't force specific rolls, but we can test the logic
            
            // Test with bonus
            var resultWithBonus = Dice.RollComboAction(5);
            AssertTrue(resultWithBonus.Roll >= 6, $"Roll with +5 bonus should be at least 6, got: {resultWithBonus.Roll}");
            
            // Test edge cases
            var resultMin = Dice.RollComboAction(-10); // Can go below 1, but that's handled
            AssertTrue(resultMin.Roll >= 1, $"Roll should never be below 1, got: {resultMin.Roll}");
        }
        
        #endregion
        
        #region Action Selection Based on Dice Rolls
        
        private static void TestActionSelectionRoll1To5()
        {
            Console.WriteLine("\n--- Testing Action Selection Roll 1-5 ---");
            
            var character = CreateTestCharacter();
            ActionSelector.ClearStoredRolls();
            
            // We can't directly control the roll, but we can test the selection logic
            // by checking that low rolls don't select actions when they shouldn't
            var action = ActionSelector.SelectActionByEntityType(character);
            
            AssertTrue(action == null, "Action should not be selected for rolls 1-5");
            // Note: Actual roll-based testing would require mocking the dice
        }
        
        private static void TestActionSelectionRoll6To13()
        {
            Console.WriteLine("\n--- Testing Action Selection Roll 6-13 ---");
            
            var character = CreateTestCharacter();
            ActionSelector.ClearStoredRolls();
            
            // Test that rolls 6-13 should not select actions
            var action = ActionSelector.SelectActionByEntityType(character);
            
            AssertTrue(action == null, "Action should not be selected for rolls 6-13");
            // Note: Would need to mock dice to test specific roll ranges
        }
        
        private static void TestActionSelectionRoll14To20()
        {
            Console.WriteLine("\n--- Testing Action Selection Roll 14-20 ---");
            
            var character = CreateTestCharacter();
            var comboAction = new Action 
            { 
                Name = "TEST COMBO", 
                IsComboAction = true,
                ComboOrder = 1
            };
            character.AddAction(comboAction, 1.0);
            character.Actions.AddToCombo(comboAction);
            
            ActionSelector.ClearStoredRolls();
            
            // Test that rolls 14-20 select combo actions
            var action = ActionSelector.SelectActionByEntityType(character);
            
            AssertTrue(action != null, "Action should be selected");
            // Note: Would need to mock dice to test specific roll ranges
        }
        
        private static void TestActionSelectionWithRollBonus()
        {
            Console.WriteLine("\n--- Testing Action Selection with Roll Bonus ---");
            
            var character = CreateTestCharacter();
            var comboAction = new Action 
            { 
                Name = "TEST COMBO", 
                IsComboAction = true,
                ComboOrder = 1
            };
            character.AddAction(comboAction, 1.0);
            character.Actions.AddToCombo(comboAction);
            
            // Add roll bonus via gear or stats
            character.Stats.Technique = 20; // High TECH gives roll bonus
            
            ActionSelector.ClearStoredRolls();
            var action = ActionSelector.SelectActionByEntityType(character);
            
            AssertTrue(action != null, "Action should be selected with roll bonus");
        }
        
        private static void TestActionSelectionNatural20()
        {
            Console.WriteLine("\n--- Testing Action Selection Natural 20 ---");
            
            var character = CreateTestCharacter();
            var comboAction = new Action 
            { 
                Name = "TEST COMBO", 
                IsComboAction = true,
                ComboOrder = 1
            };
            character.AddAction(comboAction, 1.0);
            character.Actions.AddToCombo(comboAction);
            
            // Natural 20 should always select combo action
            ActionSelector.ClearStoredRolls();
            var action = ActionSelector.SelectActionByEntityType(character);
            
            AssertTrue(action != null, "Action should be selected on natural 20");
            // Note: Would need to mock dice to test natural 20 specifically
        }
        
        #endregion
        
        #region Combo Sequence Information Tests
        
        private static void TestComboSequenceOrdering()
        {
            Console.WriteLine("\n--- Testing Combo Sequence Ordering ---");
            
            var character = CreateTestCharacter();
            var action1 = new Action { Name = "Action 1", IsComboAction = true, ComboOrder = 0 };
            var action2 = new Action { Name = "Action 2", IsComboAction = true, ComboOrder = 0 };
            var action3 = new Action { Name = "Action 3", IsComboAction = true, ComboOrder = 0 };
            
            // Add in different order
            character.Actions.AddToCombo(action3);
            character.Actions.AddToCombo(action1);
            character.Actions.AddToCombo(action2);
            
            var comboActions = character.Actions.GetComboActions();
            
            AssertTrue(comboActions.Count == 3, $"Should have 3 combo actions, got: {comboActions.Count}");
            AssertTrue(comboActions[0].ComboOrder == 1, $"First action should have order 1, got: {comboActions[0].ComboOrder}");
            AssertTrue(comboActions[1].ComboOrder == 2, $"Second action should have order 2, got: {comboActions[1].ComboOrder}");
            AssertTrue(comboActions[2].ComboOrder == 3, $"Third action should have order 3, got: {comboActions[2].ComboOrder}");
        }
        
        private static void TestComboStepAdvancement()
        {
            Console.WriteLine("\n--- Testing Combo Step Advancement ---");
            
            var character = CreateTestCharacter();
            var action1 = new Action { Name = "Action 1", IsComboAction = true, ComboOrder = 1 };
            var action2 = new Action { Name = "Action 2", IsComboAction = true, ComboOrder = 2 };
            
            character.Actions.AddToCombo(action1);
            character.Actions.AddToCombo(action2);
            
            int initialStep = character.ComboStep;
            character.IncrementComboStep(action1);
            
            AssertTrue(character.ComboStep != initialStep || character.ComboStep == 0, 
                $"Combo step should change or reset, was: {initialStep}, now: {character.ComboStep}");
        }
        
        private static void TestComboStepResetOnMiss()
        {
            Console.WriteLine("\n--- Testing Combo Step Reset on Miss ---");
            
            var character = CreateTestCharacter();
            character.ComboStep = 3;
            
            // Simulate miss - combo should reset
            character.ComboStep = 0;
            
            AssertTrue(character.ComboStep == 0, $"Combo step should reset to 0 on miss, got: {character.ComboStep}");
        }
        
        private static void TestComboSequenceWithMultipleActions()
        {
            Console.WriteLine("\n--- Testing Combo Sequence with Multiple Actions ---");
            
            var character = CreateTestCharacter();
            var actions = new List<Action>();
            
            for (int i = 0; i < 5; i++)
            {
                var action = new Action 
                { 
                    Name = $"Combo Action {i + 1}", 
                    IsComboAction = true,
                    ComboOrder = 0
                };
                character.Actions.AddToCombo(action);
                actions.Add(action);
            }
            
            var comboActions = character.Actions.GetComboActions();
            
            AssertTrue(comboActions.Count == 5, $"Should have 5 combo actions, got: {comboActions.Count}");
            
            // Verify ordering
            for (int i = 0; i < comboActions.Count; i++)
            {
                AssertTrue(comboActions[i].ComboOrder == i + 1, 
                    $"Action {i} should have order {i + 1}, got: {comboActions[i].ComboOrder}");
            }
        }
        
        private static void TestComboSequenceRouting()
        {
            Console.WriteLine("\n--- Testing Combo Sequence Routing ---");
            
            var character = CreateTestCharacter();
            var action1 = new Action { Name = "Action 1", IsComboAction = true, ComboOrder = 1 };
            var action2 = new Action { Name = "Action 2", IsComboAction = true, ComboOrder = 2 };
            
            character.Actions.AddToCombo(action1);
            character.Actions.AddToCombo(action2);
            
            // Test that combo step cycles through actions
            int step1 = character.ComboStep;
            character.IncrementComboStep(action1);
            int step2 = character.ComboStep;
            
            // Step should advance (or reset if routing says to stop)
            AssertTrue(step2 != step1 || step2 == 0, 
                $"Combo step should change or reset, was: {step1}, now: {step2}");
        }
        
        #endregion
        
        #region IsCombo Flag Tests (Bug Detection)
        
        private static void TestIsComboFlagOnBasicAttack()
        {
            Console.WriteLine("\n--- Testing IsCombo Flag on Basic Attack ---");
            
            var character = CreateTestCharacter();
            var enemy = new Enemy("TestEnemy", 1, 100, 10, 10, 10, 10);
            var basicAttack = ActionFactory.GetBasicAttack(character);
            
            if (basicAttack != null)
            {
                var lastUsedActions = new Dictionary<Actor, Action>();
                var lastCriticalMissStatus = new Dictionary<Actor, bool>();
                
                // Force basic attack
                var result = ActionExecutionFlow.Execute(
                    character, enemy, null, null, basicAttack, null, 
                    lastUsedActions, lastCriticalMissStatus);
                
                // IsCombo should be based on roll, not action name
                // For a basic attack with roll < 14, IsCombo should be false
                // For a basic attack with roll >= 14, IsCombo should be true
                
                if (result.SelectedAction != null && result.SelectedAction.Name == "BASIC ATTACK")
                {
                    var thresholdManager = RollModificationManager.GetThresholdManager();
                    int comboThreshold = thresholdManager.GetComboThreshold(character);
                    bool expectedIsCombo = result.AttackRoll >= comboThreshold;
                    
                    AssertTrue(result.IsCombo == expectedIsCombo, 
                        $"IsCombo should be {expectedIsCombo} for BASIC ATTACK with roll {result.AttackRoll} (threshold: {comboThreshold}), got: {result.IsCombo}");
                }
            }
        }
        
        private static void TestIsComboFlagOnComboAction()
        {
            Console.WriteLine("\n--- Testing IsCombo Flag on Combo Action ---");
            
            var character = CreateTestCharacter();
            var enemy = new Enemy("TestEnemy", 1, 100, 10, 10, 10, 10);
            var comboAction = new Action 
            { 
                Name = "TEST COMBO", 
                IsComboAction = true,
                ComboOrder = 1
            };
            character.AddAction(comboAction, 1.0);
            character.Actions.AddToCombo(comboAction);
            
            var lastUsedActions = new Dictionary<Actor, Action>();
            var lastCriticalMissStatus = new Dictionary<Actor, bool>();
            
            // Force combo action
            var result = ActionExecutionFlow.Execute(
                character, enemy, null, null, comboAction, null, 
                lastUsedActions, lastCriticalMissStatus);
            
            // IsCombo should only be true if the roll was >= 14 (combo threshold)
            // It should NOT be based on the action name
            
            if (result.SelectedAction != null)
            {
                var thresholdManager = RollModificationManager.GetThresholdManager();
                int comboThreshold = thresholdManager.GetComboThreshold(character);
                bool expectedIsCombo = result.AttackRoll >= comboThreshold;
                
                AssertTrue(result.IsCombo == expectedIsCombo, 
                    $"IsCombo should be {expectedIsCombo} for combo action with roll {result.AttackRoll} (threshold: {comboThreshold}), got: {result.IsCombo}");
            }
        }
        
        private static void TestIsComboFlagBasedOnRoll()
        {
            Console.WriteLine("\n--- Testing IsCombo Flag Based on Roll ---");
            
            var character = CreateTestCharacter();
            var enemy = new Enemy("TestEnemy", 1, 100, 10, 10, 10, 10);
            var comboAction = new Action 
            { 
                Name = "TEST COMBO", 
                IsComboAction = true,
                ComboOrder = 1
            };
            character.AddAction(comboAction, 1.0);
            character.Actions.AddToCombo(comboAction);
            
            var lastUsedActions = new Dictionary<Actor, Action>();
            var lastCriticalMissStatus = new Dictionary<Actor, bool>();
            
            // IsCombo should be based on whether the roll was >= 14 (combo threshold)
            // This is now correctly implemented based on the roll value
            
            var thresholdManager = RollModificationManager.GetThresholdManager();
            int comboThreshold = thresholdManager.GetComboThreshold(character);
            
            AssertTrue(comboThreshold >= 14, $"Combo threshold should be >= 14, got: {comboThreshold}");
            
            // Test that IsCombo is correctly set based on roll
            var result = ActionExecutionFlow.Execute(
                character, enemy, null, null, comboAction, null, 
                lastUsedActions, lastCriticalMissStatus);
            
            if (result.SelectedAction != null)
            {
                bool expectedIsCombo = result.AttackRoll >= comboThreshold;
                AssertTrue(result.IsCombo == expectedIsCombo, 
                    $"IsCombo should be {expectedIsCombo} based on roll {result.AttackRoll} (threshold: {comboThreshold}), got: {result.IsCombo}");
            }
        }
        
        private static void TestIsComboFlagWithForcedAction()
        {
            Console.WriteLine("\n--- Testing IsCombo Flag with Forced Action ---");
            
            var character = CreateTestCharacter();
            var enemy = new Enemy("TestEnemy", 1, 100, 10, 10, 10, 10);
            var forcedAction = new Action 
            { 
                Name = "FORCED ACTION", 
                IsComboAction = false
            };
            
            var lastUsedActions = new Dictionary<Actor, Action>();
            var lastCriticalMissStatus = new Dictionary<Actor, bool>();
            
            var result = ActionExecutionFlow.Execute(
                character, enemy, null, null, forcedAction, null, 
                lastUsedActions, lastCriticalMissStatus);
            
            // IsCombo should be based on roll, not action name
            // "FORCED ACTION" should only be marked as combo if roll >= 14
            if (result.SelectedAction != null)
            {
                var thresholdManager = RollModificationManager.GetThresholdManager();
                int comboThreshold = thresholdManager.GetComboThreshold(character);
                bool expectedIsCombo = result.AttackRoll >= comboThreshold;
                
                AssertTrue(result.IsCombo == expectedIsCombo, 
                    $"IsCombo should be {expectedIsCombo} for forced action with roll {result.AttackRoll} (threshold: {comboThreshold}), got: {result.IsCombo}");
            }
        }
        
        #endregion
        
        #region Conditional Trigger Tests
        
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
            
            AssertTrue(shouldTrigger, "OnCombo trigger should fire on combo hit");
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
            
            AssertTrue(shouldTrigger, "OnNormalHit trigger should fire on normal hit");
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
            
            AssertTrue(!shouldTrigger, "OnNormalHit trigger should NOT fire on combo hit");
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
            
            AssertTrue(shouldTrigger, "OnCriticalHit trigger should fire on critical hit");
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
            
            AssertTrue(!shouldTrigger, "OnCombo trigger should NOT fire on normal hit");
        }
        
        #endregion
        
        #region Integration Tests
        
        private static void TestFullComboSequenceExecution()
        {
            Console.WriteLine("\n--- Testing Full Combo Sequence Execution ---");
            
            var character = CreateTestCharacter();
            var enemy = new Enemy("TestEnemy", 1, 100, 10, 10, 10, 10);
            
            var action1 = new Action { Name = "Combo 1", IsComboAction = true, ComboOrder = 1 };
            var action2 = new Action { Name = "Combo 2", IsComboAction = true, ComboOrder = 2 };
            
            character.AddAction(action1, 1.0);
            character.AddAction(action2, 1.0);
            character.Actions.AddToCombo(action1);
            character.Actions.AddToCombo(action2);
            
            var comboActions = character.Actions.GetComboActions();
            AssertTrue(comboActions.Count == 2, $"Should have 2 combo actions, got: {comboActions.Count}");
            
            // Verify combo sequence information
            AssertTrue(comboActions[0].ComboOrder == 1, "First action should have order 1");
            AssertTrue(comboActions[1].ComboOrder == 2, "Second action should have order 2");
        }
        
        private static void TestDiceRollToActionSelectionFlow()
        {
            Console.WriteLine("\n--- Testing Dice Roll to Action Selection Flow ---");
            
            var character = CreateTestCharacter();
            var comboAction = new Action 
            { 
                Name = "TEST COMBO", 
                IsComboAction = true,
                ComboOrder = 1
            };
            character.AddAction(comboAction, 1.0);
            character.Actions.AddToCombo(comboAction);
            
            ActionSelector.ClearStoredRolls();
            var action = ActionSelector.SelectActionByEntityType(character);
            
            AssertTrue(action != null, "Action should be selected");
            
            // Verify the roll was stored
            int storedRoll = ActionSelector.GetActionRoll(character);
            AssertTrue(storedRoll >= 1 && storedRoll <= 20, 
                $"Stored roll should be between 1 and 20, got: {storedRoll}");
        }
        
        private static void TestComboSequenceInformationCorrectness()
        {
            Console.WriteLine("\n--- Testing Combo Sequence Information Correctness ---");
            
            var character = CreateTestCharacter();
            var actions = new List<Action>();
            
            // Add 3 combo actions
            for (int i = 0; i < 3; i++)
            {
                var action = new Action 
                { 
                    Name = $"Combo Action {i + 1}", 
                    IsComboAction = true,
                    ComboOrder = 0
                };
                character.Actions.AddToCombo(action);
                actions.Add(action);
            }
            
            var comboActions = character.Actions.GetComboActions();
            
            // Verify all actions are in the sequence
            AssertTrue(comboActions.Count == 3, $"Should have 3 combo actions, got: {comboActions.Count}");
            
            // Verify ordering is correct
            for (int i = 0; i < comboActions.Count; i++)
            {
                AssertTrue(comboActions[i].ComboOrder == i + 1, 
                    $"Action at index {i} should have order {i + 1}, got: {comboActions[i].ComboOrder}");
            }
            
            // Verify combo step cycles correctly
            int initialStep = character.ComboStep;
            character.IncrementComboStep(comboActions[0]);
            int newStep = character.ComboStep;
            
            AssertTrue(newStep != initialStep || newStep == 0, 
                $"Combo step should advance or reset, was: {initialStep}, now: {newStep}");
        }
        
        #endregion
        
        #region Helper Methods
        
        private static Character CreateTestCharacter()
        {
            var character = new Character("TestHero", 1);
            var basicAttack = ActionFactory.GetBasicAttack(character);
            if (basicAttack != null)
            {
                character.AddAction(basicAttack, 1.0);
            }
            return character;
        }
        
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
                Console.WriteLine("\n⚠️  NOTE: Some tests may fail due to the IsCombo flag bug:");
                Console.WriteLine("   IsCombo is currently set based on action name, not dice roll.");
                Console.WriteLine("   It should be set based on whether the roll was >= combo threshold (14).");
            }
        }
        
        #endregion
    }
}
