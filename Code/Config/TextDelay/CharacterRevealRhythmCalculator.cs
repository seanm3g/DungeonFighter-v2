using System;
using System.Collections.Generic;

namespace RPGGame.Config.TextDelay
{
    /// <summary>
    /// Sentence/word/position context for one character of a typewriter reveal.
    /// </summary>
    public readonly struct CharacterRevealContext
    {
        public int SentenceLen { get; }
        public int WordLen { get; }
        public int IndexInWord { get; }

        public CharacterRevealContext(int sentenceLen, int wordLen, int indexInWord)
        {
            SentenceLen = Math.Max(1, sentenceLen);
            WordLen = Math.Max(1, wordLen);
            IndexInWord = Math.Clamp(indexInWord, 0, WordLen - 1);
        }

        /// <summary>Neutral context used when rhythm is disabled or boundaries are unknown.</summary>
        public static CharacterRevealContext Neutral { get; } = new CharacterRevealContext(40, 6, 0);
    }

    /// <summary>
    /// Pure calculator for sentence/word-aware character reveal delays and word-emphasis curves.
    /// </summary>
    public static class CharacterRevealRhythmCalculator
    {
        public const string PresetFlat = "Flat";
        public const string PresetEmphasizeStart = "EmphasizeStart";
        public const string PresetEmphasizeMid = "EmphasizeMid";
        public const string PresetEmphasizeEnd = "EmphasizeEnd";

        /// <summary>
        /// True when F7 should allocate a fixed paragraph budget (sentence → word → char)
        /// instead of the legacy multiplicative base×scales×curve path.
        /// </summary>
        public static bool UsesParagraphBudget(CharacterRevealRhythmConfig? config) =>
            config != null && config.Enabled && config.ParagraphTargetMs > 0;

        /// <summary>
        /// Fills Begin/Mid/End weights for a named emphasis preset.
        /// </summary>
        public static void ApplyPresetWeights(string preset, out double begin, out double mid, out double end)
        {
            switch (NormalizePresetName(preset))
            {
                case PresetEmphasizeStart:
                    begin = 1.6;
                    mid = 1.0;
                    end = 0.85;
                    break;
                case PresetEmphasizeMid:
                    begin = 0.9;
                    mid = 1.5;
                    end = 0.9;
                    break;
                case PresetEmphasizeEnd:
                    begin = 0.85;
                    mid = 1.0;
                    end = 1.6;
                    break;
                default:
                    begin = 1.0;
                    mid = 1.0;
                    end = 1.0;
                    break;
            }
        }

        public static string NormalizePresetName(string? preset)
        {
            if (string.IsNullOrWhiteSpace(preset))
                return PresetFlat;
            if (string.Equals(preset, PresetEmphasizeStart, StringComparison.OrdinalIgnoreCase))
                return PresetEmphasizeStart;
            if (string.Equals(preset, PresetEmphasizeMid, StringComparison.OrdinalIgnoreCase))
                return PresetEmphasizeMid;
            if (string.Equals(preset, PresetEmphasizeEnd, StringComparison.OrdinalIgnoreCase))
                return PresetEmphasizeEnd;
            return PresetFlat;
        }

        /// <summary>
        /// Inverse length scale: shorter text → higher multiplier (slower reveal).
        /// </summary>
        public static double InverseLengthScale(int length, int referenceChars, double scaleMin, double scaleMax)
        {
            int len = Math.Max(1, length);
            int reference = Math.Max(1, referenceChars);
            double raw = (double)reference / len;
            double min = Math.Min(scaleMin, scaleMax);
            double max = Math.Max(scaleMin, scaleMax);
            return Math.Clamp(raw, min, max);
        }

        /// <summary>
        /// Piecewise-linear sample of Begin → Mid → End at position in word.
        /// Single-glyph words use Begin.
        /// </summary>
        public static double SampleWordCurve(int indexInWord, int wordLen, double begin, double mid, double end)
        {
            int len = Math.Max(1, wordLen);
            if (len == 1)
                return begin;

            int index = Math.Clamp(indexInWord, 0, len - 1);
            double t = (double)index / (len - 1);
            if (t <= 0.5)
            {
                double local = t / 0.5;
                return Lerp(begin, mid, local);
            }

            double localEnd = (t - 0.5) / 0.5;
            return Lerp(mid, end, localEnd);
        }

