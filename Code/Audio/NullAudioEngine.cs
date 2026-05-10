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
        /// <summary>Recorded <see cref="PlayMusic"/> invocations.</summary>
        public List<(string file, int crossfadeMs, float volume)> PlayMusicCalls { get; } = new();
        /// <summary>Recorded <see cref="StopMusic"/> invocations.</summary>
        public List<int> StopMusicCalls { get; } = new();

        /// <summary>When false (default for production), no call history is retained — keeps overhead at zero.</summary>
        public bool RecordCalls { get; set; }

        public void Play(string filePath, AudioBusKind bus, float volume)
        {
            if (RecordCalls)
                PlayCalls.Add((filePath, bus, volume));
        }

        public void PlayMusic(string filePath, int crossfadeMs, float volume)
        {
            if (RecordCalls)
                PlayMusicCalls.Add((filePath, crossfadeMs, volume));
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
            StopMusicCalls.Clear();
        }
    }
}
