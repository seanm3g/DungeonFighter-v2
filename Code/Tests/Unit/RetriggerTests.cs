using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Actions.Conditional;
using RPGGame.Data;
using RPGGame.Tests;

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

            CombatTriggerContext.ResetForBattle();
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
    }
}
