using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPGGame.Tuning
{
    public static class ClassPlaythroughBatchRunner
    {
        public static async Task<ClassPlaythroughBatchResult> RunAsync(
            int runsPerClass = 5,
            string? classesCsv = null,
            int maxActionsPerRun = 500,
            IProgress<(int completed, int total, string status)>? progress = null)
        {
            if (runsPerClass < 1)
                throw new ArgumentOutOfRangeException(nameof(runsPerClass), "runsPerClass must be at least 1.");

            var classes = ClassPlaythroughClassResolver.ResolveClasses(classesCsv);
            var stopwatch = Stopwatch.StartNew();

            var batch = new ClassPlaythroughBatchResult
            {
                RunsPerClass = runsPerClass,
                MaxActionsPerRun = maxActionsPerRun,
                Timestamp = DateTime.UtcNow
            };

            int totalRuns = classes.Count * runsPerClass;
            int completed = 0;

            foreach (var (weaponType, menuSlot, displayName) in classes)
            {
                var runs = new List<PlaythroughRunResult>();

                for (int runIndex = 0; runIndex < runsPerClass; runIndex++)
                {
                    progress?.Report((completed, totalRuns,
                        $"{displayName} run {runIndex + 1}/{runsPerClass}"));

                    var run = await ClassPlaythroughRunner.RunAsync(
                        weaponType,
                        menuSlot,
                        displayName,
                        maxActionsPerRun);

                    runs.Add(run);
                    completed++;
                    progress?.Report((completed, totalRuns,
                        $"{displayName} run {runIndex + 1}/{runsPerClass} complete"));
                }

                batch.ClassAggregates.Add(
                    ClassPlaythroughAggregator.BuildAggregate(weaponType, displayName, runs));
            }

            stopwatch.Stop();
            batch.Elapsed = stopwatch.Elapsed;
            batch.ParityWarnings = ClassPlaythroughAggregator.BuildParityWarnings(batch.ClassAggregates);
            return batch;
        }

        public static string FormatReport(ClassPlaythroughBatchResult batch)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Class Playthrough Batch Report ===");
            sb.AppendLine($"Runs per class: {batch.RunsPerClass}");
            sb.AppendLine($"Max actions per run: {batch.MaxActionsPerRun}");
            sb.AppendLine($"Elapsed: {batch.Elapsed.TotalSeconds:F1}s");
            sb.AppendLine($"Timestamp: {batch.Timestamp:u}");
            sb.AppendLine();

            sb.AppendLine("Class           Runs  Death%  AvgLvl  MedLvl  AvgDng  AvgTurns");
            sb.AppendLine("--------------  ----  ------  ------  ------  ------  --------");

            foreach (var aggregate in batch.ClassAggregates.OrderBy(a => a.WeaponMenuSlotOrDefault()))
            {
                sb.AppendLine(
                    $"{aggregate.ClassDisplayName,-14}  " +
                    $"{aggregate.RunCount,4}  " +
                    $"{aggregate.DeathRate * 100,5:F0}%  " +
                    $"{aggregate.MeanFinalLevel,6:F1}  " +
                    $"{aggregate.MedianFinalLevel,6:F1}  " +
                    $"{aggregate.MeanDungeonsCompleted,6:F1}  " +
                    $"{aggregate.MeanTurnCount,8:F1}");
            }

            if (batch.ParityWarnings.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Parity warnings:");
                foreach (var warning in batch.ParityWarnings)
                    sb.AppendLine($"  - {warning}");
            }
            else
            {
                sb.AppendLine();
                sb.AppendLine("No cross-class parity warnings.");
            }

            AppendPerLevelTurnSection(sb, batch);

            return sb.ToString();
        }

        private static void AppendPerLevelTurnSection(StringBuilder sb, ClassPlaythroughBatchResult batch)
        {
            bool anyLevelData = batch.ClassAggregates.Any(a => a.MeanActionsByLevel.Count > 0);
            if (!anyLevelData)
                return;

            sb.AppendLine();
            sb.AppendLine("Avg actions spent at each hero level (menu + combat steps):");
            sb.AppendLine();

            int maxLevel = batch.ClassAggregates
                .SelectMany(a => a.MeanActionsByLevel)
                .Select(s => s.Level)
                .DefaultIfEmpty(0)
                .Max();

            sb.Append("Level ");
            for (int level = 1; level <= maxLevel; level++)
                sb.Append($"  L{level,2}");
            sb.AppendLine();

            foreach (var aggregate in batch.ClassAggregates.OrderBy(a => a.WeaponMenuSlotOrDefault()))
            {
                sb.Append($"{aggregate.ClassDisplayName,-14}");
                for (int level = 1; level <= maxLevel; level++)
                {
                    var stat = aggregate.MeanActionsByLevel.FirstOrDefault(s => s.Level == level);
                    if (stat == null)
                        sb.Append("     -");
                    else
                        sb.Append($"  {stat.MeanActions,4:F0}");
                }
                sb.AppendLine();
            }
        }
    }

    internal static class ClassPlaythroughAggregateExtensions
    {
        internal static int WeaponMenuSlotOrDefault(this ClassPlaythroughAggregate aggregate)
        {
            int index = Array.IndexOf(ClassPresentationConfig.ClassWeaponOrder, aggregate.WeaponType);
            return index >= 0 ? index + 1 : 99;
        }
    }
}
