using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Actions;
using RPGGame.Actions.Execution;
using RPGGame.Actions.RollModification;
using RPGGame.Data;
using RPGGame.Tests;
using RPGGame.UI;
using RPGGame.Utils;

namespace RPGGame.Tests.Unit.Data.ActionBonusMechanics
{
    public static class ActionBonusMechanicsFlowTests
    {
        public static void RunAll(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            TestActionBonusAddLogic_Deterministic();
            TestFullFlow_ActionBuffsNextAction();
            TestActionBonusNextRollSurvivesComboStepMismatch();
            TestActionCadenceComboThresholdDoesNotPersistAcrossRolls();
            TestActionCadenceAdditiveBankMergesDeposits();
            TestActionCadenceBankClearsOnRoomEnd();
            TestActionGrantRequiresCombo();
            TestActionBonusClipAtFinisher();
            TestActionBonusClipMidCombo();
            TestActionBonusMissKeepsLayer();
            TestActionBonusHitNoComboKeepsLayer();
            TestActionBonusComboConsumesLayer();
            TestThreeActionComboDamageModBuffsRemainingSlots();
            TestActionBonusDurationFollowsComboBonusDurationWhenGroupCountStale();
            TestActionLabForcedGetActionUsesLoaderCadenceDuration();

            testsRun += _testsRun;
            testsPassed += _testsPassed;
            testsFailed += _testsFailed;
        }

        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;



        /// <summary>
        /// Deterministic: Simulates the ACTION bonus add logic (what ApplyHitOutcome does) to verify slot indexing.
        /// </summary>
        private static void TestActionBonusAddLogic_Deterministic()
        {
            Console.WriteLine("--- Deterministic: ACTION bonus add logic ---");
            Console.WriteLine("  Simulates ApplyHitOutcome: when slot 0 succeeds, add +3 COMBO to next-hero-roll queue.\n");

            var character = ActionBonusMechanicsTestHelpers.CreateComboWithBuffingAction();
            var comboActions = character.GetComboActions();
            int comboLength = comboActions.Count;
            character.ComboStep = 0;

            TestBase.AssertTrue(comboLength >= 2,
                "Combo must have at least 2 actions for this test",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            Console.WriteLine($"  Combo has {comboLength} actions: [{string.Join(", ", comboActions.Select(a => a.Name))}]");

            var bonuses = new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "COMBO", Value = 3 } };
            character.Effects.AddPendingActionBonusesNextHeroRoll(bonuses);

            var pending = character.Effects.PeekPendingActionBonusesNextHeroRoll();
            TestBase.AssertTrue(pending.Count > 0 && pending.Any(b => b.Type == "COMBO" && b.Value == 3),
                "Simulated add: next-hero-roll queue has +3 COMBO",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            Console.WriteLine($"  Next-roll pending: {pending.Count} bonus(es). OK.\n");
        }


