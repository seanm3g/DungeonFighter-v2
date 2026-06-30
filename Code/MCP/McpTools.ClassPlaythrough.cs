using System.ComponentModel;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Tools = RPGGame.MCP.Tools;

namespace RPGGame.MCP
{
    /// <summary>
    /// MCP tools for four-class playthrough batch balance verification.
    /// </summary>
    public static partial class McpTools
    {
        [McpServerTool(Name = "run_class_playthrough_batch", Title = "Run Class Playthrough Batch")]
        [Description("Runs headless full-game playthroughs until Death for each class. Aggregates per-class metrics.")]
        public static Task<string> RunClassPlaythroughBatch(
            [Description("Number of playthrough runs per class (default: 5)")] int runsPerClass = 5,
            [Description("Optional comma-separated classes or weapon paths")] string? classes = null,
            [Description("Maximum actions per run (default: 500)")] int maxActionsPerRun = 500)
        {
            return Tools.ClassPlaythroughTools.RunClassPlaythroughBatch(runsPerClass, classes, maxActionsPerRun);
        }

        [McpServerTool(Name = "get_class_playthrough_report", Title = "Get Class Playthrough Report")]
        [Description("Formatted report from the most recent class playthrough batch.")]
        public static Task<string> GetClassPlaythroughReport()
        {
            return Tools.ClassPlaythroughTools.GetClassPlaythroughReport();
        }
    }
}
