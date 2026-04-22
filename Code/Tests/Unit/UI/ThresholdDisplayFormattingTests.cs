using RPGGame.Tests;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Layout;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.UI
{
    public static class ThresholdDisplayFormattingTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;

            ValueColorWhiteWhenDefault(ref run, ref passed, ref failed);
            ValueColorGreenWhenLowerThanDefault(ref run, ref passed, ref failed);
            ValueColorRedWhenHigherThanDefault(ref run, ref passed, ref failed);
            DeltaSuffixFormatsBenefitAndPenalty(ref run, ref passed, ref failed);
            DeltaSuffixEmptyWhenDefault(ref run, ref passed, ref failed);
            AccuracyAppendedPositiveNegativeZero(ref run, ref passed, ref failed);
            AccuracyDeltaParenColorSign(ref run, ref passed, ref failed);
            ThresholdValueWithAccuracyPartsMatchesFormat(ref run, ref passed, ref failed);
            ClampDiceLadderFloorsAtOne(ref run, ref passed, ref failed);

            TestBase.PrintSummary("ThresholdDisplayFormattingTests", run, passed, failed);
        }

        private static void ValueColorWhiteWhenDefault(ref int run, ref int passed, ref int failed)
        {
            var c = ThresholdDisplayFormatting.GetValueColor(20, 20);
            TestBase.AssertTrue(
                ColorValidator.AreColorsEqual(c, AsciiArtAssets.Colors.White),
                "unmodified threshold uses white",
                ref run, ref passed, ref failed);
        }

        private static void ValueColorGreenWhenLowerThanDefault(ref int run, ref int passed, ref int failed)
        {
            var c = ThresholdDisplayFormatting.GetValueColor(17, 20);
            TestBase.AssertTrue(
                ColorValidator.AreColorsEqual(c, AsciiArtAssets.Colors.Green),
                "lower threshold (easier) uses green",
                ref run, ref passed, ref failed);
        }

        private static void ValueColorRedWhenHigherThanDefault(ref int run, ref int passed, ref int failed)
        {
            var c = ThresholdDisplayFormatting.GetValueColor(18, 15);
            TestBase.AssertTrue(
                ColorValidator.AreColorsEqual(c, AsciiArtAssets.Colors.Red),
                "higher threshold (harder) uses red",
                ref run, ref passed, ref failed);
        }

        private static void DeltaSuffixFormatsBenefitAndPenalty(ref int run, ref int passed, ref int failed)
        {
            TestBase.AssertEqual(" (+3)", ThresholdDisplayFormatting.FormatDeltaSuffix(17, 20),
                "benefit delta suffix", ref run, ref passed, ref failed);
            TestBase.AssertEqual(" (-2)", ThresholdDisplayFormatting.FormatDeltaSuffix(17, 15),
                "penalty delta suffix", ref run, ref passed, ref failed);
        }

        private static void DeltaSuffixEmptyWhenDefault(ref int run, ref int passed, ref int failed)
        {
            TestBase.AssertEqual("", ThresholdDisplayFormatting.FormatDeltaSuffix(14, 14),
                "no suffix when equal to default", ref run, ref passed, ref failed);
        }

        private static void AccuracyAppendedPositiveNegativeZero(ref int run, ref int passed, ref int failed)
        {
            TestBase.AssertEqual("20", ThresholdDisplayFormatting.FormatThresholdValueWithAccuracy(20, 0),
                "accuracy zero omits suffix", ref run, ref passed, ref failed);
            TestBase.AssertEqual("17 (-3)", ThresholdDisplayFormatting.FormatThresholdValueWithAccuracy(20, 3),
                "bonus lowers effective threshold; delta in parens", ref run, ref passed, ref failed);
            TestBase.AssertEqual("8 (+2)", ThresholdDisplayFormatting.FormatThresholdValueWithAccuracy(6, -2),
                "penalty raises effective threshold; delta in parens", ref run, ref passed, ref failed);
        }

        private static void AccuracyDeltaParenColorSign(ref int run, ref int passed, ref int failed)
        {
            TestBase.AssertTrue(
                ColorValidator.AreColorsEqual(ThresholdDisplayFormatting.GetAccuracyDeltaParenColor(0), AsciiArtAssets.Colors.White),
                "accuracy delta 0 uses white", ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                ColorValidator.AreColorsEqual(ThresholdDisplayFormatting.GetAccuracyDeltaParenColor(2), AsciiArtAssets.Colors.Red),
                "positive accuracy delta (penalty) uses red", ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                ColorValidator.AreColorsEqual(ThresholdDisplayFormatting.GetAccuracyDeltaParenColor(-3), AsciiArtAssets.Colors.Green),
                "negative accuracy delta (bonus) uses green", ref run, ref passed, ref failed);
        }

        private static void ThresholdValueWithAccuracyPartsMatchesFormat(ref int run, ref int passed, ref int failed)
        {
            ThresholdDisplayFormatting.GetThresholdValueWithAccuracyParts(20, 0, out int e0, out string s0, out int d0);
            TestBase.AssertEqual(20, e0, "parts effective when accuracy 0", ref run, ref passed, ref failed);
            TestBase.AssertEqual("", s0, "parts suffix when accuracy 0", ref run, ref passed, ref failed);
            TestBase.AssertEqual(0, d0, "parts delta when accuracy 0", ref run, ref passed, ref failed);

            ThresholdDisplayFormatting.GetThresholdValueWithAccuracyParts(20, 3, out int e1, out string s1, out int d1);
            string combined1 = $"{e1}{s1}";
            TestBase.AssertEqual(ThresholdDisplayFormatting.FormatThresholdValueWithAccuracy(20, 3), combined1,
                "parts match format string for bonus accuracy", ref run, ref passed, ref failed);
            TestBase.AssertEqual(-3, d1, "delta is -accuracy", ref run, ref passed, ref failed);

            ThresholdDisplayFormatting.GetThresholdValueWithAccuracyParts(6, -2, out int e2, out string s2, out int d2);
            string combined2 = $"{e2}{s2}";
            TestBase.AssertEqual(ThresholdDisplayFormatting.FormatThresholdValueWithAccuracy(6, -2), combined2,
                "parts match format string for penalty accuracy", ref run, ref passed, ref failed);
            TestBase.AssertEqual(2, d2, "penalty accuracy gives positive delta", ref run, ref passed, ref failed);
        }

        /// <summary>
        /// Large deferred ACC/COMBO peek shifts can make raw effective = base - shift negative; HUD clamps to 1
        /// (same floor as <see cref="RPGGame.ActionSelector.GetEffectiveComboThresholdForSelection"/>).
        /// </summary>
        private static void ClampDiceLadderFloorsAtOne(ref int run, ref int passed, ref int failed)
        {
            ThresholdDisplayFormatting.GetThresholdValueWithAccuracyParts(14, 40, out int raw, out _, out _);
            TestBase.AssertTrue(raw < 0, "sanity: huge shift yields negative raw effective", ref run, ref passed, ref failed);
            TestBase.AssertEqual(1, ThresholdDisplayFormatting.ClampDiceLadderDisplayValue(raw),
                "ClampDiceLadderDisplayValue floors sub-1", ref run, ref passed, ref failed);
            TestBase.AssertEqual(3, ThresholdDisplayFormatting.ClampDiceLadderDisplayValue(3),
                "ClampDiceLadderDisplayValue leaves positives", ref run, ref passed, ref failed);
        }
    }
}
