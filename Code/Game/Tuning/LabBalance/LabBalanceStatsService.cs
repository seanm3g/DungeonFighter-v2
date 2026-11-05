using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RPGGame.ActionInteractionLab;
using RPGGame.Tuning.Profiles;


namespace RPGGame.Tuning.LabBalance
{
    public sealed class LabBalanceStatsRunResult
    {
        public LabBalanceProcessLayer Layer { get; init; }
        public ActionLabEncounterSimulationReport? EncounterReport { get; init; }
        public ActionLabDungeonSimulationReport? DungeonReport { get; init; }
        public LabBalanceTargetEvaluation? Targets { get; init; }
        public LabBalanceReportDelta? Delta { get; init; }
        public IReadOnlyList<FundamentalsLevelSnapshot> AnchorSnapshots { get; init; } =
            Array.Empty<FundamentalsLevelSnapshot>();
        public IReadOnlyList<(string Weapon, double WinRate, double MedianTurns)> WeaponRows { get; init; } =
            Array.Empty<(string, double, double)>();
        public string SummaryText { get; init; } = "";
        public bool DeepLinkOnly { get; init; }
    }

    /// <summary>
    /// Batch stats + one-knob suggest/apply for Action Lab Balance layers.
    /// </summary>
    public static class LabBalanceStatsService
    {
        public static ActionLabEncounterSimulationReport? LastEncounterReport { get; private set; }
        public static ActionLabEncounterSimulationReport? PreviousEncounterReport { get; private set; }

        public static void ClearHistory()
        {
            LastEncounterReport = null;
            PreviousEncounterReport = null;
        }

        public static async Task<LabBalanceStatsRunResult> RunStatsAsync(
            ActionInteractionLabSession session,
            int batchCount,
            int maxDegreeOfParallelism = 1,
            IProgress<(int completed, int total, string status)>? progress = null)
        {
            var def = LabBalanceLayerDefinition.Get(session.ActiveBalanceProcessLayer);
            if (def.StatsMode == LabBalanceStatsMode.DeepLinkOnly)
            {
                return new LabBalanceStatsRunResult
                {
                    Layer = def.Layer,
                    DeepLinkOnly = true,
                    SummaryText =
                        "Playthrough check runs on Balance Tuning Workbench.\n" +
                        $"Open profile '{def.WorkbenchProfileId}' (Simulate → Analyze → Apply)."
                };
            }

            if (def.StatsMode == LabBalanceStatsMode.Dungeon)
            {
                var snapshot = session.CaptureSimulationSnapshot();
                var validation = ActionLabDungeonSimulator.ValidateSnapshot(snapshot);
                if (validation != null)
                {
                    return new LabBalanceStatsRunResult
                    {
                        Layer = def.Layer,
                        SummaryText = validation
                    };
                }

                string dungeonKey = session.LabDungeonCatalogKey;
                if (string.IsNullOrWhiteSpace(dungeonKey))
                {
                    var names = ActionLabDungeonFactory.ListCatalogDungeonNames();
                    dungeonKey = names.Count > 0 ? names[0] : "Forest";
                }

                int dungeonLevel = Math.Clamp(session.LabPlayer.Level + session.LabDungeonLevelDelta, 1, 99);
                var dungReport = await ActionLabDungeonSimulator.RunBatchAsync(
                    snapshot,
                    dungeonKey,
                    dungeonLevel,
                    session.LabDungeonSeed,
                    batchCount,
                    varySeedPerRun: true,
                    maxDegreeOfParallelism: maxDegreeOfParallelism).ConfigureAwait(false);

                var dungTargets = LabBalanceTargetEvaluator.EvaluateDungeon(def, dungReport);
                string dungBody = ActionLabDungeonSimulator.FormatReportText(dungReport)
                                  + System.Environment.NewLine + System.Environment.NewLine
                                  + dungTargets.FormatSummary();

                return new LabBalanceStatsRunResult
                {
                    Layer = def.Layer,
                    DungeonReport = dungReport,
                    Targets = dungTargets,
                    SummaryText = dungBody
                };
            }

            if (def.StatsMode == LabBalanceStatsMode.MultiAnchorEncounter)
            {
                return await RunMultiAnchorAsync(session, def, batchCount, maxDegreeOfParallelism, progress)
                    .ConfigureAwait(false);
            }

            if (def.StatsMode == LabBalanceStatsMode.WeaponMatrixEncounter)
            {
                return await RunWeaponMatrixAsync(session, def, batchCount, maxDegreeOfParallelism, progress)
                    .ConfigureAwait(false);
            }

            return await RunSingleEncounterAsync(session, def, batchCount, maxDegreeOfParallelism, progress,
                continuePastZeroHp: def.ContinuePastZeroHp).ConfigureAwait(false);
        }

