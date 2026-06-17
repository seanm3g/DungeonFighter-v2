using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RPGGame.Utils;
using SoundFlow.Components;
using SoundFlow.Providers;

namespace RPGGame.Audio
{
    public sealed partial class SoundFlowAudioEngine
    {
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
            bool useFadeIn = MusicFadeLoopPolicy.ShouldFadeIncomingTrack(crossfadeMs, outgoing != null);

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
                bool mp3FadeIn = useFadeIn;

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
                            var newPlayer = new SoundPlayer(engine!, format, provider);
                            ConfigureMusicPlayerForManualLooping(newPlayer);
                            bool cf = mp3CrossfadeOld != null;
                            newPlayer.Volume = (cf || mp3FadeIn) ? 0f : targetVol;
                            musicMixer!.AddComponent(newPlayer);
                            currentMusicStreamProvider = provider;
                            currentMusicPlayer = newPlayer;
                            AttachCurrentMusicManualLoop(newPlayer, gen);
                            SeekMusicIfNeeded(newPlayer, startOffsetSeconds);
                            newPlayer.Play();
                            if (cf)
                                ScheduleMusicCrossfade(mp3CrossfadeOld!, mp3CrossfadeOldProv, newPlayer, targetVol, crossfadeMs, gen);
                            else if (mp3FadeIn)
                                ScheduleMusicFadeIn(newPlayer, targetVol, crossfadeMs, gen);
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
                var newPlayer = new SoundPlayer(engine!, format, provider);
                ConfigureMusicPlayerForManualLooping(newPlayer);
                newPlayer.Volume = (useCrossfade || useFadeIn) ? 0f : targetVol;
                musicMixer!.AddComponent(newPlayer);
                currentMusicStreamProvider = provider;
                currentMusicPlayer = newPlayer;
                AttachCurrentMusicManualLoop(newPlayer, gen);
                SeekMusicIfNeeded(newPlayer, startOffsetSeconds);
                newPlayer.Play();
                if (useCrossfade)
                    ScheduleMusicCrossfade(outgoing!, outgoingProv, newPlayer, targetVol, crossfadeMs, gen);
                else if (useFadeIn)
                    ScheduleMusicFadeIn(newPlayer, targetVol, crossfadeMs, gen);
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex, "SoundFlowAudioEngine.PlayMusic", $"Could not start music '{filePath}'");
            }
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

        private static void ConfigureMusicPlayerForManualLooping(SoundPlayer player)
        {
            // SoundFlow's built-in looping refills the same audio callback recursively at EOF.
            // We restart on the next posted mixer action instead, which avoids recursive EOF handling.
            try { player.IsLooping = false; } catch { /* ignore */ }
        }

        private static void RestartMusicPlayerFromBeginning(SoundPlayer player)
        {
            try
            {
                ConfigureMusicPlayerForManualLooping(player);
                if (player.DataProvider.CanSeek)
                    player.Seek(0);
                else
                    player.Stop();
            }
            catch
            {
                try { player.Stop(); } catch { /* ignore */ }
            }

            try { player.Play(); } catch { /* ignore */ }
        }
    }
}
