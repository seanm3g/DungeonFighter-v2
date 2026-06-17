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
    }
}
