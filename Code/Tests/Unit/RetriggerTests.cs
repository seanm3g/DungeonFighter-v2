using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Actions.Conditional;
using RPGGame.Actions.Execution;
using RPGGame.Data;
using RPGGame.Tests;
using RPGGame.Utils;

namespace RPGGame.Tests.Unit
{
    public static class RetriggerTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Retrigger Tests ===\n");
            _run = _passed = _failed = 0;
            CombatTriggerContext.ResetForBattle();

            TestScheduleOpener();
            TestNoRescheduleWhilePending();
            TestAllowSchedulingGate();
            TestScheduleSlotTwo();
            TestEchoSpellRetriggersNextSlotThenAdvancesPastIt();

            CombatTriggerContext.ResetForBattle();
            RetriggerScheduler.AllowScheduling = true;
            TestBase.PrintSummary("Retrigger Tests", _run, _passed, _failed);
        }

        private static Character MakeHero()
        {
            var hero = new Character("RetrigHero", 10);
            while (hero.GetComboActions().Count > 0)
                hero.RemoveFromCombo(hero.GetComboActions()[0], ignoreWeaponRequirement: true);
            var opener = new Action
            {
                Name = "Opener",
                Type = ActionType.Attack,
                IsComboAction = true,
                ComboOrder = 1,
                ComboRouting = new ComboRoutingProperties { IsOpener = true }
            };
            var mid = new Action
            {
                Name = "Mid",
                Type = ActionType.Attack,
                IsComboAction = true,
                ComboOrder = 2
            };
            var finisher = new Action
            {
                Name = "Finisher",
                Type = ActionType.Attack,
                IsComboAction = true,
                ComboOrder = 3,
                ComboRouting = new ComboRoutingProperties { IsFinisher = true }
            };
            hero.Actions.AddToCombo(opener, maxComboLength: null);
            hero.Actions.AddToCombo(mid, maxComboLength: null);
            hero.Actions.AddToCombo(finisher, maxComboLength: null);
            hero.ComboStep = 1;
            return hero;
        }

        private static void TestScheduleOpener()
        {
            TestBase.SetCurrentTestName(nameof(TestScheduleOpener));
            CombatTriggerContext.ResetForBattle();
            RetriggerScheduler.AllowScheduling = true;
            var hero = MakeHero();
            var messages = new List<string>();
            var mid = hero.GetComboActions()[1];
            bool ok = RetriggerScheduler.TrySchedule("retrigger_opener", null, "1", mid, hero, messages);
            TestBase.AssertTrue(ok, "scheduled opener retrigger", ref _run, ref _passed, ref _failed);
            bool consumed = RetriggerScheduler.TryConsume(hero, out var forced);
            TestBase.AssertTrue(consumed && forced != null && forced.Name == "Opener",
                "consume yields opener", ref _run, ref _passed, ref _failed);
        }

        private static void TestScheduleSlotTwo()
        {
            TestBase.SetCurrentTestName(nameof(TestScheduleSlotTwo));
            CombatTriggerContext.ResetForBattle();
            RetriggerScheduler.AllowScheduling = true;
            var hero = MakeHero();
            var messages = new List<string>();
            bool ok = RetriggerScheduler.TrySchedule("retrigger_slot", "2", "1", hero.GetComboActions()[0], hero, messages);
            TestBase.AssertTrue(ok, "scheduled slot 2", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(messages.Any(m => m.Contains("slot 2", StringComparison.OrdinalIgnoreCase)),
                "prepare message names slot 2", ref _run, ref _passed, ref _failed);
            bool consumed = RetriggerScheduler.TryConsume(hero, out var forced);
            TestBase.AssertTrue(consumed && forced != null && forced.Name == "Mid",
                "consume yields strip slot 2 (Mid)", ref _run, ref _passed, ref _failed);
        }

        private static void TestNoRescheduleWhilePending()
        {
            TestBase.SetCurrentTestName(nameof(TestNoRescheduleWhilePending));
            CombatTriggerContext.ResetForBattle();
            RetriggerScheduler.AllowScheduling = true;
            var hero = MakeHero();
            var messages = new List<string>();
            var first = hero.GetComboActions()[0];
            RetriggerScheduler.TrySchedule("retrigger_opener", null, "1", first, hero, messages);
            bool second = RetriggerScheduler.TrySchedule("retrigger_finisher", null, "1", first, hero, messages);
            TestBase.AssertTrue(!second, "second schedule ignored while pending", ref _run, ref _passed, ref _failed);
            RetriggerScheduler.TryConsume(hero, out var forced);
            TestBase.AssertTrue(forced != null && forced.Name == "Opener", "first schedule wins", ref _run, ref _passed, ref _failed);
        }

        private static void TestAllowSchedulingGate()
        {
            TestBase.SetCurrentTestName(nameof(TestAllowSchedulingGate));
            CombatTriggerContext.ResetForBattle();
            RetriggerScheduler.AllowScheduling = false;
            var hero = MakeHero();
            var messages = new List<string>();
            bool ok = RetriggerScheduler.TrySchedule("retrigger_next", null, "1", hero.GetComboActions()[0], hero, messages);
            TestBase.AssertTrue(!ok, "scheduling blocked during nested retrigger", ref _run, ref _passed, ref _failed);
            RetriggerScheduler.AllowScheduling = true;
        }

        /// <summary>
        /// ECHO SPELL (slot 1) on combo plays itself and the next strip slot, then the sequence
        /// lands on the slot after that encore. The index must not wrap back onto Echo Spell.
        /// </summary>
        private static void TestEchoSpellRetriggersNextSlotThenAdvancesPastIt()
        {
            TestBase.SetCurrentTestName(nameof(TestEchoSpellRetriggersNextSlotThenAdvancesPastIt));
            _ = GameConfiguration.Instance;
            CombatTriggerContext.ResetForBattle();
            RetriggerScheduler.AllowScheduling = true;

            var hero = new Character("EchoHero", 20);
            while (hero.GetComboActions().Count > 0)
                hero.RemoveFromCombo(hero.GetComboActions()[0], ignoreWeaponRequirement: true);

            var echo = new Action
            {
                Name = "ECHO SPELL",
                Type = ActionType.Attack,
                Target = TargetType.SingleTarget,
                IsComboAction = true,
                ComboOrder = 1,
                DamageMultiplier = 1.0,
                Length = 1.0,
                Triggers = new ConditionalTriggerProperties
                {
                    Bundles = new List<ActionTriggerBundle>
                    {
                        new ActionTriggerBundle
                        {
                            When = "ONCOMBO",
                            Count = "1",
                            Scope = "",
                            Mechanics = "retrigger_next"
                        }
                    }
                }
            };
            var slam = new Action
            {
                Name = "SLAM",
                Type = ActionType.Attack,
                Target = TargetType.SingleTarget,
                IsComboAction = true,
                ComboOrder = 2,
                DamageMultiplier = 1.0,
                Length = 1.0
            };
            var third = new Action
            {
                Name = "THIRD",
                Type = ActionType.Attack,
                Target = TargetType.SingleTarget,
                IsComboAction = true,
                ComboOrder = 3,
                DamageMultiplier = 1.0,
                Length = 1.0
            };
            hero.Actions.AddToCombo(echo, maxComboLength: null);
            hero.Actions.AddToCombo(slam, maxComboLength: null);
            hero.Actions.AddToCombo(third, maxComboLength: null);
            hero.ComboStep = 0;

            var enemy = new Enemy("EchoFoe", 1, 500, 1, 1, 1, 1);
            Dice.SetTestRoll(20);
            ActionSelector.SetStoredActionRoll(hero, 20);
            var lastUsed = new Dictionary<Actor, Action>();
            var lastCrit = new Dictionary<Actor, bool>();
            var result = ActionExecutionFlow.Execute(
                hero, enemy, null, null, echo, null, lastUsed, lastCrit);
            Dice.SetTestRoll(null);
            ActionSelector.RemoveStoredRoll(hero);

            TestBase.AssertTrue(result.Hit && result.IsCombo,
                "echo spell combo-hit", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(result.SelectedAction != null && result.SelectedAction.Name == "ECHO SPELL",
                "outer swing is Echo Spell", ref _run, ref _passed, ref _failed);
            var encore = result.NestedRetriggerResults.Count == 1
                ? result.NestedRetriggerResults[0].SelectedAction
                : null;
            TestBase.AssertTrue(encore != null && encore.Name == "SLAM",
                "encore is the next strip slot (SLAM), not Echo Spell again", ref _run, ref _passed, ref _failed);
            int count = hero.GetComboActions().Count;
            int slot = count > 0 ? hero.ComboStep % count : -1;
            TestBase.AssertEqual(2, slot,
                "after Echo Spell + SLAM the sequence is on slot 3", ref _run, ref _passed, ref _failed);

            CombatTriggerContext.ResetForBattle();
            RetriggerScheduler.AllowScheduling = true;
        }
    }
}
