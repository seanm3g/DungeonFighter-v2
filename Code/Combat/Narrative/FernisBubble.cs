using System;

namespace RPGGame.Combat.Narrative
{
    /// <summary>
    /// Fernis's speech bubble on the board (D9.8): the last line he said, shown beside his shoulder sprite as it reaches
    /// the log, held for <see cref="HoldSeconds"/> and then faded over <see cref="FadeSeconds"/>. He never blocks the
    /// board: the bubble is drawn only where it fits and the line is always in the log too.
    /// </summary>
    public static class FernisBubble
    {
        public const double HoldSeconds = 5.0;
        public const double FadeSeconds = 1.5;

        private static readonly object _lock = new();
        private static string? _line;
        private static DateTimeOffset _shownUtc;

        /// <summary>Puts <paramref name="line"/> (without the "Fernis:" prefix) in the bubble.</summary>
        public static void Show(string? line, DateTimeOffset? now = null)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;
            lock (_lock)
            {
                _line = line.Trim();
                _shownUtc = now ?? DateTimeOffset.UtcNow;
            }
        }

        public static void Clear()
        {
            lock (_lock)
                _line = null;
        }

        /// <summary>The bubble's text and opacity at <paramref name="now"/> (1 while held, then down to 0); null when it's gone.</summary>
        public static (string Text, double Alpha)? Visible(DateTimeOffset now)
        {
            string? line;
            DateTimeOffset at;
            lock (_lock)
            {
                line = _line;
                at = _shownUtc;
            }
            if (line == null)
                return null;
            double age = (now - at).TotalSeconds;
            if (age < 0 || age >= HoldSeconds + FadeSeconds)
                return null;
            double alpha = age <= HoldSeconds ? 1.0 : 1.0 - (age - HoldSeconds) / FadeSeconds;
            return (line, Math.Clamp(alpha, 0.0, 1.0));
        }
    }
}
