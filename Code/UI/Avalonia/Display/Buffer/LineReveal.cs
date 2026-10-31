using System;
using System.Collections.Generic;
using System.Text;
using RPGGame.Config.TextDelay;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Display.Buffer
{
    /// <summary>
    /// Character-by-character reveal of one buffered line, timed with the F7 narrative rhythm
    /// (budget split sentence → word → character). Lines queue: each starts when the one before it finishes.
    /// The renderer asks <see cref="ApplyTo"/> for the visible slice each paint.
    /// </summary>
    internal sealed class LineReveal
    {
        private readonly DateTimeOffset start;
        /// <summary>Milliseconds after <see cref="start"/> at which each plain character shows.</summary>
        private readonly int[] shownAtMs;

        private LineReveal(DateTimeOffset start, int[] shownAtMs)
        {
            this.start = start;
            this.shownAtMs = shownAtMs;
        }

        public DateTimeOffset End => start.AddMilliseconds(shownAtMs.Length == 0 ? 0 : shownAtMs[^1]);

        /// <summary>
        /// True when new lines should type in: live GUI, delays on, not instant / muted.
        /// Off in tests and headless runs (no canvas UI).
        /// </summary>
        public static bool IsEnabled =>
            !DeveloperModeState.IsCombatLogInstant
            && !CombatManager.DisableCombatUIOutput
            && UIManager.GetCustomUIManager() is CanvasUICoordinator;

        /// <summary>Schedules <paramref name="segments"/> to start typing at <paramref name="startUtc"/>; null when there is nothing to type.</summary>
        public static LineReveal? Create(List<ColoredText> segments, DateTimeOffset startUtc)
        {
            string plain = ColoredTextRenderer.RenderAsPlainText(segments);
            int content = CharacterRevealRhythmCalculator.CountContentChars(plain);
            if (content == 0)
                return null;

            int budget = DeveloperModeState.ScaleDelayMs(content * GameConstants.TextRevealMsPerChar);
            if (budget <= 0)
                return null;

            int[] schedule = CharacterRevealRhythmCalculator.BuildParagraphSchedule(plain, budget);
            var shownAt = new int[plain.Length];
            int total = 0;
            for (int i = 0; i < plain.Length; i++)
            {
                total += i < schedule.Length ? schedule[i] : 0;
                shownAt[i] = total;
            }
            return new LineReveal(startUtc, shownAt);
        }

        /// <summary>Visible plain length at <paramref name="nowUtc"/>; the full length once done.</summary>
        public int VisibleLength(DateTimeOffset nowUtc)
        {
            double elapsed = (nowUtc - start).TotalMilliseconds;
            if (elapsed < 0)
                return 0;
            int lo = 0, hi = shownAtMs.Length;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                if (shownAtMs[mid] <= elapsed)
                    lo = mid + 1;
                else
                    hi = mid;
            }
            return lo;
        }

        public bool IsDone(DateTimeOffset nowUtc) => VisibleLength(nowUtc) >= shownAtMs.Length;

        /// <summary>
        /// The typed-so-far slice of <paramref name="segments"/>, padded with spaces (line breaks kept) to the full
        /// length so centered and wrapped lines hold their place while they grow.
        /// </summary>
        public List<ColoredText> ApplyTo(List<ColoredText> segments, DateTimeOffset nowUtc)
        {
            int visible = VisibleLength(nowUtc);
            string plain = ColoredTextRenderer.RenderAsPlainText(segments);
            if (visible >= plain.Length)
                return segments;

            var result = ColoredTextRenderer.Truncate(segments, visible);
            var pad = new StringBuilder(plain.Length - visible);
            for (int i = visible; i < plain.Length; i++)
                pad.Append(plain[i] is '\n' or '\r' ? plain[i] : ' ');
            result.Add(new ColoredText(pad.ToString(), global::Avalonia.Media.Colors.White));
            return result;
        }
    }
}
