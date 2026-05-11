using System;
using System.Collections.Generic;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Tests for <see cref="DeathScreenRenderer"/> buffer summary lines.
    /// </summary>
    public static class DeathScreenRendererTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== DeathScreenRenderer Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestBuildDeathSummaryLinesIncludesHeaderAndStat();
            TestBuildDeathSummaryLinesCentersAndAlignsMetrics();

            TestBase.PrintSummary("DeathScreenRenderer Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestBuildDeathSummaryLinesIncludesHeaderAndStat()
        {
            Console.WriteLine("--- Testing BuildDeathSummaryLines ---");

            string defeatSummary = string.Join("\n", new[]
            {
                "═══════════════════════════════════════",
                "YOU DIED",
                "═══════════════════════════════════════",
                "",
                "COMBAT PERFORMANCE",
                "Total Damage Dealt: 123",
                "Better luck next time!"
            });

            var lines = DeathScreenRenderer.BuildDeathSummaryLines(defeatSummary);
            TestBase.AssertTrue(lines.Count >= 3, "Death summary should add multiple log lines", ref _testsRun, ref _testsPassed, ref _testsFailed);

            string headerPlain = ColoredTextRenderer.RenderAsPlainText(lines[0]);
            TestBase.AssertTrue(headerPlain.Contains("YOU DIED", StringComparison.OrdinalIgnoreCase), "First line should include YOU DIED header", ref _testsRun, ref _testsPassed, ref _testsFailed);

            bool foundStat = false;
            foreach (var line in lines)
            {
                string t = ColoredTextRenderer.RenderAsPlainText(line);
                if (t.Contains("Total Damage Dealt: 123", StringComparison.Ordinal))
                {
                    foundStat = true;
                    break;
                }
            }

            TestBase.AssertTrue(foundStat, "Summary should include stats from defeat summary", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestBuildDeathSummaryLinesCentersAndAlignsMetrics()
        {
            Console.WriteLine("--- Testing centered death summary layout ---");

            const int summaryWidth = 80;
            string defeatSummary = string.Join("\n", new[]
            {
                "═══════════════════════════════════════",
                "DEFEAT STATISTICS",
                "═══════════════════════════════════════",
                "",
                "📈 CHARACTER PROGRESSION:",
                "   Level: 1 → 6 (+5)",
                "   Total XP Gained: 0",
                "",
                "⚔️  COMBAT PERFORMANCE:",
                "   Enemies Defeated: 25",
                "   Total Damage Dealt: 1,672",
                "   Total Damage Received: 232",
                "",
                "🏆 ACHIEVEMENTS UNLOCKED:",
                "   ✓ Monster Slayer",
                "Better luck next time!"
            });

            var lines = DeathScreenRenderer.BuildDeathSummaryLines(defeatSummary, summaryWidth);

            string headerPlain = ColoredTextRenderer.RenderAsPlainText(lines[0]);
            int expectedHeaderPadding = (summaryWidth - headerPlain.TrimStart().Length) / 2;
            TestBase.AssertEqual(
                expectedHeaderPadding,
                CountLeadingSpaces(headerPlain),
                "Death header should be centered within the summary width",
                ref _testsRun,
                ref _testsPassed,
                ref _testsFailed);

            string dealtLine = FindPlainLine(lines, "Total Damage Dealt:");
            string receivedLine = FindPlainLine(lines, "Total Damage Received:");
            TestBase.AssertEqual(
                dealtLine.TrimStart().IndexOf("1,672", StringComparison.Ordinal),
                receivedLine.TrimStart().IndexOf("232", StringComparison.Ordinal),
                "Metric values should align after padded labels",
                ref _testsRun,
                ref _testsPassed,
                ref _testsFailed);

            TestBase.AssertTrue(
                FindPlainLine(lines, "Monster Slayer").StartsWith(" ", StringComparison.Ordinal),
                "Achievement row should receive centering padding",
                ref _testsRun,
                ref _testsPassed,
                ref _testsFailed);
        }

        private static string FindPlainLine(List<List<ColoredText>> lines, string contains)
        {
            foreach (var line in lines)
            {
                string plain = ColoredTextRenderer.RenderAsPlainText(line);
                if (plain.Contains(contains, StringComparison.Ordinal))
                    return plain;
            }

            return string.Empty;
        }

        private static int CountLeadingSpaces(string text)
        {
            int count = 0;
            while (count < text.Length && text[count] == ' ')
                count++;
            return count;
        }
    }
}

