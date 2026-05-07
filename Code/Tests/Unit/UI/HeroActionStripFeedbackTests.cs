using System;

using RPGGame;

using RPGGame.Tests;

using RPGGame.UI.Avalonia;

using RPGGame.UI.Avalonia.Feedback;



namespace RPGGame.Tests.Unit.UI

{

    /// <summary>

    /// Tests for <see cref="HeroActionStripFeedback"/>.

    /// </summary>

    public static class HeroActionStripFeedbackTests

    {

        public static void RunAllTests()

        {

            Console.WriteLine("=== HeroActionStripFeedback Tests ===\n");

            int run = 0, passed = 0, failed = 0;



            TestHitFlashGreen(ref run, ref passed, ref failed);

            TestComboCompleteFlashGold(ref run, ref passed, ref failed);

            TestMissFlashRed(ref run, ref passed, ref failed);

            TestWrongPanelNoOverride(ref run, ref passed, ref failed);

            TestNegativeIndexNoOp(ref run, ref passed, ref failed);

            TestSuccessPulseOffPhaseNoOverride(ref run, ref passed, ref failed);

            TestMissPulseOffPhaseNoOverride(ref run, ref passed, ref failed);

            TestFlashEmphasisStaysActiveThroughOffPhase(ref run, ref passed, ref failed);

            TestFlashEmphasisWrongPanel(ref run, ref passed, ref failed);



            TestBase.PrintSummary("HeroActionStripFeedback Tests", run, passed, failed);

        }



        private static void TestHitFlashGreen(ref int run, ref int passed, ref int failed)

        {

            try

            {

                HeroActionStripFeedback.ResetForTests();

                HeroActionStripFeedback.Trigger(0, HeroActionStripFlashKind.Hit);

                TestBase.AssertTrue(

                    HeroActionStripFeedback.TryGetBorderOverride(0, out var c) && c == AsciiArtAssets.Colors.Green,

                    "Hit flash: panel 0 returns green",

                    ref run, ref passed, ref failed);

            }

            finally

            {

                HeroActionStripFeedback.ResetForTests();

            }

        }



        private static void TestComboCompleteFlashGold(ref int run, ref int passed, ref int failed)

        {

            try

            {

                HeroActionStripFeedback.ResetForTests();

                HeroActionStripFeedback.Trigger(0, HeroActionStripFlashKind.ComboComplete);

                TestBase.AssertTrue(

                    HeroActionStripFeedback.TryGetBorderOverride(0, out var c) && c == AsciiArtAssets.Colors.Gold,

                    "Combo-complete flash: panel 0 returns gold",

                    ref run, ref passed, ref failed);

            }

            finally

            {

                HeroActionStripFeedback.ResetForTests();

            }

        }



        private static void TestMissFlashRed(ref int run, ref int passed, ref int failed)

        {

            try

            {

                HeroActionStripFeedback.ResetForTests();

                HeroActionStripFeedback.Trigger(1, HeroActionStripFlashKind.Miss);

                TestBase.AssertTrue(

                    HeroActionStripFeedback.TryGetBorderOverride(1, out var c) && c == AsciiArtAssets.Colors.Red,

                    "Miss flash: panel 1 returns red (on phase)",

                    ref run, ref passed, ref failed);

            }

            finally

            {

                HeroActionStripFeedback.ResetForTests();

            }

        }



        private static void TestWrongPanelNoOverride(ref int run, ref int passed, ref int failed)

        {

            try

            {

                HeroActionStripFeedback.ResetForTests();

                HeroActionStripFeedback.Trigger(2, HeroActionStripFlashKind.Hit);

                TestBase.AssertTrue(

                    !HeroActionStripFeedback.TryGetBorderOverride(0, out _),

                    "Flash on panel 2 does not override panel 0",

                    ref run, ref passed, ref failed);

            }

            finally

            {

                HeroActionStripFeedback.ResetForTests();

            }

        }



        private static void TestNegativeIndexNoOp(ref int run, ref int passed, ref int failed)

        {

            try

            {

                HeroActionStripFeedback.ResetForTests();

                HeroActionStripFeedback.Trigger(-1, HeroActionStripFlashKind.Hit);

                TestBase.AssertTrue(

                    !HeroActionStripFeedback.TryGetBorderOverride(0, out _),

                    "Trigger(-1) leaves no active flash",

                    ref run, ref passed, ref failed);

            }

            finally

            {

                HeroActionStripFeedback.ResetForTests();

            }

        }



        private static void TestSuccessPulseOffPhaseNoOverride(ref int run, ref int passed, ref int failed)

