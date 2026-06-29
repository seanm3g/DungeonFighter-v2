using System;
using System.Threading;
using System.Threading.Tasks;
using RPGGame.Tuning.Profiles;

namespace RPGGame.Tuning
{
    public sealed class BalanceTuningRunOptions
    {
        public string ProfileId { get; set; } = BalanceTuningProfileLoader.DefaultProfileId;
        public int MaxIterations { get; set; } = 1;
        public bool AutoApply { get; set; }
        public bool DryRunApply { get; set; }
        public bool StopWhenChecksPass { get; set; } = true;
        public BalanceTuningRunMode Mode { get; set; } = BalanceTuningRunMode.FullCycle;
        public string? SessionPath { get; set; }

        /// <summary>Legacy alias — maps to battles or encounters depending on profile mode.</summary>
        public int? BattlesOrEncounters
        {
            get => EncounterCount ?? BattlesPerCombination;
            set
            {
                BattlesPerCombination = value;
                EncounterCount = value;
            }
        }

        public int? BattlesPerCombination { get; set; }
        public int? BattlesPerWeapon { get; set; }
        public int? NumberOfBattles { get; set; }
        public string? LevelsCsv { get; set; }
        public int? PlayerLevel { get; set; }
        public int? EnemyLevel { get; set; }
        public int? EncounterCount { get; set; }
        public string? EnemyType { get; set; }
        public string? WeaponType { get; set; }
        public string? ForcedCatalogAction { get; set; }

        public bool? OptimizeWinRate { get; set; }
        public int? MaxSuggestions { get; set; }
        public double? TargetMedianCombinedActions { get; set; }
        public double? TargetMedianPlayerTurns { get; set; }
        public double? TargetMedianEnemyTurns { get; set; }
        public double? MinAverageActions { get; set; }
        public double? MaxAverageActions { get; set; }
        public double? MinAverageComboStreakRuns2Plus { get; set; }
        public double? MinAverageMaxComboStreak { get; set; }
    }

