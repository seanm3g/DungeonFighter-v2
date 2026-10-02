using RPGGame.Audio;
using RPGGame.UI.Avalonia.Feedback;

namespace RPGGame.UI.BlockDisplay
{
    /// <summary>
    /// Commits punchline-timed feedback (strip flash + action SFX) together so hit/miss
    /// cues play on the combat-log reveal, not the setup telegraph.
    /// Also releases the deferred combo-strip "next" selection when the action block ends.
    /// </summary>
    internal static class PunchlineRevealFeedback
    {
        public static void CommitQueued()
        {
            HeroActionStripFeedback.CommitQueued();
            AudioCues.CommitQueued();
        }

        public static void ClearQueued()
        {
            HeroActionStripFeedback.ClearQueued();
            AudioCues.ClearQueued();
        }

        /// <summary>
        /// Action block finished presenting: allow the white next-border to follow live ComboStep.
        /// Does not cancel an in-progress gold/red pulse.
        /// </summary>
        public static void NotifyBlockFinished()
        {
            HeroActionStripFeedback.ReleaseSelectedHold();
        }
    }
}
