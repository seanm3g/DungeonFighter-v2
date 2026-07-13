using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Stun skip duration is one attack-time slot on the victim's own timeline.
    /// </summary>
    public static class StunProcessorTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== StunProcessor Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestStunSkipUsesVictimAttackTimeOnOwnTimeline();
            TestStunSkipDoesNotSnapPastLongGlobalJump();
            TestMultiTurnStunConsumesOneTurnPerSkip();

            TestBase.PrintSummary("StunProcessor Tests", _testsRun, _testsPassed, _testsFailed);
        }

        /// <summary>
        /// One stun turn clears after one skip and advances readiness by GetTotalAttackSpeed from the
        /// entity's own NextActionTime (not snapped to inflated global time).
        /// </summary>
        private static void TestStunSkipUsesVictimAttackTimeOnOwnTimeline()
        {
            Console.WriteLine("--- Stun skip uses victim attack time on own timeline ---");

            bool prevUi = CombatManager.DisableCombatUIOutput;
            CombatManager.DisableCombatUIOutput = true;
            try
            {
                using (GameTicker.BeginIsolatedEncounterGameTime())
                {
                    GameTicker.Instance.Reset();

                    var player = TestDataBuilders.Character().WithName("StunHero").WithStats(10, 10, 10, 10).Build();
                    var enemy = TestDataBuilders.Enemy().WithName("StunFoe").Build();
                    double attackTime = player.GetTotalAttackSpeed();

                    var state = new CombatStateManager();
                    state.StartBattleNarrative(player.Name, enemy.Name, "TestRoom", player.CurrentHealth, enemy.CurrentHealth);
                    var speed = state.GetCurrentActionSpeedSystem()!;
                    speed.AddEntity(player, attackTime);
                    speed.AddEntity(enemy, enemy.GetTotalAttackSpeed());
                    speed.SetEntityActionTime(player, 4.0);
                    speed.SetEntityActionTime(enemy, 4.0);

                    player.IsStunned = true;
                    player.StunTurnsRemaining = 1;

                    StunProcessor.ProcessStunnedEntity(player, state);

                    TestBase.AssertEqual(0, player.StunTurnsRemaining,
                        "1-turn stun should clear after one skip",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertTrue(!player.IsStunned,
                        "IsStunned should clear when turns hit 0",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);

                    // After skip from readyAt=4.0, next should be ~4.0 + attackTime (own timeline).
                    // Global time advanced by attackTime only (from Reset 0 → attackTime via AdvanceGameTime).
                    double timeUntilReady = speed.GetTimeUntilReady(player);
                    double expectedUntil = Math.Max(0.0, (4.0 + attackTime) - GameTicker.Instance.GetCurrentGameTime());
                    TestBase.AssertTrue(Math.Abs(timeUntilReady - expectedUntil) < 0.05,
                        $"Player readiness should follow own timeline+attackTime; until={timeUntilReady:F3} expected≈{expectedUntil:F3} attackTime={attackTime:F3}",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
            finally
            {
                CombatManager.DisableCombatUIOutput = prevUi;
            }
        }

        /// <summary>
        /// Reproduces the Room Collapse failure mode: global clock jumped far ahead while the hero's
        /// NextActionTime is still early. Stun must not snap the hero to "now" (which stranded them
        /// while the foe resolved many catch-up swings).
        /// </summary>
        private static void TestStunSkipDoesNotSnapPastLongGlobalJump()
        {
            Console.WriteLine("--- Stun skip does not snap after long global jump ---");

            bool prevUi = CombatManager.DisableCombatUIOutput;
            CombatManager.DisableCombatUIOutput = true;
            try
            {
                using (GameTicker.BeginIsolatedEncounterGameTime())
                {
                    GameTicker.Instance.Reset();

                    var player = TestDataBuilders.Character().WithName("Monroe").WithStats(10, 10, 10, 10).Build();
                    var enemy = new Enemy(
                        name: "Lich",
                        level: 1,
                        maxHealth: 100,
                        damage: 9,
                        armor: 1,
                        attackSpeed: 4.4,
                        useDirectStats: true);
                    double playerAttack = player.GetTotalAttackSpeed();

                    var state = new CombatStateManager();
                    state.StartBattleNarrative(player.Name, enemy.Name, "Rocky Outcrop", player.CurrentHealth, enemy.CurrentHealth);
                    var speed = state.GetCurrentActionSpeedSystem()!;
                    speed.AddEntity(player, playerAttack);
                    speed.AddEntity(enemy, enemy.GetTotalAttackSpeed());

                    // Hero due at T=4.4; env-style 30s jump leaves both behind the clock.
                    speed.SetEntityActionTime(player, 4.4);
                    speed.SetEntityActionTime(enemy, 0.0);
                    GameTicker.Instance.AdvanceGameTime(34.4);

                    player.IsStunned = true;
                    player.StunTurnsRemaining = 1;

                    StunProcessor.ProcessStunnedEntity(player, state);

                    TestBase.AssertEqual(0, player.StunTurnsRemaining,
                        "Stun should clear after the skip",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertTrue(speed.IsEntityReady(player),
                        "Own-timeline stun leaves hero catch-up ready (snap-to-global would push them ahead of the clock)",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);

                    // Count foe swings before the hero is next. Own-timeline stun keeps hero near T≈4.4+atk;
                    // snap-to-global would park hero near T≈34.4+atk (~8+ lich swings at 4.4s).
                    var foeSwing = new Action("Claw", ActionType.Attack, length: 1.0);
                    int enemyActionsBeforePlayer = 0;
                    const int maxSteps = 30;
                    for (int i = 0; i < maxSteps; i++)
                    {
                        // Advance clock to next ready entity when none are ready at "now"
                        var next = speed.GetNextEntityToAct();
                        if (next == null)
                        {
                            double nextReady = speed.GetNextReadyTime();
                            if (nextReady > 0)
                            {
                                double delta = nextReady - speed.GetCurrentTime();
                                if (delta > 0)
                                    speed.AdvanceTime(delta);
                            }
                            next = speed.GetNextEntityToAct();
                        }

                        TestBase.AssertTrue(next != null, "Expected a ready combatant",
                            ref _testsRun, ref _testsPassed, ref _testsFailed);
                        if (next == player)
                            break;

                        speed.ExecuteAction(enemy, foeSwing, isCriticalMiss: false);
                        enemyActionsBeforePlayer++;
                    }

                    TestBase.AssertTrue(enemyActionsBeforePlayer <= 3,
                        $"After 1-turn stun, foe should get at most a few catch-up swings before hero; got {enemyActionsBeforePlayer} (playerAtk={playerAttack:F2}s)",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertTrue(enemyActionsBeforePlayer < 8,
                        "Snap-to-global stun would allow ~8+ foe swings during one stun turn",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
            finally
            {
                CombatManager.DisableCombatUIOutput = prevUi;
            }
        }

        private static void TestMultiTurnStunConsumesOneTurnPerSkip()
        {
            Console.WriteLine("--- Multi-turn stun consumes one turn per skip ---");

            bool prevUi = CombatManager.DisableCombatUIOutput;
            CombatManager.DisableCombatUIOutput = true;
            try
            {
                using (GameTicker.BeginIsolatedEncounterGameTime())
                {
                    GameTicker.Instance.Reset();

                    var player = TestDataBuilders.Character().WithName("TwoTurnHero").Build();
                    var enemy = TestDataBuilders.Enemy().WithName("TwoTurnFoe").Build();

                    var state = new CombatStateManager();
                    state.StartBattleNarrative(player.Name, enemy.Name, "Room", player.CurrentHealth, enemy.CurrentHealth);
                    var speed = state.GetCurrentActionSpeedSystem()!;
                    speed.AddEntity(player, player.GetTotalAttackSpeed());
                    speed.AddEntity(enemy, 1.0);

                    player.IsStunned = true;
                    player.StunTurnsRemaining = 2;

                    StunProcessor.ProcessStunnedEntity(player, state);
                    TestBase.AssertEqual(1, player.StunTurnsRemaining,
                        "First skip should leave 1 turn",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertTrue(player.IsStunned,
                        "Still stunned after first of two turns",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);

                    StunProcessor.ProcessStunnedEntity(player, state);
                    TestBase.AssertEqual(0, player.StunTurnsRemaining,
                        "Second skip should clear stun",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertTrue(!player.IsStunned,
                        "IsStunned false after two skips",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
            finally
            {
                CombatManager.DisableCombatUIOutput = prevUi;
            }
        }
    }
}
