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
            // Step 5: these tests cover the retired statuses' code, which stays until step 12 (CoreStatuses).
            Console.WriteLine("=== Action Trigger Bundle Applicator Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;

            TestOnKill_FightDamageMod_DepositsToFightScope();
            TestOnKill_DoesNotFireOnHit();
            TestBlankScope_InstantDamageMod_BanksActionCadence();
            TestSameSwingDamage_LandsBeforeDamage_AndPostPassSkipsIt();
            TestSameSwingDamage_RespectsTargetHealthFilter();
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
                            Mechanics = "crit_face_min:18,weaken"
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

            bool applied = RPGGame.Combat.Rules.ActionRules.Dispatch(action, kill, hero, foe, messages);
            TestBase.AssertTrue(applied, "bundle applied on kill", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(hero.FightCadenceBuffs.HasAny,
                "FIGHT scope deposits fight cadence buffs",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(Stores.Get(foe, "rattle") > 0 || messages.Count > 0,
                "weaken (now Rattle) applies on kill",
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

            bool applied = RPGGame.Combat.Rules.ActionRules.Dispatch(action, hit, hero, foe, messages);
            TestBase.AssertTrue(!applied, "ONKILL bundle skips ActionHit", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!hero.FightCadenceBuffs.HasAny,
                "no fight deposit on hit",
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
                Advanced = new AdvancedMechanicsProperties(),
                Triggers = new ConditionalTriggerProperties
                {
                    Bundles = new List<ActionTriggerBundle>
                    {
                        new ActionTriggerBundle { When = "ONCRITICAL", Count = "1", Scope = "", Mechanics = "replace_next_roll:17" }
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
            bool applied = RPGGame.Combat.Rules.ActionRules.Dispatch(action, crit, hero, foe, messages);
            TestBase.AssertTrue(applied, "instant crit bundle applied", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(CombatTriggerContext.TryConsumePendingReplaceRollFace(hero, out int face) && face == 17,
                "the card's NEXTROLL rule forces the next roll", ref _testsRun, ref _testsPassed, ref _testsFailed);
            var plain = new CombatEvent(CombatEventType.ActionHit, hero) { Target = foe, Action = action };
            TestBase.AssertTrue(!RPGGame.Combat.Rules.ActionRules.Dispatch(action, plain, hero, foe, messages),
                "an ONCRITICAL card rule skips a plain hit", ref _testsRun, ref _testsPassed, ref _testsFailed);
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

            bool pre = RPGGame.Combat.Rules.ActionRules.Dispatch(action, HitBy(hero, foe, action), hero, foe, messages, swingOnly: true);
            TestBase.AssertTrue(pre, "pre-damage pass applies the action's own hero_action_damage",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(Math.Abs(hero.Effects.ConsumedDamageModPercent - 50) < 0.001,
                $"this swing carries +50% damage (got {hero.Effects.ConsumedDamageModPercent})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            RPGGame.Combat.Rules.ActionRules.Dispatch(action, HitBy(hero, foe, action), hero, foe, messages);
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

            RPGGame.Combat.Rules.ActionRules.Dispatch(action, HitBy(hero, foe, action), hero, foe, messages, swingOnly: true);
            TestBase.AssertTrue(hero.Effects.ConsumedDamageModPercent == 0,
                "no bonus while the target is above 25% HP",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            foe.CurrentHealth = foe.MaxHealth / 5;
            RPGGame.Combat.Rules.ActionRules.Dispatch(action, HitBy(hero, foe, action), hero, foe, messages, swingOnly: true);
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
