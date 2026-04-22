using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RPGGame.Actions.RollModification;
using RPGGame.BattleStatistics;
using RPGGame.Combat;
using RPGGame.Entity.Services;

namespace RPGGame.ActionInteractionLab
{
    /// <summary>
    /// Immutable capture of lab hero, enemy, strip, and catalog pick for encounter batch simulation.
    /// </summary>
    public sealed class LabCombatSnapshot
    {
        public LabCombatSnapshot(
            string initialPlayerJson,
            int labPanelStrDelta,
            int labPanelAgiDelta,
            int labPanelTecDelta,
            int labPanelIntDelta,
            int labPanelLevelDelta,
            int labPanelArmorDelta,
            string? sessionEnemyLoaderType,
            int enemyLevel,
            IReadOnlyList<string> comboStripActionNames,
            string selectedCatalogActionName)
        {
            InitialPlayerJson = initialPlayerJson ?? throw new ArgumentNullException(nameof(initialPlayerJson));
            LabPanelStrDelta = labPanelStrDelta;
            LabPanelAgiDelta = labPanelAgiDelta;
            LabPanelTecDelta = labPanelTecDelta;
            LabPanelIntDelta = labPanelIntDelta;
            LabPanelLevelDelta = labPanelLevelDelta;
            LabPanelArmorDelta = labPanelArmorDelta;
            SessionEnemyLoaderType = sessionEnemyLoaderType;
            EnemyLevel = enemyLevel;
            ComboStripActionNames = comboStripActionNames ?? throw new ArgumentNullException(nameof(comboStripActionNames));
            SelectedCatalogActionName = selectedCatalogActionName ?? "";
        }

        public string InitialPlayerJson { get; }
        public int LabPanelStrDelta { get; }
        public int LabPanelAgiDelta { get; }
        public int LabPanelTecDelta { get; }
        public int LabPanelIntDelta { get; }
        public int LabPanelLevelDelta { get; }
        public int LabPanelArmorDelta { get; }
        public string? SessionEnemyLoaderType { get; }
        public int EnemyLevel { get; }
        public IReadOnlyList<string> ComboStripActionNames { get; }
        public string SelectedCatalogActionName { get; }

        /// <summary>Matches <see cref="ActionInteractionLabSession"/> default dummy when no loader enemy is set.</summary>
        public static BattleConfiguration DefaultTestEnemyBattleConfig { get; } = new()
        {
            PlayerDamage = 10,
            PlayerAttackSpeed = 1.0,
            PlayerArmor = 0,
            PlayerHealth = 100,
            EnemyDamage = 10,
            EnemyAttackSpeed = 0.65,
            EnemyArmor = 5,
            EnemyHealth = 150
        };
    }

