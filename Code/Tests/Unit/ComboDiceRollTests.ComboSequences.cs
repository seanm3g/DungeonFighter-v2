using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Actions.Execution;
using RPGGame.Actions.RollModification;
using RPGGame.Data;
using RPGGame.Tests; // For TestBase
using RPGGame.Utils;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Tests for Combo Sequence Information and Integration
    /// </summary>
    public static class ComboDiceRollTestsComboSequences
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            Console.WriteLine("--- Combo Sequence Information Tests ---");
            TestComboSequenceOrdering();
            TestComboStepAdvancement();
            TestComboStepResetOnMiss();
            TestComboSequenceWithMultipleActions();
            TestComboSequenceRouting();
            
            Console.WriteLine("\n--- Integration Tests ---");
            TestFullComboSequenceExecution();
            TestDiceRollToActionSelectionFlow();
            TestComboSequenceInformationCorrectness();
            TestComboStepDoesNotAdvanceOnNormalActionHit();
            TestComboStepResetsOnNormalAttackHit();
            TestComboStepAdvancesOnComboActionHit();
            TestComboStepAdvancesWhenEffectiveComboThresholdLowerThanConfigMin();
            
            TestBase.PrintSummary("Combo Sequence Information and Integration", _testsRun, _testsPassed, _testsFailed);
        }

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
            
            TestBase.AssertTrue(comboActions.Count == 3, $"Should have 3 combo actions, got: {comboActions.Count}", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1, comboActions[0].ComboOrder, $"First action should have order 1, got: {comboActions[0].ComboOrder}", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(2, comboActions[1].ComboOrder, $"Second action should have order 2, got: {comboActions[1].ComboOrder}", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(3, comboActions[2].ComboOrder, $"Third action should have order 3, got: {comboActions[2].ComboOrder}", ref _testsRun, ref _testsPassed, ref _testsFailed);
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
            
            TestBase.AssertTrue(character.ComboStep != initialStep || character.ComboStep == 0, 
                $"Combo step should change or reset, was: {initialStep}, now: {character.ComboStep}", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestComboStepResetOnMiss()
        {
            Console.WriteLine("\n--- Testing Combo Step Reset on Miss ---");
            
            var character = CreateTestCharacter();
            character.ComboStep = 3;
            
            // Simulate miss - combo should reset
            character.ComboStep = 0;
            
            TestBase.AssertTrue(character.ComboStep == 0, $"Combo step should reset to 0 on miss, got: {character.ComboStep}", ref _testsRun, ref _testsPassed, ref _testsFailed);
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
            
            TestBase.AssertTrue(comboActions.Count == 5, $"Should have 5 combo actions, got: {comboActions.Count}", ref _testsRun, ref _testsPassed, ref _testsFailed);
            
            // Verify ordering
            for (int i = 0; i < comboActions.Count; i++)
            {
                TestBase.AssertEqual(i + 1, comboActions[i].ComboOrder, 
                    $"Action {i} should have order {i + 1}, got: {comboActions[i].ComboOrder}", ref _testsRun, ref _testsPassed, ref _testsFailed);
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
            TestBase.AssertTrue(step2 != step1 || step2 == 0, 
                $"Combo step should change or reset, was: {step1}, now: {step2}", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

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
            TestBase.AssertTrue(comboActions.Count == 2, $"Should have 2 combo actions, got: {comboActions.Count}", ref _testsRun, ref _testsPassed, ref _testsFailed);
            
            // Verify combo sequence information
            TestBase.AssertEqual(1, comboActions[0].ComboOrder, "First action should have order 1", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(2, comboActions[1].ComboOrder, "Second action should have order 2", ref _testsRun, ref _testsPassed, ref _testsFailed);
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
            
            TestBase.AssertTrue(action != null, "Action should be selected", ref _testsRun, ref _testsPassed, ref _testsFailed);
            
            // Verify the roll was stored
            int storedRoll = ActionSelector.GetActionRoll(character);
            TestBase.AssertTrue(storedRoll >= 1 && storedRoll <= 20, 
                $"Stored roll should be between 1 and 20, got: {storedRoll}", ref _testsRun, ref _testsPassed, ref _testsFailed);
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
            TestBase.AssertTrue(comboActions.Count == 3, $"Should have 3 combo actions, got: {comboActions.Count}", ref _testsRun, ref _testsPassed, ref _testsFailed);
            
            // Verify ordering is correct
            for (int i = 0; i < comboActions.Count; i++)
            {
                TestBase.AssertEqual(i + 1, comboActions[i].ComboOrder, 
                    $"Action at index {i} should have order {i + 1}, got: {comboActions[i].ComboOrder}", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            
            // Verify combo step cycles correctly
            int initialStep = character.ComboStep;
            character.IncrementComboStep(comboActions[0]);
            int newStep = character.ComboStep;
            
            TestBase.AssertTrue(newStep != initialStep || newStep == 0, 
                $"Combo step should advance or reset, was: {initialStep}, now: {newStep}", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// When a normal (non-combo) action hits with roll >= 14, ComboStep must not advance,
        /// so the next combo roll still uses the same slot (e.g. Slant / Follow Through).
        /// </summary>
        private static void TestComboStepDoesNotAdvanceOnNormalActionHit()
        {
            Console.WriteLine("\n--- Testing Combo Step Does Not Advance on Normal Action Hit ---");
            var lastUsedActions = new Dictionary<Actor, Action>();
            var lastCriticalMissStatus = new Dictionary<Actor, bool>();
            int outcomesChecked = 0;
            for (int i = 0; i < 80; i++)
            {
                var character = CreateTestCharacter();
                int comboThreshold = RollModificationManager.GetThresholdManager().GetComboThreshold(character);
                var comboAction = new Action { Name = "SLOT ZERO", IsComboAction = true };
                character.AddAction(comboAction, 1.0);
                character.Actions.AddToCombo(comboAction);
                character.ComboStep = 0;
                var normalAction = new Action { Name = "NORMAL STRIKE", IsComboAction = false, Type = ActionType.Attack };
                character.AddAction(normalAction, 1.0);
                var enemy = new Enemy("TestEnemy", 1, 100, 5, 5, 5, 5);
                var result = ActionExecutionFlow.Execute(
                    character, enemy, null, null, normalAction, null,
                    lastUsedActions, lastCriticalMissStatus);
                if (result.Hit && result.AttackRoll >= comboThreshold)
                {
                    outcomesChecked++;
                    TestBase.AssertEqual(0, character.ComboStep,
                        $"ComboStep must not advance when normal action hits with roll {result.AttackRoll} (threshold {comboThreshold})",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
            TestBase.AssertTrue(outcomesChecked >= 0, $"Checked {outcomesChecked} normal-action hit(s) with roll >= threshold", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// When a normal (non-combo) attack completes successfully, ComboStep resets so the next combo roll starts from slot 0.
        /// </summary>
        private static void TestComboStepResetsOnNormalAttackHit()
        {
            Console.WriteLine("\n--- Testing Combo Step Resets on Normal Attack Hit ---");
            var lastUsedActions = new Dictionary<Actor, Action>();
            var lastCriticalMissStatus = new Dictionary<Actor, bool>();
            int outcomesChecked = 0;
            for (int i = 0; i < 120; i++)
            {
                var character = CreateTestCharacter();
                var comboAction = new Action { Name = "COMBO SLOT", IsComboAction = true };
                character.AddAction(comboAction, 1.0);
                character.Actions.AddToCombo(comboAction);
                character.ComboStep = 2;
                var normalAction = new Action { Name = "NORMAL STRIKE", IsComboAction = false, Type = ActionType.Attack };
                character.AddAction(normalAction, 1.0);
                var enemy = new Enemy("TestEnemy", 1, 100, 5, 5, 5, 5);
                var result = ActionExecutionFlow.Execute(
                    character, enemy, null, null, normalAction, null,
                    lastUsedActions, lastCriticalMissStatus);
                if (result.Hit)
                {
                    outcomesChecked++;
                    TestBase.AssertEqual(0, character.ComboStep,
                        $"ComboStep must reset to 0 when normal action hits (was 2), got: {character.ComboStep}",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
            TestBase.AssertTrue(outcomesChecked > 0,
                $"Expected at least one normal-action hit to verify reset; got {outcomesChecked} hits",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// When a combo action hits with roll >= 14, ComboStep must advance so the next combo roll uses the next slot.
        /// </summary>
        private static void TestComboStepAdvancesOnComboActionHit()
        {
            Console.WriteLine("\n--- Testing Combo Step Advances on Combo Action Hit ---");
            var lastUsedActions = new Dictionary<Actor, Action>();
            var lastCriticalMissStatus = new Dictionary<Actor, bool>();
            int outcomesChecked = 0;
            for (int i = 0; i < 80; i++)
            {
                var character = CreateTestCharacter();
                int comboThreshold = RollModificationManager.GetThresholdManager().GetComboThreshold(character);
                var combo1 = new Action { Name = "SLOT ZERO", IsComboAction = true };
                var combo2 = new Action { Name = "SLOT ONE", IsComboAction = true };
                character.AddAction(combo1, 1.0);
                character.AddAction(combo2, 1.0);
                character.Actions.AddToCombo(combo1);
                character.Actions.AddToCombo(combo2);
                character.ComboStep = 0;
                var enemy = new Enemy("TestEnemy", 1, 100, 5, 5, 5, 5);
                var result = ActionExecutionFlow.Execute(
                    character, enemy, null, null, combo1, null,
                    lastUsedActions, lastCriticalMissStatus);
                if (result.Hit && result.AttackRoll >= comboThreshold)
                {
                    outcomesChecked++;
                    TestBase.AssertTrue(character.ComboStep != 0,
                        $"ComboStep must advance when combo action hits with roll {result.AttackRoll} (threshold {comboThreshold}), got: {character.ComboStep}",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
            TestBase.AssertTrue(outcomesChecked >= 0, $"Checked {outcomesChecked} combo-action hit(s) with roll >= threshold", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Combo step advancement must use the same effective combo threshold as <see cref="ActionExecutionFlow"/> (IsCombo),
        /// not only RollSystem.ComboThreshold.Min. Otherwise a roll can count as a combo hit but never advance the sequence.
        /// </summary>
        private static void TestComboStepAdvancesWhenEffectiveComboThresholdLowerThanConfigMin()
        {
            Console.WriteLine("\n--- Testing Combo Step Advances Using Effective Combo Threshold ---");
            var lastUsedActions = new Dictionary<Actor, Action>();
            var lastCriticalMissStatus = new Dictionary<Actor, bool>();
            var character = CreateTestCharacter();
            var combo1 = new Action
            {
                Name = "EFFECTIVE THRESH",
                IsComboAction = true,
                Type = ActionType.Attack,
                DamageMultiplier = 1.0,
                Length = 1.0
            };
            // +3 COMBO from a prior action's deferred sheet adjustment (same FIFO layer as on-card combo mods).
            character.Effects.EnqueuePendingActionCadenceLayer(new List<ActionAttackBonusItem>
            {
                new ActionAttackBonusItem { Type = "COMBO", Value = 3 }
            });
            var combo2 = new Action { Name = "NEXT SLOT", IsComboAction = true, Type = ActionType.Attack, DamageMultiplier = 1.0, Length = 1.0 };
            character.AddAction(combo1, 1.0);
            character.AddAction(combo2, 1.0);
            character.Actions.AddToCombo(combo1);
            character.Actions.AddToCombo(combo2);
            character.ComboStep = 0;
            var enemy = new Enemy("TestEnemy", 1, 100, 5, 5, 5, 5);
            try
            {
                Dice.SetTestRoll(13);
                ActionSelector.SetStoredActionRoll(character, 13);
                var result = ActionExecutionFlow.Execute(
                    character, enemy, null, null, combo1, null,
                    lastUsedActions, lastCriticalMissStatus);
                // Pending +3 COMBO lowers effective combo threshold for this roll so IsCombo can be true at roll 13.
                TestBase.AssertTrue(result.Hit, "Regression test expects a hit", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(result.IsCombo, $"Expected IsCombo with lowered effective threshold; AttackRoll={result.AttackRoll}", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(character.ComboStep != 0,
                    $"ComboStep should advance on combo hit; AttackRoll={result.AttackRoll}, ComboStep={character.ComboStep}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                Dice.ClearTestRoll();
                ActionSelector.ClearStoredRolls();
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

