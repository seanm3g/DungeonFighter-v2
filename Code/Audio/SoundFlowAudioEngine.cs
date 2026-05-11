using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
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

        /// <inheritdoc />
        public void PlaySettingsPreview(string absolutePath, float volume)
        {
            _previewMarshalContext ??= SynchronizationContext.Current;
            string ext = Path.GetExtension(absolutePath);

            if (string.Equals(ext, ".mp3", StringComparison.OrdinalIgnoreCase))
            {
                Task.Run(() =>
                {
                    MemoryStream? wav = null;
                    try
                    {
                        wav = Mp3ToWavMemoryStream.Build(absolutePath);
                    }
                    catch (Exception ex)
                    {
                        ErrorHandler.LogError(ex, "SoundFlowAudioEngine.PlaySettingsPreview", "MP3 decode failed");
                        return;
                    }

                    PostMixerAction(() => PlayOneShotWavMemoryStream(wav!, volume));
                });
                return;
            }

            Task.Run(() =>
            {
                MemoryStream? ms = null;
                try
                {
                    byte[] bytes = File.ReadAllBytes(absolutePath);
                    ms = new MemoryStream(bytes, writable: false);
                }
                catch (Exception ex)
                {
                    ErrorHandler.LogError(ex, "SoundFlowAudioEngine.PlaySettingsPreview", "Could not read audio file");
                    return;
                }

                PostMixerAction(() => PlayOneShotWavMemoryStream(ms!, volume));
            });
        }

        /// <summary>Runs on the UI/sync context that owns the SoundFlow device.</summary>
        private void PlayOneShotWavMemoryStream(MemoryStream wavStream, float volume)
        {
            if (!Initialize())
            {
                try { wavStream.Dispose(); } catch { /* ignore */ }
                return;
            }

            // Preview uses the SFX mixer and temporarily makes that path audible so the settings
            // Test button works even when the player is editing mute/volume state.
            bool savedMasterMuted = masterMuted;
            bool savedSfxMuted = sfxMuted;
            float savedMasterVol = masterVolume;
            float savedSfxVol = sfxVolume;

            void RestorePreviewMixerState()
            {
                masterMuted = savedMasterMuted;
                sfxMuted = savedSfxMuted;
                masterVolume = savedMasterVol;
                sfxVolume = savedSfxVol;
                ApplyVolumesToMixers();
            }

            void PostRestore()
            {
                PostMixerAction(RestorePreviewMixerState);
            }

            masterMuted = false;
            sfxMuted = false;
            masterVolume = Math.Max(masterVolume, 0.2f);
            sfxVolume = Math.Max(sfxVolume, 0.4f);
            ApplyVolumesToMixers();

            try
            {
                var provider = new StreamDataProvider(engine!, format, wavStream);
                var player = new SoundPlayer(engine!, format, provider) { IsLooping = false };
                player.Volume = Clamp01(volume);
                sfxMixer!.AddComponent(player);
                player.Play();
                player.PlaybackEnded += (_, _) =>
                {
                    SafeRemoveAndDispose(sfxMixer, player, provider);
                    PostRestore();
                };
            }
            catch (Exception ex)
            {
                PostRestore();
                ErrorHandler.LogError(ex, "SoundFlowAudioEngine.PlayOneShotWavMemoryStream", "Preview playback failed");
                try { wavStream.Dispose(); } catch { /* ignore */ }
            }
        }

        public void Play(string filePath, AudioBusKind bus, float volume)
        {
            if (!Initialize()) return;
            if (string.IsNullOrEmpty(filePath)) return;

            // Music routes through PlayMusic for crossfade; this method only handles SFX.
            if (bus == AudioBusKind.Music)
            {
                PlayMusic(filePath, 0, volume, 0);
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

                PostMixerAction(() => PlaySfxBytes(filePath, bytes, volume));
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex, "SoundFlowAudioEngine.Play", $"Could not play SFX '{filePath}'");
            }
        }

        private void PlaySfxBytes(string filePath, byte[] bytes, float volume)
        {
            try
            {
                if (!Initialize()) return;
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
                ErrorHandler.LogError(ex, "SoundFlowAudioEngine.PlaySfxBytes", $"Could not play SFX '{filePath}'");
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

        public void PlayMusic(string filePath, int crossfadeMs, float volume, double startOffsetSeconds = 0)
        {
            if (!Initialize()) return;
            if (string.IsNullOrEmpty(filePath)) return;

            _previewMarshalContext ??= SynchronizationContext.Current;
            var marshalCtx = _previewMarshalContext;

            int gen = Interlocked.Increment(ref _playMusicGeneration);
            CancelMusicFade();

            SoundPlayer? outgoing = currentMusicPlayer;
            StreamDataProvider? outgoingProv = currentMusicStreamProvider;
            bool useCrossfade = crossfadeMs > 0 && outgoing != null;

            if (!useCrossfade)
                StopAndDetachCurrentMusicPlayer();

            float targetVol = Clamp01(volume);

            string ext = Path.GetExtension(filePath);
            if (string.Equals(ext, ".mp3", StringComparison.OrdinalIgnoreCase))
            {
                void PostToUi(global::System.Action work)
                {
                    if (marshalCtx != null)
                        marshalCtx.Post(_ => work(), null);
                    else
                        work();
                }

                SoundPlayer? mp3CrossfadeOld = useCrossfade ? outgoing : null;
                StreamDataProvider? mp3CrossfadeOldProv = useCrossfade ? outgoingProv : null;

                Task.Run(() =>
                {
                    MemoryStream? wav = null;
                    try
                    {
                        if (!File.Exists(filePath)) return;
                        wav = Mp3ToWavMemoryStream.BuildFullTrack(filePath);
                    }
                    catch (Exception ex)
                    {
                        ErrorHandler.LogError(ex, "SoundFlowAudioEngine.PlayMusic", $"MP3 decode failed for '{filePath}'");
                        return;
                    }

                    PostToUi(() =>
                    {
                        if (gen != Volatile.Read(ref _playMusicGeneration))
                        {
                            try { wav?.Dispose(); } catch { /* ignore */ }
                            return;
                        }

                        try
                        {
                            if (!Initialize()) { try { wav?.Dispose(); } catch { } return; }
                            var provider = new StreamDataProvider(engine!, format, wav!);
                            var newPlayer = new SoundPlayer(engine!, format, provider) { IsLooping = true };
                            bool cf = mp3CrossfadeOld != null;
                            newPlayer.Volume = cf ? 0f : targetVol;
                            musicMixer!.AddComponent(newPlayer);
                            currentMusicStreamProvider = provider;
                            currentMusicPlayer = newPlayer;
                            SeekMusicIfNeeded(newPlayer, startOffsetSeconds);
                            newPlayer.Play();
                            if (cf)
                                ScheduleMusicCrossfade(mp3CrossfadeOld!, mp3CrossfadeOldProv, newPlayer, targetVol, crossfadeMs, gen);
                        }
                        catch (Exception ex)
                        {
                            ErrorHandler.LogError(ex, "SoundFlowAudioEngine.PlayMusic", $"Could not start decoded MP3 music '{filePath}'");
                            try { wav?.Dispose(); } catch { /* ignore */ }
                        }
                    });
                });
                return;
            }

            try
            {
                if (!File.Exists(filePath)) return;

                var fileStream = File.OpenRead(filePath);
                var provider = new StreamDataProvider(engine!, format, fileStream);
                var newPlayer = new SoundPlayer(engine!, format, provider) { IsLooping = true };
                newPlayer.Volume = useCrossfade ? 0f : targetVol;
                musicMixer!.AddComponent(newPlayer);
                currentMusicStreamProvider = provider;
                currentMusicPlayer = newPlayer;
                SeekMusicIfNeeded(newPlayer, startOffsetSeconds);
                newPlayer.Play();
                if (useCrossfade)
                    ScheduleMusicCrossfade(outgoing!, outgoingProv, newPlayer, targetVol, crossfadeMs, gen);
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex, "SoundFlowAudioEngine.PlayMusic", $"Could not start music '{filePath}'");
            }
        }

        private void ScheduleMusicCrossfade(
            SoundPlayer oldPlayer,
            StreamDataProvider? oldProvider,
            SoundPlayer newPlayer,
            float targetVolume,
            int crossfadeMs,
            int fadeGeneration)
        {
            float vOldStart;
            try { vOldStart = Clamp01(oldPlayer.Volume); }
            catch { vOldStart = targetVolume; }

            var cts = new CancellationTokenSource();
            lock (_musicFadeLock)
                _musicFadeCts = cts;

            _ = Task.Run(async () =>
            {
                try
                {
                    await RunMusicCrossfadeCoreAsync(
                        oldPlayer, oldProvider, newPlayer, targetVolume, vOldStart, crossfadeMs, fadeGeneration, cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    PostMixerAction(() =>
                    {
                        try { SafeRemoveAndDispose(musicMixer, oldPlayer, oldProvider); } catch { /* ignore */ }
                    });
                }
                finally
                {
                    FinishMusicFadeCts(cts);
                }
            });
        }

        private async Task RunMusicCrossfadeCoreAsync(
            SoundPlayer oldPlayer,
            StreamDataProvider? oldProvider,
            SoundPlayer newPlayer,
            float targetVolume,
            float oldVolumeStart,
            int crossfadeMs,
            int fadeGeneration,
            CancellationToken cancellationToken)
        {
            int stepCount = Math.Clamp(crossfadeMs / 25, 4, 80);
            int delayMs = Math.Max(1, crossfadeMs / stepCount);

            for (int i = 1; i <= stepCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (Volatile.Read(ref _playMusicGeneration) != fadeGeneration)
                    break;

                await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                float t = (float)i / stepCount;
                float vOut = oldVolumeStart * (1f - t);
                float vIn = targetVolume * t;
                PostMixerAction(() =>
                {
                    if (Volatile.Read(ref _playMusicGeneration) != fadeGeneration)
                        return;
                    try
                    {
                        oldPlayer.Volume = Clamp01(vOut);
                        newPlayer.Volume = Clamp01(vIn);
                    }
                    catch { /* ignore */ }
                });
            }

            PostMixerAction(() =>
            {
                if (Volatile.Read(ref _playMusicGeneration) != fadeGeneration)
                {
                    try { SafeRemoveAndDispose(musicMixer, oldPlayer, oldProvider); } catch { /* ignore */ }
                    return;
                }
                try
                {
                    newPlayer.Volume = Clamp01(targetVolume);
                    oldPlayer.Volume = 0f;
                }
                catch { /* ignore */ }
                try { SafeRemoveAndDispose(musicMixer, oldPlayer, oldProvider); } catch { /* ignore */ }
            });
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

        private void CancelMusicFade()
        {
            CancellationTokenSource? old;
            lock (_musicFadeLock)
            {
                old = _musicFadeCts;
                _musicFadeCts = null;
            }
            try
            {
                old?.Cancel();
                old?.Dispose();
            }
            catch { /* ignore */ }
        }

        private void FinishMusicFadeCts(CancellationTokenSource cts)
        {
            lock (_musicFadeLock)
            {
                if (ReferenceEquals(_musicFadeCts, cts))
                    _musicFadeCts = null;
            }
            try { cts.Dispose(); } catch { /* ignore */ }
        }

        /// <summary>
        /// Seeks the new music stream by PCM sample index. Time-based seek in SoundFlow scales by duration, which can
        /// fail when duration is not ready yet on a freshly opened decoder.
        /// </summary>
        private static void SeekMusicIfNeeded(SoundPlayer player, double startOffsetSeconds)
        {
            if (startOffsetSeconds <= 1e-9) return;
            try
            {
                var fmt = player.Format;
                int ch = fmt.Channels;
                int sr = fmt.SampleRate;
                if (ch <= 0 || sr <= 0) return;

                double interleaved = startOffsetSeconds * sr * ch;
                if (!double.IsFinite(interleaved) || interleaved <= 0) return;

                int offset = (int)Math.Round(interleaved);
                offset = (offset / ch) * ch;

                var provider = player.DataProvider;
                if (!provider.CanSeek) return;

                int len = provider.Length;
                if (len > 0)
                {
                    int max = Math.Max(0, len - ch);
                    if (offset > max) offset = max;
                }

                if (offset <= 0) return;
                player.Seek(offset);
            }
            catch { /* ignore */ }
        }

        public void StopMusic(int crossfadeMs)
        {
            int gen = Interlocked.Increment(ref _playMusicGeneration);
            CancelMusicFade();

            SoundPlayer? p = currentMusicPlayer;
            StreamDataProvider? prov = currentMusicStreamProvider;
            if (p == null) return;

            currentMusicPlayer = null;
            currentMusicStreamProvider = null;

            if (crossfadeMs <= 0)
            {
                try { p.Stop(); } catch { /* ignore */ }
                try { SafeRemoveAndDispose(musicMixer, p, prov); } catch { /* ignore */ }
                return;
            }

            float v0;
            try { v0 = Clamp01(p.Volume); }
            catch { v0 = 1f; }

            var cts = new CancellationTokenSource();
            lock (_musicFadeLock)
                _musicFadeCts = cts;

            _ = Task.Run(async () =>
            {
                try
                {
                    await RunMusicFadeOutCoreAsync(p, prov, v0, crossfadeMs, gen, cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    PostMixerAction(() =>
                    {
                        try { p.Stop(); } catch { /* ignore */ }
                        try { SafeRemoveAndDispose(musicMixer, p, prov); } catch { /* ignore */ }
                    });
                }
                finally
                {
                    FinishMusicFadeCts(cts);
                }
            });
        }

        private async Task RunMusicFadeOutCoreAsync(
            SoundPlayer player,
            StreamDataProvider? provider,
            float volumeStart,
            int fadeMs,
            int fadeGeneration,
            CancellationToken cancellationToken)
        {
            int stepCount = Math.Clamp(fadeMs / 25, 4, 80);
            int delayMs = Math.Max(1, fadeMs / stepCount);

            for (int i = 1; i <= stepCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (Volatile.Read(ref _playMusicGeneration) != fadeGeneration)
                    break;

                await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                float t = (float)i / stepCount;
                float v = volumeStart * (1f - t);
                PostMixerAction(() =>
                {
                    if (Volatile.Read(ref _playMusicGeneration) != fadeGeneration)
                        return;
                    try { player.Volume = Clamp01(v); } catch { /* ignore */ }
                });
            }

            PostMixerAction(() =>
            {
                if (Volatile.Read(ref _playMusicGeneration) != fadeGeneration)
                {
                    try { player.Stop(); } catch { /* ignore */ }
                    try { SafeRemoveAndDispose(musicMixer, player, provider); } catch { /* ignore */ }
                    return;
                }
                try { player.Stop(); } catch { /* ignore */ }
                try { SafeRemoveAndDispose(musicMixer, player, provider); } catch { /* ignore */ }
            });
        }

        private void StopAndDetachCurrentMusicPlayer()
        {
            if (currentMusicPlayer == null) return;
            try
            {
                var p = currentMusicPlayer;
                var prov = currentMusicStreamProvider;
                currentMusicPlayer = null;
                currentMusicStreamProvider = null;
                try { p.Stop(); } catch { /* ignore */ }
                SafeRemoveAndDispose(musicMixer, p, prov);
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex, "SoundFlowAudioEngine.StopAndDetachCurrentMusicPlayer", "Could not stop music");
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

        private static void SafeRemoveAndDispose(Mixer? mixer, SoundPlayer player, StreamDataProvider? provider)
        {
            try { mixer?.RemoveComponent(player); } catch { }
            try { player.Dispose(); } catch { }
            try { provider?.Dispose(); } catch { }
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
