using System;
using System.Threading;
using System.Threading.Tasks;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Providers;

namespace RPGGame.Audio
{
    public sealed partial class SoundFlowAudioEngine
    {
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

        private void ScheduleMusicFadeIn(
            SoundPlayer newPlayer,
            float targetVolume,
            int fadeMs,
            int fadeGeneration)
        {
            var cts = new CancellationTokenSource();
            lock (_musicFadeLock)
                _musicFadeCts = cts;

            _ = Task.Run(async () =>
            {
                try
                {
                    await RunMusicFadeInCoreAsync(
                        newPlayer, targetVolume, fadeMs, fadeGeneration, cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // A newer transition or stop will take over the current player and volume.
                }
                finally
                {
                    FinishMusicFadeCts(cts);
                }
            });
        }

        private async Task RunMusicFadeInCoreAsync(
            SoundPlayer newPlayer,
            float targetVolume,
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
                float vIn = targetVolume * t;
                PostMixerAction(() =>
                {
                    if (Volatile.Read(ref _playMusicGeneration) != fadeGeneration)
                        return;
                    try { newPlayer.Volume = Clamp01(vIn); }
                    catch { /* ignore */ }
                });
            }

            PostMixerAction(() =>
            {
                if (Volatile.Read(ref _playMusicGeneration) != fadeGeneration)
                    return;
                try { newPlayer.Volume = Clamp01(targetVolume); }
                catch { /* ignore */ }
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
            EventHandler<EventArgs> restartEndedLoop = AttachMusicFadeLoopRestart(oldPlayer, fadeGeneration, cancellationToken);

            try
            {
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
                            RestartStoppedMusicPlayerDuringFade(oldPlayer, fadeGeneration, cancellationToken);
                            oldPlayer.Volume = Clamp01(vOut);
                            newPlayer.Volume = Clamp01(vIn);
                        }
                        catch { /* ignore */ }
                    });
                }
            }
            finally
            {
                try { oldPlayer.PlaybackEnded -= restartEndedLoop; } catch { /* ignore */ }
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

        private void RestartStoppedMusicPlayerDuringFade(
            SoundPlayer player,
            int fadeGeneration,
            CancellationToken cancellationToken)
        {
            if (!MusicFadeLoopPolicy.ShouldRestartEndedTrack(
                fadeGeneration,
                Volatile.Read(ref _playMusicGeneration),
                cancellationToken.IsCancellationRequested))
            {
                return;
            }

            try
            {
                if (player.State != PlaybackState.Stopped) return;
                RestartMusicPlayerFromBeginning(player);
            }
            catch { /* ignore */ }
        }

        private EventHandler<EventArgs> AttachMusicFadeLoopRestart(
            SoundPlayer player,
            int fadeGeneration,
            CancellationToken cancellationToken)
        {
            ConfigureMusicPlayerForManualLooping(player);
            EventHandler<EventArgs> restartEndedLoop = (_, _) =>
            {
                if (!MusicFadeLoopPolicy.ShouldRestartEndedTrack(
                    fadeGeneration,
                    Volatile.Read(ref _playMusicGeneration),
                    cancellationToken.IsCancellationRequested))
                {
                    return;
                }

                PostMixerAction(() =>
                {
                    if (!MusicFadeLoopPolicy.ShouldRestartEndedTrack(
                        fadeGeneration,
                        Volatile.Read(ref _playMusicGeneration),
                        cancellationToken.IsCancellationRequested))
                    {
                        return;
                    }

                    RestartMusicPlayerFromBeginning(player);
                });
            };
            player.PlaybackEnded += restartEndedLoop;
            return restartEndedLoop;
        }

        private void AttachCurrentMusicManualLoop(SoundPlayer player, int trackGeneration)
        {
            EventHandler<EventArgs>? restartCurrentLoop = null;
            restartCurrentLoop = (_, _) =>
            {
                PostMixerAction(() =>
                {
                    if (!MusicFadeLoopPolicy.ShouldRestartCurrentTrack(
                        trackGeneration,
                        Volatile.Read(ref _playMusicGeneration),
                        ReferenceEquals(currentMusicPlayer, player)))
                    {
                        try { player.PlaybackEnded -= restartCurrentLoop; } catch { /* ignore */ }
                        return;
                    }

                    RestartMusicPlayerFromBeginning(player);
                });
            };

            player.PlaybackEnded += restartCurrentLoop!;
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
            EventHandler<EventArgs> restartEndedLoop = AttachMusicFadeLoopRestart(player, fadeGeneration, cancellationToken);

            try
            {
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
                        try
                        {
                            RestartStoppedMusicPlayerDuringFade(player, fadeGeneration, cancellationToken);
                            player.Volume = Clamp01(v);
                        }
                        catch { /* ignore */ }
                    });
                }
            }
            finally
            {
                try { player.PlaybackEnded -= restartEndedLoop; } catch { /* ignore */ }
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
    }
}
