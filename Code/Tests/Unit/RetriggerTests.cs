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
            TestItemCritRetriggerSlotTwoExecutesNested();
            TestNestedRetriggerUsesFreshRollNotOuterFace();

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
        /// Regression: item SlotTwoEncore (ONCRITICAL → retrigger_slot:2) must run a nested resolve of strip slot 2
        /// after the outer crit — not only emit "prepares a retrigger".
        /// </summary>
        private static void TestItemCritRetriggerSlotTwoExecutesNested()
        {
            TestBase.SetCurrentTestName(nameof(TestItemCritRetriggerSlotTwoExecutesNested));
            _ = GameConfiguration.Instance;
            CombatTriggerContext.ResetForBattle();
            RetriggerScheduler.AllowScheduling = true;

            var hero = new Character("EncoreHero", 20);
            var gear = new WeaponItem("EncoreMace", 1, 10, 1.0, WeaponType.Mace)
            {
                TriggerBundles = new List<ActionTriggerBundle>
                {
                    ItemTriggerIdentityCatalog.ToBundle(ItemTriggerIdentityCatalog.Get(41))
                }
            };
            hero.TryEquipItem(gear, "Weapon", out _, out _, ignoreAttributeRequirements: true);

            while (hero.GetComboActions().Count > 0)
                hero.RemoveFromCombo(hero.GetComboActions()[0], ignoreWeaponRequirement: true);

            var slot1 = new Action
            {
                Name = "PRE-FIGHT PLANNING",
                Type = ActionType.Attack,
                Target = TargetType.SingleTarget,
                IsComboAction = true,
                ComboOrder = 1,
                DamageMultiplier = 1.0,
                Length = 1.0
            };
            var slot2 = new Action
            {
                Name = "SLAM",
                Type = ActionType.Attack,
                Target = TargetType.SingleTarget,
                IsComboAction = true,
                ComboOrder = 2,
                DamageMultiplier = 1.0,
                Length = 1.0
            };
            hero.Actions.AddToCombo(slot1, maxComboLength: null);
            hero.Actions.AddToCombo(slot2, maxComboLength: null);
            hero.ComboStep = 0;

            var enemy = new Enemy("LichStandIn", 1, 500, 1, 1, 1, 1);
            int hpBefore = enemy.CurrentHealth;

            Dice.SetTestRoll(20);
            ActionSelector.SetStoredActionRoll(hero, 20);
            var lastUsed = new Dictionary<Actor, Action>();
            var lastCrit = new Dictionary<Actor, bool>();
            var result = ActionExecutionFlow.Execute(
                hero, enemy, null, null, slot1, null, lastUsed, lastCrit);
            Dice.SetTestRoll(null);
            ActionSelector.RemoveStoredRoll(hero);

            TestBase.AssertTrue(result.Hit && result.IsCritical,
                "outer swing is a critical hit", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(
                result.StatusEffectMessages.Any(m => m.Contains("prepares a retrigger", StringComparison.OrdinalIgnoreCase)
                    && m.Contains("slot 2", StringComparison.OrdinalIgnoreCase)),
                "prepare message for slot 2 was emitted", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(result.NestedRetriggerResults.Count == 1,
                "nested retrigger result was recorded", ref _run, ref _passed, ref _failed);
            var nested = result.NestedRetriggerResults[0];
            TestBase.AssertTrue(nested.SelectedAction != null && nested.SelectedAction.Name == "SLAM",
                "nested action is SLAM (slot 2)", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(enemy.CurrentHealth < hpBefore - result.Damage,
                "enemy took nested SLAM damage beyond outer swing", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(lastUsed.TryGetValue(hero, out var last) && last != null && last.Name == "PRE-FIGHT PLANNING",
                "last-used restored to outer swing (not encore)", ref _run, ref _passed, ref _failed);

            CombatTriggerContext.ResetForBattle();
            RetriggerScheduler.AllowScheduling = true;
        }

        /// <summary>
        /// Nested retrigger must roll a new d20; it must not reuse the outer swing's stored face.
        /// </summary>
        private static void TestNestedRetriggerUsesFreshRollNotOuterFace()
        {
            TestBase.SetCurrentTestName(nameof(TestNestedRetriggerUsesFreshRollNotOuterFace));
            _ = GameConfiguration.Instance;
            CombatTriggerContext.ResetForBattle();
            RetriggerScheduler.AllowScheduling = true;

            var hero = new Character("FreshRollHero", 20);
            var gear = new WeaponItem("EncoreSword", 1, 10, 1.0, WeaponType.Sword)
            {
                TriggerBundles = new List<ActionTriggerBundle>
                {
                    ItemTriggerIdentityCatalog.ToBundle(ItemTriggerIdentityCatalog.Get(41))
                }
            };
            hero.TryEquipItem(gear, "Weapon", out _, out _, ignoreAttributeRequirements: true);

            while (hero.GetComboActions().Count > 0)
                hero.RemoveFromCombo(hero.GetComboActions()[0], ignoreWeaponRequirement: true);

            var slot1 = new Action
            {
                Name = "Opener",
                Type = ActionType.Attack,
                Target = TargetType.SingleTarget,
                IsComboAction = true,
                DamageMultiplier = 1.0,
                Length = 1.0
            };
            var slot2 = new Action
            {
                Name = "Encore",
                Type = ActionType.Attack,
                Target = TargetType.SingleTarget,
                IsComboAction = true,
                DamageMultiplier = 1.0,
                Length = 1.0
            };
            hero.Actions.AddToCombo(slot1, maxComboLength: null);
            hero.Actions.AddToCombo(slot2, maxComboLength: null);

            var enemy = new Enemy("FreshRollFoe", 1, 500, 1, 1, 1, 1);

            // Outer face via stored roll; nested fresh Dice.Roll(1,20) consumes forced queue (7).
            Dice.ClearTestRoll();
            Dice.QueueAsyncForcedD20Rolls(7);
            ActionSelector.SetStoredActionRoll(hero, 20);
            var lastUsed = new Dictionary<Actor, Action>();
            var lastCrit = new Dictionary<Actor, bool>();
            var result = ActionExecutionFlow.Execute(
                hero, enemy, null, null, slot1, null, lastUsed, lastCrit);
            Dice.SetTestRoll(null);
            Dice.ClearAsyncForcedD20Rolls();
            ActionSelector.RemoveStoredRoll(hero);

            TestBase.AssertTrue(result.NestedRetriggerResults.Count == 1,
                "nested result present", ref _run, ref _passed, ref _failed);
            if (result.NestedRetriggerResults.Count == 1)
            {
                TestBase.AssertEqual(7, result.NestedRetriggerResults[0].BaseRoll,
                    "nested swing used fresh forced d20 (7), not outer 20", ref _run, ref _passed, ref _failed);
            }

            CombatTriggerContext.ResetForBattle();
            RetriggerScheduler.AllowScheduling = true;
        }
    }
}
