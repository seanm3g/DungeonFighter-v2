using System;
using System.Collections.Generic;
using System.IO;
using RPGGame.Combat.Events;
using RPGGame.Utils;

namespace RPGGame.Audio
{
    /// <summary>
    /// Resolves <see cref="AudioCue"/> values to audio files via <see cref="AudioConfig"/> and
    /// dispatches them to an <see cref="IAudioEngine"/>. Non-outcome combat cues subscribe to
    /// <see cref="CombatEventBus"/>; the five direct action outcome cues are triggered by
    /// <see cref="Actions.Execution.ActionEventPublisher"/>.
    /// </summary>
    /// <remarks>
    /// Rate-limit (per cue, from <see cref="AudioCueBinding.RateLimitMs"/>) prevents chatty cues
    /// like <see cref="AudioCue.Combat_Hit"/> from saturating the mixer. Missing files are logged
    /// once each and skipped; nothing throws.
    /// </remarks>
    public sealed class AudioCueDispatcher : IDisposable
    {
        private readonly IAudioEngine engine;
        private readonly Func<AudioConfig> configResolver;
        private readonly Func<bool> globalEnabledResolver;
        private readonly Dictionary<AudioCue, long> lastFireTicksByCue = new();
        private readonly HashSet<string> warnedMissingFiles = new();
        private readonly object subscribeLock = new();
        private bool subscribed;
        private Action<CombatEvent>? deathHandler;
        private Action<CombatEvent>? heroLowHealthHandler;
        private Action<CombatEvent>? enemyLowHealthHandler;
        private Action<CombatEvent>? statusHandler;

        /// <summary>For unit tests: deterministic clock override. Assigned via reflection in tests.</summary>
#pragma warning disable CS0649
        internal Func<long>? TicksProviderForTests;
#pragma warning restore CS0649

        /// <param name="engine">Audio backend (production = <see cref="SoundFlowAudioEngine"/>; tests = <see cref="NullAudioEngine"/>).</param>
        /// <param name="configResolver">Reads <see cref="AudioConfig"/> at use time so live edits take effect. Defaults to <see cref="AudioConfig.Instance"/>.</param>
        /// <param name="globalEnabledResolver">Returns false to short-circuit all playback (defaults to <see cref="GameSettings.EnableSoundEffects"/>).</param>
        public AudioCueDispatcher(
            IAudioEngine engine,
            Func<AudioConfig>? configResolver = null,
            Func<bool>? globalEnabledResolver = null)
        {
            this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
            this.configResolver = configResolver ?? (() => AudioConfig.Instance);
            this.globalEnabledResolver = globalEnabledResolver ?? (() => GameSettings.Instance.EnableSoundEffects);
        }

        /// <summary>Subscribes the dispatcher to non-outcome <see cref="CombatEventBus"/> events for the duration of its life. Safe to call multiple times.</summary>
        public void SubscribeToCombatEvents()
        {
            lock (subscribeLock)
            {
                if (subscribed) return;
                deathHandler      = OnEnemyDied;
                heroLowHealthHandler  = OnHeroLowHealth;
                enemyLowHealthHandler = OnEnemyLowHealth;
                statusHandler     = OnStatusEffectApplied;
                CombatEventBus.Instance.Subscribe(CombatEventType.EnemyDied, deathHandler);
                CombatEventBus.Instance.Subscribe(CombatEventType.HeroLowHealth, heroLowHealthHandler);
                CombatEventBus.Instance.Subscribe(CombatEventType.EnemyLowHealth, enemyLowHealthHandler);
                CombatEventBus.Instance.Subscribe(CombatEventType.StatusEffectApplied, statusHandler);
                subscribed = true;
            }
        }

