using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

            // Check for style violations
            CheckStyleViolations(lines, result);
            issueCount += result.StyleIssues.Count;

            // Check for complexity
            CheckComplexity(lines, result);
            issueCount += result.ComplexityIssues.Count;

            // Check for security concerns
            CheckSecurity(content, result);
            issueCount += result.SecurityConcerns.Count;

            // Check for performance issues
            CheckPerformance(content, result);
            issueCount += result.PerformanceIssues.Count;

            // Check for documentation
            CheckDocumentation(lines, result);
            issueCount += result.DocumentationGaps.Count;

            // Check for best practices
            CheckBestPractices(content, result);
            issueCount += result.BestPracticeViolations.Count;

            // Calculate quality score
            result.QualityScore = Math.Max(0, 100 - (issueCount * 5));
            GenerateRecommendations(result);

            return result;
        }

        private static void CheckStyleViolations(string[] lines, ReviewResult result)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // Check for trailing whitespace
                if (line.Length > 0 && char.IsWhiteSpace(line[line.Length - 1]))
                {
                    result.StyleIssues.Add($"Line {i + 1}: Trailing whitespace");
                }

                // Check for inconsistent indentation
                if (line.Length > 0 && !line.StartsWith("//"))
                {
                    var leadingSpaces = line.TakeWhile(c => c == ' ').Count();
                    if (leadingSpaces % 4 != 0 && leadingSpaces > 0)
                    {
                        result.StyleIssues.Add($"Line {i + 1}: Inconsistent indentation (use 4 spaces)");
                    }
                }

                // Check for long lines
                if (line.Length > 120)
                {
                    result.StyleIssues.Add($"Line {i + 1}: Line too long ({line.Length} chars, max 120)");
                }
            }
        }

        private static void CheckComplexity(string[] lines, ReviewResult result)
        {
            var methodPattern = new Regex(@"^\s*(public|private|protected)?\s+\w+\s+\w+\s*\(");
            var inMethod = false;
            var braceCount = 0;
            var currentMethod = "";

            foreach (var line in lines)
            {
                if (methodPattern.IsMatch(line))
                {
                    inMethod = true;
                    currentMethod = line.Trim();
                    braceCount = 0;
                }

                if (inMethod)
                {
                    braceCount += line.Count(c => c == '{');
                    braceCount -= line.Count(c => c == '}');

                    // Check for deeply nested code (4+ levels)
                    var indentLevel = line.TakeWhile(c => c == ' ').Count() / 4;
                    if (indentLevel > 4)
                    {
                        result.ComplexityIssues.Add($"Deep nesting detected: {currentMethod}");
                    }

                    if (braceCount == 0 && inMethod)
                    {
                        inMethod = false;
                    }
                }
            }

            // Check for methods with too many parameters
            var paramPattern = new Regex(@"\w+\s+\w+\s*\(([^)]*)\)");
            foreach (var line in lines)
            {
                var match = paramPattern.Match(line);
                if (match.Success)
                {
                    var paramCount = match.Groups[1].Value.Split(',').Length;
                    if (paramCount > 5)
                    {
                        result.ComplexityIssues.Add($"Method has {paramCount} parameters (max 5 recommended)");
                    }
                }
            }
        }

        private static void CheckSecurity(string content, ReviewResult result)
        {
            // Check for SQL injection vulnerability
            if (content.Contains("SELECT") && content.Contains("string.Format"))
            {
                result.SecurityConcerns.Add("Potential SQL injection: Using string.Format with SQL queries");
            }

            // Check for hardcoded passwords
            if (Regex.IsMatch(content, @"password\s*=\s*[""']", RegexOptions.IgnoreCase))
            {
                result.SecurityConcerns.Add("Hardcoded password or credential found");
            }

            // Check for use of deprecated security methods
            if (content.Contains("MD5") || content.Contains("SHA1"))
            {
                result.SecurityConcerns.Add("Deprecated cryptographic algorithm used");
            }

            // Check for eval/reflection abuse
            if (content.Contains("Reflection.Invoke") || content.Contains("Expression.Compile"))
            {
                result.SecurityConcerns.Add("Dynamic code execution detected - verify necessity");
            }
        }

        private static void CheckPerformance(string content, ReviewResult result)
        {
            // Check for string concatenation in loops
            if (content.Contains("+=") && content.Contains("for "))
            {
                result.PerformanceIssues.Add("String concatenation in loop detected - use StringBuilder");
            }

            // Check for inefficient LINQ
            var whereCount = Regex.Matches(content, @"\.Where\(").Count;
            if (whereCount > 1)
            {
                result.PerformanceIssues.Add("Multiple LINQ Where clauses - consider combining");
            }

            // Check for missing null checks before operations
            if (Regex.IsMatch(content, @"\.\w+\(.*\)", RegexOptions.IgnoreCase) &&
                !content.Contains("?"))
            {
                result.PerformanceIssues.Add("Consider using null-coalescing operators");
            }

            // Check for LINQ on large collections
            if (content.Contains(".ToList()") && content.Contains(".Where("))
            {
                result.PerformanceIssues.Add("ToList() before filtering - evaluate before materializing");
            }
        }

        private static void CheckDocumentation(string[] lines, ReviewResult result)
        {
            var undocumentedMethods = 0;
            var undocumentedClasses = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // Check for public methods without documentation
                if ((line.Contains("public ") || line.Contains("public async ")) &&
                    line.Contains("(") &&
                    (i == 0 || !lines[i - 1].Contains("///")))
                {
                    undocumentedMethods++;
                }

                // Check for public classes without documentation
                if (line.Contains("public class ") && (i == 0 || !lines[i - 1].Contains("///")))
                {
                    undocumentedClasses++;
                }
            }

            if (undocumentedMethods > 0)
            {
                result.DocumentationGaps.Add($"{undocumentedMethods} public methods lack XML documentation");
            }

            if (undocumentedClasses > 0)
            {
                result.DocumentationGaps.Add($"{undocumentedClasses} public classes lack XML documentation");
            }
        }

        private static void CheckBestPractices(string content, ReviewResult result)
        {
            // Check for magic numbers
            if (Regex.IsMatch(content, @"[^=]= \d{3,}"))
            {
                result.BestPracticeViolations.Add("Magic numbers detected - use named constants");
            }

            // Check for catching generic Exception
            if (content.Contains("catch (Exception"))
            {
                result.BestPracticeViolations.Add("Catching generic Exception - catch specific exceptions");
            }

            // Check for empty catch blocks
            if (Regex.IsMatch(content, @"catch.*\{\s*\}", RegexOptions.Singleline))
            {
                result.BestPracticeViolations.Add("Empty catch blocks detected - add logging or handling");
            }

            // Check for TODO comments
            if (content.Contains("TODO") || content.Contains("FIXME"))
            {
                result.BestPracticeViolations.Add("Unresolved TODO/FIXME comments found");
            }

            // Check for commented-out code
            var commentedLines = Regex.Matches(content, @"^\s*//\s*\w+").Count;
            if (commentedLines > 5)
            {
                result.BestPracticeViolations.Add($"Multiple commented-out code lines ({commentedLines}) - remove or document");
            }
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
