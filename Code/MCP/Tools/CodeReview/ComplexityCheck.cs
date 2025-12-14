using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RPGGame.MCP.Tools.CodeReview
{
    /// <summary>
    /// Checks for code complexity issues
    /// </summary>
    public class ComplexityCheck : ICodeReviewCheck
    {
        public List<string> Check(string content, string[] lines)
        {
            var issues = new List<string>();
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
                        issues.Add($"Deep nesting detected: {currentMethod}");
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
                        issues.Add($"Method has {paramCount} parameters (max 5 recommended)");
                    }
                }
            }
            
            return issues;
        }
    }
}

