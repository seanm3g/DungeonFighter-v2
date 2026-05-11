namespace RPGGame.Audio
{
    /// <summary>
    /// Static facade for firing audio cues from non-DI call sites
    /// (menu commands, save manager, equipment manager, etc.).
    /// </summary>
    /// <remarks>
    /// Set up by <see cref="AudioBootstrap"/> after <see cref="AudioCueDispatcher"/> is constructed.
    /// If audio has not been initialized (tests, headless mode, or initialization failure),
    /// <see cref="Trigger"/> is a silent no-op — call sites never need to null-check.
    /// </remarks>
    public static class AudioCues
    {
        private static AudioCueDispatcher? _dispatcher;

        /// <summary>True if a dispatcher has been wired up.</summary>
        public static bool IsInitialized => _dispatcher != null;

        /// <summary>Called once by <see cref="AudioBootstrap"/>.</summary>
        public static void SetDispatcher(AudioCueDispatcher? dispatcher)
        {
            _dispatcher = dispatcher;
        }

        /// <summary>Plays the cue (if a dispatcher is wired up). No-op otherwise.</summary>
        /// <param name="settingsPreview">Passes through to <see cref="AudioCueDispatcher.Trigger(AudioCue, bool)"/> for the Audio settings <c>Test</c> button.</param>
        public static void Trigger(AudioCue cue, bool settingsPreview = false)
        {
            _dispatcher?.Trigger(cue, settingsPreview);
        }

        /// <summary>Stops the currently playing music (if any).</summary>
        public static void StopMusic()
        {
            _dispatcher?.StopMusic();
        }
    }
}
