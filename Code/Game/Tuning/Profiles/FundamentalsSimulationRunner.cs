using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RPGGame.ActionInteractionLab;

namespace RPGGame.Tuning.Profiles
{
    public sealed class FundamentalsSimulationResult
    {
        public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
        public int EncounterCount { get; init; }
        public int SuccessfulEncounters { get; init; }
        public int ErroredEncounters { get; init; }
        public double AverageActionsPerEncounter { get; init; }
        public double MedianActionsPerEncounter { get; init; }
        public double AveragePlayerTurnsPerEncounter { get; init; }
        public double MedianPlayerTurnsPerEncounter { get; init; }
        public double AverageEnemyTurnsPerEncounter { get; init; }
        public double MedianEnemyTurnsPerEncounter { get; init; }
        public int MinActions { get; init; }
        public int MaxActions { get; init; }
        public double AverageComboPlusEventsPerEncounter { get; init; }
        public double AverageComboStreakRuns2PlusPerEncounter { get; init; }
        public double AverageMaxComboStreak { get; init; }
        public Dictionary<int, int> ComboStreakRunTotals { get; init; } = new();
        public string ForcedCatalogAction { get; init; } = "";
        public int ComboStripCount { get; init; }
        public string? EnemyType { get; init; }
        public int PlayerLevel { get; init; }
        public int EnemyLevel { get; init; }
        public string WeaponType { get; init; } = "Sword";
        public double AverageSimAdvanceCalls { get; init; }
        public double WinRate { get; init; }
        public double TurnDurationStdDev { get; init; }
        public double AverageMissRate { get; init; }
        public double AverageCritRate { get; init; }
        public double AverageLossSeverity { get; init; }
        public double AverageTurnsBelowZero { get; init; }
        public bool ContinuePastZeroHp { get; init; }
        public IReadOnlyList<FundamentalsLevelSnapshot> LevelSnapshots { get; init; } = Array.Empty<FundamentalsLevelSnapshot>();

        public FundamentalsLevelSnapshot? GetLevelSnapshot(int level) =>
            LevelSnapshots.FirstOrDefault(s => s.Level == level);