        {

            var gs = GameSettings.Instance;

            int savedPulse = gs.ActionStripSuccessFlashPulseHalfPeriodMs;

            int savedTotal = gs.ActionStripSuccessFlashDurationMs;

            try

            {

                HeroActionStripFeedback.ResetForTests();

                var t0 = new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero);

                var clock = t0;

                HeroActionStripFeedback.UtcNowProviderForTests = () => clock;



                gs.ActionStripSuccessFlashPulseHalfPeriodMs = 100;

                gs.ActionStripSuccessFlashDurationMs = 3000;

                gs.ValidateAndFix();



                HeroActionStripFeedback.Trigger(0, HeroActionStripFlashKind.ComboComplete);

                TestBase.AssertTrue(

                    HeroActionStripFeedback.TryGetBorderOverride(0, out var gold) && gold == AsciiArtAssets.Colors.Gold,

                    "Combo pulse first half-period: gold",

                    ref run, ref passed, ref failed);



                clock = t0.AddMilliseconds(150);

                TestBase.AssertTrue(

                    !HeroActionStripFeedback.TryGetBorderOverride(0, out _),

                    "Combo pulse second half-period: no override (off phase)",

                    ref run, ref passed, ref failed);

            }

            finally

            {

                gs.ActionStripSuccessFlashPulseHalfPeriodMs = savedPulse;

                gs.ActionStripSuccessFlashDurationMs = savedTotal;

                gs.ValidateAndFix();

                HeroActionStripFeedback.ResetForTests();

            }

        }



        private static void TestMissPulseOffPhaseNoOverride(ref int run, ref int passed, ref int failed)

        {

            var gs = GameSettings.Instance;

            int savedPulse = gs.ActionStripSuccessFlashPulseHalfPeriodMs;

            int savedMiss = gs.ActionStripMissFlashDurationMs;

            try

            {

                HeroActionStripFeedback.ResetForTests();

                var t0 = new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero);

                var clock = t0;

                HeroActionStripFeedback.UtcNowProviderForTests = () => clock;



                gs.ActionStripSuccessFlashPulseHalfPeriodMs = 100;

                gs.ActionStripMissFlashDurationMs = 3000;

                gs.ValidateAndFix();



                HeroActionStripFeedback.Trigger(0, HeroActionStripFlashKind.Miss);

                TestBase.AssertTrue(

                    HeroActionStripFeedback.TryGetBorderOverride(0, out var red) && red == AsciiArtAssets.Colors.Red,

                    "Miss pulse first half-period: red",

                    ref run, ref passed, ref failed);



                clock = t0.AddMilliseconds(150);

                TestBase.AssertTrue(

                    !HeroActionStripFeedback.TryGetBorderOverride(0, out _),

                    "Miss pulse second half-period: no override (off phase)",

                    ref run, ref passed, ref failed);

            }

            finally

            {

                gs.ActionStripSuccessFlashPulseHalfPeriodMs = savedPulse;

                gs.ActionStripMissFlashDurationMs = savedMiss;

                gs.ValidateAndFix();

                HeroActionStripFeedback.ResetForTests();

            }

        }



        private static void TestFlashEmphasisStaysActiveThroughOffPhase(ref int run, ref int passed, ref int failed)

        {

            var gs = GameSettings.Instance;

            int savedPulse = gs.ActionStripSuccessFlashPulseHalfPeriodMs;

            int savedTotal = gs.ActionStripSuccessFlashDurationMs;

            try

            {

                HeroActionStripFeedback.ResetForTests();

                var t0 = new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero);

                var clock = t0;

                HeroActionStripFeedback.UtcNowProviderForTests = () => clock;



                gs.ActionStripSuccessFlashPulseHalfPeriodMs = 100;

                gs.ActionStripSuccessFlashDurationMs = 3000;

                gs.ValidateAndFix();



                HeroActionStripFeedback.Trigger(0, HeroActionStripFlashKind.Hit);

                TestBase.AssertTrue(

                    HeroActionStripFeedback.IsFlashEmphasisActive(0),

                    "Flash emphasis on during first half-period",

                    ref run, ref passed, ref failed);



                clock = t0.AddMilliseconds(150);

                TestBase.AssertTrue(

                    !HeroActionStripFeedback.TryGetBorderOverride(0, out _),

                    "Off phase: no color override",

                    ref run, ref passed, ref failed);

                TestBase.AssertTrue(

                    HeroActionStripFeedback.IsFlashEmphasisActive(0),

                    "Flash emphasis still on during off phase (thick border window)",

                    ref run, ref passed, ref failed);

            }

            finally

            {

                gs.ActionStripSuccessFlashPulseHalfPeriodMs = savedPulse;

                gs.ActionStripSuccessFlashDurationMs = savedTotal;

                gs.ValidateAndFix();

                HeroActionStripFeedback.ResetForTests();

            }

        }



        private static void TestFlashEmphasisWrongPanel(ref int run, ref int passed, ref int failed)

        {

            try

            {

                HeroActionStripFeedback.ResetForTests();

                HeroActionStripFeedback.Trigger(2, HeroActionStripFlashKind.Hit);

                TestBase.AssertTrue(

                    !HeroActionStripFeedback.IsFlashEmphasisActive(0),

                    "Flash on panel 2 does not emphasize panel 0",

                    ref run, ref passed, ref failed);

                TestBase.AssertTrue(

                    HeroActionStripFeedback.IsFlashEmphasisActive(2),

                    "Flash emphasis on triggered panel",

                    ref run, ref passed, ref failed);

            }

            finally

            {

                HeroActionStripFeedback.ResetForTests();

            }

        }

    }

}


