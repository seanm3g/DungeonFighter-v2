using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RPGGame.Config;
using RPGGame.MCP;
using RPGGame.MCP.Tools;
using RPGGame.Tuning.Profiles;

namespace RPGGame.Tuning
{
    /// <summary>
    /// Profile-driven tuning: TUNESIM → TUNEANALYZE → TUNEAPPLY.
    /// </summary>
    public static class BalanceTuningWorkflow
    {
        private static bool _gameInitialized;

        public static void ListProfiles()
        {
            var profiles = BalanceTuningProfileLoader.ListProfiles();
            Console.WriteLine("=== Balance tuning profiles ===");
            if (profiles.Count == 0)
            {
                Console.WriteLine($"No profiles in {BalanceTuningProfileLoader.ProfilesRelativeDir}");
                return;
            }

            foreach (var p in profiles)
            {
                Console.WriteLine();
                Console.WriteLine($"  {p.Id}");
                Console.WriteLine($"    {p.Name}");
                Console.WriteLine($"    Sim: {p.Simulation.Mode} | Validators: {string.Join(", ", p.Analysis.Validators)}");
                Console.WriteLine($"    Suggesters: {string.Join(", ", p.Analysis.Suggesters)}");
                if (!string.IsNullOrWhiteSpace(p.Description))
                    Console.WriteLine($"    {p.Description}");
            }
        }

        public static Task<int> RunSimAsync(
            string profileId = BalanceTuningProfileLoader.DefaultProfileId,
            string[]? cliArgs = null,
            string? sessionPath = null,
            IBalanceTuningRunSink? sink = null,
            int iteration = 0,
            int maxIterations = 0)
        {
            var profile = BalanceTuningProfileLoader.Load(profileId);
            SimulationRunOverrides? overrides = cliArgs != null ? TuningCliArgs.BuildOverrides(cliArgs, profile) : null;
            return RunSimWithProfileAsync(profile, overrides, sessionPath, sink, iteration, maxIterations);
        }

        public static Task<int> RunSimWithProfileAsync(
            BalanceTuningProfile profile,
            SimulationRunOverrides? overrides = null,
            string? sessionPath = null,
            IBalanceTuningRunSink? sink = null,
            int iteration = 0,
            int maxIterations = 0) =>
            RunSimCoreAsync(profile, overrides, sessionPath, sink, iteration, maxIterations);

        public static Task<int> RunAnalyzeAsync(
            string? sessionPath = null,
            IBalanceTuningRunSink? sink = null,
            int iteration = 0,
            int maxIterations = 0) =>
            RunAnalyzeCoreAsync(sessionPath, sink, iteration, maxIterations);

        public static Task<int> RunApplyAsync(
            bool dryRun = false,
            string? sessionPath = null,
            IBalanceTuningRunSink? sink = null,
            int iteration = 0,
            int maxIterations = 0) =>
            RunApplyCoreAsync(dryRun, sessionPath, sink, iteration, maxIterations);

        private static async Task<int> RunSimCoreAsync(
            BalanceTuningProfile profile,
            SimulationRunOverrides? overrides,
            string? sessionPath,
            IBalanceTuningRunSink? sink,
            int iteration,
            int maxIterations)
        {
            if (!_gameInitialized)
            {
                await EnsureGameInitializedAsync();
                _gameInitialized = true;
            }

            Write(sink, BalanceTuningLogLevel.Info, $"=== TUNESIM: {profile.Name} ({profile.Id}) ===", iteration, maxIterations);
            Write(sink, BalanceTuningLogLevel.Info, $"Simulation mode: {profile.Simulation.Mode}", iteration, maxIterations);

            var outcome = await TuningSimulationRunner.RunAsync(
                profile,
                overrides,
                CreateProgressReporter(sink, iteration, maxIterations));

            Write(sink, BalanceTuningLogLevel.Info, outcome.FormatReport(), iteration, maxIterations);

            var session = LevelTuningSessionStore.Load(sessionPath);
            session.ProfileId = profile.Id;
            session.ProfileName = profile.Name;
            session.SimulationMode = outcome.Mode;
            session.RunAnalysisConfig = CloneAnalysisConfig(profile.Analysis);
            session.Analysis = null;
            session.LastApply = null;

            if (outcome.MultiLevel != null)
            {
                session.Simulation = LevelTuningSessionStore.FromSimulationResult(outcome.MultiLevel);
                session.Comprehensive = null;
                session.Fundamentals = null;
            }
            else if (outcome.Comprehensive != null)
            {
                session.Comprehensive = ComprehensiveSimulationMapper.FromResult(
                    outcome.Comprehensive,
                    outcome.PlayerLevel,
                    outcome.EnemyLevel,
                    outcome.BattlesPerCombination);
                session.Simulation = null;
                session.Fundamentals = null;
            }
            else if (outcome.Fundamentals != null)
            {
                session.Fundamentals = ComprehensiveSimulationMapper.FromFundamentalsResult(outcome.Fundamentals);
                session.Simulation = null;
                session.Comprehensive = null;
            }

            LevelTuningSessionStore.Save(session, sessionPath);

            Write(sink, BalanceTuningLogLevel.Info,
                $"Session saved: {LevelTuningSessionStore.ResolvePath(sessionPath)}", iteration, maxIterations);

            return outcome.MeetsTargets && outcome.Mode == TuningSimulationModes.MultiLevelWeaponEnemy ? 0 : 1;
        }

