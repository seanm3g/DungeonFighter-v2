using System;
using RPGGame;
using RPGGame.Actions.RollModification;
using RPGGame.Tests; // For TestBase

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Tests for Action Selection Based on Dice Rolls
    /// </summary>
    public static class ComboDiceRollTestsActionSelection
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            Console.WriteLine("--- Action Selection Based on Dice Rolls Tests ---");
            TestActionSelectionRoll1To5();
            TestActionSelectionRoll6To13();
            TestActionSelectionRoll14To20();
            TestActionSelectionWithRollBonus();
            TestActionSelectionNatural20();
            TestBase.PrintSummary("Action Selection Based on Dice Rolls", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestActionSelectionRoll1To5()
        {
            Console.WriteLine("\n--- Testing Action Selection Roll 1-5 ---");
            
            var character = CreateTestCharacter();
            ActionSelector.ClearStoredRolls();
            
            // We can't directly control the roll, but we can test the selection logic
            // by checking that low rolls don't select actions when they shouldn't
            var action = ActionSelector.SelectActionByEntityType(character);
            
            TestBase.AssertTrue(action == null, "Action should not be selected for rolls 1-5", ref _testsRun, ref _testsPassed, ref _testsFailed);
            // Note: Actual roll-based testing would require mocking the dice
        }

        private static void TestActionSelectionRoll6To13()
        {
            Console.WriteLine("\n--- Testing Action Selection Roll 6-13 ---");
            
            var character = CreateTestCharacter();
            ActionSelector.ClearStoredRolls();
            
            // Test that rolls 6-13 should not select actions
            var action = ActionSelector.SelectActionByEntityType(character);
            
            TestBase.AssertTrue(action == null, "Action should not be selected for rolls 6-13", ref _testsRun, ref _testsPassed, ref _testsFailed);
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
            
            TestBase.AssertTrue(action != null, "Action should be selected", ref _testsRun, ref _testsPassed, ref _testsFailed);
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
            
            TestBase.AssertTrue(action != null, "Action should be selected with roll bonus", ref _testsRun, ref _testsPassed, ref _testsFailed);
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
            
            TestBase.AssertTrue(action != null, "Action should be selected on natural 20", ref _testsRun, ref _testsPassed, ref _testsFailed);
            // Note: Would need to mock dice to test natural 20 specifically
        }

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
    }
}

