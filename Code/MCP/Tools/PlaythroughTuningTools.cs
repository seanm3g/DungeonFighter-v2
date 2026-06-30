using System.Linq;
using System.Threading.Tasks;
using RPGGame.Tuning;
using RPGGame.Tuning.Profiles;

namespace RPGGame.MCP.Tools
{
    public static class PlaythroughTuningTools
    {
        public static Task<string> RunPlaythroughTuneSim(
            string profileId = PlaythroughTuningRunner.DefaultProfileId,
            int runsPerClass = 10,
            int maxActionsPerRun = 500,
            string? classes = null)
        {
            return McpToolExecutor.ExecuteAsync(async () =>
            {
                string[] args =
                {
                    "TUNESIM",
                    "--profile", profileId,
                    "--runs-per-class", runsPerClass.ToString(),
                    "--max-actions-per-run", maxActionsPerRun.ToString()
                };
                if (!string.IsNullOrWhiteSpace(classes))
                {
                    args = args.Concat(new[] { "--classes", classes }).ToArray();
                }

                int code = await BalanceTuningWorkflow.RunSimAsync(profileId, args);
                var session = LevelTuningSessionStore.Load();
                return new
                {
                    exitCode = code,
                    profileId,
                    simulationMode = session.SimulationMode,
                    playthrough = session.PlaythroughBatch,
                    report = session.PlaythroughBatch != null
                        ? ClassPlaythroughBatchRunner.FormatReport(McpToolState.LastPlaythroughBatchResult!)
                        : null
                };
            }, writeIndented: true);
        }

        public static Task<string> RunPlaythroughTuneAnalyze()
        {
            return McpToolExecutor.ExecuteAsync(async () =>
            {
                int code = await BalanceTuningWorkflow.RunAnalyzeAsync();
                var session = LevelTuningSessionStore.Load();
                return new
                {
                    exitCode = code,
                    analysis = session.Analysis,
                    playthrough = session.PlaythroughBatch
                };
            }, writeIndented: true);
        }

        public static Task<string> RunPlaythroughTuneApply(bool dryRun = false)
        {
            return McpToolExecutor.ExecuteAsync(async () =>
            {
                int code = await BalanceTuningWorkflow.RunApplyAsync(dryRun);
                var session = LevelTuningSessionStore.Load();
                return new
                {
                    exitCode = code,
                    dryRun,
                    lastApply = session.LastApply
                };
            }, writeIndented: true);
        }

        public static Task<string> RunPlaythroughTuneLoop(
            int maxIterations = 8,
            int runsPerClass = 10,
            bool dryRun = false)
        {
            return McpToolExecutor.ExecuteAsync(async () =>
            {
                await PlaythroughTuningRunner.Run(maxIterations, runsPerClass, stopWhenPass: true, dryRun);
                var session = LevelTuningSessionStore.Load();
                return new
                {
                    profileId = session.ProfileId,
                    playthrough = session.PlaythroughBatch,
                    analysis = session.Analysis,
                    lastApply = session.LastApply
                };
            }, writeIndented: true);
        }
    }
}
