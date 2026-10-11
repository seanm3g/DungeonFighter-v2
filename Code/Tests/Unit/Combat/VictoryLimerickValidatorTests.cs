using System;
using RPGGame.Combat.Narrative;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>Tests for normalizing model output into a 5-line victory limerick.</summary>
    public static class VictoryLimerickValidatorTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== VictoryLimerickValidator Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;

            TestNormalizeAcceptsFiveCleanLines();
            TestNormalizeRejectsWrongLineCount();
            TestNormalizeRejectsDigitHeavyLines();
            TestNormalizeStripsQuotesAndListPrefixes();
            TestNormalizeSkipsMetaHeader();
            TestNormalizeRejectsHeroNameTokens();
            TestSplitToDisplayLines();

            TestBase.PrintSummary("VictoryLimerickValidator Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestNormalizeAcceptsFiveCleanLines()
        {
            Console.WriteLine("--- Normalize accepts five clean lines ---");
            string raw =
                "There once was the tara-weaving wanderer\n" +
                "who fought through a crypt with a plan\n" +
                "The Wight took a swing\n" +
                "but steel had its sting\n" +
                "and silence returned to the land.";

            bool ok = VictoryLimerickValidator.TryNormalize(raw, out string limerick);
            TestBase.AssertTrue(ok, "valid limerick should normalize", ref _testsRun, ref _testsPassed, ref _testsFailed);
            var lines = VictoryLimerickValidator.SplitToDisplayLines(limerick);
            TestBase.AssertEqual(5, lines.Count, "normalized limerick has 5 lines",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestNormalizeRejectsWrongLineCount()
        {
            Console.WriteLine("--- Normalize rejects wrong line count ---");
            bool ok = VictoryLimerickValidator.TryNormalize("one\ntwo\nthree", out _);
            TestBase.AssertFalse(ok, "three lines should fail", ref _testsRun, ref _testsPassed, ref _testsFailed);

            ok = VictoryLimerickValidator.TryNormalize(null, out _);
            TestBase.AssertFalse(ok, "null should fail", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestNormalizeRejectsDigitHeavyLines()
        {
            Console.WriteLine("--- Normalize rejects digit-heavy lines ---");
            string raw =
                "There once was the tara-weaving wanderer\n" +
                "who dealt damage for 999\n" +
                "The Wight took a swing\n" +
                "but steel had its sting\n" +
                "and silence returned to the land.";

            bool ok = VictoryLimerickValidator.TryNormalize(raw, out _);
            TestBase.AssertFalse(ok, "line with many digits should fail",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestNormalizeStripsQuotesAndListPrefixes()
        {
            Console.WriteLine("--- Normalize strips quotes and list prefixes ---");
            string raw =
                "\"There once was the tara-weaving wanderer\"\n" +
                "- who fought through a crypt with a plan\n" +
                "* The Wight took a swing\n" +
                "1. but steel had its sting\n" +
                "and silence returned to the land.";

            bool ok = VictoryLimerickValidator.TryNormalize(raw, out string limerick);
            TestBase.AssertTrue(ok, "decorated lines should normalize",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(limerick.Contains("There once was the tara-weaving wanderer", StringComparison.Ordinal),
                "quotes stripped from first line",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(limerick.Contains("- who", StringComparison.Ordinal),
                "dash prefix stripped",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestNormalizeSkipsMetaHeader()
        {
            Console.WriteLine("--- Normalize skips meta header ---");
            string raw =
                "Here is a limerick:\n" +
                "There once was the tara-weaving wanderer\n" +
                "who fought through a crypt with a plan\n" +
                "The Wight took a swing\n" +
                "but steel had its sting\n" +
                "and silence returned to the land.";

            bool ok = VictoryLimerickValidator.TryNormalize(raw, out string limerick);
            TestBase.AssertTrue(ok, "meta header skipped so five verse lines remain",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(limerick.Contains("Here is a limerick", StringComparison.OrdinalIgnoreCase),
                "meta line not in result",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestNormalizeRejectsHeroNameTokens()
        {
            Console.WriteLine("--- Normalize rejects hero name tokens ---");
            string raw =
                "There once was a hero named Taran\n" +
                "who fought through a crypt with a plan\n" +
                "The Wight took a swing\n" +
                "but steel had its sting\n" +
                "and silence returned to the land.";

            bool ok = VictoryLimerickValidator.TryNormalize(raw, "Taran", out _);
            TestBase.AssertFalse(ok, "literal hero name should fail when heroName provided",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            string clean =
                "There once was the tara-weaving wanderer\n" +
                "who fought through a crypt with a plan\n" +
                "The Wight took a swing\n" +
                "but steel had its sting\n" +
                "and silence returned to the land.";
            ok = VictoryLimerickValidator.TryNormalize(clean, "Taran", out string limerick);
            TestBase.AssertTrue(ok, "epithet verse accepted with heroName check",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(limerick.Contains("Taran", StringComparison.OrdinalIgnoreCase),
                "accepted verse has no hero name",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSplitToDisplayLines()
        {
            Console.WriteLine("--- SplitToDisplayLines ---");
            var lines = VictoryLimerickValidator.SplitToDisplayLines("a\n\nb\nc");
            TestBase.AssertEqual(3, lines.Count, "blank lines dropped",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, VictoryLimerickValidator.SplitToDisplayLines("").Count,
                "empty input yields no lines",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
