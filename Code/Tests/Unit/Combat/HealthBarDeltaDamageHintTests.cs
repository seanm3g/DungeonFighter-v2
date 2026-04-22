using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Combat.UI;
using RPGGame.Tests;
using RPGGame.UI.Avalonia;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Tests for DoT → health bar delta hint bridge (<see cref="HealthBarDeltaDamageHint"/>, <see cref="HealthBarEntityId"/>).
    /// </summary>
    public static class HealthBarDeltaDamageHintTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== HealthBarDeltaDamageHint Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestHealthBarEntityId_EnemyUsesEnemyPrefix();
            TestTryConsume_OrdersPoisonThenBurn();
            TestTryConsume_MismatchClearsPendingAndReturnsFalse();
            TestHealthTracker_ConsumesHintOnDamageAndStoresParts();
            TestProcessStatusEffectsWithBreakdown_MatchesProcessStatusEffectsTotal();
            TestRecordAfterMitigation_ScalesPoisonBurnToActualHpLost();

            TestBase.PrintSummary("HealthBarDeltaDamageHint Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestHealthBarEntityId_EnemyUsesEnemyPrefix()
        {
            Console.WriteLine("--- HealthBarEntityId: Enemy uses enemy_ prefix (not player_) ---");
            var enemy = TestDataBuilders.Enemy().WithName("Slime").Build();
            string? id = HealthBarEntityId.ForActor(enemy);
            TestBase.AssertEqual("enemy_Slime", id ?? "",
                "Enemy subclass of Character must still map to enemy_* bar id",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestTryConsume_OrdersPoisonThenBurn()
        {
            Console.WriteLine("--- HealthBarDeltaDamageHint: TryConsume orders poison then burn ---");
            HealthBarDeltaDamageHint.ClearAll();
            const string id = "player_TestHero";
            HealthBarDeltaDamageHint.SetPending(id, poisonDamage: 3, burnDamage: 2, bleedDamage: 0);
            bool ok = HealthBarDeltaDamageHint.TryConsume(id, 5, out var parts);
            TestBase.AssertTrue(ok && parts != null && parts.Count == 2,
                "TryConsume should succeed with two segments",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (parts != null && parts.Count == 2)
            {
                TestBase.AssertTrue(parts[0].Kind == HealthBarDotDamageKind.Poison,
                    "first chunk is poison (inner / tick order)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(3, parts[0].Amount, "poison amount", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(parts[1].Kind == HealthBarDotDamageKind.Burn,
                    "second chunk is burn",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(2, parts[1].Amount, "burn amount", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestRecordAfterMitigation_ScalesPoisonBurnToActualHpLost()
        {
            Console.WriteLine("--- RecordAfterMitigation: scales to actual HP lost (mitigation case) ---");
            HealthBarDeltaDamageHint.ClearAll();
            const string id = "enemy_Mitigated";
            // Requested 14 (10 poison + 4 burn) but only 7 HP removed (e.g. damage reduction)
            HealthBarDeltaDamageHint.RecordAfterMitigation(id, poisonRequested: 10, burnRequested: 4, bleedRequested: 0, requestedTotal: 14, actualHpLost: 7);
            bool ok = HealthBarDeltaDamageHint.TryConsume(id, 7, out var parts);
            TestBase.AssertTrue(ok && parts != null && parts.Count == 2,
                "scaled parts should consume for actual total 7",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (parts != null && parts.Count == 2)
            {
                TestBase.AssertEqual(7, parts[0].Amount + parts[1].Amount,
                    "segment amounts sum to actual HP lost",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(parts[0].Kind == HealthBarDotDamageKind.Poison, "poison first", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(parts[1].Kind == HealthBarDotDamageKind.Burn, "burn second", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestTryConsume_MismatchClearsPendingAndReturnsFalse()
        {
            Console.WriteLine("--- HealthBarDeltaDamageHint: wrong total removes hint ---");
            HealthBarDeltaDamageHint.ClearAll();
            const string id = "enemy_Boss";
            HealthBarDeltaDamageHint.SetPending(id, 2, 2, 0);
            bool ok = HealthBarDeltaDamageHint.TryConsume(id, 99, out var parts);
            TestBase.AssertFalse(ok, "mismatch should not succeed", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(parts == null, "parts should be null on mismatch", ref _testsRun, ref _testsPassed, ref _testsFailed);
            bool ok2 = HealthBarDeltaDamageHint.TryConsume(id, 4, out _);
            TestBase.AssertFalse(ok2, "hint should be cleared after failed consume", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHealthTracker_ConsumesHintOnDamageAndStoresParts()
        {
            Console.WriteLine("--- HealthTracker: ties hint to next health decrease ---");
            HealthBarDeltaDamageHint.ClearAll();
            var tracker = new HealthTracker();
            const string id = "player_A";
            tracker.UpdateHealth(id, 100);
            HealthBarDeltaDamageHint.SetPending(id, 1, 0, 4);
            tracker.UpdateHealth(id, 95);
            var parts = tracker.GetDamageDeltaDotParts(id);
            TestBase.AssertTrue(parts != null && parts.Count == 2,
                "expected poison + bleed segments",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (parts != null && parts.Count == 2)
            {
                TestBase.AssertTrue(parts[0].Kind == HealthBarDotDamageKind.Poison, "poison first", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(parts[1].Kind == HealthBarDotDamageKind.Bleed, "bleed second", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestProcessStatusEffectsWithBreakdown_MatchesProcessStatusEffectsTotal()
        {
            Console.WriteLine("--- ProcessStatusEffectsWithBreakdown.TotalDamage matches ProcessStatusEffects ---");
            using (GameTicker.BeginIsolatedEncounterGameTime())
            {
                static Enemy MakeDotDummy(string name)
                {
                    var e = TestDataBuilders.Enemy().WithName(name).Build();
                    e.PoisonPercentOfMaxHealth = 2;
                    e.LastPoisonTickTime = 0;
                    e.BurnIntensity = 4;
                    e.PendingBurnFromHits = 0;
                    e.LastBurnTickTime = 0;
                    return e;
                }

                var enemyA = MakeDotDummy("DotA");
                var enemyB = MakeDotDummy("DotB");
                GameTicker.Instance.AdvanceGameTime(5.1);

                var listA = new List<string>();
                var listB = new List<string>();
                int totalLegacy = CombatEffectsSimplified.ProcessStatusEffects(enemyA, listA);
                CombatEffectsSimplified.StatusEffectDamageBreakdown bd =
                    CombatEffectsSimplified.ProcessStatusEffectsWithBreakdown(enemyB, listB);
                TestBase.AssertEqual(totalLegacy, bd.TotalDamage,
                    "breakdown total should match ProcessStatusEffects for equivalent pre-tick actor state",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(bd.PoisonDamage + bd.BurnDamage, bd.TotalDamage,
                    "components sum to total",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }
    }
}
