using System.Collections.Generic;

namespace RPGGame.MCP.Tools.PerformanceProfiling
{
    /// <summary>
    /// Data structure for performance reports
    /// Extracted from PerformanceProfilerAgent for better organization
    /// </summary>
    public class PerformanceReport
    {
        public string ComponentName { get; set; } = string.Empty;
        public long TotalExecutionTimeMs { get; set; }
        public List<string> HotPaths { get; set; } = new();
        public List<string> BottlenecksIdentified { get; set; } = new();
        public List<string> MemoryIssues { get; set; } = new();
        public List<string> OptimizationSuggestions { get; set; } = new();
        public double PerformanceScore { get; set; } // 0-100
        public List<(string Path, long TimeMs, double Percentage)> TimingBreakdown { get; set; } = new();
    }
}

