using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace RPGGame.MCP.Tools
{
    /// <summary>
    /// Executes MCP tools with standardized error handling and serialization
    /// </summary>
    public static class McpToolExecutor
    {
        /// <summary>
        /// Executes a tool function with error handling
        /// </summary>
        public static async Task<string> ExecuteAsync(Func<Task<object>> toolFunction, bool writeIndented = false)
        {
            try
            {
                var result = await toolFunction();
                return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = writeIndented });
            }
            catch (Exception ex)
            {
                return SerializeError(ex.Message, ex.StackTrace);
            }
        }

        /// <summary>
        /// Executes a synchronous tool function with error handling
        /// </summary>
        public static Task<string> ExecuteAsync(Func<object> toolFunction, bool writeIndented = false)
        {
            try
            {
                var result = toolFunction();
                return Task.FromResult(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = writeIndented }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(SerializeError(ex.Message, ex.StackTrace));
            }
        }

        /// <summary>
        /// Serializes error to JSON
        /// </summary>
        private static string SerializeError(string error, string? stackTrace = null)
        {
            if (stackTrace != null)
            {
                return JsonSerializer.Serialize(new { error = error, stackTrace = stackTrace });
            }
            return JsonSerializer.Serialize(new { error = error });
        }
    }
}
