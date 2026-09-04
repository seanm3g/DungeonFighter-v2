using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using RPGGame.ActionInteractionLab;
using RPGGame.Tuning;
using RPGGame.UI.Avalonia.Settings.ViewModels;

namespace RPGGame.Tuning.LabBalance
{
    /// <summary>View model for <see cref="UI.Avalonia.ActionInteractionLab.ActionLabBalanceWindow"/>.</summary>
    public sealed class ActionLabBalanceViewModel : INotifyPropertyChanged
    {
        private LabBalanceProcessLayer _selectedLayer = LabBalanceProcessLayer.CombatEquation;
        private string _scenarioSummary = "";
        private string _targetRecipe = LabBalanceTargetEvaluator.FormatRecipeForLayer(
            LabBalanceLayerDefinition.Get(LabBalanceProcessLayer.CombatEquation));
        private string _reportText = "Run Stats to generate a report.";
        private string _suggestionText = "No suggestion yet.";
        private string _statusText = "";
        private bool _isBusy;
        private int _autoIterations = 10;
        private bool _loadingTargets;
        private LabBalanceStatsRunResult? _lastRun;
        private LabBalanceScenarioSummary? _lastScenario;

        public ObservableCollection<LabBalanceLayerDefinition> Layers { get; } = new(
            LabBalanceLayerDefinition.All);

        public ObservableCollection<CombatTuningParameterViewModel> Knobs { get; } = new();

        public LabBalanceLayerDefinition SelectedLayerDefinition
        {
            get => LabBalanceLayerDefinition.Get(SelectedLayer);
            set
            {
                if (value == null || value.Layer == SelectedLayer)
                    return;
                SelectedLayer = value.Layer;
            }
        }

