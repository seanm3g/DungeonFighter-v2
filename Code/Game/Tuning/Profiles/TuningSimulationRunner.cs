using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RPGGame.MCP.Tools;

namespace RPGGame.Tuning.Profiles
{
    public sealed class TuningSimulationOutcome
    {
        public string Mode { get; init; } = "";
        public string ProfileId { get; init; } = "";
        public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
        public MultiLevelSimulationResult? MultiLevel { get; init; }
        public BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult? Comprehensive { get; init; }
        public FundamentalsSimulationResult? Fundamentals { get; init; }
        public ClassPlaythroughBatchResult? PlaythroughBatch { get; init; }
        public PlaythroughSimulationDto? PlaythroughSnapshot { get; init; }
        public int PlayerLevel { get; init; }
        public int EnemyLevel { get; init; }
        public int BattlesPerCombination { get; init; }

        public bool OptimizeWinRate { get; init; }

        public bool MeetsTargets =>
            Mode switch
            {
                TuningSimulationModes.MultiLevelWeaponEnemy => OptimizeWinRate
                    ? MultiLevel?.AllAnchorsWithinTolerance == true
                    : MultiLevel != null && MultiLevel.LevelSnapshots.Count > 0,
                TuningSimulationModes.ComprehensiveWeaponEnemy => Comprehensive != null,
                TuningSimulationModes.ClassBuildMatrix => Comprehensive != null,
                TuningSimulationModes.DungeonScaling => Comprehensive != null,
                TuningSimulationModes.FundamentalsEncounter => Fundamentals?.SuccessfulEncounters > 0,
                TuningSimulationModes.ClassPlaythroughBatch => PlaythroughBatch?.ClassAggregates.Count > 0,
                _ => false
            };

        public string FormatReport() => Mode switch
        {
            TuningSimulationModes.MultiLevelWeaponEnemy when MultiLevel != null =>
                MultiLevelSimulationRunner.FormatReport(MultiLevel),
            TuningSimulationModes.ComprehensiveWeaponEnemy when Comprehensive != null =>
                FormatComprehensiveReport(Comprehensive, PlayerLevel, EnemyLevel),
            TuningSimulationModes.FundamentalsEncounter when Fundamentals != null =>
                Fundamentals.FormatReport(),
            TuningSimulationModes.ClassPlaythroughBatch when PlaythroughBatch != null =>
                ClassPlaythroughBatchRunner.FormatReport(PlaythroughBatch),
            _ => "No simulation results."
        };

        private static string FormatComprehensiveReport(
            BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult result,
            int playerLevel,
            int enemyLevel)
        {
            var lines = new List<string>
            {
                $"Comprehensive weapon×enemy @ P{playerLevel}/E{enemyLevel}",
                $"Overall win rate: {result.OverallWinRate:F1}%",
                $"Average turns: {result.OverallAverageTurns:F1}",
                $"Total battles: {result.TotalBattles}",
                ""
            };

            if (result.WeaponStatistics.Count > 0)
            {
                lines.Add("Weapon win rates:");
                foreach (var w in result.WeaponStatistics.OrderByDescending(k => k.Value.WinRate))
                    lines.Add($"  {w.Key}: {w.Value.WinRate:F1}%");
                lines.Add("");
            }

            if (result.EnemyStatistics.Count > 0)
            {
                lines.Add("Enemy win rates (player perspective):");
                foreach (var e in result.EnemyStatistics.OrderByDescending(k => k.Value.WinRate))
                    lines.Add($"  {e.Key}: {e.Value.WinRate:F1}%");
            }

            return string.Join(System.Environment.NewLine, lines);
        }
    }

    public static class TuningSimulationRunner
    {
        public static async Task<TuningSimulationOutcome> RunAsync(
            BalanceTuningProfile profile,
            SimulationRunOverrides? overrides = null,
            IProgress<(int completed, int total, string status)>? progress = null)
        {
            var sim = profile.Simulation;
            string mode = sim.Mode;

            return mode switch
            {
                TuningSimulationModes.ComprehensiveWeaponEnemy => await RunComprehensiveAsync(profile, overrides, progress),
                TuningSimulationModes.FundamentalsEncounter => await RunFundamentalsAsync(profile, overrides, progress),
                TuningSimulationModes.ClassBuildMatrix => await RunComprehensiveAsync(profile, overrides, progress, TuningSimulationModes.ClassBuildMatrix),
                TuningSimulationModes.DungeonScaling => await RunComprehensiveAsync(profile, overrides, progress, TuningSimulationModes.DungeonScaling),
                TuningSimulationModes.ClassPlaythroughBatch => await RunPlaythroughBatchAsync(profile, overrides, progress),
                _ => await RunMultiLevelAsync(profile, overrides, progress)
            };
        }

