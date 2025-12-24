using System.Linq;
using System.Text;

namespace RPGGame.MCP.Tools.PerformanceProfiling
{
    /// <summary>
    /// Formats performance reports into readable text output
    /// Extracted from PerformanceProfilerAgent to separate formatting logic
    /// </summary>
    public static class PerformanceReportFormatter
    {
        public static string FormatReport(PerformanceReport report)
        {
            var output = new StringBuilder();

            output.AppendLine("ANALYSIS RESULTS:\n");

            output.AppendLine($"Component: {report.ComponentName}");
            output.AppendLine($"Total Execution Time: {report.TotalExecutionTimeMs}ms");
            output.AppendLine($"Performance Score: {report.PerformanceScore:F1}/100\n");

            if (report.TimingBreakdown.Count > 0)
            {
                output.AppendLine("TIMING BREAKDOWN:");
                foreach (var (path, timeMs, percentage) in report.TimingBreakdown.OrderByDescending(x => x.TimeMs))
                {
                    output.AppendLine($"  • {path,-40} {timeMs,6}ms ({percentage,5:F1}%)");
                }
                output.AppendLine();
            }

            if (report.HotPaths.Count > 0)
            {
                output.AppendLine("HOT PATHS (Most Time-Consuming):");
                foreach (var path in report.HotPaths)
                {
                    output.AppendLine($"  • {path}");
                }
                output.AppendLine();
            }

            if (report.BottlenecksIdentified.Count > 0)
            {
                output.AppendLine("IDENTIFIED BOTTLENECKS:");
                foreach (var bottleneck in report.BottlenecksIdentified)
                {
                    output.AppendLine($"  ⚠ {bottleneck}");
                }
                output.AppendLine();
            }

            if (report.MemoryIssues.Count > 0)
            {
                output.AppendLine("MEMORY CONCERNS:");
                foreach (var issue in report.MemoryIssues)
                {
                    output.AppendLine($"  ⚠ {issue}");
                }
                output.AppendLine();
            }

            if (report.OptimizationSuggestions.Count > 0)
            {
                output.AppendLine("OPTIMIZATION SUGGESTIONS:");
                int priority = 1;
                foreach (var suggestion in report.OptimizationSuggestions)
                {
                    output.AppendLine($"  {priority}. {suggestion}");
                    priority++;
                }
                output.AppendLine();
            }

            output.AppendLine("╔════════════════════════════════════════════════════════╗");
            if (report.PerformanceScore >= 85)
                output.AppendLine("║     Excellent performance - Monitor for regressions    ║");
            else if (report.PerformanceScore >= 75)
                output.AppendLine("║     Good performance - Some optimization opportunities ║");
            else if (report.PerformanceScore >= 65)
                output.AppendLine("║     Fair performance - Optimization recommended        ║");
            else
                output.AppendLine("║     Poor performance - Optimization needed urgently    ║");
            output.AppendLine("╚════════════════════════════════════════════════════════╝");

            return output.ToString();
        }
    }
}

