using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace RPGGame.Utils
{
    /// <summary>
    /// Tracks and reports build time and execution time metrics
    /// </summary>
    public static class BuildExecutionMetrics
    {
        private static readonly string MetricsFilePath = Path.Combine("GameData", "build_execution_metrics.json");
        private static readonly object _lockObject = new object();
        private static Stopwatch? _executionStopwatch;
        private static DateTime? _executionStartTime;
        private static Stopwatch? _launchStopwatch;
        private static DateTime? _launchStartTime;

        /// <summary>
        /// Starts tracking application execution time
        /// </summary>
        public static void StartExecutionTracking()
        {
            _executionStopwatch = Stopwatch.StartNew();
            _executionStartTime = DateTime.UtcNow;
            
            // Also start launch time tracking
            _launchStopwatch = Stopwatch.StartNew();
            _launchStartTime = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Gets the compile/build time from the DLL's last write time
        /// Only returns a value if the DLL was written recently (within last 10 minutes)
        /// to avoid showing stale build times
        /// </summary>
        public static TimeSpan? GetCompileTime()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var assemblyPath = assembly.Location;
                
                if (string.IsNullOrEmpty(assemblyPath) || !File.Exists(assemblyPath))
                    return null;
                
                var compileTime = File.GetLastWriteTimeUtc(assemblyPath);
                var now = DateTime.UtcNow;
                var timeSinceCompile = now - compileTime;
                
                // Only return compile time if it was within the last 10 minutes
                // This ensures we only show compile time for fresh builds
                if (timeSinceCompile.TotalMinutes > 10)
                    return null;
                
                return timeSinceCompile;
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// Records and outputs launch time (time from program start to ready)
        /// </summary>
        public static void RecordLaunchTime(string mode = "GUI")
        {
            if (_launchStopwatch == null || _launchStartTime == null)
                return;
            
            _launchStopwatch.Stop();
            var launchTimeMs = _launchStopwatch.ElapsedMilliseconds;
            var launchTimeSeconds = _launchStopwatch.Elapsed.TotalSeconds;
            
            // Get compile time
            var compileTime = GetCompileTime();
            
            // Output to console
            Console.WriteLine("\n═══════════════════════════════════════════════════════");
            if (compileTime.HasValue)
            {
                var compileTimeSeconds = compileTime.Value.TotalSeconds;
                var compileTimeMs = (long)compileTime.Value.TotalMilliseconds;
                Console.WriteLine($"  Compile Time: {compileTimeSeconds:F2} seconds ({compileTimeMs} ms) ago");
            }
            else
            {
                Console.WriteLine($"  Compile Time: Unable to determine");
            }
            Console.WriteLine($"  Launch Time: {launchTimeSeconds:F2} seconds ({launchTimeMs} ms)");
            Console.WriteLine($"  Mode: {mode}");
            Console.WriteLine($"═══════════════════════════════════════════════════════\n");
        }

        /// <summary>
        /// Stops tracking and records execution time
        /// </summary>
        /// <param name="mode">Execution mode (GUI, MCP, PLAY, etc.)</param>
        public static void StopExecutionTracking(string mode = "GUI")
        {
            if (_executionStopwatch == null || _executionStartTime == null)
                return;

            _executionStopwatch.Stop();
            var executionTimeMs = _executionStopwatch.ElapsedMilliseconds;
            var executionTimeSeconds = _executionStopwatch.Elapsed.TotalSeconds;

            RecordExecutionMetric(mode, executionTimeMs, executionTimeSeconds);
            
            // Output to console if not in GUI mode
            if (mode != "GUI")
            {
                Console.WriteLine($"\n═══════════════════════════════════════════════════════");
                Console.WriteLine($"  Execution Time: {executionTimeSeconds:F2} seconds ({executionTimeMs} ms)");
                Console.WriteLine($"  Mode: {mode}");
                Console.WriteLine($"═══════════════════════════════════════════════════════\n");
            }
        }

        /// <summary>
        /// Records a build time metric
        /// </summary>
        /// <param name="buildType">Type of build (Debug, Release, etc.)</param>
        /// <param name="buildTimeMs">Build time in milliseconds</param>
        /// <param name="buildTimeSeconds">Build time in seconds</param>
        /// <param name="success">Whether the build succeeded</param>
        public static void RecordBuildMetric(string buildType, long buildTimeMs, double buildTimeSeconds, bool success)
        {
            var metric = new BuildMetric
            {
                Timestamp = DateTime.UtcNow,
                BuildType = buildType,
                BuildTimeMs = buildTimeMs,
                BuildTimeSeconds = buildTimeSeconds,
                Success = success
            };

            SaveMetric(metric);
            
            Console.WriteLine($"\n═══════════════════════════════════════════════════════");
            Console.WriteLine($"  Build Time: {buildTimeSeconds:F2} seconds ({buildTimeMs} ms)");
            Console.WriteLine($"  Build Type: {buildType}");
            Console.WriteLine($"  Status: {(success ? "SUCCESS" : "FAILED")}");
            Console.WriteLine($"═══════════════════════════════════════════════════════\n");
        }

        /// <summary>
        /// Records an execution time metric
        /// </summary>
        private static void RecordExecutionMetric(string mode, long executionTimeMs, double executionTimeSeconds)
        {
            var metric = new ExecutionMetric
            {
                Timestamp = DateTime.UtcNow,
                Mode = mode,
                ExecutionTimeMs = executionTimeMs,
                ExecutionTimeSeconds = executionTimeSeconds
            };

            SaveMetric(metric);
        }

        /// <summary>
        /// Saves a metric to the metrics file
        /// </summary>
        private static void SaveMetric(object metric)
        {
            try
            {
                lock (_lockObject)
                {
                    MetricsData? metricsData;
                    
                    // Load existing metrics
                    if (File.Exists(MetricsFilePath))
                    {
                        var json = File.ReadAllText(MetricsFilePath);
                        metricsData = JsonSerializer.Deserialize<MetricsData>(json);
                    }
                    else
                    {
                        metricsData = new MetricsData();
                    }

                    if (metricsData == null)
                        metricsData = new MetricsData();

                    // Add the new metric
                    if (metric is BuildMetric buildMetric)
                    {
                        metricsData.BuildMetrics.Add(buildMetric);
                        // Keep only last 100 build metrics
                        if (metricsData.BuildMetrics.Count > 100)
                            metricsData.BuildMetrics.RemoveAt(0);
                    }
                    else if (metric is ExecutionMetric execMetric)
                    {
                        metricsData.ExecutionMetrics.Add(execMetric);
                        // Keep only last 100 execution metrics
                        if (metricsData.ExecutionMetrics.Count > 100)
                            metricsData.ExecutionMetrics.RemoveAt(0);
                    }

                    // Save metrics
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    var jsonOutput = JsonSerializer.Serialize(metricsData, options);
                    
                    // Ensure directory exists
                    var directory = Path.GetDirectoryName(MetricsFilePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                        Directory.CreateDirectory(directory);

                    File.WriteAllText(MetricsFilePath, jsonOutput);
                }
            }
            catch (Exception ex)
            {
                // Silently fail - metrics are not critical
                Debug.WriteLine($"Failed to save metric: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets build time statistics
        /// </summary>
        public static BuildTimeStats GetBuildTimeStats(string? buildType = null)
        {
            try
            {
                if (!File.Exists(MetricsFilePath))
                    return new BuildTimeStats();

                lock (_lockObject)
                {
                    var json = File.ReadAllText(MetricsFilePath);
                    var metricsData = JsonSerializer.Deserialize<MetricsData>(json);
                    
                    if (metricsData == null || metricsData.BuildMetrics.Count == 0)
                        return new BuildTimeStats();

                    var metrics = buildType != null
                        ? metricsData.BuildMetrics.Where(m => m.BuildType == buildType && m.Success).ToList()
                        : metricsData.BuildMetrics.Where(m => m.Success).ToList();

                    if (metrics.Count == 0)
                        return new BuildTimeStats();

                    return new BuildTimeStats
                    {
                        Count = metrics.Count,
                        AverageMs = metrics.Average(m => m.BuildTimeMs),
                        MinMs = metrics.Min(m => m.BuildTimeMs),
                        MaxMs = metrics.Max(m => m.BuildTimeMs),
                        LatestMs = metrics.Last().BuildTimeMs
                    };
                }
            }
            catch
            {
                return new BuildTimeStats();
            }
        }

        /// <summary>
        /// Gets execution time statistics
        /// </summary>
        public static ExecutionTimeStats GetExecutionTimeStats(string? mode = null)
        {
            try
            {
                if (!File.Exists(MetricsFilePath))
                    return new ExecutionTimeStats();

                lock (_lockObject)
                {
                    var json = File.ReadAllText(MetricsFilePath);
                    var metricsData = JsonSerializer.Deserialize<MetricsData>(json);
                    
                    if (metricsData == null || metricsData.ExecutionMetrics.Count == 0)
                        return new ExecutionTimeStats();

                    var metrics = mode != null
                        ? metricsData.ExecutionMetrics.Where(m => m.Mode == mode).ToList()
                        : metricsData.ExecutionMetrics;

                    if (metrics.Count == 0)
                        return new ExecutionTimeStats();

                    return new ExecutionTimeStats
                    {
                        Count = metrics.Count,
                        AverageMs = metrics.Average(m => m.ExecutionTimeMs),
                        MinMs = metrics.Min(m => m.ExecutionTimeMs),
                        MaxMs = metrics.Max(m => m.ExecutionTimeMs),
                        LatestMs = metrics.Last().ExecutionTimeMs
                    };
                }
            }
            catch
            {
                return new ExecutionTimeStats();
            }
        }

        /// <summary>
        /// Prints a summary of build and execution metrics
        /// </summary>
        public static void PrintMetricsSummary()
        {
            Console.WriteLine("\n═══════════════════════════════════════════════════════");
            Console.WriteLine("  BUILD & EXECUTION METRICS SUMMARY");
            Console.WriteLine("═══════════════════════════════════════════════════════\n");

            var buildStats = GetBuildTimeStats();
            if (buildStats.Count > 0)
            {
                Console.WriteLine("Build Time Statistics:");
                Console.WriteLine($"  Total Builds: {buildStats.Count}");
                Console.WriteLine($"  Average: {buildStats.AverageMs / 1000.0:F2}s ({buildStats.AverageMs}ms)");
                Console.WriteLine($"  Min: {buildStats.MinMs / 1000.0:F2}s ({buildStats.MinMs}ms)");
                Console.WriteLine($"  Max: {buildStats.MaxMs / 1000.0:F2}s ({buildStats.MaxMs}ms)");
                Console.WriteLine($"  Latest: {buildStats.LatestMs / 1000.0:F2}s ({buildStats.LatestMs}ms)");
                Console.WriteLine();
            }

            var execStats = GetExecutionTimeStats();
            if (execStats.Count > 0)
            {
                Console.WriteLine("Execution Time Statistics:");
                Console.WriteLine($"  Total Executions: {execStats.Count}");
                Console.WriteLine($"  Average: {execStats.AverageMs / 1000.0:F2}s ({execStats.AverageMs}ms)");
                Console.WriteLine($"  Min: {execStats.MinMs / 1000.0:F2}s ({execStats.MinMs}ms)");
                Console.WriteLine($"  Max: {execStats.MaxMs / 1000.0:F2}s ({execStats.MaxMs}ms)");
                Console.WriteLine($"  Latest: {execStats.LatestMs / 1000.0:F2}s ({execStats.LatestMs}ms)");
                Console.WriteLine();
            }

            Console.WriteLine("═══════════════════════════════════════════════════════\n");
        }

        #region Data Classes

        private class MetricsData
        {
            public List<BuildMetric> BuildMetrics { get; set; } = new List<BuildMetric>();
            public List<ExecutionMetric> ExecutionMetrics { get; set; } = new List<ExecutionMetric>();
        }

        private class BuildMetric
        {
            public DateTime Timestamp { get; set; }
            public string BuildType { get; set; } = "";
            public long BuildTimeMs { get; set; }
            public double BuildTimeSeconds { get; set; }
            public bool Success { get; set; }
        }

        private class ExecutionMetric
        {
            public DateTime Timestamp { get; set; }
            public string Mode { get; set; } = "";
            public long ExecutionTimeMs { get; set; }
            public double ExecutionTimeSeconds { get; set; }
        }

        public class BuildTimeStats
        {
            public int Count { get; set; }
            public double AverageMs { get; set; }
            public long MinMs { get; set; }
            public long MaxMs { get; set; }
            public long LatestMs { get; set; }
        }

        public class ExecutionTimeStats
        {
            public int Count { get; set; }
            public double AverageMs { get; set; }
            public long MinMs { get; set; }
            public long MaxMs { get; set; }
            public long LatestMs { get; set; }
        }

        #endregion
    }
}