        /// <summary>
        /// Builds a per-index delay schedule so content-char delays sum ≈ <paramref name="targetMs"/>.
        /// Budget is split equally across sentences, then words, then characters.
        /// Soft-wrap newlines get 0 and are excluded from the budget. Rounding is at the char level.
        /// </summary>
        public static int[] BuildParagraphSchedule(string plainText, int targetMs)
        {
            if (string.IsNullOrEmpty(plainText))
                return Array.Empty<int>();

            int[] schedule = new int[plainText.Length];
            int budget = Math.Max(0, targetMs);
            if (budget <= 0)
                return schedule;

            var sentences = CollectSentenceRanges(plainText);
            if (sentences.Count == 0)
                return schedule;

            double sentenceBudget = (double)budget / sentences.Count;
            foreach (var (sentenceStart, sentenceEnd) in sentences)
            {
                var words = CollectWordRanges(plainText, sentenceStart, sentenceEnd);
                if (words.Count == 0)
                    continue;

                double wordBudget = sentenceBudget / words.Count;
                foreach (var (wordStart, wordEnd) in words)
                {
                    int charCount = Math.Max(1, wordEnd - wordStart);
                    int charDelay = (int)Math.Round(wordBudget / charCount, MidpointRounding.AwayFromZero);
                    if (charDelay < 0)
                        charDelay = 0;
                    for (int i = wordStart; i < wordEnd; i++)
                        schedule[i] = charDelay;
                }
            }

            return schedule;
        }

        /// <summary>
        /// Convenience overload using <see cref="CharacterRevealRhythmConfig.ParagraphTargetMs"/>.
        /// Returns an all-zero schedule when budget mode is off (caller should use legacy path).
        /// </summary>
        public static int[] BuildParagraphSchedule(string plainText, CharacterRevealRhythmConfig config)
        {
            if (!UsesParagraphBudget(config))
            {
                if (string.IsNullOrEmpty(plainText))
                    return Array.Empty<int>();
                return new int[plainText.Length];
            }

            return BuildParagraphSchedule(plainText, config.ParagraphTargetMs);
        }

        /// <summary>
        /// Plain length covering the first <paramref name="contentCharCount"/> non-linebreak characters
        /// in <paramref name="wrappedPlain"/> (soft-wrap newlines are skipped in the count).
        /// </summary>
        public static int WrappedLengthForContentCount(string wrappedPlain, int contentCharCount)
        {
            if (string.IsNullOrEmpty(wrappedPlain) || contentCharCount <= 0)
                return 0;

            int seen = 0;
            for (int i = 0; i < wrappedPlain.Length; i++)
            {
                char c = wrappedPlain[i];
                if (c == '\n' || c == '\r')
                    continue;
                seen++;
                if (seen >= contentCharCount)
                    return i + 1;
            }

            return wrappedPlain.Length;
        }

