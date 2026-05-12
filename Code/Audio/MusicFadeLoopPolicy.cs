namespace RPGGame.Audio
{
    /// <summary>
    /// Small policy helper for music fade decisions that can be tested without the audio backend.
    /// </summary>
    internal static class MusicFadeLoopPolicy
    {
        public static bool ShouldFadeIncomingTrack(
            int crossfadeMs,
            bool hasOutgoingTrack)
        {
            return crossfadeMs > 0 && !hasOutgoingTrack;
        }

        public static bool ShouldRestartEndedTrack(
            int fadeGeneration,
            int currentGeneration,
            bool cancellationRequested)
        {
            return !cancellationRequested && fadeGeneration == currentGeneration;
        }

        public static bool ShouldRestartCurrentTrack(
            int trackGeneration,
            int currentGeneration,
            bool isCurrentTrack)
        {
            return isCurrentTrack && trackGeneration == currentGeneration;
        }
    }
}
