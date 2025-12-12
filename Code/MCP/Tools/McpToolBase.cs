using System;
using System.Text.Json;
using System.Threading.Tasks;
using RPGGame.MCP;

namespace RPGGame.MCP.Tools
{
    /// <summary>
    /// Base class for MCP tools with common execution pattern
    /// </summary>
    public abstract class McpToolBase : IMcpTool
    {
        protected static GameWrapper? GetGameWrapper()
        {
            return McpToolState.GameWrapper;
        }

        protected static void SetLastTestResult(BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult? result)
        {
            McpToolState.LastTestResult = result;
        }

        protected static BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult? GetLastTestResult()
        {
            return McpToolState.LastTestResult;
        }

        protected static BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult? GetBaselineTestResult()
        {
            return McpToolState.BaselineTestResult;
        }

        protected static void SetBaselineTestResult(BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult? result)
        {
            McpToolState.BaselineTestResult = result;
        }

        protected static RPGGame.Editors.VariableEditor GetVariableEditor()
        {
            return McpToolState.GetVariableEditor();
        }

        /// <summary>
        /// Executes the tool with error handling
        /// </summary>
        public async Task<string> ExecuteAsync()
        {
            try
            {
                var result = await ExecuteCoreAsync();
                return SerializeResult(result);
            }
            catch (Exception ex)
            {
                return SerializeError(ex.Message, ex.StackTrace);
            }
        }

        /// <summary>
        /// Core execution logic - override in derived classes
        /// </summary>
        protected abstract Task<object> ExecuteCoreAsync();

        /// <summary>
        /// Serializes result to JSON
        /// </summary>
        protected virtual string SerializeResult(object result, bool writeIndented = false)
        {
            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = writeIndented });
        }

        /// <summary>
        /// Serializes error to JSON
        /// </summary>
        protected virtual string SerializeError(string error, string? stackTrace = null)
        {
            var errorObj = new { error = error };
            if (stackTrace != null)
            {
                return JsonSerializer.Serialize(new { error = error, stackTrace = stackTrace });
            }
            return JsonSerializer.Serialize(errorObj);
        }

        /// <summary>
        /// Validates that game wrapper is initialized
        /// </summary>
        protected void ValidateGameWrapper()
        {
            if (GetGameWrapper() == null)
                throw new InvalidOperationException("Game wrapper not initialized");
        }
    }
}
