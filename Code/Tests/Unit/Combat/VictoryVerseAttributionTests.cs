using System;
using System.Text.RegularExpressions;
using RPGGame.Combat.Narrative;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>Tests for victory-verse artist credit formatting.</summary>
    public static class VictoryVerseAttributionTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        private static readonly Regex AttributionPattern = new(
            @"^-[A-Za-z][A-Za-z'\-]* [A-Za-z][A-Za-z'\-]*, (?:[0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|250) aod$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public static void RunAllTests()
        {
            Console.WriteLine("=== VictoryVerseAttribution Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;

            TestFormatShape();
            TestFormatClampsYear();
            TestFormatFallbackNames();
            TestCreateRandomMatchesShape();

            TestBase.PrintSummary("VictoryVerseAttribution Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestFormatShape()
        {
            Console.WriteLine("--- Format shape ---");
            string credit = VictoryVerseAttribution.Format("Aric", "Bonehelm", 47);
            TestBase.AssertEqual("-Aric Bonehelm, 47 aod", credit,
                "exact attribution shape",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(AttributionPattern.IsMatch(credit),
                "format matches artist credit pattern",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestFormatClampsYear()
        {
            Console.WriteLine("--- Format clamps year ---");
            TestBase.AssertEqual("-Aric Bran, 0 aod",
                VictoryVerseAttribution.Format("Aric", "Bran", -3),
                "year below min clamps to 0",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("-Aric Bran, 250 aod",
                VictoryVerseAttribution.Format("Aric", "Bran", 999),
                "year above max clamps to 250",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestFormatFallbackNames()
        {
            Console.WriteLine("--- Format fallback names ---");
            TestBase.AssertEqual("-Unknown Bran, 10 aod",
                VictoryVerseAttribution.Format("  ", "Bran", 10),
                "blank first becomes Unknown",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("-Aric Unknown, 10 aod",
                VictoryVerseAttribution.Format("Aric", null!, 10),
                "null last becomes Unknown",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCreateRandomMatchesShape()
        {
            Console.WriteLine("--- CreateRandom matches shape ---");
            FlavorText.Reload();
            for (int i = 0; i < 8; i++)
            {
                string credit = VictoryVerseAttribution.CreateRandom();
                TestBase.AssertTrue(credit.StartsWith('-'),
                    $"starts with dash: {credit}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(!credit.Contains(" / ", StringComparison.Ordinal),
                    $"no slash separator: {credit}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(credit.EndsWith(" aod", StringComparison.Ordinal),
                    $"ends with aod: {credit}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(!credit.EndsWith("aod.", StringComparison.Ordinal),
                    $"no trailing period: {credit}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                int comma = credit.LastIndexOf(',');
                TestBase.AssertTrue(comma > 0, $"has year comma: {credit}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                string yearPart = credit[(comma + 1)..].Trim();
                // "47 aod"
                string yearToken = yearPart.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
                TestBase.AssertTrue(int.TryParse(yearToken, out int year),
                    $"year parses: {credit}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(year >= VictoryVerseAttribution.MinYearAod && year <= VictoryVerseAttribution.MaxYearAod,
                    $"year in range: {credit}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }
    }
}
