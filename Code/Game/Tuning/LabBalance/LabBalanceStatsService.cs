using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RPGGame.ActionInteractionLab;
using RPGGame.Tuning.Profiles;
using RPGGame.Tuning.Suggesters;

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

    /// <summary>One-knob Suggest outcome with an honest empty-state reason when no suggestion.</summary>
    public sealed class LabBalanceSuggestResult
    {
        public TuningSuggestion? Suggestion { get; init; }
        public string EmptyReason { get; init; } = "";

        public static LabBalanceSuggestResult Ok(TuningSuggestion suggestion) =>
            new() { Suggestion = suggestion };

        public static LabBalanceSuggestResult Empty(string reason) =>
            new() { EmptyReason = reason };
    }

    /// <summary>
    /// Batch stats + one-knob suggest/apply for Action Lab Balance layers.
    /// </summary>
    public static class LabBalanceStatsService
    {
        public static ActionLabEncounterSimulationReport? LastEncounterReport { get; private set; }
        public static ActionLabEncounterSimulationReport? PreviousEncounterReport { get; private set; }
        public static TuningSuggestion? PendingSuggestion { get; private set; }

        public static void ClearHistory()
        {
            LastEncounterReport = null;
            PreviousEncounterReport = null;
            PendingSuggestion = null;
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

        public static LabBalanceSuggestResult SuggestOne(
            ActionInteractionLabSession session, LabBalanceStatsRunResult? lastRun)
        {
            PendingSuggestion = null;
            var def = LabBalanceLayerDefinition.Get(session.ActiveBalanceProcessLayer);

            if (def.StatsMode == LabBalanceStatsMode.DeepLinkOnly)
                return LabBalanceSuggestResult.Empty("Open Workbench for playthrough — no Lab Suggest on this layer.");

            if (lastRun == null)
                return LabBalanceSuggestResult.Empty("Run Stats first");

            if (lastRun.Targets?.AllPassed == true)
                return LabBalanceSuggestResult.Empty("All targets met — no adjustment needed");

            if (lastRun.EncounterReport == null)
            {
                if (lastRun.DungeonReport != null)
                {
                    var dungeonSug = SuggestFromFailedChecks(lastRun.Targets, def, encounterReport: null);
                    if (dungeonSug == null)
                    {
                        string detail = DescribeUnaddressedFails(lastRun.Targets);
                        return LabBalanceSuggestResult.Empty(
                            string.IsNullOrEmpty(detail)
                                ? "No dungeon-layer suggestion available."
                                : $"No suggestion for layer knobs — {detail}");
                    }

                    PrefixReasonWithFail(dungeonSug, lastRun.Targets);
                    PendingSuggestion = dungeonSug;
                    return LabBalanceSuggestResult.Ok(dungeonSug);
                }

                return LabBalanceSuggestResult.Empty("Run Stats first (no encounter report).");
            }

            var snapshot = session.CaptureSimulationSnapshot();
            var config = new SimulationProfileConfig
            {
                Mode = "fundamentals_encounter",
                PlayerLevel = Math.Clamp(session.LabPlayer.Level, 1, 99),
                EnemyLevel = Math.Clamp(session.LabEnemy.Level, 1, 99),
                WeaponType = InferWeaponType(session),
                EnemyType = snapshot.SessionEnemyLoaderType,
                ForcedCatalogAction = snapshot.SelectedCatalogActionName,
                ContinuePastZeroHp = def.ContinuePastZeroHp,
                EncounterCount = lastRun.EncounterReport.EncounterCount
            };

            var fundamentals = FundamentalsSimulationResult.FromReport(lastRun.EncounterReport, snapshot, config);
            if (lastRun.AnchorSnapshots.Count > 0)
            {
                fundamentals = CloneWithSnapshots(fundamentals, lastRun.AnchorSnapshots);
            }

            var targets = def.Targets;
            List<TuningSuggestion> suggestions;

            if (def.Layer == LabBalanceProcessLayer.Feel)
            {
                suggestions = SuggestFeel(fundamentals, lastRun.EncounterReport, targets);
            }
            else
            {
                suggestions = FundamentalsDurationAdjustmentSuggester.Suggest(fundamentals, targets);
            }

            var allowedIds = new HashSet<string>(
                LabBalanceScenarioApplicator.GetKnobsForLayer(def.Layer).Select(k => k.Id),
                StringComparer.OrdinalIgnoreCase);

            // Progression suggester uses logical names; map to registry ids when possible.
            var top = suggestions.FirstOrDefault(s =>
                allowedIds.Contains(s.Parameter)
                || MapSuggestionToRegistryId(s) is { } mapped && allowedIds.Contains(mapped)
                || IsLegacyProgressionSuggestion(s));

            if (top == null && suggestions.Count > 0)
                top = suggestions[0];

            if (top == null && lastRun.Targets != null && lastRun.Targets.Checks.Any(c => !c.Passed))
            {
                top = SuggestFromFailedChecks(lastRun.Targets, def, lastRun.EncounterReport);
            }

            if (top == null)
            {
                string detail = DescribeUnaddressedFails(lastRun.Targets);
                return LabBalanceSuggestResult.Empty(
                    string.IsNullOrEmpty(detail)
                        ? "No suggestion — duration/feel suggester returned nothing for this layer."
                        : $"No suggestion for layer knobs — {detail}");
            }

            PrefixReasonWithFail(top, lastRun.Targets);
            PendingSuggestion = top;
            return LabBalanceSuggestResult.Ok(top);
        }

        public static bool ApplyPendingSuggestion(bool saveConfig = true, bool refreshLabUi = true)
        {
            if (PendingSuggestion == null)
                return false;

            bool ok = TuningSuggestionApplier.Apply(PendingSuggestion);
            if (ok && saveConfig)
                GameConfiguration.Instance.SaveToFile();

            ActionInteractionLabSession.ApplyTuningToActiveLabHeroIfAny(refreshUi: refreshLabUi);
            return ok;
        }

        /// <summary>
        /// Run Stats → Suggest → Apply until targets pass, max iterations, no suggestion,
        /// apply failure, or checklist progress stagnates (no PassedCount gain for 3 iters).
        /// Returns a start→end progress report (not full encounter dumps each step).
        /// </summary>
        public static async Task<string> RunAutoLoopAsync(
            ActionInteractionLabSession session,
            int maxIterations,
            int batchCount,
            int maxDegreeOfParallelism = 1,
            bool stopWhenPass = true,
            int stagnationLimit = 3)
        {
            int max = Math.Clamp(maxIterations, 1, 50);
            int stagnateAfter = Math.Clamp(stagnationLimit, 1, max);
            var def = LabBalanceLayerDefinition.Get(session.ActiveBalanceProcessLayer);
            var startKnobs = SnapshotLayerKnobValues(def.Layer);

            var iterLog = new StringBuilder();
            var applied = new List<string>();
            LabBalanceTargetEvaluation? startTargets = null;
            LabBalanceTargetEvaluation? endTargets = null;
            LabBalanceStatsRunResult? lastRun = null;
            string stopReason = "Max iterations reached";
            int iterationsUsed = 0;
            int bestPassed = -1;
            int stagnantStreak = 0;

            for (int i = 1; i <= max; i++)
            {
                iterationsUsed = i;
                var run = await RunStatsAsync(session, batchCount, maxDegreeOfParallelism).ConfigureAwait(true);
                lastRun = run;
                endTargets = run.Targets;
                startTargets ??= run.Targets;

                int passed = run.Targets?.PassedCount ?? 0;
                int total = run.Targets?.Checks.Count ?? 0;

                if (passed > bestPassed)
                {
                    bestPassed = passed;
                    stagnantStreak = 0;
                }
                else
                {
                    stagnantStreak++;
                }

                iterLog.AppendLine($"--- Iteration {i}/{max} — targets {passed}/{total} ---");
                if (run.DeepLinkOnly)
                {
                    iterLog.AppendLine(run.SummaryText);
                    stopReason = "Playthrough layer — open Workbench (no Lab loop)";
                    break;
                }

                if (run.Targets != null)
                    iterLog.AppendLine(run.Targets.FormatSummary());
                else
                    iterLog.AppendLine(run.SummaryText);

                if (stopWhenPass && run.Targets?.AllPassed == true)
                {
                    stopReason = "All targets met";
                    break;
                }

                if (stagnantStreak >= stagnateAfter && i > 1)
                {
                    stopReason =
                        $"No checklist improvement for {stagnateAfter} iterations (best {bestPassed}/{total})";
                    break;
                }

                var outcome = SuggestOne(session, run);
                if (outcome.Suggestion == null)
                {
                    iterLog.AppendLine(outcome.EmptyReason);
                    stopReason = outcome.EmptyReason;
                    break;
                }

                var suggestion = outcome.Suggestion;
                string applyLine =
                    $"{suggestion.Parameter}: {suggestion.CurrentValue:F3} → {suggestion.SuggestedValue:F3}";
                iterLog.AppendLine($"Suggest/Apply: {applyLine}");
                if (!string.IsNullOrWhiteSpace(suggestion.Reason))
                    iterLog.AppendLine($"  {suggestion.Reason}");

                if (!ApplyPendingSuggestion(saveConfig: true, refreshLabUi: false))
                {
                    stopReason = $"Apply failed — could not write '{suggestion.Parameter}'";
                    iterLog.AppendLine(stopReason);
                    break;
                }

                applied.Add(applyLine);
                iterLog.AppendLine("Applied + saved.");
            }

            var endKnobs = SnapshotLayerKnobValues(def.Layer);
            ActionInteractionLabSession.ApplyTuningToActiveLabHeroIfAny(refreshUi: true);
            return FormatLoopProgressReport(
                def.DisplayName,
                max,
                iterationsUsed,
                stopReason,
                startTargets,
                endTargets,
                startKnobs,
                endKnobs,
                applied,
                iterLog.ToString(),
                lastRun).TrimEnd();
        }

        /// <summary>Builds the RUN loop start→end progress report (unit-tested).</summary>
        internal static string FormatLoopProgressReport(
            string layerDisplayName,
            int maxIterations,
            int iterationsUsed,
            string stopReason,
            LabBalanceTargetEvaluation? startTargets,
            LabBalanceTargetEvaluation? endTargets,
            IReadOnlyDictionary<string, double> startKnobs,
            IReadOnlyDictionary<string, double> endKnobs,
            IReadOnlyList<string> appliedLines,
            string iterationLog,
            LabBalanceStatsRunResult? lastRun = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== RUN LOOP PROGRESS REPORT ===");
            sb.AppendLine($"Layer: {layerDisplayName}");
            sb.AppendLine($"Iterations: {iterationsUsed}/{maxIterations}");
            sb.AppendLine($"Stop: {stopReason}");
            sb.AppendLine();

            sb.AppendLine("--- START ---");
            if (startTargets != null)
                sb.AppendLine(startTargets.FormatSummary());
            else
                sb.AppendLine("(no checklist)");
            sb.AppendLine();

            sb.AppendLine("--- ITERATIONS ---");
            sb.AppendLine(string.IsNullOrWhiteSpace(iterationLog) ? "(none)" : iterationLog.TrimEnd());
            sb.AppendLine();

            sb.AppendLine("--- END ---");
            if (endTargets != null)
                sb.AppendLine(endTargets.FormatSummary());
            else
                sb.AppendLine("(no checklist)");
            sb.AppendLine();

            sb.AppendLine("--- CHECKLIST PROGRESS (start → end) ---");
            sb.AppendLine(FormatChecklistProgress(startTargets, endTargets));
            sb.AppendLine();

            sb.AppendLine("--- KNOBS CHANGED ---");
            var knobDelta = FormatKnobDelta(startKnobs, endKnobs);
            sb.AppendLine(string.IsNullOrWhiteSpace(knobDelta) ? "(none)" : knobDelta);
            if (appliedLines.Count > 0)
            {
                sb.AppendLine("Applied this loop:");
                foreach (var line in appliedLines)
                    sb.AppendLine($"  {line}");
            }

            if (lastRun != null && !string.IsNullOrWhiteSpace(lastRun.SummaryText)
                && lastRun.Targets?.AllPassed != true)
            {
                sb.AppendLine();
                sb.AppendLine("--- LAST FULL STATS (for detail) ---");
                sb.AppendLine(lastRun.SummaryText.TrimEnd());
            }

            return sb.ToString().TrimEnd();
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

        private static List<TuningSuggestion> SuggestFeel(
            FundamentalsSimulationResult fundamentals,
            ActionLabEncounterSimulationReport report,
            FundamentalsAnalysisTargets targets)
        {
            var list = new List<TuningSuggestion>();
            double comboFloor = targets.MinAverageMaxComboStreak > 0
                ? targets.MinAverageMaxComboStreak
                : 2.0;

            var compression = CombatTuningParameterRegistry.GetById(
                RollFeelVarianceCompression.MasterParameterId);
            if (compression != null && report.StdDevTurns > 8)
            {
                double next = Math.Min(compression.Maximum, compression.GetValue() + 0.1);
                list.Add(new TuningSuggestion
                {
                    Category = "roll_feel",
                    Parameter = compression.Id,
                    CurrentValue = compression.GetValue(),
                    SuggestedValue = next,
                    Reason = $"Turn stddev {report.StdDevTurns:F1} is high — increase variance compression",
                    Impact = "Reduce outcome swing"
                });
            }

            if (fundamentals.AverageMaxComboStreak < comboFloor)
            {
                var combo = CombatTuningParameterRegistry.All
                    .FirstOrDefault(p => p.Layer == CombatTuningLayer.ComboAffordance && p.IsImplemented);
                if (combo != null)
                {
                    list.Add(new TuningSuggestion
                    {
                        Category = "roll_feel",
                        Parameter = combo.Id,
                        CurrentValue = combo.GetValue(),
                        SuggestedValue = Math.Clamp(combo.GetValue() * 0.95, combo.Minimum, combo.Maximum),
                        Reason =
                            $"Avg max combo streak {fundamentals.AverageMaxComboStreak:F2} below target {comboFloor:F1}",
                        Impact = "Improve combo affordance"
                    });
                }
            }

            if (list.Count == 0)
                list.AddRange(FundamentalsDurationAdjustmentSuggester.Suggest(fundamentals, targets));

            return list;
        }

        /// <summary>
        /// Lab-local fallback when duration/feel suggesters return nothing while checklist FAILs remain.
        /// </summary>
        internal static TuningSuggestion? SuggestFromFailedChecks(
            LabBalanceTargetEvaluation? evaluation,
            LabBalanceLayerDefinition def,
            ActionLabEncounterSimulationReport? encounterReport)
        {
            if (evaluation == null)
                return null;

            var fail = evaluation.Checks.FirstOrDefault(c => !c.Passed);
            if (fail == null)
                return null;

            var knobs = LabBalanceScenarioApplicator.GetKnobsForLayer(def.Layer)
                .Where(k => k.IsImplemented)
                .ToList();
            if (knobs.Count == 0)
                return null;

            string address = $"Addresses FAIL {fail.Name} ({fail.ActualText} vs want {fail.TargetText})";

            if (fail.Name.Contains("std-dev", StringComparison.OrdinalIgnoreCase)
                || fail.Name.Contains("spread", StringComparison.OrdinalIgnoreCase))
            {
                var compression = knobs.FirstOrDefault(k =>
                                       k.Id.Equals(RollFeelVarianceCompression.MasterParameterId,
                                           StringComparison.OrdinalIgnoreCase))
                                   ?? knobs.FirstOrDefault(k => k.Layer == CombatTuningLayer.RollFeel)
                                   ?? knobs.FirstOrDefault(k => k.Layer == CombatTuningLayer.WinRate);
                if (compression != null)
                {
                    double cur = compression.GetValue();
                    double next = fail.Name.Contains("spread", StringComparison.OrdinalIgnoreCase)
                        ? Math.Clamp(cur * 0.97, compression.Minimum, compression.Maximum)
                        : Math.Min(compression.Maximum, cur + 0.1);
                    return new TuningSuggestion
                    {
                        Category = "lab_balance",
                        Parameter = compression.Id,
                        CurrentValue = cur,
                        SuggestedValue = next,
                        Reason = address,
                        Impact = fail.Name.Contains("spread", StringComparison.OrdinalIgnoreCase)
                            ? "Pull weapon paths closer together"
                            : "Reduce outcome swing"
                    };
                }
            }

            if (fail.Name.Contains("combo", StringComparison.OrdinalIgnoreCase))
            {
                var combo = knobs.FirstOrDefault(k => k.Layer == CombatTuningLayer.ComboAffordance)
                            ?? knobs.FirstOrDefault(k =>
                                k.Id.Equals(RollFeelVarianceCompression.MasterParameterId,
                                    StringComparison.OrdinalIgnoreCase));
                if (combo != null)
                {
                    double next = combo.Id.Equals(RollFeelVarianceCompression.MasterParameterId,
                        StringComparison.OrdinalIgnoreCase)
                        ? Math.Min(combo.Maximum, combo.GetValue() + 0.1)
                        : Math.Clamp(combo.GetValue() * 0.95, combo.Minimum, combo.Maximum);
                    return new TuningSuggestion
                    {
                        Category = "lab_balance",
                        Parameter = combo.Id,
                        CurrentValue = combo.GetValue(),
                        SuggestedValue = next,
                        Reason = address,
                        Impact = "Raise combo affordance / compress variance toward streak floors"
                    };
                }
            }

            if (fail.Name.Contains("clear rate", StringComparison.OrdinalIgnoreCase))
            {
                bool tooLow = TryParseDouble(fail.ActualText.TrimEnd('%'), out double actualClear)
                              && actualClear < def.MinDungeonClearRate * 100;
                var hp = tooLow
                    ? knobs.FirstOrDefault(k => k.Id.Equals("playerBaseHealth", StringComparison.OrdinalIgnoreCase))
                      ?? knobs.FirstOrDefault(k => k.Id.Equals("enemyBaselineHealth", StringComparison.OrdinalIgnoreCase))
                      ?? knobs.FirstOrDefault()
                    : knobs.FirstOrDefault(k => k.Id.Equals("enemyBaselineHealth", StringComparison.OrdinalIgnoreCase))
                      ?? knobs.FirstOrDefault(k => k.Id.Equals("playerBaseHealth", StringComparison.OrdinalIgnoreCase))
                      ?? knobs.FirstOrDefault();
                if (hp != null)
                {
                    double cur = hp.GetValue();
                    bool isPlayer = hp.Id.Equals("playerBaseHealth", StringComparison.OrdinalIgnoreCase);
                    double next = tooLow
                        ? (isPlayer ? Math.Min(hp.Maximum, cur * 1.05) : Math.Max(hp.Minimum, cur * 0.95))
                        : (isPlayer ? Math.Max(hp.Minimum, cur * 0.95) : Math.Min(hp.Maximum, cur * 1.05));
                    return new TuningSuggestion
                    {
                        Category = "lab_balance",
                        Parameter = hp.Id,
                        CurrentValue = cur,
                        SuggestedValue = next,
                        Reason = address,
                        Impact = tooLow ? "Improve dungeon survivability" : "Increase dungeon attrition"
                    };
                }
            }

            if (fail.Name.Contains("Win rate", StringComparison.OrdinalIgnoreCase))
            {
                bool tooLow = encounterReport != null
                    ? encounterReport.WinRate < 0.85
                    : (TryParseDouble(fail.ActualText.TrimEnd('%'), out double wrPct) && wrPct < 85);
                CombatTuningParameter? hp = tooLow
                    ? knobs.FirstOrDefault(k => k.Id.Equals("playerBaseHealth", StringComparison.OrdinalIgnoreCase))
                      ?? knobs.FirstOrDefault(k => k.Id.Equals("enemyBaselineHealth", StringComparison.OrdinalIgnoreCase))
                    : knobs.FirstOrDefault(k => k.Id.Equals("enemyBaselineHealth", StringComparison.OrdinalIgnoreCase))
                      ?? knobs.FirstOrDefault(k => k.Id.Equals("playerBaseHealth", StringComparison.OrdinalIgnoreCase));

                if (hp != null)
                {
                    double cur = hp.GetValue();
                    bool isPlayerHp = hp.Id.Equals("playerBaseHealth", StringComparison.OrdinalIgnoreCase);
                    double next = isPlayerHp
                        ? (tooLow ? Math.Min(hp.Maximum, cur * 1.05) : Math.Max(hp.Minimum, cur * 0.95))
                        : (tooLow ? Math.Max(hp.Minimum, cur * 0.95) : Math.Min(hp.Maximum, cur * 1.05));

                    return new TuningSuggestion
                    {
                        Category = "lab_balance",
                        Parameter = hp.Id,
                        CurrentValue = cur,
                        SuggestedValue = next,
                        Reason = address,
                        Impact = tooLow ? "Nudge win rate up" : "Nudge win rate down"
                    };
                }
            }

            if (encounterReport != null)
            {
                int dir = EstimateDurationDirection(fail, def.Targets, encounterReport);
                if (dir != 0)
                {
                    CombatTuningParameter? pick;
                    double cur;
                    double next;
                    if (dir < 0)
                    {
                        pick = knobs.FirstOrDefault(k =>
                                   k.Id.Equals("enemyBaselineHealth", StringComparison.OrdinalIgnoreCase))
                               ?? knobs.FirstOrDefault(k =>
                                   k.Id.Equals("combatTempoScale", StringComparison.OrdinalIgnoreCase))
                               ?? knobs.FirstOrDefault(k =>
                                   k.Id.Equals("globalEnemyHealthMult", StringComparison.OrdinalIgnoreCase))
                               ?? knobs.FirstOrDefault();
                        if (pick != null)
                        {
                            cur = pick.GetValue();
                            next = Math.Min(pick.Maximum, cur * 1.08);
                            return new TuningSuggestion
                            {
                                Category = "lab_balance",
                                Parameter = pick.Id,
                                CurrentValue = cur,
                                SuggestedValue = next,
                                Reason = address,
                                Impact = "Lengthen fights toward target band"
                            };
                        }
                    }
                    else
                    {
                        pick = knobs.FirstOrDefault(k =>
                                   k.Id.Equals("enemyBaselineHealth", StringComparison.OrdinalIgnoreCase))
                               ?? knobs.FirstOrDefault(k =>
                                   k.Id.Equals("combatTempoScale", StringComparison.OrdinalIgnoreCase))
                               ?? knobs.FirstOrDefault(k =>
                                   k.Id.Equals("playerBaseHealth", StringComparison.OrdinalIgnoreCase))
                               ?? knobs.FirstOrDefault();
                        if (pick != null)
                        {
                            cur = pick.GetValue();
                            bool raisePlayer = pick.Id.Equals("playerBaseHealth", StringComparison.OrdinalIgnoreCase);
                            next = raisePlayer
                                ? Math.Min(pick.Maximum, cur * 1.05)
                                : Math.Max(pick.Minimum, cur * 0.92);
                            return new TuningSuggestion
                            {
                                Category = "lab_balance",
                                Parameter = pick.Id,
                                CurrentValue = cur,
                                SuggestedValue = next,
                                Reason = address,
                                Impact = "Shorten fights toward target band"
                            };
                        }
                    }
                }

                var any = knobs[0];
                double aCur = any.GetValue();
                double median = LabBalanceTargetEvaluator.EstimateMedianCombined(encounterReport);
                double targetCombined = def.Targets.TargetMedianCombinedActions > 0
                    ? def.Targets.TargetMedianCombinedActions
                    : 27;
                double aNext = median < targetCombined
                    ? Math.Min(any.Maximum, aCur * 1.05)
                    : Math.Max(any.Minimum, aCur * 0.95);
                if (Math.Abs(aNext - aCur) > 1e-6)
                {
                    return new TuningSuggestion
                    {
                        Category = "lab_balance",
                        Parameter = any.Id,
                        CurrentValue = aCur,
                        SuggestedValue = aNext,
                        Reason = address,
                        Impact = "Layer-knob nudge from worst FAIL"
                    };
                }
            }

            var fallback = knobs[0];
            double fCur = fallback.GetValue();
            double fNext = Math.Min(fallback.Maximum, fCur * 1.05);
            if (Math.Abs(fNext - fCur) < 1e-6)
                return null;

            return new TuningSuggestion
            {
                Category = "lab_balance",
                Parameter = fallback.Id,
                CurrentValue = fCur,
                SuggestedValue = fNext,
                Reason = address,
                Impact = "Layer-knob nudge from worst FAIL"
            };
        }

        /// <returns>-1 too short, +1 too long, 0 unknown</returns>
        private static int EstimateDurationDirection(
            LabBalanceTargetCheck fail,
            FundamentalsAnalysisTargets targets,
            ActionLabEncounterSimulationReport report)
        {
            if (!TryParseDouble(fail.ActualText, out double actual))
            {
                actual = LabBalanceTargetEvaluator.EstimateMedianCombined(report);
            }

            if (fail.TargetText.Contains('–') || fail.TargetText.Contains('-'))
            {
                // mean band "24–30"
                var parts = fail.TargetText.Split(new[] { '–', '-' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2
                    && TryParseDouble(parts[0], out double min)
                    && TryParseDouble(parts[1], out double max))
                {
                    if (actual < min) return -1;
                    if (actual > max) return 1;
                }
            }

            if (fail.TargetText.Contains('±'))
            {
                var bits = fail.TargetText.Split('±', StringSplitOptions.TrimEntries);
                if (bits.Length >= 1 && TryParseDouble(bits[0], out double center))
                    return actual < center ? -1 : 1;
            }

            if (fail.TargetText.TrimStart().StartsWith(">=", StringComparison.Ordinal))
                return -1; // below floor

            double combinedTarget = targets.TargetMedianCombinedActions > 0
                ? targets.TargetMedianCombinedActions
                : 27;
            return actual < combinedTarget ? -1 : 1;
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

        private static void PrefixReasonWithFail(TuningSuggestion suggestion, LabBalanceTargetEvaluation? targets)
        {
            if (targets == null)
                return;
            var fail = targets.Checks.FirstOrDefault(c => !c.Passed);
            if (fail == null)
                return;
            string prefix = $"Addresses FAIL {fail.Name} ({fail.ActualText} vs want {fail.TargetText})";
            if (suggestion.Reason.StartsWith("Addresses FAIL", StringComparison.Ordinal))
                return;
            suggestion.Reason = string.IsNullOrWhiteSpace(suggestion.Reason)
                ? prefix
                : prefix + " — " + suggestion.Reason;
        }

        private static string DescribeUnaddressedFails(LabBalanceTargetEvaluation? targets)
        {
            if (targets == null || targets.Checks.Count == 0)
                return "no checklist FAILs available";
            var fails = targets.Checks.Where(c => !c.Passed).Take(3).ToList();
            if (fails.Count == 0)
                return "checklist already passed";
            return "FAIL " + string.Join("; ",
                fails.Select(f => $"{f.Name} {f.ActualText} (want {f.TargetText})"));
        }

        private static string? MapSuggestionToRegistryId(TuningSuggestion s) =>
            s.Parameter switch
            {
                "BaseHealth" => "playerBaseHealth",
                "HealthPerLevel" => "playerHealthPerLevel",
                "CombatTempoScale" => "combatTempoScale",
                "BaseHealthScale" => "baseHealthScale",
                "ProgressionShape" => "progressionShape",
                "PlayerEnemyParity" => "playerEnemyParity",
                "HealthMultiplier" => "globalEnemyHealthMult",
                "DamageMultiplier" => "globalEnemyDamageMult",
                "BaselineHealth" => "enemyBaselineHealth",
                _ => null
            };

        private static bool IsLegacyProgressionSuggestion(TuningSuggestion s) =>
            s.Category is "enemy_progression" or "player" or "global" or "enemy_baseline";

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