        internal static string FormatChecklistProgress(
            LabBalanceTargetEvaluation? start,
            LabBalanceTargetEvaluation? end)
        {
            if (start == null && end == null)
                return "(no checklist data)";

            if (start == null || end == null)
            {
                var only = end ?? start!;
                return $"{only.PassedCount}/{only.Checks.Count} passed (one-sided)";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Passed: {start.PassedCount}/{start.Checks.Count} → {end.PassedCount}/{end.Checks.Count}");

            var endByName = end.Checks.ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);
            foreach (var s in start.Checks)
            {
                if (!endByName.TryGetValue(s.Name, out var e))
                {
                    sb.AppendLine($"  {s.Name}: {(s.Passed ? "PASS" : "FAIL")} {s.ActualText} → (missing at end)");
                    continue;
                }

                string from = $"{(s.Passed ? "PASS" : "FAIL")} {s.ActualText}";
                string to = $"{(e.Passed ? "PASS" : "FAIL")} {e.ActualText}";
                string mark = s.Passed == e.Passed
                    ? (s.Passed ? "=" : "…")
                    : (e.Passed ? "↑" : "↓");
                sb.AppendLine($"  {mark} {s.Name}: {from} → {to}");
            }

            foreach (var e in end.Checks)
            {
                if (start.Checks.All(s => !string.Equals(s.Name, e.Name, StringComparison.OrdinalIgnoreCase)))
                    sb.AppendLine($"  + {e.Name}: {(e.Passed ? "PASS" : "FAIL")} {e.ActualText} (new)");
            }

            return sb.ToString().TrimEnd();
        }

