using System;
using System.Collections.Generic;
using RPGGame.Tests;
using RPGGame;
using RPGGame.Combat.Calculators;
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
            // Step 5: these tests cover the retired statuses' code, which stays until step 12 (CoreStatuses).
            Console.WriteLine("=== CombatEffectsSimplified Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestProcessStatusEffects_NoPoisonOrBurn_ReturnsZeroTotalDamage();
            TestBurnIntensityDecayAndPendingMerge();
            TestProcessStatusEffects_DoesNotRepeatDotStateBeforeNextTick();
            TestBleedOnActionWithoutGameTimeTick();
            TestNonLivingEnemyBurnTicksViaStatusEffectProcessor();
            TestWeaponDoTModsRequireCriticalHitEvent();

            TestBase.PrintSummary("CombatEffectsSimplified Tests", _testsRun, _testsPassed, _testsFailed);
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
                .WithModification(new Modification
                {
                    Effect = "weaponAcid",
                    RolledValue = 2,
                    MinValue = 2,
                    MaxValue = 2
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
            target.PendingBurnFromHits = 0;
            target.PendingBleedFromHits = 0;
            _ = CombatEffectsSimplified.ApplyStatusEffects(jab, attacker, target, resultsNon, nonCrit);
            TestBase.AssertTrue(target.PendingBurnFromHits > 0 || target.PendingBleedFromHits > 0 || (target.ExposeStacks ?? 0) > 0,
                "non-crit landed hit applies gear DoT (ONCONNECT default)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var crit = new CombatEvent(CombatEventType.ActionHit, attacker)
            {
                Target = target,
                Action = jab,
                IsCritical = true
            };
            var resultsCrit = new List<string>();
            _ = CombatEffectsSimplified.ApplyStatusEffects(jab, attacker, target, resultsCrit, crit);
            TestBase.AssertTrue(target.PendingBurnFromHits > 0 && target.PendingBleedFromHits > 0 && (target.ExposeStacks ?? 0) > 0,
                "critical hit event should apply burn, bleed and acid (as Expose)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestProcessStatusEffects_NoPoisonOrBurn_ReturnsZeroTotalDamage()
        {
            Console.WriteLine("--- ProcessStatusEffects: clean actor yields no damage ---");

            var actor = TestDataBuilders.Character().WithName("CleanActor").Build();
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

        private static void TestProcessStatusEffects_DoesNotRepeatDotStateBeforeNextTick()
        {
            Console.WriteLine("--- ProcessStatusEffects: no duplicate DoT state before next tick ---");
            using (GameTicker.BeginIsolatedEncounterGameTime())
            {
                var enemy = TestDataBuilders.Enemy().WithName("PoisonDummy").WithHealth(100).Build();
                enemy.BurnIntensity = 4;
                enemy.LastBurnTickTime = 0;

                GameTicker.Instance.AdvanceGameTime(5.1);
                var firstResults = new List<string>();
                int firstDamage = CombatEffectsSimplified.ProcessStatusEffects(enemy, firstResults);

                TestBase.AssertTrue(firstDamage > 0,
                    "first DoT processing after interval should tick",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(firstResults.Count > 0,
                    "first DoT processing should log tick output",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                var secondResults = new List<string>();
                int secondDamage = CombatEffectsSimplified.ProcessStatusEffects(enemy, secondResults);

                TestBase.AssertEqual(0, secondDamage,
                    "second DoT processing before next interval should not tick",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(0, secondResults.Count,
                    "second DoT processing before next interval should not repeat poison/burn detail lines",
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

                    // Non-living templates count as undead: burn ticks deal ×1.5 (StatusAffinities.json).
                    TestBase.AssertEqual(hpBefore - 15, enemy.CurrentHealth,
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
    }
}
