using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Actions.Execution;
using RPGGame.Actions.RollModification;
using RPGGame.Tests; // For TestBase

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Tests for IsCombo Flag (Bug Detection)
    /// </summary>
    public static class ComboDiceRollTestsIsComboFlag
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            Console.WriteLine("--- IsCombo Flag Tests (Bug Detection) ---");
            // TestIsComboFlagOnBasicAttack() - Removed: BASIC ATTACK has been removed from the game
            TestIsComboFlagOnComboAction();
            TestIsComboFlagBasedOnRoll();
            TestIsComboFlagWithForcedAction();
            TestBase.PrintSummary("IsCombo Flag Tests", _testsRun, _testsPassed, _testsFailed);
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
                
                    TestBase.AssertTrue(result.IsCombo == expectedIsCombo, 
                        $"IsCombo should be {expectedIsCombo} for combo action with roll {result.AttackRoll} (threshold: {comboThreshold}), got: {result.IsCombo}", ref _testsRun, ref _testsPassed, ref _testsFailed);
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
            
            TestBase.AssertTrue(comboThreshold >= 14, $"Combo threshold should be >= 14, got: {comboThreshold}", ref _testsRun, ref _testsPassed, ref _testsFailed);
            
            // Test that IsCombo is correctly set based on roll
            var result = ActionExecutionFlow.Execute(
                character, enemy, null, null, comboAction, null, 
                lastUsedActions, lastCriticalMissStatus);
            
            if (result.SelectedAction != null)
            {
                bool expectedIsCombo = result.AttackRoll >= comboThreshold;
                TestBase.AssertTrue(result.IsCombo == expectedIsCombo, 
                    $"IsCombo should be {expectedIsCombo} based on roll {result.AttackRoll} (threshold: {comboThreshold}), got: {result.IsCombo}", ref _testsRun, ref _testsPassed, ref _testsFailed);
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
                
                TestBase.AssertTrue(result.IsCombo == expectedIsCombo, 
                    $"IsCombo should be {expectedIsCombo} for forced action with roll {result.AttackRoll} (threshold: {comboThreshold}), got: {result.IsCombo}", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static Character CreateTestCharacter()
        {
            var character = new Character("TestHero", 1);
            // BASIC ATTACK has been removed from the game
            return character;
        }
    }
}