        private static async Task<TuningSimulationOutcome> RunFundamentalsAsync(
            BalanceTuningProfile profile,
            SimulationRunOverrides? overrides,
            IProgress<(int completed, int total, string status)>? progress)
        {
            if (overrides?.PlayerLevel != null)
                profile.Simulation.PlayerLevel = overrides.PlayerLevel.Value;
            if (overrides?.EnemyLevel != null)
                profile.Simulation.EnemyLevel = overrides.EnemyLevel.Value;

            var result = await FundamentalsSimulationRunner.RunAsync(profile, overrides, progress);
            return new TuningSimulationOutcome
            {
                Mode = TuningSimulationModes.FundamentalsEncounter,
                ProfileId = profile.Id,
                Fundamentals = result,
                PlayerLevel = result.PlayerLevel,
                EnemyLevel = result.EnemyLevel,
                TimestampUtc = result.TimestampUtc,
                OptimizeWinRate = profile.Analysis.OptimizeWinRate
            };
        }

        private static async Task<TuningSimulationOutcome> RunMultiLevelAsync(
            BalanceTuningProfile profile,
            SimulationRunOverrides? overrides,
            IProgress<(int completed, int total, string status)>? progress)
        {
            var sim = profile.Simulation;
            var levels = overrides?.Levels ?? sim.Levels ?? LevelWinRateCurve.GetDefaultAnchorLevels();
            int battles = overrides?.BattlesPerCombination ?? sim.BattlesPerCombination;

            var result = await MultiLevelSimulationRunner.RunAsync(levels, battles, progress);

            return new TuningSimulationOutcome
            {
                Mode = TuningSimulationModes.MultiLevelWeaponEnemy,
                ProfileId = profile.Id,
                MultiLevel = result,
                BattlesPerCombination = battles,
                TimestampUtc = result.Timestamp,
                OptimizeWinRate = profile.Analysis.OptimizeWinRate
            };
        }

        private static async Task<TuningSimulationOutcome> RunComprehensiveAsync(
            BalanceTuningProfile profile,
            SimulationRunOverrides? overrides,
            IProgress<(int completed, int total, string status)>? progress,
            string? modeOverride = null)
        {
            var sim = profile.Simulation;
            int playerLevel = overrides?.PlayerLevel ?? sim.PlayerLevel;
            int enemyLevel = overrides?.EnemyLevel ?? sim.EnemyLevel;
            int battles = overrides?.BattlesPerCombination ?? sim.BattlesPerCombination;

            progress?.Report((0, 1, $"Comprehensive P{playerLevel}/E{enemyLevel}: starting"));

            var result = await BattleStatisticsRunner.RunComprehensiveWeaponEnemyTests(
                battles,
                playerLevel,
                enemyLevel,
                progress);

            McpToolState.LastTestResult = result;

            progress?.Report((1, 1, $"Comprehensive: WR {result.OverallWinRate:F1}%"));

            return new TuningSimulationOutcome
            {
                Mode = modeOverride ?? TuningSimulationModes.ComprehensiveWeaponEnemy,
                ProfileId = profile.Id,
                Comprehensive = result,
                PlayerLevel = playerLevel,
                EnemyLevel = enemyLevel,
                BattlesPerCombination = battles,
                TimestampUtc = DateTime.UtcNow,
                OptimizeWinRate = profile.Analysis.OptimizeWinRate
            };
        }

        private static async Task<TuningSimulationOutcome> RunPlaythroughBatchAsync(
            BalanceTuningProfile profile,
            SimulationRunOverrides? overrides,
            IProgress<(int completed, int total, string status)>? progress)
        {
            var sim = profile.Simulation;
            int runsPerClass = overrides?.RunsPerClass ?? sim.RunsPerClass;
            int maxActions = overrides?.MaxActionsPerRun ?? sim.MaxActionsPerRun;
            string? classes = overrides?.Classes ?? sim.Classes;

            var batch = await ClassPlaythroughBatchRunner.RunAsync(
                runsPerClass,
                classes,
                maxActions,
                progress);

            McpToolState.LastPlaythroughBatchResult = batch;

            var snapshot = PlaythroughSimulationMapper.FromBatch(batch, profile.Analysis.PlaythroughTargets);

            return new TuningSimulationOutcome
            {
                Mode = TuningSimulationModes.ClassPlaythroughBatch,
                ProfileId = profile.Id,
                PlaythroughBatch = batch,
                PlaythroughSnapshot = snapshot,
                TimestampUtc = batch.Timestamp,
                OptimizeWinRate = profile.Analysis.OptimizeWinRate
            };
        }
    }

    public sealed class SimulationRunOverrides
    {
        public int? BattlesPerCombination { get; init; }
        public int? BattlesPerWeapon { get; init; }
        public int? NumberOfBattles { get; init; }
        public IReadOnlyList<int>? Levels { get; init; }
        public int? PlayerLevel { get; init; }
        public int? EnemyLevel { get; init; }
        public int? EncounterCount { get; init; }
        public string? WeaponType { get; init; }
        public string? EnemyType { get; init; }
        public string? ForcedCatalogAction { get; init; }
        public bool? ContinuePastZeroHp { get; init; }
        public int? NegativeHpFloor { get; init; }
        public int? RunsPerClass { get; init; }
        public int? MaxActionsPerRun { get; init; }
        public string? Classes { get; init; }
    }
}
