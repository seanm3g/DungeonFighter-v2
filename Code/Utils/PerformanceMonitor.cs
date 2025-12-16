using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace RPGGame.Utils
{
    /// <summary>
    /// Performance monitoring infrastructure with counters and regression testing
    /// Tracks performance metrics for critical paths and detects regressions
    /// </summary>
    public static class PerformanceMonitor
    {
        private static readonly Dictionary<string, PerformanceCounter> _counters = new Dictionary<string, PerformanceCounter>();
        private static readonly Dictionary<string, List<long>> _history = new Dictionary<string, List<long>>();
        private static readonly object _lockObject = new object();
        private static bool _enabled = true;
        private const int MaxHistorySize = 100;

        /// <summary>
        /// Enables or disables performance monitoring
        /// </summary>
        public static bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        /// <summary>
        /// Starts timing a performance operation
        /// </summary>
        /// <param name="operationName">Name of the operation being timed</param>
        /// <returns>IDisposable timer that stops when disposed</returns>
        public static IDisposable StartTimer(string operationName)
        {
            if (!_enabled) return new NullTimer();
            
            return new PerformanceTimer(operationName);
        }

        /// <summary>
        /// Records a performance measurement
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="milliseconds">Duration in milliseconds</param>
        public static void RecordMeasurement(string operationName, long milliseconds)
        {
            if (!_enabled) return;

            lock (_lockObject)
            {
                if (!_counters.ContainsKey(operationName))
                {
                    _counters[operationName] = new PerformanceCounter(operationName);
                }

                var counter = _counters[operationName];
                counter.RecordMeasurement(milliseconds);

                // Store in history for regression detection
                if (!_history.ContainsKey(operationName))
                {
                    _history[operationName] = new List<long>();
                }

                var history = _history[operationName];
                history.Add(milliseconds);
                if (history.Count > MaxHistorySize)
                {
                    history.RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// Gets performance statistics for an operation
        /// </summary>
        public static PerformanceStats GetStats(string operationName)
        {
            lock (_lockObject)
            {
                if (!_counters.TryGetValue(operationName, out var counter))
                {
                    return new PerformanceStats(operationName, 0, 0, 0, 0);
                }

                return new PerformanceStats(
                    operationName,
                    counter.Count,
                    counter.AverageMs,
                    counter.MinMs,
                    counter.MaxMs
                );
            }
        }

        /// <summary>
        /// Gets all performance statistics
        /// </summary>
        public static List<PerformanceStats> GetAllStats()
        {
            lock (_lockObject)
            {
                return _counters.Values
                    .Select(c => new PerformanceStats(
                        c.OperationName,
                        c.Count,
                        c.AverageMs,
                        c.MinMs,
                        c.MaxMs
                    ))
                    .OrderBy(s => s.OperationName)
                    .ToList();
            }
        }

        /// <summary>
        /// Checks for performance regressions by comparing current performance to historical average
        /// </summary>
        /// <param name="operationName">Name of the operation to check</param>
        /// <param name="thresholdMultiplier">Multiplier for regression threshold (default 1.5 = 50% slower)</param>
        /// <returns>True if regression detected, false otherwise</returns>
        public static bool CheckRegression(string operationName, double thresholdMultiplier = 1.5)
        {
            if (!_enabled) return false;

            lock (_lockObject)
            {
                if (!_history.TryGetValue(operationName, out var history) || history.Count < 10)
                {
                    return false; // Not enough data
                }

                // Calculate baseline (average of first half of history)
                int baselineCount = history.Count / 2;
                double baseline = history.Take(baselineCount).Average();

                // Calculate current (average of recent measurements)
                int recentCount = Math.Min(10, history.Count - baselineCount);
                double current = history.TakeLast(recentCount).Average();

                // Check if current is significantly worse than baseline
                return current > baseline * thresholdMultiplier;
            }
        }

        /// <summary>
        /// Gets performance report as formatted string
        /// </summary>
        public static string GetReport()
        {
            var stats = GetAllStats();
            if (stats.Count == 0)
            {
                return "No performance data collected.";
            }

            var report = new System.Text.StringBuilder();
            report.AppendLine("=== PERFORMANCE REPORT ===");
            report.AppendLine();

            foreach (var stat in stats)
            {
                report.AppendLine($"{stat.OperationName}:");
                report.AppendLine($"  Count: {stat.Count}");
                report.AppendLine($"  Average: {stat.AverageMs:F2}ms");
                report.AppendLine($"  Min: {stat.MinMs}ms");
                report.AppendLine($"  Max: {stat.MaxMs}ms");
                
                if (CheckRegression(stat.OperationName))
                {
                    report.AppendLine("  ⚠️  REGRESSION DETECTED");
                }
                
                report.AppendLine();
            }

            return report.ToString();
        }

        /// <summary>
        /// Clears all performance data
        /// </summary>
        public static void Clear()
        {
            lock (_lockObject)
            {
                _counters.Clear();
                _history.Clear();
            }
        }

        /// <summary>
        /// Resets counters for a specific operation
        /// </summary>
        public static void Reset(string operationName)
        {
            lock (_lockObject)
            {
                _counters.Remove(operationName);
                _history.Remove(operationName);
            }
        }

        private class PerformanceCounter
        {
            public string OperationName { get; }
            public int Count { get; private set; }
            public double AverageMs { get; private set; }
            public long MinMs { get; private set; } = long.MaxValue;
            public long MaxMs { get; private set; } = long.MinValue;

            private long _totalMs = 0;

            public PerformanceCounter(string operationName)
            {
                OperationName = operationName;
            }

            public void RecordMeasurement(long milliseconds)
            {
                Count++;
                _totalMs += milliseconds;
                AverageMs = (double)_totalMs / Count;
                
                if (milliseconds < MinMs) MinMs = milliseconds;
                if (milliseconds > MaxMs) MaxMs = milliseconds;
            }
        }

        private class PerformanceTimer : IDisposable
        {
            private readonly string _operationName;
            private readonly Stopwatch _stopwatch;

            public PerformanceTimer(string operationName)
            {
                _operationName = operationName;
                _stopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                _stopwatch.Stop();
                RecordMeasurement(_operationName, _stopwatch.ElapsedMilliseconds);
            }
        }

        private class NullTimer : IDisposable
        {
            public void Dispose() { }
        }
    }

    /// <summary>
    /// Performance statistics for an operation
    /// </summary>
    public class PerformanceStats
    {
        public string OperationName { get; }
        public int Count { get; }
        public double AverageMs { get; }
        public long MinMs { get; }
        public long MaxMs { get; }

        public PerformanceStats(string operationName, int count, double averageMs, long minMs, long maxMs)
        {
            OperationName = operationName;
            Count = count;
            AverageMs = averageMs;
            MinMs = minMs;
            MaxMs = maxMs;
        }
    }
}