        /// <summary>
        /// Full sequence: Action 1 has "For next ACTION: +3 COMBO". When Action 1 succeeds, Action 2 gets the bonus.
        /// When we roll for Action 2, the bonus is consumed and applied to the roll.
        /// </summary>
        private static void TestFullFlow_ActionBuffsNextAction()
        {
            Console.WriteLine("--- Full Flow: Action 1 buffs Action 2 (For next ACTION) ---");
            Console.WriteLine("  Setup: Combo [SetupStrike, Finisher]. SetupStrike grants +3 COMBO to next action.");
            Console.WriteLine("  Step 1: Execute SetupStrike. On success -> next hero roll gets +3 COMBO.");
            Console.WriteLine("  Step 2: Execute Finisher. Bonuses consumed and applied to roll.\n");

            var lastUsed = new Dictionary<Actor, Action>();
            var lastCritMiss = new Dictionary<Actor, bool>();
            int successCount = 0;

            for (int i = 0; i < 100; i++)
            {
                var character = ActionBonusMechanicsTestHelpers.CreateComboWithBuffingAction();
                var comboActions = character.GetComboActions();
                if (comboActions.Count < 2)
                {
                    Console.WriteLine($"  WARNING: Combo has {comboActions.Count} actions (expected 2). Skipping run.");
                    continue;
                }
                var setupAction = comboActions[0];
                var finisherAction = comboActions[1];
                var enemy = new Enemy("TestEnemy", 1, 100, 5, 5, 5, 5);

                // Step 1: Execute SetupStrike (has "For next ACTION: +3 COMBO")
                var result1 = ActionExecutionFlow.Execute(character, enemy, null, null, setupAction, null, lastUsed, lastCritMiss);
                if (!result1.Hit || !result1.IsCombo) continue;

                successCount++;
                Console.WriteLine($"  Run {successCount}: SetupStrike succeeded (hit+combo). ComboStep={character.ComboStep}, ComboLength={comboActions.Count}");

                var pendingBefore = character.Effects.PeekPendingActionBonusesNextHeroRoll();
                Console.WriteLine($"  Next-roll pending: {pendingBefore.Count} bonus(es) - {string.Join(", ", pendingBefore.Select(b => $"{b.Type}+{b.Value}"))}");
                TestBase.AssertTrue(pendingBefore.Count > 0 && pendingBefore.Any(b => b.Type == "COMBO" && b.Value == 3),
                    "After SetupStrike succeeds: next-hero-roll queue has +3 COMBO pending",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Step 2: Execute Finisher. Bonuses consumed only on hit+combo.
                character.ComboStep = 1;
                int comboMin = GameConfiguration.Instance.RollSystem.ComboThreshold.Min;
                if (comboMin <= 0) comboMin = 14;
                Dice.SetTestRoll(Math.Max(comboMin + 1, 15));
                ActionSelector.SetStoredActionRoll(character, Math.Max(comboMin + 1, 15));
                var result2 = ActionExecutionFlow.Execute(character, enemy, null, null, finisherAction, null, lastUsed, lastCritMiss);
                Dice.ClearTestRoll();
                ActionSelector.ClearStoredRolls();

                TestBase.AssertTrue(result2.Hit && result2.IsCombo,
                    "Finisher must hit+combo to consume pending ACTION bonus",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                var pendingAfter = character.Effects.PeekPendingActionBonusesNextHeroRoll();
                TestBase.AssertTrue(pendingAfter.Count == 0,
                    "After Finisher hit+combo: next-roll ACTION bonuses consumed",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                Console.WriteLine($"  Run {successCount}: Finisher executed. Bonuses consumed. Hit={result2.Hit}, Combo={result2.IsCombo}.\n");
                break;
            }

            TestBase.AssertTrue(successCount >= 1,
                $"Full flow verified in {successCount} successful run(s)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }


        /// <summary>
        /// ACTION cadence bonuses must apply on the hero's next roll even if ComboStep no longer matches the slot
        /// that would have received them under the old per-slot model (simulates an enemy turn or combo drift).
        /// </summary>
        private static void TestActionBonusNextRollSurvivesComboStepMismatch()
        {
            Console.WriteLine("--- Regression: ACTION roll buff survives ComboStep mismatch before next hero roll ---\n");

            var lastUsed = new Dictionary<Actor, Action>();
            var lastCritMiss = new Dictionary<Actor, bool>();
            int comboMin = GameConfiguration.Instance.RollSystem.ComboThreshold.Min;
            if (comboMin <= 0)
                comboMin = 14;
            int edgeTotal = comboMin - 1;

            try
            {
                ActionSelector.ClearStoredRolls();

                var character = ActionBonusMechanicsTestHelpers.CreateComboWithBuffingAction();
                character.Intelligence = 0;
                var comboActions = character.GetComboActions();
                var setup = comboActions[0];
                var finisher = comboActions[1];
                var enemy = new Enemy("TestEnemy", 1, 100, 5, 5, 5, 5);

                Dice.SetTestRoll(Math.Max(comboMin + 1, 15));
                var r1 = ActionExecutionFlow.Execute(character, enemy, null, null, setup, null, lastUsed, lastCritMiss);
                TestBase.AssertTrue(r1.Hit && r1.IsCombo,
                    "Setup: hit + combo so ACTION bonus is queued for next hero roll",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(character.Effects.PeekPendingActionBonusesNextHeroRoll().Any(b => b.Type == "COMBO" && b.Value == 3),
                    "Pending +3 COMBO is on next-roll queue",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Would have targeted slot 1 under old model; force step 0 so slot-based consumption would miss the buff.
                character.ComboStep = 0;
                Dice.SetTestRoll(edgeTotal);
                var r2 = ActionExecutionFlow.Execute(character, enemy, null, null, finisher, null, lastUsed, lastCritMiss);
                TestBase.AssertTrue(r2.Hit && r2.IsCombo,
                    "Finisher with edge attack total + queued COMBO must hit+combo",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(character.Effects.PeekPendingActionBonusesNextHeroRoll().Count == 0,
                    "Next-roll ACTION bonuses consumed after finisher hit+combo",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                Dice.ClearTestRoll();
                ActionSelector.ClearStoredRolls();
            }
        }


        /// <summary>
        /// ACTION cadence +COMBO to threshold applies to one roll only; the Next turn must not keep an easier combo threshold.
        /// Uses attack total = comboMin - 1: with default threshold it is not a combo; with a one-shot -3 it is; third swing must revert.
        /// </summary>
        private static void TestActionCadenceComboThresholdDoesNotPersistAcrossRolls()
        {
            Console.WriteLine("--- Regression: ACTION COMBO threshold does not persist across rolls ---\n");

            var lastUsed = new Dictionary<Actor, Action>();
            var lastCritMiss = new Dictionary<Actor, bool>();
            var character = ActionBonusMechanicsTestHelpers.CreateComboWithBuffingAction();
            character.Intelligence = 0;
            var comboActions = character.GetComboActions();
            var setup = comboActions[0];
            var finisher = comboActions[1];
            var enemy = new Enemy("TestEnemy", 1, 100, 5, 5, 5, 5);
            int comboMin = GameConfiguration.Instance.RollSystem.ComboThreshold.Min;
            if (comboMin <= 0)
                comboMin = 14;
            int edgeTotal = comboMin - 1;

            try
            {
                ActionSelector.ClearStoredRolls();

                Dice.SetTestRoll(Math.Max(comboMin + 1, 15));
                var r1 = ActionExecutionFlow.Execute(character, enemy, null, null, setup, null, lastUsed, lastCritMiss);
                TestBase.AssertTrue(r1.Hit && r1.IsCombo,
                    "Setup: hit + combo so ACTION bonus is queued for next slot",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                Dice.SetTestRoll(edgeTotal);
                character.ComboStep = 1;
                var r2 = ActionExecutionFlow.Execute(character, enemy, null, null, finisher, null, lastUsed, lastCritMiss);
                TestBase.AssertTrue(r2.Hit,
                    "Finisher with edge attack total still hits",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(r2.IsCombo,
                    "Finisher: +3 COMBO threshold bonus makes edge total count as combo",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                Dice.SetTestRoll(edgeTotal);
                character.ComboStep = 0;
                var r3 = ActionExecutionFlow.Execute(character, enemy, null, null, setup, null, lastUsed, lastCritMiss);
                TestBase.AssertTrue(r3.Hit,
                    "Setup again with same edge total still hits",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(!r3.IsCombo,
                    "Third swing: no stale combo threshold — edge total must not count as combo",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                Dice.ClearTestRoll();
                ActionSelector.ClearStoredRolls();
            }
        }


        /// <summary>
        /// ACTION cadence deposits merge additively; one redeem clears the full bank.
        /// </summary>
        private static void TestActionCadenceAdditiveBankMergesDeposits()
        {
            Console.WriteLine("--- ACTION cadence: additive bank (multiple deposits, one redeem) ---\n");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            var oneDeposit = new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "COMBO", Value = 2 } };
            for (int i = 0; i < 3; i++)
                character.Effects.AddPendingActionBonusesNextHeroRoll(oneDeposit);

            TestBase.AssertEqual(3, character.Effects.GetPendingActionCadenceLayerCount(),
                "Three deposits increment stack count",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            var peeked = character.Effects.PeekPendingActionBonusesNextHeroRoll();
            TestBase.AssertTrue(peeked.Count == 1 && peeked[0].Type == "COMBO" && Math.Abs(peeked[0].Value - 6) < 0.01,
                "Three +2 COMBO deposits merge to +6 COMBO",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var redeemed = character.Effects.ConsumePendingActionBonusesNextHeroRoll();
            TestBase.AssertTrue(redeemed.Count == 1 && Math.Abs(redeemed[0].Value - 6) < 0.01,
                "Redeem returns full merged bank",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, character.Effects.GetPendingActionCadenceLayerCount(),
                "After redeem bank is empty",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            Console.WriteLine("  OK: deposits stack additively; one redeem clears the bank.\n");
        }


        private static void TestActionCadenceBankClearsOnRoomEnd()
        {
            Console.WriteLine("--- ACTION cadence: bank clears when room ends (ClearAllTempEffects) ---\n");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            character.Effects.AccumulatePendingActionCadenceBank(new List<ActionAttackBonusItem>
            {
                new ActionAttackBonusItem { Type = "DAMAGE_MOD", Value = 25 }
            }, 2);
            TestBase.AssertTrue(character.Effects.HasPendingActionCadenceBank(),
                "Bank has deposits before room end",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            character.Effects.ClearAllTempEffects();
            TestBase.AssertFalse(character.Effects.HasPendingActionCadenceBank(),
                "Bank cleared after room-end temp effect wipe",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, character.Effects.GetPendingActionCadenceLayerCount(),
                "Deposit count reset after room end",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            Console.WriteLine("  OK: ACTION bank decays on room end.\n");
        }


        private static void TestActionGrantRequiresCombo()
        {
            Console.WriteLine("\n--- ACTION grant: hit without combo queues nothing ---\n");

            var lastUsed = new Dictionary<Actor, Action>();
            var lastCritMiss = new Dictionary<Actor, bool>();
            int comboMin = GameConfiguration.Instance.RollSystem.ComboThreshold.Min;
            if (comboMin <= 0) comboMin = 14;
            int hitNoComboRoll = Math.Max(1, comboMin - 1);

            try
            {
                ActionSelector.ClearStoredRolls();
                var character = ActionBonusMechanicsTestHelpers.CreateComboWithBuffingAction();
                character.Intelligence = 0;
                var setup = character.GetComboActions()[0];
                var enemy = new Enemy("TestEnemy", 1, 100, 5, 5, 5, 5);

                Dice.SetTestRoll(hitNoComboRoll);
                ActionSelector.SetStoredActionRoll(character, hitNoComboRoll);
                var result = ActionExecutionFlow.Execute(character, enemy, null, null, setup, null, lastUsed, lastCritMiss);
                TestBase.AssertTrue(result.Hit && !result.IsCombo,
                    "Setup hits but does not combo",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(0, character.Effects.GetPendingActionCadenceLayerCount(),
                    "Hit without combo must not queue ACTION bonus layers",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                Dice.ClearTestRoll();
                ActionSelector.ClearStoredRolls();
            }
        }


        private static void TestActionBonusClipAtFinisher()
        {
            Console.WriteLine("\n--- ACTION grant clip: finisher grants zero layers ---\n");

            var lastUsed = new Dictionary<Actor, Action>();
            var lastCritMiss = new Dictionary<Actor, bool>();
            int comboMin = GameConfiguration.Instance.RollSystem.ComboThreshold.Min;
            if (comboMin <= 0) comboMin = 14;

            try
            {
                ActionSelector.ClearStoredRolls();
                var character = ActionBonusMechanicsTestHelpers.CreateComboWithBuffingAction(count: 3);
                var finisher = character.GetComboActions()[1];
                var enemy = new Enemy("TestEnemy", 1, 100, 5, 5, 5, 5);
                character.ComboStep = 1;

                Dice.SetTestRoll(Math.Max(comboMin + 1, 15));
                ActionSelector.SetStoredActionRoll(character, Math.Max(comboMin + 1, 15));
                var result = ActionExecutionFlow.Execute(character, enemy, null, null, finisher, null, lastUsed, lastCritMiss);
                TestBase.AssertTrue(result.Hit && result.IsCombo,
                    "Finisher hit+combo",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(0, character.Effects.GetPendingActionCadenceLayerCount(),
                    "Finisher with Count=3 clips to 0 remaining combo slots",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                Dice.ClearTestRoll();
                ActionSelector.ClearStoredRolls();
            }
        }


        private static void TestActionBonusClipMidCombo()
        {
            Console.WriteLine("\n--- ACTION grant clip: mid-combo Count=3 clips to remaining slots ---\n");

            var lastUsed = new Dictionary<Actor, Action>();
            var lastCritMiss = new Dictionary<Actor, bool>();
            int comboMin = GameConfiguration.Instance.RollSystem.ComboThreshold.Min;
            if (comboMin <= 0) comboMin = 14;

            try
            {
                ActionSelector.ClearStoredRolls();
                var character = ActionBonusMechanicsTestHelpers.CreateFourActionComboWithBuffingAtSlot(1, bonusCount: 3);
                var granting = character.GetComboActions()[1];
                var enemy = new Enemy("TestEnemy", 1, 100, 5, 5, 5, 5);
                character.ComboStep = 1;

                Dice.SetTestRoll(Math.Max(comboMin + 1, 15));
                ActionSelector.SetStoredActionRoll(character, Math.Max(comboMin + 1, 15));
                var result = ActionExecutionFlow.Execute(character, enemy, null, null, granting, null, lastUsed, lastCritMiss);
                TestBase.AssertTrue(result.Hit && result.IsCombo,
                    "Mid-combo action hit+combo",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(2, character.Effects.GetPendingActionCadenceLayerCount(),
                    "Slot 1 of 4 with Count=3 clips to 2 remaining actions",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                Dice.ClearTestRoll();
                ActionSelector.ClearStoredRolls();
            }
        }


        private static void TestActionBonusMissKeepsLayer()
        {
            Console.WriteLine("\n--- ACTION consume: miss keeps FIFO layer ---\n");

            var lastUsed = new Dictionary<Actor, Action>();
            var lastCritMiss = new Dictionary<Actor, bool>();

            try
            {
                ActionSelector.ClearStoredRolls();
                var character = ActionBonusMechanicsTestHelpers.CreateComboWithBuffingAction();
                character.Effects.AddPendingActionBonusesNextHeroRoll(new List<ActionAttackBonusItem>
                {
                    new ActionAttackBonusItem { Type = "COMBO", Value = 3 }
                });
                var finisher = character.GetComboActions()[1];
                var enemy = new Enemy("TestEnemy", 1, 100, 5, 5, 5, 5);
                character.ComboStep = 1;

                Dice.SetTestRoll(1);
                ActionSelector.SetStoredActionRoll(character, 1);
                var result = ActionExecutionFlow.Execute(character, enemy, null, null, finisher, null, lastUsed, lastCritMiss);
                TestBase.AssertFalse(result.Hit,
                    "Forced miss",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(1, character.Effects.GetPendingActionCadenceLayerCount(),
                    "Miss must keep pending ACTION layer until hit+combo",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(character.Effects.PeekPendingActionBonusesNextHeroRoll()
                        .Any(b => string.Equals(b.Type, "COMBO", StringComparison.OrdinalIgnoreCase) && b.Value == 3),
                    "Miss must leave COMBO bonus in the bank",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                Dice.ClearTestRoll();
                ActionSelector.ClearStoredRolls();
            }
        }


        private static void TestActionBonusHitNoComboKeepsLayer()
        {
            Console.WriteLine("\n--- ACTION consume: hit without combo keeps FIFO layer ---\n");

            var lastUsed = new Dictionary<Actor, Action>();
            var lastCritMiss = new Dictionary<Actor, bool>();

            try
            {
                ActionSelector.ClearStoredRolls();
                var character = ActionBonusMechanicsTestHelpers.CreateComboWithBuffingAction();
                character.Intelligence = 0;
                var tm = RollModificationManager.GetThresholdManager();
                tm.ResetThresholds(character);
                TechniqueMilestoneThresholdBonuses.Apply(tm, character);
                NaiveteThresholdBonuses.Apply(tm, character);
                int hitMin = tm.GetHitThreshold(character);
                int comboTh = tm.GetComboThreshold(character);
                character.Effects.AddPendingActionBonusesNextHeroRoll(new List<ActionAttackBonusItem>
                {
                    new ActionAttackBonusItem { Type = "SPEED_MOD", Value = 10 }
                });
                var finisher = character.GetComboActions()[1];
                var enemy = new Enemy("TestEnemy", 1, 100, 5, 5, 5, 5);
                character.ComboStep = 1;

                int targetAttack = Math.Max(hitMin, comboTh - 1);
                int rollBonus = ActionUtilities.CalculateRollBonus(character, finisher, consumeTempBonus: false);
                int baseRoll = targetAttack - rollBonus;
                Dice.SetTestRoll(baseRoll);
                ActionSelector.SetStoredActionRoll(character, baseRoll);
                var result = ActionExecutionFlow.Execute(character, enemy, null, null, finisher, null, lastUsed, lastCritMiss);
                TestBase.AssertTrue(result.Hit && !result.IsCombo,
                    "Hit without combo",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(1, character.Effects.GetPendingActionCadenceLayerCount(),
                    "Hit without combo must keep pending ACTION layer until hit+combo",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(0, character.Effects.ConsumedSpeedModPercent,
                    "SPEED_MOD must not redeem on non-combo hit",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                Dice.ClearTestRoll();
                ActionSelector.ClearStoredRolls();
            }
        }


        private static void TestActionBonusComboConsumesLayer()
        {
            Console.WriteLine("\n--- ACTION consume: hit+combo removes FIFO layer ---\n");

            var lastUsed = new Dictionary<Actor, Action>();
            var lastCritMiss = new Dictionary<Actor, bool>();
            int comboMin = GameConfiguration.Instance.RollSystem.ComboThreshold.Min;
            if (comboMin <= 0) comboMin = 14;

            try
            {
                ActionSelector.ClearStoredRolls();
                var character = ActionBonusMechanicsTestHelpers.CreateComboWithBuffingAction();
                character.Effects.AddPendingActionBonusesNextHeroRoll(new List<ActionAttackBonusItem>
                {
                    new ActionAttackBonusItem { Type = "COMBO", Value = 3 }
                });
                var finisher = character.GetComboActions()[1];
                var enemy = new Enemy("TestEnemy", 1, 100, 5, 5, 5, 5);
                character.ComboStep = 1;

                Dice.SetTestRoll(Math.Max(comboMin + 1, 15));
                ActionSelector.SetStoredActionRoll(character, Math.Max(comboMin + 1, 15));
                var result = ActionExecutionFlow.Execute(character, enemy, null, null, finisher, null, lastUsed, lastCritMiss);
                TestBase.AssertTrue(result.Hit && result.IsCombo,
                    "Hit+combo consumes layer",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(0, character.Effects.GetPendingActionCadenceLayerCount(),
                    "Hit+combo must consume pending ACTION layer",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                Dice.ClearTestRoll();
                ActionSelector.ClearStoredRolls();
            }
        }


        /// <summary>
        /// 3-action combo [buff, mid, finisher]: ACTION x3 clipped to 2 deposits (+50% DAMAGE_MOD bank); mid redeems all.
        /// </summary>
        private static void TestThreeActionComboDamageModBuffsRemainingSlots()
        {
            Console.WriteLine("\n--- ACTION x3: additive bank redeems on first hit+combo (mid) ---\n");

            var lastUsed = new Dictionary<Actor, Action>();
            var lastCritMiss = new Dictionary<Actor, bool>();
            int comboMin = GameConfiguration.Instance.RollSystem.ComboThreshold.Min;
            if (comboMin <= 0) comboMin = 14;
            int comboRoll = Math.Max(comboMin + 2, 16);

            try
            {
                ActionSelector.ClearStoredRolls();
                var character = ActionBonusMechanicsTestHelpers.CreateThreeActionComboWithDamageModBuff(count: 3, damageModPercent: 25);
                var combo = character.GetComboActions();
                var buff = combo[0];
                var mid = combo[1];
                var finisher = combo[2];
                var enemy = new Enemy("TestEnemy", 1, 500, 5, 5, 5, 5);

                Dice.SetTestRoll(comboRoll);
                ActionSelector.SetStoredActionRoll(character, comboRoll);
                var r1 = ActionExecutionFlow.Execute(character, enemy, null, null, buff, null, lastUsed, lastCritMiss);
                TestBase.AssertTrue(r1.Hit && r1.IsCombo,
                    "Buff action hit+combo grants FIFO layers",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(2, character.Effects.GetPendingActionCadenceLayerCount(),
                    "Count=3 clipped to 2 additive deposits after slot 0",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                var bankPeek = character.Effects.PeekPendingActionBonusesNextHeroRoll();
                TestBase.AssertTrue(bankPeek.Any(b => b.Type == "DAMAGE_MOD" && Math.Abs(b.Value - 50) < 0.01),
                    "Two +25% deposits merge to +50% DAMAGE_MOD in bank",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(character.Effects.PeekTurnBonuses().Count == 0,
                    "ACTION cadence must not duplicate DAMAGE_MOD into ATTACK queue",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                character.ComboStep = 1;
                int hpBeforeMid = enemy.CurrentHealth;
                Dice.SetTestRoll(comboRoll);
                ActionSelector.SetStoredActionRoll(character, comboRoll);
                var r2 = ActionExecutionFlow.Execute(character, enemy, null, null, mid, null, lastUsed, lastCritMiss);
                TestBase.AssertTrue(r2.Hit && r2.IsCombo,
                    "Mid action hit+combo redeems full bank",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(0, character.Effects.GetPendingActionCadenceLayerCount(),
                    "Bank empty after mid redeems",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                int midDamage = hpBeforeMid - enemy.CurrentHealth;
                TestBase.AssertTrue(midDamage > 0, "Mid action dealt damage", ref _testsRun, ref _testsPassed, ref _testsFailed);

                character.ComboStep = 2;
                int hpBeforeFinisher = enemy.CurrentHealth;
                Dice.SetTestRoll(comboRoll);
                ActionSelector.SetStoredActionRoll(character, comboRoll);
                var r3 = ActionExecutionFlow.Execute(character, enemy, null, null, finisher, null, lastUsed, lastCritMiss);
                TestBase.AssertTrue(r3.Hit && r3.IsCombo,
                    "Finisher hit+combo after bank already redeemed",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(0, character.Effects.GetPendingActionCadenceLayerCount(),
                    "Bank still empty after finisher",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                int finisherDamage = hpBeforeFinisher - enemy.CurrentHealth;
                TestBase.AssertTrue(finisherDamage > 0, "Finisher dealt damage", ref _testsRun, ref _testsPassed, ref _testsFailed);

                var baseline = ActionBonusMechanicsTestHelpers.CreateThreeActionComboWithDamageModBuff(count: 3, damageModPercent: 25);
                var baselineEnemy = new Enemy("TestEnemy", 1, 500, 5, 5, 5, 5);
                baseline.ComboStep = 1;
                int hpBeforeBaselineMid = baselineEnemy.CurrentHealth;
                Dice.SetTestRoll(comboRoll);
                ActionSelector.SetStoredActionRoll(baseline, comboRoll);
                _ = ActionExecutionFlow.Execute(baseline, baselineEnemy, null, null, mid, null, lastUsed, lastCritMiss);
                int baselineMidDamage = hpBeforeBaselineMid - baselineEnemy.CurrentHealth;
                TestBase.AssertTrue(midDamage > baselineMidDamage,
                    "Mid with +50% bank deals more than unbuffed mid (+25% x2 deposits)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                baseline.ComboStep = 2;
                int hpBeforeBaselineFin = baselineEnemy.CurrentHealth;
                Dice.SetTestRoll(comboRoll);
                ActionSelector.SetStoredActionRoll(baseline, comboRoll);
                var rBaseFin = ActionExecutionFlow.Execute(baseline, baselineEnemy, null, null, finisher, null, lastUsed, lastCritMiss);
                TestBase.AssertTrue(rBaseFin.Hit, "Baseline finisher hits", ref _testsRun, ref _testsPassed, ref _testsFailed);
                int baselineFinDamage = hpBeforeBaselineFin - baselineEnemy.CurrentHealth;
                TestBase.AssertTrue(Math.Abs(finisherDamage - baselineFinDamage) < 0.01,
                    "Finisher deals normal damage after bank was spent on mid",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                Dice.ClearTestRoll();
                ActionSelector.ClearStoredRolls();
            }
        }


        /// <summary>
        /// Spreadsheet Duration (ComboBonusDuration) is authoritative when ActionAttackBonuses.Count is stale after an edit.
        /// </summary>
        private static void TestActionBonusDurationFollowsComboBonusDurationWhenGroupCountStale()
        {
            Console.WriteLine("\n--- ACTION duration: ComboBonusDuration overrides stale bonus group Count ---\n");

            var lastUsed = new Dictionary<Actor, Action>();
            var lastCritMiss = new Dictionary<Actor, bool>();
            int comboMin = GameConfiguration.Instance.RollSystem.ComboThreshold.Min;
            if (comboMin <= 0) comboMin = 14;
            int comboRoll = Math.Max(comboMin + 2, 16);

            try
            {
                ActionSelector.ClearStoredRolls();
                var character = ActionBonusMechanicsTestHelpers.CreateFourActionComboWithDamageModBuffAtSlot0(staleGroupCount: 3, comboBonusDuration: 2, damageModPercent: 25);
                var buff = character.GetComboActions()[0];
                var enemy = new Enemy("TestEnemy", 1, 500, 5, 5, 5, 5);

                Dice.SetTestRoll(comboRoll);
                ActionSelector.SetStoredActionRoll(character, comboRoll);
                var result = ActionExecutionFlow.Execute(character, enemy, null, null, buff, null, lastUsed, lastCritMiss);
                TestBase.AssertTrue(result.Hit && result.IsCombo,
                    "ACTION BONUS hit+combo",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(2, character.Effects.GetPendingActionCadenceLayerCount(),
                    "ComboBonusDuration=2 should add two additive deposits even when bonus group Count=3",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                var bank = character.Effects.PeekPendingActionBonusesNextHeroRoll();
                TestBase.AssertTrue(bank.Any(b => b.Type == "DAMAGE_MOD" && Math.Abs(b.Value - 50) < 0.01),
                    "Duration 2 merges to +50% DAMAGE_MOD in bank",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(2, ActionCadenceDurationResolver.GetDisplayCount(buff, buff.ActionAttackBonuses!.BonusGroups[0]),
                    "Display count should follow ComboBonusDuration",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                Dice.ClearTestRoll();
                ActionSelector.ClearStoredRolls();
            }
        }


        /// <summary>
        /// Action Lab passes a fresh <see cref="ActionLoader.GetAction"/> instance while the combo strip may hold stale bonus group Count.
        /// </summary>
        private static void TestActionLabForcedGetActionUsesLoaderCadenceDuration()
        {
            Console.WriteLine("\n--- ACTION Lab: forced GetAction uses loader cadence duration ---\n");

            var loaded = ActionLoader.GetAction("ACTION BONUS");
            if (loaded == null)
            {
                Console.WriteLine("  (skipped — ACTION BONUS not in Actions.json)");
                return;
            }

            var data = ActionLoader.GetActionData("ACTION BONUS");
            TestBase.AssertTrue(data != null && data.ComboBonusDuration == 3,
                "ACTION BONUS loader data should have cadence duration 3",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var lastUsed = new Dictionary<Actor, Action>();
            var lastCritMiss = new Dictionary<Actor, bool>();
            int comboMin = GameConfiguration.Instance.RollSystem.ComboThreshold.Min;
            if (comboMin <= 0) comboMin = 14;
            int comboRoll = Math.Max(comboMin + 2, 16);

            try
            {
                ActionSelector.ClearStoredRolls();
                var character = ActionBonusMechanicsTestHelpers.BuildFourActionLabComboWithStaleStripBonus(staleGroupCount: 5);
                var forced = ActionLoader.GetAction("ACTION BONUS");
                TestBase.AssertNotNull(forced, "Forced lab action resolves", ref _testsRun, ref _testsPassed, ref _testsFailed);

                Dice.SetTestRoll(comboRoll);
                ActionSelector.SetStoredActionRoll(character, comboRoll);
                var result = ActionExecutionFlow.Execute(character, new Enemy("T", 1, 500, 5, 5, 5, 5), null, null, forced, null, lastUsed, lastCritMiss);
                TestBase.AssertTrue(result.Hit && result.IsCombo, "ACTION BONUS hit+combo", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(3, character.Effects.GetPendingActionCadenceLayerCount(),
                    "Forced GetAction should add three additive deposits from loader duration (not stale strip Count=5)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                Dice.ClearTestRoll();
                ActionSelector.ClearStoredRolls();
            }
        }
    }
}
