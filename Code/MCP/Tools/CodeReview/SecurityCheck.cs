using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RPGGame.MCP.Tools.CodeReview
{
    /// <summary>
    /// Checks for security concerns in code
    /// </summary>
    public class SecurityCheck : ICodeReviewCheck
    {
        public List<string> Check(string content, string[] lines)
        {
            var issues = new List<string>();
            
            // Check for SQL injection vulnerability
            if (content.Contains("SELECT") && content.Contains("string.Format"))
            {
                issues.Add("Potential SQL injection: Using string.Format with SQL queries");
            }

            // Check for hardcoded passwords
            if (Regex.IsMatch(content, @"password\s*=\s*[""']", RegexOptions.IgnoreCase))
            {
                issues.Add("Hardcoded password or credential found");
            }

            // Check for use of deprecated security methods
            if (content.Contains("MD5") || content.Contains("SHA1"))
            {
                issues.Add("Deprecated cryptographic algorithm used");
            }

            // Check for eval/reflection abuse
            if (content.Contains("Reflection.Invoke") || content.Contains("Expression.Compile"))
            {
                issues.Add("Dynamic code execution detected - verify necessity");
            }
            
            return issues;
        }
    }
}