        private static Task<int> RunAnalyzeCoreAsync(
            string? sessionPath,
            IBalanceTuningRunSink? sink,
            int iteration,
            int maxIterations)
        {
            var session = LevelTuningSessionStore.Load(sessionPath);
            if (session.Simulation == null && session.Comprehensive == null && session.Fundamentals == null)
            {
                Write(sink, BalanceTuningLogLevel.Error, "No simulation in session. Run TUNESIM first.", iteration, maxIterations);
                return Task.FromResult(2);
            }

            var profileId = session.ProfileId ?? BalanceTuningProfileLoader.DefaultProfileId;
            BalanceTuningProfile profile;
            try
            {
                profile = BalanceTuningProfileLoader.Load(profileId);
            }
            catch (Exception ex)
            {
                Write(sink, BalanceTuningLogLevel.Error, $"Could not load profile '{profileId}': {ex.Message}", iteration, maxIterations);
                return Task.FromResult(2);
            }

            if (session.RunAnalysisConfig != null)
                profile.Analysis = CloneAnalysisConfig(session.RunAnalysisConfig);

            var simulation = ComprehensiveSimulationMapper.ToOutcome(session);
            if (simulation.MultiLevel != null)
                McpToolState.LastMultiLevelResult = simulation.MultiLevel;
            if (simulation.Comprehensive != null)
                McpToolState.LastTestResult = simulation.Comprehensive;

            var outcome = TuningAnalysisPipeline.Analyze(profile, simulation);

            Write(sink, BalanceTuningLogLevel.Info, $"=== TUNEANALYZE: {profile.Name} ({profile.Id}) ===", iteration, maxIterations);
            Write(sink, BalanceTuningLogLevel.Info, simulation.FormatReport(), iteration, maxIterations);
            Write(sink, BalanceTuningLogLevel.Info, $"Quality score: {outcome.Analysis.QualityScore:F1}/100", iteration, maxIterations);
            if (!string.IsNullOrEmpty(outcome.Analysis.PrimaryDial))
            {
                Write(sink, BalanceTuningLogLevel.Info, $"Primary dial: {outcome.Analysis.PrimaryDial}", iteration, maxIterations);
                Write(sink, BalanceTuningLogLevel.Info, $"Diagnosis: {outcome.Analysis.DialDiagnosis}", iteration, maxIterations);
            }
            Write(sink, BalanceTuningLogLevel.Info, $"Summary: {outcome.Analysis.Summary}", iteration, maxIterations);
            WriteValidation(sink, outcome.Validation, iteration, maxIterations);

            var top = outcome.Analysis.Suggestions.FirstOrDefault();
            if (top != null)
            {
                Write(sink, BalanceTuningLogLevel.Idea, "Recommended adjustment (one knob):", iteration, maxIterations);
                Write(sink, BalanceTuningLogLevel.Info, $"  {top.Parameter}: {top.CurrentValue} → {top.SuggestedValue}", iteration, maxIterations);
                Write(sink, BalanceTuningLogLevel.Info, $"  Reason: {top.Reason}", iteration, maxIterations);
                Write(sink, BalanceTuningLogLevel.Info, $"  Impact: {top.Impact}", iteration, maxIterations);
            }
            else if (outcome.AllChecksPass)
            {
                Write(sink, BalanceTuningLogLevel.Success, "All checks pass. No adjustment recommended.", iteration, maxIterations);
            }
            else
            {
                Write(sink, BalanceTuningLogLevel.Warning, "No automated suggestion available.", iteration, maxIterations);
            }

            session.Analysis = new AnalysisSessionDto
            {
                QualityScore = outcome.Analysis.QualityScore,
                Summary = outcome.Analysis.Summary,
                AllAnchorsPass = outcome.AllChecksPass,
                PrimaryDial = outcome.Analysis.PrimaryDial,
                DialDiagnosis = outcome.Analysis.DialDiagnosis,
                ValidationWarnings = outcome.Validation.Warnings,
                ValidationErrors = outcome.Validation.Errors,
                TopSuggestion = top == null ? null : ToSuggestionDto(top)
            };
            LevelTuningSessionStore.Save(session, sessionPath);

            Write(sink, BalanceTuningLogLevel.Info, "Analysis saved to session.", iteration, maxIterations);

            return Task.FromResult(outcome.AllChecksPass ? 0 : 1);
        }