        public static FundamentalsSimulationResult FromReport(
            ActionLabEncounterSimulationReport report,
            LabCombatSnapshot snapshot,
            SimulationProfileConfig config)
        {
            var samples = report.Encounters.Where(e => string.IsNullOrEmpty(e.ErrorMessage)).ToList();
            int denom = samples.Count;

            double avgStreakRuns = denom > 0
                ? samples.Average(e => e.PlayerComboStreakRunCounts.Values.Sum())
                : 0;

            static double MedianOf(IReadOnlyList<double> values)
            {
                if (values.Count == 0)
                    return 0;
                var sorted = values.OrderBy(v => v).ToList();
                int mid = sorted.Count / 2;
                return (sorted.Count & 1) == 1
                    ? sorted[mid]
                    : 0.5 * (sorted[mid - 1] + sorted[mid]);
            }

            var playerTurnSamples = samples.Select(e => (double)e.PlayerTurns).ToList();
            var enemyTurnSamples = samples.Select(e => (double)e.EnemyTurns).ToList();
            var advanceSamples = samples.Select(e => (double)e.SimAdvanceCalls).ToList();
            var turnSamples = samples.Select(e => (double)e.Turns).ToList();
            double turnStdDev = turnSamples.Count > 1
                ? Math.Sqrt(turnSamples.Sum(t => Math.Pow(t - turnSamples.Average(), 2)) / turnSamples.Count)
                : 0;

            int totalAttempts = samples.Sum(e => e.PlayerMissCount + e.PlayerDamageEvents);
            double missRate = totalAttempts > 0
                ? samples.Sum(e => e.PlayerMissCount) / (double)totalAttempts
                : 0;
            int totalDamageEvents = samples.Sum(e => e.PlayerDamageEvents);
            double critRate = totalDamageEvents > 0
                ? samples.Sum(e => e.PlayerCritCount) / (double)totalDamageEvents
                : 0;

            var combinedTurnSamples = samples.Select(e => (double)(e.PlayerTurns + e.EnemyTurns)).ToList();

            return new FundamentalsSimulationResult
            {
                TimestampUtc = DateTime.UtcNow,
                EncounterCount = report.EncounterCount,
                SuccessfulEncounters = denom,
                ErroredEncounters = report.ErroredEncounters,
                AverageActionsPerEncounter = combinedTurnSamples.Count > 0 ? combinedTurnSamples.Average() : 0,
                MedianActionsPerEncounter = MedianOf(combinedTurnSamples),
                AveragePlayerTurnsPerEncounter = playerTurnSamples.Count > 0 ? playerTurnSamples.Average() : 0,
                MedianPlayerTurnsPerEncounter = MedianOf(playerTurnSamples),
                AverageEnemyTurnsPerEncounter = enemyTurnSamples.Count > 0 ? enemyTurnSamples.Average() : 0,
                MedianEnemyTurnsPerEncounter = MedianOf(enemyTurnSamples),
                MinActions = samples.Count > 0 ? samples.Min(e => e.PlayerTurns + e.EnemyTurns) : report.MinTurns,
                MaxActions = samples.Count > 0 ? samples.Max(e => e.PlayerTurns + e.EnemyTurns) : report.MaxTurns,
                AverageComboPlusEventsPerEncounter = denom > 0 ? samples.Average(e => e.PlayerComboCount) : 0,
                AverageComboStreakRuns2PlusPerEncounter = avgStreakRuns,
                AverageMaxComboStreak = report.AveragePlayerMaxComboStreak,
                ComboStreakRunTotals = new Dictionary<int, int>(report.PlayerComboStreakRunTotals),
                ForcedCatalogAction = snapshot.SelectedCatalogActionName,
                ComboStripCount = snapshot.ComboStripActionNames.Count,
                EnemyType = snapshot.SessionEnemyLoaderType,
                PlayerLevel = config.PlayerLevel,
                EnemyLevel = config.EnemyLevel,
                WeaponType = config.WeaponType,
                AverageSimAdvanceCalls = advanceSamples.Count > 0 ? advanceSamples.Average() : 0,
                WinRate = report.WinRate,
                TurnDurationStdDev = turnStdDev,
                AverageMissRate = missRate,
                AverageCritRate = critRate,
                AverageLossSeverity = denom > 0 ? samples.Average(e => e.LossSeverityScore) : 0,
                AverageTurnsBelowZero = denom > 0 ? samples.Average(e => e.TurnsBelowZero) : 0,
                ContinuePastZeroHp = config.ContinuePastZeroHp
            };
        }

        public string FormatReport()
        {
            var lines = new List<string>
            {
                "Combat fundamentals (no armor/accessories; baseline weapon only)",
                $"  Hero L{PlayerLevel} ({WeaponType}) vs enemy L{EnemyLevel}" +
                    (string.IsNullOrEmpty(EnemyType) ? " (test dummy)" : $" ({EnemyType})"),
                $"  Combo strip: {ComboStripCount} action(s), forced on combo rolls: {ForcedCatalogAction}",
                "",
                $"Encounters: {SuccessfulEncounters}/{EncounterCount} successful ({ErroredEncounters} errors)",
                "",
                "Hero turns per encounter (player actions)",
                $"  Mean: {AveragePlayerTurnsPerEncounter:F2}  Median: {MedianPlayerTurnsPerEncounter:F2}",
                "Enemy turns per encounter (enemy actions)",
                $"  Mean: {AverageEnemyTurnsPerEncounter:F2}  Median: {MedianEnemyTurnsPerEncounter:F2}",
                "Combined actions per encounter (hero + enemy)",
                $"  Mean: {AverageActionsPerEncounter:F2}  Median: {MedianActionsPerEncounter:F2}",
                $"  Min: {MinActions}  Max: {MaxActions}",
                $"  (sim advance calls avg: {AverageSimAdvanceCalls:F1})",
                "",
                "Combo+ outcomes (successful rolls meeting combo threshold)",
                $"  Mean combo+ events per encounter: {AverageComboPlusEventsPerEncounter:F2}",
                $"  Mean longest combo+ chain per encounter: {AverageMaxComboStreak:F2}",
                $"  Mean completed 2+ combo+ chains per encounter: {AverageComboStreakRuns2PlusPerEncounter:F2}",
            };

            if (ComboStreakRunTotals.Count > 0)
            {
                lines.Add("");
                lines.Add("Completed combo+ chain lengths (pooled across encounters, length ≥ 2):");
                foreach (var kv in ComboStreakRunTotals.OrderBy(k => k.Key))
                    lines.Add($"  length {kv.Key}: {kv.Value} run(s)");
            }

            if (LevelSnapshots.Count > 1)
            {
                lines.Add("");
                lines.Add("Per-level combined actions (median hero+enemy turns):");
                foreach (var snap in LevelSnapshots.OrderBy(s => s.Level))
                    lines.Add($"  L{snap.Level}: median {snap.MedianCombinedActions:F0}, WR {snap.WinRate * 100:F0}% ({snap.SuccessfulEncounters} fights)");
            }

            return string.Join(System.Environment.NewLine, lines);
        }
    }