        internal static string FormatKnobDelta(
            IReadOnlyDictionary<string, double> start,
            IReadOnlyDictionary<string, double> end)
        {
            var sb = new StringBuilder();
            foreach (var key in start.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
            {
                if (!end.TryGetValue(key, out double endVal))
                    continue;
                double startVal = start[key];
                if (Math.Abs(startVal - endVal) < 1e-9)
                    continue;
                sb.AppendLine($"  {key}: {startVal:F3} → {endVal:F3}");
            }

            foreach (var key in end.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
            {
                if (start.ContainsKey(key))
                    continue;
                sb.AppendLine($"  {key}: (new) → {end[key]:F3}");
            }

            return sb.ToString().TrimEnd();
        }

        private static Dictionary<string, double> SnapshotLayerKnobValues(LabBalanceProcessLayer layer)
        {
            var dict = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var knobs in LabBalanceScenarioApplicator.GetKnobsForLayer(layer))
                dict[knobs.Id] = knobs.GetValue();
            return dict;
        }

        private static async Task<LabBalanceStatsRunResult> RunSingleEncounterAsync(
            ActionInteractionLabSession session,
            LabBalanceLayerDefinition def,
            int batchCount,
            int maxDegreeOfParallelism,
            IProgress<(int completed, int total, string status)>? progress,
            bool continuePastZeroHp)
        {
            var snapshot = session.CaptureSimulationSnapshot();
            var validation = ActionLabEncounterSimulator.ValidateSnapshot(snapshot);
            if (validation != null)
            {
                return new LabBalanceStatsRunResult
                {
                    Layer = def.Layer,
                    SummaryText = validation
                };
            }

            var report = await ActionLabEncounterSimulator.RunBatchAsync(
                snapshot,
                batchCount,
                Random.Shared,
                maxDegreeOfParallelism: maxDegreeOfParallelism,
                progress: progress,
                continuePastZeroHp: continuePastZeroHp).ConfigureAwait(false);

            session.RecordEncounterSimulationTurns(report);
            PreviousEncounterReport = LastEncounterReport;
            LastEncounterReport = report;

            int level = Math.Clamp(session.LabPlayer.Level, 1, 99);
            var targets = LabBalanceTargetEvaluator.EvaluateForLayer(def, report, level);
            var delta = LabBalanceTargetEvaluator.ComputeDelta(PreviousEncounterReport, report);

            string body = ActionLabEncounterReportFormatter.FormatReportText(report, snapshot)
                          + System.Environment.NewLine + System.Environment.NewLine
                          + targets.FormatSummary()
                          + System.Environment.NewLine
                          + delta.FormatSummary();

            return new LabBalanceStatsRunResult
            {
                Layer = def.Layer,
                EncounterReport = report,
                Targets = targets,
                Delta = delta,
                SummaryText = body
            };
        }

        private static async Task<LabBalanceStatsRunResult> RunMultiAnchorAsync(
            ActionInteractionLabSession session,
            LabBalanceLayerDefinition def,
            int batchCount,
            int maxDegreeOfParallelism,
            IProgress<(int completed, int total, string status)>? progress)
        {
            var anchors = def.LevelAnchors.Count > 0 ? def.LevelAnchors : new[] { 1, 10, 25 };
            int perAnchor = Math.Max(1, batchCount / anchors.Count);
            var snapshots = new List<FundamentalsLevelSnapshot>();
            ActionLabEncounterSimulationReport? last = null;
            var sb = new StringBuilder();
            sb.AppendLine($"Level curve — {perAnchor} fights per anchor");

            string weapon = InferWeaponType(session);
            string enemy = session.SessionEnemyLoaderType
                ?? FundamentalsCombatSetup.DefaultFundamentalsEnemyType;

            for (int i = 0; i < anchors.Count; i++)
            {
                int level = anchors[i];
                progress?.Report((i, anchors.Count, $"L{level}"));
                var labSnap = LabBalanceScenarioApplicator.BuildFundamentalsStyleSnapshot(
                    weapon, enemy, level, level);
                var report = await ActionLabEncounterSimulator.RunBatchAsync(
                    labSnap, perAnchor, Random.Shared, maxDegreeOfParallelism: maxDegreeOfParallelism,
                    continuePastZeroHp: false).ConfigureAwait(false);
                last = report;
                var config = new SimulationProfileConfig
                {
                    PlayerLevel = level,
                    EnemyLevel = level,
                    WeaponType = weapon,
                    EnemyType = enemy,
                    ForcedCatalogAction = labSnap.SelectedCatalogActionName
                };
                var fund = FundamentalsSimulationResult.FromReport(report, labSnap, config);
                var levelSnap = FundamentalsLevelSnapshot.FromResult(fund);
                snapshots.Add(levelSnap);
                sb.AppendLine(
                    $"  L{level}: median combined {levelSnap.MedianCombinedActions:F1}, WR {levelSnap.WinRate * 100:F0}%");
            }

            PreviousEncounterReport = LastEncounterReport;
            LastEncounterReport = last;
            LabBalanceTargetEvaluation? targets = null;
            LabBalanceReportDelta? delta = null;
            if (snapshots.Count > 0)
            {
                targets = LabBalanceTargetEvaluator.EvaluateAnchors(def, snapshots);
                if (last != null)
                    delta = LabBalanceTargetEvaluator.ComputeDelta(PreviousEncounterReport, last);
                sb.AppendLine();
                sb.AppendLine(targets.FormatSummary());
                if (delta != null)
                    sb.AppendLine(delta.FormatSummary());
            }

            return new LabBalanceStatsRunResult
            {
                Layer = def.Layer,
                EncounterReport = last,
                Targets = targets,
                Delta = delta,
                AnchorSnapshots = snapshots,
                SummaryText = sb.ToString().TrimEnd()
            };
        }

        private static async Task<LabBalanceStatsRunResult> RunWeaponMatrixAsync(
            ActionInteractionLabSession session,
            LabBalanceLayerDefinition def,
            int batchCount,
            int maxDegreeOfParallelism,
            IProgress<(int completed, int total, string status)>? progress)
        {
            var weapons = def.WeaponTypes.Count > 0
                ? def.WeaponTypes
                : new[] { "Mace", "Sword", "Dagger", "Wand" };
            int perWeapon = Math.Max(1, batchCount / weapons.Count);
            int level = Math.Clamp(def.DefaultPlayerLevel, 1, 99);
            string enemy = session.SessionEnemyLoaderType
                ?? FundamentalsCombatSetup.DefaultFundamentalsEnemyType;
            var rows = new List<(string Weapon, double WinRate, double MedianTurns)>();
            var sb = new StringBuilder();
            sb.AppendLine($"Weapon parity @ L{level} vs {enemy} — {perWeapon} fights each");
            ActionLabEncounterSimulationReport? last = null;

            for (int i = 0; i < weapons.Count; i++)
            {
                string wt = weapons[i];
                progress?.Report((i, weapons.Count, wt));
                var labSnap = LabBalanceScenarioApplicator.BuildFundamentalsStyleSnapshot(
                    wt, enemy, level, level);
                var report = await ActionLabEncounterSimulator.RunBatchAsync(
                    labSnap, perWeapon, Random.Shared, maxDegreeOfParallelism: maxDegreeOfParallelism)
                    .ConfigureAwait(false);
                last = report;
                rows.Add((wt, report.WinRate, report.MedianTurns));
                sb.AppendLine($"  {wt}: WR {report.WinRate * 100:F0}%, median turns {report.MedianTurns:F1}");
            }

            double spread = rows.Count > 0
                ? rows.Max(r => r.WinRate) - rows.Min(r => r.WinRate)
                : 0;
            sb.AppendLine($"Win-rate spread (best−worst): {spread * 100:F1}%");

            PreviousEncounterReport = LastEncounterReport;
            LastEncounterReport = last;
            var targets = LabBalanceTargetEvaluator.EvaluateWeaponMatrix(def, rows);
            sb.AppendLine();
            sb.AppendLine(targets.FormatSummary());

            return new LabBalanceStatsRunResult
            {
                Layer = def.Layer,
                EncounterReport = last,
                Targets = targets,
                WeaponRows = rows,
                SummaryText = sb.ToString().TrimEnd()
            };
        }

        private static bool TryParseDouble(string text, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text))
                return false;
            string cleaned = text.Trim().TrimEnd('%');
            return double.TryParse(cleaned, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out value);
        }

