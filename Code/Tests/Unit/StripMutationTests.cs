using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Actions.Conditional;
using RPGGame.Combat.Events;
using RPGGame.Data;
using RPGGame.Entity.Actions.ComboRouting;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Fight-scoped strip mutations and ComboRouter disable/override behavior.
    /// </summary>
    public static class StripMutationTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Strip Mutation Tests ===\n");
            _run = _passed = _failed = 0;
            CombatTriggerContext.ResetForBattle();

            TestDisableSkipsSlot();
            TestPendingJumpOverride();
            TestReplaceNextConsumesOnce();
            TestStripJumpFromBundle();
            TestStripJumpSwingLandsOnSlot3();
            TestStripRandomSingleSlotDoesNotThrow();
            TestStripRandomOneEnabledSlotDoesNotThrow();

            CombatTriggerContext.ResetForBattle();
            TestBase.PrintSummary("Strip Mutation Tests", _run, _passed, _failed);
        }

        private static Character MakeHeroWithStrip(params string[] names)
        {
            var hero = new Character("StripHero", 10);
            hero.Stats.Intelligence = 20; // ordered combo routing
            while (hero.GetComboActions().Count > 0)
                hero.RemoveFromCombo(hero.GetComboActions()[0], ignoreWeaponRequirement: true);
            for (int i = 0; i < names.Length; i++)
            {
                var a = new Action
                {
                    Name = names[i],
                    Type = ActionType.Attack,
                    IsComboAction = true,
                    ComboOrder = i + 1,
                    ComboRouting = new ComboRoutingProperties()
                };
                // Bypass effective-max cap used by CharacterFacade.AddToCombo
                hero.Actions.AddToCombo(a, maxComboLength: null);
            }
            hero.ComboStep = 0;
            return hero;
        }

        private static void TestDisableSkipsSlot()
        {
            TestBase.SetCurrentTestName(nameof(TestDisableSkipsSlot));
            CombatTriggerContext.ResetForBattle();
            var hero = MakeHeroWithStrip("A", "B", "C");
            var state = CombatTriggerContext.GetOrCreateStripState(hero);
            state.DisableSlot(1); // disable B

            var result = ComboRouter.RouteCombo(hero, hero.GetComboActions()[0], 0, hero.GetComboActions());
            TestBase.AssertEqual(2, result.NextSlotIndex, "should skip disabled slot 1 → land on 2", ref _run, ref _passed, ref _failed);
        }

        private static void TestPendingJumpOverride()
        {
            TestBase.SetCurrentTestName(nameof(TestPendingJumpOverride));
            CombatTriggerContext.ResetForBattle();
            var hero = MakeHeroWithStrip("A", "B", "C", "D");
            var state = CombatTriggerContext.GetOrCreateStripState(hero);
            state.SetPendingRouting(ComboRouter.RoutingAction.JumpToSlot, 4);

            var result = ComboRouter.RouteCombo(hero, hero.GetComboActions()[0], 0, hero.GetComboActions());
            TestBase.AssertEqual(3, result.NextSlotIndex, "pending jump to slot 4 → index 3", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!state.HasPendingRouting, "pending routing consumed", ref _run, ref _passed, ref _failed);
        }

        private static void TestReplaceNextConsumesOnce()
        {
            TestBase.SetCurrentTestName(nameof(TestReplaceNextConsumesOnce));
            CombatTriggerContext.ResetForBattle();
            var hero = MakeHeroWithStrip("Slash", "Bash", "Crash");
            var state = CombatTriggerContext.GetOrCreateStripState(hero);
            state.ReplaceNextActionName = "Crash";

            bool ok = StripMutationApplier.TryConsumeReplaceNext(hero, out var act);
            TestBase.AssertTrue(ok && act != null && act.Name == "Crash", "replace next yields Crash", ref _run, ref _passed, ref _failed);
            bool ok2 = StripMutationApplier.TryConsumeReplaceNext(hero, out _);
            TestBase.AssertTrue(!ok2, "replace next is one-shot", ref _run, ref _passed, ref _failed);
        }

        private static void TestStripJumpFromBundle()
        {
            TestBase.SetCurrentTestName(nameof(TestStripJumpFromBundle));
            CombatTriggerContext.ResetForBattle();
            var hero = MakeHeroWithStrip("A", "B", "C");
            var foe = new Enemy("Dummy", 1, 50, 5, 5, 5, 5);
            var action = new Action
            {
                Name = "Jumper",
                Type = ActionType.Attack,
                Triggers = new ConditionalTriggerProperties
                {
                    Bundles = new List<ActionTriggerBundle>
                    {
                        new ActionTriggerBundle
                        {
                            When = "ONCOMBO",
                            Count = "3",
                            Scope = "",
                            Mechanics = "strip_jump"
                        }
                    }
                }
            };
            var messages = new List<string>();
            var evt = new CombatEvent(CombatEventType.ActionHit, hero)
            {
                Target = foe,
                Action = action,
                IsCombo = true,
                RollValue = 15
            };
            bool applied = RPGGame.Combat.Rules.ActionRules.Dispatch(action, evt, hero, foe, messages);
            TestBase.AssertTrue(applied, "strip_jump bundle applied", ref _run, ref _passed, ref _failed);
            var state = CombatTriggerContext.GetStripState(hero);
            TestBase.AssertTrue(state != null && state.HasPendingRouting
                                && state.PendingRoutingAction == ComboRouter.RoutingAction.JumpToSlot
                                && state.PendingJumpToSlot1Based == 3,
                "pending jump to slot 3", ref _run, ref _passed, ref _failed);
        }

        /// <summary>
        /// Playtest 11-21: STRIP JUMP (slot 1) connecting as a combo must move the strip straight to slot 3, so the card the
        /// board highlights next (ComboStep) is the one the hero actually swings, not slot 2.
        /// </summary>
        private static void TestStripJumpSwingLandsOnSlot3()
        {
            TestBase.SetCurrentTestName(nameof(TestStripJumpSwingLandsOnSlot3));
            CombatTriggerContext.ResetForBattle();
            var jump = ActionLoader.GetAction("STRIP JUMP");
            if (jump == null)
            {
                TestBase.AssertTrue(false, "STRIP JUMP loads", ref _run, ref _passed, ref _failed);
                return;
            }
            var hero = MakeHeroWithStrip();
            jump.IsComboAction = true;
            jump.ComboOrder = 1;
            hero.Actions.AddToCombo(jump, maxComboLength: null);
            foreach (var (name, order) in new[] { ("B", 2), ("C", 3), ("D", 4) })
                hero.Actions.AddToCombo(new Action
                {
                    Name = name, Type = ActionType.Attack, IsComboAction = true, ComboOrder = order,
                    ComboRouting = new ComboRoutingProperties()
                }, maxComboLength: null);
            var combo = hero.GetComboActions();
            int jumpSlot = combo.FindIndex(a => a.Name == "STRIP JUMP");
            hero.ComboStep = jumpSlot;
            var foe = new Enemy("StripFoe", 1, 5000, 0, 0, 0, 0);
            var lastUsed = new Dictionary<Actor, Action>();
            var lastCritMiss = new Dictionary<Actor, bool>();
            try
            {
                Dice.SetTestRoll(19);
                ActionSelector.SetStoredActionRoll(hero, 19);
                var r = RPGGame.Actions.Execution.ActionExecutionFlow.Execute(hero, foe, null, null, combo[jumpSlot], null, lastUsed, lastCritMiss);
                TestBase.AssertTrue(jumpSlot == 0 && r.Hit && r.IsCombo, $"STRIP JUMP (slot {jumpSlot + 1}) hit+combo", ref _run, ref _passed, ref _failed);
                int step = hero.ComboStep % combo.Count;
                TestBase.AssertEqual(2, step, $"After STRIP JUMP the strip is on slot 3 (step {step})", ref _run, ref _passed, ref _failed);
                var state = CombatTriggerContext.GetStripState(hero);
                TestBase.AssertTrue(state == null || !state.HasPendingRouting, "No jump left pending for a later swing", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(state != null && state.WasSkippedThisRun(1) && !state.WasSkippedThisRun(0) && !state.WasSkippedThisRun(2),
                    "Slot 2 is marked skipped (not drawn as a lit link)", ref _run, ref _passed, ref _failed);
                state!.RecordAdvance(2, 3);
                TestBase.AssertTrue(state.WasSkippedThisRun(1), "Skip mark holds while the run continues", ref _run, ref _passed, ref _failed);
                state.RecordAdvance(3, 0);
                TestBase.AssertTrue(!state.WasSkippedThisRun(1), "A wrap starts a clean run", ref _run, ref _passed, ref _failed);
            }
            finally
            {
                Dice.ClearTestRoll();
                ActionSelector.ClearStoredRolls();
            }
        }

        /// <summary>
        /// Regression: strip_random on a 1-slot strip used to call Dice.Roll(1, 1) and crash the dungeon.
        /// </summary>
        private static void TestStripRandomSingleSlotDoesNotThrow()
        {
            TestBase.SetCurrentTestName(nameof(TestStripRandomSingleSlotDoesNotThrow));
            CombatTriggerContext.ResetForBattle();
            var hero = MakeHeroWithStrip("Solo");
            var state = CombatTriggerContext.GetOrCreateStripState(hero);
            state.SetPendingRouting(ComboRouter.RoutingAction.RandomAction);

            RoutingResult? result = null;
            Exception? thrown = null;
            try
            {
                result = ComboRouter.RouteCombo(hero, hero.GetComboActions()[0], 0, hero.GetComboActions());
            }
            catch (Exception ex)
            {
                thrown = ex;
            }

            TestBase.AssertTrue(thrown == null, "1-slot strip_random must not throw", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0, result?.NextSlotIndex ?? -1, "1-slot random lands on slot 0", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(result?.RoutingAction == ComboRouter.RoutingAction.RandomAction,
                "routing action is RandomAction", ref _run, ref _passed, ref _failed);
        }

        /// <summary>
        /// Regression: when all but one slots are disabled, random pick must not roll a 1-sided die.
        /// </summary>
        private static void TestStripRandomOneEnabledSlotDoesNotThrow()
        {
            TestBase.SetCurrentTestName(nameof(TestStripRandomOneEnabledSlotDoesNotThrow));
            CombatTriggerContext.ResetForBattle();
            var hero = MakeHeroWithStrip("A", "B", "C");
            var state = CombatTriggerContext.GetOrCreateStripState(hero);
            state.DisableSlot(0);
            state.DisableSlot(2);
            state.SetPendingRouting(ComboRouter.RoutingAction.RandomAction);

            RoutingResult? result = null;
            Exception? thrown = null;
            try
            {
                result = ComboRouter.RouteCombo(hero, hero.GetComboActions()[0], 0, hero.GetComboActions());
            }
            catch (Exception ex)
            {
                thrown = ex;
            }

            TestBase.AssertTrue(thrown == null, "single enabled-slot random must not throw", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(1, result?.NextSlotIndex ?? -1, "random with only slot 1 enabled → 1", ref _run, ref _passed, ref _failed);
        }
    }
}
