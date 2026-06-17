using System;
using System.Collections.Concurrent;
using System.Threading;
using Avalonia.Threading;
using RPGGame.Utils;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Providers;
using SoundFlow.Structs;

namespace RPGGame.Audio
{
    /// <summary>
    /// Production <see cref="IAudioEngine"/> backed by SoundFlow (MiniAudio).
    /// </summary>
    /// <remarks>
    /// Holds one <see cref="MiniAudioEngine"/> + one default <see cref="AudioPlaybackDevice"/>.
    /// Routes audio through two child <see cref="Mixer"/>s (Music + SFX) attached to
    /// <see cref="AudioPlaybackDevice.MasterMixer"/> so per-bus volume / mute can be controlled
    /// independently of the master and of individual cue volumes.
    /// SFX players are cached by absolute path; music streams a fresh player per <see cref="PlayMusic"/>.
    /// Every public method is wrapped in try/catch — a misbehaving audio file must never crash the game.
    /// </remarks>
    public sealed partial class SoundFlowAudioEngine : IAudioEngine, IDisposable
    {
        private readonly object initLock = new();
        private MiniAudioEngine? engine;
        private AudioPlaybackDevice? device;
        private AudioFormat format;
        private Mixer? musicMixer;
        private Mixer? sfxMixer;
        private SoundPlayer? currentMusicPlayer;
        private StreamDataProvider? currentMusicStreamProvider;
        private int _playMusicGeneration;
        private CancellationTokenSource? _musicFadeCts;
        private readonly object _musicFadeLock = new();
        private readonly ConcurrentDictionary<string, byte[]> sfxFileCache = new(StringComparer.OrdinalIgnoreCase);

        private float masterVolume = 1.0f;
        private float musicVolume = 0.7f;
        private float sfxVolume = 0.9f;
        private bool masterMuted;
        private bool musicMuted;
        private bool sfxMuted;
        private bool disposed;
        private bool initFailed;
        private SynchronizationContext? _previewMarshalContext;

        /// <summary>True if the backend successfully initialized at least once.</summary>
        public bool IsInitialized => engine != null && device != null && !disposed;

        /// <summary>Initializes the underlying MiniAudio device. Idempotent; safe to call more than once.</summary>
        public bool Initialize()
        {
            if (disposed) return false;
            _previewMarshalContext ??= SynchronizationContext.Current;
            if (engine != null && device != null) return true;
            if (initFailed) return false;
            lock (initLock)
            {
                if (engine != null && device != null) return true;
                try
                {
                    engine = new MiniAudioEngine();
                    format = AudioFormat.DvdHq; // 48 kHz, stereo, F32
                    device = engine.InitializePlaybackDevice(null, format, null);
                    musicMixer = new Mixer(engine, format);
                    sfxMixer = new Mixer(engine, format);
                    device.MasterMixer.AddComponent(musicMixer);
                    device.MasterMixer.AddComponent(sfxMixer);
                    ApplyVolumesToMixers();
                    device.Start();
                    return true;
                }
                catch (Exception ex)
                {
                    ErrorHandler.LogError(ex, "SoundFlowAudioEngine.Initialize",
                        "Audio backend failed to initialize; audio will be silent for this session");
                    initFailed = true;
                    SafeDisposeBackend();
                    return false;
                }
            }
        }

        public bool TryGetMusicPlaybackTime(out double seconds)
        {
            seconds = 0;
            try
            {
                var p = currentMusicPlayer;
                if (p == null) return false;
                float time = p.Time;
                if (!float.IsFinite(time) || time < 0f) return false;
                seconds = time;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void SetBusVolume(AudioBusKind bus, float volume)
        {
            float v = Clamp01(volume);
            if (bus == AudioBusKind.Music) musicVolume = v;
            else sfxVolume = v;
            ApplyVolumesToMixers();
        }

        public void SetMasterVolume(float volume)
        {
            masterVolume = Clamp01(volume);
            ApplyVolumesToMixers();
        }

        public void SetBusMute(AudioBusKind bus, bool muted)
        {
            if (bus == AudioBusKind.Music) musicMuted = muted;
            else sfxMuted = muted;
            ApplyVolumesToMixers();
        }

        public void SetMasterMute(bool muted)
        {
            masterMuted = muted;
            ApplyVolumesToMixers();
        }

        private void ApplyVolumesToMixers()
        {
            if (musicMixer == null || sfxMixer == null) return;
            try
            {
                float masterFactor = masterMuted ? 0f : masterVolume;
                musicMixer.Volume = (musicMuted ? 0f : musicVolume) * masterFactor;
                sfxMixer.Volume   = (sfxMuted   ? 0f : sfxVolume)   * masterFactor;
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex, "SoundFlowAudioEngine.ApplyVolumesToMixers", "Could not apply mixer volume");
            }
        }

        public void Shutdown()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            SafeDisposeBackend();
        }

        private void SafeDisposeBackend()
        {
            Interlocked.Increment(ref _playMusicGeneration);
            CancelMusicFade();
            try { StopAndDetachCurrentMusicPlayer(); } catch { /* ignore */ }
            try { musicMixer?.Dispose(); } catch { }
            try { sfxMixer?.Dispose(); } catch { }
            try { device?.Dispose(); } catch { }
            try { engine?.Dispose(); } catch { }
            currentMusicPlayer = null;
            currentMusicStreamProvider = null;
            musicMixer = null;
            sfxMixer = null;
            device = null;
            engine = null;
            sfxFileCache.Clear();
        }

        private void PostMixerAction(global::System.Action action)
        {
            var ctx = _previewMarshalContext ?? SynchronizationContext.Current;
            if (ctx != null)
            {
                ctx.Post(_ => action(), null);
                return;
            }

            try
            {
                if (Dispatcher.UIThread.CheckAccess())
                    action();
                else
                    Dispatcher.UIThread.Post(action, DispatcherPriority.Send);
            }
            catch
            {
                action();
            }
        }

        private static void SafeRemoveAndDispose(Mixer? mixer, SoundPlayer player, StreamDataProvider? provider)
        {
            try { mixer?.RemoveComponent(player); } catch { }
            try { player.Dispose(); } catch { }
            try { provider?.Dispose(); } catch { }
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
