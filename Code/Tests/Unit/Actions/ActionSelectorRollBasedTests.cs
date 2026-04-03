using System;
using RPGGame;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Actions
{
    /// <summary>
    /// Comprehensive tests for ActionSelector roll-based selection logic
    /// These tests use Dice.SetTestRoll() to control roll values and verify
    /// that action selection works correctly based on base roll values.
    /// 
    /// Action selection rules:
    /// - Roll 1-13: Normal action (non-combo)
    /// - Roll 14-19: Combo action
    /// - Roll 20: Combo action (natural 20)
    /// </summary>
    public static class ActionSelectorRollBasedTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all ActionSelector roll-based tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionSelector Roll-Based Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            // Ensure test mode is cleared before starting
            Dice.ClearTestRoll();

            TestBaseRollThresholds();
            TestBaseRollVsTotalRoll();
            TestNatural20();
            TestEdgeCases();
            TestRollBoundaries();

            // Clean up test mode
            Dice.ClearTestRoll();

            TestBase.PrintSummary("ActionSelector Roll-Based Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Base Roll Threshold Tests

        /// <summary>
        /// Tests that base roll thresholds correctly determine action type
        /// </summary>
        private static void TestBaseRollThresholds()
        {
            Console.WriteLine("--- Testing Base Roll Thresholds ---");

            var character = CreateTestCharacterWithBothActionTypes();

            // Test roll 12 - should be normal attack (below combo threshold of 14)
            Dice.SetTestRoll(12);
            ActionSelector.ClearStoredRolls();
            var action12 = ActionSelector.SelectActionBasedOnRoll(character);
            TestBase.AssertTrue(action12 != null, 
                "Roll 12 should return an action", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!action12!.IsComboAction, 
                "Roll 12 should select normal action (not combo), got IsComboAction=" + action12.IsComboAction, 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test roll 13 - should be normal attack (below combo threshold of 14)
            Dice.SetTestRoll(13);
            ActionSelector.ClearStoredRolls();
            var action13 = ActionSelector.SelectActionBasedOnRoll(character);
            TestBase.AssertTrue(action13 != null, 
                "Roll 13 should return an action", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!action13!.IsComboAction, 
                "Roll 13 should select normal action (not combo), got IsComboAction=" + action13.IsComboAction, 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test roll 14 - should be combo action (at combo threshold)
            Dice.SetTestRoll(14);
            ActionSelector.ClearStoredRolls();
            var action14 = ActionSelector.SelectActionBasedOnRoll(character);
            TestBase.AssertTrue(action14 != null, 
                "Roll 14 should return an action", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(action14!.IsComboAction, 
                "Roll 14 should select combo action, got IsComboAction=" + action14.IsComboAction, 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test roll 15 - should be combo action
            Dice.SetTestRoll(15);
            ActionSelector.ClearStoredRolls();
            var action15 = ActionSelector.SelectActionBasedOnRoll(character);
            TestBase.AssertTrue(action15 != null, 
                "Roll 15 should return an action", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(action15!.IsComboAction, 
                "Roll 15 should select combo action, got IsComboAction=" + action15.IsComboAction, 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Base Roll vs Total Roll Tests

        /// <summary>
        /// Tests that action type is determined by BASE ROLL only, not total roll with bonuses.
        /// This is the critical test that would have caught the bug.
        /// </summary>
        private static void TestBaseRollVsTotalRoll()
        {
            Console.WriteLine("\n--- Testing Base Roll vs Total Roll (Critical Bug Test) ---");

            var character = CreateTestCharacterWithBothActionTypes();

            // CRITICAL TEST: Base roll 12 with bonuses should still be normal attack
            // This is the exact scenario that had the bug - bonuses shouldn't affect action type
            Dice.SetTestRoll(12);
            ActionSelector.ClearStoredRolls();
            
            // Add roll bonuses that would push total to 14+
            character.Stats.Intelligence = 20; // This adds roll bonus
            // Note: The exact bonus calculation depends on the system, but we're testing
            // that bonuses don't affect action type selection
            
            var action12WithBonus = ActionSelector.SelectActionBasedOnRoll(character);
            TestBase.AssertTrue(action12WithBonus != null, 
                "Roll 12 with bonuses should return an action", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!action12WithBonus!.IsComboAction, 
                "Base roll 12 with bonuses should STILL select normal action (base roll determines type, not total), got IsComboAction=" + action12WithBonus.IsComboAction, 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test roll 13 with bonus - should still be normal
            Dice.SetTestRoll(13);
            ActionSelector.ClearStoredRolls();
            var action13WithBonus = ActionSelector.SelectActionBasedOnRoll(character);
            TestBase.AssertTrue(action13WithBonus != null, 
                "Roll 13 with bonuses should return an action", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!action13WithBonus!.IsComboAction, 
                "Base roll 13 with bonuses should STILL select normal action, got IsComboAction=" + action13WithBonus.IsComboAction, 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test roll 14 with bonus - should be combo (base roll is already at threshold)
            Dice.SetTestRoll(14);
            ActionSelector.ClearStoredRolls();
            var action14WithBonus = ActionSelector.SelectActionBasedOnRoll(character);
            TestBase.AssertTrue(action14WithBonus != null, 
                "Roll 14 with bonuses should return an action", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(action14WithBonus!.IsComboAction, 
                "Base roll 14 with bonuses should select combo action, got IsComboAction=" + action14WithBonus.IsComboAction, 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Natural 20 Tests

        /// <summary>
        /// Tests that natural 20 always triggers combo action
        /// </summary>
        private static void TestNatural20()
        {
            Console.WriteLine("\n--- Testing Natural 20 ---");

            var character = CreateTestCharacterWithBothActionTypes();

            Dice.SetTestRoll(20);
            ActionSelector.ClearStoredRolls();
            var action20 = ActionSelector.SelectActionBasedOnRoll(character);
            
            TestBase.AssertTrue(action20 != null, 
                "Natural 20 should return an action", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(action20!.IsComboAction, 
                "Natural 20 should always select combo action, got IsComboAction=" + action20.IsComboAction, 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Edge Case Tests

        /// <summary>
        /// Tests edge cases and boundary conditions
        /// </summary>
        private static void TestEdgeCases()
        {
            Console.WriteLine("\n--- Testing Edge Cases ---");

            var character = CreateTestCharacterWithBothActionTypes();

            // Test roll 1 - should be normal (or null if it's a fail)
            Dice.SetTestRoll(1);
            ActionSelector.ClearStoredRolls();
            var action1 = ActionSelector.SelectActionBasedOnRoll(character);
            if (action1 != null)
            {
                TestBase.AssertTrue(!action1.IsComboAction, 
                    "Roll 1 should not select combo action if action is returned", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test roll 6 - should be normal (normal attack range)
            Dice.SetTestRoll(6);
            ActionSelector.ClearStoredRolls();
            var action6 = ActionSelector.SelectActionBasedOnRoll(character);
            TestBase.AssertTrue(action6 != null, 
                "Roll 6 should return an action", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!action6!.IsComboAction, 
                "Roll 6 should select normal action, got IsComboAction=" + action6.IsComboAction, 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test roll 19 - should be combo
            Dice.SetTestRoll(19);
            ActionSelector.ClearStoredRolls();
            var action19 = ActionSelector.SelectActionBasedOnRoll(character);
            TestBase.AssertTrue(action19 != null, 
                "Roll 19 should return an action", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(action19!.IsComboAction, 
                "Roll 19 should select combo action, got IsComboAction=" + action19.IsComboAction, 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Roll Boundary Tests

        /// <summary>
        /// Tests the critical boundary between normal and combo actions (13 vs 14)
        /// </summary>
        private static void TestRollBoundaries()
        {
            Console.WriteLine("\n--- Testing Roll Boundaries (13 vs 14) ---");

            var character = CreateTestCharacterWithBothActionTypes();

            // Test roll 13 - last value before combo threshold
            Dice.SetTestRoll(13);
            ActionSelector.ClearStoredRolls();
            var action13 = ActionSelector.SelectActionBasedOnRoll(character);
            TestBase.AssertTrue(action13 != null, 
                "Roll 13 should return an action", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!action13!.IsComboAction, 
                "Roll 13 (boundary) should select normal action, got IsComboAction=" + action13.IsComboAction, 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test roll 14 - first value at combo threshold
            Dice.SetTestRoll(14);
            ActionSelector.ClearStoredRolls();
            var action14 = ActionSelector.SelectActionBasedOnRoll(character);
            TestBase.AssertTrue(action14 != null, 
                "Roll 14 should return an action", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(action14!.IsComboAction, 
                "Roll 14 (boundary) should select combo action, got IsComboAction=" + action14.IsComboAction, 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a test character with both combo and normal actions available
        /// </summary>
        private static Character CreateTestCharacterWithBothActionTypes()
        {
            var character = TestDataBuilders.Character()
                .WithName("TestHero")
                .WithLevel(1)
                .Build();

            // Add a normal (non-combo) action
            var normalAction = TestDataBuilders.CreateMockAction("Normal Attack", ActionType.Attack);
            normalAction.IsComboAction = false;
            character.AddAction(normalAction, 1.0);

            // Add a combo action
            var comboAction = TestDataBuilders.CreateMockAction("Combo Attack", ActionType.Attack);
            comboAction.IsComboAction = true;
            comboAction.ComboOrder = 1;
            character.AddAction(comboAction, 1.0);
            character.Actions.AddToCombo(comboAction);

            return character;
        }

        #endregion
    }
}
