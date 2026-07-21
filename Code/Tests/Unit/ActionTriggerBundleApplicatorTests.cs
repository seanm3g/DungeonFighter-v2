using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Actions.Conditional;
using RPGGame.Combat.Events;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Combat applicator for TRIGGERS band triples (WHEN × SCOPE × mechanic pointers).
    /// </summary>
    public static class ActionTriggerBundleApplicatorTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Action Trigger Bundle Applicator Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;

            TestOnKill_FightDamageMod_DepositsToFightScope();
            TestOnKill_DoesNotFireOnHit();
            TestOwnedWeaken_SkippedByWholeRowGate_OnHit();
            TestBlankScope_InstantDamageMod_BanksActionCadence();
            TestGateOnlyListed_PoisonStillAppliesOnHitWhileWeakenIsOwned();

            TestBase.PrintSummary("Action Trigger Bundle Applicator Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static Action MakeKillFinisher()
        {
            return new Action
            {
                Name = "Finisher",
                Type = ActionType.Attack,
                CausesWeaken = true,
                CausesPoison = true,
                PoisonPercentToAdd = 1,
                DamageMod = "5",
                Advanced = new AdvancedMechanicsProperties(),
                Triggers = new ConditionalTriggerProperties
                {
                    Bundles = new List<ActionTriggerBundle>
                    {
                        new ActionTriggerBundle
                        {
                            When = "ONKILL",
                            Count = "1",
                            Scope = "FIGHT",
                            Mechanics = "hero_next_action_damage,weaken"
                        }
                    }
                }
            };
        }

        private static void TestOnKill_FightDamageMod_DepositsToFightScope()
        {
            TestBase.SetCurrentTestName(nameof(TestOnKill_FightDamageMod_DepositsToFightScope));
            var hero = new Character("BundleHero", 1);
            var foe = new Enemy("Goblin", 1, 100, 10, 5, 5, 5);
            var action = MakeKillFinisher();
            var messages = new List<string>();
            var kill = new CombatEvent(CombatEventType.EnemyDied, hero)
            {
                Target = foe,
                Action = action
            };

            bool applied = ActionTriggerBundleApplicator.ApplyMatchingBundles(action, kill, hero, foe, messages);
            TestBase.AssertTrue(applied, "bundle applied on kill", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(hero.FightCadenceBuffs.HasAny,
                "FIGHT scope deposits fight cadence buffs",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(foe.IsWeakened || messages.Count > 0,
                "weaken applies on kill",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestOnKill_DoesNotFireOnHit()
        {
            TestBase.SetCurrentTestName(nameof(TestOnKill_DoesNotFireOnHit));
            var hero = new Character("BundleHero2", 1);
            var foe = new Enemy("Goblin", 1, 100, 10, 5, 5, 5);
            var action = MakeKillFinisher();
            var messages = new List<string>();
            var hit = new CombatEvent(CombatEventType.ActionHit, hero)
            {
                Target = foe,
                Action = action
            };

            bool applied = ActionTriggerBundleApplicator.ApplyMatchingBundles(action, hit, hero, foe, messages);
            TestBase.AssertTrue(!applied, "ONKILL bundle skips ActionHit", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!hero.FightCadenceBuffs.HasAny,
                "no fight deposit on hit",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestOwnedWeaken_SkippedByWholeRowGate_OnHit()
        {
            TestBase.SetCurrentTestName(nameof(TestOwnedWeaken_SkippedByWholeRowGate_OnHit));
            var hero = new Character("BundleHero3", 1);
            var foe = new Enemy("Goblin", 1, 100, 10, 5, 5, 5);
            var action = MakeKillFinisher();
            // No whole-row ONKILL — poison keeps default-on-hit; weaken is owned by bundle.
            var results = new List<string>();
            var hit = new CombatEvent(CombatEventType.ActionHit, hero)
            {
                Target = foe,
                Action = action,
                IsCritical = true
            };

            _ = CombatEffectsSimplified.ApplyStatusEffects(action, hero, foe, results, hit);
            TestBase.AssertTrue(!foe.IsWeakened,
                "owned weaken does not apply via whole-row on hit",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestBlankScope_InstantDamageMod_BanksActionCadence()
        {
            TestBase.SetCurrentTestName(nameof(TestBlankScope_InstantDamageMod_BanksActionCadence));
            var hero = new Character("BundleHero4", 1);
            var foe = new Enemy("Goblin", 1, 100, 10, 5, 5, 5);
            var action = new Action
            {
                Name = "LuckyShot",
                DamageMod = "10",
                Advanced = new AdvancedMechanicsProperties(),
                Triggers = new ConditionalTriggerProperties
                {
                    Bundles = new List<ActionTriggerBundle>
                    {
                        new ActionTriggerBundle
                        {
                            When = "ONCRITICAL",
                            Count = "1",
                            Scope = "",
                            Mechanics = "hero_next_action_damage"
                        }
                    }
                }
            };
            var crit = new CombatEvent(CombatEventType.ActionHit, hero)
            {
                Target = foe,
                Action = action,
                IsCritical = true
            };
            var messages = new List<string>();
            bool applied = ActionTriggerBundleApplicator.ApplyMatchingBundles(action, crit, hero, foe, messages);
            TestBase.AssertTrue(applied, "instant crit bundle applied", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(hero.Effects.HasPendingActionCadenceBank(),
                "blank scope banks next-action damage",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGateOnlyListed_PoisonStillAppliesOnHitWhileWeakenIsOwned()
        {
            TestBase.SetCurrentTestName(nameof(TestGateOnlyListed_PoisonStillAppliesOnHitWhileWeakenIsOwned));
            var hero = new Character("BundleHero5", 1);
            var foe = new Enemy("Goblin", 1, 100, 10, 5, 5, 5);
            var action = MakeKillFinisher();
            var results = new List<string>();
            var hit = new CombatEvent(CombatEventType.ActionHit, hero)
            {
                Target = foe,
                Action = action,
                IsCritical = true
            };

            _ = CombatEffectsSimplified.ApplyStatusEffects(action, hero, foe, results, hit);
            TestBase.AssertTrue(foe.PoisonPercentOfMaxHealth > 0,
                "non-owned poison still applies on crit hit (gate_only_listed)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!foe.IsWeakened,
                "owned weaken still skipped on hit",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
