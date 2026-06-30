using System;
using System.Threading.Tasks;
using RPGGame.MCP.Tools;
using RPGGame.Tuning;

namespace RPGGame.Game
{
    /// <summary>
    /// Runs headless playthrough batches for all four classes. Run: dotnet run -- PLAYTODEATHBATCH [runsPerClass]
    /// </summary>
    public static class PlayUntilDeathBatch
    {
        public static async Task RunAsync(int runsPerClass = 3, int maxActionsPerRun = 500, string? classesCsv = null)
        {
            Console.WriteLine("=== PLAY UNTIL DEATH BATCH ===");
            Console.WriteLine($"Runs per class: {runsPerClass}");
            Console.WriteLine($"Max actions per run: {maxActionsPerRun}\n");

            var batch = await ClassPlaythroughBatchRunner.RunAsync(
                runsPerClass,
                classesCsv,
                maxActionsPerRun);

            McpToolState.LastPlaythroughBatchResult = batch;
            Console.WriteLine(ClassPlaythroughBatchRunner.FormatReport(batch));
        }
    }
}
