using System.Collections.Generic;
using RPGGame;
using RPGGame.Combat;
using RPGGame.Data;

namespace RPGGame.Tests.Unit.Combat
{
    public static class ActionMechanicTagProcessorTests
    {
        private static int _run;
        private static int _pass;
        private static int _fail;

        public static void RunAll(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            _run = _pass = _fail = 0;
            TestSwiftQueuesSpeedMod(ref _run, ref _pass, ref _fail);
            TestBludgeonQueuesDamageMod(ref _run, ref _pass, ref _fail);
            TestBludgeonTagSkippedWhenSheetDamageModPresent(ref _run, ref _pass, ref _fail);
            testsRun += _run;
            testsPassed += _pass;
            testsFailed += _fail;
        }

        private static void TestSwiftQueuesSpeedMod(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(TestSwiftQueuesSpeedMod));
            var hero = new Character("Hero", 1);
            var action = new Action("Quick Step") { Tags = new List<string> { "swift" } };
            ActionMechanicTagProcessor.QueueNextActionBonuses(hero, action);
            var pending = hero.Effects.PeekPendingActionBonusesNextHeroRoll();
            TestBase.AssertTrue(pending.Count > 0 && pending[0].Type == "SPEED_MOD",
                "swift queues SPEED_MOD", ref run, ref pass, ref fail);
        }

        private static void TestBludgeonQueuesDamageMod(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(TestBludgeonQueuesDamageMod));
            var hero = new Character("Hero", 1);
            var action = new Action("Heavy Follow") { Tags = new List<string> { "bludgeon" } };
            ActionMechanicTagProcessor.QueueNextActionBonuses(hero, action);
            var pending = hero.Effects.PeekPendingActionBonusesNextHeroRoll();
            TestBase.AssertTrue(pending.Count > 0 && pending[0].Type == "DAMAGE_MOD",
                "bludgeon queues DAMAGE_MOD", ref run, ref pass, ref fail);
        }

        private static void TestBludgeonTagSkippedWhenSheetDamageModPresent(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(TestBludgeonTagSkippedWhenSheetDamageModPresent));
            var hero = new Character("Hero", 1);
            var action = new Action("ACTION BONUS")
            {
                Tags = new List<string> { "bludgeon" },
                DamageMod = "25",
                Cadence = "Action"
            };
            ActionMechanicTagProcessor.QueueNextActionBonuses(hero, action);
            TestBase.AssertEqual(0, hero.Effects.GetPendingActionCadenceLayerCount(),
                "bludgeon tag must not duplicate sheet damageMod FIFO layer",
                ref run, ref pass, ref fail);
        }
    }
}
