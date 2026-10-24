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
            TestRollMarkerDuringFlash(ref run, ref passed, ref failed);
            TestRollMarkerWrongPanel(ref run, ref passed, ref failed);
            TestRollMarkerOffPhaseStillVisible(ref run, ref passed, ref failed);
            TestFeedbackExpiresAfterThreeSeconds(ref run, ref passed, ref failed);
            TestCaretTumblesBeforeLanding(ref run, ref passed, ref failed);
            TestTumbleFaceLandsOnRoll(ref run, ref passed, ref failed);

            TestBase.PrintSummary("ThresholdBarFeedback Tests", run, passed, failed);
        }

        private static readonly DateTimeOffset Start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        /// <summary><paramref name="ms"/> after the caret lands on a roll triggered at <see cref="Start"/>.</summary>
        private static DateTimeOffset Landed(double ms) => Start.AddMilliseconds(ThresholdBarFeedback.RollTumbleMs + ms);

        private static void TestHeroSegmentHighlightOnPhase(ref int run, ref int passed, ref int failed)
        {
            try
            {
                ThresholdBarFeedback.ResetForTests();
                ThresholdBarFeedback.UtcNowProviderForTests = () => Start;
                ThresholdBarFeedback.Trigger(ThresholdBarPanel.Hero, 2, 12);
                ThresholdBarFeedback.UtcNowProviderForTests = () => Landed(0);
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
                ThresholdBarFeedback.Trigger(ThresholdBarPanel.Hero, 1, 8);
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
                ThresholdBarFeedback.Trigger(ThresholdBarPanel.Enemy, 3, 16);
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
            var start = Start;

            try
            {
                gs.ActionStripSuccessFlashPulseHalfPeriodMs = 100;
                ThresholdBarFeedback.ResetForTests();
                ThresholdBarFeedback.UtcNowProviderForTests = () => start;
                ThresholdBarFeedback.Trigger(ThresholdBarPanel.Hero, 1, 5);

                ThresholdBarFeedback.UtcNowProviderForTests = () => Landed(150);
                TestBase.AssertFalse(
                    ThresholdBarFeedback.TryGetSegmentHighlight(ThresholdBarPanel.Hero, 1, out _),
                    "Off phase returns no highlight",
                    ref run, ref passed, ref failed);

                ThresholdBarFeedback.UtcNowProviderForTests = () => Landed(200);
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

        private static void TestRollMarkerDuringFlash(ref int run, ref int passed, ref int failed)
        {
            try
            {
                ThresholdBarFeedback.ResetForTests();
                ThresholdBarFeedback.UtcNowProviderForTests = () => Start;
                ThresholdBarFeedback.Trigger(ThresholdBarPanel.Hero, 2, 14);
                ThresholdBarFeedback.UtcNowProviderForTests = () => Landed(0);
                TestBase.AssertTrue(
                    ThresholdBarFeedback.TryGetRollMarker(ThresholdBarPanel.Hero, out int roll) && roll == 14,
                    "Hero roll marker returns triggered roll",
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
            var start = Start;

            try
            {
                gs.ActionStripSuccessFlashPulseHalfPeriodMs = 100;
                ThresholdBarFeedback.ResetForTests();
                ThresholdBarFeedback.UtcNowProviderForTests = () => start;
                ThresholdBarFeedback.Trigger(ThresholdBarPanel.Hero, 1, 11);

                ThresholdBarFeedback.UtcNowProviderForTests = () => Landed(150);
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
            var start = Start;

            try
            {
                ThresholdBarFeedback.ResetForTests();
                ThresholdBarFeedback.UtcNowProviderForTests = () => start;
                ThresholdBarFeedback.Trigger(ThresholdBarPanel.Hero, 0, 9);

                ThresholdBarFeedback.UtcNowProviderForTests = () => Landed(50);
                TestBase.AssertTrue(
                    ThresholdBarFeedback.TryGetSegmentHighlight(ThresholdBarPanel.Hero, 0, out _),
                    "Segment pulse still active during three-second window",
                    ref run, ref passed, ref failed);

                ThresholdBarFeedback.UtcNowProviderForTests = () =>
                    Landed(ThresholdBarFeedback.FeedbackDurationMs - 1);
                TestBase.AssertTrue(
                    ThresholdBarFeedback.TryGetRollMarker(ThresholdBarPanel.Hero, out _),
                    "Roll caret visible just before three-second window ends",
                    ref run, ref passed, ref failed);

                ThresholdBarFeedback.UtcNowProviderForTests = () =>
                    Landed(ThresholdBarFeedback.FeedbackDurationMs);
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

        private static void TestCaretTumblesBeforeLanding(ref int run, ref int passed, ref int failed)
        {
            try
            {
                ThresholdBarFeedback.ResetForTests();
                ThresholdBarFeedback.UtcNowProviderForTests = () => Start;
                ThresholdBarFeedback.Trigger(ThresholdBarPanel.Hero, 1, 6);

                bool sawOtherFace = false, highlightDuringTumble = false;
                for (int ms = 0; ms < ThresholdBarFeedback.RollTumbleMs; ms += 10)
                {
                    ThresholdBarFeedback.UtcNowProviderForTests = () => Start.AddMilliseconds(ms);
                    if (ThresholdBarFeedback.TryGetRollMarker(ThresholdBarPanel.Hero, out int face) && face != 6)
                        sawOtherFace = true;
                    if (ThresholdBarFeedback.TryGetSegmentHighlight(ThresholdBarPanel.Hero, 1, out _))
                        highlightDuringTumble = true;
                }

                TestBase.AssertTrue(sawOtherFace, "Caret shows other faces while the roll tumbles", ref run, ref passed, ref failed);
                TestBase.AssertFalse(highlightDuringTumble, "Segment does not pulse until the caret lands", ref run, ref passed, ref failed);

                ThresholdBarFeedback.UtcNowProviderForTests = () => Landed(0);
                TestBase.AssertTrue(
                    ThresholdBarFeedback.TryGetRollMarker(ThresholdBarPanel.Hero, out int landed) && landed == 6,
                    "Caret lands on the roll after the tumble",
                    ref run, ref passed, ref failed);
            }
            finally
            {
                ThresholdBarFeedback.ResetForTests();
            }
        }

        private static void TestTumbleFaceLandsOnRoll(ref int run, ref int passed, ref int failed)
        {
            bool inRange = true, landsOnRoll = true;
            for (int roll = 1; roll <= 20; roll++)
            {
                for (double p = 0; p < 1.0; p += 0.01)
                {
                    int face = ThresholdBarFeedback.TumbleFace(roll, p);
                    inRange &= face >= 1 && face <= 20;
                }
                landsOnRoll &= ThresholdBarFeedback.TumbleFace(roll, 1.0) == roll;
            }

            TestBase.AssertTrue(inRange, "Tumble faces stay within 1-20", ref run, ref passed, ref failed);
            TestBase.AssertTrue(landsOnRoll, "Tumble ends on the rolled face", ref run, ref passed, ref failed);
        }
    }
}
