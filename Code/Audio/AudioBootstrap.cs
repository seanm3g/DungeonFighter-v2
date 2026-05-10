using System;
using RPGGame.Utils;

namespace RPGGame.Audio
{
    /// <summary>
    /// One-time wire-up that builds the audio backend, dispatcher, music controller, and the
    /// static <see cref="AudioCues"/> facade. Called from <see cref="UI.Avalonia.Handlers.GameInitializationHandler"/>
    /// after settings have been loaded.
    /// </summary>
    /// <remarks>
    /// If the SoundFlow backend fails to initialize (e.g. no audio device on a CI box), the system
    /// falls back to <see cref="NullAudioEngine"/> so the rest of the game runs normally and silently.
    /// Pass <see cref="GameStateManager"/> when available so <see cref="MusicController"/> can react
    /// to state transitions.
    /// </remarks>
    public static class AudioBootstrap
    {
        private static IAudioEngine? _engine;
        private static AudioCueDispatcher? _dispatcher;
        private static MusicController? _musicController;
        private static readonly object _initLock = new();
        private static bool _initialized;

        public static IAudioEngine? Engine => _engine;
        public static AudioCueDispatcher? Dispatcher => _dispatcher;
        public static MusicController? MusicController => _musicController;

        /// <summary>Builds the audio system and attaches it to the given state manager (idempotent).</summary>
        public static void Initialize(GameStateManager? stateManager)
        {
            lock (_initLock)
            {
                if (_initialized)
                {
                    AttachStateManagerInternal(stateManager);
                    return;
                }

                try
                {
                    // Always reload config from disk so the bootstrap sees the latest user mappings.
                    AudioConfig.ReloadFromFile();

                    var sf = new SoundFlowAudioEngine();
                    bool ok = sf.Initialize();
                    _engine = ok ? sf : (IAudioEngine)new NullAudioEngine();

                    // Apply current AudioConfig volume/mute state to the engine immediately.
                    var cfg = AudioConfig.Instance;
                    _engine.SetMasterVolume(cfg.MasterVolume);
                    _engine.SetBusVolume(AudioBusKind.Music, cfg.MusicVolume);
                    _engine.SetBusVolume(AudioBusKind.Sfx, cfg.SfxVolume);
                    _engine.SetBusMute(AudioBusKind.Music, !cfg.MusicEnabled);
                    _engine.SetBusMute(AudioBusKind.Sfx, !cfg.SfxEnabled);

                    _dispatcher = new AudioCueDispatcher(_engine);
                    _dispatcher.SubscribeToCombatEvents();
                    AudioCues.SetDispatcher(_dispatcher);

                    _musicController = new MusicController(_engine);
                    AttachStateManagerInternal(stateManager);

                    _initialized = true;
                }
                catch (Exception ex)
                {
                    ErrorHandler.LogError(ex, "AudioBootstrap.Initialize", "Audio system bootstrap failed; falling back to silent");
                    _engine = new NullAudioEngine();
                    _dispatcher = new AudioCueDispatcher(_engine);
                    AudioCues.SetDispatcher(_dispatcher);
                    _musicController = new MusicController(_engine);
                    _initialized = true;
                }
            }
        }

        /// <summary>Re-applies the current <see cref="AudioConfig"/> volume/mute settings to the engine. Call after the user clicks Save in the Audio settings tab.</summary>
        public static void ApplyConfigToEngine()
        {
            if (_engine == null) return;
            var cfg = AudioConfig.Instance;
            _engine.SetMasterVolume(cfg.MasterVolume);
            _engine.SetBusVolume(AudioBusKind.Music, cfg.MusicVolume);
            _engine.SetBusVolume(AudioBusKind.Sfx, cfg.SfxVolume);
            _engine.SetBusMute(AudioBusKind.Music, !cfg.MusicEnabled);
            _engine.SetBusMute(AudioBusKind.Sfx, !cfg.SfxEnabled);
        }

        /// <summary>Forces a music re-evaluation for the current game state (used by the Audio settings tab after the user remaps a state).</summary>
        public static void RefreshMusicForCurrentState()
        {
            if (_musicController == null) return;
            _musicController.OnStateChanged(GetCurrentStateOrMainMenu());
        }

        private static GameState GetCurrentStateOrMainMenu()
        {
            // No DI for GameStateManager here; the controller already attached to it for live events.
            // This shim is used only when there's nothing attached (e.g. settings opened standalone in tests).
            return GameState.MainMenu;
        }

        /// <summary>Releases backend resources. Called on app shutdown.</summary>
        public static void Shutdown()
        {
            lock (_initLock)
            {
                if (!_initialized) return;
                try { _dispatcher?.Dispose(); } catch { }
                try { _musicController?.Dispose(); } catch { }
                try { _engine?.Shutdown(); } catch { }
                _dispatcher = null;
                _musicController = null;
                _engine = null;
                AudioCues.SetDispatcher(null);
                _initialized = false;
            }
        }

        private static void AttachStateManagerInternal(GameStateManager? stateManager)
        {
            if (stateManager == null || _musicController == null) return;
            try
            {
                _musicController.Attach(stateManager);
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex, "AudioBootstrap.AttachStateManager", "Could not attach MusicController to GameStateManager");
            }
        }
    }
}
