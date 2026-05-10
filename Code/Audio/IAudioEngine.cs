namespace RPGGame.Audio
{
    /// <summary>
    /// Backend-agnostic audio playback contract.
    /// </summary>
    /// <remarks>
    /// The production backend is <see cref="SoundFlowAudioEngine"/>; <see cref="NullAudioEngine"/>
    /// is used for tests and headless launches (MCP server, automated tuning, CI). The rest of the
    /// game depends only on this interface, so swapping backends is one-line.
    /// </remarks>
    public interface IAudioEngine
    {
        /// <summary>Plays a one-shot sound effect on the given bus. <paramref name="filePath"/> is the absolute path to the audio file.</summary>
        /// <param name="filePath">Resolved absolute path to a supported audio file (WAV / MP3 / FLAC by default).</param>
        /// <param name="bus">Bus the sound routes through. Music cues always crossfade via <see cref="PlayMusic"/> instead.</param>
        /// <param name="volume">Volume multiplier 0..1 applied on top of the bus volume.</param>
        void Play(string filePath, AudioBusKind bus, float volume);

        /// <summary>Starts or crossfades to a music track on the music bus. Replaces any current music.</summary>
        /// <param name="filePath">Resolved absolute path to a supported audio file.</param>
        /// <param name="crossfadeMs">Crossfade duration in milliseconds (0 = instant cut).</param>
        /// <param name="volume">Volume multiplier 0..1 applied on top of the music bus volume.</param>
        void PlayMusic(string filePath, int crossfadeMs, float volume);

        /// <summary>Stops the currently playing music with an optional fade-out.</summary>
        void StopMusic(int crossfadeMs);

        /// <summary>Sets the volume for a given bus (0..1). Master volume is the implicit third lever applied by the backend.</summary>
        void SetBusVolume(AudioBusKind bus, float volume);

        /// <summary>Sets the master volume (0..1) applied to both buses.</summary>
        void SetMasterVolume(float volume);

        /// <summary>Mutes or un-mutes a bus without changing its stored volume.</summary>
        void SetBusMute(AudioBusKind bus, bool muted);

        /// <summary>Mutes or un-mutes the master output without changing stored volume.</summary>
        void SetMasterMute(bool muted);

        /// <summary>Releases backend resources. Safe to call multiple times.</summary>
        void Shutdown();
    }
}
