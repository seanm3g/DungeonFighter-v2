using System.Collections.Generic;
using System.Linq;

namespace RPGGame.MCP.Tools.CodeReview
{
    /// <summary>
    /// Checks for style violations in code
    /// </summary>
    public class StyleViolationCheck : ICodeReviewCheck
    {
        public List<string> Check(string content, string[] lines)
        {
            var issues = new List<string>();
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // Check for trailing whitespace
                if (line.Length > 0 && char.IsWhiteSpace(line[line.Length - 1]))
                {
                    issues.Add($"Line {i + 1}: Trailing whitespace");
                }

                // Check for inconsistent indentation
                if (line.Length > 0 && !line.StartsWith("//"))
                {
                    var leadingSpaces = line.TakeWhile(c => c == ' ').Count();
                    if (leadingSpaces % 4 != 0 && leadingSpaces > 0)
                    {
                        issues.Add($"Line {i + 1}: Inconsistent indentation (use 4 spaces)");
                    }
                }

                // Check for long lines
                if (line.Length > 120)
                {
                    issues.Add($"Line {i + 1}: Line too long ({line.Length} chars, max 120)");
                }
            }
            
            return issues;
        }
    }
}

