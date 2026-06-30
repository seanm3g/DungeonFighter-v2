using System.ComponentModel;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Tools = RPGGame.MCP.Tools;

namespace RPGGame.MCP
{
    public static partial class McpTools
    {
        [McpServerTool(Name = "run_playthrough_tune_sim", Title = "Run Playthrough Tune Sim")]
        [Description("TUNESIM for class-playthrough-balance profile — batch playthroughs across all four classes.")]
        public static Task<string> RunPlaythroughTuneSim(
            [Description("Profile id (default: class-playthrough-balance)")] string profileId = "class-playthrough-balance",
            [Description("Runs per class (default: 10)")] int runsPerClass = 10,
            [Description("Max actions per run (default: 500)")] int maxActionsPerRun = 500,
            [Description("Optional comma-separated class filter")] string? classes = null)
        {
            return Tools.PlaythroughTuningTools.RunPlaythroughTuneSim(profileId, runsPerClass, maxActionsPerRun, classes);
        }

        [McpServerTool(Name = "run_playthrough_tune_analyze", Title = "Run Playthrough Tune Analyze")]
        [Description("TUNEANALYZE on the last playthrough batch session — progression + parity validators, one knob suggestion.")]
        public static Task<string> RunPlaythroughTuneAnalyze()
        {
            return Tools.PlaythroughTuningTools.RunPlaythroughTuneAnalyze();
        }

        [McpServerTool(Name = "run_playthrough_tune_apply", Title = "Run Playthrough Tune Apply")]
        [Description("TUNEAPPLY — apply the top playthrough tuning suggestion from the session.")]
        public static Task<string> RunPlaythroughTuneApply(
            [Description("Dry run only (default: false)")] bool dryRun = false)
        {
            return Tools.PlaythroughTuningTools.RunPlaythroughTuneApply(dryRun);
        }

        [McpServerTool(Name = "run_playthrough_tune_loop", Title = "Run Playthrough Tune Loop")]
        [Description("Full loop: sim → analyze → apply until pass or max iterations (class-playthrough-balance profile).")]
        public static Task<string> RunPlaythroughTuneLoop(
            [Description("Max iterations (default: 8)")] int maxIterations = 8,
            [Description("Runs per class per iteration (default: 10)")] int runsPerClass = 10,
            [Description("Dry run — analyze only, no apply (default: false)")] bool dryRun = false)
        {
            return Tools.PlaythroughTuningTools.RunPlaythroughTuneLoop(maxIterations, runsPerClass, dryRun);
        }
    }
}