    /// <summary>
    /// Runs profile-driven tuning cycles with live progress for the workbench UI.
    /// </summary>
    public sealed class BalanceTuningRunCoordinator
    {
        public async Task RunAsync(
            BalanceTuningRunOptions options,
            IBalanceTuningRunSink uiSink,
            CancellationToken cancellationToken = default)
        {
            var sink = new CompositeBalanceTuningRunSink(uiSink);
            int maxIter = Math.Max(1, options.MaxIterations);

            try
            {
                sink.SetStep(BalanceTuningRunStep.Initializing, 0, maxIter, "Loading game data");
                sink.Log(BalanceTuningLogLevel.Info, "Initializing game for tuning...");
                await BalanceTuningWorkflow.EnsureGameInitializedAsync().ConfigureAwait(false);

                var profile = BalanceTuningProfileLoader.Load(options.ProfileId);
                BalanceTuningProfileApplicator.ApplyAll(profile, options);

                sink.Log(BalanceTuningLogLevel.Info,
                    $"Profile: {profile.Name} ({profile.Id}) — sim mode {profile.Simulation.Mode}");
                if (profile.Simulation.Mode == TuningSimulationModes.MultiLevelWeaponEnemy)
                {
                    int battles = profile.Simulation.BattlesPerCombination;
                    int perLevel = MultiLevelSimulationRunner.EstimateBattlesPerLevel(battles);
                    int levelCount = profile.Simulation.Levels?.Count ?? LevelWinRateCurve.GetDefaultAnchorLevels().Count;
                    sink.Log(BalanceTuningLogLevel.Info,
                        $"Estimated workload: ~{perLevel * levelCount:N0} battles " +
                        $"({levelCount} levels × {perLevel:N0} weapon×enemy matchups @ {battles} battles each). " +
                        "Reduce levels or battles in parameters for faster runs.");
                }
                else if (profile.Simulation.Mode == TuningSimulationModes.FundamentalsEncounter)
                {
                    sink.Log(BalanceTuningLogLevel.Info,
                        $"Fundamentals: {profile.Simulation.EncounterCount} encounters @ P{profile.Simulation.PlayerLevel}/E{profile.Simulation.EnemyLevel}.");
                }

                sink.Log(BalanceTuningLogLevel.Info,
                    $"Validators: {string.Join(", ", TuningAnalysisCriteria.GetEffectiveValidators(profile.Analysis))}");
                var effectiveSuggesters = TuningAnalysisCriteria.GetEffectiveSuggesters(profile.Analysis);
                if (effectiveSuggesters.Count > 0)
                    sink.Log(BalanceTuningLogLevel.Info,
                        $"Suggesters: {string.Join(", ", effectiveSuggesters)}");
                if (!profile.Analysis.OptimizeWinRate)
                    sink.Log(BalanceTuningLogLevel.Info, "Win-rate optimization disabled for this profile.");

                bool canApply = effectiveSuggesters.Count > 0;

                for (int iteration = 1; iteration <= maxIter; iteration++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var runProfile = BalanceTuningProfileLoader.Load(options.ProfileId);
                    BalanceTuningProfileApplicator.ApplyAll(runProfile, options);
                    var iterationSimOverrides = BalanceTuningProfileApplicator.BuildSimulationOverrides(runProfile, options);

                    sink.Log(BalanceTuningLogLevel.Info, $"——— Iteration {iteration}/{maxIter} ———", iteration, maxIter);

                    string? iterationReport = null;
                    double qualityScore = 0;
                    bool allPass = false;
                    bool suggestionAvailable = false;
                    bool changeApplied = false;

                    if (options.Mode is BalanceTuningRunMode.FullCycle
                        or BalanceTuningRunMode.SimulateOnly
                        or BalanceTuningRunMode.SimulateAndAnalyze)
                    {
                        sink.SetStep(BalanceTuningRunStep.Simulate, iteration, maxIter);
                        sink.Log(BalanceTuningLogLevel.Info, "Step 1/3: Running simulation...", iteration, maxIter);
                        await BalanceTuningWorkflow.RunSimWithProfileAsync(
                            runProfile,
                            iterationSimOverrides,
                            options.SessionPath,
                            sink,
                            iteration,
                            maxIter).ConfigureAwait(false);

                        var session = LevelTuningSessionStore.Load(options.SessionPath);
                        iterationReport = BuildSimReport(session);
                    }

                    if (options.Mode is BalanceTuningRunMode.FullCycle
                        or BalanceTuningRunMode.AnalyzeOnly
                        or BalanceTuningRunMode.SimulateAndAnalyze)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        sink.SetStep(BalanceTuningRunStep.Analyze, iteration, maxIter);
                        sink.Log(BalanceTuningLogLevel.Info, "Step 2/3: Analyzing results...", iteration, maxIter);

                        int analyzeCode = await BalanceTuningWorkflow.RunAnalyzeAsync(
                            options.SessionPath,
                            sink,
                            iteration,
                            maxIter).ConfigureAwait(false);

                        var session = LevelTuningSessionStore.Load(options.SessionPath);
                        qualityScore = session.Analysis?.QualityScore ?? 0;
                        allPass = session.Analysis?.AllAnchorsPass == true;
                        suggestionAvailable = session.Analysis?.TopSuggestion != null;
                        iterationReport = (iterationReport ?? "") + "\n\n" + BuildAnalysisReport(session);

                        if (session.Analysis?.TopSuggestion is { } suggestion)
                        {
                            if (!string.IsNullOrEmpty(session.Analysis.PrimaryDial))
                            {
                                sink.RecordIdea(
                                    iteration,
                                    $"Dial: {session.Analysis.PrimaryDial}",
                                    session.Analysis.DialDiagnosis ?? "");
                            }

                            sink.RecordIdea(
                                iteration,
                                $"{suggestion.Parameter}: {suggestion.CurrentValue} → {suggestion.SuggestedValue}",
                                suggestion.Reason);
                        }
                        else if (analyzeCode == 0)
                        {
                            sink.RecordIdea(iteration, "Targets met", "No adjustment recommended this iteration.");
                        }

                        if (options.Mode == BalanceTuningRunMode.FullCycle
                            && options.AutoApply
                            && canApply
                            && suggestionAvailable)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            sink.SetStep(BalanceTuningRunStep.Apply, iteration, maxIter);
                            sink.Log(BalanceTuningLogLevel.Info,
                                options.DryRunApply ? "Step 3/3: Preview apply (dry run)..." : "Step 3/3: Applying suggestion...",
                                iteration, maxIter);

                            var before = session.Analysis!.TopSuggestion!;
                            int applyCode = await BalanceTuningWorkflow.RunApplyAsync(
                                options.DryRunApply,
                                options.SessionPath,
                                sink,
                                iteration,
                                maxIter).ConfigureAwait(false);

                            changeApplied = applyCode == 0 && !options.DryRunApply;
                            sink.RecordChange(new BalanceTuningChangeEntry
                            {
                                Iteration = iteration,
                                ProfileId = options.ProfileId,
                                Parameter = before.Parameter,
                                FromValue = before.CurrentValue,
                                ToValue = before.SuggestedValue,
                                Reason = before.Reason,
                                Applied = changeApplied,
                                PatchName = changeApplied
                                    ? LevelTuningSessionStore.Load(options.SessionPath).LastApply?.PatchName
                                    : null
                            });
                        }
                    }

                    sink.IterationCompleted(new BalanceTuningIterationResult
                    {
                        Iteration = iteration,
                        MaxIterations = maxIter,
                        ProfileId = options.ProfileId,
                        ReportText = iterationReport ?? "",
                        QualityScore = qualityScore,
                        AllChecksPass = allPass,
                        SuggestionAvailable = suggestionAvailable,
                        ChangeApplied = changeApplied
                    });

                    if (allPass && options.Mode == BalanceTuningRunMode.FullCycle && options.StopWhenChecksPass)
                    {
                        sink.Log(BalanceTuningLogLevel.Success, "All checks pass — stopping early.", iteration, maxIter);
                        break;
                    }
                }

