using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Actions
{
    /// <summary>
    /// Comprehensive tests for ActionUtilities
    /// Tests action utilities, roll bonus calculation, and damage multiplier calculation
    /// </summary>
    public static class ActionUtilitiesTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all ActionUtilities tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionUtilities Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestGetComboActions();
            TestGetComboStep();
            TestGetNextComboSlotForPendingBonuses();
            TestGetComboAmplificationExponentUsesComboListOrder();
            TestCalculateRollBonus();
            TestCalculateDamageMultiplier();

            TestBase.PrintSummary("ActionUtilities Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Combo Tests

        private static void TestGetComboActions()
        {
            Console.WriteLine("--- Testing GetComboActions ---");

            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            var comboActions = ActionUtilities.GetComboActions(character);
            TestBase.AssertNotNull(comboActions,
                "GetComboActions should return a list",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (comboActions != null)
            {
                TestBase.AssertTrue(comboActions.Count >= 0,
                    $"Combo actions count should be >= 0, got {comboActions.Count}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test with enemy
            var enemy = TestDataBuilders.Enemy()
                .WithName("TestEnemy")
                .WithLevel(1)
                .Build();

            var enemyComboActions = ActionUtilities.GetComboActions(enemy);
            TestBase.AssertNotNull(enemyComboActions,
                "GetComboActions should work for enemies",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGetComboStep()
        {
            Console.WriteLine("\n--- Testing GetComboStep ---");

            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            var step = ActionUtilities.GetComboStep(character);
            TestBase.AssertTrue(step >= 0,
                $"Combo step should be >= 0, got {step}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with enemy (should return 0)
            var enemy = TestDataBuilders.Enemy()
                .WithName("TestEnemy")
                .WithLevel(1)
                .Build();

            var enemyStep = ActionUtilities.GetComboStep(enemy);
            TestBase.AssertEqual(0, enemyStep,
                "Enemy combo step should be 0",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Finisher (last slot): pending ACTION bonuses must target slot 0 even if ComboStep is out of sync with the executed action.
        /// </summary>
        private static void TestGetNextComboSlotForPendingBonuses()
        {
            Console.WriteLine("\n--- Testing GetNextComboSlotForPendingBonuses ---");

            var character = new Character("SlotWrapTest", 1);
            var opener = TestDataBuilders.CreateMockAction("Opener", ActionType.Attack);
            opener.IsComboAction = true;
            opener.ComboOrder = 1;
            opener.ComboRouting.IsOpener = true;
            var mid = TestDataBuilders.CreateMockAction("Mid", ActionType.Attack);
            mid.IsComboAction = true;
            mid.ComboOrder = 2;
            var finisher = TestDataBuilders.CreateMockAction("FinisherSlot", ActionType.Attack);
            finisher.IsComboAction = true;
            finisher.ComboOrder = 3;
            finisher.ComboRouting.IsFinisher = true;

            character.AddAction(opener, 1.0);
            character.AddAction(mid, 1.0);
            character.AddAction(finisher, 1.0);
            character.Actions.AddToCombo(opener);
            character.Actions.AddToCombo(mid);
            character.Actions.AddToCombo(finisher);

            var combo = ActionUtilities.GetComboActions(character);
            TestBase.AssertTrue(combo.Count >= 3, "Test combo should have 3 actions", ref _testsRun, ref _testsPassed, ref _testsFailed);

            character.ComboStep = 0;
            int nextAfterFinisher = ActionUtilities.GetNextComboSlotForPendingBonuses(character, finisher, combo);
            TestBase.AssertEqual(0, nextAfterFinisher,
                "Finisher wraps for-next-ACTION bonuses to the beginning of the sequence (slot 0)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            int nextAfterOpener = ActionUtilities.GetNextComboSlotForPendingBonuses(character, opener, combo);
            TestBase.AssertEqual(1, nextAfterOpener,
                "After opener, next pending slot is 1",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// AMP tier follows the ordered combo list passed into damage math (strip slot 0, 1, …), not opener/finisher tags.
        /// </summary>
        private static void TestGetComboAmplificationExponentUsesComboListOrder()
        {
            Console.WriteLine("\n--- Testing GetComboAmplificationExponent uses combo list slot index ---");

            var hero = TestDataBuilders.Character().WithName("AmpOrder").Build();
            var slot0 = new Action { Name = "CHANNEL", IsComboAction = true };
            var slot1 = new Action { Name = "STAB", IsComboAction = true };
            slot1.ComboRouting.IsOpener = true;
            var combo = new List<Action> { slot0, slot1 };

            int e0 = ActionUtilities.GetComboAmplificationExponent(hero, slot0, combo);
            int e1 = ActionUtilities.GetComboAmplificationExponent(hero, slot1, combo);
            TestBase.AssertEqual(0, e0,
                $"First list entry should be amp exponent 0 (got {e0})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1, e1,
                $"Second list entry should be amp exponent 1 even when tagged opener (got {e1})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Calculation Tests

        private static void TestCalculateRollBonus()
        {
            Console.WriteLine("\n--- Testing CalculateRollBonus ---");

            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            var bonus = ActionUtilities.CalculateRollBonus(character, null, consumeTempBonus: false);
            TestBase.AssertTrue(bonus >= 0,
                $"Roll bonus should be >= 0, got {bonus}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with action
            var action = TestDataBuilders.CreateMockAction("TestAction", ActionType.Attack);
            var bonusWithAction = ActionUtilities.CalculateRollBonus(character, action, consumeTempBonus: false);
            TestBase.AssertTrue(bonusWithAction >= 0,
                $"Roll bonus with action should be >= 0, got {bonusWithAction}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCalculateDamageMultiplier()
        {
            Console.WriteLine("\n--- Testing CalculateDamageMultiplier ---");

            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            var action = TestDataBuilders.CreateMockAction("TestAction", ActionType.Attack);
            var multiplier = ActionUtilities.CalculateDamageMultiplier(character, action);

            TestBase.AssertTrue(multiplier > 0,
                $"Damage multiplier should be > 0, got {multiplier}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with combo action
            action.IsComboAction = true;
            var comboMultiplier = ActionUtilities.CalculateDamageMultiplier(character, action);
            TestBase.AssertTrue(comboMultiplier > 0,
                $"Combo damage multiplier should be > 0, got {comboMultiplier}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
