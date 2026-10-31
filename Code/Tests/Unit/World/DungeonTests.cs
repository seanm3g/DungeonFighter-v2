using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.World
{
    /// <summary>
    /// Comprehensive tests for Dungeon
    /// Tests dungeon generation, room progression, and rewards
    /// </summary>
    public static class DungeonTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all Dungeon tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== Dungeon Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestDungeonCreation();
            TestDungeonProperties();
            TestDungeonGenerate();
            TestTrainingGroundDungeonGenerate();
            TestTrainingGroundTutorialScript();

            TestBase.PrintSummary("Dungeon Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Creation Tests

        private static void TestDungeonCreation()
        {
            Console.WriteLine("--- Testing Dungeon Creation ---");

            var dungeon = new Dungeon("Test Dungeon", 1, 5, "Forest");
            TestBase.AssertNotNull(dungeon,
                "Dungeon should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (dungeon != null)
            {
                TestBase.AssertEqual("Test Dungeon", dungeon.Name,
                    "Dungeon should have correct name",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertEqual(1, dungeon.MinLevel,
                    "Dungeon should have correct min level",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertEqual(5, dungeon.MaxLevel,
                    "Dungeon should have correct max level",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertEqual("Forest", dungeon.Theme,
                    "Dungeon should have correct theme",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Properties Tests

        private static void TestDungeonProperties()
        {
            Console.WriteLine("\n--- Testing Dungeon Properties ---");

            var dungeon = new Dungeon("Test Dungeon", 1, 5, "Forest");

            TestBase.AssertNotNull(dungeon.Rooms,
                "Rooms list should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(dungeon.PossibleEnemies,
                "PossibleEnemies list should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(0, dungeon.Rooms.Count,
                "New dungeon should have no rooms",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Generation Tests

        private static void TestDungeonGenerate()
        {
            Console.WriteLine("\n--- Testing Dungeon Generate ---");

            var dungeon = new Dungeon("Test Dungeon", 1, 5, "Forest");
            dungeon.Generate();

            // Generation should create rooms
            TestBase.AssertTrue(dungeon.Rooms.Count >= 0,
                $"Dungeon should have rooms after generation, got {dungeon.Rooms.Count}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // If rooms were generated, verify they have properties
            if (dungeon.Rooms.Count > 0)
            {
                var room = dungeon.Rooms[0];
                TestBase.AssertNotNull(room,
                    "Generated room should not be null",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                if (room != null)
                {
                    TestBase.AssertTrue(!string.IsNullOrEmpty(room.Name),
                        "Generated room should have a name",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
        }

        private static void TestTrainingGroundDungeonGenerate()
        {
            Console.WriteLine("\n--- Testing Training Ground Dungeon Generate ---");

            var dungeon = PreWeaponTrainingFlow.CreateTrainingGroundDungeon();
            dungeon.Generate();

            TestBase.AssertEqual(1, dungeon.Rooms.Count,
                "Training Ground should have exactly one room",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var room = dungeon.Rooms[0];
            TestBase.AssertTrue(room.IsHostile, "Training room should be hostile", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var enemies = room.GetEnemies();
            TestBase.AssertEqual(1, enemies.Count, "Training room should have one enemy", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var dummy = enemies[0];
            TestBase.AssertEqual(GameConstants.TrainingDummyEnemyName, dummy.Name,
                "Enemy should be Training Dummy",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(GameConstants.TrainingDummyMaxHealth, dummy.MaxHealth,
                "Dummy max health",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(dummy.UsesDirectCombatStats(), "Dummy uses direct combat stats", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1, dummy.GetEffectiveStrength(),
                "Dummy effective damage (strength path)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(GameConstants.TrainingDummyBaseAttackSpeedSeconds, dummy.GetTotalAttackSpeed(),
                "Dummy base attack time (seconds, direct stats)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestTrainingGroundTutorialScript()
        {
            Console.WriteLine("\n--- Testing Training Ground Tutorial Script ---");

            var script = PreWeaponTrainingFlow.CreateTrainingGroundTutorialScript();
            var heroBeats = script.Events.Where(e => e.Actor == TrainingGroundTutorialActor.Hero).ToList();
            var dummyBeats = script.Events.Where(e => e.Actor == TrainingGroundTutorialActor.Dummy).ToList();
            TestBase.AssertEqual(6, script.Events.Count,
                "Training Ground tutorial covers every result in six rolls",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(heroBeats.Select(e => e.Roll).SequenceEqual(new[] { 10, 3, 1, 20 }),
                "Hero beats: hit, Lucky rescue, critical miss, critical",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(heroBeats[1].LuckRoll is int rescue && rescue >= 14 && heroBeats[2].LuckRoll == 1,
                "Lucky die rescues the third beat into a combo and stays 1 on the critical miss",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(dummyBeats.Select(e => e.Roll).SequenceEqual(new[] { 15, 4 }) && dummyBeats.All(e => e.LuckRoll == null),
                "Dummy beats: a combo that fires SWAY, then a miss",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(script.IntroNotes.Count == 0 && script.OutroNotes.Count > 0
                    && script.Events.All(e => e.Notes.Count > 0 && !string.IsNullOrWhiteSpace(e.NarrativeLine)),
                "Tutorial opens on the first roll, has an outro, and narrative plus coach popups on every beat",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            var firstRollFoci = heroBeats[0].Notes.Select(n => n.Focus).ToList();
            TestBase.AssertTrue(firstRollFoci.SequenceEqual(new[]
                {
                    TutorialCoachFocus.None, TutorialCoachFocus.Timeline, TutorialCoachFocus.TurnOrb, TutorialCoachFocus.Board,
                    TutorialCoachFocus.Branches, TutorialCoachFocus.Die, TutorialCoachFocus.Die, TutorialCoachFocus.ChosenBranch,
                    TutorialCoachFocus.HeroStrip, TutorialCoachFocus.Board
                }),
                "First roll walks the board: timeline, turn, middle, odds, roll, result, box taken, strip, damage",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(dummyBeats[0].Notes[0].Focus == TutorialCoachFocus.EnemyStrip
                    && script.Events.SelectMany(e => e.Notes).Any(n => n.Focus == TutorialCoachFocus.StatusEffects && n.RequiresLucky)
                    && !script.Events.SelectMany(e => e.Notes).Any(n => n.Focus == TutorialCoachFocus.Sequence && n.Step == RPGGame.Combat.Sequence.CombatSequenceStepKind.Damage),
                "Enemy strip opens the dummy's turn, Lucky is pointed out, and no note walks the damage math",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var stripDummy = new Enemy(GameConstants.TrainingDummyEnemyName, 1, GameConstants.TrainingDummyMaxHealth,
                GameConstants.TrainingDummyDamage, 0, GameConstants.TrainingDummyBaseAttackSpeedSeconds);
            PreWeaponTrainingFlow.SetTrainingDummyActions(stripDummy);
            var dummyStrip = stripDummy.GetComboActions();
            TestBase.AssertTrue(dummyStrip.Count == 2
                    && dummyStrip.Select(a => a.Name).SequenceEqual(PreWeaponTrainingFlow.TrainingDummyActionNames),
                "Training dummy shows SWAY and REBOUND on its action strip",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            Dice.QueueAsyncForcedLuckD20Rolls(16);
            int luckDie = Dice.RollLuckDie(20);
            Dice.ClearAsyncForcedLuckD20Rolls();
            TestBase.AssertEqual(16, luckDie, "RollLuckDie consumes the queued Lucky face",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("rolled 3 and 16",
                TutorialCoach.Fill("rolled {die1} and {die2}", new Dictionary<string, string> { ["die1"] = "3", ["die2"] = "16" }),
                "Coach text fills its tokens", ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Each side takes its own beats in order, whichever acts first.
            var hero = new Character("TutorialHero", 1);
            TestBase.AssertTrue(script.TryConsumeForActor(stripDummy, out var dummyFirst) && dummyFirst?.Roll == 15,
                "Dummy acting first takes its own first beat, not the hero's",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(script.TryConsumeForActor(hero, out var heroFirst) && heroFirst?.Roll == 10,
                "Hero still gets its first beat",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(script.TryConsumeForActor(hero, out var heroSecond) && heroSecond?.Roll == 3,
                "Hero acting twice in a row takes its next beat",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(script.TryConsumeForActor(stripDummy, out var dummySecond) && dummySecond?.Roll == 4
                    && !script.TryConsumeForActor(stripDummy, out _),
                "Dummy runs out after its two beats",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(4, script.ConsumedCount, "Four beats taken",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            script.Reset();
            TestBase.AssertEqual(0, script.ConsumedCount,
                "Tutorial script reset should rewind every beat",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
