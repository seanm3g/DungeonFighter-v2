using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Tuning
{
    public enum PlaythroughStopReason
    {
        Death,
        MaxActions,
        Error
    }

    public class PlaythroughRunResult
    {
        public WeaponType WeaponType { get; set; }
        public string ClassDisplayName { get; set; } = "";
        public int WeaponMenuSlot { get; set; }
        public bool ReachedDeath { get; set; }
        public PlaythroughStopReason StopReason { get; set; }
        public int TurnCount { get; set; }
        public int DungeonsAttempted { get; set; }
        public int DungeonsCompleted { get; set; }
        public int FinalLevel { get; set; }
        public int FinalHealth { get; set; }
        public int FinalMaxHealth { get; set; }
        public int InventoryCount { get; set; }
        public int TotalXp { get; set; }
        public string? FinalState { get; set; }
        public string? ErrorMessage { get; set; }
        /// <summary>Menu/combat actions taken while the hero was at each character level (level → action count).</summary>
        public Dictionary<int, int> ActionsByLevel { get; set; } = new();
    }

    public class PlaythroughLevelTurnStats
    {
        public int Level { get; set; }
        public double MeanActions { get; set; }
        public int SampleRunCount { get; set; }
    }

    public class ClassPlaythroughAggregate
    {
        public WeaponType WeaponType { get; set; }
        public string ClassDisplayName { get; set; } = "";
        public int RunCount { get; set; }
        public double DeathRate { get; set; }
        public double MeanFinalLevel { get; set; }
        public double MedianFinalLevel { get; set; }
        public int MinFinalLevel { get; set; }
        public int MaxFinalLevel { get; set; }
        public double MeanDungeonsCompleted { get; set; }
        public int MinDungeonsCompleted { get; set; }
        public int MaxDungeonsCompleted { get; set; }
        public double MeanTurnCount { get; set; }
        public List<PlaythroughLevelTurnStats> MeanActionsByLevel { get; set; } = new();
        public List<PlaythroughRunResult> Runs { get; set; } = new();
    }

    public class ClassPlaythroughBatchResult
    {
        public int RunsPerClass { get; set; }
        public int MaxActionsPerRun { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public TimeSpan Elapsed { get; set; }
        public List<ClassPlaythroughAggregate> ClassAggregates { get; set; } = new();
        public List<string> ParityWarnings { get; set; } = new();
        public bool HasParityWarnings => ParityWarnings.Count > 0;
    }

    public static class ClassPlaythroughAggregator
    {
        public const int DefaultLevelSpreadThreshold = 3;
        public const int DefaultDungeonSpreadThreshold = 2;

        public static ClassPlaythroughAggregate BuildAggregate(
            WeaponType weaponType,
            string classDisplayName,
            IReadOnlyList<PlaythroughRunResult> runs)
        {
            var aggregate = new ClassPlaythroughAggregate
            {
                WeaponType = weaponType,
                ClassDisplayName = classDisplayName,
                RunCount = runs.Count,
                Runs = runs.ToList()
            };

            if (runs.Count == 0)
                return aggregate;

            var levels = new List<int>();
            var dungeons = new List<int>();
            var turns = new List<int>();
            int deaths = 0;

            foreach (var run in runs)
            {
                levels.Add(run.FinalLevel);
                dungeons.Add(run.DungeonsCompleted);
                turns.Add(run.TurnCount);
                if (run.ReachedDeath)
                    deaths++;
            }

            aggregate.DeathRate = (double)deaths / runs.Count;
            aggregate.MeanFinalLevel = levels.Average();
            aggregate.MedianFinalLevel = Median(levels);
            aggregate.MinFinalLevel = levels.Min();
            aggregate.MaxFinalLevel = levels.Max();
            aggregate.MeanDungeonsCompleted = dungeons.Average();
            aggregate.MinDungeonsCompleted = dungeons.Min();
            aggregate.MaxDungeonsCompleted = dungeons.Max();
            aggregate.MeanTurnCount = turns.Average();
            aggregate.MeanActionsByLevel = BuildMeanActionsByLevel(runs);

            return aggregate;
        }

        public static List<PlaythroughLevelTurnStats> BuildMeanActionsByLevel(IReadOnlyList<PlaythroughRunResult> runs)
        {
            var buckets = new Dictionary<int, List<int>>();
            foreach (var run in runs)
            {
                foreach (var (level, actions) in run.ActionsByLevel)
                {
                    if (level <= 0)
                        continue;
                    if (!buckets.TryGetValue(level, out var list))
                    {
                        list = new List<int>();
                        buckets[level] = list;
                    }
                    list.Add(actions);
                }
            }

            return buckets
                .OrderBy(kv => kv.Key)
                .Select(kv => new PlaythroughLevelTurnStats
                {
                    Level = kv.Key,
                    MeanActions = kv.Value.Average(),
                    SampleRunCount = kv.Value.Count
                })
                .ToList();
        }

        public static List<string> BuildParityWarnings(
            IReadOnlyList<ClassPlaythroughAggregate> aggregates,
            int levelSpreadThreshold = DefaultLevelSpreadThreshold,
            int dungeonSpreadThreshold = DefaultDungeonSpreadThreshold)
        {
            var warnings = new List<string>();
            if (aggregates.Count < 2)
                return warnings;

            var meanLevels = aggregates.Select(a => a.MeanFinalLevel).ToList();
            double levelSpread = meanLevels.Max() - meanLevels.Min();
            if (levelSpread > levelSpreadThreshold)
            {
                warnings.Add(
                    $"Mean level-at-death spread is {levelSpread:F1} (threshold {levelSpreadThreshold}). " +
                    $"Lowest: {aggregates.OrderBy(a => a.MeanFinalLevel).First().ClassDisplayName} ({meanLevels.Min():F1}), " +
                    $"Highest: {aggregates.OrderByDescending(a => a.MeanFinalLevel).First().ClassDisplayName} ({meanLevels.Max():F1}).");
            }

            var meanDungeons = aggregates.Select(a => a.MeanDungeonsCompleted).ToList();
            double dungeonSpread = meanDungeons.Max() - meanDungeons.Min();
            if (dungeonSpread > dungeonSpreadThreshold)
            {
                warnings.Add(
                    $"Mean dungeons-completed spread is {dungeonSpread:F1} (threshold {dungeonSpreadThreshold}). " +
                    $"Lowest: {aggregates.OrderBy(a => a.MeanDungeonsCompleted).First().ClassDisplayName} ({meanDungeons.Min():F1}), " +
                    $"Highest: {aggregates.OrderByDescending(a => a.MeanDungeonsCompleted).First().ClassDisplayName} ({meanDungeons.Max():F1}).");
            }

            foreach (var aggregate in aggregates)
            {
                if (aggregate.MeanDungeonsCompleted <= 0.01 && aggregate.RunCount > 0)
                    warnings.Add($"{aggregate.ClassDisplayName} averaged 0 dungeons completed across {aggregate.RunCount} run(s).");
            }

            return warnings;
        }

        private static double Median(IReadOnlyList<int> values)
        {
            if (values.Count == 0)
                return 0;

            var sorted = values.OrderBy(v => v).ToList();
            int mid = sorted.Count / 2;
            if (sorted.Count % 2 == 0)
                return (sorted[mid - 1] + sorted[mid]) / 2.0;
            return sorted[mid];
        }
    }
}
