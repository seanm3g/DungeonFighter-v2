using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RPGGame.MCP.Tools.CodeReview;

namespace RPGGame.MCP.Tools
{
    /// <summary>
    /// Code Review Agent - Automated code quality analysis and peer review
    /// Analyzes C# code for style violations, complexity, security issues, and best practices
    /// </summary>
    public class CodeReviewAgent
    {
        public class ReviewResult
        {
            public string FilePath { get; set; } = string.Empty;
            public double QualityScore { get; set; } // 0-100
            public List<string> StyleIssues { get; set; } = new();
            public List<string> ComplexityIssues { get; set; } = new();
            public List<string> SecurityConcerns { get; set; } = new();
            public List<string> PerformanceIssues { get; set; } = new();
            public List<string> DocumentationGaps { get; set; } = new();
            public List<string> BestPracticeViolations { get; set; } = new();
            public List<string> Recommendations { get; set; } = new();
        }

        public static async Task<string> ReviewFile(string filePath)
        {
            var output = new StringBuilder();
            output.AppendLine("╔════════════════════════════════════════════════════════╗");
            output.AppendLine("║     CODE REVIEW AGENT - File Analysis                 ║");
            output.AppendLine("╚════════════════════════════════════════════════════════╝\n");

            try
            {
                if (!File.Exists(filePath))
                {
                    output.AppendLine($"✗ File not found: {filePath}");
                    return output.ToString();
                }

                var content = await File.ReadAllTextAsync(filePath);
                var result = AnalyzeCode(content, filePath);

                output.Append(FormatReview(result));

                return output.ToString();
            }
            catch (Exception ex)
            {
                output.AppendLine($"✗ Error reviewing file: {ex.Message}");
                return output.ToString();
            }
        }

        public static Task<string> ReviewDiff()
        {
            var output = new StringBuilder();
            output.AppendLine("╔════════════════════════════════════════════════════════╗");
            output.AppendLine("║     CODE REVIEW AGENT - Uncommitted Changes           ║");
            output.AppendLine("╚════════════════════════════════════════════════════════╝\n");

            try
            {
                output.AppendLine("Analyzing uncommitted changes...\n");
                output.AppendLine("📋 This would analyze git diff output");
                output.AppendLine("   (Requires git integration - not yet available)\n");
                output.AppendLine("For now, use: /review file [path]");

                return Task.FromResult(output.ToString());
            }
            catch (Exception ex)
            {
                output.AppendLine($"✗ Error reviewing diff: {ex.Message}");
                return Task.FromResult(output.ToString());
            }
        }

        public static Task<string> ReviewPullRequest()
        {
            var output = new StringBuilder();
            output.AppendLine("╔════════════════════════════════════════════════════════╗");
            output.AppendLine("║     CODE REVIEW AGENT - Pull Request Review           ║");
            output.AppendLine("╚════════════════════════════════════════════════════════╝\n");

            try
            {
                output.AppendLine("Analyzing PR changes...\n");
                output.AppendLine("📋 This would review branch vs main");
                output.AppendLine("   (Requires git integration - not yet available)\n");

                return Task.FromResult(output.ToString());
            }
            catch (Exception ex)
            {
                output.AppendLine($"✗ Error reviewing PR: {ex.Message}");
                return Task.FromResult(output.ToString());
            }
        }

        private static ReviewResult AnalyzeCode(string content, string filePath)
        {
            var result = new ReviewResult { FilePath = filePath };
            var lines = content.Split('\n');
            var issueCount = 0;

            // Run all checks
            var styleCheck = new StyleViolationCheck();
            result.StyleIssues = styleCheck.Check(content, lines);
            issueCount += result.StyleIssues.Count;

            var complexityCheck = new ComplexityCheck();
            result.ComplexityIssues = complexityCheck.Check(content, lines);
            issueCount += result.ComplexityIssues.Count;

            var securityCheck = new SecurityCheck();
            result.SecurityConcerns = securityCheck.Check(content, lines);
            issueCount += result.SecurityConcerns.Count;

            var performanceCheck = new PerformanceCheck();
            result.PerformanceIssues = performanceCheck.Check(content, lines);
            issueCount += result.PerformanceIssues.Count;

            var documentationCheck = new DocumentationCheck();
            result.DocumentationGaps = documentationCheck.Check(content, lines);
            issueCount += result.DocumentationGaps.Count;

            var bestPracticeCheck = new BestPracticeCheck();
            result.BestPracticeViolations = bestPracticeCheck.Check(content, lines);
            issueCount += result.BestPracticeViolations.Count;

            // Calculate quality score
            result.QualityScore = Math.Max(0, 100 - (issueCount * 5));
            GenerateRecommendations(result);

            return result;
        }