        private static Task<int> RunApplyCoreAsync(
            bool dryRun,
            string? sessionPath,
            IBalanceTuningRunSink? sink,
            int iteration,
            int maxIterations)
        {
            var session = LevelTuningSessionStore.Load(sessionPath);
            if (session.Analysis?.TopSuggestion == null)
            {
                Write(sink, BalanceTuningLogLevel.Error, "No suggestion in session. Run TUNEANALYZE first.", iteration, maxIterations);
                return Task.FromResult(2);
            }

            var suggestion = session.Analysis.TopSuggestion;
            string profileLabel = session.ProfileId ?? "tuning";

            Write(sink, BalanceTuningLogLevel.Info, $"=== TUNEAPPLY ({profileLabel}) ===", iteration, maxIterations);
            Write(sink, BalanceTuningLogLevel.Change,
                $"  {suggestion.Parameter}: {suggestion.CurrentValue} → {suggestion.SuggestedValue}", iteration, maxIterations);
            Write(sink, BalanceTuningLogLevel.Info, $"  Reason: {suggestion.Reason}", iteration, maxIterations);

            if (Math.Abs(suggestion.SuggestedValue - suggestion.CurrentValue) < 0.0001)
            {
                Write(sink, BalanceTuningLogLevel.Warning,
                    "Suggestion is a no-op (suggested equals current). Skipping apply.", iteration, maxIterations);
                return Task.FromResult(0);
            }

            if (dryRun)
            {
                Write(sink, BalanceTuningLogLevel.Warning, "Dry run — no changes written.", iteration, maxIterations);
                return Task.FromResult(0);
            }

            var tuningSuggestion = new AutomatedTuningEngine.TuningSuggestion
            {
                Id = suggestion.Id,
                Category = suggestion.Category,
                Target = session.ProfileName ?? profileLabel,
                Parameter = suggestion.Parameter,
                CurrentValue = suggestion.CurrentValue,
                SuggestedValue = suggestion.SuggestedValue,
                Reason = suggestion.Reason,
                Impact = suggestion.Impact
            };

            bool applied = AutomatedTuningEngine.ApplySuggestion(tuningSuggestion);
            if (!applied)
            {
                Write(sink, BalanceTuningLogLevel.Error, "Failed to apply suggestion.", iteration, maxIterations);
                session.LastApply = new ApplySessionDto
                {
                    AppliedUtc = DateTime.UtcNow,
                    SuggestionId = suggestion.Id,
                    Success = false,
                    Message = "ApplySuggestion returned false"
                };
                LevelTuningSessionStore.Save(session, sessionPath);
                return Task.FromResult(3);
            }

            GameConfiguration.Instance.SaveToFile();
            GameConfiguration.ResetInstance();
            _ = GameConfiguration.Instance;

            string patchName = $"{profileLabel}_apply_{DateTime.Now:yyyyMMdd_HHmmss}";
            try
            {
                var patch = BalancePatchManager.CreatePatch(
                    patchName,
                    "BalanceTuningWorkflow",
                    suggestion.Reason,
                    "1.0",
                    new List<string> { profileLabel, "tune-apply" });
                BalancePatchManager.SavePatch(patch);
            }
            catch (Exception ex)
            {
                Write(sink, BalanceTuningLogLevel.Warning, $"Config saved but patch export failed: {ex.Message}", iteration, maxIterations);
            }

            session.LastApply = new ApplySessionDto
            {
                AppliedUtc = DateTime.UtcNow,
                SuggestionId = suggestion.Id,
                Success = true,
                PatchName = patchName,
                Message = "Applied and saved to active balance config"
            };
            session.Analysis = null;
            LevelTuningSessionStore.Save(session, sessionPath);

            Write(sink, BalanceTuningLogLevel.Success, "Applied successfully. Balance config saved.", iteration, maxIterations);
            Write(sink, BalanceTuningLogLevel.Info, $"Patch: {patchName}", iteration, maxIterations);

            return Task.FromResult(0);
        }

