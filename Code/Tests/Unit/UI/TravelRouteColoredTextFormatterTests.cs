using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.UI
{
    public static class TravelRouteColoredTextFormatterTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== TravelRouteColoredTextFormatter Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestHeaderWithDice();
            TestHeaderWithoutDice();
            TestTravelStepPlaintext();
            TestOutcomeLabels();
            TestSummaryConcatenation();

            TestBase.PrintSummary("TravelRouteColoredTextFormatter Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static string Flatten(IReadOnlyList<ColoredText> segments) =>
            string.Concat(segments.Select(s => s.Text));

        private static void TestHeaderWithDice()
        {
            var route = new TravelRouteResult
            {
                EventCount = 10,
                EventCountDice = new[] { 3, 3, 3, 1 }
            };
            var segs = TravelRouteColoredTextFormatter.FormatEventCountHeader(route);
            var text = Flatten(segs);
            TestBase.AssertTrue(text == "Travel events (4d4): 3+3+3+1 = 10",
                "Header with 4d4 dice matches expected plaintext",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHeaderWithoutDice()
        {
            var route = new TravelRouteResult { EventCount = 7, EventCountDice = System.Array.Empty<int>() };
            var text = Flatten(TravelRouteColoredTextFormatter.FormatEventCountHeader(route));
            TestBase.AssertTrue(text == "Travel events: 7",
                "Header without dice uses compact form",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestTravelStepPlaintext()
        {
            var step = new TravelStepResult
            {
                StepNumber = 2,
                Roll = 19,
                Outcome = TravelRollOutcome.Combo,
                TravelMinutes = 4,
                Event = new TravelEvent { Title = "Clear Animal Trail", Narrative = "A game trail cuts cleanly." }
            };
            var text = Flatten(TravelRouteColoredTextFormatter.FormatTravelStep(step));
            TestBase.AssertTrue(text.Contains("2. ") && text.Contains("d20 19") && text.Contains("Combo") &&
                                 text.Contains("Clear Animal Trail") && text.Contains("A game trail cuts cleanly."),
                "Travel step segments concatenate to readable log line",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestOutcomeLabels()
        {
            TestBase.AssertTrue(
                TravelRouteColoredTextFormatter.FormatOutcomeLabel(TravelRollOutcome.CriticalMiss) == "Critical miss",
                "Critical miss label",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                TravelRouteColoredTextFormatter.FormatOutcomeLabel(TravelRollOutcome.Critical) == "Critical",
                "Critical label",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSummaryConcatenation()
        {
            var route = new TravelRouteResult
            {
                Steps = new List<TravelStepResult>
                {
                    new TravelStepResult { ProgressDelta = 5, TravelMinutes = 70, XpGained = 4 }
                },
                LootFound = new List<Item> { new Item(ItemType.Weapon, "Loot") }
            };
            var text = Flatten(TravelRouteColoredTextFormatter.FormatRouteSummary(route));
            TestBase.AssertTrue(text.Contains("Progress +5") && text.Contains("1 hr 10 min") && text.Contains("XP +4") &&
                                 text.Contains("loot 1"),
                "Summary includes progress, time, XP, and loot",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
