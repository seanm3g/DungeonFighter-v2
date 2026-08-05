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
            bool applied = ActionTriggerBundleApplicator.ApplyMatchingBundles(action, evt, hero, foe, messages);
            TestBase.AssertTrue(applied, "strip_jump bundle applied", ref _run, ref _passed, ref _failed);
            var state = CombatTriggerContext.GetStripState(hero);
            TestBase.AssertTrue(state != null && state.HasPendingRouting
                                && state.PendingRoutingAction == ComboRouter.RoutingAction.JumpToSlot
                                && state.PendingJumpToSlot1Based == 3,
                "pending jump to slot 3", ref _run, ref _passed, ref _failed);
        }
    }
}