    /// <summary>Metrics from one simulated encounter.</summary>
    public sealed class EncounterMetrics
    {
        public bool PlayerWon { get; set; }
        public int Turns { get; set; }
        public int PlayerDamageDealt { get; set; }
        public int PlayerComboCount { get; set; }
        /// <summary>Longest single chain of consecutive successful player actions flagged as combo (timeline order).</summary>
        public int PlayerMaxComboStreak { get; set; }
        /// <summary>Completed combo chains of exact length ≥ 2 within this encounter (each uninterrupted run counts once).</summary>
        public Dictionary<int, int> PlayerComboStreakRunCounts { get; } = new();
        public int PlayerCritCount { get; set; }
        public int PlayerDamageEvents { get; set; }
        public int EnemyDamageDealt { get; set; }
        public int EnemyComboCount { get; set; }
        public int PlayerFinalHealth { get; set; }
        public int EnemyFinalHealth { get; set; }
        public double CombatGameTime { get; set; }
        public double PlayerDpsVersusTime { get; set; }
        public CombatSingleTurnResult? TerminalReason { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>Aggregated results from a batch of lab encounter simulations.</summary>
    public sealed class ActionLabEncounterSimulationReport
    {
        public List<EncounterMetrics> Encounters { get; } = new();
        public int EncounterCount => Encounters.Count;
        public int PlayerWins { get; set; }
        public double WinRate { get; set; }
        public double AverageTurns { get; set; }
        public int MinTurns { get; set; }
        public int MaxTurns { get; set; }
        public double MedianTurns { get; set; }
        public double StdDevTurns { get; set; }
        public double MedianPlayerDamage { get; set; }
        public double StdDevPlayerDamage { get; set; }
        public double MedianPlayerDps { get; set; }
        public double StdDevPlayerDps { get; set; }
        public double MedianCombatGameTime { get; set; }
        public double StdDevCombatGameTime { get; set; }
        public double AverageTurnsOnWin { get; set; }
        public double AverageTurnsOnLoss { get; set; }
        public double AveragePlayerDamageOnWin { get; set; }
        public double AveragePlayerDamageOnLoss { get; set; }
        public double AveragePlayerDpsOnWin { get; set; }
        public double AveragePlayerDpsOnLoss { get; set; }
        public double AveragePlayerHpRemainingOnWin { get; set; }
        public double AveragePlayerHpRemainingOnLoss { get; set; }
        public double AverageEnemyDamage { get; set; }
        public double AverageEnemyComboCount { get; set; }
        public double AveragePlayerDamage { get; set; }
        public double AveragePlayerDps { get; set; }
        public double AverageCritsPerEncounter { get; set; }
        public double CritRatePerDamageEvent { get; set; }
        public Dictionary<int, int> PlayerComboCountHistogram { get; } = new();
        /// <summary>Pooled counts of combo chains (length ≥ 2) across successful encounters.</summary>
        public Dictionary<int, int> PlayerComboStreakRunTotals { get; } = new();
        /// <summary>Bucket = min(<see cref="MaxComboStreakHistogramBucket"/>, longest chain in encounter); last bucket is <c>N+</c>.</summary>
        public Dictionary<int, int> PlayerMaxComboStreakHistogram { get; } = new();
        /// <summary>Mean of <see cref="EncounterMetrics.PlayerMaxComboStreak"/> over successful (non-errored) encounters.</summary>
        public double AveragePlayerMaxComboStreak { get; set; }
        public Dictionary<string, int> TurnCountHistogram { get; } = new();
        public Dictionary<CombatSingleTurnResult, int> TerminalReasonCounts { get; } = new();
        public int ErroredEncounters { get; set; }

        /// <summary>Wall-clock time spent in <see cref="ActionLabEncounterSimulator.RunBatchAsync"/> (reload, encounters, aggregate).</summary>
        public TimeSpan SimulationWallElapsed { get; set; }
    }

    /// <summary>
    /// Runs full lab-style encounters without mutating <see cref="ActionInteractionLabSession"/> entities.
    /// </summary>
    public static class ActionLabEncounterSimulator
    {
        /// <summary>Default number of encounters for the Action Lab <c>[ Sim 1000 ]</c> batch button.</summary>
        public const int DefaultBatchEncounterCount = 1000;

        /// <summary>
        /// Serializes encounter execution when <see cref="RunBatchAsync"/> uses parallelism. Combat/narrative code still
        /// shares static services (roll packages, registries, etc.) that are not safe for concurrent encounters; the
        /// outer loop stays parallel-friendly for scheduling while each encounter runs alone.
        /// </summary>
        private static readonly SemaphoreSlim EncounterExecutionMutex = new SemaphoreSlim(1, 1);

        private const int ComboHistogramMaxBucket = 10;
        private const int MaxComboStreakHistogramBucket = 15;

        private static readonly string[] TurnHistogramBucketOrder =
        {
            "1-5", "6-10", "11-20", "21-40", "41+",
        };

        /// <summary>Validates snapshot for simulation (non-empty strip, known forced action).</summary>
        public static string? ValidateSnapshot(LabCombatSnapshot snapshot)
        {
            if (snapshot.ComboStripActionNames.Count == 0)
                return "Add at least one action to the hero combo strip before running simulation.";
            if (string.IsNullOrWhiteSpace(snapshot.SelectedCatalogActionName))
                return "Select a catalog action name for forced combo steps.";
            var forced = ActionLoader.GetAction(snapshot.SelectedCatalogActionName.Trim());
            if (forced == null)
                return $"Unknown catalog action '{snapshot.SelectedCatalogActionName}'.";
            return null;
        }

        /// <summary>
        /// Derives a stable <see cref="Random"/> seed for encounter <paramref name="encounterIndex"/> within a Monte Carlo batch.
        /// Parallel batches use this so each encounter has an isolated RNG stream derived from one batch seed.
        /// </summary>
        public static int DeriveEncounterRandomSeed(int batchSeed, int encounterIndex)
        {
            unchecked
            {
                uint h = (uint)batchSeed;
                h ^= (uint)encounterIndex * 0x9E3779B1u;
                h ^= h >> 16;
                h *= 0x7feb352du;
                h ^= h >> 15;
                h *= 0x846ca68bu;
                h ^= h >> 16;
                return (int)h;
            }
        }

        /// <param name="maxDegreeOfParallelism">
        /// Degree of parallelism for encounter execution. Use <c>1</c> for one <paramref name="rng"/> stream across encounters.
        /// Use <c>-1</c> for <see cref="Environment.ProcessorCount"/>. Values greater than <c>1</c> cap worker count.
        /// When parallelism is greater than <c>1</c>, one batch seed is taken from <paramref name="rng"/> and each encounter uses <see cref="DeriveEncounterRandomSeed"/>.
        /// <see cref="ActionLoader.ReloadActions"/> runs once before workers; encounters only read loaded data.
        /// </param>
        public static async Task<ActionLabEncounterSimulationReport> RunBatchAsync(
            LabCombatSnapshot snapshot,
            int encounterCount,
            Random rng,
            int maxDegreeOfParallelism = -1)
        {
            if (encounterCount < 1)
                throw new ArgumentOutOfRangeException(nameof(encounterCount));
            if (maxDegreeOfParallelism < -1 || maxDegreeOfParallelism == 0)
                throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism));
            var validation = ValidateSnapshot(snapshot);
            if (validation != null)
                throw new InvalidOperationException(validation);

            int dop = ResolveMaxDegreeOfParallelism(maxDegreeOfParallelism);

            var report = new ActionLabEncounterSimulationReport();
            var wall = Stopwatch.StartNew();
            ActionLoader.ReloadActions();
            // Default dummy path still calls GetAllEnemyTypes from parallel workers; preload + lock in EnemyLoader avoids races.
            EnemyLoader.LoadEnemies();

            bool prevUi = CombatManager.DisableCombatUIOutput;
            CombatManager.DisableCombatUIOutput = true;
            try
            {
                if (dop <= 1)
                {
                    for (int i = 0; i < encounterCount; i++)
                    {
                        var m = await RunSingleEncounterAsync(snapshot, rng).ConfigureAwait(false);
                        report.Encounters.Add(m);
                    }
                }
                else
                {
                    int batchSeed = rng.Next();
                    var results = new EncounterMetrics[encounterCount];
                    var options = new ParallelOptions { MaxDegreeOfParallelism = dop };
                    await Parallel.ForEachAsync(
                        Enumerable.Range(0, encounterCount),
                        options,
                        async (i, cancellationToken) =>
                        {
                            var encRng = new Random(DeriveEncounterRandomSeed(batchSeed, i));
                            await EncounterExecutionMutex.WaitAsync(cancellationToken).ConfigureAwait(false);
                            try
                            {
                                results[i] = await RunSingleEncounterAsync(snapshot, encRng).ConfigureAwait(false);
                            }
                            finally
                            {
                                EncounterExecutionMutex.Release();
                            }
                        }).ConfigureAwait(false);

                    foreach (var m in results)
                        report.Encounters.Add(m);
                }
            }
            finally
            {
                CombatManager.DisableCombatUIOutput = prevUi;
            }

            Aggregate(report);
            wall.Stop();
            report.SimulationWallElapsed = wall.Elapsed;
            return report;
        }

