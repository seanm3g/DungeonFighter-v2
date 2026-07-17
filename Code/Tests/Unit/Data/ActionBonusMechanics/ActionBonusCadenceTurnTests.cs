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
    public static class ActionBonusCadenceTurnTests
    {
        public static void RunAll(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            TestForNextAttack_OnSuccess();
            TestForNextAttack_OnHit_AppliesPrimaryCategoryBonus();
            TestForNextAttack_OnFailure();
            TestTurnCadenceStatBonusDoesNotStackOnGrantingHit();

            testsRun += _testsRun;
            testsPassed += _testsPassed;
            testsFailed += _testsFailed;
        }

        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;



        /// <summary>
        /// For Next turn: When the roll HITS, stat bonuses (STR, AGI, etc) ARE applied.
        /// </summary>
        private static void TestForNextAttack_OnSuccess()
        {
            Console.WriteLine("\n--- For Next turn: On SUCCESS (hit) ---");
            Console.WriteLine("  ATTACK bonuses are consumed on every roll. Stat bonuses (STR/AGI) apply ONLY when we HIT.\n");

            var lastUsed = new Dictionary<Actor, Action>();
            var lastCritMiss = new Dictionary<Actor, bool>();
            int verified = 0;

            for (int i = 0; i < 100; i++)
            {
                var character = ActionBonusMechanicsTestHelpers.CreateTestCharacterWithCombo();
                character.Stats.TempStrengthBonus = 0;
                character.Effects.AddActionAttackBonuses(new ActionAttackBonuses
                {
                    BonusGroups = new List<ActionAttackBonusGroup>
                    {
                        new ActionAttackBonusGroup
                        {
                            CadenceType = "TURN",
                            Count = 1,
                            Bonuses = new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "STR", Value = 5 } }
                        }
                    }
                });

                var action = character.GetComboActions()[0];
                var enemy = new Enemy("TestEnemy", 1, 100, 5, 5, 5, 5);
                var result = ActionExecutionFlow.Execute(character, enemy, null, null, action, null, lastUsed, lastCritMiss);

                if (result.Hit)
                {
                    verified++;
                    TestBase.AssertTrue(character.Stats.TempStrengthBonus >= 5,
                        "ATTACK on HIT: STR bonus applied",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    Console.WriteLine($"  Verified: Roll hit -> TempStrengthBonus = {character.Stats.TempStrengthBonus}.\n");
                    break;
                }
            }

            TestBase.AssertTrue(verified >= 1,
                "For Next turn on hit: stat bonus applied",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }


        /// <summary>
        /// ATTACK cadence stat bonus type <c>PRIMARY</c> resolves to the hero's highest effective attribute.
        /// </summary>
        private static void TestForNextAttack_OnHit_AppliesPrimaryCategoryBonus()
        {
            Console.WriteLine("\n--- For Next turn: PRIMARY category on SUCCESS ---");

            var lastUsed = new Dictionary<Actor, Action>();
            var lastCritMiss = new Dictionary<Actor, bool>();
            int verified = 0;

            for (int i = 0; i < 100; i++)
            {
                var character = ActionBonusMechanicsTestHelpers.CreateTestCharacterWithCombo();
                character.Stats.Strength = 20;
                character.Stats.Agility = 8;
                character.Stats.Technique = 8;
                character.Stats.Intelligence = 8;
                character.Stats.TempStrengthBonus = 0;
                character.Effects.AddActionAttackBonuses(new ActionAttackBonuses
                {
                    BonusGroups = new List<ActionAttackBonusGroup>
                    {
                        new ActionAttackBonusGroup
                        {
                            CadenceType = "TURN",
                            Count = 1,
                            Bonuses = new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "PRIMARY", Value = 5 } }
                        }
                    }
                });

                var action = character.GetComboActions()[0];
                var enemy = new Enemy("TestEnemy", 1, 100, 5, 5, 5, 5);
                var result = ActionExecutionFlow.Execute(character, enemy, null, null, action, null, lastUsed, lastCritMiss);

                if (result.Hit)
                {
                    verified++;
                    TestBase.AssertTrue(character.Stats.TempStrengthBonus >= 5,
                        "ATTACK on HIT: PRIMARY bonus applies to highest stat (STR)",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    break;
                }
            }

            TestBase.AssertTrue(verified >= 1,
                "For Next turn PRIMARY on hit: resolved to primary attribute",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }


        /// <summary>
        /// For Next turn: When the roll MISSES, stat bonuses are consumed but NOT applied.
        /// </summary>
        private static void TestForNextAttack_OnFailure()
        {
            Console.WriteLine("\n--- For Next turn: On FAILURE (miss) ---");
            Console.WriteLine("  ATTACK bonuses are consumed on every roll. When we MISS, stat bonuses are wasted (not applied).\n");

            var lastUsed = new Dictionary<Actor, Action>();
            var lastCritMiss = new Dictionary<Actor, bool>();
            int verified = 0;

            for (int i = 0; i < 100; i++)
            {
                var character = ActionBonusMechanicsTestHelpers.CreateTestCharacterWithCombo();
                character.Stats.TempStrengthBonus = 0;
                character.Effects.AddActionAttackBonuses(new ActionAttackBonuses
                {
                    BonusGroups = new List<ActionAttackBonusGroup>
                    {
                        new ActionAttackBonusGroup
                        {
                            CadenceType = "TURN",
                            Count = 1,
                            Bonuses = new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "STR", Value = 5 } }
                        }
                    }
                });

                var action = character.GetComboActions()[0];
                var enemy = new Enemy("TestEnemy", 1, 100, 5, 5, 5, 5);
                var result = ActionExecutionFlow.Execute(character, enemy, null, null, action, null, lastUsed, lastCritMiss);

                if (!result.Hit)
                {
                    verified++;
                    TestBase.AssertTrue(character.Stats.TempStrengthBonus == 0,
                        "ATTACK on MISS: STR bonus NOT applied (consumed but wasted)",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertTrue(character.Effects.PeekTurnBonuses().Count == 0,
                        "ATTACK bonus consumed even on miss",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    Console.WriteLine($"  Verified: Roll missed -> TempStrengthBonus = 0, bonus consumed.\n");
                    break;
                }
            }

            TestBase.AssertTrue(verified >= 1,
                "For Next turn on miss: stat bonus not applied",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }


        /// <summary>
        /// TURN cadence stat bonuses (e.g. READ BOOK +1 INT) queue for the next roll and must not stack on the granting hit.
        /// </summary>
        private static void TestTurnCadenceStatBonusDoesNotStackOnGrantingHit()
        {
            Console.WriteLine("\n--- TURN cadence: stat bonus queues without stacking on grant hit ---\n");

            var lastUsed = new Dictionary<Actor, Action>();
            var lastCritMiss = new Dictionary<Actor, bool>();

            var character = new Character("TestHero", 1);
            character.Stats.Intelligence = 6;
            character.Stats.TempIntelligenceBonus = 0;

            var readBook = TestDataBuilders.CreateMockAction("READ BOOK", ActionType.Spell);
            readBook.Cadence = "TURN";
            readBook.Advanced.StatBonuses = new List<StatBonusEntry>
            {
                new StatBonusEntry { Type = "INT", Value = 1 }
            };
            readBook.ActionAttackBonuses = new ActionAttackBonuses
            {
                BonusGroups = new List<ActionAttackBonusGroup>
                {
                    new ActionAttackBonusGroup
                    {
                        CadenceType = "TURN",
                        Count = 1,
                        Bonuses = new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "INT", Value = 1 } }
                    }
                }
            };

            var enemy = new Enemy("TestEnemy", 1, 100, 5, 5, 5, 5);

            try
            {
                Dice.SetTestRoll(16);
                ActionSelector.SetStoredActionRoll(character, 16);
                var grant = ActionExecutionFlow.Execute(character, enemy, null, null, readBook, null, lastUsed, lastCritMiss);
                TestBase.AssertTrue(grant.Hit,
                    "READ BOOK grant hit",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(0, character.Stats.TempIntelligenceBonus,
                    "Granting hit must not apply TURN stat bonus immediately",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(character.Effects.PeekTurnBonuses().Any(b => b.Type == "INT" && b.Value == 1),
                    "TURN +1 INT queued for next roll",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                var followUp = TestDataBuilders.CreateMockAction("SLAM", ActionType.Attack);
                followUp.DamageMultiplier = 1.0;
                Dice.SetTestRoll(16);
                ActionSelector.SetStoredActionRoll(character, 16);
                var consume = ActionExecutionFlow.Execute(character, enemy, null, null, followUp, null, lastUsed, lastCritMiss);
                TestBase.AssertTrue(consume.Hit,
                    "Follow-up hit consumes TURN INT",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(1, character.Stats.TempIntelligenceBonus,
                    "Consumed TURN INT applies once (+1), not stacked",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                Dice.SetTestRoll(16);
                ActionSelector.SetStoredActionRoll(character, 16);
                _ = ActionExecutionFlow.Execute(character, enemy, null, null, readBook, null, lastUsed, lastCritMiss);
                TestBase.AssertEqual(0, character.Stats.TempIntelligenceBonus,
                    "Temp INT expires before the next granting action",
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