        public LabBalanceProcessLayer SelectedLayer
        {
            get => _selectedLayer;
            set
            {
                if (_selectedLayer == value)
                    return;
                _selectedLayer = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedLayerDefinition));
                LoadTargetEditorFromDefinition();
                RefreshTargetRecipe();
            }
        }

        public string ScenarioSummary
        {
            get => _scenarioSummary;
            set { _scenarioSummary = value; OnPropertyChanged(); }
        }

        public string TargetRecipe
        {
            get => _targetRecipe;
            set { _targetRecipe = value; OnPropertyChanged(); }
        }

        public string ReportText
        {
            get => _reportText;
            set { _reportText = value; OnPropertyChanged(); }
        }

        public string SuggestionText
        {
            get => _suggestionText;
            set { _suggestionText = value; OnPropertyChanged(); }
        }

        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanInteract)); }
        }

        public bool CanInteract => !IsBusy;

        /// <summary>Max RUN loop iterations (timeout when goals are not met).</summary>
        public int AutoIterations
        {
            get => _autoIterations;
            set
            {
                int clamped = Math.Clamp(value, 1, 50);
                if (_autoIterations == clamped)
                    return;
                _autoIterations = clamped;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AutoIterationsDecimal));
            }
        }

        /// <summary>Avalonia <c>NumericUpDown.Value</c> is <c>decimal?</c>; bind this, not <see cref="AutoIterations"/>.</summary>
        public decimal? AutoIterationsDecimal
        {
            get => _autoIterations;
            set => AutoIterations = value.HasValue ? (int)value.Value : 10;
        }

        public bool ShowTargetEditor => SelectedLayerDefinition.StatsMode != LabBalanceStatsMode.DeepLinkOnly;
        public bool ShowDurationTargets => SelectedLayerDefinition.HasFlag(LabBalanceCheckFlags.Duration);
        public bool ShowComboTargets => SelectedLayerDefinition.HasFlag(LabBalanceCheckFlags.Combo);
        public bool ShowFeelVarianceTarget => SelectedLayerDefinition.HasFlag(LabBalanceCheckFlags.FeelVariance);
        public bool ShowWinRateFixedTargets => SelectedLayerDefinition.HasFlag(LabBalanceCheckFlags.WinRateFixed);
        public bool ShowWeaponSpreadTarget => SelectedLayerDefinition.HasFlag(LabBalanceCheckFlags.WeaponSpread);
        public bool ShowDungeonClearTargets => SelectedLayerDefinition.HasFlag(LabBalanceCheckFlags.DungeonClear);

        public decimal? TargetMedianCombined
        {
            get => (decimal)SelectedLayerDefinition.Targets.TargetMedianCombinedActions;
            set => SetTargetDouble(v => SelectedLayerDefinition.Targets.TargetMedianCombinedActions = v, value, 27);
        }

        public decimal? TargetMedianHeroTurns
        {
            get => (decimal)SelectedLayerDefinition.Targets.TargetMedianPlayerTurns;
            set => SetTargetDouble(v => SelectedLayerDefinition.Targets.TargetMedianPlayerTurns = v, value, 12);
        }

        public decimal? TargetMedianEnemyTurns
        {
            get => (decimal)SelectedLayerDefinition.Targets.TargetMedianEnemyTurns;
            set => SetTargetDouble(v => SelectedLayerDefinition.Targets.TargetMedianEnemyTurns = v, value, 12);
        }

        public decimal? TargetTempoTolerance
        {
            get => (decimal)SelectedLayerDefinition.Targets.TempoTolerance;
            set => SetTargetDouble(v => SelectedLayerDefinition.Targets.TempoTolerance = Math.Max(0.1, v), value, 1.5);
        }

        public decimal? TargetMeanMin
        {
            get => (decimal)SelectedLayerDefinition.Targets.MinAverageActions;
            set => SetTargetDouble(v => SelectedLayerDefinition.Targets.MinAverageActions = v, value, 24);
        }

        public decimal? TargetMeanMax
        {
            get => (decimal)SelectedLayerDefinition.Targets.MaxAverageActions;
            set => SetTargetDouble(v => SelectedLayerDefinition.Targets.MaxAverageActions = v, value, 30);
        }

        public decimal? TargetMinComboRuns
        {
            get => (decimal)SelectedLayerDefinition.Targets.MinAverageComboStreakRuns2Plus;
            set => SetTargetDouble(v => SelectedLayerDefinition.Targets.MinAverageComboStreakRuns2Plus = v, value, 0.5);
        }

        public decimal? TargetMinMaxComboStreak
        {
            get => (decimal)SelectedLayerDefinition.Targets.MinAverageMaxComboStreak;
            set => SetTargetDouble(v => SelectedLayerDefinition.Targets.MinAverageMaxComboStreak = v, value, 2);
        }

        public decimal? TargetMaxTurnStdDev
        {
            get => (decimal)SelectedLayerDefinition.MaxTurnStdDev;
            set => SetTargetDouble(v => SelectedLayerDefinition.MaxTurnStdDev = v, value, 8);
        }

        public decimal? TargetWinRateMinPct
        {
            get => (decimal)(SelectedLayerDefinition.WinRateMin * 100.0);
            set => SetTargetDouble(v => SelectedLayerDefinition.WinRateMin = Math.Clamp(v, 0, 100) / 100.0, value, 90);
        }

        public decimal? TargetWinRateMaxPct
        {
            get => (decimal)(SelectedLayerDefinition.WinRateMax * 100.0);
            set => SetTargetDouble(v => SelectedLayerDefinition.WinRateMax = Math.Clamp(v, 0, 100) / 100.0, value, 99);
        }

        public decimal? TargetWeaponWrSpreadPp
        {
            get => (decimal)(SelectedLayerDefinition.MaxWeaponWinRateSpread * 100.0);
            set => SetTargetDouble(v => SelectedLayerDefinition.MaxWeaponWinRateSpread = Math.Clamp(v, 0, 100) / 100.0, value, 12);
        }

        public decimal? TargetDungeonClearMinPct
        {
            get => (decimal)(SelectedLayerDefinition.MinDungeonClearRate * 100.0);
            set => SetTargetDouble(v => SelectedLayerDefinition.MinDungeonClearRate = Math.Clamp(v, 0, 100) / 100.0, value, 70);
        }

        public decimal? TargetDungeonClearMaxPct
        {
            get => (decimal)(SelectedLayerDefinition.MaxDungeonClearRate * 100.0);
            set => SetTargetDouble(v => SelectedLayerDefinition.MaxDungeonClearRate = Math.Clamp(v, 0, 100) / 100.0, value, 95);
        }

        public LabBalanceStatsRunResult? LastRun => _lastRun;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ActionLabBalanceViewModel()
        {
            LoadTargetEditorFromDefinition();
        }

        public void RefreshTargetRecipe() =>
            TargetRecipe = LabBalanceTargetEvaluator.FormatRecipeForLayer(SelectedLayerDefinition);

        public void LoadTargetEditorFromDefinition()
        {
            _loadingTargets = true;
            try
            {
                OnPropertyChanged(nameof(ShowTargetEditor));
                OnPropertyChanged(nameof(ShowDurationTargets));
                OnPropertyChanged(nameof(ShowComboTargets));
                OnPropertyChanged(nameof(ShowFeelVarianceTarget));
                OnPropertyChanged(nameof(ShowWinRateFixedTargets));
                OnPropertyChanged(nameof(ShowWeaponSpreadTarget));
                OnPropertyChanged(nameof(ShowDungeonClearTargets));
                OnPropertyChanged(nameof(TargetMedianCombined));
                OnPropertyChanged(nameof(TargetMedianHeroTurns));
                OnPropertyChanged(nameof(TargetMedianEnemyTurns));
                OnPropertyChanged(nameof(TargetTempoTolerance));
                OnPropertyChanged(nameof(TargetMeanMin));
                OnPropertyChanged(nameof(TargetMeanMax));
                OnPropertyChanged(nameof(TargetMinComboRuns));
                OnPropertyChanged(nameof(TargetMinMaxComboStreak));
                OnPropertyChanged(nameof(TargetMaxTurnStdDev));
                OnPropertyChanged(nameof(TargetWinRateMinPct));
                OnPropertyChanged(nameof(TargetWinRateMaxPct));
                OnPropertyChanged(nameof(TargetWeaponWrSpreadPp));
                OnPropertyChanged(nameof(TargetDungeonClearMinPct));
                OnPropertyChanged(nameof(TargetDungeonClearMaxPct));
            }
            finally
            {
                _loadingTargets = false;
            }
        }

        public void ResetTargetsToDefaults()
        {
            LabBalanceLayerDefinition.ResetEditableTargets(SelectedLayer);
            LoadTargetEditorFromDefinition();
            RefreshTargetRecipe();
            StatusText = "Checklist targets reset to defaults";
        }

        private void SetTargetDouble(Action<double> apply, decimal? value, double fallback)
        {
            if (_loadingTargets)
                return;
            apply(value.HasValue ? (double)value.Value : fallback);
            RefreshTargetRecipe();
            StatusText = "Checklist targets updated — Run Stats to re-check";
        }

        public void ReloadKnobs()
        {
            Knobs.Clear();
            foreach (var p in LabBalanceScenarioApplicator.GetKnobsForLayer(SelectedLayer))
            {
                var param = p;
                Knobs.Add(new CombatTuningParameterViewModel(param, v =>
                {
                    param.SetValue(v);
                    ActionInteractionLabSession.ApplyTuningToActiveLabHeroIfAny();
                }));
            }
        }

        public void RefreshKnobValues()
        {
            foreach (var k in Knobs)
                k.ReloadFromConfig();
        }

        public void SaveKnobs()
        {
            foreach (var k in Knobs)
            {
                k.FlushPendingText();
                k.CommitToConfig();
            }

            GameConfiguration.Instance.SaveToFile();
            ActionInteractionLabSession.ApplyTuningToActiveLabHeroIfAny();
            StatusText = "Knobs saved to GameConfiguration";
        }

        public LabBalanceScenarioSummary? ApplyScenario(ActionInteractionLabSession session)
        {
            _lastScenario = LabBalanceScenarioApplicator.Apply(session, SelectedLayer);
            ScenarioSummary =
                $"{_lastScenario.Description}\n" +
                $"Weapon: {_lastScenario.WeaponType} | Enemy: {_lastScenario.EnemyType} L{_lastScenario.EnemyLevel} | " +
                $"Hero L{_lastScenario.PlayerLevel} | Forced: {_lastScenario.ForcedCatalogAction}\n" +
                $"Mode: {(SelectedLayerDefinition.StatsMode)} | Weapon-only: {_lastScenario.WeaponOnly}";
            RefreshTargetRecipe();
            LoadTargetEditorFromDefinition();
            ReloadKnobs();
            StatusText = $"Applied layer: {SelectedLayerDefinition.DisplayName}";
            return _lastScenario;
        }

        public async Task RunStatsAsync(ActionInteractionLabSession session)
        {
            if (IsBusy)
                return;
            IsBusy = true;
            StatusText = "Running stats…";
            try
            {
                int n = session.EncounterSimulationBatchCount;
                int dop = session.UseParallelEncounterSimulation ? -1 : 1;
                session.SetEncounterSimulationRunning(true);
                _lastRun = await LabBalanceStatsService.RunStatsAsync(session, n, dop).ConfigureAwait(true);
                ReportText = _lastRun.SummaryText;
                StatusText = FormatRunStatus(_lastRun);
            }
            catch (Exception ex)
            {
                ReportText = ex.ToString();
                StatusText = "Stats failed";
            }
            finally
            {
                session.SetEncounterSimulationRunning(false);
                IsBusy = false;
            }
        }

        public void Suggest(ActionInteractionLabSession session)
        {
            var outcome = LabBalanceStatsService.SuggestOne(session, _lastRun);
            if (outcome.Suggestion == null)
            {
                SuggestionText = outcome.EmptyReason;
                StatusText = outcome.EmptyReason;
                return;
            }

            var suggestion = outcome.Suggestion;
            SuggestionText =
                $"{suggestion.Parameter}: {suggestion.CurrentValue:F3} → {suggestion.SuggestedValue:F3}\n" +
                $"{suggestion.Reason}\n{suggestion.Impact}";
            StatusText = "Suggestion ready — Apply to commit";
        }

        public void ApplySuggestion()
        {
            if (LabBalanceStatsService.PendingSuggestion == null)
            {
                StatusText = "Nothing to apply";
                return;
            }

            if (LabBalanceStatsService.ApplyPendingSuggestion(saveConfig: true))
            {
                RefreshKnobValues();
                StatusText = "Suggestion applied and config saved";
            }
            else
            {
                string param = LabBalanceStatsService.PendingSuggestion?.Parameter ?? "?";
                StatusText = $"Apply failed — could not write '{param}'";
            }
        }

        public async Task RunAutoAsync(ActionInteractionLabSession session)
        {
            if (IsBusy)
                return;
            IsBusy = true;
            StatusText = $"RUN loop (max {AutoIterations})…";
            try
            {
                int n = session.EncounterSimulationBatchCount;
                int dop = session.UseParallelEncounterSimulation ? -1 : 1;
                session.SetEncounterSimulationRunning(true);
                string log = await LabBalanceStatsService.RunAutoLoopAsync(
                    session, AutoIterations, n, dop, stopWhenPass: true).ConfigureAwait(true);
                ReportText = log;
                RefreshKnobValues();
                StatusText = log.Contains("All targets met", StringComparison.Ordinal)
                    ? "RUN loop finished — targets met"
                    : "RUN loop finished — see progress report";
            }
            catch (Exception ex)
            {
                ReportText = ex.ToString();
                StatusText = "RUN loop failed";
            }
            finally
            {
                session.SetEncounterSimulationRunning(false);
                IsBusy = false;
            }
        }

        internal static string FormatRunStatus(LabBalanceStatsRunResult run)
        {
            if (run.DeepLinkOnly)
                return "Open Workbench for playthrough";

            if (run.Targets == null || run.Targets.Checks.Count == 0)
                return "No checklist for this run — see report";

            int n = run.Targets.Checks.Count;
            int k = run.Targets.PassedCount;
            if (run.Targets.AllPassed)
                return $"Targets met ({k}/{n})";
            return $"Targets unmet ({k}/{n}) — Suggest for one knob";
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
