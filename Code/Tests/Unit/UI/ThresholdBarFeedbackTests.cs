using System;
using RPGGame;
using RPGGame.Tests;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Feedback;

namespace RPGGame.Tests.Unit.UI
{
    public static class ThresholdBarFeedbackTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== ThresholdBarFeedback Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            TestHeroSegmentHighlightOnPhase(ref run, ref passed, ref failed);
            TestWrongPanelNoHighlight(ref run, ref passed, ref failed);
            TestWrongSegmentNoHighlight(ref run, ref passed, ref failed);
            TestNegativeIndexNoOp(ref run, ref passed, ref failed);
            TestOffPhaseNoHighlight(ref run, ref passed, ref failed);

            TestBase.PrintSummary("ThresholdBarFeedback Tests", run, passed, failed);
        }

        private static void TestHeroSegmentHighlightOnPhase(ref int run, ref int passed, ref int failed)
        {
            try
            {
                ThresholdBarFeedback.ResetForTests();
                ThresholdBarFeedback.Trigger(ThresholdBarPanel.Hero, 2);
                TestBase.AssertTrue(
                    ThresholdBarFeedback.TryGetSegmentHighlight(ThresholdBarPanel.Hero, 2, out var c)
                    && c == AsciiArtAssets.Colors.Gold,
                    "Hero segment 2 returns gold on pulse on-phase",
                    ref run, ref passed, ref failed);
            }
            finally
            {
                ThresholdBarFeedback.ResetForTests();
            }
        }

        private static void TestWrongPanelNoHighlight(ref int run, ref int passed, ref int failed)
        {
            try
            {
                ThresholdBarFeedback.ResetForTests();
                ThresholdBarFeedback.Trigger(ThresholdBarPanel.Hero, 1);
                TestBase.AssertFalse(
                    ThresholdBarFeedback.TryGetSegmentHighlight(ThresholdBarPanel.Enemy, 1, out _),
                    "Enemy panel does not highlight hero flash",
                    ref run, ref passed, ref failed);
            }
            finally
            {
                ThresholdBarFeedback.ResetForTests();
            }
        }

        private static void TestWrongSegmentNoHighlight(ref int run, ref int passed, ref int failed)
        {
            try
            {
                ThresholdBarFeedback.ResetForTests();
                ThresholdBarFeedback.Trigger(ThresholdBarPanel.Enemy, 3);
                TestBase.AssertFalse(
                    ThresholdBarFeedback.TryGetSegmentHighlight(ThresholdBarPanel.Enemy, 0, out _),
                    "Non-selected segment does not highlight",
                    ref run, ref passed, ref failed);
            }
            finally
            {
                ThresholdBarFeedback.ResetForTests();
            }
        }

        private static void TestNegativeIndexNoOp(ref int run, ref int passed, ref int failed)
        {
            try
            {
                ThresholdBarFeedback.ResetForTests();
                ThresholdBarFeedback.Trigger(ThresholdBarPanel.Hero, -1);
                TestBase.AssertFalse(
                    ThresholdBarFeedback.TryGetSegmentHighlight(ThresholdBarPanel.Hero, 0, out _),
                    "Negative segment index is ignored",
                    ref run, ref passed, ref failed);
            }
            finally
            {
                ThresholdBarFeedback.ResetForTests();
            }
        }

        private static void TestOffPhaseNoHighlight(ref int run, ref int passed, ref int failed)
        {
            var gs = GameSettings.Instance;
            int savedPulse = gs.ActionStripSuccessFlashPulseHalfPeriodMs;
            int savedMiss = gs.ActionStripMissFlashDurationMs;
            var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

            try
            {
                gs.ActionStripSuccessFlashPulseHalfPeriodMs = 100;
                gs.ActionStripMissFlashDurationMs = 3000;
                ThresholdBarFeedback.ResetForTests();
                ThresholdBarFeedback.UtcNowProviderForTests = () => start;
                ThresholdBarFeedback.Trigger(ThresholdBarPanel.Hero, 1);

                ThresholdBarFeedback.UtcNowProviderForTests = () => start.AddMilliseconds(150);
                TestBase.AssertFalse(
                    ThresholdBarFeedback.TryGetSegmentHighlight(ThresholdBarPanel.Hero, 1, out _),
                    "Off phase returns no highlight",
                    ref run, ref passed, ref failed);

                ThresholdBarFeedback.UtcNowProviderForTests = () => start.AddMilliseconds(200);
                TestBase.AssertTrue(
                    ThresholdBarFeedback.TryGetSegmentHighlight(ThresholdBarPanel.Hero, 1, out _),
                    "Second on phase returns highlight",
                    ref run, ref passed, ref failed);
            }
            finally
            {
                gs.ActionStripSuccessFlashPulseHalfPeriodMs = savedPulse;
                gs.ActionStripMissFlashDurationMs = savedMiss;
                ThresholdBarFeedback.ResetForTests();
            }
        }
    }
}