        /// <summary>Unsubscribes from the combat event bus (used by tests and on shutdown).</summary>
        public void UnsubscribeFromCombatEvents()
        {
            lock (subscribeLock)
            {
                if (!subscribed) return;
                if (deathHandler      != null) CombatEventBus.Instance.Unsubscribe(CombatEventType.EnemyDied, deathHandler);
                if (heroLowHealthHandler  != null) CombatEventBus.Instance.Unsubscribe(CombatEventType.HeroLowHealth, heroLowHealthHandler);
                if (enemyLowHealthHandler != null) CombatEventBus.Instance.Unsubscribe(CombatEventType.EnemyLowHealth, enemyLowHealthHandler);
                if (statusHandler     != null) CombatEventBus.Instance.Unsubscribe(CombatEventType.StatusEffectApplied, statusHandler);
                subscribed = false;
            }
        }

        private void OnEnemyDied(CombatEvent _)    => Trigger(AudioCue.Combat_EnemyDied);
        private void OnHeroLowHealth(CombatEvent _) => Trigger(AudioCue.Combat_HeroLowHealth);
        private void OnEnemyLowHealth(CombatEvent _) => Trigger(AudioCue.Combat_EnemyLowHealth);
        private void OnStatusEffectApplied(CombatEvent _) => Trigger(AudioCue.Combat_StatusApplied);

        /// <summary>Plays the audio bound to a cue (if any). All public call sites enter here.</summary>
        /// <param name="settingsPreview">When true (settings <c>Test</c> button), ignores master mute and per-bus mutes so the user can verify a file binding.</param>
        public void Trigger(AudioCue cue, bool settingsPreview = false)
        {
            if (cue == AudioCue.None) return;
            if (!settingsPreview && !globalEnabledResolver()) return;

            var config = configResolver();
            var binding = config.GetBinding(cue);
            if (binding == null || string.IsNullOrEmpty(binding.File)) return;

            var bus = cue.GetBus();
            if (!settingsPreview)
            {
                if (bus == AudioBusKind.Music && !config.MusicEnabled) return;
                if (bus == AudioBusKind.Sfx && !config.SfxEnabled) return;
            }

            if (!settingsPreview && !PassesRateLimit(cue, binding)) return;

            string absolute = AudioConfig.ResolveAssetPath(binding.File);
            if (string.IsNullOrEmpty(absolute) || !File.Exists(absolute))
            {
                WarnMissingOnce(cue, absolute);
                return;
            }

            try
            {
                if (settingsPreview)
                {
                    // Use the dedicated preview path for both music and SFX so Test remains audible
                    // even while master/SFX/music are muted in the settings panel.
                    engine.PlaySettingsPreview(absolute, binding.Volume);
                }
                else if (bus == AudioBusKind.Music)
                {
                    double? outgoing = engine.TryGetMusicPlaybackTime(out var t) ? t : (double?)null;
                    double startOffset = config.ComputeMusicStartOffsetSecondsForTransition(outgoing);
                    engine.PlayMusic(absolute, config.MusicCrossfadeMs, binding.Volume, startOffset);
                }
                else
                    engine.Play(absolute, AudioBusKind.Sfx, binding.Volume);
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex, "AudioCueDispatcher.Trigger", $"Could not play cue {cue}");
            }
        }

        /// <summary>Stops the currently playing music (uses the configured crossfade).</summary>
        public void StopMusic()
        {
            try
            {
                engine.StopMusic(configResolver().MusicCrossfadeMs);
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex, "AudioCueDispatcher.StopMusic", "Could not stop music");
            }
        }

        private bool PassesRateLimit(AudioCue cue, AudioCueBinding binding)
        {
            if (binding.RateLimitMs is not int ms || ms <= 0) return true;
            long nowTicks = TicksProviderForTests?.Invoke() ?? System.Environment.TickCount64;
            if (lastFireTicksByCue.TryGetValue(cue, out long last))
            {
                if (nowTicks - last < ms) return false;
            }
            lastFireTicksByCue[cue] = nowTicks;
            return true;
        }

        private void WarnMissingOnce(AudioCue cue, string path)
        {
            string key = $"{cue}:{path}";
            lock (warnedMissingFiles)
            {
                if (warnedMissingFiles.Add(key))
                    DebugLogger.Log("AudioCueDispatcher", $"Cue {cue} bound to missing file '{path}' — skipping (won't warn again)");
            }
        }

        public void Dispose() => UnsubscribeFromCombatEvents();
    }
}
