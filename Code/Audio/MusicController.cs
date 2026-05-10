using System;
using System.IO;
using RPGGame.Utils;

namespace RPGGame.Audio
{
    /// <summary>
    /// Subscribes to <see cref="GameStateManager.StateChanged"/> and crossfades music whenever the
    /// active <see cref="GameState"/> changes. Mapping (state name → music cue) lives in
    /// <see cref="AudioConfig.StateMusicMap"/>.
    /// </summary>
    /// <remarks>
    /// Tracks the currently-playing cue so we don't restart the same track when transient state
    /// transitions don't change the music (e.g. <see cref="GameState.Dungeon"/> ↔ <see cref="GameState.Combat"/>
    /// when both are mapped to <see cref="AudioCue.Music_Dungeon"/>).
    /// </remarks>
    public sealed class MusicController : IDisposable
    {
        private readonly IAudioEngine engine;
        private readonly Func<AudioConfig> configResolver;
        private readonly Func<bool> globalEnabledResolver;
        private GameStateManager? attachedStateManager;
        private EventHandler<StateChangedEventArgs>? stateChangedHandler;
        private AudioCue currentMusicCue = AudioCue.None;

        public AudioCue CurrentMusicCue => currentMusicCue;

        public MusicController(
            IAudioEngine engine,
            Func<AudioConfig>? configResolver = null,
            Func<bool>? globalEnabledResolver = null)
        {
            this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
            this.configResolver = configResolver ?? (() => AudioConfig.Instance);
            this.globalEnabledResolver = globalEnabledResolver ?? (() => GameSettings.Instance.EnableSoundEffects);
        }

        /// <summary>Attaches this controller to a <see cref="GameStateManager"/> so it reacts to state transitions.</summary>
        public void Attach(GameStateManager stateManager)
        {
            if (stateManager == null) throw new ArgumentNullException(nameof(stateManager));
            Detach();
            attachedStateManager = stateManager;
            stateChangedHandler = (sender, args) => OnStateChanged(args.NewState);
            stateManager.StateChanged += stateChangedHandler;
            OnStateChanged(stateManager.CurrentState);
        }

        public void Detach()
        {
            if (attachedStateManager != null && stateChangedHandler != null)
            {
                attachedStateManager.StateChanged -= stateChangedHandler;
            }
            attachedStateManager = null;
            stateChangedHandler = null;
        }

        /// <summary>Public entry point so tests can drive state changes without hooking the real <see cref="GameStateManager"/>.</summary>
        public void OnStateChanged(GameState newState)
        {
            if (!globalEnabledResolver())
            {
                if (currentMusicCue != AudioCue.None)
                {
                    SafeStopMusic();
                    currentMusicCue = AudioCue.None;
                }
                return;
            }

            var config = configResolver();
            AudioCue nextCue = config.GetMusicCueForState(newState.ToString());

            if (nextCue == currentMusicCue) return;

            if (nextCue == AudioCue.None)
            {
                SafeStopMusic();
                currentMusicCue = AudioCue.None;
                return;
            }

            var binding = config.GetBinding(nextCue);
            if (binding == null || string.IsNullOrEmpty(binding.File))
            {
                SafeStopMusic();
                currentMusicCue = AudioCue.None;
                return;
            }

            if (!config.MusicEnabled)
            {
                SafeStopMusic();
                currentMusicCue = AudioCue.None;
                return;
            }

            string absolute = AudioConfig.ResolveAssetPath(binding.File);
            if (string.IsNullOrEmpty(absolute) || !File.Exists(absolute))
            {
                SafeStopMusic();
                currentMusicCue = AudioCue.None;
                return;
            }

            try
            {
                engine.PlayMusic(absolute, config.MusicCrossfadeMs, binding.Volume);
                currentMusicCue = nextCue;
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex, "MusicController.OnStateChanged", $"Could not start music for state {newState}");
            }
        }

        private void SafeStopMusic()
        {
            try { engine.StopMusic(configResolver().MusicCrossfadeMs); }
            catch (Exception ex) { ErrorHandler.LogError(ex, "MusicController.SafeStopMusic", "Could not stop music"); }
        }

        public void Dispose() => Detach();
    }
}
