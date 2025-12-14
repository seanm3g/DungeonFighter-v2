using System.Collections.Generic;

namespace RPGGame.MCP.Tools.CodeReview
{
    /// <summary>
    /// Checks for documentation gaps in code
    /// </summary>
    public class DocumentationCheck : ICodeReviewCheck
    {
        public List<string> Check(string content, string[] lines)
        {
            var issues = new List<string>();
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
                issues.Add($"{undocumentedMethods} public methods lack XML documentation");
            }

            if (undocumentedClasses > 0)
            {
                issues.Add($"{undocumentedClasses} public classes lack XML documentation");
            }
            
            return issues;
        }
    }
}