    public static class FundamentalsSimulationRunner
    {
        public static async Task<FundamentalsSimulationResult> RunAsync(
            BalanceTuningProfile profile,
            SimulationRunOverrides? overrides = null,
            IProgress<(int completed, int total, string status)>? progress = null)
        {
            var levels = FundamentalsLevelAnchors.ResolveEvaluationLevels(profile.Simulation, overrides);
            if (levels.Count <= 1)
            {
                int level = levels[0];
                profile.Simulation.PlayerLevel = level;
                profile.Simulation.EnemyLevel = level;
                return await RunSingleLevelAsync(profile, overrides, progress).ConfigureAwait(false);
            }

            return await RunMultiLevelAsync(profile, levels, overrides, progress).ConfigureAwait(false);
        }

        private static async Task<FundamentalsSimulationResult> RunMultiLevelAsync(
            BalanceTuningProfile profile,
            IReadOnlyList<int> levels,
            SimulationRunOverrides? overrides,
            IProgress<(int completed, int total, string status)>? progress)
        {
            int totalEncounters = overrides?.EncounterCount ?? profile.Simulation.EncounterCount;
            if (totalEncounters < 1)
                totalEncounters = 500;

            int perLevel = Math.Max(5, totalEncounters / levels.Count);
            var snapshots = new List<FundamentalsLevelSnapshot>();
            var levelResults = new List<FundamentalsSimulationResult>();
            int completedLevels = 0;

            foreach (int level in levels)
            {
                profile.Simulation.PlayerLevel = level;
                profile.Simulation.EnemyLevel = level;

                var levelOverrides = new SimulationRunOverrides
                {
                    EncounterCount = perLevel,
                    ContinuePastZeroHp = overrides?.ContinuePastZeroHp,
                    NegativeHpFloor = overrides?.NegativeHpFloor,
                    WeaponType = overrides?.WeaponType,
                    EnemyType = overrides?.EnemyType,
                    ForcedCatalogAction = overrides?.ForcedCatalogAction
                };

                var levelProgress = new Progress<(int completed, int total, string status)>(inner =>
                {
                    int done = completedLevels * perLevel + inner.completed;
                    progress?.Report((done, perLevel * levels.Count, $"L{level}: {inner.status}"));
                });

                var result = await RunSingleLevelAsync(profile, levelOverrides, levelProgress).ConfigureAwait(false);
                levelResults.Add(result);
                snapshots.Add(FundamentalsLevelSnapshot.FromResult(result));
                completedLevels++;
            }

            progress?.Report((perLevel * levels.Count, perLevel * levels.Count,
                $"Done: {levels.Count} levels, median combined {snapshots.Average(s => s.MedianCombinedActions):F1}"));

            return AggregateLevelResults(profile.Simulation, levelResults, snapshots);
        }

