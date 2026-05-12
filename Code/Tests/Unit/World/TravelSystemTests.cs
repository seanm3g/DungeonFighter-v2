using System;
using System.Linq;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.World
{
    public static class TravelSystemTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Travel System Tests ===\n");

            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestRollOutcomeBands();
            TestRegionCatalogLoadsThreeRegions();
            TestTravelEventCatalogLoadsOneHundredEvents();
            TestRouteGenerationCreatesTenStepsAndMovesRegion();
            TestCriticalMissTravelCannotKillCharacter();

            TestBase.PrintSummary("Travel System Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestRollOutcomeBands()
        {
            TestBase.AssertEqualEnum(TravelRollOutcome.CriticalMiss, TravelRollResolver.Resolve(1),
                "Roll 1 should be critical miss", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqualEnum(TravelRollOutcome.Miss, TravelRollResolver.Resolve(5),
                "Roll 5 should be miss", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqualEnum(TravelRollOutcome.Hit, TravelRollResolver.Resolve(13),
                "Roll 13 should be hit", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqualEnum(TravelRollOutcome.Combo, TravelRollResolver.Resolve(19),
                "Roll 19 should be combo", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqualEnum(TravelRollOutcome.Critical, TravelRollResolver.Resolve(20),
                "Roll 20 should be critical", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRegionCatalogLoadsThreeRegions()
        {
            var catalog = new TravelRegionCatalog();
            var regions = catalog.GetAllRegions();

            TestBase.AssertEqual(3, regions.Count,
                "Region catalog should load exactly three regions",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(regions.Any(region => region.Theme == "Forest") &&
                                regions.Any(region => region.Theme == "Lava") &&
                                regions.Any(region => region.Theme == "Crypt"),
                "Region catalog should include Forest, Lava, and Crypt themes",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestTravelEventCatalogLoadsOneHundredEvents()
        {
            var catalog = new TravelEventCatalog();
            var events = catalog.GetAllEvents();
            var counts = catalog.CountByOutcome();

            TestBase.AssertEqual(100, events.Count,
                "Travel event catalog should load 100 events",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            foreach (var outcome in Enum.GetValues<TravelRollOutcome>())
            {
                TestBase.AssertEqual(20, counts[outcome],
                    $"Travel event catalog should include 20 {outcome} events",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestRouteGenerationCreatesTenStepsAndMovesRegion()
        {
            var character = TestDataBuilders.Character().WithName("Traveler").WithLevel(5).Build();
            character.CurrentRegionId = "forest";
            var generator = new TravelRouteGenerator();

            var result = generator.GenerateRoute(character, "lava", Enumerable.Repeat(6, TravelRouteGenerator.RouteStepCount).ToList());

            TestBase.AssertEqual(TravelRouteGenerator.RouteStepCount, result.Steps.Count,
                "Travel route should generate exactly ten steps",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("lava", character.CurrentRegionId,
                "Travel route should update the character's current region",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(result.Steps.All(step => step.Outcome == TravelRollOutcome.Hit),
                "Scripted hit rolls should produce hit travel steps",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCriticalMissTravelCannotKillCharacter()
        {
            var character = TestDataBuilders.Character().WithName("UnluckyTraveler").WithLevel(1).Build();
            character.CurrentRegionId = "forest";
            character.CurrentHealth = 3;
            var generator = new TravelRouteGenerator();

            generator.GenerateRoute(character, "crypt", Enumerable.Repeat(1, TravelRouteGenerator.RouteStepCount).ToList());

            TestBase.AssertTrue(character.CurrentHealth >= 1,
                "Travel critical misses should not kill the character",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
