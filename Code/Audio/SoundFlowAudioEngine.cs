using System;
using System.Collections.Concurrent;
using System.IO;
using RPGGame.Utils;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
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
    public sealed class SoundFlowAudioEngine : IAudioEngine, IDisposable
    {
        private readonly object initLock = new();
        private MiniAudioEngine? engine;
        private AudioPlaybackDevice? device;
        private AudioFormat format;
        private Mixer? musicMixer;
        private Mixer? sfxMixer;
        private SoundPlayer? currentMusicPlayer;
        private readonly ConcurrentDictionary<string, byte[]> sfxFileCache = new(StringComparer.OrdinalIgnoreCase);

        private float masterVolume = 1.0f;
        private float musicVolume = 0.7f;
        private float sfxVolume = 0.9f;
        private bool masterMuted;
        private bool musicMuted;
        private bool sfxMuted;
        private bool disposed;
        private bool initFailed;

        /// <summary>True if the backend successfully initialized at least once.</summary>
        public bool IsInitialized => engine != null && device != null && !disposed;

        /// <summary>Initializes the underlying MiniAudio device. Idempotent; safe to call more than once.</summary>
        public bool Initialize()
        {
            if (disposed) return false;
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

        public void Play(string filePath, AudioBusKind bus, float volume)
        {
            if (!Initialize()) return;
            if (string.IsNullOrEmpty(filePath)) return;

            // Music routes through PlayMusic for crossfade; this method only handles SFX.
            if (bus == AudioBusKind.Music)
            {
                PlayMusic(filePath, 0, volume);
                return;
            }

            try
            {
                if (!sfxFileCache.TryGetValue(filePath, out var bytes))
                {
                    if (!File.Exists(filePath)) return;
                    bytes = File.ReadAllBytes(filePath);
                    sfxFileCache[filePath] = bytes;
                }

                var stream = new MemoryStream(bytes, writable: false);
                var provider = new StreamDataProvider(engine!, format, stream);
                var player = new SoundPlayer(engine!, format, provider);
                player.Volume = Clamp01(volume);
                sfxMixer!.AddComponent(player);
                player.Play();
                // Self-cleanup when the player finishes; SoundFlow's SoundPlayer does not auto-remove from its mixer.
                player.PlaybackEnded += (_, _) => SafeRemoveAndDispose(sfxMixer, player, provider);
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex, "SoundFlowAudioEngine.Play", $"Could not play SFX '{filePath}'");
            }
        }

        public void PlayMusic(string filePath, int crossfadeMs, float volume)
        {
            if (!Initialize()) return;
            if (string.IsNullOrEmpty(filePath)) return;
            try
            {
                if (!File.Exists(filePath)) return;
                var fileStream = File.OpenRead(filePath);
                var provider = new StreamDataProvider(engine!, format, fileStream);
                var newPlayer = new SoundPlayer(engine!, format, provider) { IsLooping = true };
                newPlayer.Volume = Clamp01(volume);
                musicMixer!.AddComponent(newPlayer);

                // Replace previous music. SoundFlow has no built-in crossfade primitive, so we
                // start the new track at the requested volume and stop the old one immediately;
                // the crossfadeMs argument is reserved for a future ramp. Both are mixed during
                // the brief overlap so transitions still sound clean.
                var previous = currentMusicPlayer;
                currentMusicPlayer = newPlayer;
                newPlayer.Play();
                if (previous != null)
                {
                    try { previous.Stop(); } catch { /* ignore */ }
                    SafeRemoveAndDispose(musicMixer, previous, null);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex, "SoundFlowAudioEngine.PlayMusic", $"Could not start music '{filePath}'");
            }
        }

        public void StopMusic(int crossfadeMs)
        {
            if (currentMusicPlayer == null) return;
            try
            {
                var p = currentMusicPlayer;
                currentMusicPlayer = null;
                p.Stop();
                SafeRemoveAndDispose(musicMixer, p, null);
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex, "SoundFlowAudioEngine.StopMusic", "Could not stop music");
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
            try { currentMusicPlayer?.Stop(); } catch { }
            try { musicMixer?.Dispose(); } catch { }
            try { sfxMixer?.Dispose(); } catch { }
            try { device?.Dispose(); } catch { }
            try { engine?.Dispose(); } catch { }
            currentMusicPlayer = null;
            musicMixer = null;
            sfxMixer = null;
            device = null;
            engine = null;
            sfxFileCache.Clear();
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
