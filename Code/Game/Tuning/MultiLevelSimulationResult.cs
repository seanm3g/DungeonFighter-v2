using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Tuning
{
    /// <summary>
    /// Win rate and duration snapshot for one same-level simulation sweep point.
    /// </summary>
    public sealed class LevelSimulationSnapshot
    {
        public int Level { get; init; }
        public double TargetWinRate { get; init; }
        public double ActualWinRate { get; init; }
        public double Delta => ActualWinRate - TargetWinRate;
        public double AverageTurns { get; init; }
        public bool WithinTolerance { get; init; }
        public double ConformanceScore { get; init; }
        public int TotalBattles { get; init; }
    }

    /// <summary>
    /// Aggregated results from simulating multiple hero/enemy levels.
    /// </summary>
    public sealed class MultiLevelSimulationResult
    {
        public List<LevelSimulationSnapshot> LevelSnapshots { get; set; } = new();
        public int WorstLevel { get; set; }
        public double WorstDeltaMagnitude { get; set; }
        public double OverallCurveScore { get; set; }
        public bool AllAnchorsWithinTolerance { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public int BattlesPerCombination { get; set; }

        public LevelSimulationSnapshot? GetWorstSnapshot() =>
            LevelSnapshots.FirstOrDefault(s => s.Level == WorstLevel);

        public IEnumerable<LevelSimulationSnapshot> GetAnchorSnapshots()
        {
            var anchorLevels = new HashSet<int>(LevelWinRateCurve.GetCurveAnchorLevels());
            return LevelSnapshots.Where(s => anchorLevels.Contains(s.Level));
        }
    }
}
