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
    /// Combat action outcomes use <see cref="QueueForPunchline"/> / <see cref="CommitQueued"/> so SFX
    /// play on the combat-log reveal, not the setup telegraph.
    /// </remarks>
    public static class AudioCues
    {
        private static AudioCueDispatcher? _dispatcher;
        private static AudioCue? _queuedPunchlineCue;

        /// <summary>True if a dispatcher has been wired up.</summary>
        public static bool IsInitialized => _dispatcher != null;

        /// <summary>Called once by <see cref="AudioBootstrap"/>.</summary>
        public static void SetDispatcher(AudioCueDispatcher? dispatcher)
        {
            _dispatcher = dispatcher;
            if (dispatcher == null)
                ClearQueued();
        }

        /// <summary>Plays the cue (if a dispatcher is wired up). No-op otherwise.</summary>
        /// <param name="settingsPreview">Passes through to <see cref="AudioCueDispatcher.Trigger(AudioCue, bool)"/> for the Audio settings <c>Test</c> button.</param>
        public static void Trigger(AudioCue cue, bool settingsPreview = false)
        {
            _dispatcher?.Trigger(cue, settingsPreview);
        }

        /// <summary>
        /// Hold a combat outcome cue until the action-block punchline so hit/miss SFX
        /// does not leak during <c>{Actor} Attacks {Target}...</c> setup.
        /// </summary>
        public static void QueueForPunchline(AudioCue cue)
        {
            if (cue == AudioCue.None)
                return;
            _queuedPunchlineCue = cue;
        }

        /// <summary>Play a previously queued combat outcome cue, if any.</summary>
        public static void CommitQueued()
        {
            if (_queuedPunchlineCue is not AudioCue cue)
                return;
            _queuedPunchlineCue = null;
            Trigger(cue);
        }

        /// <summary>Drop a queued combat outcome cue when the action block is not shown.</summary>
        public static void ClearQueued()
        {
            _queuedPunchlineCue = null;
        }

        /// <summary>Stops the currently playing music (if any).</summary>
        public static void StopMusic()
        {
            _dispatcher?.StopMusic();
        }
    }
}
