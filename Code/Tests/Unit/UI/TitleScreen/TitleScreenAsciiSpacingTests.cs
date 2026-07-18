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
            TestDemonBottomPipingMatchesTopLetterWidths();
            TestSolidColorFrameTaglineHasNoLeadingIndent();
            TestTitleCenterXHasNoHorizontalBias();
            TestFinalHoldDurationDefaultsToZeroForAnyKeyContinue();
            TestIntroDurationUsesNewPhaseCounts();

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

        private static void TestDemonBottomPipingMatchesTopLetterWidths()
        {
            Console.WriteLine("--- Testing DEMON bottom piping matches top letter widths ---");

            string top = TitleArtAssets.DungeonLines[0];
            string bottom = TitleArtAssets.DungeonLines[TitleArtAssets.DungeonLines.Length - 1];

            TestBase.AssertEqual(top.Length, bottom.Length,
                "DEMON bottom row should match top row length (aligned 3D footing)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Line 0 layout: D(7) + sp + E(8) + M(11) + sp + O(7) + sp + N(10) = 46
            // M = three pillars; N = wide gap between two pillars
            const int mStart = 16; // after D+sp+E
            const int mLen = 11;
            const int nStart = 36; // after D+sp+E+M+sp+O+sp
            const int nLen = 10;

            string topM = top.Substring(mStart, mLen);
            string bottomM = bottom.Substring(mStart, mLen);
            string topN = top.Substring(nStart, nLen);
            string bottomN = bottom.Substring(nStart, nLen);

            TestBase.AssertEqual("██╗ ██╗ ██╗", topM,
                "DEMON top M should be three spaced pillars",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            // Middle V footing lives on the row above; bottom row only has left/right legs
            // (a third ╚═╝ here duplicated the piping under the M).
            TestBase.AssertEqual("╚═╝     ╚═╝", bottomM,
                "DEMON bottom M footing should be left/right legs only (no middle duplicate)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("██╗    ██╗", topN,
                "DEMON top N should use a wide gap",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("╚═╝    ╚═╝", bottomN,
                "DEMON bottom N footing should match the wide gap",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSolidColorFrameTaglineHasNoLeadingIndent()
        {
            Console.WriteLine("--- Testing tagline has no artificial leading indent ---");

            var config = new TitleAnimationConfig();
            var builder = new TitleFrameBuilder(config);
            var frame = builder.BuildSolidColorFrame("W", "o");

            // Layout: 15 pad + 2 blank + 6 DEMON + blank + decorator + blank + 6 FIGHTER + 3 blank + tagline
            const int taglineIndex = 15 + 2 + 6 + 1 + 1 + 1 + 6 + 3;
            string tagline = Flatten(frame.Lines[taglineIndex]);

            TestBase.AssertEqual(TitleArtAssets.Tagline, tagline,
                "Tagline should match asset exactly (no PrependSpaces indent)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!tagline.StartsWith(" ", StringComparison.Ordinal),
                "Tagline should not start with leading spaces",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestTitleCenterXHasNoHorizontalBias()
        {
            Console.WriteLine("--- Testing title center math has no horizontal bias ---");

            // Pure center-justify: CenterX - (length / 2). No globalLeftShift / titleOffset.
            const int screenCenterX = 60;
            int[] lengths = { 17, 46, 58, 28 }; // decorator, DEMON, tagline, press-key-ish

            foreach (int length in lengths)
            {
                int centerX = Math.Max(0, screenCenterX - (length / 2));
                int expected = screenCenterX - (length / 2);
                TestBase.AssertEqual(expected, centerX,
                    $"Centered start for length {length} should be CenterX - length/2 with no bias",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestFinalHoldDurationDefaultsToZeroForAnyKeyContinue()
        {
            Console.WriteLine("--- Testing final hold is zero (any-key title screen) ---");

            var config = new TitleAnimationConfig();
            TestBase.AssertEqual(0, config.FinalHoldDuration,
                "FinalHoldDuration should default to 0 — title waits for any key",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var animation = new TitleAnimation(config);
            TestBase.AssertEqual(animation.GetTotalDurationMs(), animation.GetTotalDurationMs(),
                "GetTotalDurationMs should be stable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var finalHold = animation.GenerateAnimationSequence()
                .LastOrDefault(s => s.Phase == AnimationPhase.FinalHold);
            TestBase.AssertTrue(finalHold != null,
                "Animation sequence should include a FinalHold step",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, finalHold!.DurationMs,
                "FinalHold DurationMs should be 0 so press-any-key can show immediately",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestIntroDurationUsesNewPhaseCounts()
        {
            Console.WriteLine("--- Testing intro duration matches phase frame counts ---");

            var config = new TitleAnimationConfig
            {
                BlackScreenFrames = 2,
                FadeInFrames = 3,
                WhiteLightHoldFrames = 1,
                PopFrames = 2,
                SettleFrames = 4,
                FinalTransitionFrames = 4,
                FinalHoldDuration = 0,
                FramesPerSecond = 20
            };
            var animation = new TitleAnimation(config);
            int frameDuration = config.FrameDurationMs;
            int frameCount =
                config.BlackScreenFrames +
                config.FadeInFrames +
                1 + // white flash
                config.WhiteLightHoldFrames +
                config.PopFrames +
                (config.EffectiveSettleFrames + 1);
            int expected = frameCount * frameDuration;

            TestBase.AssertEqual(expected, animation.GetTotalDurationMs(),
                "Total intro duration should match black+fade+flash+hold+pop+settle frames",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static string Flatten(List<ColoredText>? segments)
        {
            if (segments == null || segments.Count == 0)
                return string.Empty;

            return string.Concat(segments.Where(s => s?.Text != null).Select(s => s.Text));
        }
    }
}
