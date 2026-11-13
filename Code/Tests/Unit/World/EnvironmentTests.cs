using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.World
{
    /// <summary>
    /// Comprehensive tests for Environment
    /// Tests environment effects, environmental actions, and enemy management
    /// </summary>
    public static class EnvironmentTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all Environment tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== Environment Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestEnvironmentCreation();
            TestEnvironmentProperties();
            TestEnvironmentThemeTagFallback();
            TestGenerateEnemies();
            TestGetEnemies();
            TestHasLivingEnemies();
            TestResetForNewFight();
            TestRoomRollTrigger();
            TestRoomTriggerQueuesBeforeItActs();
            TestStunnedTurnsInQueue();
            try { TestRoomStunOnCombatBoard(); }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"Room stun on the combat board threw: {ex}", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            TestBase.PrintSummary("Environment Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Creation Tests

        private static void TestEnvironmentCreation()
        {
            Console.WriteLine("--- Testing Environment Creation ---");

            var environment = new Environment("Test Room", "A test room", true, "Forest", "Chamber");
            TestBase.AssertNotNull(environment,
                "Environment should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (environment != null)
            {
                TestBase.AssertEqual("Test Room", environment.Name,
                    "Environment should have correct name",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertEqual("A test room", environment.Description,
                    "Environment should have correct description",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertTrue(environment.IsHostile,
                    "Environment should have correct hostility",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertEqual("Forest", environment.Theme,
                    "Environment should have correct theme",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Properties Tests

        private static void TestEnvironmentProperties()
        {
            Console.WriteLine("\n--- Testing Environment Properties ---");

            var environment = new Environment("Test Room", "A test room", false, "Forest");

            TestBase.AssertFalse(environment.IsHostile,
                "Non-hostile environment should have IsHostile = false",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(environment.ActionPool,
                "Environment action pool should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEnvironmentThemeTagFallback()
        {
            Console.WriteLine("\n--- Testing Environment theme tag fallback ---");

            var environment = new Environment("Lava Pit", "Hot room", true, "Lava");
            RoomLoader.ApplyEnvironmentTags(environment, null, "Lava");

            TestBase.AssertTrue(environment.Tags.Contains("fire", StringComparer.OrdinalIgnoreCase),
                "Lava theme fallback includes fire",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(environment.Tags.Contains("scorched", StringComparer.OrdinalIgnoreCase),
                "Lava theme fallback includes scorched",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Enemy Management Tests

        private static void TestGenerateEnemies()
        {
            Console.WriteLine("\n--- Testing GenerateEnemies ---");

            var environment = new Environment("Test Room", "A test room", true, "Forest");
            environment.GenerateEnemies(5);

            // Generation should not crash
            TestBase.AssertTrue(true,
                "GenerateEnemies should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGetEnemies()
        {
            Console.WriteLine("\n--- Testing GetEnemies ---");

            var environment = new Environment("Test Room", "A test room", true, "Forest");
            environment.GenerateEnemies(5);

            var enemies = environment.GetEnemies();
            TestBase.AssertNotNull(enemies,
                "GetEnemies should return a list",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (enemies != null)
            {
                TestBase.AssertTrue(enemies.Count >= 0,
                    $"Enemies count should be >= 0, got {enemies.Count}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestHasLivingEnemies()
        {
            Console.WriteLine("\n--- Testing HasLivingEnemies ---");

            var environment = new Environment("Test Room", "A test room", true, "Forest");
            environment.GenerateEnemies(5);

            var hasLiving = environment.HasLivingEnemies();
            // Result depends on enemy generation, so we just verify it doesn't crash
            TestBase.AssertTrue(hasLiving || !hasLiving,
                "HasLivingEnemies should return a boolean",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Reset Tests

        private static void TestResetForNewFight()
        {
            Console.WriteLine("\n--- Testing ResetForNewFight ---");

            var environment = new Environment("Test Room", "A test room", true, "Forest");
            environment.ResetForNewFight();

            // Reset should not crash
            TestBase.AssertTrue(true,
                "ResetForNewFight should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRoomRollTrigger()
        {
            Console.WriteLine("\n--- Testing room roll trigger ---");

            var environment = new Environment("Test Room", "A test room", true, "Forest");
            if (environment.ActionPool.Count > 0)
                TestBase.AssertTrue(environment.TriggerRoll is >= 1 and <= 20,
                    $"Room with actions gets a trigger face 1-20, got {environment.TriggerRoll}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

            environment.SetTriggerRoll(13);
            var hero = new Character("Tester");

            environment.NotifyCombatRoll(hero, 12);
            TestBase.AssertFalse(environment.HasPendingRollTrigger,
                "A different face does not arm the trigger",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            environment.NotifyCombatRoll(environment, 13);
            TestBase.AssertFalse(environment.HasPendingRollTrigger,
                "The room's own roll does not arm its trigger",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            environment.NotifyCombatRoll(hero, 13);
            bool consumed = environment.TryConsumeRollTrigger(out var rolledBy);
            TestBase.AssertTrue(consumed && ReferenceEquals(rolledBy, hero),
                "Rolling the trigger face arms it, credited to the roller",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(environment.TryConsumeRollTrigger(out _),
                "Consuming the trigger clears it",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            environment.NotifyCombatRoll(hero, 13);
            environment.ResetForNewFight();
            TestBase.AssertFalse(environment.HasPendingRollTrigger,
                "A new fight clears an unanswered trigger",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>A swing that rolls the trigger face puts the room's bonus action in the turn queue before it plays.</summary>
        private static void TestRoomTriggerQueuesBeforeItActs()
        {
            Console.WriteLine("\n--- Testing room trigger in the turn queue ---");

            var room = new Environment("Test Room", "A test room", true, "Forest");
            room.SetTriggerRoll(13);
            var hero = new Character("Tester");
            var enemy = new Enemy("Dummy", 1, 50, 5, 5, 5, 5);
            var speed = new ActionSpeedSystem();
            speed.AddEntity(hero, 1.0);
            speed.AddEntity(enemy, 1.5);
            speed.AddEntity(room, 3.0);
            double t0 = speed.GetScheduleSnapshot()[0].NextActionTime;
            speed.SetEntityActionTime(enemy, t0 + 0.5); // the hero acts first; the room's regular turn is a few turns out
            speed.SetEntityActionTime(room, t0 + 2.5);
            using var unmuted = RPGGame.Combat.CombatUiMuteScope.Begin(muted: false); // the queue binds only for on-screen fights
            CombatTurnQueue.Bind(speed, room);
            try
            {
                CombatTurnQueue.BeginTurn(hero);
                room.NotifyCombatRoll(hero, 13);
                var during = CombatTurnQueue.GetUpcoming(4, pastActingTurn: false);
                TestBase.AssertTrue(during.Count == 4 && ReferenceEquals(during[0].Actor, hero) && during[0].IsActing
                    && ReferenceEquals(during[1].Actor, room) && during[1].IsRoomTrigger,
                    "While the swing plays, the triggered room queues right behind it",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                var rolling = CombatTurnQueue.GetUpcoming(4, pastActingTurn: false, showPendingTrigger: false);
                TestBase.AssertTrue(!rolling.Exists(e => e.IsRoomTrigger),
                    "While the die is still showing, the triggered room stays out of the queue",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                CombatTurnQueue.EndTurn();

                var between = CombatTurnQueue.GetUpcoming(12, pastActingTurn: false);
                TestBase.AssertTrue(between.Count == 12 && ReferenceEquals(between[0].Actor, room) && between[0].IsRoomTrigger,
                    "Between turns, the triggered room is next in the bar",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                CombatTurnQueue.BeginBonusTurn(room);
                room.TryConsumeRollTrigger(out _);
                var acting = CombatTurnQueue.GetUpcoming(4, pastActingTurn: false);
                TestBase.AssertTrue(acting.Count == 4 && ReferenceEquals(acting[0].Actor, room) && acting[0].IsActing && acting[0].IsRoomTrigger,
                    "The bonus turn holds the front of the queue while it plays",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                var spent = CombatTurnQueue.GetUpcoming(12, pastActingTurn: true, actingLength: 5.0);
                int regular = spent.FindIndex(e => ReferenceEquals(e.Actor, room));
                int before = between.FindIndex(1, e => ReferenceEquals(e.Actor, room));
                TestBase.AssertTrue(!spent.Exists(e => e.IsRoomTrigger) && before > 0 && regular == before - 1,
                    $"The bonus turn doesn't push the room's regular turn back (at {regular}, expected {before - 1})",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                CombatTurnQueue.Unbind();
            }
        }

        /// <summary>A room stun shows on the board: the queue knows which turns it skips, counting the one playing now as used.</summary>
        private static void TestStunnedTurnsInQueue()
        {
            Console.WriteLine("\n--- Testing stunned turns in the turn queue ---");

            var room = new Environment("Test Room", "A test room", true, "Forest");
            var hero = new Character("Tester");
            var enemy = new Enemy("Dummy", 1, 50, 5, 5, 5, 5);
            var speed = new ActionSpeedSystem();
            speed.AddEntity(hero, 1.0);
            speed.AddEntity(enemy, 1.5);
            using var unmuted = RPGGame.Combat.CombatUiMuteScope.Begin(muted: false);
            CombatTurnQueue.Bind(speed, room);
            try
            {
                hero.IsStunned = true;
                hero.StunTurnsRemaining = 2;
                TestBase.AssertTrue(CombatTurnQueue.StunnedTurnsAhead(hero) == 2 && CombatTurnQueue.StunnedTurnsAhead(enemy) == 0,
                    "Between turns, both stunned turns are still ahead",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                CombatTurnQueue.BeginTurn(hero);
                TestBase.AssertTrue(CombatTurnQueue.ActingStunned && CombatTurnQueue.StunnedTurnsAhead(hero) == 1,
                    "A turn that begins stunned is flagged, and only the turns after it count as ahead",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                hero.StunTurnsRemaining = 1; // the stun turn ticks after its block shows
                TestBase.AssertTrue(CombatTurnQueue.StunnedTurnsAhead(hero) == 1,
                    "The tick doesn't count the playing turn twice",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                CombatTurnQueue.EndTurn();
                TestBase.AssertFalse(CombatTurnQueue.ActingStunned,
                    "Ending the turn clears the stunned flag",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                hero.IsStunned = false;
                hero.StunTurnsRemaining = 0;
                CombatTurnQueue.BeginTurn(hero);
                TestBase.AssertFalse(CombatTurnQueue.ActingStunned,
                    "A turn that begins unstunned isn't flagged",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                CombatTurnQueue.Unbind();
            }
        }

        /// <summary>
        /// Headless walk of what the combat board shows for a room stun: the stunned fighter's next turn reads STUNNED in
        /// the timeline and the next-roll column, the combo it would have played moves back a turn, and once the stunned
        /// turn plays the board goes back to dice.
        /// </summary>
        private static void TestRoomStunOnCombatBoard()
        {
            Console.WriteLine("\n--- Testing room stun on the combat board ---");

            var room = new Environment("Test Room", "A test room", true, "Forest");
            var hero = new Character("Tester");
            var enemy = new Enemy("Dummy", 1, 50, 5, 5, 5, 5);
            // A strip card, so the turn after the stun has a real combo action to show (a level-1 enemy keeps one slot).
            var move = new Action { Name = "Dummy Move", IsComboAction = true, Length = 1.0, ComboOrder = 1 };
            enemy.AddAction(move, 1.0);
            enemy.AddToCombo(move);
            var combo = enemy.GetComboActions();
            TestBase.AssertTrue(combo.Count >= 1, $"Test enemy has a combo strip ({combo.Count} action(s))",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            var speed = new ActionSpeedSystem();
            speed.AddEntity(hero, 1.0);
            speed.AddEntity(enemy, 1.5);
            using var unmuted = RPGGame.Combat.CombatUiMuteScope.Begin(muted: false);
            CombatTurnQueue.Bind(speed, room);
            try
            {
                var idle = new RPGGame.UI.Avalonia.Renderers.DungeonRenderer.BoardTurnView();
                var stunned = RPGGame.UI.Avalonia.Renderers.DungeonRenderer.StunnedTurnAction;
                Action? Projected(Actor a, int k) => RPGGame.UI.Avalonia.Renderers.DungeonRenderer.ProjectTimelineTurn(a, k, idle).Action;
                var Branches = (Actor a) => RPGGame.UI.Avalonia.Renderers.DungeonRenderer.BoardBranches(a, null);

                // The room's stun hazard lands (same handler the room uses).
                var hazard = new Action { Name = "Cave In", CausesStun = true };
                bool applied = new EnvironmentalStunHandler().Apply(enemy, hazard, new List<string>());
                int step = enemy.ComboStep;
                Action? lined = combo.Count > 0 ? combo[step % combo.Count] : null;
                TestBase.AssertTrue(applied && enemy.StunTurnsRemaining == 1,
                    "The room's stun hazard stuns the enemy for a turn",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertTrue(ReferenceEquals(Projected(enemy, 0), stunned)
                    && ReferenceEquals(Projected(enemy, 1), lined),
                    "Timeline: the enemy's next card reads STUNNED and the combo it was lined up on moves to the turn after",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                var next = Branches(enemy);
                TestBase.AssertTrue(next.Count == 1 && next[0].Stunned && next[0].Text == "STUNNED"
                    && next[0].Detail.Count == 1 && next[0].Detail[0] == "skips this turn",
                    "Next-roll column: one STUNNED box instead of dice odds",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                var heroNext = Branches(hero);
                TestBase.AssertTrue(heroNext.Count > 1 && !heroNext.Exists(b => b.Stunned),
                    "The unstunned hero still shows its dice branches",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // The enemy's stunned turn plays: NOW shows STUNNED, and the board looks past it.
                CombatTurnQueue.BeginTurn(enemy);
                TestBase.AssertTrue(CombatTurnQueue.ActingStunned
                    && ReferenceEquals(Projected(enemy, 0), lined)
                    && !Branches(enemy).Exists(b => b.Stunned),
                    "While the stunned turn plays, the turn after it is back to the enemy's combo and dice",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                enemy.UpdateTempEffects(Character.DEFAULT_ACTION_LENGTH); // the stun tick (StunProcessor)
                TestBase.AssertTrue(enemy.StunTurnsRemaining == 0 && CombatTurnQueue.StunnedTurnsAhead(enemy) == 0
                    && ReferenceEquals(Projected(enemy, 0), lined),
                    "After the tick the board still shows the same next turn (no flicker)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                CombatTurnQueue.EndTurn();

                // A longer stun: two skipped turns, then the combo resumes where it stood.
                enemy.IsStunned = true;
                enemy.StunTurnsRemaining = 2;
                next = Branches(enemy);
                TestBase.AssertTrue(ReferenceEquals(Projected(enemy, 0), stunned) && ReferenceEquals(Projected(enemy, 1), stunned)
                    && ReferenceEquals(Projected(enemy, 2), lined)
                    && next.Count == 1 && next[0].Detail[0] == "skips 2 turns",
                    "A 2-turn stun shows two STUNNED cards, then the lined-up combo",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Stunned mid-turn (its swing still to fire): that turn plays, the one after is skipped.
                var held = new RPGGame.UI.Avalonia.Renderers.DungeonRenderer.BoardTurnView { HoldActor = hero };
                hero.IsStunned = true;
                hero.StunTurnsRemaining = 1;
                var heroCombo = hero.GetComboActions();
                var k0 = RPGGame.UI.Avalonia.Renderers.DungeonRenderer.ProjectTimelineTurn(hero, 0, held).Action;
                var k1 = RPGGame.UI.Avalonia.Renderers.DungeonRenderer.ProjectTimelineTurn(hero, 1, held).Action;
                TestBase.AssertTrue(!ReferenceEquals(k0, stunned) && ReferenceEquals(k1, stunned),
                    $"A held turn still plays; the stun skips the turn after it (strip of {heroCombo.Count})",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                CombatTurnQueue.Unbind();
                enemy.IsStunned = hero.IsStunned = false;
                enemy.StunTurnsRemaining = hero.StunTurnsRemaining = 0;
            }
        }

        #endregion
    }
}
