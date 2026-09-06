using RPGGame.Audio;
using RPGGame.UI.Avalonia.Feedback;

namespace RPGGame.UI.BlockDisplay
{
    /// <summary>
    /// Commits punchline-timed feedback (strip flash + action SFX) together so hit/miss
    /// cues play on the combat-log reveal, not the setup telegraph.
    /// </summary>
    internal static class PunchlineRevealFeedback
    {
        public static void CommitQueued(bool includeAudio = true)
        {
            HeroActionStripFeedback.CommitQueued();
            if (includeAudio) AudioCues.CommitQueued();
        }

        public static void ClearQueued()
        {
            HeroActionStripFeedback.ClearQueued();
            AudioCues.ClearQueued();
        }
    }
}