        /// <summary>Counts non-linebreak characters in plain text.</summary>
        public static int CountContentChars(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return 0;
            int count = 0;
            for (int i = 0; i < plainText.Length; i++)
            {
                char c = plainText[i];
                if (c != '\n' && c != '\r')
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Computes per-character delay in ms before combat-speed scaling.
        /// When <paramref name="config"/>.Enabled is false, uses flat base + battle ramp (legacy path).
        /// Prefer <see cref="BuildParagraphSchedule"/> when <see cref="UsesParagraphBudget"/> is true.
        /// </summary>
        public static int ComputeCharDelayMs(
            CharacterRevealRhythmConfig config,
            in CharacterRevealContext ctx,
            int charsTyped)
        {
            if (config == null)
                config = CharacterRevealRhythmConfig.CreateDefault();

            int baseMs = Math.Max(0, config.BaseCharDelayMs);
            int minMs = Math.Max(0, config.MinCharDelayMs);
            int maxMs = Math.Max(minMs, config.MaxCharDelayMs);
            int rampChars = Math.Max(1, config.BattleRampChars);
            int typed = Math.Max(0, charsTyped);
            int rampBonus = typed / rampChars;

            if (!config.Enabled)
            {
                // Legacy flat path: min(base + ramp, max) using Max as the legacy ceiling.
                int legacyMax = maxMs;
                if (legacyMax < baseMs)
                    legacyMax = baseMs;
                return Math.Min(baseMs + rampBonus, legacyMax);
            }

            double sentenceScale = InverseLengthScale(
                ctx.SentenceLen,
                config.SentenceReferenceChars,
                config.SentenceScaleMin,
                config.SentenceScaleMax);
            double wordScale = InverseLengthScale(
                ctx.WordLen,
                config.WordReferenceChars,
                config.WordScaleMin,
                config.WordScaleMax);
            double curve = SampleWordCurve(
                ctx.IndexInWord,
                ctx.WordLen,
                config.WordBeginWeight,
                config.WordMidWeight,
                config.WordEndWeight);

            double rhythm = baseMs * sentenceScale * wordScale * curve;
            int delayMs = (int)Math.Round(rhythm + rampBonus, MidpointRounding.AwayFromZero);
            return Math.Clamp(delayMs, minMs, maxMs);
        }

        /// <summary>
        /// Resolves sentence/word/position context for the character at <paramref name="charIndex"/> in plain text.
        /// Newlines are ignored for length counts. Spaces and punctuation are 1-char words;
        /// letter/digit runs (including apostrophes) are multi-char words.
        /// </summary>
        public static CharacterRevealContext ResolveContext(string plainText, int charIndex)
        {
            if (string.IsNullOrEmpty(plainText))
                return CharacterRevealContext.Neutral;

            int index = Math.Clamp(charIndex, 0, plainText.Length - 1);
            char c = plainText[index];
            if (c == '\n' || c == '\r')
                return CharacterRevealContext.Neutral;

            int sentenceStart = FindSentenceStart(plainText, index);
            int sentenceEnd = FindSentenceEnd(plainText, index);
            int sentenceLen = Math.Max(1, CountContentCharsInRange(plainText, sentenceStart, sentenceEnd));

            int wordStart = FindWordStart(plainText, index);
            int wordEnd = FindWordEnd(plainText, index);
            int wordLen = Math.Max(1, wordEnd - wordStart);
            int indexInWord = Math.Clamp(index - wordStart, 0, wordLen - 1);

            return new CharacterRevealContext(sentenceLen, wordLen, indexInWord);
        }

        private static List<(int Start, int End)> CollectSentenceRanges(string text)
        {
            var ranges = new List<(int Start, int End)>();
            int i = 0;
            while (i < text.Length)
            {
                while (i < text.Length && (text[i] == '\n' || text[i] == '\r'))
                    i++;
                if (i >= text.Length)
                    break;

                int start = i;
                int end = FindSentenceEnd(text, start);
                if (end <= start)
                    end = text.Length;

                // Trim trailing newlines from the sentence range for word collection.
                int contentEnd = end;
                while (contentEnd > start && (text[contentEnd - 1] == '\n' || text[contentEnd - 1] == '\r'))
                    contentEnd--;

                if (contentEnd > start && CountContentCharsInRange(text, start, contentEnd) > 0)
                    ranges.Add((start, contentEnd));

                i = Math.Max(end, start + 1);
            }

            return ranges;
        }

        private static List<(int Start, int End)> CollectWordRanges(string text, int sentenceStart, int sentenceEnd)
        {
            var ranges = new List<(int Start, int End)>();
            int i = Math.Max(0, sentenceStart);
            int end = Math.Min(text.Length, sentenceEnd);
            while (i < end)
            {
                if (text[i] == '\n' || text[i] == '\r')
                {
                    i++;
                    continue;
                }

                int wordStart = FindWordStart(text, i);
                int wordEnd = FindWordEnd(text, i);
                if (wordEnd > end)
                    wordEnd = end;
                if (wordStart < end && wordEnd > wordStart)
                    ranges.Add((wordStart, wordEnd));
                i = Math.Max(wordEnd, i + 1);
            }

            return ranges;
        }

        private static int FindSentenceStart(string text, int index)
        {
            for (int i = index - 1; i >= 0; i--)
            {
                char c = text[i];
                if (c == '.' || c == '!' || c == '?')
                    return i + 1;
            }
            return 0;
        }

        private static int FindSentenceEnd(string text, int index)
        {
            for (int i = index; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '.' || c == '!' || c == '?')
                    return i + 1;
            }
            return text.Length;
        }

        private static int FindWordStart(string text, int index)
        {
            if (!IsWordBody(text[index]))
                return index;

            int start = index;
            while (start > 0 && IsWordBody(text[start - 1]))
                start--;
            return start;
        }

        private static int FindWordEnd(string text, int index)
        {
            if (!IsWordBody(text[index]))
                return index + 1;

            int end = index + 1;
            while (end < text.Length && IsWordBody(text[end]))
                end++;
            return end;
        }

        /// <summary>
        /// Letters/digits/apostrophe form multi-char words; spaces and punctuation are 1-char words.
        /// </summary>
        private static bool IsWordBody(char c) =>
            char.IsLetterOrDigit(c) || c == '\'' || c == '\u2019';

        private static int CountContentCharsInRange(string text, int start, int end)
        {
            int count = 0;
            int s = Math.Max(0, start);
            int e = Math.Min(text.Length, end);
            for (int i = s; i < e; i++)
            {
                char c = text[i];
                if (c != '\n' && c != '\r')
                    count++;
            }
            return count;
        }

        private static double Lerp(double a, double b, double t)
        {
            return a + (b - a) * Math.Clamp(t, 0.0, 1.0);
        }
    }
}
