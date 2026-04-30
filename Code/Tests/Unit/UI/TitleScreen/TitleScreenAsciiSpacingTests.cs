using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Tests;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.TitleScreen;

namespace RPGGame.Tests.Unit.UI.TitleScreen
{
    public static class TitleScreenAsciiSpacingTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== TitleScreen ASCII Spacing Tests ===\n");

            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestTemplateFramePreservesDungeonAsciiLinesExactly();

            TestBase.PrintSummary("TitleScreen ASCII Spacing Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestTemplateFramePreservesDungeonAsciiLinesExactly()
        {
            Console.WriteLine("--- Testing template frame preserves DEMON ASCII exactly ---");

            var config = new TitleAnimationConfig();
            var builder = new TitleFrameBuilder(config);

            // Use the same templates used for the final title frame.
            var frame = builder.BuildTemplateFrame("title_dungeon_yellow_orange", "title_fighter_yellow_orange");

            // Layout: 15 top padding + 2 blank lines, then 6 "DungeonLines" (DEMON).
            const int dungeonStartIndex = 17;

            for (int i = 0; i < TitleArtAssets.DungeonLines.Length; i++)
            {
                string expected = TitleArtAssets.DungeonLines[i];
                string actual = Flatten(frame.Lines[dungeonStartIndex + i]);

                TestBase.AssertEqual(expected.Length, actual.Length,
                    $"Dungeon line {i} should preserve exact length",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertEqual(expected, actual,
                    $"Dungeon line {i} should preserve exact characters (no injected spaces)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static string Flatten(List<ColoredText>? segments)
        {
            if (segments == null || segments.Count == 0)
                return string.Empty;

            return string.Concat(segments.Where(s => s?.Text != null).Select(s => s.Text));
        }
    }
}

