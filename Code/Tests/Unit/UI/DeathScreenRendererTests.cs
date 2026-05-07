using System;
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
    }
}