        private static void WriteValidation(
            IBalanceTuningRunSink? sink,
            BalanceValidator.ValidationResult validation,
            int iteration,
            int maxIterations)
        {
            foreach (var w in validation.Warnings)
                Write(sink, BalanceTuningLogLevel.Warning, $"  - {w}", iteration, maxIterations);
            foreach (var e in validation.Errors)
                Write(sink, BalanceTuningLogLevel.Error, $"  - {e}", iteration, maxIterations);
        }

        private static void Write(
            IBalanceTuningRunSink? sink,
            BalanceTuningLogLevel level,
            string message,
            int iteration,
            int maxIterations)
        {
            if (sink != null)
                sink.Log(level, message, iteration, maxIterations);
            else
                Console.WriteLine(message);
        }

        private static IProgress<(int completed, int total, string status)> CreateProgressReporter(
            IBalanceTuningRunSink? sink,
            int iteration,
            int maxIterations) =>
            new Progress<(int completed, int total, string status)>(report =>
            {
                if (sink != null)
                {
                    sink.ReportProgress(report.completed, report.total, report.status, iteration, maxIterations);
                    return;
                }

                int pct = report.total > 0 ? (int)((double)report.completed / report.total * 100) : 0;
                Console.Write($"\r[{report.completed}/{report.total}] {pct}% - {report.status}".PadRight(90));
                if (report.completed >= report.total && report.total > 0)
                    Console.WriteLine();
            });

        private static SuggestionDto ToSuggestionDto(TuningSuggestion s) =>
            new()
            {
                Id = s.Id,
                Category = s.Category,
                Parameter = s.Parameter,
                CurrentValue = s.CurrentValue,
                SuggestedValue = s.SuggestedValue,
                Reason = s.Reason,
                Impact = s.Impact
            };

        private static AnalysisProfileConfig CloneAnalysisConfig(AnalysisProfileConfig source)
        {
            var clone = new AnalysisProfileConfig
            {
                OptimizeWinRate = source.OptimizeWinRate,
                Validators = new List<string>(source.Validators),
                Suggesters = new List<string>(source.Suggesters),
                MaxSuggestions = source.MaxSuggestions
            };

            if (source.FundamentalsTargets != null)
            {
                var t = source.FundamentalsTargets;
                clone.FundamentalsTargets = new FundamentalsAnalysisTargets
                {
                    TargetMedianPlayerTurns = t.TargetMedianPlayerTurns,
                    TargetMedianEnemyTurns = t.TargetMedianEnemyTurns,
                    TargetMedianCombinedActions = t.TargetMedianCombinedActions,
                    MinAverageActions = t.MinAverageActions,
                    MaxAverageActions = t.MaxAverageActions,
                    MinAverageComboStreakRuns2Plus = t.MinAverageComboStreakRuns2Plus,
                    MinAverageMaxComboStreak = t.MinAverageMaxComboStreak,
                    TempoTolerance = t.TempoTolerance,
                    RequireL1AnchorBeforeScaling = t.RequireL1AnchorBeforeScaling
                };
            }

            clone.EnableDialRouting = source.EnableDialRouting;

            return clone;
        }

        public static async Task EnsureGameInitializedAsync()
        {
            var gameWrapper = new GameWrapper();
            McpTools.SetGameWrapper(gameWrapper);
            try
            {
                gameWrapper.InitializeGame();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Note: {ex.Message}");
            }

            await Task.CompletedTask;
        }
    }
}
