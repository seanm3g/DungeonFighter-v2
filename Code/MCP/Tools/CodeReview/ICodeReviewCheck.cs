using System.Collections.Generic;

namespace RPGGame.MCP.Tools.CodeReview
{
    /// <summary>
    /// Interface for code review checks
    /// </summary>
    public interface ICodeReviewCheck
    {
        /// <summary>
        /// Performs the check and returns a list of issues found
        /// </summary>
        List<string> Check(string content, string[] lines);
    }
}

