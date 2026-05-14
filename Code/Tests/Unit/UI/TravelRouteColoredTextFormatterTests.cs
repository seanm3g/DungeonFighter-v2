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

            TestTravelStepPlaintext();
            TestOutcomeLabels();
            TestSummaryConcatenation();

            TestBase.PrintSummary("TravelRouteColoredTextFormatter Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static string Flatten(IReadOnlyList<ColoredText> segments) =>
            string.Concat(segments.Select(s => s.Text));

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
            TestBase.AssertTrue(
                text.Contains("2. Clear Animal Trail - A game trail cuts cleanly.") &&
                text.Contains("\n     d20 19 Combo (4 min)"),
                "Travel step: title line then indented roll line",
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
