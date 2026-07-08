using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RPGGame.Actions.RollModification;
using RPGGame.BattleStatistics;
using RPGGame.Combat;
using RPGGame.Tuning;

namespace RPGGame.ActionInteractionLab
{
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
            int maxDegreeOfParallelism = -1,
            IProgress<(int completed, int total, string status)>? progress = null,
            bool continuePastZeroHp = false)
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

            using (CombatUiMuteScope.Begin(muted: true))
            {
                if (dop <= 1)
                {
                    using (DeveloperSimMode.BeginScope(continuePastZeroHp))
                    {
                        for (int i = 0; i < encounterCount; i++)
                        {
                            var m = await RunSingleEncounterAsync(snapshot, rng).ConfigureAwait(false);
                            report.Encounters.Add(m);
                            ReportBatchProgress(progress, i + 1, encounterCount);
                        }
                    }
                }
                else
                {
                    int batchSeed = rng.Next();
                    var results = new EncounterMetrics[encounterCount];
                    int completed = 0;
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
                                using (DeveloperSimMode.BeginScope(continuePastZeroHp))
                                    results[i] = await RunSingleEncounterAsync(snapshot, encRng).ConfigureAwait(false);
                            }
                            finally
                            {
                                EncounterExecutionMutex.Release();
                                int done = Interlocked.Increment(ref completed);
                                ReportBatchProgress(progress, done, encounterCount);
                            }
                        }).ConfigureAwait(false);

                    foreach (var m in results)
                        report.Encounters.Add(m);
                }
            }

            Aggregate(report);
            wall.Stop();
            report.SimulationWallElapsed = wall.Elapsed;
            return report;
        }

        private static void ReportBatchProgress(
            IProgress<(int completed, int total, string status)>? progress,
            int completed,
            int total)
        {
            if (progress == null)
                return;

            int interval = SimulationProgressReporter.GetReportInterval(total);
            if (completed < total && completed > 0 && completed % interval != 0)
                return;

            progress.Report((completed, total, $"Encounter {completed}/{total}"));
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
                int comboBucket = Math.Min(ActionLabEncounterReportFormatter.ComboHistogramMaxBucket, Math.Max(0, e.PlayerComboCount));
                report.PlayerComboCountHistogram.TryGetValue(comboBucket, out int hc);
                report.PlayerComboCountHistogram[comboBucket] = hc + 1;

                string turnKey = ActionLabEncounterReportFormatter.TurnBucketLabel(e.Turns);
                report.TurnCountHistogram.TryGetValue(turnKey, out int tc);
                report.TurnCountHistogram[turnKey] = tc + 1;

                if (e.TerminalReason.HasValue)
                {
                    var k = e.TerminalReason.Value;
                    report.TerminalReasonCounts.TryGetValue(k, out int tr);
                    report.TerminalReasonCounts[k] = tr + 1;
                }

                int streakBucket = Math.Min(ActionLabEncounterReportFormatter.MaxComboStreakHistogramBucket, Math.Max(0, e.PlayerMaxComboStreak));
                report.PlayerMaxComboStreakHistogram.TryGetValue(streakBucket, out int sh);
                report.PlayerMaxComboStreakHistogram[streakBucket] = sh + 1;

                foreach (var kv in e.PlayerComboStreakRunCounts)
                {
                    report.PlayerComboStreakRunTotals.TryGetValue(kv.Key, out int rt);
                    report.PlayerComboStreakRunTotals[kv.Key] = rt + kv.Value;
                }
            }
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
                    Dice.ClearAsyncForcedD20Rolls();
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
                    int advanceCalls = 0;
                    int playerMinHealth = player.MaxHealth;
                    int turnsBelowZero = 0;
                    int maxAdvances = 50_000;

                    while (DeveloperSimMode.ShouldContinueEncounter(player.CurrentHealth, enemy.CurrentHealth, enemy.IsAlive)
                           && safety++ < maxAdvances)
                    {
                        if (player.CurrentHealth < playerMinHealth)
                            playerMinHealth = player.CurrentHealth;

                        int d20 = rng.Next(1, 21);
                        Dice.QueueAsyncForcedD20Rolls(d20);
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
                            Dice.ClearAsyncForcedD20Rolls();
                        }

                        advanceCalls++;

                        if (player.CurrentHealth <= 0)
                            turnsBelowZero++;

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

                    if (safety >= maxAdvances)
                    {
                        metrics.ErrorMessage = "Encounter exceeded iteration safety cap.";
                        metrics.TerminalReason = CombatSingleTurnResult.LoopLimitExceeded;
                        metrics.PlayerWon = enemy.CurrentHealth <= 0 && player.CurrentHealth > DeveloperSimMode.NegativeHpFloor;
                    }
                    else if (!metrics.TerminalReason.HasValue)
                    {
                        if (DeveloperSimMode.ContinuePastZeroHp && player.CurrentHealth <= DeveloperSimMode.NegativeHpFloor)
                        {
                            metrics.TerminalReason = CombatSingleTurnResult.PlayerDefeated;
                            metrics.PlayerWon = false;
                        }
                        else if (!player.IsAlive && !DeveloperSimMode.ContinuePastZeroHp)
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

                    metrics.PlayerMinHealth = playerMinHealth;
                    metrics.TurnsBelowZero = turnsBelowZero;
                    metrics.LossSeverityScore = ComputeLossSeverity(playerMinHealth, turnsBelowZero);

                    if (narrativeStarted && combatManager != null)
                    {
                        combatManager.EndBattleNarrative(player, enemy);
                        narrativeStarted = false;
                    }

                    FillMetricsFromCombat(combatManager!, player, enemy, metrics);
                    metrics.SimAdvanceCalls = advanceCalls;
                }
                catch (Exception ex)
                {
                    metrics.ErrorMessage = $"{ex.GetType().Name}: {ex.Message}";
                }
                finally
                {
                    Dice.ClearAsyncForcedD20Rolls();
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

                    combatManager?.Cleanup();
                }

                return metrics;
            }
        }

        private static double ComputeLossSeverity(int playerMinHealth, int turnsBelowZero)
        {
            double hpPenalty = playerMinHealth < 0 ? Math.Abs(playerMinHealth) : 0;
            return hpPenalty + turnsBelowZero * 5.0;
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
                metrics.PlayerMissCount = events.Count(e =>
                    e.Actor == player.Name && e.Target == enemy.Name && !e.IsSuccess);

                int playerActions = events.Count(e =>
                    e.Actor == player.Name && e.Target == enemy.Name && e.IsSuccess);
                int enemyActions = events.Count(e =>
                    e.Actor == enemy.Name && e.Target == player.Name && e.IsSuccess);

                if (currentTurn > 0)
                {
                    metrics.PlayerTurns = currentTurn;
                    metrics.EnemyTurns = Math.Max(0, currentTurn - (metrics.PlayerWon ? 1 : 0));
                    turnCount = metrics.PlayerTurns + metrics.EnemyTurns;
                }
                else if (playerActions + enemyActions > 0)
                {
                    metrics.PlayerTurns = playerActions;
                    metrics.EnemyTurns = enemyActions;
                    turnCount = playerActions + enemyActions;
                }
                else if (currentTurn > 0)
                {
                    metrics.PlayerTurns = currentTurn;
                    turnCount = currentTurn;
                }
                else if (totalActionCount > 0)
                {
                    turnCount = totalActionCount;
                }
            }
            else
            {
                metrics.PlayerDamageDealt = Math.Max(0, enemy.MaxHealth - enemy.CurrentHealth);
                metrics.EnemyDamageDealt = Math.Max(0, player.MaxHealth - player.CurrentHealth);
                turnCount = totalActionCount > 0 ? totalActionCount : currentTurn > 0 ? currentTurn : 1;
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
            var player = LabCombatEntityFactory.ClonePlayerFromJson(
                snapshot.InitialPlayerJson,
                "Lab sim: invalid initial player JSON.");
            LabCombatEntityFactory.ApplyPanelDeltas(player, snapshot);
            LabCombatEntityFactory.ReapplyComboStrip(player, snapshot.ComboStripActionNames);
            var enemy = LabCombatEntityFactory.BuildLabEnemy(
                snapshot.SessionEnemyLoaderType,
                snapshot.EnemyLevel,
                snapshot.LabEnemyBattleConfig ?? LabCombatSnapshot.DefaultTestEnemyBattleConfig);
            var room = TestCharacterFactory.CreateTestEnvironment();
            return (player, enemy, room);
        }
    }
}
