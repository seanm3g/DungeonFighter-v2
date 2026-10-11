using System;
using RPGGame.Tests;
using RPGGame.World.DungeonMap;

namespace RPGGame.Tests.Unit.World
{
    public static class DungeonTraversalStrategyTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Dungeon Traversal Strategy Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestResolverSmartWhenTotalMeetsThreshold();
            TestResolverRandomWhenTotalBelowThreshold();
            TestSmartStepperMovesTowardTarget();
            TestRandomStepperStaysOnFloor();
            TestNavigatorReachesContent();
            TestExploreOutcomeBands();

            TestBase.PrintSummary("Dungeon Traversal Strategy Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestResolverSmartWhenTotalMeetsThreshold()
        {
            var hero = new Character("IntHero", 1);
            hero.Intelligence = 10;
            Dice.SetTestRoll(5); // 5+10 = 15
            try
            {
                var result = TraversalStrategyResolver.Resolve(hero);
                TestBase.AssertEqualEnum(TraversalStrategy.Smart, result.Strategy,
                    "5+10 should be Smart", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(15, result.Total, "Total 15", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                Dice.SetTestRoll(null);
            }
        }

        private static void TestResolverRandomWhenTotalBelowThreshold()
        {
            var hero = new Character("LowInt", 1);
            hero.Intelligence = 1;
            Dice.SetTestRoll(10); // 10+1 = 11
            try
            {
                var result = TraversalStrategyResolver.Resolve(hero);
                TestBase.AssertEqualEnum(TraversalStrategy.Random, result.Strategy,
                    "10+1 should be Random", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                Dice.SetTestRoll(null);
            }
        }

        private static void TestSmartStepperMovesTowardTarget()
        {
            var room = new Environment("C", "d", true, "Generic");
            var state = DungeonMapGenerator.Generate(new[] { room }, new Random(1));
            state.TryGetNextContentTarget(out int tx, out int ty);

            var stepper = new SmartPathStepper();
            int before = Manhattan(state.HeroX, state.HeroY, tx, ty);
            if (before == 0)
            {
                // Move start away for the test
                foreach (var (nx, ny) in SmartPathStepper.GetWalkableNeighbors(state, state.HeroX, state.HeroY))
                {
                    if (nx != tx || ny != ty)
                    {
                        state.MoveHeroTo(nx, ny);
                        break;
                    }
                }
                before = Manhattan(state.HeroX, state.HeroY, tx, ty);
            }

            TestBase.AssertTrue(stepper.TryGetNextStep(state, tx, ty, out int nx2, out int ny2),
                "Smart finds a step", ref _testsRun, ref _testsPassed, ref _testsFailed);
            int after = Manhattan(nx2, ny2, tx, ty);
            TestBase.AssertTrue(after < before || before == 0,
                "Smart reduces Manhattan (or already there)", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRandomStepperStaysOnFloor()
        {
            var rooms = new[]
            {
                new Environment("A", "d", true, "Generic"),
                new Environment("B", "d", true, "Generic")
            };
            var state = DungeonMapGenerator.Generate(rooms, new Random(3));
            state.TryGetNextContentTarget(out int tx, out int ty);
            var stepper = new RandomPathStepper(new Random(3));

            TestBase.AssertTrue(stepper.TryGetNextStep(state, tx, ty, out int nx, out int ny),
                "Random finds a step", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(state.GetCell(nx, ny).IsWalkable,
                "Random step is walkable", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestNavigatorReachesContent()
        {
            var rooms = new[]
            {
                new Environment("A", "d", true, "Generic"),
                new Environment("B", "d", true, "Generic")
            };
            var state = DungeonMapGenerator.Generate(rooms, new Random(5));
            state.CurrentStrategy = TraversalStrategy.Smart;
            state.NextContentIndex = 0;

            // Production dungeon entry walks with onStep: null — no per-step HUD paint
            // (full RenderRoomEntry each step flickered the right LOCATION/ENEMY panel).
            bool reached = DungeonTraversalNavigator.WalkToNextContentAsync(state, onStep: null)
                .GetAwaiter().GetResult();
            TestBase.AssertTrue(reached, "Navigator reaches first content without step UI callbacks",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(state.InteractionCell.HasValue, "Interaction set on arrival",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestExploreOutcomeBands()
        {
            TestBase.AssertEqualEnum(ExplorationOutcome.SpotEnemyEarly,
                ExplorationManager.ResolveOutcomeForStrategy(15, TraversalStrategy.Smart),
                "Smart 15 spots early", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqualEnum(ExplorationOutcome.FindNothing,
                ExplorationManager.ResolveOutcomeForStrategy(1, TraversalStrategy.Smart),
                "Smart 1 find nothing", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqualEnum(ExplorationOutcome.EnvironmentalHazard,
                ExplorationManager.ResolveOutcomeForStrategy(15, TraversalStrategy.Random),
                "Random 15 hazard", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqualEnum(ExplorationOutcome.SpotEnemyEarly,
                ExplorationManager.ResolveOutcomeForStrategy(20, TraversalStrategy.Random),
                "Random 20 spots", ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Smart FindNothing never surprises
            var hostile = new Environment("H", "d", true, "Generic");
            hostile.GetEnemies().Add(new Enemy("E", 1, 10, 1, 0, 1.0, PrimaryAttribute.Strength, true, EnemyArchetype.Knight, useDirectStats: true));
            Dice.SetTestRoll(1);
            try
            {
                var mgr = new ExplorationManager();
                var smart = mgr.ExploreRoom(hostile, new Character("P", 1), TraversalStrategy.Smart);
                TestBase.AssertFalse(smart.IsSurprised, "Smart find-nothing no surprise", ref _testsRun, ref _testsPassed, ref _testsFailed);

                Dice.SetTestRoll(1);
                var random = mgr.ExploreRoom(hostile, new Character("P", 1), TraversalStrategy.Random);
                TestBase.AssertTrue(random.IsSurprised, "Random find-nothing surprises", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                Dice.SetTestRoll(null);
            }
        }

        private static int Manhattan(int x1, int y1, int x2, int y2) =>
            Math.Abs(x1 - x2) + Math.Abs(y1 - y2);
    }
}
