using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Tests for <see cref="DungeonCompletionRenderer"/> buffer summary lines.
    /// </summary>
    public static class DungeonCompletionRendererTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== DungeonCompletionRenderer Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestBuildCompletionSummaryLinesIncludesVictoryAndOmitsTopDetailRows();
            TestBuildCompletionSummaryLinesCentersAndAlignsMetrics();

            TestBase.PrintSummary("DungeonCompletionRenderer Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestBuildCompletionSummaryLinesIncludesVictoryAndOmitsTopDetailRows()
        {
            Console.WriteLine("--- Testing BuildCompletionSummaryLines ---");

            var dungeon = new Dungeon("Haunted Crypt", 1, 5, "Crypt");
            dungeon.Rooms.Add(new Environment("Room A", "Test", false, "Crypt"));
            dungeon.Rooms.Add(new Environment("Room B", "Test", false, "Crypt"));

            var player = new Character("Hero", 1);
            player.CurrentHealth = player.GetEffectiveMaxHealth();

            var lines = DungeonCompletionRenderer.BuildCompletionSummaryLines(
                dungeon,
                player,
                350,
                null,
                new List<LevelUpInfo>(),
                new List<Item>());

            TestBase.AssertTrue(lines.Count >= 8, "Completion summary should add multiple log lines", ref _testsRun, ref _testsPassed, ref _testsFailed);

            string headerPlain = ColoredTextRenderer.RenderAsPlainText(lines[0]);
            TestBase.AssertTrue(
                headerPlain.Contains("VICTORY", StringComparison.OrdinalIgnoreCase),
                "First line should include victory header",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            bool foundDungeonName = false;
            bool foundHealthLine = false;
            foreach (var line in lines)
            {
                string t = ColoredTextRenderer.RenderAsPlainText(line);
                if (t.Contains("Haunted Crypt", StringComparison.Ordinal))
                    foundDungeonName = true;
                if (t.Contains("Health:", StringComparison.Ordinal) || t.Contains("Fully Restored", StringComparison.Ordinal))
                    foundHealthLine = true;
            }

            TestBase.AssertFalse(foundDungeonName, "Summary should omit dungeon detail row", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(foundHealthLine, "Summary should omit health detail row", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestBuildCompletionSummaryLinesCentersAndAlignsMetrics()
        {
            Console.WriteLine("--- Testing centered completion summary layout ---");

            const int summaryWidth = 80;
            var dungeon = new Dungeon("Ancient Forest", 1, 5, "Forest");
            dungeon.Rooms.Add(new Environment("Room A", "Test", false, "Forest"));
            dungeon.Rooms.Add(new Environment("Room B", "Test", false, "Forest"));

            var player = new Character("Hero", 1);
            player.CurrentHealth = player.GetEffectiveMaxHealth();
            player.SessionStats.EnemiesDefeated = 4;
            player.SessionStats.TotalDamageDealt = 235;
            player.SessionStats.TotalDamageReceived = 29;

            var lines = DungeonCompletionRenderer.BuildCompletionSummaryLines(
                dungeon,
                player,
                450,
                null,
                new List<LevelUpInfo>(),
                new List<Item>(),
                summaryWidth);

            string headerPlain = ColoredTextRenderer.RenderAsPlainText(lines[0]);
            int expectedHeaderPadding = (summaryWidth - headerPlain.TrimStart().Length) / 2;
            TestBase.AssertEqual(
                expectedHeaderPadding,
                CountLeadingSpaces(headerPlain),
                "Victory header should be centered within the summary width",
                ref _testsRun,
                ref _testsPassed,
                ref _testsFailed);

            string roomsLine = FindPlainLine(lines, "Rooms Cleared:");
            string enemiesLine = FindPlainLine(lines, "Enemies Defeated:");
            TestBase.AssertEqual(
                roomsLine.TrimStart().IndexOf("2", StringComparison.Ordinal),
                enemiesLine.TrimStart().IndexOf("4", StringComparison.Ordinal),
                "Metric values should align after padded labels",
                ref _testsRun,
                ref _testsPassed,
                ref _testsFailed);

            TestBase.AssertTrue(
                FindPlainLine(lines, "Experience Gained:").StartsWith(" ", StringComparison.Ordinal),
                "Reward row should receive centering padding",
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
