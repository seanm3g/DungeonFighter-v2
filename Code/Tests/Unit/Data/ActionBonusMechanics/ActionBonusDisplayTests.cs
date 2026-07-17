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
    public static class ActionBonusDisplayTests
    {
        public static void RunAll(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            TestStateAndDisplay();

            testsRun += _testsRun;
            testsPassed += _testsPassed;
            testsFailed += _testsFailed;
        }

        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;



        private static void TestStateAndDisplay()
        {
            Console.WriteLine("\n--- State and Display ---");

            // Pending add/consume
            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            character.Effects.AddPendingActionBonuses(1, new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "COMBO", Value = 3 } });
            var consumed = character.Effects.ConsumePendingActionBonusesForSlot(1);
            TestBase.AssertTrue(consumed.Count == 1 && consumed[0].Type == "COMBO",
                "Pending bonuses: add then consume returns correct bonuses",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // ATTACK consume
            character.Effects.ClearAllTempEffects();
            character.Effects.AddActionAttackBonuses(new ActionAttackBonuses
            {
                BonusGroups = new List<ActionAttackBonusGroup>
                {
                    new ActionAttackBonusGroup { CadenceType = "TURN", Count = 1, Bonuses = new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "HIT", Value = 2 } } }
                }
            });
            var attackConsumed = character.Effects.GetAndConsumeTurnBonuses();
            TestBase.AssertTrue(attackConsumed.Count > 0 && character.Effects.PeekTurnBonuses().Count == 0,
                "ATTACK bonuses: consume clears queue",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // BuildActiveModifierLines
            character.Effects.ClearAllTempEffects();
            character.Effects.AddPendingActionBonusesNextHeroRoll(new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "COMBO", Value = 3 } });
            var lines = CombatActionStripBuilder.BuildActiveModifierLines(character);
            TestBase.AssertTrue(lines != null && lines.Count >= 1 && lines[0].Contains("Next action:") && lines[0].Contains("COMBO"),
                "Display: BuildActiveModifierLines shows 'Next action: ... COMBO'",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Clear on combo change
            character.Effects.AddPendingActionBonuses(0, new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "HIT", Value = 1 } });
            character.Effects.AddPendingActionBonusesNextHeroRoll(new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "ACCURACY", Value = 2 } });
            character.Effects.ClearPendingActionBonuses();
            TestBase.AssertTrue(!character.Effects.GetPendingActionBonusSlots().Any() && character.Effects.PeekPendingActionBonusesNextHeroRoll().Count == 0,
                "ClearPendingActionBonuses clears slot map and next-hero-roll queue",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            Console.WriteLine();
        }
    }
}
