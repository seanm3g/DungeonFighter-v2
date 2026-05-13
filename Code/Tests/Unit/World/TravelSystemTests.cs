using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
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
            TestTravelPacingMakesGoodRollsFaster();
            TestRegionCatalogLoadsThreeRegions();
            TestTravelEventCatalogLoadsOneHundredEvents();
            TestRouteGenerationCreatesScriptedStepCountAndMovesRegion();
            TestRouteGenerationSumsTravelTimeFromSteps();
            TestTravelTimeMultiplierScalesRouteTotals();
            TestCriticalMissTravelCannotKillCharacter();
            TestTravelEventCountRollIsFourD4Sum();
            TestClampTravelEventCount();
            TestCreateRouteResultRollsThreeJourneyThemesFromDestinationPool();

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

        private static void TestTravelPacingMakesGoodRollsFaster()
        {
            TestBase.AssertTrue(
                TravelPacing.GetDelayMs(TravelRollOutcome.Critical) < TravelPacing.GetDelayMs(TravelRollOutcome.Combo) &&
                TravelPacing.GetDelayMs(TravelRollOutcome.Combo) < TravelPacing.GetDelayMs(TravelRollOutcome.Hit) &&
                TravelPacing.GetDelayMs(TravelRollOutcome.Hit) < TravelPacing.GetDelayMs(TravelRollOutcome.Miss) &&
                TravelPacing.GetDelayMs(TravelRollOutcome.Miss) < TravelPacing.GetDelayMs(TravelRollOutcome.CriticalMiss),
                "Travel delays should get slower as roll outcomes get worse",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(
                TravelPacing.GetTravelMinutes(TravelRollOutcome.Critical) < TravelPacing.GetTravelMinutes(TravelRollOutcome.Combo) &&
                TravelPacing.GetTravelMinutes(TravelRollOutcome.Combo) < TravelPacing.GetTravelMinutes(TravelRollOutcome.Hit) &&
                TravelPacing.GetTravelMinutes(TravelRollOutcome.Hit) < TravelPacing.GetTravelMinutes(TravelRollOutcome.Miss) &&
                TravelPacing.GetTravelMinutes(TravelRollOutcome.Miss) < TravelPacing.GetTravelMinutes(TravelRollOutcome.CriticalMiss),
                "Travel time should get longer as roll outcomes get worse",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
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
                "Region catalog should include Forest, Lava, and Crypt primary themes",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            foreach (var region in regions)
            {
                var pool = region.ResolveLinkedDungeonThemePool();
                TestBase.AssertEqual(3, pool.Count,
                    $"Region '{region.Id}' should define exactly three linked dungeon themes",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            var forest = catalog.GetById("forest");
            var lava = catalog.GetById("lava");
            var crypt = catalog.GetById("crypt");
            TestBase.AssertTrue(forest != null && lava != null && crypt != null,
                "Region catalog should include forest, lava, and crypt ids",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            CollectionContainsThemes(forest!.ResolveLinkedDungeonThemePool(), new[] { "Forest", "Crypt", "Sky" },
                "forest region pool", ref _testsRun, ref _testsPassed, ref _testsFailed);
            CollectionContainsThemes(lava!.ResolveLinkedDungeonThemePool(), new[] { "Lava", "Ice", "Sky" },
                "lava region pool", ref _testsRun, ref _testsPassed, ref _testsFailed);
            CollectionContainsThemes(crypt!.ResolveLinkedDungeonThemePool(), new[] { "Crypt", "Dark", "Forest" },
                "crypt region pool", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void CollectionContainsThemes(
            IReadOnlyList<string> pool,
            string[] expectedSet,
            string label,
            ref int testsRun,
            ref int testsPassed,
            ref int testsFailed)
        {
            foreach (var theme in pool)
            {
                TestBase.AssertTrue(
                    expectedSet.Contains(theme, StringComparer.OrdinalIgnoreCase),
                    $"{label} should only contain configured dungeon themes (got '{theme}')",
                    ref testsRun, ref testsPassed, ref testsFailed);
            }
        }

        private static void TestCreateRouteResultRollsThreeJourneyThemesFromDestinationPool()
        {
            var catalog = new TravelRegionCatalog();
            var generator = new TravelRouteGenerator(catalog, new TravelEventCatalog());
            var forest = catalog.GetById("forest");
            TestBase.AssertTrue(forest != null, "forest region should exist", ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (forest == null)
                return;

            var character = TestDataBuilders.Character().WithName("JourneyTester").WithLevel(5).Build();
            character.CurrentRegionId = "lava";

            for (int i = 0; i < 30; i++)
            {
                var result = generator.CreateRouteResult(character, "forest");
                TestBase.AssertEqual(TravelRouteGenerator.JourneyThemePickCount, result.JourneyDungeonThemes.Count,
                    "Each route should roll exactly three journey dungeon themes",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                foreach (var theme in result.JourneyDungeonThemes)
                {
                    TestBase.AssertTrue(
                        forest.ResolveLinkedDungeonThemePool().Any(t => string.Equals(t, theme, StringComparison.OrdinalIgnoreCase)),
                        $"Journey theme '{theme}' should be drawn from the destination pool",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
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

        private static void TestRouteGenerationCreatesScriptedStepCountAndMovesRegion()
        {
            const int scriptedSteps = 10;
            var character = TestDataBuilders.Character().WithName("Traveler").WithLevel(5).Build();
            character.CurrentRegionId = "forest";
            var generator = new TravelRouteGenerator();

            var result = generator.GenerateRoute(
                character,
                "lava",
                Enumerable.Repeat(6, scriptedSteps).ToList(),
                scriptedEventCount: scriptedSteps);

            TestBase.AssertEqual(scriptedSteps, result.EventCount,
                "Scripted travel event count should be stored on the route",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(scriptedSteps, result.Steps.Count,
                "Travel route should generate one step per scripted travel event",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("lava", character.CurrentRegionId,
                "Travel route should update the character's current region",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(result.Steps.All(step => step.Outcome == TravelRollOutcome.Hit),
                "Scripted hit rolls should produce hit travel steps",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRouteGenerationSumsTravelTimeFromSteps()
        {
            const int scriptedSteps = 10;
            var character = TestDataBuilders.Character().WithName("TimedTraveler").WithLevel(5).Build();
            character.CurrentRegionId = "forest";
            var generator = new TravelRouteGenerator();
            var scriptedRolls = new[] { 20, 14, 6, 5, 1, 20, 14, 6, 5, 1 };

            var result = generator.GenerateRoute(character, "crypt", scriptedRolls, scriptedEventCount: scriptedSteps);

            TestBase.AssertEqual(result.Steps.Sum(step => step.DelayMs), result.TotalDelayMs,
                "Route total delay should sum the generated step delays",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(result.Steps.Sum(step => step.TravelMinutes), result.TotalTravelMinutes,
                "Route total travel time should sum the generated step travel minutes",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(result.IsComplete,
                "Generated routes should be marked complete after all steps",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestTravelTimeMultiplierScalesRouteTotals()
        {
            double prev = GameSettings.Instance.TravelTimeMultiplier;
            try
            {
                const int scriptedSteps = 10;
                var rolls = Enumerable.Repeat(10, scriptedSteps).ToList();
                var generator = new TravelRouteGenerator();

                GameSettings.Instance.TravelTimeMultiplier = 1.0;
                GameSettings.Instance.ValidateAndFix();
                var c1 = TestDataBuilders.Character().WithName("ScaleA").WithLevel(5).Build();
                c1.CurrentRegionId = "forest";
                var r1 = generator.GenerateRoute(c1, "lava", rolls, scriptedEventCount: scriptedSteps);

                GameSettings.Instance.TravelTimeMultiplier = 2.0;
                GameSettings.Instance.ValidateAndFix();
                var c2 = TestDataBuilders.Character().WithName("ScaleB").WithLevel(5).Build();
                c2.CurrentRegionId = "forest";
                var r2 = generator.GenerateRoute(c2, "lava", rolls, scriptedEventCount: scriptedSteps);

                TestBase.AssertEqual(r1.TotalDelayMs * 2, r2.TotalDelayMs,
                    "Doubling travel time multiplier should double total travel step delay",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(r1.TotalTravelMinutes * 2, r2.TotalTravelMinutes,
                    "Doubling travel time multiplier should double total travel minutes",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                GameSettings.Instance.TravelTimeMultiplier = prev;
                GameSettings.Instance.ValidateAndFix();
            }
        }

        private static void TestCriticalMissTravelCannotKillCharacter()
        {
            var character = TestDataBuilders.Character().WithName("UnluckyTraveler").WithLevel(1).Build();
            character.CurrentRegionId = "forest";
            character.CurrentHealth = 3;
            var generator = new TravelRouteGenerator();

            const int scriptedSteps = 10;
            generator.GenerateRoute(character, "crypt", Enumerable.Repeat(1, scriptedSteps).ToList(), scriptedEventCount: scriptedSteps);

            TestBase.AssertTrue(character.CurrentHealth >= 1,
                "Travel critical misses should not kill the character",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestTravelEventCountRollIsFourD4Sum()
        {
            Dice.SetTestRoll(3);
            try
            {
                int total = TravelRouteGenerator.RollTravelEventDice(out int[] dice);
                TestBase.AssertEqual(TravelRouteGenerator.TravelEventCountDice, dice.Length,
                    "4d4 should produce four die results",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(12, total,
                    "With test d4 fixed at 3, 4d4 should sum to 12",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(dice.All(face => face == 3),
                    "Each d4 face should match the forced test roll",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                Dice.ClearTestRoll();
            }
        }

        private static void TestClampTravelEventCount()
        {
            TestBase.AssertEqual(TravelRouteGenerator.MinTravelEvents, TravelRouteGenerator.ClampTravelEventCount(1),
                "Travel event count should clamp up to the 4d4 minimum",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(TravelRouteGenerator.MaxTravelEvents, TravelRouteGenerator.ClampTravelEventCount(99),
                "Travel event count should clamp down to the 4d4 maximum",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
