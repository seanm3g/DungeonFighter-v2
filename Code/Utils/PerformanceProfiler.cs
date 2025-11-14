using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RPGGame.Utils
{
    /// <summary>
    /// Performance profiler for measuring and tracking operation execution times
    /// </summary>
    public static class PerformanceProfiler
    {
        private static readonly Dictionary<string, List<long>> _measurements = new Dictionary<string, List<long>>();
        private static readonly Dictionary<string, Stopwatch> _activeTimers = new Dictionary<string, Stopwatch>();
        private static readonly object _lock = new object();
        
        /// <summary>
        /// Starts measuring an operation
        /// </summary>
        /// <param name="operation">Name of the operation to measure</param>
        public static void StartMeasurement(string operation)
        {
            lock (_lock)
            {
                if (_activeTimers.ContainsKey(operation))
                {
                    _activeTimers[operation].Restart();
                }
                else
                {
                    _activeTimers[operation] = Stopwatch.StartNew();
                }
            }
        }
        
        /// <summary>
        /// Ends measuring an operation and records the result
        /// </summary>
        /// <param name="operation">Name of the operation to stop measuring</param>
        public static void EndMeasurement(string operation)
        {
            lock (_lock)
            {
                if (_activeTimers.TryGetValue(operation, out var stopwatch))
                {
                    stopwatch.Stop();
                    var elapsedMs = stopwatch.ElapsedMilliseconds;
                    
                    if (!_measurements.ContainsKey(operation))
                    {
                        _measurements[operation] = new List<long>();
                    }
                    
                    _measurements[operation].Add(elapsedMs);
                }
            }
        }
        
        /// <summary>
        /// Measures the execution time of an operation
        /// </summary>
        /// <param name="operation">Name of the operation</param>
        /// <param name="action">Action to measure</param>
        public static void Measure(string operation, System.Action action)
        {
            StartMeasurement(operation);
            try
            {
                action();
            }
            finally
            {
                EndMeasurement(operation);
            }
        }
        
        /// <summary>
        /// Measures the execution time of a function and returns its result
        /// </summary>
        /// <typeparam name="T">Return type of the function</typeparam>
        /// <param name="operation">Name of the operation</param>
        /// <param name="func">Function to measure</param>
        /// <returns>Result of the function</returns>
        public static T Measure<T>(string operation, System.Func<T> func)
        {
            StartMeasurement(operation);
            try
            {
                return func();
            }
            finally
            {
                EndMeasurement(operation);
            }
        }
        
        /// <summary>
        /// Gets performance statistics for an operation
        /// </summary>
        /// <param name="operation">Name of the operation</param>
        /// <returns>Performance statistics or null if no data</returns>
        public static PerformanceStats? GetStats(string operation)
        {
            lock (_lock)
            {
                if (!_measurements.TryGetValue(operation, out var times) || times.Count == 0)
                {
                    return null;
                }
                
                return new PerformanceStats
                {
                    Operation = operation,
                    Count = times.Count,
                    Average = times.Average(),
                    Min = times.Min(),
                    Max = times.Max(),
                    Total = times.Sum()
                };
            }
        }
        
        /// <summary>
        /// Generates a performance report for all measured operations
        /// </summary>
        /// <returns>Formatted performance report</returns>
        public static string GenerateReport()
        {
            lock (_lock)
            {
                if (_measurements.Count == 0)
                {
                    return "No performance data available.";
                }
                
                var report = new System.Text.StringBuilder();
                report.AppendLine("=== Performance Report ===");
                report.AppendLine();
                
                foreach (var kvp in _measurements.OrderBy(x => x.Key))
                {
                    var stats = GetStats(kvp.Key);
                    if (stats != null)
                    {
                        report.AppendLine($"Operation: {stats.Operation}");
                        report.AppendLine($"  Count: {stats.Count}");
                        report.AppendLine($"  Average: {stats.Average:F2}ms");
                        report.AppendLine($"  Min: {stats.Min}ms");
                        report.AppendLine($"  Max: {stats.Max}ms");
                        report.AppendLine($"  Total: {stats.Total}ms");
                        report.AppendLine();
                    }
                }
                
                return report.ToString();
            }
        }
        
        /// <summary>
        /// Clears all performance data
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _measurements.Clear();
                _activeTimers.Clear();
            }
        }
        
        /// <summary>
        /// Performance statistics for an operation
        /// </summary>
        public class PerformanceStats
        {
            public string Operation { get; set; } = string.Empty;
            public int Count { get; set; }
            public double Average { get; set; }
            public long Min { get; set; }
            public long Max { get; set; }
            public long Total { get; set; }
        }
    }
}