        private static int ResolveMaxDegreeOfParallelism(int maxDegreeOfParallelism)
        {
            if (maxDegreeOfParallelism < -1 || maxDegreeOfParallelism == 0)
                throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism));
            if (maxDegreeOfParallelism == -1)
                return Math.Max(1, global::System.Environment.ProcessorCount);
            return maxDegreeOfParallelism;
        }

        private static void Aggregate(ActionLabEncounterSimulationReport report)
        {
            if (report.Encounters.Count == 0)
                return;

            report.ErroredEncounters = report.Encounters.Count(e => !string.IsNullOrEmpty(e.ErrorMessage));
            var samples = report.Encounters.Where(e => string.IsNullOrEmpty(e.ErrorMessage)).ToList();
            int denom = samples.Count;
            if (denom == 0)
                return;

            report.PlayerWins = samples.Count(e => e.PlayerWon);
            report.WinRate = report.PlayerWins / (double)denom;
            report.AverageTurns = samples.Average(e => e.Turns);
            report.MinTurns = samples.Min(e => e.Turns);
            report.MaxTurns = samples.Max(e => e.Turns);
            report.MedianTurns = Median(samples.Select(e => (double)e.Turns).ToList());
            report.StdDevTurns = PopulationStdDev(samples.Select(e => (double)e.Turns).ToList());
            report.AveragePlayerDamage = samples.Average(e => e.PlayerDamageDealt);
            report.MedianPlayerDamage = Median(samples.Select(e => (double)e.PlayerDamageDealt).ToList());
            report.StdDevPlayerDamage = PopulationStdDev(samples.Select(e => (double)e.PlayerDamageDealt).ToList());
            report.AveragePlayerDps = samples.Average(e => e.PlayerDpsVersusTime);
            report.MedianPlayerDps = Median(samples.Select(e => e.PlayerDpsVersusTime).ToList());
            report.StdDevPlayerDps = PopulationStdDev(samples.Select(e => e.PlayerDpsVersusTime).ToList());
            report.MedianCombatGameTime = Median(samples.Select(e => e.CombatGameTime).ToList());
            report.StdDevCombatGameTime = PopulationStdDev(samples.Select(e => e.CombatGameTime).ToList());
            report.AverageCritsPerEncounter = samples.Average(e => e.PlayerCritCount);
            int totalDamageEvents = samples.Sum(e => e.PlayerDamageEvents);
            int totalCrits = samples.Sum(e => e.PlayerCritCount);
            report.CritRatePerDamageEvent = totalDamageEvents > 0 ? totalCrits / (double)totalDamageEvents : 0.0;
            report.AverageEnemyDamage = samples.Average(e => e.EnemyDamageDealt);
            report.AverageEnemyComboCount = samples.Average(e => e.EnemyComboCount);
            report.AveragePlayerMaxComboStreak = samples.Average(e => (double)e.PlayerMaxComboStreak);

            var wins = samples.Where(e => e.PlayerWon).ToList();
            var losses = samples.Where(e => !e.PlayerWon).ToList();
            report.AverageTurnsOnWin = wins.Count > 0 ? wins.Average(e => e.Turns) : double.NaN;
            report.AverageTurnsOnLoss = losses.Count > 0 ? losses.Average(e => e.Turns) : double.NaN;
            report.AveragePlayerDamageOnWin = wins.Count > 0 ? wins.Average(e => e.PlayerDamageDealt) : double.NaN;
            report.AveragePlayerDamageOnLoss = losses.Count > 0 ? losses.Average(e => e.PlayerDamageDealt) : double.NaN;
            report.AveragePlayerDpsOnWin = wins.Count > 0 ? wins.Average(e => e.PlayerDpsVersusTime) : double.NaN;
            report.AveragePlayerDpsOnLoss = losses.Count > 0 ? losses.Average(e => e.PlayerDpsVersusTime) : double.NaN;
            report.AveragePlayerHpRemainingOnWin = wins.Count > 0 ? wins.Average(e => (double)e.PlayerFinalHealth) : double.NaN;
            report.AveragePlayerHpRemainingOnLoss = losses.Count > 0 ? losses.Average(e => (double)e.PlayerFinalHealth) : double.NaN;

            foreach (var e in samples)
            {
                int comboBucket = Math.Min(ComboHistogramMaxBucket, Math.Max(0, e.PlayerComboCount));
                report.PlayerComboCountHistogram.TryGetValue(comboBucket, out int hc);
                report.PlayerComboCountHistogram[comboBucket] = hc + 1;

                string turnKey = TurnBucketLabel(e.Turns);
                report.TurnCountHistogram.TryGetValue(turnKey, out int tc);
                report.TurnCountHistogram[turnKey] = tc + 1;

                if (e.TerminalReason.HasValue)
                {
                    var k = e.TerminalReason.Value;
                    report.TerminalReasonCounts.TryGetValue(k, out int tr);
                    report.TerminalReasonCounts[k] = tr + 1;
                }

                int streakBucket = Math.Min(MaxComboStreakHistogramBucket, Math.Max(0, e.PlayerMaxComboStreak));
                report.PlayerMaxComboStreakHistogram.TryGetValue(streakBucket, out int sh);
                report.PlayerMaxComboStreakHistogram[streakBucket] = sh + 1;

                foreach (var kv in e.PlayerComboStreakRunCounts)
                {
                    report.PlayerComboStreakRunTotals.TryGetValue(kv.Key, out int rt);
                    report.PlayerComboStreakRunTotals[kv.Key] = rt + kv.Value;
                }
            }
        }

        /// <summary>Formats <paramref name="report"/> as plain text for the report dialog.</summary>
        public static string FormatReportText(ActionLabEncounterSimulationReport report, LabCombatSnapshot? snapshot = null)
        {
            var lines = new List<string>();
            if (snapshot != null)
            {
                lines.Add("Setup");
                lines.Add($"  Forced catalog action: {snapshot.SelectedCatalogActionName}");
                lines.Add($"  Hero combo strip: {snapshot.ComboStripActionNames.Count} action(s)");
                lines.Add(string.IsNullOrEmpty(snapshot.SessionEnemyLoaderType)
                    ? "  Enemy: default test dummy (lab baseline stats)"
                    : $"  Enemy: loader '{snapshot.SessionEnemyLoaderType}' level {snapshot.EnemyLevel}");
                lines.Add("");
            }

            lines.Add("Summary");
            lines.Add($"  Encounters (total): {report.EncounterCount}");
            lines.Add($"  Successful (no sim error): {report.EncounterCount - report.ErroredEncounters}");
            lines.Add($"  Errored / aborted: {report.ErroredEncounters}");
            lines.Add($"  Player wins: {report.PlayerWins} ({report.WinRate:P1} of successful)");
            lines.Add("");

            lines.Add("Tempo (turns)");
            lines.Add($"  Mean: {report.AverageTurns:F2}  Median: {report.MedianTurns:F2}  Std dev: {report.StdDevTurns:F2}");
            lines.Add($"  Min: {report.MinTurns}  Max: {report.MaxTurns}");
            lines.Add($"  Mean turns on win: {FmtMaybeNaN(report.AverageTurnsOnWin, "F2")}  on loss: {FmtMaybeNaN(report.AverageTurnsOnLoss, "F2")}");
            lines.Add("");

            lines.Add("Turn count histogram (successful encounters)");
            foreach (var key in TurnHistogramBucketOrder)
            {
                if (!report.TurnCountHistogram.TryGetValue(key, out int n) || n == 0)
                    continue;
                int ok = report.EncounterCount - report.ErroredEncounters;
                double pct = ok > 0 ? n / (double)ok : 0;
                lines.Add($"  {key,-7}: {n} ({pct:P1})");
            }

            lines.Add("");
            lines.Add("Damage and DPS (player)");
            lines.Add($"  Damage — mean: {report.AveragePlayerDamage:F1}  median: {report.MedianPlayerDamage:F1}  std dev: {report.StdDevPlayerDamage:F1}");
            lines.Add($"  DPS — mean: {report.AveragePlayerDps:F2}  median: {report.MedianPlayerDps:F2}  std dev: {report.StdDevPlayerDps:F2}");
            lines.Add($"  Mean damage on win: {FmtMaybeNaN(report.AveragePlayerDamageOnWin, "F1")}  on loss: {FmtMaybeNaN(report.AveragePlayerDamageOnLoss, "F1")}");
            lines.Add($"  Mean DPS on win: {FmtMaybeNaN(report.AveragePlayerDpsOnWin, "F2")}  on loss: {FmtMaybeNaN(report.AveragePlayerDpsOnLoss, "F2")}");
            lines.Add($"  Combat game time — median: {report.MedianCombatGameTime:F3}  std dev: {report.StdDevCombatGameTime:F3}");
            lines.Add("");

            lines.Add("HP remaining (end of fight)");
            lines.Add($"  Mean player HP on win: {FmtMaybeNaN(report.AveragePlayerHpRemainingOnWin, "F1")}  on loss: {FmtMaybeNaN(report.AveragePlayerHpRemainingOnLoss, "F1")}");
            lines.Add("");

            lines.Add("Enemy (from narrative)");
            lines.Add($"  Mean damage to player: {report.AverageEnemyDamage:F1}");
            lines.Add($"  Mean enemy combo events: {report.AverageEnemyComboCount:F2}");
            lines.Add("");

            lines.Add("Crits");
            lines.Add($"  Mean crits per encounter: {report.AverageCritsPerEncounter:F2}");
            lines.Add($"  Crit rate (all player damage events): {report.CritRatePerDamageEvent:P1}");
            lines.Add("");

            lines.Add("Player combo events per encounter (successful player actions with IsCombo)");
            for (int b = 0; b <= ComboHistogramMaxBucket; b++)
            {
                if (!report.PlayerComboCountHistogram.TryGetValue(b, out int n) || n == 0)
                    continue;
                string label = b == ComboHistogramMaxBucket ? $"{ComboHistogramMaxBucket}+" : b.ToString();
                int ok = report.EncounterCount - report.ErroredEncounters;
                double pct = ok > 0 ? n / (double)ok : 0;
                lines.Add($"  {label,3}: {n} ({pct:P1})");
            }

            int okEnc = report.EncounterCount - report.ErroredEncounters;
            var streakSamples = report.Encounters.Where(e => string.IsNullOrEmpty(e.ErrorMessage)).ToList();
            lines.Add("");
            lines.Add("Player combo chains (consecutive successful combo-flagged actions)");
            if (okEnc > 0 && streakSamples.Count > 0)
            {
                lines.Add("  P(encounter had longest chain ≥ N) — among successful encounters:");
                for (int n = 2; n <= 10; n++)
                {
                    int c = streakSamples.Count(e => e.PlayerMaxComboStreak >= n);
                    lines.Add($"    ≥{n}: {c} ({c / (double)okEnc:P1})");
                }

                lines.Add($"  Mean longest chain per encounter: {report.AveragePlayerMaxComboStreak:F2}");

                lines.Add("  Longest chain in encounter (histogram, successful encounters)");
                for (int b = 0; b <= MaxComboStreakHistogramBucket; b++)
                {
                    if (!report.PlayerMaxComboStreakHistogram.TryGetValue(b, out int hn) || hn == 0)
                        continue;
                    string label = b == MaxComboStreakHistogramBucket ? $"{MaxComboStreakHistogramBucket}+" : b.ToString();
                    lines.Add($"    {label,3}: {hn} ({hn / (double)okEnc:P1})");
                }

                int totalRuns = report.PlayerComboStreakRunTotals.Values.Sum();
                lines.Add("  Pooled combo runs (each chain of length ≥ 2 counts once; share of all such runs):");
                if (totalRuns > 0)
                {
                    foreach (var len in report.PlayerComboStreakRunTotals.Keys.OrderBy(k => k))
                    {
                        int rc = report.PlayerComboStreakRunTotals[len];
                        lines.Add($"    length {len}: {rc} ({rc / (double)totalRuns:P1} of runs)");
                    }
                }
                else
                    lines.Add("    (no chains of length ≥ 2 in sample)");
            }
            else
                lines.Add("  (no successful encounters to summarize)");

            if (report.TerminalReasonCounts.Count > 0)
            {
                lines.Add("");
                lines.Add("Last AdvanceSingleTurnAsync result (all encounters)");
                foreach (var kv in report.TerminalReasonCounts.OrderBy(k => k.Key.ToString()))
                    lines.Add($"  {kv.Key}: {kv.Value}");
            }

            var errors = report.Encounters.Select((e, i) => (i, e.ErrorMessage)).Where(x => !string.IsNullOrEmpty(x.ErrorMessage)).Take(12).ToList();
            if (errors.Count > 0)
            {
                lines.Add("");
                lines.Add("Sample errors:");
                foreach (var (idx, msg) in errors)
                    lines.Add($"  #{idx + 1}: {msg}");
            }

            if (report.SimulationWallElapsed > TimeSpan.Zero)
            {
                lines.Add("");
                lines.Add($"Simulation wall time: {FormatSimulationWallTime(report.SimulationWallElapsed)}");
            }

            return string.Join(global::System.Environment.NewLine, lines);
        }

        private static string FormatSimulationWallTime(TimeSpan elapsed)
        {
            if (elapsed.TotalHours >= 1.0)
                return $"{(int)elapsed.TotalHours} h {elapsed.Minutes} min {elapsed.Seconds}.{elapsed.Milliseconds:D3} s";
            if (elapsed.TotalMinutes >= 1.0)
                return $"{(int)elapsed.TotalMinutes} min {elapsed.Seconds}.{elapsed.Milliseconds:D3} s";
            return $"{elapsed.TotalSeconds:F2} s";
        }

        private static string FmtMaybeNaN(double value, string format)
        {
            if (double.IsNaN(value))
                return "n/a";
            return value.ToString(format);
        }

        private static string TurnBucketLabel(int turns)
        {
            if (turns <= 5) return "1-5";
            if (turns <= 10) return "6-10";
            if (turns <= 20) return "11-20";
            if (turns <= 40) return "21-40";
            return "41+";
        }

        private static double Median(IReadOnlyList<double> values)
        {
            if (values.Count == 0)
                return 0;
            var sorted = values.OrderBy(x => x).ToList();
            int mid = sorted.Count / 2;
            if ((sorted.Count & 1) == 1)
                return sorted[mid];
            return 0.5 * (sorted[mid - 1] + sorted[mid]);
        }

        private static double PopulationStdDev(IReadOnlyList<double> values)
        {
            if (values.Count == 0)
                return 0;
            double mean = values.Average();
            double variance = values.Sum(x => (x - mean) * (x - mean)) / values.Count;
            return Math.Sqrt(variance);
        }

        public static async Task<EncounterMetrics> RunSingleEncounterAsync(LabCombatSnapshot snapshot, Random rng)
        {
            var metrics = new EncounterMetrics { PlayerWon = false };
            Character? player = null;
            Enemy? enemy = null;
            CombatManager? combatManager = null;
            var thresholdManager = RollModificationManager.GetThresholdManager();
            bool narrativeStarted = false;

            using (GameTicker.BeginIsolatedEncounterGameTime())
            {
                try
                {
                    Environment? room;
                    (player, enemy, room) = BuildFightEntities(snapshot);
                    combatManager = new CombatManager();

                    GameTicker.Instance.Reset();
                    player.ComboStep = 0;
                    enemy.ComboStep = 0;
                    combatManager.StartBattleNarrative(player.Name, enemy.Name, room.Name, player.CurrentHealth, enemy.CurrentHealth);
                    narrativeStarted = true;
                    combatManager.InitializeCombatEntities(player, enemy, room, playerGetsFirstAttack: true, enemyGetsFirstAttack: false);
                    room.ResetForNewFight();
                    Dice.ClearAsyncLabEncounterTestRoll();
                    Dice.ClearTestRoll();

                    var forced = ActionLoader.GetAction(snapshot.SelectedCatalogActionName.Trim());
                    if (forced == null)
                    {
                        metrics.ErrorMessage = $"Unknown action '{snapshot.SelectedCatalogActionName}'.";
                        return metrics;
                    }

                    if (!forced.IsComboAction)
                        forced.IsComboAction = true;

                    int safety = 0;
                    while (player.IsAlive && enemy.IsAlive && safety++ < 50_000)
                    {
                        int d20 = rng.Next(1, 21);
                        Dice.SetAsyncLabEncounterTestRoll(d20);
                        ActionSelector.SetStoredActionRoll(player, d20);
                        ActionSelector.SetStoredActionRoll(enemy, d20);

                        Action? forcedForPlayer = ActionSelector.WouldNaturalRollSelectComboAction(player, d20) ? forced : null;
                        CombatSingleTurnResult step;
                        try
                        {
                            step = await combatManager.AdvanceSingleTurnAsync(player, enemy, room, forcedForPlayer).ConfigureAwait(false);
                        }
                        finally
                        {
                            Dice.ClearAsyncLabEncounterTestRoll();
                        }

                        if (step == CombatSingleTurnResult.Advanced)
                            continue;

                        metrics.TerminalReason = step;
                        if (step == CombatSingleTurnResult.EnemyDefeated)
                            metrics.PlayerWon = player.IsAlive;
                        else if (step == CombatSingleTurnResult.PlayerDefeated)
                            metrics.PlayerWon = false;
                        else
                            metrics.PlayerWon = false;

                        break;
                    }

                    if (safety >= 50_000)
                    {
                        metrics.ErrorMessage = "Encounter exceeded iteration safety cap.";
                        metrics.TerminalReason = CombatSingleTurnResult.LoopLimitExceeded;
                        metrics.PlayerWon = false;
                    }
                    else if (!metrics.TerminalReason.HasValue)
                    {
                        if (!player.IsAlive)
                        {
                            metrics.TerminalReason = CombatSingleTurnResult.PlayerDefeated;
                            metrics.PlayerWon = false;
                        }
                        else if (!enemy.IsAlive)
                        {
                            metrics.TerminalReason = CombatSingleTurnResult.EnemyDefeated;
                            metrics.PlayerWon = true;
                        }
                    }

                    if (narrativeStarted && combatManager != null)
                    {
                        combatManager.EndBattleNarrative(player, enemy);
                        narrativeStarted = false;
                    }

                    FillMetricsFromCombat(combatManager!, player, enemy, metrics);
                }
                catch (Exception ex)
                {
                    metrics.ErrorMessage = $"{ex.GetType().Name}: {ex.Message}";
                }
                finally
                {
                    Dice.ClearAsyncLabEncounterTestRoll();
                    Dice.ClearTestRoll();
                    ActionSelector.RemoveStoredRoll(player);
                    ActionSelector.RemoveStoredRoll(enemy);
                    if (narrativeStarted && combatManager != null && player != null && enemy != null)
                    {
                        try
                        {
                            combatManager.EndBattleNarrative(player, enemy);
                        }
                        catch
                        {
                            /* best-effort */
                        }
                    }

                    if (player != null)
                        thresholdManager.ResetThresholds(player);
                    if (enemy != null)
                        thresholdManager.ResetThresholds(enemy);
                }

                return metrics;
            }
        }

        private static void FillMetricsFromCombat(CombatManager combatManager, Character player, Enemy enemy, EncounterMetrics metrics)
        {
            var narrative = combatManager.GetCurrentBattleNarrative();
            int currentTurn = combatManager.GetCurrentTurn();
            int totalActionCount = combatManager.GetTotalActionCount();

            int turnCount = 0;
            if (narrative != null)
            {
                var events = narrative.GetAllEvents();
                var stats = BattleNarrativeGenerator.CalculateStatistics(events, player.Name, enemy.Name);
                metrics.PlayerDamageDealt = stats.TotalPlayerDamage;
                metrics.PlayerComboCount = stats.PlayerComboCount;
                var streakStats = BattleNarrativeGenerator.CalculatePlayerComboStreakStatistics(events, player.Name, enemy.Name);
                metrics.PlayerMaxComboStreak = streakStats.MaxStreak;
                foreach (var kv in streakStats.RunCountsByLength)
                    metrics.PlayerComboStreakRunCounts[kv.Key] = kv.Value;
                metrics.EnemyDamageDealt = stats.TotalEnemyDamage;
                metrics.EnemyComboCount = stats.EnemyComboCount;

                var playerDamageToEnemy = events
                    .Where(e => e.Actor == player.Name && e.Target == enemy.Name && e.Damage > 0)
                    .ToList();
                metrics.PlayerDamageEvents = playerDamageToEnemy.Count;
                metrics.PlayerCritCount = playerDamageToEnemy.Count(e => e.IsCritical);

                if (currentTurn > 0)
                    turnCount = currentTurn;
                else if (totalActionCount > 0)
                    turnCount = totalActionCount;
                else
                {
                    int pa = events.Count(e => e.Actor == player.Name && e.Target == enemy.Name);
                    int ea = events.Count(e => e.Actor == enemy.Name && e.Target == player.Name);
                    turnCount = Math.Max(1, pa + ea);
                }
            }
            else
            {
                metrics.PlayerDamageDealt = Math.Max(0, enemy.MaxHealth - enemy.CurrentHealth);
                metrics.EnemyDamageDealt = Math.Max(0, player.MaxHealth - player.CurrentHealth);
                turnCount = currentTurn > 0 ? currentTurn : totalActionCount > 0 ? totalActionCount : 1;
            }

            metrics.Turns = turnCount;
            metrics.PlayerFinalHealth = player.CurrentHealth;
            metrics.EnemyFinalHealth = enemy.CurrentHealth;

            var speed = combatManager.GetCurrentActionSpeedSystem();
            double t = speed?.GetCurrentTime() ?? 0.0;
            metrics.CombatGameTime = t;
            const double epsilon = 1e-6;
            metrics.PlayerDpsVersusTime = t > epsilon ? metrics.PlayerDamageDealt / t : metrics.PlayerDamageDealt;
        }

        private static (Character player, Enemy enemy, Environment room) BuildFightEntities(LabCombatSnapshot snapshot)
        {
            var serializer = new CharacterSerializer();
            var data = serializer.Deserialize(snapshot.InitialPlayerJson)
                ?? throw new InvalidOperationException("Lab sim: invalid initial player JSON.");
            var player = serializer.CreateCharacterFromSaveData(data);

            ApplyLabPanelDeltas(player, snapshot);
            ReapplyComboStrip(player, snapshot.ComboStripActionNames);

            Enemy enemy;
            if (!string.IsNullOrEmpty(snapshot.SessionEnemyLoaderType))
            {
                EnemyLoader.LoadEnemies();
                enemy = EnemyLoader.CreateEnemy(snapshot.SessionEnemyLoaderType.Trim(), snapshot.EnemyLevel)
                    ?? TestCharacterFactory.CreateTestEnemy(LabCombatSnapshot.DefaultTestEnemyBattleConfig, 0, snapshot.EnemyLevel);
            }
            else
            {
                enemy = TestCharacterFactory.CreateTestEnemy(LabCombatSnapshot.DefaultTestEnemyBattleConfig, 0, snapshot.EnemyLevel);
            }

            var room = TestCharacterFactory.CreateTestEnvironment();
            return (player, enemy, room);
        }

        private static void ApplyLabPanelDeltas(Character labPlayer, LabCombatSnapshot snapshot)
        {
            if (snapshot.LabPanelLevelDelta != 0)
                labPlayer.ApplyActionLabLevelDelta(snapshot.LabPanelLevelDelta);
            if (snapshot.LabPanelStrDelta != 0)
                labPlayer.Stats.Strength = Math.Max(1, labPlayer.Stats.Strength + snapshot.LabPanelStrDelta);
            if (snapshot.LabPanelAgiDelta != 0)
                labPlayer.Stats.Agility = Math.Max(1, labPlayer.Stats.Agility + snapshot.LabPanelAgiDelta);
            if (snapshot.LabPanelTecDelta != 0)
                labPlayer.Stats.Technique = Math.Max(1, labPlayer.Stats.Technique + snapshot.LabPanelTecDelta);
            if (snapshot.LabPanelIntDelta != 0)
                labPlayer.Stats.Intelligence = Math.Max(1, labPlayer.Stats.Intelligence + snapshot.LabPanelIntDelta);
            if (snapshot.LabPanelArmorDelta != 0)
                labPlayer.ActionLabArmorBonus = Math.Max(0, labPlayer.ActionLabArmorBonus + snapshot.LabPanelArmorDelta);
        }

        private static void ReapplyComboStrip(Character labPlayer, IReadOnlyList<string> orderedActionNames)
        {
            foreach (var a in labPlayer.GetComboActions().ToList())
                labPlayer.RemoveFromCombo(a, ignoreWeaponRequirement: true);

            int nextSlot = 1;
            foreach (var name in orderedActionNames)
            {
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                var poolEntry = labPlayer.ActionPool.FirstOrDefault(item =>
                    item.action != null && string.Equals(item.action.Name, name, StringComparison.OrdinalIgnoreCase));
                Action? act = poolEntry.action;
                if (act == null)
                    act = ActionLoader.GetAction(name);
                if (act == null)
                    continue;
                if (!act.IsComboAction)
                    act.IsComboAction = true;
                act.ComboOrder = nextSlot++;
                labPlayer.AddToCombo(act);
            }
        }
    }
}
