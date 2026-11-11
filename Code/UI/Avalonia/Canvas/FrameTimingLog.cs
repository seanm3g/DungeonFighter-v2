using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace RPGGame.UI.Avalonia.Canvas
{
    /// <summary>
    /// Opt-in paint timing (DF_FRAME_TIMING=1): records named samples in ms and writes a summary line every 2 s to
    /// frame-timing.log next to the executable. Off by default; when off every call is a single bool check.
    /// </summary>
    internal static class FrameTimingLog
    {
        public static readonly bool Enabled = System.Environment.GetEnvironmentVariable("DF_FRAME_TIMING") == "1";
        private static readonly object Gate = new();
        private static readonly Dictionary<string, List<double>> Samples = new();
        private static readonly Stopwatch Clock = Stopwatch.StartNew();
        private static double lastFlush;

        public static long Now() => Stopwatch.GetTimestamp();

        public static double MsSince(long start) => (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;

        public static void Add(string name, double ms)
        {
            if (!Enabled)
                return;
            lock (Gate)
            {
                if (!Samples.TryGetValue(name, out var list))
                    Samples[name] = list = new List<double>();
                list.Add(ms);
                double now = Clock.Elapsed.TotalSeconds;
                if (now - lastFlush < 2)
                    return;
                lastFlush = now;
                var parts = Samples.OrderBy(k => k.Key).Select(k =>
                {
                    var s = k.Value.OrderBy(v => v).ToList();
                    return $"{k.Key}: n={s.Count} med={s[s.Count / 2]:F1} p90={s[(int)(s.Count * 0.9)]:F1} max={s[^1]:F1}";
                });
                try
                {
                    File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "frame-timing.log"),
                        $"{DateTime.Now:HH:mm:ss} {string.Join(" | ", parts)}{System.Environment.NewLine}");
                }
                catch { }
                Samples.Clear();
            }
        }
    }
}
