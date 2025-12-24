using System;
using System.Text;
using System.Threading.Tasks;
using RPGGame.MCP.Tools.PerformanceProfiling;

namespace RPGGame.MCP.Tools
{
    /// <summary>
    /// Facade for MCP Tools for performance profiling and analysis
    /// Provides 4 tools for performance analysis: ProfileSystem, ComparePerformance, IdentifyBottlenecks, BenchmarkCriticalPaths
    /// 
    /// Refactored from 473 lines to ~100 lines using Facade pattern.
    /// Delegates to:
    /// - PerformanceReportGenerator: Generates performance reports for components
    /// - PerformanceReportFormatter: Formats performance reports into readable text
    /// - PerformanceBenchmarker: Performs benchmarking and comparison operations
    /// </summary>
    public static class PerformanceProfilerAgent
    {
        // Type alias for backward compatibility
        public class PerformanceReport : PerformanceProfiling.PerformanceReport { }

        public static Task<string> ProfileSystem(string component)
        {
            var output = new StringBuilder();
            output.AppendLine("╔════════════════════════════════════════════════════════╗");
            output.AppendLine("║     PERFORMANCE PROFILER AGENT - System Analysis       ║");
            output.AppendLine("╚════════════════════════════════════════════════════════╝\n");

            try
            {
                output.AppendLine($"Profiling Component: {component}\n");
                output.AppendLine("Running performance benchmarks...\n");

                var report = PerformanceReportGenerator.GenerateReport(component);
                output.Append(PerformanceReportFormatter.FormatReport(report));

                return Task.FromResult(output.ToString());
            }
            catch (Exception ex)
            {
                output.AppendLine($"✗ Error profiling system: {ex.Message}");
                return Task.FromResult(output.ToString());
            }
        }

        public static Task<string> ComparePerformance(string baseline)
        {
            var output = new StringBuilder();
            output.AppendLine("╔════════════════════════════════════════════════════════╗");
            output.AppendLine("║     PERFORMANCE PROFILER AGENT - Baseline Comparison   ║");
            output.AppendLine("╚════════════════════════════════════════════════════════╝\n");

            try
            {
                output.AppendLine($"Baseline: {baseline}\n");
                output.AppendLine("Running comparison benchmarks...\n");

                var comparison = PerformanceBenchmarker.CompareWithBaseline(baseline);
                output.Append(comparison);

                return Task.FromResult(output.ToString());
            }
            catch (Exception ex)
            {
                output.AppendLine($"✗ Error comparing performance: {ex.Message}");
                return Task.FromResult(output.ToString());
            }
        }

        public static Task<string> IdentifyBottlenecks()
        {
            var output = new StringBuilder();
            output.AppendLine("╔════════════════════════════════════════════════════════╗");
            output.AppendLine("║     PERFORMANCE PROFILER AGENT - Bottleneck Analysis   ║");
            output.AppendLine("╚════════════════════════════════════════════════════════╝\n");

            try
            {
                output.AppendLine("Analyzing all systems for performance bottlenecks...\n");

                var bottlenecks = PerformanceBenchmarker.FindPerformanceBottlenecks();
                output.Append(bottlenecks);

                return Task.FromResult(output.ToString());
            }
            catch (Exception ex)
            {
                output.AppendLine($"✗ Error identifying bottlenecks: {ex.Message}");
                return Task.FromResult(output.ToString());
            }
        }

        public static Task<string> BenchmarkCriticalPaths()
        {
            var output = new StringBuilder();
            output.AppendLine("╔════════════════════════════════════════════════════════╗");
            output.AppendLine("║     PERFORMANCE PROFILER AGENT - Critical Path Analysis║");
            output.AppendLine("╚════════════════════════════════════════════════════════╝\n");

            try
            {
                output.AppendLine("Benchmarking critical code paths...\n");

                var benchmarks = PerformanceBenchmarker.BenchmarkCriticalCodePaths();
                output.Append(benchmarks);

                return Task.FromResult(output.ToString());
            }
            catch (Exception ex)
            {
                output.AppendLine($"✗ Error benchmarking: {ex.Message}");
                return Task.FromResult(output.ToString());
            }
        }
    }
}