                sink.SetStep(BalanceTuningRunStep.Completed, maxIter, maxIter, "Run finished");
                sink.Log(BalanceTuningLogLevel.Success, "Tuning run complete.");
            }
            catch (OperationCanceledException)
            {
                sink.SetStep(BalanceTuningRunStep.Cancelled, 0, maxIter, "Cancelled by user");
                sink.Log(BalanceTuningLogLevel.Warning, "Run cancelled.");
            }
            catch (Exception ex)
            {
                sink.SetStep(BalanceTuningRunStep.Failed, 0, maxIter, ex.Message);
                sink.Log(BalanceTuningLogLevel.Error, $"{ex.GetType().Name}: {ex.Message}");
                throw;
            }
        }

        private static string BuildSimReport(LevelTuningSession session)
        {
            var outcome = ComprehensiveSimulationMapper.ToOutcome(session);
            return outcome.FormatReport();
        }

        private static string BuildAnalysisReport(LevelTuningSession session)
        {
            if (session.Analysis == null)
                return "No analysis in session.";

            var lines = new System.Collections.Generic.List<string>
            {
                "=== Analysis ===",
                $"Quality score: {session.Analysis.QualityScore:F1}/100",
                $"Summary: {session.Analysis.Summary}"
            };

            if (!string.IsNullOrEmpty(session.Analysis.PrimaryDial))
            {
                lines.Add($"Primary dial: {session.Analysis.PrimaryDial}");
                if (!string.IsNullOrEmpty(session.Analysis.DialDiagnosis))
                    lines.Add($"Diagnosis: {session.Analysis.DialDiagnosis}");
            }

            foreach (var w in session.Analysis.ValidationWarnings)
                lines.Add($"  ⚠ {w}");
            foreach (var e in session.Analysis.ValidationErrors)
                lines.Add($"  ✗ {e}");

            if (session.Analysis.TopSuggestion is { } s)
            {
                lines.Add("");
                lines.Add($"Suggestion: {s.Parameter} {s.CurrentValue} → {s.SuggestedValue}");
                lines.Add($"  {s.Reason}");
            }

            return string.Join(System.Environment.NewLine, lines);
        }
    }
}
