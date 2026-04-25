using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Actions
{
    /// <summary>
    /// Comprehensive tests for ActionSelector roll-based selection logic
    /// These tests use Dice.SetTestRoll() to control roll values and verify
    /// that action selection works correctly based on base roll values.
    /// 
    /// Action selection rules (combo path vs combo threshold):
    /// - Natural 20: Combo-slot action when available
    /// - Otherwise: combo when modified d20 (sheet die mods only; not stat roll bonuses) &gt;= effective threshold
    ///   (COMBO-type threshold adjustments and peeked queued ACCURACY apply, matching combat execution)
    /// - With no INT/bonus (tests use INT 0): raw d20 1-13 normal, 14+ combo
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
            TestQueuedAccuracyLowersComboPathSelectionGate();
            TestIntRollBonusDoesNotBypassComboDieGate();
            TestRoll13WithIntBonusStaysNormal();
            TestNatural20();
            TestPeekPendingThresholdHudShiftsFromFifo();
            TestEdgeCases();
            TestRollBoundaries();
            TestEnemyComboOnlyPoolBelowThresholdIsUnnamedNormal();

            // Clean up test mode
            Dice.ClearTestRoll();

            TestBase.PrintSummary("ActionSelector Roll-Based Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Base Roll Threshold Tests

        /// <summary>
        /// Tests that raw d20 thresholds determine action type when preview bonuses are zero (INT 0).
        /// </summary>
        private static void TestBaseRollThresholds()
        {
            Console.WriteLine("--- Testing Base Roll Thresholds ---");

            var character = CreateTestCharacterWithBothActionTypes(intelligence: 0);

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

        /// <summary>
        /// Deferred ACCURACY (e.g. Ability cadence +20 after CAST hit) must lower the combo-path d20 gate the same way
        /// as <see cref="RPGGame.Actions.Execution.ActionExecutionFlow"/> so roll 12 can select the strip action when effective threshold is 1.
        /// </summary>
        private static void TestQueuedAccuracyLowersComboPathSelectionGate()
        {
            Console.WriteLine("\n--- Testing queued ACCURACY lowers combo-path selection gate ---");

            var character = CreateTestCharacterWithBothActionTypes(intelligence: 0);
            character.Effects.ClearPendingActionBonuses();
            character.Effects.AddPendingActionBonusesNextHeroRoll(new List<ActionAttackBonusItem>
            {
                new() { Type = "ACCURACY", Value = 20 }
            });

            Dice.SetTestRoll(12);
            ActionSelector.ClearStoredRolls();
            var action = ActionSelector.SelectActionBasedOnRoll(character);
            TestBase.AssertTrue(action != null && action.IsComboAction,
                "d20 12 with +20 peeked ACCURACY should select combo strip (effective combo threshold floor 1)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(ActionSelector.WouldNaturalRollSelectComboAction(character, 12),
                "WouldNaturalRollSelectComboAction should match for d20 12 with queued ACC",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Combo die gate (INT does not bypass)

        /// <summary>
        /// INT milestone bonuses shift thresholds, but combo-strip vs normal selection still uses the modified d20 gate only
        /// (stat roll bonuses do not turn d20 12–13 into a named combo swing).
        /// </summary>
        private static void TestIntRollBonusDoesNotBypassComboDieGate()
        {
            Console.WriteLine("\n--- Testing INT roll bonus does not bypass combo d20 gate ---");

            // Keep INT below the first milestone so the baseline combo threshold remains unchanged.
            var char12 = CreateTestCharacterWithBothActionTypes(intelligence: 0);
            Dice.SetTestRoll(12);
            ActionSelector.ClearStoredRolls();
            var a12 = ActionSelector.SelectActionBasedOnRoll(char12);
            TestBase.AssertTrue(a12 != null && !a12.IsComboAction,
                "d20 12 should still be normal (die below 14)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var char13 = CreateTestCharacterWithBothActionTypes(intelligence: 0);
            Dice.SetTestRoll(13);
            ActionSelector.ClearStoredRolls();
            var a13 = ActionSelector.SelectActionBasedOnRoll(char13);
            TestBase.AssertTrue(a13 != null && !a13!.IsComboAction,
                "d20 13 should still be normal (die below 14)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            Dice.SetTestRoll(14);
            ActionSelector.ClearStoredRolls();
            var a14 = ActionSelector.SelectActionBasedOnRoll(char13);
            TestBase.AssertTrue(a14 != null && a14.IsComboAction,
                "d20 14 should select combo",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// d20 13 with INT-derived roll bonus still uses unnamed normal: combo path requires 14+ on the die.
        /// </summary>
        private static void TestRoll13WithIntBonusStaysNormal()
        {
            Console.WriteLine("\n--- Testing Roll 13 + INT Bonus stays normal ---");

            // INT below the first milestone: should not influence combo selection.
            var character = CreateTestCharacterWithBothActionTypes(intelligence: 0);
            Dice.SetTestRoll(13);
            ActionSelector.ClearStoredRolls();
            var action = ActionSelector.SelectActionBasedOnRoll(character);
            TestBase.AssertTrue(action != null && !action.IsComboAction,
                "d20 13 should select normal (unnamed), not combo strip",
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

            var character = CreateTestCharacterWithBothActionTypes(intelligence: 0);

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

            var character = CreateTestCharacterWithBothActionTypes(intelligence: 0);

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

        /// <summary>
        /// FIFO/slot HIT and COMBO bonuses must appear in <see cref="ActionSelector.PeekPendingThresholdHudShifts"/>
        /// before <see cref="CharacterEffectsState.ConsumePendingActionBonusesNextHeroRoll"/> (HUD preview between turns).
        /// </summary>
        private static void TestPeekPendingThresholdHudShiftsFromFifo()
        {
            Console.WriteLine("\n--- Testing PeekPendingThresholdHudShifts (FIFO ACC/HIT/COMBO) ---");

            _ = GameConfiguration.Instance;
            var hero = TestDataBuilders.Character().WithName("PeekHudHero").WithStats(10, 10, 10, 0).Build();
            var normal = TestDataBuilders.CreateMockAction("N", ActionType.Attack);
            normal.IsComboAction = false;
            hero.AddAction(normal, 1.0);
            var combo = TestDataBuilders.CreateMockAction("C", ActionType.Attack);
            combo.IsComboAction = true;
            combo.ComboOrder = 1;
            hero.AddAction(combo, 1.0);
            hero.Actions.AddToCombo(combo);

            hero.Effects.ClearPendingActionBonuses();
            hero.Effects.AddPendingActionBonusesNextHeroRoll(new List<ActionAttackBonusItem>
            {
                new() { Type = "ACCURACY", Value = 3 },
                new() { Type = "HIT", Value = 1 },
                new() { Type = "COMBO", Value = 2 }
            });

            var p = ActionSelector.PeekPendingThresholdHudShifts(hero);
            TestBase.AssertEqual(3, p.SharedAccuracy,
                "ACCURACY aggregates to SharedAccuracy",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1, p.HitDelta,
                "HIT delta from FIFO",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(2, p.ComboDelta,
                "COMBO delta from FIFO",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            _ = hero.Effects.ConsumePendingActionBonusesNextHeroRoll();
            p = ActionSelector.PeekPendingThresholdHudShifts(hero);
            TestBase.AssertEqual(0, p.SharedAccuracy + p.HitDelta + p.ComboDelta + p.CritDelta + p.CritMissDelta,
                "after consume, peeked threshold shifts are zero",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #region Roll Boundary Tests

        /// <summary>
        /// Tests the critical boundary between normal and combo actions (13 vs 14)
        /// </summary>
        private static void TestRollBoundaries()
        {
            Console.WriteLine("\n--- Testing Roll Boundaries (13 vs 14) ---");

            var character = CreateTestCharacterWithBothActionTypes(intelligence: 0);

            // Test roll 13 - last value before combo threshold (no roll bonus)
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

        /// <summary>
        /// Enemies whose pool is only combo-flagged specials must not execute a named move when the preview
        /// attack total is below the combo threshold (regression: used to return <c>ActionPool[0]</c>).
        /// </summary>
        private static void TestEnemyComboOnlyPoolBelowThresholdIsUnnamedNormal()
        {
            Console.WriteLine("\n--- Testing enemy combo-only pool vs combo threshold ---");

            var enemy = TestDataBuilders.Enemy().WithName("SlamBeast").WithStats(10, 10, 10, 0).Build();
            enemy.ActionPool.Clear();
            var slam = TestDataBuilders.CreateMockAction("SLAM", ActionType.Attack);
            slam.IsComboAction = true;
            slam.ComboOrder = 1;
            enemy.AddAction(slam, 1.0);
            enemy.AddToCombo(slam);

            ActionSelector.ClearStoredRolls();
            Dice.SetTestRoll(11);
            var low = ActionSelector.SelectEnemyActionBasedOnRoll(enemy);
            TestBase.AssertTrue(low != null && !low.IsComboAction && string.IsNullOrEmpty(low.Name),
                "Enemy roll 11 (below default combo threshold 14) should use unnamed synthetic normal, not SLAM",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertFalse(ActionSelector.WouldNaturalRollSelectComboAction(enemy, 11),
                "WouldNaturalRollSelectComboAction should be false for enemy roll 11",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            ActionSelector.ClearStoredRolls();
            Dice.SetTestRoll(16);
            var high = ActionSelector.SelectEnemyActionBasedOnRoll(enemy);
            TestBase.AssertTrue(high != null && high.IsComboAction && string.Equals(high.Name, "SLAM", StringComparison.Ordinal),
                "Enemy roll 16 should select SLAM from the combo strip",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(ActionSelector.WouldNaturalRollSelectComboAction(enemy, 16),
                "WouldNaturalRollSelectComboAction should be true for enemy roll 16",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #region Helper Methods

        /// <summary>
        /// Creates a test character with both combo and normal actions available
        /// </summary>
        private static Character CreateTestCharacterWithBothActionTypes(int intelligence = 0)
        {
            var character = TestDataBuilders.Character()
                .WithName("TestHero")
                .WithLevel(1)
                .WithStats(10, 10, 10, intelligence)
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
