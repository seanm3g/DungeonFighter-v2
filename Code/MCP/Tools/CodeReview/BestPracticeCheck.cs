using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RPGGame.MCP.Tools.CodeReview
{
    /// <summary>
    /// Checks for best practice violations in code
    /// </summary>
    public class BestPracticeCheck : ICodeReviewCheck
    {
        public List<string> Check(string content, string[] lines)
        {
            var issues = new List<string>();
            
            // Check for magic numbers
            if (Regex.IsMatch(content, @"[^=]= \d{3,}"))
            {
                issues.Add("Magic numbers detected - use named constants");
            }

            // Check for catching generic Exception
            if (content.Contains("catch (Exception"))
            {
                issues.Add("Catching generic Exception - catch specific exceptions");
            }

            // Check for empty catch blocks
            if (Regex.IsMatch(content, @"catch.*\{\s*\}", RegexOptions.Singleline))
            {
                issues.Add("Empty catch blocks detected - add logging or handling");
            }

            // Check for TODO comments
            if (content.Contains("TODO") || content.Contains("FIXME"))
            {
                issues.Add("Unresolved TODO/FIXME comments found");
            }

            // Check for commented-out code
            var commentedLines = Regex.Matches(content, @"^\s*//\s*\w+").Count;
            if (commentedLines > 5)
            {
                issues.Add($"Multiple commented-out code lines ({commentedLines}) - remove or document");
            }
            
            return issues;
        }
    }
}

