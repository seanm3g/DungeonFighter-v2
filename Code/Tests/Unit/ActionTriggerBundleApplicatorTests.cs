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
            TestSameSwingDamage_LandsBeforeDamage_AndPostPassSkipsIt();
            TestSameSwingDamage_RespectsTargetHealthFilter();
            TestTakeHit_StripActionBanksNextActionDamage();
            TestTakeHit_StripActionOnConnectDoesNotFireOnEnemyHit();
            TestWeaponSpeedFlat_ShortensTheSwingThatUsedIt();

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

        private static Action MakeSameSwingAction(string name, params string[] conditions)
        {
            return new Action
            {
                Name = name,
                Type = ActionType.Attack,
                Advanced = new AdvancedMechanicsProperties(),
                Triggers = new ConditionalTriggerProperties
                {
                    TriggerConditions = conditions.ToList(),
                    Bundles = new List<ActionTriggerBundle>
                    {
                        new ActionTriggerBundle { When = "ONCONNECT", Count = "1", Scope = "", Mechanics = "hero_action_damage", Value = 50 }
                    }
                }
            };
        }

        private static CombatEvent HitBy(Actor source, Actor target, Action action) =>
            new CombatEvent(CombatEventType.ActionHit, source) { Target = target, Action = action };

        private static void TestSameSwingDamage_LandsBeforeDamage_AndPostPassSkipsIt()
        {
            TestBase.SetCurrentTestName(nameof(TestSameSwingDamage_LandsBeforeDamage_AndPostPassSkipsIt));
            var hero = new Character("SameSwingHero", 1);
            var foe = new Enemy("Goblin", 1, 100, 10, 5, 5, 5);
            var action = MakeSameSwingAction("SAME SWING");
            var messages = new List<string>();

            bool pre = ActionTriggerBundleApplicator.ApplySameSwingBundles(action, HitBy(hero, foe, action), hero, foe, messages);
            TestBase.AssertTrue(pre, "pre-damage pass applies the action's own hero_action_damage",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(Math.Abs(hero.Effects.ConsumedDamageModPercent - 50) < 0.001,
                $"this swing carries +50% damage (got {hero.Effects.ConsumedDamageModPercent})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            ActionTriggerBundleApplicator.ApplyMatchingBundles(action, HitBy(hero, foe, action), hero, foe, messages, skipSameSwing: true);
            TestBase.AssertTrue(Math.Abs(hero.Effects.ConsumedDamageModPercent - 50) < 0.001,
                "post-hit pass does not apply it a second time",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSameSwingDamage_RespectsTargetHealthFilter()
        {
            TestBase.SetCurrentTestName(nameof(TestSameSwingDamage_RespectsTargetHealthFilter));
            var hero = new Character("ExecuteHero", 1);
            var foe = new Enemy("Goblin", 1, 100, 10, 5, 5, 5);
            var action = MakeSameSwingAction("EXECUTE", "IFTARGETHEALTHBELOW:25");
            var messages = new List<string>();

            ActionTriggerBundleApplicator.ApplySameSwingBundles(action, HitBy(hero, foe, action), hero, foe, messages);
            TestBase.AssertTrue(hero.Effects.ConsumedDamageModPercent == 0,
                "no bonus while the target is above 25% HP",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            foe.CurrentHealth = foe.MaxHealth / 5;
            ActionTriggerBundleApplicator.ApplySameSwingBundles(action, HitBy(hero, foe, action), hero, foe, messages);
            TestBase.AssertTrue(Math.Abs(hero.Effects.ConsumedDamageModPercent - 50) < 0.001,
                $"+50% once the target is at 20% HP (got {hero.Effects.ConsumedDamageModPercent})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static Character HeroWithStripAction(string heroName, Action action)
        {
            var hero = new Character(heroName, 1);
            action.IsComboAction = true;
            hero.AddAction(action, 1.0);
            hero.AddToCombo(action);
            return hero;
        }

        private static void TestTakeHit_StripActionBanksNextActionDamage()
        {
            TestBase.SetCurrentTestName(nameof(TestTakeHit_StripActionBanksNextActionDamage));
            var retaliate = new Action
            {
                Name = "RETALIATE",
                Type = ActionType.Attack,
                Advanced = new AdvancedMechanicsProperties(),
                Triggers = new ConditionalTriggerProperties
                {
                    Bundles = new List<ActionTriggerBundle>
                    {
                        new ActionTriggerBundle { When = "ONTAKEHIT", Count = "1", Scope = "ACTION", Mechanics = "hero_next_action_damage", Value = 25 }
                    }
                }
            };
            var hero = HeroWithStripAction("RetaliateHero", retaliate);
            var foe = new Enemy("Goblin", 1, 100, 10, 5, 5, 5);
            var bite = new Action { Name = "BITE", Type = ActionType.Attack, Advanced = new AdvancedMechanicsProperties() };
            var messages = new List<string>();

            bool fired = EquippedItemTriggerApplicator.ApplyFromDefender(hero, foe, HitBy(foe, hero, bite), messages);
            TestBase.AssertTrue(fired, "slotted ONTAKEHIT bundle fires when the hero is hit",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(hero.Effects.HasPendingActionCadenceBank(),
                "it banks next-action damage",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestTakeHit_StripActionOnConnectDoesNotFireOnEnemyHit()
        {
            TestBase.SetCurrentTestName(nameof(TestTakeHit_StripActionOnConnectDoesNotFireOnEnemyHit));
            var strike = new Action
            {
                Name = "SETUP STRIKE",
                Type = ActionType.Attack,
                Advanced = new AdvancedMechanicsProperties(),
                Triggers = new ConditionalTriggerProperties
                {
                    Bundles = new List<ActionTriggerBundle>
                    {
                        new ActionTriggerBundle { When = "ONCONNECT", Count = "1", Scope = "ACTION", Mechanics = "hero_next_action_damage", Value = 25 }
                    }
                }
            };
            var hero = HeroWithStripAction("ConnectHero", strike);
            var foe = new Enemy("Goblin", 1, 100, 10, 5, 5, 5);
            var bite = new Action { Name = "BITE", Type = ActionType.Attack, Advanced = new AdvancedMechanicsProperties() };

            EquippedItemTriggerApplicator.ApplyFromDefender(hero, foe, HitBy(foe, hero, bite), new List<string>());
            TestBase.AssertTrue(!hero.Effects.HasPendingActionCadenceBank(),
                "a slotted action's ONCONNECT bundle ignores the enemy's hit",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestWeaponSpeedFlat_ShortensTheSwingThatUsedIt()
        {
            TestBase.SetCurrentTestName(nameof(TestWeaponSpeedFlat_ShortensTheSwingThatUsedIt));
            var hero = new Character("WeaponSpeedHero", 1);
            var swing = new Action { Name = "SWING", Type = ActionType.Attack, Length = 1.0, Advanced = new AdvancedMechanicsProperties() };
            var speed = new ActionSpeedSystem();
            speed.AddEntity(hero, 1.0);

            double plain = speed.ExecuteAction(hero, swing);
            // The action flow clears consumed mods before the duration is set; WEAPON_SPEED rides over in LastSwingWeaponSpeedFlat.
            hero.Effects.LastSwingWeaponSpeedFlat = 5;
            double faster = speed.ExecuteAction(hero, swing);

            TestBase.AssertTrue(faster < plain,
                $"+5 weapon speed shortens the swing ({faster:F3}s vs {plain:F3}s)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(hero.Effects.LastSwingWeaponSpeedFlat == 0 && hero.Effects.ConsumedWeaponSpeedFlat == 0,
                "the carried points are used once and nothing stays behind",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
