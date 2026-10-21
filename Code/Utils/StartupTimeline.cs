using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace RPGGame.Utils
{
    /// <summary>
    /// Records named GUI startup phases (ms since process start, delta since previous mark) and
    /// appends the run to GameData/startup_timing.log when <see cref="Flush"/> is called at window reveal.
    /// </summary>
    public static class StartupTimeline
    {
        private const int MaxLogBytes = 256 * 1024;
        private static readonly object _lock = new object();
        private static readonly List<(string phase, double atMs, int threadId)> _marks = new();
        private static readonly DateTime _processStartUtc = SafeProcessStartUtc();
        private static bool _flushed;

        /// <summary>Records a phase boundary. No-op after <see cref="Flush"/>.</summary>
        public static void Mark(string phase)
        {
            double atMs = (DateTime.UtcNow - _processStartUtc).TotalMilliseconds;
            lock (_lock)
            {
                if (_flushed)
                    return;
                _marks.Add((phase, atMs, System.Environment.CurrentManagedThreadId));
            }
        }

        /// <summary>Writes the recorded phases to the console and the startup log once.</summary>
        public static void Flush()
        {
            string report;
            lock (_lock)
            {
                if (_flushed)
                    return;
                _flushed = true;
                report = BuildReport();
            }

            Console.WriteLine(report);
            try
            {
                string path = RPGGame.GameConstants.ResolveGameDataPath("startup_timing.log");
                if (File.Exists(path) && new FileInfo(path).Length > MaxLogBytes)
                    File.Delete(path);
                File.AppendAllText(path, report + System.Environment.NewLine);
            }
            catch (Exception ex)
            {
                DebugLogger.Log("StartupTimeline", $"Could not write startup log: {ex.Message}");
            }
        }

        private static string BuildReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"=== Startup {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
            sb.AppendLine($"{"phase",-36} {"at ms",8} {"+ms",8}  thread");
            double previous = 0;
            foreach (var (phase, atMs, threadId) in _marks)
            {
                sb.AppendLine($"{phase,-36} {atMs,8:F0} {atMs - previous,8:F0}  {threadId}");
                previous = atMs;
            }
            return sb.ToString().TrimEnd();
        }

        private static DateTime SafeProcessStartUtc()
        {
            try
            {
                return Process.GetCurrentProcess().StartTime.ToUniversalTime();
            }
            catch
            {
                return DateTime.UtcNow;
            }
        }
    }
}
