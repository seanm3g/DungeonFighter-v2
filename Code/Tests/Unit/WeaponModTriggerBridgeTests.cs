using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Actions.Conditional;
using RPGGame.Combat.Events;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    public static class WeaponModTriggerBridgeTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Weapon Mod Trigger Bridge Tests ===\n");
            _run = _passed = _failed = 0;

            TestDefaultWhenIsOnCritical();
            TestCustomTriggerWhen();
            TestGateMatchesOnCritical();

            TestBase.PrintSummary("Weapon Mod Trigger Bridge Tests", _run, _passed, _failed);
        }

        private static void TestDefaultWhenIsOnCritical()
        {
            TestBase.SetCurrentTestName(nameof(TestDefaultWhenIsOnCritical));
            var mod = new Modification { Effect = "weaponPoison", RolledValue = 2 };
            string when = CombatEffectsSimplified.ResolveWeaponModTriggerWhen(mod);
            TestBase.AssertEqual("ONCRITICAL", when, "weaponPoison defaults to ONCRITICAL", ref _run, ref _passed, ref _failed);
        }

        private static void TestCustomTriggerWhen()
        {
            TestBase.SetCurrentTestName(nameof(TestCustomTriggerWhen));
            var mod = new Modification
            {
                Effect = "weaponBurn",
                TriggerWhen = "ONCONNECT",
                RolledValue = 3
            };
            string when = CombatEffectsSimplified.ResolveWeaponModTriggerWhen(mod);
            TestBase.AssertEqual("ONCONNECT", when, "explicit TriggerWhen wins", ref _run, ref _passed, ref _failed);
        }

        private static void TestGateMatchesOnCritical()
        {
            TestBase.SetCurrentTestName(nameof(TestGateMatchesOnCritical));
            var hero = new Character("BridgeHero", 5);
            var action = new Action { Name = "Swing" };
            var crit = new CombatEvent(CombatEventType.ActionHit, hero)
            {
                Action = action,
                IsCritical = true,
                RollValue = 20
            };
            var miss = new CombatEvent(CombatEventType.ActionMiss, hero)
            {
                Action = action,
                IsMiss = true,
                RollValue = 2
            };
            TestBase.AssertTrue(
                ActionTriggerGate.MatchesConditionToken("ONCRITICAL", action, crit),
                "crit event matches ONCRITICAL", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(
                !ActionTriggerGate.MatchesConditionToken("ONCRITICAL", action, miss),
                "miss does not match ONCRITICAL", ref _run, ref _passed, ref _failed);
        }
    }
}