        private static FundamentalsSimulationResult AggregateLevelResults(
            SimulationProfileConfig sim,
            IReadOnlyList<FundamentalsSimulationResult> levelResults,
            IReadOnlyList<FundamentalsLevelSnapshot> snapshots)
        {
            int totalEncounters = levelResults.Sum(r => r.EncounterCount);
            int totalSuccess = levelResults.Sum(r => r.SuccessfulEncounters);
            double weight = totalSuccess > 0 ? totalSuccess : 1;

            double Weighted(Func<FundamentalsSimulationResult, double> pick) =>
                levelResults.Sum(r => pick(r) * r.SuccessfulEncounters) / weight;

            var mergedStreakTotals = new Dictionary<int, int>();
            foreach (var r in levelResults)
            {
                foreach (var kv in r.ComboStreakRunTotals)
                {
                    mergedStreakTotals.TryGetValue(kv.Key, out int n);
                    mergedStreakTotals[kv.Key] = n + kv.Value;
                }
            }

            var first = levelResults[0];
            return new FundamentalsSimulationResult
            {
                TimestampUtc = DateTime.UtcNow,
                EncounterCount = totalEncounters,
                SuccessfulEncounters = totalSuccess,
                ErroredEncounters = levelResults.Sum(r => r.ErroredEncounters),
                AverageActionsPerEncounter = Weighted(r => r.AverageActionsPerEncounter),
                MedianActionsPerEncounter = snapshots.Average(s => s.MedianCombinedActions),
                AveragePlayerTurnsPerEncounter = Weighted(r => r.AveragePlayerTurnsPerEncounter),
                MedianPlayerTurnsPerEncounter = snapshots.Average(s => s.MedianPlayerTurns),
                AverageEnemyTurnsPerEncounter = Weighted(r => r.AverageEnemyTurnsPerEncounter),
                MedianEnemyTurnsPerEncounter = snapshots.Average(s => s.MedianEnemyTurns),
                MinActions = levelResults.Min(r => r.MinActions),
                MaxActions = levelResults.Max(r => r.MaxActions),
                AverageComboPlusEventsPerEncounter = Weighted(r => r.AverageComboPlusEventsPerEncounter),
                AverageComboStreakRuns2PlusPerEncounter = Weighted(r => r.AverageComboStreakRuns2PlusPerEncounter),
                AverageMaxComboStreak = Weighted(r => r.AverageMaxComboStreak),
                ComboStreakRunTotals = mergedStreakTotals,
                ForcedCatalogAction = first.ForcedCatalogAction,
                ComboStripCount = first.ComboStripCount,
                EnemyType = first.EnemyType,
                PlayerLevel = 1,
                EnemyLevel = 1,
                WeaponType = first.WeaponType,
                AverageSimAdvanceCalls = Weighted(r => r.AverageSimAdvanceCalls),
                WinRate = Weighted(r => r.WinRate),
                TurnDurationStdDev = Weighted(r => r.TurnDurationStdDev),
                AverageMissRate = Weighted(r => r.AverageMissRate),
                AverageCritRate = Weighted(r => r.AverageCritRate),
                AverageLossSeverity = Weighted(r => r.AverageLossSeverity),
                AverageTurnsBelowZero = Weighted(r => r.AverageTurnsBelowZero),
                ContinuePastZeroHp = sim.ContinuePastZeroHp,
                LevelSnapshots = snapshots
            };
        }

        private static async Task<FundamentalsSimulationResult> RunSingleLevelAsync(
            BalanceTuningProfile profile,
            SimulationRunOverrides? overrides = null,
            IProgress<(int completed, int total, string status)>? progress = null)
        {
            var sim = profile.Simulation;
            int encounters = overrides?.EncounterCount ?? sim.EncounterCount;
            if (encounters < 1)
                encounters = 500;

            if (overrides?.ContinuePastZeroHp != null)
                profile.Simulation.ContinuePastZeroHp = overrides.ContinuePastZeroHp.Value;

            var snapshot = FundamentalsCombatSetup.BuildSnapshot(sim);
            string? validation = ActionLabEncounterSimulator.ValidateSnapshot(snapshot);
            if (validation != null)
                throw new InvalidOperationException(validation);

            progress?.Report((0, encounters, $"Running {encounters} fundamentals encounters..."));

            var encounterProgress = new Progress<(int completed, int total, string status)>(inner =>
                progress?.Report((inner.completed, inner.total, inner.status)));

            var rng = new Random();
            bool continuePastZeroHp = overrides?.ContinuePastZeroHp ?? sim.ContinuePastZeroHp;
            int? negativeHpFloor = continuePastZeroHp ? overrides?.NegativeHpFloor : null;

            ActionLabEncounterSimulationReport report;
            using (DeveloperSimMode.BeginScope(continuePastZeroHp, negativeHpFloor))
            {
                report = await ActionLabEncounterSimulator.RunBatchAsync(
                        snapshot, encounters, rng, maxDegreeOfParallelism: 1, encounterProgress, continuePastZeroHp)
                    .ConfigureAwait(false);
            }

            progress?.Report((encounters, encounters,
                $"Done: {report.AverageTurns:F1} mean actions, {report.AveragePlayerMaxComboStreak:F2} mean max combo+ chain"));

            return FundamentalsSimulationResult.FromReport(report, snapshot, sim);
        }
    }
}
