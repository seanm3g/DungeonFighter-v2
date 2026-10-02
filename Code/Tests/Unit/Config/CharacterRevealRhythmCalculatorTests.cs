using System;
using RPGGame.Config;
using RPGGame.Config.TextDelay;

namespace RPGGame.Tests.Unit.Config
{
    /// <summary>
    /// Unit tests for sentence/word-aware character reveal rhythm calculator.
    /// </summary>
    public static class CharacterRevealRhythmCalculatorTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== CharacterRevealRhythmCalculator Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestShortSentenceSlowerThanLong();
            TestShortWordSlowerThanLong();
            TestWordCurveEmphasizePeaks();
            TestClampAndDisabledFallback();
            TestBattleRampAdditive();
            TestResolveContextSentenceAndWord();
            TestPresetWeights();
            TestParagraphScheduleSumNearTarget();
            TestParagraphScheduleLongerTextFasterChars();
            TestParagraphScheduleNewlinesZero();
            TestParagraphScheduleGrowthRebudget();
            TestUsesParagraphBudgetGate();

            Console.WriteLine($"\nCharacterRevealRhythmCalculator: {_testsPassed}/{_testsRun} passed, {_testsFailed} failed");
        }

        private static CharacterRevealRhythmConfig FlatConfig()
        {
            return new CharacterRevealRhythmConfig
            {
                Enabled = true,
                BaseCharDelayMs = 10,
                MinCharDelayMs = 1,
                MaxCharDelayMs = 100,
                SentenceReferenceChars = 40,
                SentenceScaleMin = 0.5,
                SentenceScaleMax = 2.0,
                WordReferenceChars = 6,
                WordScaleMin = 0.75,
                WordScaleMax = 1.5,
                WordBeginWeight = 1.0,
                WordMidWeight = 1.0,
                WordEndWeight = 1.0,
                BattleRampChars = 200
            };
        }

        private static void TestShortSentenceSlowerThanLong()
        {
            Console.WriteLine("--- Short sentence slower than long ---");
            var config = FlatConfig();
            var shortSentence = new CharacterRevealContext(sentenceLen: 10, wordLen: 6, indexInWord: 0);
            var longSentence = new CharacterRevealContext(sentenceLen: 80, wordLen: 6, indexInWord: 0);
            int shortDelay = CharacterRevealRhythmCalculator.ComputeCharDelayMs(config, shortSentence, 0);
            int longDelay = CharacterRevealRhythmCalculator.ComputeCharDelayMs(config, longSentence, 0);
            AssertTrue(shortDelay > longDelay,
                $"Short sentence delay ({shortDelay}) should exceed long ({longDelay})");
        }

        private static void TestShortWordSlowerThanLong()
        {
            Console.WriteLine("--- Short word slower than long ---");
            var config = FlatConfig();
            var shortWord = new CharacterRevealContext(sentenceLen: 40, wordLen: 2, indexInWord: 0);
            var longWord = new CharacterRevealContext(sentenceLen: 40, wordLen: 12, indexInWord: 0);
            int shortDelay = CharacterRevealRhythmCalculator.ComputeCharDelayMs(config, shortWord, 0);
            int longDelay = CharacterRevealRhythmCalculator.ComputeCharDelayMs(config, longWord, 0);
            AssertTrue(shortDelay > longDelay,
                $"Short word delay ({shortDelay}) should exceed long ({longDelay})");
        }

        private static void TestWordCurveEmphasizePeaks()
        {
            Console.WriteLine("--- Word curve emphasize begin/mid/end ---");
            var config = FlatConfig();
            config.WordBeginWeight = 2.0;
            config.WordMidWeight = 1.0;
            config.WordEndWeight = 1.0;
            int begin = CharacterRevealRhythmCalculator.ComputeCharDelayMs(
                config, new CharacterRevealContext(40, 5, 0), 0);
            int mid = CharacterRevealRhythmCalculator.ComputeCharDelayMs(
                config, new CharacterRevealContext(40, 5, 2), 0);
            int end = CharacterRevealRhythmCalculator.ComputeCharDelayMs(
                config, new CharacterRevealContext(40, 5, 4), 0);
            AssertTrue(begin > mid && begin > end,
                $"Emphasize begin: begin={begin} should peak vs mid={mid} end={end}");

            config.WordBeginWeight = 1.0;
            config.WordMidWeight = 2.0;
            config.WordEndWeight = 1.0;
            begin = CharacterRevealRhythmCalculator.ComputeCharDelayMs(
                config, new CharacterRevealContext(40, 5, 0), 0);
            mid = CharacterRevealRhythmCalculator.ComputeCharDelayMs(
                config, new CharacterRevealContext(40, 5, 2), 0);
            end = CharacterRevealRhythmCalculator.ComputeCharDelayMs(
                config, new CharacterRevealContext(40, 5, 4), 0);
            AssertTrue(mid > begin && mid > end,
                $"Emphasize mid: mid={mid} should peak vs begin={begin} end={end}");

            config.WordBeginWeight = 1.0;
            config.WordMidWeight = 1.0;
            config.WordEndWeight = 2.0;
            begin = CharacterRevealRhythmCalculator.ComputeCharDelayMs(
                config, new CharacterRevealContext(40, 5, 0), 0);
            mid = CharacterRevealRhythmCalculator.ComputeCharDelayMs(
                config, new CharacterRevealContext(40, 5, 2), 0);
            end = CharacterRevealRhythmCalculator.ComputeCharDelayMs(
                config, new CharacterRevealContext(40, 5, 4), 0);
            AssertTrue(end > begin && end > mid,
                $"Emphasize end: end={end} should peak vs begin={begin} mid={mid}");
        }

        private static void TestClampAndDisabledFallback()
        {
            Console.WriteLine("--- Clamp and disabled flat fallback ---");
            var config = FlatConfig();
            config.BaseCharDelayMs = 10;
            config.MinCharDelayMs = 5;
            config.MaxCharDelayMs = 12;
            config.SentenceScaleMax = 3.0;
            // Very short sentence → would want 30ms but clamps to 12
            int clamped = CharacterRevealRhythmCalculator.ComputeCharDelayMs(
                config, new CharacterRevealContext(5, 6, 0), 0);
            AssertTrue(clamped == 12, $"Enabled clamp should be 12 (got {clamped})");

            config.Enabled = false;
            config.BaseCharDelayMs = 3;
            config.MaxCharDelayMs = 10;
            config.BattleRampChars = 200;
            int flat0 = CharacterRevealRhythmCalculator.ComputeCharDelayMs(
                config, new CharacterRevealContext(5, 2, 0), 0);
            int flat200 = CharacterRevealRhythmCalculator.ComputeCharDelayMs(
                config, new CharacterRevealContext(5, 2, 0), 200);
            int flatCap = CharacterRevealRhythmCalculator.ComputeCharDelayMs(
                config, new CharacterRevealContext(5, 2, 0), 2000);
            AssertTrue(flat0 == 3, $"Disabled base should be 3 (got {flat0})");
            AssertTrue(flat200 == 4, $"Disabled +1 ramp should be 4 (got {flat200})");
            AssertTrue(flatCap == 10, $"Disabled cap should be 10 (got {flatCap})");
        }

        private static void TestBattleRampAdditive()
        {
            Console.WriteLine("--- Battle ramp additive on rhythm ---");
            var config = FlatConfig();
            config.BaseCharDelayMs = 10;
            var ctx = new CharacterRevealContext(40, 6, 0); // scales = 1
            int at0 = CharacterRevealRhythmCalculator.ComputeCharDelayMs(config, ctx, 0);
            int at200 = CharacterRevealRhythmCalculator.ComputeCharDelayMs(config, ctx, 200);
            AssertTrue(at0 == 10, $"Base rhythm should be 10 (got {at0})");
            AssertTrue(at200 == 11, $"Ramp should add +1 → 11 (got {at200})");
        }

        private static void TestResolveContextSentenceAndWord()
        {
            Console.WriteLine("--- ResolveContext sentence/word boundaries ---");
            string text = "Hi. Longer sentence here!";
            // 'H' of Hi
            var hi = CharacterRevealRhythmCalculator.ResolveContext(text, 0);
            AssertTrue(hi.SentenceLen == 3, $"'Hi.' sentence len should be 3 (got {hi.SentenceLen})");
            AssertTrue(hi.WordLen == 2 && hi.IndexInWord == 0,
                $"'H' word should be len 2 index 0 (got {hi.WordLen}/{hi.IndexInWord})");

            // space after Hi.
            int spaceIdx = text.IndexOf(' ');
            var space = CharacterRevealRhythmCalculator.ResolveContext(text, spaceIdx);
            AssertTrue(space.WordLen == 1 && space.IndexInWord == 0,
                $"Space should be 1-char word (got {space.WordLen}/{space.IndexInWord})");

            // 'L' of Longer
            int longerIdx = text.IndexOf('L');
            var longer = CharacterRevealRhythmCalculator.ResolveContext(text, longerIdx);
            AssertTrue(longer.SentenceLen > hi.SentenceLen,
                $"Second sentence should be longer than first ({longer.SentenceLen} vs {hi.SentenceLen})");
            AssertTrue(longer.WordLen == 6 && longer.IndexInWord == 0,
                $"'Longer' should be len 6 (got {longer.WordLen}/{longer.IndexInWord})");
        }

        private static void TestPresetWeights()
        {
            Console.WriteLine("--- Preset weight fills ---");
            CharacterRevealRhythmCalculator.ApplyPresetWeights(
                CharacterRevealRhythmCalculator.PresetEmphasizeEnd, out double b, out double m, out double e);
            AssertTrue(e > b && e > m, $"EmphasizeEnd should peak at end (b={b}, m={m}, e={e})");
            AssertTrue(
                CharacterRevealRhythmCalculator.NormalizePresetName("emphasizestart") ==
                CharacterRevealRhythmCalculator.PresetEmphasizeStart,
                "NormalizePresetName should accept case-insensitive EmphasizeStart");
        }

        private static void TestParagraphScheduleSumNearTarget()
        {
            Console.WriteLine("--- Paragraph schedule sum near target ---");
            const int target = 8000;
            string text = "You gather yourself. The Goblin snarls. Steel finds a seam.";
            int[] schedule = CharacterRevealRhythmCalculator.BuildParagraphSchedule(text, target);
            AssertTrue(schedule.Length == text.Length, "Schedule length matches plain text");

            long sum = 0;
            for (int i = 0; i < schedule.Length; i++)
            {
                if (text[i] == '\n' || text[i] == '\r')
                    AssertTrue(schedule[i] == 0, "Newline delay is 0");
                else
                    sum += schedule[i];
            }

            // Rounding at char level — allow ~15% slack around the target.
            AssertTrue(sum > target * 0.85 && sum < target * 1.15,
                $"Sum of content delays ({sum}) should be near {target}");
        }

        private static void TestParagraphScheduleLongerTextFasterChars()
        {
            Console.WriteLine("--- Longer paragraph → shorter mean char delay ---");
            const int target = 8000;
            string shortText = "Hi.";
            string longText = "You gather yourself for the swing. The Goblin snarls and plants its feet. Steel finds a seam between the plates.";
            int[] shortSched = CharacterRevealRhythmCalculator.BuildParagraphSchedule(shortText, target);
            int[] longSched = CharacterRevealRhythmCalculator.BuildParagraphSchedule(longText, target);

            double Mean(int[] s, string t)
            {
                long sum = 0;
                int n = 0;
                for (int i = 0; i < s.Length; i++)
                {
                    if (t[i] == '\n' || t[i] == '\r') continue;
                    sum += s[i];
                    n++;
                }
                return n == 0 ? 0 : (double)sum / n;
            }

            double shortMean = Mean(shortSched, shortText);
            double longMean = Mean(longSched, longText);
            AssertTrue(shortMean > longMean,
                $"Short mean ({shortMean:F1}) should exceed long mean ({longMean:F1})");
        }

        private static void TestParagraphScheduleNewlinesZero()
        {
            Console.WriteLine("--- Soft-wrap newlines get zero delay ---");
            string text = "Hello.\nWorld.";
            int[] schedule = CharacterRevealRhythmCalculator.BuildParagraphSchedule(text, 1000);
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                    AssertTrue(schedule[i] == 0, $"Newline at {i} has 0 delay");
            }
        }

        private static void TestParagraphScheduleGrowthRebudget()
        {
            Console.WriteLine("--- Growth re-budgets full paragraph ---");
            const int target = 8000;
            string first = "You gather yourself.";
            string grown = "You gather yourself. The Goblin snarls back.";
            int[] firstSched = CharacterRevealRhythmCalculator.BuildParagraphSchedule(first, target);
            int[] grownSched = CharacterRevealRhythmCalculator.BuildParagraphSchedule(grown, target);

            AssertTrue(grownSched.Length > first.Length, "Grown schedule is longer");
            long sumFirst = 0;
            int nFirst = 0;
            for (int i = 0; i < first.Length; i++)
            {
                if (first[i] == '\n' || first[i] == '\r') continue;
                sumFirst += firstSched[i];
                nFirst++;
            }
            long sumGrownPrefix = 0;
            int nGrown = 0;
            for (int i = 0; i < first.Length && i < grown.Length; i++)
            {
                if (grown[i] == '\n' || grown[i] == '\r') continue;
                sumGrownPrefix += grownSched[i];
                nGrown++;
            }
            double meanFirst = (double)sumFirst / Math.Max(1, nFirst);
            double meanGrownPrefix = (double)sumGrownPrefix / Math.Max(1, nGrown);
            AssertTrue(meanGrownPrefix < meanFirst,
                $"Re-budgeted prefix mean ({meanGrownPrefix:F1}) should be under first mean ({meanFirst:F1})");
        }

        private static void TestUsesParagraphBudgetGate()
        {
            Console.WriteLine("--- UsesParagraphBudget gate ---");
            var on = new CharacterRevealRhythmConfig { Enabled = true, ParagraphTargetMs = 8000 };
            var offTarget = new CharacterRevealRhythmConfig { Enabled = true, ParagraphTargetMs = 0 };
            var disabled = new CharacterRevealRhythmConfig { Enabled = false, ParagraphTargetMs = 8000 };
            AssertTrue(CharacterRevealRhythmCalculator.UsesParagraphBudget(on), "Enabled + target > 0");
            AssertTrue(!CharacterRevealRhythmCalculator.UsesParagraphBudget(offTarget), "Target 0 disables budget");
            AssertTrue(!CharacterRevealRhythmCalculator.UsesParagraphBudget(disabled), "Disabled disables budget");
        }

        private static void AssertTrue(bool condition, string message)
        {
            _testsRun++;
            if (condition)
            {
                _testsPassed++;
                Console.WriteLine($"  PASS: {message}");
            }
            else
            {
                _testsFailed++;
                Console.WriteLine($"  FAIL: {message}");
            }
        }
    }
}
