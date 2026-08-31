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
            TestRollMarkerTravelsThenLands(ref run, ref passed, ref failed);
            TestRollMarkerWrongPanel(ref run, ref passed, ref failed);
            TestRollMarkerOffPhaseStillVisible(ref run, ref passed, ref failed);
            TestFeedbackExpiresAfterThreeSeconds(ref run, ref passed, ref failed);
            TestHeroAndEnemyIndependent(ref run, ref passed, ref failed);

            TestBase.PrintSummary("ThresholdBarFeedback Tests", run, passed, failed);
        }

        private static void TestHeroSegmentHighlightOnPhase(ref int run, ref int passed, ref int failed)
        {
            var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            try
            {
                ThresholdBarFeedback.ResetForTests();
                ThresholdBarFeedback.UtcNowProviderForTests = () => start;
                ThresholdBarFeedback.Trigger(ThresholdBarPanel.Hero, 2, 12);
                // Pulse begins after diamond travel completes.
                ThresholdBarFeedback.UtcNowProviderForTests = () =>
                    start.AddMilliseconds(ThresholdBarFeedback.TravelDurationMs);
                TestBase.AssertTrue(
                    ThresholdBarFeedback.TryGetSegmentHighlight(ThresholdBarPanel.Hero, 2, out var c)
                    && c == AsciiArtAssets.Colors.Gold,
                    "Hero segment 2 returns gold on pulse on-phase after travel",
                    ref run, ref passed, ref failed);
            }
            finally
            {
                ThresholdBarFeedback.ResetForTests();
            }
        }

        private static void TestWrongPanelNoHighlight(ref int run, ref int passed, ref int failed)
        {
            var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            try
            {
                ThresholdBarFeedback.ResetForTests();
                ThresholdBarFeedback.UtcNowProviderForTests = () => start;
                ThresholdBarFeedback.Trigger(ThresholdBarPanel.Hero, 1, 8);
                ThresholdBarFeedback.UtcNowProviderForTests = () =>
                    start.AddMilliseconds(ThresholdBarFeedback.TravelDurationMs);
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
            var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            try
            {
                ThresholdBarFeedback.ResetForTests();
                ThresholdBarFeedback.UtcNowProviderForTests = () => start;
                ThresholdBarFeedback.Trigger(ThresholdBarPanel.Enemy, 3, 16);
                ThresholdBarFeedback.UtcNowProviderForTests = () =>
                    start.AddMilliseconds(ThresholdBarFeedback.TravelDurationMs);
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
                ThresholdBarFeedback.Trigger(ThresholdBarPanel.Hero, -1, 10);
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
            var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            int travel = ThresholdBarFeedback.TravelDurationMs;

            try
            {
                gs.ActionStripSuccessFlashPulseHalfPeriodMs = 100;
                ThresholdBarFeedback.ResetForTests();
                ThresholdBarFeedback.UtcNowProviderForTests = () => start;
                ThresholdBarFeedback.Trigger(ThresholdBarPanel.Hero, 1, 5);

                ThresholdBarFeedback.UtcNowProviderForTests = () => start.AddMilliseconds(travel + 150);
                TestBase.AssertFalse(
                    ThresholdBarFeedback.TryGetSegmentHighlight(ThresholdBarPanel.Hero, 1, out _),
                    "Off phase returns no highlight",
                    ref run, ref passed, ref failed);

                ThresholdBarFeedback.UtcNowProviderForTests = () => start.AddMilliseconds(travel + 200);
                TestBase.AssertTrue(
                    ThresholdBarFeedback.TryGetSegmentHighlight(ThresholdBarPanel.Hero, 1, out _),
                    "Second on phase returns highlight",
                    ref run, ref passed, ref failed);
            }
            finally
            {
                gs.ActionStripSuccessFlashPulseHalfPeriodMs = savedPulse;
                ThresholdBarFeedback.ResetForTests();
            }
        }

        private static void TestRollMarkerTravelsThenLands(ref int run, ref int passed, ref int failed)
        {
            var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            try
            {
                ThresholdBarFeedback.ResetForTests();
                ThresholdBarFeedback.UtcNowProviderForTests = () => start;
                ThresholdBarFeedback.Trigger(ThresholdBarPanel.Hero, 2, 14);

                ThresholdBarFeedback.UtcNowProviderForTests = () => start.AddMilliseconds(0);
                TestBase.AssertTrue(
                    ThresholdBarFeedback.TryGetRollMarker(ThresholdBarPanel.Hero, out int early) && early >= 1 && early <= 14,
                    "Hero roll marker is visible during travel",
                    ref run, ref passed, ref failed);

                ThresholdBarFeedback.UtcNowProviderForTests = () =>
                    start.AddMilliseconds(ThresholdBarFeedback.TravelDurationMs);
                TestBase.AssertTrue(
                    ThresholdBarFeedback.TryGetRollMarker(ThresholdBarPanel.Hero, out int landed) && landed == 14,
                    "Hero roll marker lands on triggered roll after travel",
                    ref run, ref passed, ref failed);
            }
            finally
            {
                ThresholdBarFeedback.ResetForTests();
            }
        }

        private static void TestRollMarkerWrongPanel(ref int run, ref int passed, ref int failed)
        {
            try
            {
                ThresholdBarFeedback.ResetForTests();
                ThresholdBarFeedback.Trigger(ThresholdBarPanel.Hero, 1, 7);
                TestBase.AssertFalse(
                    ThresholdBarFeedback.TryGetRollMarker(ThresholdBarPanel.Enemy, out _),
                    "Enemy panel does not show hero roll marker",
                    ref run, ref passed, ref failed);
            }
            finally
            {
                ThresholdBarFeedback.ResetForTests();
            }
        }

        private static void TestRollMarkerOffPhaseStillVisible(ref int run, ref int passed, ref int failed)
        {
            var gs = GameSettings.Instance;
            int savedPulse = gs.ActionStripSuccessFlashPulseHalfPeriodMs;
            var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            int travel = ThresholdBarFeedback.TravelDurationMs;

            try
            {
                gs.ActionStripSuccessFlashPulseHalfPeriodMs = 100;
                ThresholdBarFeedback.ResetForTests();
                ThresholdBarFeedback.UtcNowProviderForTests = () => start;
                ThresholdBarFeedback.Trigger(ThresholdBarPanel.Hero, 1, 11);

                ThresholdBarFeedback.UtcNowProviderForTests = () => start.AddMilliseconds(travel + 150);
                TestBase.AssertFalse(
                    ThresholdBarFeedback.TryGetSegmentHighlight(ThresholdBarPanel.Hero, 1, out _),
                    "Off phase hides segment highlight",
                    ref run, ref passed, ref failed);
                TestBase.AssertTrue(
                    ThresholdBarFeedback.TryGetRollMarker(ThresholdBarPanel.Hero, out int roll) && roll == 11,
                    "Roll caret stays visible during segment off-phase",
                    ref run, ref passed, ref failed);
            }
            finally
            {
                gs.ActionStripSuccessFlashPulseHalfPeriodMs = savedPulse;
                ThresholdBarFeedback.ResetForTests();
            }
        }

        private static void TestFeedbackExpiresAfterThreeSeconds(ref int run, ref int passed, ref int failed)
        {
            var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

            try
            {
                ThresholdBarFeedback.ResetForTests();
                ThresholdBarFeedback.UtcNowProviderForTests = () => start;
                ThresholdBarFeedback.Trigger(ThresholdBarPanel.Hero, 0, 9);

                ThresholdBarFeedback.UtcNowProviderForTests = () =>
                    start.AddMilliseconds(ThresholdBarFeedback.TravelDurationMs + 50);
                TestBase.AssertTrue(
                    ThresholdBarFeedback.TryGetSegmentHighlight(ThresholdBarPanel.Hero, 0, out _),
                    "Segment pulse still active during three-second window after travel",
                    ref run, ref passed, ref failed);

                ThresholdBarFeedback.UtcNowProviderForTests = () =>
                    start.AddMilliseconds(ThresholdBarFeedback.FeedbackDurationMs - 1);
                TestBase.AssertTrue(
                    ThresholdBarFeedback.TryGetRollMarker(ThresholdBarPanel.Hero, out _),
                    "Roll caret visible just before three-second window ends",
                    ref run, ref passed, ref failed);

                ThresholdBarFeedback.UtcNowProviderForTests = () =>
                    start.AddMilliseconds(ThresholdBarFeedback.FeedbackDurationMs);
                TestBase.AssertFalse(
                    ThresholdBarFeedback.TryGetRollMarker(ThresholdBarPanel.Hero, out _),
                    "Roll caret clears after three seconds",
                    ref run, ref passed, ref failed);
                TestBase.AssertFalse(
                    ThresholdBarFeedback.TryGetSegmentHighlight(ThresholdBarPanel.Hero, 0, out _),
                    "Segment pulse clears after three seconds",
                    ref run, ref passed, ref failed);
            }
            finally
            {
                ThresholdBarFeedback.ResetForTests();
            }
        }

        private static void TestHeroAndEnemyIndependent(ref int run, ref int passed, ref int failed)
        {
            var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            try
            {
                ThresholdBarFeedback.ResetForTests();
                ThresholdBarFeedback.UtcNowProviderForTests = () => start;
                ThresholdBarFeedback.Trigger(ThresholdBarPanel.Hero, 1, 10);
                ThresholdBarFeedback.Trigger(ThresholdBarPanel.Enemy, 2, 18);
                ThresholdBarFeedback.UtcNowProviderForTests = () =>
                    start.AddMilliseconds(ThresholdBarFeedback.TravelDurationMs);
                TestBase.AssertTrue(
                    ThresholdBarFeedback.TryGetRollMarker(ThresholdBarPanel.Hero, out int hr) && hr == 10,
                    "Hero marker independent after dual trigger",
                    ref run, ref passed, ref failed);
                TestBase.AssertTrue(
                    ThresholdBarFeedback.TryGetRollMarker(ThresholdBarPanel.Enemy, out int er) && er == 18,
                    "Enemy marker independent after dual trigger",
                    ref run, ref passed, ref failed);
            }
            finally
            {
                ThresholdBarFeedback.ResetForTests();
            }
        }
    }
}
