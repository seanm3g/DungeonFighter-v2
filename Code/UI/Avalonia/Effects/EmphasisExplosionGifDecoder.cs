using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SkiaSharp;

namespace RPGGame.UI.Avalonia.Effects
{
    /// <summary>
    /// Decodes an animated GIF into BGRA frames with near-black pixels made transparent
    /// so the starburst can composite over the game background without a black box.
    /// Avalonia <see cref="Bitmap"/> conversion is deferred (needs a live platform).
    /// </summary>
    public static class EmphasisExplosionGifDecoder
    {
        /// <summary>RGB max channel at or below this becomes fully transparent.</summary>
        public const byte BlackKeyThreshold = 18;

        public sealed class RawGifFrame
        {
            public RawGifFrame(byte[] bgraPremul, int width, int height, int delayMs)
            {
                BgraPremul = bgraPremul ?? throw new ArgumentNullException(nameof(bgraPremul));
                Width = width;
                Height = height;
                DelayMs = delayMs;
            }

            public byte[] BgraPremul { get; }
            public int Width { get; }
            public int Height { get; }
            public int DelayMs { get; }
        }

        public static bool TryLoad(string path, out IReadOnlyList<RawGifFrame> frames, out string? error)
        {
            frames = Array.Empty<RawGifFrame>();
            error = null;

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                error = "file missing";
                return false;
            }

            try
            {
                using var stream = File.OpenRead(path);
                using var codec = SKCodec.Create(stream);
                if (codec == null)
                {
                    error = "codec null";
                    return false;
                }

                int count = Math.Max(1, codec.FrameCount);
                var info = codec.Info.WithAlphaType(SKAlphaType.Premul);
                var list = new List<RawGifFrame>(count);

                using var composed = new SKBitmap(info);
                using var canvas = new SKCanvas(composed);
                canvas.Clear(SKColors.Transparent);

                SKBitmap? savedPrevious = null;
                for (int i = 0; i < count; i++)
                {
                    SKCodecFrameInfo frameInfo = codec.FrameInfo[i];
                    int prior = frameInfo.RequiredFrame;

                    if (frameInfo.DisposalMethod == SKCodecAnimationDisposalMethod.RestorePrevious
                        && savedPrevious != null)
                    {
                        canvas.Clear(SKColors.Transparent);
                        canvas.DrawBitmap(savedPrevious, 0, 0);
                    }

                    using var frameBmp = new SKBitmap(info);
                    var opts = new SKCodecOptions(i, prior);
                    var result = codec.GetPixels(info, frameBmp.GetPixels(), opts);
                    if (result != SKCodecResult.Success && result != SKCodecResult.IncompleteInput)
                    {
                        error = $"frame {i}: {result}";
                        savedPrevious?.Dispose();
                        return false;
                    }

                    if (frameInfo.DisposalMethod == SKCodecAnimationDisposalMethod.RestorePrevious
                        && savedPrevious == null)
                    {
                        savedPrevious = composed.Copy();
                    }

                    canvas.DrawBitmap(frameBmp, 0, 0);

                    using var keyed = KeyOutNearBlack(composed);
                    list.Add(ToRawFrame(keyed, NormalizeDelayMs(frameInfo.Duration)));

                    if (frameInfo.DisposalMethod == SKCodecAnimationDisposalMethod.RestoreBackgroundColor)
                    {
                        canvas.Clear(SKColors.Transparent);
                        savedPrevious?.Dispose();
                        savedPrevious = null;
                    }
                    else if (frameInfo.DisposalMethod != SKCodecAnimationDisposalMethod.RestorePrevious)
                    {
                        savedPrevious?.Dispose();
                        savedPrevious = null;
                    }
                }

                savedPrevious?.Dispose();
                frames = list;
                return list.Count > 0;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static SKBitmap KeyOutNearBlack(SKBitmap source)
        {
            var copy = source.Copy() ?? throw new InvalidOperationException("bitmap copy failed");
            KeyOutNearBlackInPlace(copy);
            return copy;
        }

        /// <summary>Testable chroma-key: returns how many pixels were cleared.</summary>
        public static int KeyOutNearBlackInPlace(SKBitmap bitmap)
        {
            IntPtr ptr = bitmap.GetPixels();
            if (ptr == IntPtr.Zero)
                return 0;

            int n = bitmap.Width * bitmap.Height;
            int cleared = 0;
            unsafe
            {
                uint* p = (uint*)ptr.ToPointer();
                for (int i = 0; i < n; i++)
                {
                    uint px = p[i];
                    byte a = (byte)((px >> 24) & 0xFF);
                    byte r = (byte)((px >> 16) & 0xFF);
                    byte g = (byte)((px >> 8) & 0xFF);
                    byte b = (byte)(px & 0xFF);
                    if (a == 0)
                        continue;
                    if (r <= BlackKeyThreshold && g <= BlackKeyThreshold && b <= BlackKeyThreshold)
                    {
                        p[i] = 0;
                        cleared++;
                    }
                }
            }
            return cleared;
        }

        public static RawGifFrame ToRawFrame(SKBitmap sk, int delayMs)
        {
            int w = sk.Width;
            int h = sk.Height;
            int stride = sk.RowBytes;
            var bytes = new byte[stride * h];
            System.Runtime.InteropServices.Marshal.Copy(sk.GetPixels(), bytes, 0, bytes.Length);
            return new RawGifFrame(bytes, w, h, delayMs);
        }

        /// <summary>
        /// Builds an Avalonia bitmap from a raw frame. Requires Avalonia platform (GUI); returns null in headless tests.
        /// </summary>
        public static Bitmap? TryCreateAvaloniaBitmap(RawGifFrame frame)
        {
            try
            {
                var bmp = new WriteableBitmap(
                    new PixelSize(frame.Width, frame.Height),
                    new Vector(96, 96),
                    PixelFormat.Bgra8888,
                    AlphaFormat.Premul);

                using (var fb = bmp.Lock())
                {
                    int srcStride = frame.Width * 4;
                    // Raw buffer may use SK row bytes (>= width*4).
                    int packedStride = frame.BgraPremul.Length / Math.Max(1, frame.Height);
                    int copyBytes = Math.Min(srcStride, Math.Min(fb.RowBytes, packedStride));
                    for (int y = 0; y < frame.Height; y++)
                    {
                        System.Runtime.InteropServices.Marshal.Copy(
                            frame.BgraPremul,
                            y * packedStride,
                            fb.Address + y * fb.RowBytes,
                            copyBytes);
                    }
                }

                return bmp;
            }
            catch
            {
                return null;
            }
        }

        public static int NormalizeDelayMs(int duration)
        {
            if (duration <= 0)
                return 100;
            return Math.Clamp(duration, 16, 2000);
        }

        public static void DisposeBitmaps(IEnumerable<Bitmap> bitmaps)
        {
            foreach (var b in bitmaps)
                b.Dispose();
        }
    }
}
