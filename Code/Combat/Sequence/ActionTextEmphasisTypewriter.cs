using System;

namespace RPGGame.Combat.Sequence
{
    /// <summary>
    /// Whole-word typewriter jumps for DistortionPulse actions in F7 narrative prose.
    /// </summary>
    public static class ActionTextEmphasisTypewriter
    {
        /// <summary>
        /// Next visible length after <paramref name="currentVisibleLength"/> when typing toward
        /// <paramref name="targetVisibleLength"/>. DistortionPulse action words dump in one beat.
        /// </summary>
        public static int NextVisibleLength(
            string plainWrapped,
            int currentVisibleLength,
            int targetVisibleLength,
            string? actionName)
        {
            if (string.IsNullOrEmpty(plainWrapped))
                return targetVisibleLength;

            int from = Math.Max(0, currentVisibleLength);
            int to = Math.Clamp(targetVisibleLength, from, plainWrapped.Length);
            if (from >= to)
                return from;

            int next = from + 1;
            if (!ActionTextEffectCatalog.IsDistortionPulse(actionName))
                return Math.Min(next, to);

            if (TryGetWordEndExclusive(plainWrapped, from, actionName!, out int wordEnd))
                return Math.Min(to, Math.Max(next, wordEnd));

            return Math.Min(next, to);
        }

        /// <summary>
        /// True when advancing from <paramref name="previousVisibleLength"/> to
        /// <paramref name="newVisibleLength"/> completes a DistortionPulse action word.
        /// </summary>
        public static bool CompletesDistortionPulseWord(
            string plainWrapped,
            int previousVisibleLength,
            int newVisibleLength,
            string? actionName)
        {
            if (!ActionTextEffectCatalog.IsDistortionPulse(actionName))
                return false;
            if (string.IsNullOrEmpty(plainWrapped) || newVisibleLength <= previousVisibleLength)
                return false;

            if (!TryGetWordEndExclusive(plainWrapped, previousVisibleLength, actionName!, out int wordEnd))
                return false;

            return previousVisibleLength < wordEnd && newVisibleLength >= wordEnd;
        }

        /// <summary>
        /// Finds the exclusive end index of a whole-word match containing <paramref name="revealIndex"/>.
        /// </summary>
        public static bool TryGetWordEndExclusive(
            string plain,
            int revealIndex,
            string word,
            out int endExclusive)
        {
            endExclusive = 0;
            if (string.IsNullOrEmpty(plain) || string.IsNullOrWhiteSpace(word))
                return false;
            if (revealIndex < 0 || revealIndex >= plain.Length)
                return false;

            string needle = word.Trim();
            if (needle.Length == 0)
                return false;

            int searchFrom = 0;
            while (searchFrom <= plain.Length - needle.Length)
            {
                int idx = plain.IndexOf(needle, searchFrom, StringComparison.OrdinalIgnoreCase);
                if (idx < 0)
                    return false;

                int end = idx + needle.Length;
                if (IsWordBoundary(plain, idx, end)
                    && revealIndex >= idx
                    && revealIndex < end)
                {
                    endExclusive = end;
                    return true;
                }

                searchFrom = idx + 1;
            }

            return false;
        }

        private static bool IsWordBoundary(string plain, int start, int end)
        {
            if (start > 0 && IsWordChar(plain[start - 1]))
                return false;
            if (end < plain.Length && IsWordChar(plain[end]))
                return false;
            return true;
        }

        private static bool IsWordChar(char c) =>
            char.IsLetterOrDigit(c) || c == '_' || c == '\'';
    }
}
