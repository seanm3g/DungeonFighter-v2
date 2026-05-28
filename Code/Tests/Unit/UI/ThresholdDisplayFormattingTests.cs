using Avalonia.Media;
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
            SignedDeltaSuffixMatchesHitRowConvention(ref run, ref passed, ref failed);
            AccuracyAppendedPositiveNegativeZero(ref run, ref passed, ref failed);
            AccuracyDeltaParenColorSign(ref run, ref passed, ref failed);
            ThresholdValueWithAccuracyPartsMatchesFormat(ref run, ref passed, ref failed);
            ClampDiceLadderFloorsAtOne(ref run, ref passed, ref failed);
            ExclusiveD20ChancesUseOutcomeBands(ref run, ref passed, ref failed);
            ExclusiveD20ChancesReflectShiftedComboThreshold(ref run, ref passed, ref failed);
            ExclusiveD20MissPercentCompletesHundred(ref run, ref passed, ref failed);
            ExclusiveD20ChanceRowsUseDisplayOrder(ref run, ref passed, ref failed);
            ChanceRowsSortDescendingByPercent(ref run, ref passed, ref failed);
            BuildChancePercentByLabelMapsLabels(ref run, ref passed, ref failed);
            D20ChancePercentFormatsWithinBounds(ref run, ref passed, ref failed);
            ChanceDeltaColorUsesSignedPercentDifference(ref run, ref passed, ref failed);
            D20OutcomeSegmentsDefaultLayout(ref run, ref passed, ref failed);
            D20OutcomeSegmentsSumToTwentyFaces(ref run, ref passed, ref failed);
            D20OutcomeSegmentColorsMatchSpec(ref run, ref passed, ref failed);
            D20OutcomeSegmentsShiftWithComboThreshold(ref run, ref passed, ref failed);
            FindSegmentIndexForRollMatchesDefaultLayout(ref run, ref passed, ref failed);

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

        /// <summary>
        /// Threshold HUD uses one parenthetical: <c>effective − defaultBaseline</c> (see DiceRollThresholdRowsRenderer).
        /// </summary>
        private static void SignedDeltaSuffixMatchesHitRowConvention(ref int run, ref int passed, ref int failed)
        {
            TestBase.AssertEqual("", ThresholdDisplayFormatting.FormatSignedDeltaSuffix(0),
                "signed suffix empty at zero", ref run, ref passed, ref failed);
            TestBase.AssertEqual(" (-4)", ThresholdDisplayFormatting.FormatSignedDeltaSuffix(-4),
                "combined stat + queued shift shows single delta", ref run, ref passed, ref failed);
            TestBase.AssertEqual(" (-2)", ThresholdDisplayFormatting.FormatSignedDeltaSuffix(-2),
                "negative delta suffix", ref run, ref passed, ref failed);
            TestBase.AssertEqual(" (+1)", ThresholdDisplayFormatting.FormatSignedDeltaSuffix(1),
                "positive delta suffix", ref run, ref passed, ref failed);
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

        private static void ExclusiveD20ChancesUseOutcomeBands(ref int run, ref int passed, ref int failed)
        {
            var chances = ThresholdDisplayFormatting.CalculateExclusiveD20OutcomeChances(
                critMinRoll: 20,
                comboMinRoll: 14,
                hitMinRoll: 6,
                critMissMaxRoll: 1);

            TestBase.AssertEqual(5, chances.CritPercent, "default crit chance is one d20 face", ref run, ref passed, ref failed);
            TestBase.AssertEqual(30, chances.ComboPercent, "default combo chance is rolls 14-19", ref run, ref passed, ref failed);
            TestBase.AssertEqual(40, chances.HitPercent, "default normal hit chance is rolls 6-13", ref run, ref passed, ref failed);
            TestBase.AssertEqual(5, chances.CritMissPercent, "default crit miss chance is one d20 face", ref run, ref passed, ref failed);
            TestBase.AssertEqual(20, chances.MissPercent, "remaining d20 faces are plain miss", ref run, ref passed, ref failed);
        }

        private static void ExclusiveD20ChancesReflectShiftedComboThreshold(ref int run, ref int passed, ref int failed)
        {
            var chances = ThresholdDisplayFormatting.CalculateExclusiveD20OutcomeChances(
                critMinRoll: 20,
                comboMinRoll: 12,
                hitMinRoll: 6,
                critMissMaxRoll: 1);

            TestBase.AssertEqual(5, chances.CritPercent, "shifted combo keeps crit chance separate", ref run, ref passed, ref failed);
            TestBase.AssertEqual(40, chances.ComboPercent, "combo 12 means rolls 12-19", ref run, ref passed, ref failed);
            TestBase.AssertEqual(30, chances.HitPercent, "normal hit shrinks to rolls 6-11", ref run, ref passed, ref failed);
            TestBase.AssertEqual(5, chances.CritMissPercent, "crit miss remains one face", ref run, ref passed, ref failed);
            TestBase.AssertEqual(20, chances.MissPercent, "miss fills remainder to 100%", ref run, ref passed, ref failed);
        }

        private static void ExclusiveD20MissPercentCompletesHundred(ref int run, ref int passed, ref int failed)
        {
            var chances = ThresholdDisplayFormatting.CalculateExclusiveD20OutcomeChances(
                critMinRoll: 20,
                comboMinRoll: 14,
                hitMinRoll: 6,
                critMissMaxRoll: 1);
            int sum = chances.CritPercent + chances.ComboPercent + chances.HitPercent + chances.CritMissPercent + chances.MissPercent;
            TestBase.AssertEqual(100, sum, "crit+combo+hit+crit miss+miss sums to 100%", ref run, ref passed, ref failed);
        }

        private static void ExclusiveD20ChanceRowsUseDisplayOrder(ref int run, ref int passed, ref int failed)
        {
            var chances = ThresholdDisplayFormatting.CalculateExclusiveD20OutcomeChances(
                critMinRoll: 20,
                comboMinRoll: 14,
                hitMinRoll: 6,
                critMissMaxRoll: 1);
            var rows = ThresholdDisplayFormatting.GetExclusiveD20ChanceRows(chances);

            TestBase.AssertEqual("Crit", rows[0].Label, "chance row 1 label", ref run, ref passed, ref failed);
            TestBase.AssertEqual(5, rows[0].Percent, "chance row 1 percent", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Combo", rows[1].Label, "chance row 2 label", ref run, ref passed, ref failed);
            TestBase.AssertEqual(30, rows[1].Percent, "chance row 2 percent", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Hit", rows[2].Label, "chance row 3 label", ref run, ref passed, ref failed);
            TestBase.AssertEqual(40, rows[2].Percent, "chance row 3 percent", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Miss", rows[3].Label, "chance row 4 label", ref run, ref passed, ref failed);
            TestBase.AssertEqual(20, rows[3].Percent, "chance row 4 percent", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Crit Miss", rows[4].Label, "chance row 5 label", ref run, ref passed, ref failed);
            TestBase.AssertEqual(5, rows[4].Percent, "chance row 5 percent", ref run, ref passed, ref failed);
        }

        private static void ChanceRowsSortDescendingByPercent(ref int run, ref int passed, ref int failed)
        {
            var rows = new[]
            {
                new ThresholdDisplayFormatting.D20ChanceDisplayRow("Low", 10),
                new ThresholdDisplayFormatting.D20ChanceDisplayRow("High", 40),
                new ThresholdDisplayFormatting.D20ChanceDisplayRow("Mid", 25)
            };
            var sorted = ThresholdDisplayFormatting.SortChanceRowsByPercentDescending(rows);
            TestBase.AssertEqual("High", sorted[0].Label, "sort puts highest percent first", ref run, ref passed, ref failed);
            TestBase.AssertEqual(40, sorted[0].Percent, "highest percent value", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Mid", sorted[1].Label, "sort middle", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Low", sorted[2].Label, "sort puts lowest last", ref run, ref passed, ref failed);

            var tie = new[]
            {
                new ThresholdDisplayFormatting.D20ChanceDisplayRow("Zebra", 15),
                new ThresholdDisplayFormatting.D20ChanceDisplayRow("Apple", 15)
            };
            var tieSorted = ThresholdDisplayFormatting.SortChanceRowsByPercentDescending(tie);
            TestBase.AssertEqual("Apple", tieSorted[0].Label, "equal percent: alphabetical label order", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Zebra", tieSorted[1].Label, "equal percent: second label", ref run, ref passed, ref failed);
        }

        private static void BuildChancePercentByLabelMapsLabels(ref int run, ref int passed, ref int failed)
        {
            var chances = ThresholdDisplayFormatting.CalculateExclusiveD20OutcomeChances(20, 14, 6, 1);
            var canonical = ThresholdDisplayFormatting.GetExclusiveD20ChanceRows(chances);
            var map = ThresholdDisplayFormatting.BuildChancePercentByLabel(canonical);
            TestBase.AssertTrue(map.TryGetValue("Crit Miss", out int cm) && cm == 5, "Crit Miss label maps", ref run, ref passed, ref failed);
            TestBase.AssertTrue(map.TryGetValue("Miss", out int m) && m == 20, "Miss label maps", ref run, ref passed, ref failed);
        }

        private static void D20ChancePercentFormatsWithinBounds(ref int run, ref int passed, ref int failed)
        {
            TestBase.AssertEqual("0%", ThresholdDisplayFormatting.FormatD20ChancePercent(-5),
                "chance percent clamps low", ref run, ref passed, ref failed);
            TestBase.AssertEqual("45%", ThresholdDisplayFormatting.FormatD20ChancePercent(45),
                "chance percent formats d20 increment", ref run, ref passed, ref failed);
            TestBase.AssertEqual("100%", ThresholdDisplayFormatting.FormatD20ChancePercent(105),
                "chance percent clamps high", ref run, ref passed, ref failed);
        }

        private static void ChanceDeltaColorUsesSignedPercentDifference(ref int run, ref int passed, ref int failed)
        {
            TestBase.AssertTrue(
                ColorValidator.AreColorsEqual(ThresholdDisplayFormatting.GetChanceDeltaColor(30, 30), AsciiArtAssets.Colors.White),
                "unchanged chance percent uses white", ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                ColorValidator.AreColorsEqual(ThresholdDisplayFormatting.GetChanceDeltaColor(35, 30), AsciiArtAssets.Colors.Green),
                "positive chance percent delta uses green", ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                ColorValidator.AreColorsEqual(ThresholdDisplayFormatting.GetChanceDeltaColor(25, 30), AsciiArtAssets.Colors.Red),
                "negative chance percent delta uses red", ref run, ref passed, ref failed);
        }

        private static void D20OutcomeSegmentsDefaultLayout(ref int run, ref int passed, ref int failed)
        {
            var segments = ThresholdDisplayFormatting.BuildD20OutcomeSegments(20, 14, 6, 1);
            TestBase.AssertEqual(5, segments.Length, "default thresholds yield five segments", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Crit Miss", segments[0].Label, "segment 1 label", ref run, ref passed, ref failed);
            TestBase.AssertEqual(1, segments[0].FaceCount, "segment 1 faces", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Miss", segments[1].Label, "segment 2 label", ref run, ref passed, ref failed);
            TestBase.AssertEqual(4, segments[1].FaceCount, "segment 2 faces", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Hit", segments[2].Label, "segment 3 label", ref run, ref passed, ref failed);
            TestBase.AssertEqual(8, segments[2].FaceCount, "segment 3 faces", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Combo", segments[3].Label, "segment 4 label", ref run, ref passed, ref failed);
            TestBase.AssertEqual(6, segments[3].FaceCount, "segment 4 faces", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Crit", segments[4].Label, "segment 5 label", ref run, ref passed, ref failed);
            TestBase.AssertEqual(1, segments[4].FaceCount, "segment 5 faces", ref run, ref passed, ref failed);
        }

        private static void D20OutcomeSegmentsSumToTwentyFaces(ref int run, ref int passed, ref int failed)
        {
            var segments = ThresholdDisplayFormatting.BuildD20OutcomeSegments(18, 12, 7, 2);
            int sum = 0;
            for (int i = 0; i < segments.Length; i++)
                sum += segments[i].FaceCount;
            TestBase.AssertEqual(20, sum, "segment face counts sum to d20", ref run, ref passed, ref failed);
        }

        private static void D20OutcomeSegmentColorsMatchSpec(ref int run, ref int passed, ref int failed)
        {
            TestBase.AssertTrue(
                ColorValidator.AreColorsEqual(
                    ThresholdDisplayFormatting.GetD20OutcomeColor("Crit Miss"),
                    ThresholdDisplayFormatting.D20ThresholdBarColors.CritMiss),
                "Crit Miss uses #C0392B", ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                ColorValidator.AreColorsEqual(
                    ThresholdDisplayFormatting.GetD20OutcomeColor("Miss"),
                    ThresholdDisplayFormatting.D20ThresholdBarColors.Miss),
                "Miss uses #E07B54", ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                ColorValidator.AreColorsEqual(
                    ThresholdDisplayFormatting.GetD20OutcomeColor("Hit"),
                    ThresholdDisplayFormatting.D20ThresholdBarColors.Hit),
                "Hit uses #5B8DB8", ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                ColorValidator.AreColorsEqual(
                    ThresholdDisplayFormatting.GetD20OutcomeColor("Combo"),
                    ThresholdDisplayFormatting.D20ThresholdBarColors.Combo),
                "Combo uses #9B59B6", ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                ColorValidator.AreColorsEqual(
                    ThresholdDisplayFormatting.GetD20OutcomeColor("Crit"),
                    ThresholdDisplayFormatting.D20ThresholdBarColors.Crit),
                "Crit uses #F0C040", ref run, ref passed, ref failed);
        }

        private static void D20OutcomeSegmentsShiftWithComboThreshold(ref int run, ref int passed, ref int failed)
        {
            var segments = ThresholdDisplayFormatting.BuildD20OutcomeSegments(20, 10, 6, 1);
            int comboFaces = 0;
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i].Label == "Combo")
                    comboFaces = segments[i].FaceCount;
            }
            TestBase.AssertEqual(10, comboFaces, "lower combo threshold widens combo band to 10 faces", ref run, ref passed, ref failed);
        }

        private static void FindSegmentIndexForRollMatchesDefaultLayout(ref int run, ref int passed, ref int failed)
        {
            const int crit = 20, combo = 14, hit = 6, critMiss = 1;
            TestBase.AssertEqual(0, ThresholdDisplayFormatting.FindSegmentIndexForRoll(1, crit, combo, hit, critMiss),
                "roll 1 is crit miss segment", ref run, ref passed, ref failed);
            TestBase.AssertEqual(1, ThresholdDisplayFormatting.FindSegmentIndexForRoll(5, crit, combo, hit, critMiss),
                "roll 5 is miss segment", ref run, ref passed, ref failed);
            TestBase.AssertEqual(2, ThresholdDisplayFormatting.FindSegmentIndexForRoll(10, crit, combo, hit, critMiss),
                "roll 10 is hit segment", ref run, ref passed, ref failed);
            TestBase.AssertEqual(3, ThresholdDisplayFormatting.FindSegmentIndexForRoll(15, crit, combo, hit, critMiss),
                "roll 15 is combo segment", ref run, ref passed, ref failed);
            TestBase.AssertEqual(4, ThresholdDisplayFormatting.FindSegmentIndexForRoll(20, crit, combo, hit, critMiss),
                "roll 20 is crit segment", ref run, ref passed, ref failed);
        }
    }
}
