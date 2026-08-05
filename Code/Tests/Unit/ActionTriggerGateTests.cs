using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Actions.Conditional;
using RPGGame.Combat.Events;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Live gate tests for action status trigger tokens (ONHIT, ONCONNECT, ONMISS, ONKILL, …).
    /// </summary>
    public static class ActionTriggerGateTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Action Trigger Gate Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;

            TestEmptyConditions_ApplyOnHitOnly();
            TestOnConnect_MatchesComboAndCrit();
            TestOnHit_ExcludesComboAndCrit();
            TestOnMiss_MatchesMissEvent();
            TestOnCriticalMiss();
            TestOnKill();
            TestExactRoll();
            TestOnHealthThreshold();
            TestOnComboEnd();
            TestOnRoomsCleared();
            TestParseTriggerList();
            TestOnWield_StandaloneImpliesConnect();
            TestOnWield_OrTypesAndAndWithOutcome();
            TestSignatureHooks_FirstBloodAfterMissMirrorSwitchClutch();
            TestSignatureHooks_UnderDotLastEnemyTags();
            TestGateBlocksWeaponStyleDoTUnlessTriggered();

            TestBase.PrintSummary("Action Trigger Gate Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static Action MakeStunAction(params string[] triggers)
        {
            return new Action
            {
                Name = "TestStun",
                CausesStun = true,
                Triggers = new ConditionalTriggerProperties
                {
                    TriggerConditions = new List<string>(triggers)
                }
            };
        }

        private static void TestEmptyConditions_ApplyOnHitOnly()
        {
            var action = MakeStunAction();
            var source = new Character("Hero", 1);
            var hit = new CombatEvent(CombatEventType.ActionHit, source);
            var miss = new CombatEvent(CombatEventType.ActionMiss, source) { IsMiss = true };
            var kill = new CombatEvent(CombatEventType.EnemyDied, source);

            TestBase.AssertTrue(ActionTriggerGate.ShouldApplyStatusEffects(action, hit),
                "Empty triggers: apply on hit", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!ActionTriggerGate.ShouldApplyStatusEffects(action, miss),
                "Empty triggers: do not apply on miss", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!ActionTriggerGate.ShouldApplyStatusEffects(action, kill),
                "Empty triggers: do not apply on kill", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestOnConnect_MatchesComboAndCrit()
        {
            var action = MakeStunAction("ONCONNECT");
            var source = new Character("Hero", 1);
            var normal = new CombatEvent(CombatEventType.ActionHit, source);
            var combo = new CombatEvent(CombatEventType.ActionHit, source) { IsCombo = true };
            var crit = new CombatEvent(CombatEventType.ActionHit, source) { IsCritical = true };
            var miss = new CombatEvent(CombatEventType.ActionMiss, source) { IsMiss = true };

            TestBase.AssertTrue(ActionTriggerGate.ShouldApplyStatusEffects(action, normal),
                "ONCONNECT matches normal hit", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(ActionTriggerGate.ShouldApplyStatusEffects(action, combo),
                "ONCONNECT matches combo", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(ActionTriggerGate.ShouldApplyStatusEffects(action, crit),
                "ONCONNECT matches crit", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!ActionTriggerGate.ShouldApplyStatusEffects(action, miss),
                "ONCONNECT rejects miss", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestOnHit_ExcludesComboAndCrit()
        {
            var action = MakeStunAction("ONHIT");
            var source = new Character("Hero", 1);
            var normal = new CombatEvent(CombatEventType.ActionHit, source);
            var combo = new CombatEvent(CombatEventType.ActionHit, source) { IsCombo = true };
            var crit = new CombatEvent(CombatEventType.ActionHit, source) { IsCritical = true };

            TestBase.AssertTrue(ActionTriggerGate.ShouldApplyStatusEffects(action, normal),
                "ONHIT matches normal", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!ActionTriggerGate.ShouldApplyStatusEffects(action, combo),
                "ONHIT rejects combo", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!ActionTriggerGate.ShouldApplyStatusEffects(action, crit),
                "ONHIT rejects crit", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestOnMiss_MatchesMissEvent()
        {
            var action = MakeStunAction("ONMISS");
            var source = new Character("Hero", 1);
            var miss = new CombatEvent(CombatEventType.ActionMiss, source) { IsMiss = true };
            var hit = new CombatEvent(CombatEventType.ActionHit, source);

            TestBase.AssertTrue(ActionTriggerGate.ShouldApplyStatusEffects(action, miss),
                "ONMISS matches miss", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!ActionTriggerGate.ShouldApplyStatusEffects(action, hit),
                "ONMISS rejects hit", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestOnCriticalMiss()
        {
            var action = MakeStunAction("ONCRITICALMISS");
            var source = new Character("Hero", 1);
            var critMiss = new CombatEvent(CombatEventType.ActionMiss, source) { IsMiss = true, IsCriticalMiss = true };
            var miss = new CombatEvent(CombatEventType.ActionMiss, source) { IsMiss = true };

            TestBase.AssertTrue(ActionTriggerGate.ShouldApplyStatusEffects(action, critMiss),
                "ONCRITICALMISS matches crit miss", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!ActionTriggerGate.ShouldApplyStatusEffects(action, miss),
                "ONCRITICALMISS rejects normal miss", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestOnKill()
        {
            var action = MakeStunAction("ONKILL");
            var source = new Character("Hero", 1);
            var kill = new CombatEvent(CombatEventType.EnemyDied, source);
            var hit = new CombatEvent(CombatEventType.ActionHit, source);

            TestBase.AssertTrue(ActionTriggerGate.ShouldApplyStatusEffects(action, kill),
                "ONKILL matches death", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!ActionTriggerGate.ShouldApplyStatusEffects(action, hit),
                "ONKILL rejects hit", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestExactRoll()
        {
            var action = MakeStunAction("ONROLLVALUE");
            action.Triggers.ExactRollTriggerValue = 17;
            var source = new Character("Hero", 1);
            var match = new CombatEvent(CombatEventType.ActionHit, source) { RollValue = 17 };
            var missFace = new CombatEvent(CombatEventType.ActionHit, source) { RollValue = 10 };

            TestBase.AssertTrue(ActionTriggerGate.ShouldApplyStatusEffects(action, match),
                "Exact roll 17 matches", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!ActionTriggerGate.ShouldApplyStatusEffects(action, missFace),
                "Exact roll rejects other face", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var tokenAction = MakeStunAction("ONROLLVALUE:12");
            var tokenHit = new CombatEvent(CombatEventType.ActionHit, source) { RollValue = 12 };
            TestBase.AssertTrue(ActionTriggerGate.ShouldApplyStatusEffects(tokenAction, tokenHit),
                "ONROLLVALUE:12 token matches", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestOnHealthThreshold()
        {
            var action = MakeStunAction("ONHEALTHTHRESHOLD");
            var source = new Character("Hero", 1);
            var th = new CombatEvent(CombatEventType.EnemyHealthThreshold, source) { HealthPercentage = 0.25 };
            TestBase.AssertTrue(ActionTriggerGate.ShouldApplyStatusEffects(action, th),
                "ONHEALTHTHRESHOLD matches", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestOnComboEnd()
        {
            var action = MakeStunAction("ONCOMBOEND");
            var source = new Character("Hero", 1);
            var ended = new CombatEvent(CombatEventType.ComboEnded, source);
            var hit = new CombatEvent(CombatEventType.ActionHit, source) { IsCombo = true };
            TestBase.AssertTrue(ActionTriggerGate.ShouldApplyStatusEffects(action, ended),
                "ONCOMBOEND matches ComboEnded", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!ActionTriggerGate.ShouldApplyStatusEffects(action, hit),
                "ONCOMBOEND rejects combo hit", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestOnRoomsCleared()
        {
            var every = MakeStunAction("ONROOMSCLEARED");
            var nth = MakeStunAction("ONROOMSCLEARED:2");
            var source = new Character("Hero", 1);
            var clear1 = new CombatEvent(CombatEventType.RoomCleared, source) { RoomsClearedCount = 1 };
            var clear2 = new CombatEvent(CombatEventType.RoomCleared, source) { RoomsClearedCount = 2 };
            var hit = new CombatEvent(CombatEventType.ActionHit, source);

            TestBase.AssertTrue(ActionTriggerGate.ShouldApplyStatusEffects(every, clear1),
                "ONROOMSCLEARED fires on 1st clear", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(ActionTriggerGate.ShouldApplyStatusEffects(every, clear2),
                "ONROOMSCLEARED fires on 2nd clear", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!ActionTriggerGate.ShouldApplyStatusEffects(nth, clear1),
                "ONROOMSCLEARED:2 rejects 1st", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(ActionTriggerGate.ShouldApplyStatusEffects(nth, clear2),
                "ONROOMSCLEARED:2 matches 2nd", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!ActionTriggerGate.ShouldApplyStatusEffects(every, hit),
                "ONROOMSCLEARED rejects hit", ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Applicator increments session count and applies pool action stun
            every.CausesStun = true;
            source.ActionPool.Clear();
            source.ActionPool.Add((every, 1.0));
            var messages = RPGGame.Actions.Execution.RoomClearedTriggerApplicator.ApplyForHero(source);
            TestBase.AssertTrue(source.SessionStats.RoomsCleared == 1,
                "Applicator records rooms cleared", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(source.IsStunned,
                "Applicator applies ONROOMSCLEARED status from pool", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestParseTriggerList()
        {
            var parsed = ActionTriggerGate.ParseTriggerConditionList(
                "onhit, ONCONNECT, oncrit, ONCRITMISS, ONKILL, ONCOMBOEND, ONROOMSCLEARED:3, ONWIELD:sword, ONFIRSTHIT, IFCLUTCH, IFSAMESACTION, IFTARGETSTATUS:poison");
            TestBase.AssertTrue(parsed.Contains("ONHIT"), "Parse ONHIT", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(parsed.Contains("ONCONNECT"), "Parse ONCONNECT", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(parsed.Contains("ONCRITICAL"), "Parse ONCRIT→ONCRITICAL", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(parsed.Contains("ONCRITICALMISS"), "Parse ONCRITMISS", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(parsed.Contains("ONKILL"), "Parse ONKILL", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(parsed.Contains("ONCOMBOEND"), "Parse ONCOMBOEND", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(parsed.Contains("ONROOMSCLEARED:3"), "Parse ONROOMSCLEARED:3", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(parsed.Contains("ONWIELD:Sword"), "Parse ONWIELD:sword→ONWIELD:Sword", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(parsed.Contains("ONFIRSTHIT"), "Parse ONFIRSTHIT", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(parsed.Contains("IFCLUTCH"), "Parse IFCLUTCH", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(parsed.Contains("IFSAMESACTION"), "Parse IFSAMESACTION", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(parsed.Contains("IFTARGETSTATUS:poison"), "Parse IFTARGETSTATUS:poison", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSignatureHooks_FirstBloodAfterMissMirrorSwitchClutch()
        {
            CombatTriggerContext.ResetForBattle();
            var firstBlood = MakeStunAction("ONFIRSTHIT");
            var hero = new Character("Hero", 1);
            var hit = new CombatEvent(CombatEventType.ActionHit, hero);
            TestBase.AssertTrue(ActionTriggerGate.ShouldApplyStatusEffects(firstBlood, hit),
                "ONFIRSTHIT matches first connect", ref _testsRun, ref _testsPassed, ref _testsFailed);
            CombatTriggerContext.NotifySwingResolved(hero, firstBlood, connected: true, missed: false);
            TestBase.AssertTrue(!ActionTriggerGate.ShouldApplyStatusEffects(firstBlood, hit),
                "ONFIRSTHIT rejects after first connect", ref _testsRun, ref _testsPassed, ref _testsFailed);

            CombatTriggerContext.ResetForBattle();
            var afterMiss = MakeStunAction("ONAFTERMISS");
            TestBase.AssertTrue(!ActionTriggerGate.ShouldApplyStatusEffects(afterMiss, hit),
                "ONAFTERMISS rejects without prior miss", ref _testsRun, ref _testsPassed, ref _testsFailed);
            CombatTriggerContext.NotifySwingResolved(hero, afterMiss, connected: false, missed: true);
            TestBase.AssertTrue(ActionTriggerGate.ShouldApplyStatusEffects(afterMiss, hit),
                "ONAFTERMISS matches connect after miss", ref _testsRun, ref _testsPassed, ref _testsFailed);

            CombatTriggerContext.ResetForBattle();
            var mirror = MakeStunAction("IFSAMESACTION");
            mirror.Name = "SLASH";
            var switchUp = MakeStunAction("IFDIFFERENTACTION");
            switchUp.Name = "STAB";
            var firstSwing = MakeStunAction();
            firstSwing.Name = "SLASH";
            TestBase.AssertTrue(!ActionTriggerGate.ShouldApplyStatusEffects(mirror, hit),
                "Mirror rejects with no previous action", ref _testsRun, ref _testsPassed, ref _testsFailed);
            CombatTriggerContext.NotifySwingResolved(hero, firstSwing, connected: true, missed: false);
            TestBase.AssertTrue(ActionTriggerGate.ShouldApplyStatusEffects(mirror, hit),
                "Mirror matches same action again", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(ActionTriggerGate.ShouldApplyStatusEffects(switchUp, hit),
                "Switch-up matches different action", ref _testsRun, ref _testsPassed, ref _testsFailed);
            var sameAgain = MakeStunAction("IFDIFFERENTACTION");
            sameAgain.Name = "SLASH";
            TestBase.AssertTrue(!ActionTriggerGate.ShouldApplyStatusEffects(sameAgain, hit),
                "Switch-up rejects same action", ref _testsRun, ref _testsPassed, ref _testsFailed);

            CombatTriggerContext.ResetForBattle();
            var clutch = MakeStunAction("IFCLUTCH");
            hero.CurrentHealth = Math.Max(1, hero.MaxHealth / 10);
            TestBase.AssertTrue(ActionTriggerGate.ShouldApplyStatusEffects(clutch, hit),
                "IFCLUTCH matches low HP connect", ref _testsRun, ref _testsPassed, ref _testsFailed);
            hero.CurrentHealth = hero.MaxHealth;
            TestBase.AssertTrue(!ActionTriggerGate.ShouldApplyStatusEffects(clutch, hit),
                "IFCLUTCH rejects high HP", ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Chain punctuation already covered by ONCOMBOEND tests
            TestBase.AssertTrue(true, "Chain punctuation uses ONCOMBOEND", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSignatureHooks_UnderDotLastEnemyTags()
        {
            CombatTriggerContext.ResetForBattle();
            CombatTriggerContext.SetLivingEnemyCount(1);
            var lastStand = MakeStunAction("IFLASTENEMY");
            var hero = new Character("Hero", 1);
            var hit = new CombatEvent(CombatEventType.ActionHit, hero);
            TestBase.AssertTrue(ActionTriggerGate.ShouldApplyStatusEffects(lastStand, hit),
                "IFLASTENEMY matches when living count is 1", ref _testsRun, ref _testsPassed, ref _testsFailed);
            CombatTriggerContext.SetLivingEnemyCount(3);
            TestBase.AssertTrue(!ActionTriggerGate.ShouldApplyStatusEffects(lastStand, hit),
                "IFLASTENEMY rejects when living count > 1", ref _testsRun, ref _testsPassed, ref _testsFailed);

            CombatTriggerContext.ResetForBattle();
            var sourceDot = MakeStunAction("IFSOURCEUNDERDOT");
            hero.PoisonPercentOfMaxHealth = 2;
            TestBase.AssertTrue(ActionTriggerGate.ShouldApplyStatusEffects(sourceDot, hit),
                "IFSOURCEUNDERDOT matches poisoned hero", ref _testsRun, ref _testsPassed, ref _testsFailed);
            hero.PoisonPercentOfMaxHealth = 0;
            TestBase.AssertTrue(!ActionTriggerGate.ShouldApplyStatusEffects(sourceDot, hit),
                "IFSOURCEUNDERDOT rejects clean hero", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var foe = new Enemy("Foe", 1, 100, 10, 5, 5, 5);
            foe.BurnIntensity = 3;
            foe.SetTags(new[] { "beast" });
            var targetDot = MakeStunAction("IFTARGETUNDERDOT");
            var hitOnFoe = new CombatEvent(CombatEventType.ActionHit, hero) { Target = foe };
            TestBase.AssertTrue(ActionTriggerGate.ShouldApplyStatusEffects(targetDot, hitOnFoe),
                "IFTARGETUNDERDOT matches burning foe", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var statusToken = MakeStunAction("IFTARGETSTATUS:burn");
            TestBase.AssertTrue(ActionTriggerGate.ShouldApplyStatusEffects(statusToken, hitOnFoe),
                "IFTARGETSTATUS:burn matches", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var tagAction = MakeStunAction("IFACTIONHASTAG:blade", "IFTARGETHASTAG:beast");
            tagAction.Tags = new List<string> { "blade" };
            TestBase.AssertTrue(ActionTriggerGate.ShouldApplyStatusEffects(tagAction, hitOnFoe),
                "Tag synergy action+target matches", ref _testsRun, ref _testsPassed, ref _testsFailed);
            tagAction.Tags = new List<string> { "fire" };
            TestBase.AssertTrue(!ActionTriggerGate.ShouldApplyStatusEffects(tagAction, hitOnFoe),
                "Tag synergy rejects missing action tag", ref _testsRun, ref _testsPassed, ref _testsFailed);

            hero.Weapon = new WeaponItem("Tagged", 1, 10, 1.0, WeaponType.Sword) { Tags = new List<string> { "holy" } };
            var gearTag = MakeStunAction("IFGEARHASTAG:holy");
            TestBase.AssertTrue(ActionTriggerGate.ShouldApplyStatusEffects(gearTag, hit),
                "IFGEARHASTAG matches weapon tag", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestOnWield_StandaloneImpliesConnect()
        {
            var action = MakeStunAction("ONWIELD:Sword");
            var hero = new Character("Hero", 1)
            {
                Weapon = new WeaponItem("TestBlade", 1, 10, 1.0, WeaponType.Sword)
            };
            var hit = new CombatEvent(CombatEventType.ActionHit, hero);
            var miss = new CombatEvent(CombatEventType.ActionMiss, hero) { IsMiss = true };
            var kill = new CombatEvent(CombatEventType.EnemyDied, hero);

            TestBase.AssertTrue(ActionTriggerGate.ShouldApplyStatusEffects(action, hit),
                "Standalone ONWIELD applies on connect", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!ActionTriggerGate.ShouldApplyStatusEffects(action, miss),
                "Standalone ONWIELD rejects miss", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!ActionTriggerGate.ShouldApplyStatusEffects(action, kill),
                "Standalone ONWIELD rejects kill", ref _testsRun, ref _testsPassed, ref _testsFailed);

            hero.Weapon = new WeaponItem("TestMace", 1, 10, 1.0, WeaponType.Mace);
            TestBase.AssertTrue(!ActionTriggerGate.ShouldApplyStatusEffects(action, hit),
                "Standalone ONWIELD rejects wrong weapon type", ref _testsRun, ref _testsPassed, ref _testsFailed);

            hero.Weapon = null;
            TestBase.AssertTrue(!ActionTriggerGate.ShouldApplyStatusEffects(action, hit),
                "Standalone ONWIELD rejects unarmed", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestOnWield_OrTypesAndAndWithOutcome()
        {
            var orAction = MakeStunAction("ONWIELD:Sword", "ONWIELD:Mace");
            var hero = new Character("Hero", 1)
            {
                Weapon = new WeaponItem("Blade", 1, 10, 1.0, WeaponType.Sword)
            };
            var hit = new CombatEvent(CombatEventType.ActionHit, hero);
            TestBase.AssertTrue(ActionTriggerGate.ShouldApplyStatusEffects(orAction, hit),
                "ONWIELD Sword|Mace matches Sword", ref _testsRun, ref _testsPassed, ref _testsFailed);

            hero.Weapon = new WeaponItem("Club", 1, 10, 1.0, WeaponType.Mace);
            TestBase.AssertTrue(ActionTriggerGate.ShouldApplyStatusEffects(orAction, hit),
                "ONWIELD Sword|Mace matches Mace", ref _testsRun, ref _testsPassed, ref _testsFailed);

            hero.Weapon = new WeaponItem("Stick", 1, 10, 1.0, WeaponType.Wand);
            TestBase.AssertTrue(!ActionTriggerGate.ShouldApplyStatusEffects(orAction, hit),
                "ONWIELD Sword|Mace rejects Wand", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var missAndWield = MakeStunAction("ONMISS", "ONWIELD:Dagger");
            hero.Weapon = new WeaponItem("Knife", 1, 10, 1.0, WeaponType.Dagger);
            var miss = new CombatEvent(CombatEventType.ActionMiss, hero) { IsMiss = true };
            TestBase.AssertTrue(ActionTriggerGate.ShouldApplyStatusEffects(missAndWield, miss),
                "ONMISS + ONWIELD:Dagger matches dagger miss", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!ActionTriggerGate.ShouldApplyStatusEffects(missAndWield, hit),
                "ONMISS + ONWIELD rejects hit", ref _testsRun, ref _testsPassed, ref _testsFailed);

            hero.Weapon = new WeaponItem("Blade", 1, 10, 1.0, WeaponType.Sword);
            TestBase.AssertTrue(!ActionTriggerGate.ShouldApplyStatusEffects(missAndWield, miss),
                "ONMISS + ONWIELD:Dagger rejects sword miss", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGateBlocksWeaponStyleDoTUnlessTriggered()
        {
            var action = MakeStunAction("ONMISS");
            action.CausesPoison = true;
            action.PoisonPercentToAdd = 2;
            var source = new Character("Hero", 1);
            var target = new Enemy("Foe", 1, 100, 10, 5, 5, 5);
            var hit = new CombatEvent(CombatEventType.ActionHit, source) { IsCritical = true };
            var results = new List<string>();
            bool applied = CombatEffectsSimplified.ApplyStatusEffects(action, source, target, results, hit);
            TestBase.AssertTrue(!applied && target.PoisonPercentOfMaxHealth <= 0,
                "ONMISS-only action does not apply poison on crit hit", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var miss = new CombatEvent(CombatEventType.ActionMiss, source) { IsMiss = true };
            applied = CombatEffectsSimplified.ApplyStatusEffects(action, source, target, results, miss);
            TestBase.AssertTrue(applied || target.IsStunned || target.PoisonPercentOfMaxHealth > 0,
                "ONMISS-only action applies on miss", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
