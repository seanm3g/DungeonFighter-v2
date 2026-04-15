using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Tests;
using RPGGame.Actions.RollModification;
using RPGGame.Data;

namespace RPGGame.Tests.Unit.Actions
{
    /// <summary>
    /// Comprehensive tests for RollModificationManager
    /// Tests roll modifications, threshold management, and modifier application
    /// </summary>
    public static class RollModificationManagerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all RollModificationManager tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== RollModificationManager Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestApplyActionRollModifications();
            TestGetThresholdManager();
            TestApplyThresholdOverrides();
            TestDeferredRollModAdjustmentsNotAppliedImmediately();
            TestDeferredRollModAdjustmentsScaleWithMultihit();
            TestDeferredRollModOverridesWhenCadenceSet();
            TestDeferredRollModOverridesWhenCadenceBlank();
            TestImmediateRollModOverridesWhenCadenceAttack();
            TestDeferredSheetAccuracyQueuedWithBlankCadence();

            TestBase.PrintSummary("RollModificationManager Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Roll Modification Tests

        private static void TestApplyActionRollModifications()
        {
            Console.WriteLine("--- Testing ApplyActionRollModifications ---");

            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            var enemy = TestDataBuilders.Enemy()
                .WithName("TestEnemy")
                .WithLevel(1)
                .Build();

            var action = TestDataBuilders.CreateMockAction("TestAction", ActionType.Attack);
            var baseRoll = 10;

            var modifiedRoll = RollModificationManager.ApplyActionRollModifications(
                baseRoll, action, character, enemy);

            TestBase.AssertTrue(modifiedRoll >= 1 && modifiedRoll <= 20,
                $"Modified roll should be in valid range (1-20), got {modifiedRoll}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Threshold Management Tests

        private static void TestGetThresholdManager()
        {
            Console.WriteLine("\n--- Testing GetThresholdManager ---");

            var manager = RollModificationManager.GetThresholdManager();
            TestBase.AssertNotNull(manager,
                "GetThresholdManager should return a threshold manager",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestApplyThresholdOverrides()
        {
            Console.WriteLine("\n--- Testing ApplyThresholdOverrides ---");

            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            var enemy = TestDataBuilders.Enemy()
                .WithName("TestEnemy")
                .WithLevel(1)
                .Build();

            var action = TestDataBuilders.CreateMockAction("TestAction", ActionType.Attack);
            
            // Test applying threshold overrides (should not crash)
            RollModificationManager.ApplyThresholdOverrides(action, character, enemy);
            TestBase.AssertTrue(true,
                "ApplyThresholdOverrides should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with null target
            RollModificationManager.ApplyThresholdOverrides(action, character, null);
            TestBase.AssertTrue(true,
                "ApplyThresholdOverrides should work with null target",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Sheet HIT/COMBO/CRIT adjustments must not change thresholds on the same roll; they enqueue for the next attack.
        /// </summary>
        private static void TestDeferredRollModAdjustmentsNotAppliedImmediately()
        {
            Console.WriteLine("\n--- Testing deferred roll-mod threshold adjustments ---");

            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            var tm = RollModificationManager.GetThresholdManager();
            tm.Clear();
            int hitBefore = tm.GetHitThreshold(character);

            var action = TestDataBuilders.CreateMockAction("SoftMiss", ActionType.Attack);
            action.RollMods.HitThresholdAdjustment = 5;
            action.RollMods.ComboThresholdAdjustment = -5;

            RollModificationManager.ApplyThresholdOverrides(action, character, null);
            TestBase.AssertEqual(hitBefore, tm.GetHitThreshold(character),
                "Hit threshold should be unchanged after ApplyThresholdOverrides when only adjustments are set",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            RollModificationManager.EnqueueDeferredRollModThresholdAdjustmentsForNextRoll(action, character, null);
            var layer = character.Effects.ConsumePendingActionBonusesNextHeroRoll();
            TestBase.AssertTrue(layer.Any(b => b.Type == "HIT" && (int)b.Value == 5),
                "Deferred layer should contain HIT +5",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(layer.Any(b => b.Type == "COMBO" && (int)b.Value == -5),
                "Deferred layer should contain COMBO -5",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Sheet CRIT (etc.) deferred to the next roll scales by this action's multihit count (e.g. -2 × 2 hits = -4).
        /// </summary>
        private static void TestDeferredRollModAdjustmentsScaleWithMultihit()
        {
            Console.WriteLine("\n--- Testing deferred roll-mod adjustments scale with multihit ---");

            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            var action = TestDataBuilders.CreateMockAction("TwinStrike", ActionType.Attack);
            action.Advanced.MultiHitCount = 2;
            action.RollMods.CriticalHitThresholdAdjustment = -2;

            RollModificationManager.EnqueueDeferredRollModThresholdAdjustmentsForNextRoll(action, character, null);
            var layer = character.Effects.ConsumePendingActionBonusesNextHeroRoll();
            TestBase.AssertTrue(layer.Any(b => b.Type == "CRIT" && Math.Abs(b.Value + 4) < 0.001),
                "2-hit action with CRIT -2 should enqueue CRIT -4 for the next roll",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var actionOneHit = TestDataBuilders.CreateMockAction("Jab", ActionType.Attack);
            actionOneHit.Advanced.MultiHitCount = 1;
            actionOneHit.RollMods.HitThresholdAdjustment = 3;
            RollModificationManager.EnqueueDeferredRollModThresholdAdjustmentsForNextRoll(actionOneHit, character, null);
            var layer2 = character.Effects.ConsumePendingActionBonusesNextHeroRoll();
            TestBase.AssertTrue(layer2.Any(b => b.Type == "HIT" && Math.Abs(b.Value - 3) < 0.001),
                "Single-hit action should not change deferred bonus magnitude",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Non-empty cadence: absolute threshold overrides must not apply on the current roll; they enqueue as SET_* on the next package.
        /// </summary>
        private static void TestDeferredRollModOverridesWhenCadenceSet()
        {
            Console.WriteLine("\n--- Testing deferred roll-mod overrides when cadence is set ---");

            var character = TestDataBuilders.Character().WithName("CadenceDefer").Build();
            var tm = RollModificationManager.GetThresholdManager();
            tm.Clear();
            int comboBefore = tm.GetComboThreshold(character);

            var action = TestDataBuilders.CreateMockAction("ComboSetter", ActionType.Attack);
            action.Cadence = "Action";
            action.RollMods.ComboThresholdOverride = 10;

            RollModificationManager.ApplyThresholdOverrides(action, character, null);
            TestBase.AssertEqual(comboBefore, tm.GetComboThreshold(character),
                "Combo threshold unchanged on current roll when cadence defers overrides",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            RollModificationManager.EnqueueDeferredRollModThresholdAdjustmentsForNextRoll(action, character, null);
            var layer = character.Effects.ConsumePendingActionBonusesNextHeroRoll();
            TestBase.AssertTrue(layer.Any(b => b.Type == RollModificationManager.SetComboThresholdType && Math.Abs(b.Value - 10) < 0.001),
                "Deferred FIFO layer should contain SET combo override 10",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Blank cadence: absolute overrides defer (next ACTION / 1 turn window).
        /// </summary>
        private static void TestDeferredRollModOverridesWhenCadenceBlank()
        {
            Console.WriteLine("\n--- Testing deferred roll-mod overrides when cadence is blank ---");

            var character = TestDataBuilders.Character().WithName("BlankCadence").Build();
            var tm = RollModificationManager.GetThresholdManager();
            tm.Clear();
            int comboBefore = tm.GetComboThreshold(character);

            var action = TestDataBuilders.CreateMockAction("BlankCombo", ActionType.Attack);
            action.Cadence = "";
            action.RollMods.ComboThresholdOverride = 12;

            RollModificationManager.ApplyThresholdOverrides(action, character, null);
            TestBase.AssertEqual(comboBefore, tm.GetComboThreshold(character),
                "Combo threshold unchanged on current roll when cadence is blank (deferred)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            RollModificationManager.EnqueueDeferredRollModThresholdAdjustmentsForNextRoll(action, character, null);
            var layer = character.Effects.ConsumePendingActionBonusesNextHeroRoll();
            TestBase.AssertTrue(layer.Any(b => b.Type == RollModificationManager.SetComboThresholdType && Math.Abs(b.Value - 12) < 0.001),
                "Blank cadence: deferred FIFO layer should contain SET combo override 12",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// ATTACK cadence: absolute overrides apply immediately on the current roll.
        /// </summary>
        private static void TestImmediateRollModOverridesWhenCadenceAttack()
        {
            Console.WriteLine("\n--- Testing immediate roll-mod overrides when cadence is ATTACK ---");

            var character = TestDataBuilders.Character().WithName("AttackCadence").Build();
            var tm = RollModificationManager.GetThresholdManager();
            tm.Clear();

            var action = TestDataBuilders.CreateMockAction("ImmediateCombo", ActionType.Attack);
            action.Cadence = "ATTACK";
            action.RollMods.ComboThresholdOverride = 11;

            RollModificationManager.ApplyThresholdOverrides(action, character, null);
            TestBase.AssertEqual(11, tm.GetComboThreshold(character),
                "Combo threshold applies immediately when cadence is ATTACK",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Sheet RollBonus is no longer added inside EnqueueDeferred — it is queued on successful hit as FIFO ACCURACY.
        /// </summary>
        private static void TestDeferredSheetAccuracyQueuedWithBlankCadence()
        {
            Console.WriteLine("\n--- Testing deferred sheet accuracy: not auto-queued from EnqueueDeferred ---");

            var character = TestDataBuilders.Character().WithName("AccDefer").Build();
            var action = TestDataBuilders.CreateMockAction("FinisherStyle", ActionType.Attack);
            action.Cadence = "";
            action.Advanced.RollBonus = 3;

            RollModificationManager.EnqueueDeferredRollModThresholdAdjustmentsForNextRoll(action, character, null);
            var layer = character.Effects.ConsumePendingActionBonusesNextHeroRoll();
            TestBase.AssertTrue(layer.Count == 0,
                "EnqueueDeferred should not enqueue sheet ACCURACY from RollBonus (hit path queues ACCURACY)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            character.Effects.AddPendingActionBonusesNextHeroRoll(new List<ActionAttackBonusItem>
            {
                new ActionAttackBonusItem { Type = "ACCURACY", Value = 3 }
            });
            var manual = character.Effects.ConsumePendingActionBonusesNextHeroRoll();
            TestBase.AssertTrue(manual.Count == 1 && manual[0].Type == "ACCURACY" && Math.Abs(manual[0].Value - 3) < 0.001,
                "Manual ACCURACY FIFO matches hit-path queue",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
