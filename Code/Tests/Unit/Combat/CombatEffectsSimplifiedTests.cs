using System;
using System.Collections.Generic;
using RPGGame.Tests;
using RPGGame;
using RPGGame.Combat.Events;
using RPGGame.Combat.Turn;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Tests for CombatEffectsSimplified — application and turn processing of status effects.
    /// </summary>
    public static class CombatEffectsSimplifiedTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all CombatEffectsSimplified tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== CombatEffectsSimplified Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestApplyStatusEffects_PoisonFromAction_RegistersOnTarget();
            TestProcessStatusEffects_NoPoisonOrBurn_ReturnsZeroTotalDamage();
            TestBurnIntensityDecayAndPendingMerge();
            TestPoisonPercentUnchangedAcrossTicks();
            TestBleedOnActionWithoutGameTimeTick();
            TestCanEntityAct_NotStunned_ReturnsTrue();
            TestCanEntityAct_Stunned_ReturnsFalse();
            TestCalculateEffectDuration_RespectsMinimumOneTurn();
            TestNonLivingEnemyBurnTicksViaStatusEffectProcessor();
            TestWeaponDoTModsRequireCriticalHitEvent();
            TestWeakenStatusLogLineUsesEntityColorOnActorName();

            TestBase.PrintSummary("CombatEffectsSimplified Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestApplyStatusEffects_PoisonFromAction_RegistersOnTarget()
        {
            Console.WriteLine("--- ApplyStatusEffects: poison action applies via registry ---");

            var attacker = TestDataBuilders.Character().WithName("AllyAttacker").Build();
            var target = TestDataBuilders.Enemy().WithName("PoisonTarget").Build();
            var action = TestDataBuilders.CreateMockAction("VenomStrike", ActionType.Attack);
            action.CausesPoison = true;

            var results = new List<string>();
            var critHit = new CombatEvent(CombatEventType.ActionHit, attacker)
            {
                Target = target,
                Action = action,
                IsCritical = true
            };
            bool applied = CombatEffectsSimplified.ApplyStatusEffects(action, attacker, target, results, critHit);

            TestBase.AssertTrue(target.PoisonPercentOfMaxHealth > 0 || applied,
                "Poison-from-action path should increase poison % or set effectsApplied",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestWeaponDoTModsRequireCriticalHitEvent()
        {
            Console.WriteLine("--- ApplyStatusEffects: weapon poison/burn/bleed only when hit event is critical ---");

            var attacker = TestDataBuilders.Character().WithName("DotStriker").WithStats(20, 20, 20, 20).Build();
            var weapon = TestDataBuilders.Weapon()
                .WithModification(new Modification
                {
                    Effect = "weaponPoison",
                    RolledValue = 2.0,
                    MinValue = 2,
                    MaxValue = 2
                })
                .WithModification(new Modification
                {
                    Effect = "weaponBurn",
                    RolledValue = 1,
                    MinValue = 1,
                    MaxValue = 1
                })
                .WithModification(new Modification
                {
                    Effect = "weaponBleed",
                    RolledValue = 1,
                    MinValue = 1,
                    MaxValue = 1
                })
                .Build();
            attacker.EquipItem(weapon, "weapon");

            var target = TestDataBuilders.Enemy().WithName("Dummy").WithHealth(200).Build();
            var jab = TestDataBuilders.CreateMockAction("JAB", ActionType.Attack);

            var nonCrit = new CombatEvent(CombatEventType.ActionHit, attacker)
            {
                Target = target,
                Action = jab,
                IsCritical = false
            };
            var resultsNon = new List<string>();
            target.PoisonPercentOfMaxHealth = 0;
            target.PendingBurnFromHits = 0;
            target.PendingBleedFromHits = 0;
            _ = CombatEffectsSimplified.ApplyStatusEffects(jab, attacker, target, resultsNon, nonCrit);
            TestBase.AssertTrue(target.PoisonPercentOfMaxHealth < 0.0001 && target.PendingBurnFromHits == 0 && target.PendingBleedFromHits == 0,
                "non-crit hit event must not apply weapon poison, burn, or bleed",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var crit = new CombatEvent(CombatEventType.ActionHit, attacker)
            {
                Target = target,
                Action = jab,
                IsCritical = true
            };
            var resultsCrit = new List<string>();
            _ = CombatEffectsSimplified.ApplyStatusEffects(jab, attacker, target, resultsCrit, crit);
            TestBase.AssertTrue(target.PoisonPercentOfMaxHealth > 0 && target.PendingBurnFromHits > 0 && target.PendingBleedFromHits > 0,
                "critical hit event should apply all three weapon DoT channels",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestProcessStatusEffects_NoPoisonOrBurn_ReturnsZeroTotalDamage()
        {
            Console.WriteLine("--- ProcessStatusEffects: clean actor yields no damage ---");

            var actor = TestDataBuilders.Character().WithName("CleanActor").Build();
            actor.PoisonPercentOfMaxHealth = 0;
            actor.BurnIntensity = 0;
            actor.PendingBurnFromHits = 0;

            var results = new List<string>();
            int total = CombatEffectsSimplified.ProcessStatusEffects(actor, results);

            TestBase.AssertEqual(0, total,
                "No poison/burn should produce zero processed damage total",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestBurnIntensityDecayAndPendingMerge()
        {
            Console.WriteLine("--- Burn: decay + pending merge on global tick ---");
            using (GameTicker.BeginIsolatedEncounterGameTime())
            {
                var enemy = TestDataBuilders.Enemy().WithName("BurnDummy").Build();
                enemy.BurnIntensity = 5;
                enemy.PendingBurnFromHits = 3;
                enemy.LastBurnTickTime = 0;
                GameTicker.Instance.AdvanceGameTime(5.1);
                int dmg = enemy.ProcessBurn(GameTicker.Instance.GetCurrentGameTime());
                TestBase.AssertEqual(5, dmg, "burn damage", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(7, enemy.BurnIntensity, "burn intensity after decay+pending", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestPoisonPercentUnchangedAcrossTicks()
        {
            Console.WriteLine("--- Poison: stored % unchanged after tick damage ---");
            using (GameTicker.BeginIsolatedEncounterGameTime())
            {
                var enemy = TestDataBuilders.Enemy().WithName("PoisonDummy").Build();
                enemy.PoisonPercentOfMaxHealth = 4;
                enemy.LastPoisonTickTime = 0;
                GameTicker.Instance.AdvanceGameTime(5.1);
                double pctBefore = enemy.PoisonPercentOfMaxHealth;
                _ = enemy.ProcessPoison(GameTicker.Instance.GetCurrentGameTime());
                TestBase.AssertTrue(Math.Abs(pctBefore - enemy.PoisonPercentOfMaxHealth) < 0.0001,
                    "poison % should not decay on tick",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestBleedOnActionWithoutGameTimeTick()
        {
            Console.WriteLine("--- Bleed: resolves on action pulse without advance game time ---");
            var enemy = TestDataBuilders.Enemy().WithName("BleedDummy").Build();
            enemy.BleedIntensity = 4;
            enemy.PendingBleedFromHits = 2;
            int dmg = enemy.ProcessBleedOnAction();
            TestBase.AssertEqual(4, dmg, "bleed damage", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(5, enemy.BleedIntensity, "bleed intensity after decay+pending", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Non-living enemies (elemental/undead) must still run global burn ticks; <see cref="Enemy.IsLiving"/> only gates poison/bleed.
        /// </summary>
        private static void TestNonLivingEnemyBurnTicksViaStatusEffectProcessor()
        {
            Console.WriteLine("--- StatusEffectProcessor: non-living enemy still takes burn tick damage ---");
            bool prevUi = TurnManager.DisableCombatUIOutput;
            TurnManager.DisableCombatUIOutput = true;
            try
            {
                using (GameTicker.BeginIsolatedEncounterGameTime())
                {
                    var player = TestDataBuilders.Character().WithName("Player").Build();
                    player.BurnIntensity = 0;
                    player.PendingBurnFromHits = 0;

                    var enemy = new Enemy("Elemental", 1, 100, 5, 5, 5, 5, 0, PrimaryAttribute.Strength, isLiving: false);
                    TestBase.AssertTrue(!enemy.IsLiving && enemy.IsAlive,
                        "fixture should be non-living template but alive in combat",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);

                    int hpBefore = enemy.CurrentHealth;
                    enemy.BurnIntensity = 10;
                    enemy.LastBurnTickTime = 0;
                    GameTicker.Instance.AdvanceGameTime(5.1);

                    StatusEffectProcessor.ProcessDamageOverTimeEffects(player, enemy);

                    TestBase.AssertEqual(hpBefore - 10, enemy.CurrentHealth,
                        "burn tick should reduce HP on non-living enemy",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertEqual(9, enemy.BurnIntensity,
                        "burn intensity should decay after tick",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
            finally
            {
                TurnManager.DisableCombatUIOutput = prevUi;
            }
        }

        private static void TestCanEntityAct_NotStunned_ReturnsTrue()
        {
            Console.WriteLine("--- CanEntityAct: unstunned actor can act ---");

            var actor = TestDataBuilders.Character().WithName("Ready").Build();
            actor.IsStunned = false;
            var results = new List<string>();

            bool can = CombatEffectsSimplified.CanEntityAct(actor, results);

            TestBase.AssertTrue(can, "Unstunned actor should be able to act",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCanEntityAct_Stunned_ReturnsFalse()
        {
            Console.WriteLine("--- CanEntityAct: stunned actor cannot act ---");

            var actor = TestDataBuilders.Character().WithName("StunnedHero").Build();
            actor.IsStunned = true;
            var results = new List<string>();

            bool can = CombatEffectsSimplified.CanEntityAct(actor, results);

            TestBase.AssertTrue(!can, "Stunned actor should not act",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(results.Count > 0, "Stun path should add a message",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestWeakenStatusLogLineUsesEntityColorOnActorName()
        {
            Console.WriteLine("--- Status log: weaken line uses EntityColorHelper for name ---");

            var target = TestDataBuilders.Character().WithName("Zion Twilight").Build();
            var action = new Action { CausesWeaken = true };
            var results = new List<string>();
            var handler = new WeakenEffectHandler();
            bool applied = handler.Apply(target, action, results);

            TestBase.AssertTrue(applied, "Weaken handler should apply",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(results.Count == 1, "Expected one log line",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(results[0].IndexOf("{{player|", StringComparison.OrdinalIgnoreCase) < 0,
                "Log line should not use legacy {{player|...}} name markup",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var expectedRgb = EntityColorHelper.GetActorColor(target);
            var parsed = ColoredTextParser.Parse(results[0]);
            ColoredText? nameSegment = null;
            foreach (var seg in parsed)
            {
                if (seg.Text.IndexOf("Zion", StringComparison.Ordinal) >= 0)
                {
                    nameSegment = seg;
                    break;
                }
            }

            TestBase.AssertTrue(nameSegment != null, "Parsed line should include the actor name",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            bool rgbMatch = nameSegment!.Color.R == expectedRgb.R
                && nameSegment.Color.G == expectedRgb.G
                && nameSegment.Color.B == expectedRgb.B;
            TestBase.AssertTrue(rgbMatch,
                $"Name color should match EntityColorHelper (expected {expectedRgb}, got {nameSegment.Color})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCalculateEffectDuration_RespectsMinimumOneTurn()
        {
            Console.WriteLine("--- CalculateEffectDuration: minimum 1 turn ---");

            var attacker = TestDataBuilders.Character().WithName("Caster").Build();
            var target = TestDataBuilders.Enemy().WithName("Victim").Build();

            int d = CombatEffectsSimplified.CalculateEffectDuration(1, attacker, target);

            TestBase.AssertTrue(d >= 1, $"Duration should be at least 1, got {d}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
