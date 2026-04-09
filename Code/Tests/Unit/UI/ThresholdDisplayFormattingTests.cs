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
    }
}
