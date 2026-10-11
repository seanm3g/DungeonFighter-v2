using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;

namespace RPGGame.Diagnostics
{
    /// <summary>Opt-in (DF_PROFILE=1) call counters and timers for headless tools.</summary>
    public static class ToolProfiler
    {
        public static readonly bool Enabled = System.Environment.GetEnvironmentVariable("DF_PROFILE") == "1";
        private static readonly ConcurrentDictionary<string, (long calls, long ticks)> Stats = new();

        public static long Start() => Enabled ? Stopwatch.GetTimestamp() : 0;

        public static void Stop(string key, long start)
        {
            if (!Enabled)
                return;
            long elapsed = Stopwatch.GetTimestamp() - start;
            Stats.AddOrUpdate(key, (1, elapsed), (_, v) => (v.calls + 1, v.ticks + elapsed));
        }

        public static string Report() =>
            string.Join(System.Environment.NewLine, Stats.OrderByDescending(kv => kv.Value.ticks)
                .Select(kv => $"  {kv.Key,-40} calls {kv.Value.calls,10}  total {kv.Value.ticks * 1000.0 / Stopwatch.Frequency,9:F0} ms"));
    }
}
