using System;
using System.Collections.Generic;
using System.IO;
using RPGGame.Combat.Events;
using RPGGame.Utils;

namespace RPGGame.Audio
{
    /// <summary>
    /// Resolves <see cref="AudioCue"/> values to audio files via <see cref="AudioConfig"/> and
    /// dispatches them to an <see cref="IAudioEngine"/>. Subscribes to <see cref="CombatEventBus"/>
    /// so combat-driven cues fire automatically.
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
        private Action<CombatEvent>? hitHandler;
        private Action<CombatEvent>? missHandler;
        private Action<CombatEvent>? critHandler;
        private Action<CombatEvent>? deathHandler;
        private Action<CombatEvent>? comboEndedHandler;
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

        /// <summary>Subscribes the dispatcher to <see cref="CombatEventBus"/> for the duration of its life. Safe to call multiple times.</summary>
        public void SubscribeToCombatEvents()
        {
            lock (subscribeLock)
            {
                if (subscribed) return;
                hitHandler        = OnActionHit;
                missHandler       = OnActionMiss;
                critHandler       = OnActionCritical;
                deathHandler      = OnEnemyDied;
                comboEndedHandler = OnComboEnded;
                statusHandler     = OnStatusEffectApplied;
                CombatEventBus.Instance.Subscribe(CombatEventType.ActionHit, hitHandler);
                CombatEventBus.Instance.Subscribe(CombatEventType.ActionMiss, missHandler);
                CombatEventBus.Instance.Subscribe(CombatEventType.ActionCritical, critHandler);
                CombatEventBus.Instance.Subscribe(CombatEventType.EnemyDied, deathHandler);
                CombatEventBus.Instance.Subscribe(CombatEventType.ComboEnded, comboEndedHandler);
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
                if (hitHandler        != null) CombatEventBus.Instance.Unsubscribe(CombatEventType.ActionHit, hitHandler);
                if (missHandler       != null) CombatEventBus.Instance.Unsubscribe(CombatEventType.ActionMiss, missHandler);
                if (critHandler       != null) CombatEventBus.Instance.Unsubscribe(CombatEventType.ActionCritical, critHandler);
                if (deathHandler      != null) CombatEventBus.Instance.Unsubscribe(CombatEventType.EnemyDied, deathHandler);
                if (comboEndedHandler != null) CombatEventBus.Instance.Unsubscribe(CombatEventType.ComboEnded, comboEndedHandler);
                if (statusHandler     != null) CombatEventBus.Instance.Unsubscribe(CombatEventType.StatusEffectApplied, statusHandler);
                subscribed = false;
            }
        }

        private void OnActionHit(CombatEvent evt)
        {
            if (evt.IsCritical) return;
            Trigger(AudioCue.Combat_Hit);
        }
        private void OnActionMiss(CombatEvent _)   => Trigger(AudioCue.Combat_Miss);
        private void OnActionCritical(CombatEvent _) => Trigger(AudioCue.Combat_Critical);
        private void OnEnemyDied(CombatEvent _)    => Trigger(AudioCue.Combat_EnemyDied);
        private void OnComboEnded(CombatEvent _)   => Trigger(AudioCue.Combat_ComboComplete);
        private void OnStatusEffectApplied(CombatEvent _) => Trigger(AudioCue.Combat_StatusApplied);

        /// <summary>Plays the audio bound to a cue (if any). All public call sites enter here.</summary>
        public void Trigger(AudioCue cue)
        {
            if (cue == AudioCue.None) return;
            if (!globalEnabledResolver()) return;

            var config = configResolver();
            var binding = config.GetBinding(cue);
            if (binding == null || string.IsNullOrEmpty(binding.File)) return;

            var bus = cue.GetBus();
            if (bus == AudioBusKind.Music && !config.MusicEnabled) return;
            if (bus == AudioBusKind.Sfx && !config.SfxEnabled) return;

            if (!PassesRateLimit(cue, binding)) return;

            string absolute = AudioConfig.ResolveAssetPath(binding.File);
            if (string.IsNullOrEmpty(absolute) || !File.Exists(absolute))
            {
                WarnMissingOnce(cue, absolute);
                return;
            }

            try
            {
                if (bus == AudioBusKind.Music)
                    engine.PlayMusic(absolute, config.MusicCrossfadeMs, binding.Volume);
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
