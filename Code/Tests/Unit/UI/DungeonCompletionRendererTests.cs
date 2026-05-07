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

            TestBuildCompletionSummaryLinesIncludesVictoryAndDungeonName();

            TestBase.PrintSummary("DungeonCompletionRenderer Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestBuildCompletionSummaryLinesIncludesVictoryAndDungeonName()
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
            foreach (var line in lines)
            {
                string t = ColoredTextRenderer.RenderAsPlainText(line);
                if (t.Contains("Haunted Crypt", StringComparison.Ordinal))
                {
                    foundDungeonName = true;
                    break;
                }
            }

            TestBase.AssertTrue(foundDungeonName, "Summary should include dungeon name", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
