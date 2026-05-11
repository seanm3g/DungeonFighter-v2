using System;
using System.IO;
using System.Text;
using NLayer;

namespace RPGGame.Audio
{
    /// <summary>
    /// Decodes MP3 to an in-memory PCM 16-bit WAV so SoundFlow can open it without the synchronous
    /// MP3 probe that froze the UI (debug session c9a928).
    /// </summary>
    internal static class Mp3ToWavMemoryStream
    {
        /// <param name="maxDecodeSeconds">Caps decode work and memory for very long tracks (settings preview).</param>
        internal static MemoryStream Build(string mp3Path, int maxDecodeSeconds = 120)
        {
            using var mp3 = new MpegFile(mp3Path);
            Validate(mp3, out int sampleRate, out int channels);
            long? cap = maxDecodeSeconds <= 0 ? null : (long)sampleRate * channels * maxDecodeSeconds;
            return DecodeToWavStream(mp3, sampleRate, channels, cap);
        }

        /// <summary>Decodes the entire MP3 to WAV (for looping BGM; stops at end of file).</summary>
        internal static MemoryStream BuildFullTrack(string mp3Path)
        {
            using var mp3 = new MpegFile(mp3Path);
            Validate(mp3, out int sampleRate, out int channels);
            return DecodeToWavStream(mp3, sampleRate, channels, maxFloatSamples: null);
        }

        private static void Validate(MpegFile mp3, out int sampleRate, out int channels)
        {
            channels = mp3.Channels;
            sampleRate = mp3.SampleRate;
            if (channels <= 0 || sampleRate <= 0)
                throw new InvalidDataException("Invalid MP3 format (channels/sample rate).");
        }

        private static MemoryStream DecodeToWavStream(MpegFile mp3, int sampleRate, int channels, long? maxFloatSamples)
        {
            long cap = maxFloatSamples ?? long.MaxValue;
            var floatBuf = new float[8192];
            byte[] pcm;
            using (var pcmBody = new MemoryStream())
            {
                using (var bw = new BinaryWriter(pcmBody))
                {
                    long floatSamplesDecoded = 0;
                    while (floatSamplesDecoded < cap)
                    {
                        long remaining = cap - floatSamplesDecoded;
                        int want = (int)Math.Min((long)floatBuf.Length, remaining);
                        if (want <= 0) break;
                        int n = mp3.ReadSamples(floatBuf, 0, want);
                        if (n <= 0) break;
                        for (int i = 0; i < n; i++)
                        {
                            float f = floatBuf[i];
                            short s = (short)Math.Clamp((int)Math.Round(f * 32767f), -32768, 32767);
                            bw.Write(s);
                        }

                        floatSamplesDecoded += n;
                    }
                }

                pcm = pcmBody.ToArray();
            }

            return WriteWavPcm16(sampleRate, (short)channels, pcm);
        }

        private static MemoryStream WriteWavPcm16(int sampleRate, short channels, byte[] pcmData)
        {
            int byteRate = sampleRate * channels * 2;
            int blockAlign = channels * 2;
            int dataChunkSize = pcmData.Length;
            int riffSize = 36 + dataChunkSize;

            var ms = new MemoryStream(8 + riffSize);
            using (var w = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
            {
                w.Write(Encoding.ASCII.GetBytes("RIFF"));
                w.Write(riffSize);
                w.Write(Encoding.ASCII.GetBytes("WAVE"));
                w.Write(Encoding.ASCII.GetBytes("fmt "));
                w.Write(16);
                w.Write((short)1);
                w.Write(channels);
                w.Write(sampleRate);
                w.Write(byteRate);
                w.Write((short)blockAlign);
                w.Write((short)16);
                w.Write(Encoding.ASCII.GetBytes("data"));
                w.Write(dataChunkSize);
                w.Write(pcmData);
            }

            ms.Position = 0;
            return ms;
        }
    }
}
