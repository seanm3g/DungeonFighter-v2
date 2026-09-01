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
    }
}
