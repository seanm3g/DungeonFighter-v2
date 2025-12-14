using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RPGGame.MCP.Tools.CodeReview
{
    /// <summary>
    /// Checks for performance issues in code
    /// </summary>
    public class PerformanceCheck : ICodeReviewCheck
    {
        public List<string> Check(string content, string[] lines)
        {
            var issues = new List<string>();
            
            // Check for string concatenation in loops
            if (content.Contains("+=") && content.Contains("for "))
            {
                issues.Add("String concatenation in loop detected - use StringBuilder");
            }

            // Check for inefficient LINQ
            var whereCount = Regex.Matches(content, @"\.Where\(").Count;
            if (whereCount > 1)
            {
                issues.Add("Multiple LINQ Where clauses - consider combining");
            }

            // Check for missing null checks before operations
            if (Regex.IsMatch(content, @"\.\w+\(.*\)", RegexOptions.IgnoreCase) &&
                !content.Contains("?"))
            {
                issues.Add("Consider using null-coalescing operators");
            }

            // Check for LINQ on large collections
            if (content.Contains(".ToList()") && content.Contains(".Where("))
            {
                issues.Add("ToList() before filtering - evaluate before materializing");
            }
            
            return issues;
        }
    }
}

