using System.Collections.Generic;

namespace RPGGame.Audio
{
    /// <summary>
    /// No-op <see cref="IAudioEngine"/>. Used for unit tests, MCP server, automated tuning, and any headless run.
    /// </summary>
    /// <remarks>
    /// Records every call so tests can assert routing without producing real audio.
    /// </remarks>
    public sealed class NullAudioEngine : IAudioEngine
    {
        /// <summary>Recorded <see cref="Play"/> invocations (only populated when <see cref="RecordCalls"/> is true).</summary>
        public List<(string file, AudioBusKind bus, float volume)> PlayCalls { get; } = new();
        /// <summary>Recorded <see cref="PlayMusic"/> invocations (last item is <c>startOffsetSeconds</c>).</summary>
        public List<(string file, int crossfadeMs, float volume, double startOffsetSeconds)> PlayMusicCalls { get; } = new();
        /// <summary>Recorded <see cref="PlaySettingsPreview"/> invocations.</summary>
        public List<(string file, float volume)> PlaySettingsPreviewCalls { get; } = new();
        /// <summary>Recorded <see cref="StopMusic"/> invocations.</summary>
        public List<int> StopMusicCalls { get; } = new();

        /// <summary>When false (default for production), no call history is retained — keeps overhead at zero.</summary>
        public bool RecordCalls { get; set; }

        /// <summary>When set, <see cref="TryGetMusicPlaybackTime"/> returns this value (tests simulate an active music clock).</summary>
        public double? SimulatedMusicPlaybackTimeSeconds { get; set; }

        public void Play(string filePath, AudioBusKind bus, float volume)
        {
            if (RecordCalls)
                PlayCalls.Add((filePath, bus, volume));
        }

        public void PlayMusic(string filePath, int crossfadeMs, float volume, double startOffsetSeconds = 0)
        {
            if (RecordCalls)
                PlayMusicCalls.Add((filePath, crossfadeMs, volume, startOffsetSeconds));
        }

        public bool TryGetMusicPlaybackTime(out double seconds)
        {
            if (SimulatedMusicPlaybackTimeSeconds is double t && double.IsFinite(t) && t >= 0)
            {
                seconds = t;
                return true;
            }
            seconds = 0;
            return false;
        }

        public void PlaySettingsPreview(string absolutePath, float volume)
        {
            if (RecordCalls)
                PlaySettingsPreviewCalls.Add((absolutePath, volume));
        }

        public void StopMusic(int crossfadeMs)
        {
            if (RecordCalls)
                StopMusicCalls.Add(crossfadeMs);
        }

        public void SetBusVolume(AudioBusKind bus, float volume) { }
        public void SetMasterVolume(float volume) { }
        public void SetBusMute(AudioBusKind bus, bool muted) { }
        public void SetMasterMute(bool muted) { }
        public void Shutdown() { }

        /// <summary>Clears all recorded history (test helper).</summary>
        public void ClearHistory()
        {
            PlayCalls.Clear();
            PlayMusicCalls.Clear();
            PlaySettingsPreviewCalls.Clear();
            StopMusicCalls.Clear();
        }
    }
}
