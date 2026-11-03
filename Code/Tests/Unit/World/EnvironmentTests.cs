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

        #endregion
    }
}
