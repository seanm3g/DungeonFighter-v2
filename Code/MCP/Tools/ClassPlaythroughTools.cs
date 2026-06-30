using System;
using System.Linq;
using System.Threading.Tasks;
using RPGGame.Tuning;

namespace RPGGame.MCP.Tools
{
    public static class ClassPlaythroughTools
    {
        public static Task<string> RunClassPlaythroughBatch(
            int runsPerClass = 5,
            string? classes = null,
            int maxActionsPerRun = 500)
        {
            return McpToolExecutor.ExecuteAsync(async () =>
            {
                var batch = await ClassPlaythroughBatchRunner.RunAsync(
                    runsPerClass,
                    classes,
                    maxActionsPerRun);

                McpToolState.LastPlaythroughBatchResult = batch;

                return new
                {
                    summary = ClassPlaythroughBatchRunner.FormatReport(batch),
                    runsPerClass = batch.RunsPerClass,
                    maxActionsPerRun = batch.MaxActionsPerRun,
                    elapsedSeconds = batch.Elapsed.TotalSeconds,
                    hasParityWarnings = batch.HasParityWarnings,
                    parityWarnings = batch.ParityWarnings,
                    classes = batch.ClassAggregates.Select(a => new
                    {
                        className = a.ClassDisplayName,
                        weaponType = a.WeaponType.ToString(),
                        runCount = a.RunCount,
                        deathRate = a.DeathRate,
                        meanFinalLevel = a.MeanFinalLevel,
                        medianFinalLevel = a.MedianFinalLevel,
                        meanDungeonsCompleted = a.MeanDungeonsCompleted,
                        meanTurnCount = a.MeanTurnCount,
                        meanActionsByLevel = a.MeanActionsByLevel.Select(s => new
                        {
                            level = s.Level,
                            meanActions = s.MeanActions,
                            sampleRunCount = s.SampleRunCount
                        }).ToList(),
                        minFinalLevel = a.MinFinalLevel,
                        maxFinalLevel = a.MaxFinalLevel
                    }).ToList()
                };
            }, writeIndented: true);
        }

        public static Task<string> GetClassPlaythroughReport()
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var batch = McpToolState.LastPlaythroughBatchResult;
                if (batch == null)
                    throw new InvalidOperationException("No class playthrough batch result available. Run run_class_playthrough_batch first.");

                return new
                {
                    report = ClassPlaythroughBatchRunner.FormatReport(batch),
                    hasParityWarnings = batch.HasParityWarnings,
                    parityWarnings = batch.ParityWarnings,
                    timestamp = batch.Timestamp,
                    elapsedSeconds = batch.Elapsed.TotalSeconds
                };
            }, writeIndented: true);
        }
    }
}