        private static string InferWeaponType(ActionInteractionLabSession session)
        {
            if (session.LabPlayer.Weapon is WeaponItem w)
                return w.WeaponType.ToString();
            return "Sword";
        }

        private static FundamentalsSimulationResult CloneWithSnapshots(
            FundamentalsSimulationResult source,
            IReadOnlyList<FundamentalsLevelSnapshot> snaps) =>
            new()
            {
                TimestampUtc = source.TimestampUtc,
                EncounterCount = source.EncounterCount,
                SuccessfulEncounters = source.SuccessfulEncounters,
                ErroredEncounters = source.ErroredEncounters,
                AverageActionsPerEncounter = source.AverageActionsPerEncounter,
                MedianActionsPerEncounter = source.MedianActionsPerEncounter,
                AveragePlayerTurnsPerEncounter = source.AveragePlayerTurnsPerEncounter,
                MedianPlayerTurnsPerEncounter = source.MedianPlayerTurnsPerEncounter,
                AverageEnemyTurnsPerEncounter = source.AverageEnemyTurnsPerEncounter,
                MedianEnemyTurnsPerEncounter = source.MedianEnemyTurnsPerEncounter,
                MinActions = source.MinActions,
                MaxActions = source.MaxActions,
                AverageComboPlusEventsPerEncounter = source.AverageComboPlusEventsPerEncounter,
                AverageComboStreakRuns2PlusPerEncounter = source.AverageComboStreakRuns2PlusPerEncounter,
                AverageMaxComboStreak = source.AverageMaxComboStreak,
                ComboStreakRunTotals = source.ComboStreakRunTotals,
                ForcedCatalogAction = source.ForcedCatalogAction,
                ComboStripCount = source.ComboStripCount,
                EnemyType = source.EnemyType,
                PlayerLevel = source.PlayerLevel,
                EnemyLevel = source.EnemyLevel,
                WeaponType = source.WeaponType,
                AverageSimAdvanceCalls = source.AverageSimAdvanceCalls,
                WinRate = source.WinRate,
                TurnDurationStdDev = source.TurnDurationStdDev,
                AverageMissRate = source.AverageMissRate,
                AverageCritRate = source.AverageCritRate,
                AverageLossSeverity = source.AverageLossSeverity,
                AverageTurnsBelowZero = source.AverageTurnsBelowZero,
                ContinuePastZeroHp = source.ContinuePastZeroHp,
                LevelSnapshots = snaps
            };
    }
}
