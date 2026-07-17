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
    public static class ActionBonusCadenceActionTests
    {
        public static void RunAll(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            TestForNextAction_OnSuccess();
            TestForNextAction_OnFailure();
            TestActionCadenceStrAppliesOnNextActionCombo();

            testsRun += _testsRun;
            testsPassed += _testsPassed;
            testsFailed += _testsFailed;
        }

        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;



        /// <summary>
        /// For next ACTION: When the SOURCE action SUCCEEDS (hit+combo), bonuses are added to the next slot.
        /// </summary>
        private static void TestForNextAction_OnSuccess()
        {
            Console.WriteLine("\n--- For next ACTION: On SUCCESS ---");
            Console.WriteLine("  When Action 1 (with ACTION cadence) HITS and COMBOs -> next slot gets the bonus.\n");

            var lastUsed = new Dictionary<Actor, Action>();
            var lastCritMiss = new Dictionary<Actor, bool>();
            int verified = 0;

            for (int i = 0; i < 80; i++)
            {
                var character = ActionBonusMechanicsTestHelpers.CreateComboWithBuffingAction();
                var action1 = character.GetComboActions()[0];
                var enemy = new Enemy("TestEnemy", 1, 100, 5, 5, 5, 5);

                var result = ActionExecutionFlow.Execute(character, enemy, null, null, action1, null, lastUsed, lastCritMiss);

                if (result.Hit && result.IsCombo)
                {
                    verified++;
                    var pending = character.Effects.PeekPendingActionBonusesNextHeroRoll();
                    Console.WriteLine($"  ComboStep={character.ComboStep}, Next-roll pending count={pending.Count}");
                    TestBase.AssertTrue(pending.Count > 0,
                        "ACTION cadence on SUCCESS: next-hero-roll queue has pending bonuses",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    Console.WriteLine($"  Verified: Action succeeded -> slot 2 has {pending.Count} bonus(es).\n");
                    break;
                }
            }

            TestBase.AssertTrue(verified >= 1,
                "For next ACTION on success: bonuses added to next slot",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }


        /// <summary>
        /// For next ACTION: When the SOURCE action FAILS (miss), NO bonuses are added to the next slot.
        /// </summary>
        private static void TestForNextAction_OnFailure()
        {
            Console.WriteLine("\n--- For next ACTION: On FAILURE ---");
            Console.WriteLine("  When Action 1 (with ACTION cadence) MISSES -> next slot gets NOTHING.\n");

            var lastUsed = new Dictionary<Actor, Action>();
            var lastCritMiss = new Dictionary<Actor, bool>();
            int verified = 0;

            for (int i = 0; i < 80; i++)
            {
                var character = ActionBonusMechanicsTestHelpers.CreateComboWithBuffingAction();
                var action1 = character.GetComboActions()[0];
                var enemy = new Enemy("TestEnemy", 1, 100, 5, 5, 5, 5);

                var result = ActionExecutionFlow.Execute(character, enemy, null, null, action1, null, lastUsed, lastCritMiss);

                if (!result.Hit)
                {
                    verified++;
                    var pending = character.Effects.PeekPendingActionBonusesNextHeroRoll();
                    TestBase.AssertTrue(pending.Count == 0,
                        "ACTION cadence on FAILURE: next-hero-roll queue has NO pending bonuses",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    Console.WriteLine($"  Verified: Action missed -> next-roll queue empty.\n");
                    break;
                }
            }

            TestBase.AssertTrue(verified >= 1,
                "For next ACTION on failure: no bonuses added",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }


        /// <summary>
        /// ACTION cadence STR (e.g. WIND UP → SLAM): banked +1 STR must apply on the next hit+combo, not only roll modifiers.
        /// </summary>
        private static void TestActionCadenceStrAppliesOnNextActionCombo()
        {
            Console.WriteLine("\n--- For next ACTION: STR applies on next hit+combo (WIND UP → SLAM) ---\n");

            var lastUsed = new Dictionary<Actor, Action>();
            var lastCritMiss = new Dictionary<Actor, bool>();
            int comboMin = GameConfiguration.Instance.RollSystem.ComboThreshold.Min;
            if (comboMin <= 0) comboMin = 14;

            try
            {
                ActionSelector.ClearStoredRolls();

                var character = new Character("TestHero", 1);
                character.Stats.TempStrengthBonus = 0;

                var windUp = TestDataBuilders.CreateMockAction("WIND UP", ActionType.Attack);
                windUp.IsComboAction = true;
                windUp.ComboOrder = 1;
                windUp.Cadence = "ACTION";
                windUp.ActionAttackBonuses = new ActionAttackBonuses
                {
                    BonusGroups = new List<ActionAttackBonusGroup>
                    {
                        new ActionAttackBonusGroup
                        {
                            CadenceType = "ACTION",
                            Count = 1,
                            Bonuses = new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "STR", Value = 1 } }
                        }
                    }
                };

                var slam = TestDataBuilders.CreateMockAction("SLAM", ActionType.Attack);
                slam.IsComboAction = true;
                slam.ComboOrder = 2;
                slam.DamageMultiplier = 1.0;

                character.AddAction(windUp, 1.0);
                character.AddAction(slam, 1.0);
                character.Actions.AddToCombo(windUp);
                character.Actions.AddToCombo(slam);
                character.ComboStep = 0;

                var enemy = new Enemy("TestEnemy", 1, 100, 5, 5, 5, 5);

                Dice.SetTestRoll(Math.Max(comboMin + 1, 15));
                var r1 = ActionExecutionFlow.Execute(character, enemy, null, null, windUp, null, lastUsed, lastCritMiss);
                TestBase.AssertTrue(r1.Hit && r1.IsCombo,
                    "WIND UP hit+combo queues +1 STR for next action",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(character.Effects.PeekPendingActionBonusesNextHeroRoll().Any(b => b.Type == "STR" && b.Value == 1),
                    "Pending bank shows +1 STR",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(0, character.Stats.TempStrengthBonus,
                    "STR not applied until next action redeems the bank",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                character.ComboStep = 1;
                Dice.SetTestRoll(Math.Max(comboMin + 1, 15));
                ActionSelector.SetStoredActionRoll(character, Math.Max(comboMin + 1, 15));
                var r2 = ActionExecutionFlow.Execute(character, enemy, null, null, slam, null, lastUsed, lastCritMiss);
                TestBase.AssertTrue(r2.Hit && r2.IsCombo,
                    "SLAM hit+combo redeems pending ACTION STR",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(character.Stats.TempStrengthBonus >= 1,
                    "SLAM hit+combo applies banked +1 STR",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(character.Effects.PeekPendingActionBonusesNextHeroRoll().Count == 0,
                    "Bank consumed after SLAM hit+combo",
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
