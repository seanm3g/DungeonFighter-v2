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
    public static class ActionBonusLoadedActionsTests
    {
        public static void RunAll(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            TestCadenceTypeFromActionsJson();
            TestAllLoadedActionsWithBonuses();

            testsRun += _testsRun;
            testsPassed += _testsPassed;
            testsFailed += _testsFailed;
        }

        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;



        private static void TestCadenceTypeFromActionsJson()
        {
            Console.WriteLine("\n--- CadenceType from Actions.json ---");
            Console.WriteLine("  Verify loaded actions with cadence have correct ACTION/ATTACK/ABILITY type.\n");

            ActionLoader.LoadActions();
            var allActions = ActionLoader.GetAllActions();
            var withBonuses = allActions.Where(a =>
                a.ActionAttackBonuses != null &&
                a.ActionAttackBonuses.BonusGroups != null &&
                a.ActionAttackBonuses.BonusGroups.Count > 0).ToList();

            int valid = 0;
            foreach (var action in withBonuses)
            {
                foreach (var group in action.ActionAttackBonuses!.BonusGroups)
                {
                    var ct = group.CadenceType ?? "";
                    if (ct == "ACTION" || ct == "TURN") valid++;
                }
            }

            TestBase.AssertTrue(withBonuses.Count == 0 || valid > 0,
                $"Loaded {withBonuses.Count} actions with bonuses; {valid} have valid CadenceType",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }


        /// <summary>
        /// For each loaded action with ACTION/ATTACK bonuses, verifies the modification is correctly
        /// applied to the next ACTION or ATTACK. Outputs each action name and what it modifies.
        /// </summary>
        private static void TestAllLoadedActionsWithBonuses()
        {
            Console.WriteLine("\n--- All Loaded Actions: Modifications to Next ACTION/ATTACK ---");
            Console.WriteLine("  Each action that buffs the next action or attack is tested.\n");

            ActionLoader.LoadActions();
            var allActions = ActionLoader.GetAllActions();
            var withBonuses = allActions
                .Where(a => a.ActionAttackBonuses != null &&
                            a.ActionAttackBonuses.BonusGroups != null &&
                            a.ActionAttackBonuses.BonusGroups.Count > 0)
                .ToList();

            int actionTested = 0;
            int actionPassed = 0;

            foreach (var action in withBonuses)
            {
                foreach (var group in action.ActionAttackBonuses!.BonusGroups)
                {
                    var ct = group.CadenceType ?? group.Keyword ?? "";
                    if (ct != "ACTION" && ct != "TURN") continue;

                    string desc = ActionAttackKeywordProcessor.GenerateKeywordString(
                        new ActionAttackBonuses { BonusGroups = new List<ActionAttackBonusGroup> { group } });
                    string modDesc = string.IsNullOrEmpty(desc) ? ActionBonusMechanicsTestHelpers.FormatBonusGroupShort(group) : desc;
                    Console.WriteLine($"  [{action.Name}] {ct}: {modDesc}");

                    var character = TestDataBuilders.Character().WithName("TestHero").Build();
                    character.Effects.ClearAllTempEffects();

                    if (ct == "ACTION")
                    {
                        if (group.Bonuses != null && group.Bonuses.Count > 0)
                        {
                            int stacks = group.Count > 0 ? group.Count : 1;
                            character.Effects.AccumulatePendingActionCadenceBank(group.Bonuses, stacks);
                            var pending = character.Effects.PeekPendingActionBonusesNextHeroRoll();
                            bool ok = pending.Count >= 1;
                            foreach (var b in group.Bonuses)
                                ok = ok && pending.Any(p => p.Type == b.Type && Math.Abs(p.Value - b.Value * stacks) < 0.01);
                            TestBase.AssertTrue(ok,
                                $"{action.Name} ACTION: bonuses stored additively for next hero roll",
                                ref _testsRun, ref _testsPassed, ref _testsFailed);
                            if (ok) actionPassed++;
                        }
                    }
                    else // ATTACK
                    {
                        character.Effects.AddActionAttackBonuses(new ActionAttackBonuses { BonusGroups = new List<ActionAttackBonusGroup> { group } });
                        var peeked = character.Effects.PeekTurnBonuses();
                        bool ok = peeked.Count >= (group.Bonuses?.Count ?? 0);
                        if (group.Bonuses != null)
                            foreach (var b in group.Bonuses)
                                ok = ok && peeked.Any(p => p.Type == b.Type && Math.Abs(p.Value - b.Value) < 0.01);
                        TestBase.AssertTrue(ok,
                            $"{action.Name} ATTACK: bonuses queued for next roll",
                            ref _testsRun, ref _testsPassed, ref _testsFailed);
                        if (ok) actionPassed++;
                    }
                    actionTested++;
                }
            }

            Console.WriteLine($"\n  Tested {actionTested} action modification(s).\n");
        }
    }
}
