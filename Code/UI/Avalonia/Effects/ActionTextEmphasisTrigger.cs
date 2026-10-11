using System;

namespace RPGGame.UI.Avalonia.Effects
{
    /// <summary>
    /// Queues DistortionPulse locate+fire after the combat-log paint that reveals the word.
    /// F7 typewriter <c>ReplaceLast</c> only schedules a Background render; locating
    /// <see cref="GameCanvasControl.TextElements"/> synchronously always misses.
    /// </summary>
    public static class ActionTextEmphasisTrigger
    {
        public const int DefaultMaxAttempts = 4;

        /// <summary>
        /// Forces a paint, then retries <paramref name="tryTrigger"/> on the Background queue
        /// until it succeeds or <paramref name="maxAttempts"/> is exhausted.
        /// </summary>
        public static void QueueAfterPaint(
            System.Action forceRender,
            Func<string, bool> tryTrigger,
            string word,
            System.Action<System.Action> postBackground,
            int maxAttempts = DefaultMaxAttempts)
        {
            if (forceRender == null)
                throw new ArgumentNullException(nameof(forceRender));
            if (tryTrigger == null)
                throw new ArgumentNullException(nameof(tryTrigger));
            if (postBackground == null)
                throw new ArgumentNullException(nameof(postBackground));
            if (string.IsNullOrWhiteSpace(word))
                return;

            int attempts = Math.Max(1, maxAttempts);
            forceRender();

            void Attempt(int remaining)
            {
                if (tryTrigger(word))
                    return;
                if (remaining <= 1)
                    return;
                postBackground(() => Attempt(remaining - 1));
            }

            postBackground(() => Attempt(attempts));
        }
    }
}
