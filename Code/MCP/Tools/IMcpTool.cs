using System.Threading.Tasks;

namespace RPGGame.MCP.Tools
{
    /// <summary>
    /// Interface for MCP tools that can be executed
    /// </summary>
    public interface IMcpTool
    {
        /// <summary>
        /// Executes the tool and returns JSON result
        /// </summary>
        Task<string> ExecuteAsync();
    }
}