        private static void GenerateRecommendations(ReviewResult result)
        {
            if (result.StyleIssues.Count > 0)
            {
                result.Recommendations.Add("✓ Fix style violations for consistency");
            }

            if (result.ComplexityIssues.Count > 0)
            {
                result.Recommendations.Add("✓ Reduce complexity - consider extracting methods");
            }

            if (result.SecurityConcerns.Count > 0)
            {
                result.Recommendations.Add("✓ Address security concerns before merge");
            }

            if (result.PerformanceIssues.Count > 0)
            {
                result.Recommendations.Add("✓ Optimize performance-critical sections");
            }

            if (result.DocumentationGaps.Count > 0)
            {
                result.Recommendations.Add("✓ Add XML documentation for public APIs");
            }

            if (result.BestPracticeViolations.Count > 0)
            {
                result.Recommendations.Add("✓ Follow C# best practices and conventions");
            }

            if (result.Recommendations.Count == 0)
            {
                result.Recommendations.Add("✓ Code looks good! Minor improvements possible.");
            }
        }

        private static string FormatReview(ReviewResult result)
        {
            var output = new StringBuilder();

            output.AppendLine($"File: {result.FilePath}");
            output.AppendLine($"Quality Score: {result.QualityScore:F1}/100\n");

            if (result.StyleIssues.Count > 0)
            {
                output.AppendLine("❌ STYLE VIOLATIONS:");
                foreach (var issue in result.StyleIssues.Take(5))
                {
                    output.AppendLine($"   • {issue}");
                }
                if (result.StyleIssues.Count > 5)
                    output.AppendLine($"   ... and {result.StyleIssues.Count - 5} more\n");
                else
                    output.AppendLine();
            }

            if (result.ComplexityIssues.Count > 0)
            {
                output.AppendLine("⚠️  COMPLEXITY ISSUES:");
                foreach (var issue in result.ComplexityIssues.Take(3))
                {
                    output.AppendLine($"   • {issue}");
                }
                output.AppendLine();
            }

            if (result.SecurityConcerns.Count > 0)
            {
                output.AppendLine("🔒 SECURITY CONCERNS:");
                foreach (var issue in result.SecurityConcerns)
                {
                    output.AppendLine($"   • {issue}");
                }
                output.AppendLine();
            }

            if (result.PerformanceIssues.Count > 0)
            {
                output.AppendLine("⚡ PERFORMANCE ISSUES:");
                foreach (var issue in result.PerformanceIssues.Take(3))
                {
                    output.AppendLine($"   • {issue}");
                }
                output.AppendLine();
            }

            if (result.DocumentationGaps.Count > 0)
            {
                output.AppendLine("📚 DOCUMENTATION GAPS:");
                foreach (var issue in result.DocumentationGaps)
                {
                    output.AppendLine($"   • {issue}");
                }
                output.AppendLine();
            }

            if (result.BestPracticeViolations.Count > 0)
            {
                output.AppendLine("💡 BEST PRACTICE VIOLATIONS:");
                foreach (var issue in result.BestPracticeViolations.Take(3))
                {
                    output.AppendLine($"   • {issue}");
                }
                output.AppendLine();
            }

            output.AppendLine("📋 RECOMMENDATIONS:");
            foreach (var rec in result.Recommendations)
            {
                output.AppendLine($"   {rec}");
            }

            output.AppendLine("\n╔════════════════════════════════════════════════════════╗");
            if (result.QualityScore >= 80)
                output.AppendLine("║     ✓ Code is ready for review                        ║");
            else if (result.QualityScore >= 60)
                output.AppendLine("║     ⚠ Address issues before submitting                ║");
            else
                output.AppendLine("║     ✗ Significant improvements needed                 ║");
            output.AppendLine("╚════════════════════════════════════════════════════════╝");

            return output.ToString();
        }
    }
}
