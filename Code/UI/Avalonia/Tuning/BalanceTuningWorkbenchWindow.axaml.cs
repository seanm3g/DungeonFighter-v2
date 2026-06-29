using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using RPGGame.Tuning;
using RPGGame.Tuning.Profiles;
using RPGGame.Tuning.Suggesters;
using RPGGame.UI.Avalonia.Settings;

namespace RPGGame.UI.Avalonia.Tuning
{
    public partial class BalanceTuningWorkbenchWindow : Window, IBalanceTuningRunSink
    {
        private static BalanceTuningWorkbenchWindow? _instance;

        private readonly BalanceTuningRunCoordinator _coordinator = new();
        private CancellationTokenSource? _runCts;
        private readonly StringBuilder _liveLog = new();
        private readonly StringBuilder _changelog = new();
        private readonly StringBuilder _ideas = new();
        private readonly StringBuilder _results = new();
        private int _currentIteration;
        private int _currentMaxIterations;
        private int _lastLoggedProgressCompleted = -1;

        private ComboBox? _profileComboBox;
        private ComboBox? _runModeComboBox;
        private NumericUpDown? _iterationsUpDown;
        private CheckBox? _autoApplyCheckBox;
        private CheckBox? _dryRunCheckBox;
        private CheckBox? _stopWhenPassCheckBox;
        private Button? _startButton;
        private Button? _stopButton;
        private TextBlock? _stepTextBlock;
        private ProgressBar? _runProgressBar;
        private TextBlock? _progressDetailTextBlock;
        private TextBox? _liveLogTextBox;
        private TextBox? _changelogTextBox;
        private TextBox? _ideasTextBox;
        private TextBox? _resultsTextBox;

        private NumericUpDown? _battlesPerCombinationUpDown;
        private NumericUpDown? _encounterCountUpDown;
        private NumericUpDown? _playerLevelUpDown;
        private NumericUpDown? _enemyLevelUpDown;
        private TextBox? _levelsTextBox;
        private ComboBox? _weaponTypeComboBox;
        private TextBox? _enemyTypeTextBox;
        private TextBox? _forcedActionTextBox;
        private NumericUpDown? _battlesPerWeaponUpDown;
        private NumericUpDown? _numberOfBattlesUpDown;
        private CheckBox? _optimizeWinRateCheckBox;
        private NumericUpDown? _maxSuggestionsUpDown;
        private NumericUpDown? _targetCombinedUpDown;
        private NumericUpDown? _targetHeroTurnsUpDown;
        private NumericUpDown? _targetEnemyTurnsUpDown;
        private NumericUpDown? _minAverageActionsUpDown;
        private NumericUpDown? _maxAverageActionsUpDown;
        private NumericUpDown? _minComboStreakRunsUpDown;
        private NumericUpDown? _minMaxComboStreakUpDown;
        private TextBlock? _battlesLabel;
        private TextBlock? _encountersLabel;
        private Border? _progressionDiagnosisPanel;
        private TextBlock? _worstAnchorTextBlock;
        private TextBlock? _primaryAxisTextBlock;
        private TextBlock? _suggestionSummaryTextBlock;
        private Button? _openProgressionCurveButton;

        public BalanceTuningWorkbenchWindow()
        {
            InitializeComponent();
            WireControls();
            LoadProfiles();
            PopulateRunModes();
            PopulateWeaponTypes();

            if (_startButton != null)
                _startButton.Click += OnStartClick;
            if (_stopButton != null)
                _stopButton.Click += OnStopClick;
            var clearButton = this.FindControl<Button>("ClearButton");
            if (clearButton != null)
                clearButton.Click += OnClearClick;

            if (_profileComboBox != null)
                _profileComboBox.SelectionChanged += OnProfileSelectionChanged;

            Closed += (_, _) => { if (ReferenceEquals(_instance, this)) _instance = null; };
        }

        public static void Open(Window? owner)
        {
            if (_instance != null)
            {
                _instance.Activate();
                return;
            }

            var window = new BalanceTuningWorkbenchWindow();
            _instance = window;
            if (owner != null)
                window.Show(owner);
            else
                window.Show();
        }

        private void WireControls()
        {
            _profileComboBox = this.FindControl<ComboBox>("ProfileComboBox");
            _runModeComboBox = this.FindControl<ComboBox>("RunModeComboBox");
            _iterationsUpDown = this.FindControl<NumericUpDown>("IterationsUpDown");
            _autoApplyCheckBox = this.FindControl<CheckBox>("AutoApplyCheckBox");
            _dryRunCheckBox = this.FindControl<CheckBox>("DryRunCheckBox");
            _stopWhenPassCheckBox = this.FindControl<CheckBox>("StopWhenPassCheckBox");
            _startButton = this.FindControl<Button>("StartButton");
            _stopButton = this.FindControl<Button>("StopButton");
            _stepTextBlock = this.FindControl<TextBlock>("StepTextBlock");
            _runProgressBar = this.FindControl<ProgressBar>("RunProgressBar");
            _progressDetailTextBlock = this.FindControl<TextBlock>("ProgressDetailTextBlock");
            _liveLogTextBox = this.FindControl<TextBox>("LiveLogTextBox");
            _changelogTextBox = this.FindControl<TextBox>("ChangelogTextBox");
            _ideasTextBox = this.FindControl<TextBox>("IdeasTextBox");
            _resultsTextBox = this.FindControl<TextBox>("ResultsTextBox");

            _battlesPerCombinationUpDown = this.FindControl<NumericUpDown>("BattlesPerCombinationUpDown");
            _encounterCountUpDown = this.FindControl<NumericUpDown>("EncounterCountUpDown");
            _playerLevelUpDown = this.FindControl<NumericUpDown>("PlayerLevelUpDown");
            _enemyLevelUpDown = this.FindControl<NumericUpDown>("EnemyLevelUpDown");
            _levelsTextBox = this.FindControl<TextBox>("LevelsTextBox");
            _weaponTypeComboBox = this.FindControl<ComboBox>("WeaponTypeComboBox");
            _enemyTypeTextBox = this.FindControl<TextBox>("EnemyTypeTextBox");
            _forcedActionTextBox = this.FindControl<TextBox>("ForcedActionTextBox");
            _battlesPerWeaponUpDown = this.FindControl<NumericUpDown>("BattlesPerWeaponUpDown");
            _numberOfBattlesUpDown = this.FindControl<NumericUpDown>("NumberOfBattlesUpDown");
            _optimizeWinRateCheckBox = this.FindControl<CheckBox>("OptimizeWinRateCheckBox");
            _maxSuggestionsUpDown = this.FindControl<NumericUpDown>("MaxSuggestionsUpDown");
            _targetCombinedUpDown = this.FindControl<NumericUpDown>("TargetCombinedUpDown");
            _targetHeroTurnsUpDown = this.FindControl<NumericUpDown>("TargetHeroTurnsUpDown");
            _targetEnemyTurnsUpDown = this.FindControl<NumericUpDown>("TargetEnemyTurnsUpDown");
            _minAverageActionsUpDown = this.FindControl<NumericUpDown>("MinAverageActionsUpDown");
            _maxAverageActionsUpDown = this.FindControl<NumericUpDown>("MaxAverageActionsUpDown");
            _minComboStreakRunsUpDown = this.FindControl<NumericUpDown>("MinComboStreakRunsUpDown");
            _minMaxComboStreakUpDown = this.FindControl<NumericUpDown>("MinMaxComboStreakUpDown");
            _battlesLabel = this.FindControl<TextBlock>("BattlesLabel");
            _encountersLabel = this.FindControl<TextBlock>("EncountersLabel");
            _progressionDiagnosisPanel = this.FindControl<Border>("ProgressionDiagnosisPanel");
            _worstAnchorTextBlock = this.FindControl<TextBlock>("WorstAnchorTextBlock");
            _primaryAxisTextBlock = this.FindControl<TextBlock>("PrimaryAxisTextBlock");
            _suggestionSummaryTextBlock = this.FindControl<TextBlock>("SuggestionSummaryTextBlock");
            _openProgressionCurveButton = this.FindControl<Button>("OpenProgressionCurveButton");
            if (_openProgressionCurveButton != null)
                _openProgressionCurveButton.Click += OnOpenProgressionCurveClick;
        }

        private void OnOpenProgressionCurveClick(object? sender, RoutedEventArgs e) =>
            CombatTuningNavigation.RequestOpenProgressionCurveInSettings();

        private void PopulateWeaponTypes()
        {
            if (_weaponTypeComboBox == null)
                return;

            _weaponTypeComboBox.ItemsSource = new[] { "Sword", "Dagger", "Mace", "Wand" };
            _weaponTypeComboBox.SelectedIndex = 0;
        }

        private void LoadProfiles()
        {
            if (_profileComboBox == null)
                return;

            var profiles = BalanceTuningProfileLoader.ListProfiles()
                .Select(p => new ProfileListItem(p))
                .ToList();

            if (profiles.Count == 0)
            {
                profiles.Add(new ProfileListItem(new BalanceTuningProfile
                {
                    Id = "(none)",
                    Name = "No profiles found — check GameData/TuningProfiles"
                }));
            }

            _profileComboBox.ItemsSource = profiles;
            _profileComboBox.SelectedIndex = 0;
            if (_profileComboBox.SelectedItem is ProfileListItem first)
                LoadParametersFromProfile(first.Profile);
        }

        private void OnProfileSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_profileComboBox?.SelectedItem is not ProfileListItem item || item.Profile.Id == "(none)")
                return;

            LoadParametersFromProfile(item.Profile);
        }

        private void LoadParametersFromProfile(BalanceTuningProfile profile)
        {
            var options = new BalanceTuningRunOptions { ProfileId = profile.Id };
            BalanceTuningProfileApplicator.PopulateOptionsFromProfile(profile, options);

            SetNumeric(_battlesPerCombinationUpDown, options.BattlesPerCombination);
            SetNumeric(_encounterCountUpDown, options.EncounterCount);
            SetNumeric(_playerLevelUpDown, options.PlayerLevel);
            SetNumeric(_enemyLevelUpDown, options.EnemyLevel);
            SetNumeric(_battlesPerWeaponUpDown, options.BattlesPerWeapon);
            SetNumeric(_numberOfBattlesUpDown, options.NumberOfBattles);

            if (_levelsTextBox != null)
                _levelsTextBox.Text = options.LevelsCsv ?? "";
            if (_enemyTypeTextBox != null)
                _enemyTypeTextBox.Text = options.EnemyType ?? "";
            if (_forcedActionTextBox != null)
                _forcedActionTextBox.Text = options.ForcedCatalogAction ?? "";
            if (_weaponTypeComboBox != null && !string.IsNullOrWhiteSpace(options.WeaponType))
            {
                _weaponTypeComboBox.SelectedItem = options.WeaponType;
                if (_weaponTypeComboBox.SelectedIndex < 0)
                    _weaponTypeComboBox.SelectedIndex = 0;
            }

            if (_optimizeWinRateCheckBox != null)
                _optimizeWinRateCheckBox.IsChecked = options.OptimizeWinRate == true;
            SetNumeric(_maxSuggestionsUpDown, options.MaxSuggestions);
            SetNumeric(_targetCombinedUpDown, options.TargetMedianCombinedActions);
            SetNumeric(_targetHeroTurnsUpDown, options.TargetMedianPlayerTurns);
            SetNumeric(_targetEnemyTurnsUpDown, options.TargetMedianEnemyTurns);
            SetNumeric(_minAverageActionsUpDown, options.MinAverageActions);
            SetNumeric(_maxAverageActionsUpDown, options.MaxAverageActions);
            SetNumeric(_minComboStreakRunsUpDown, options.MinAverageComboStreakRuns2Plus);
            SetNumeric(_minMaxComboStreakUpDown, options.MinAverageMaxComboStreak);

            UpdateFieldVisibility(profile.Simulation.Mode);
        }

        private void UpdateFieldVisibility(string mode)
        {
            bool fundamentals = mode == TuningSimulationModes.FundamentalsEncounter;
            bool multiLevel = mode == TuningSimulationModes.MultiLevelWeaponEnemy;

            SetControlEnabled(_encounterCountUpDown, fundamentals);
            SetControlEnabled(_battlesPerCombinationUpDown, !fundamentals);
            SetControlEnabled(_levelsTextBox, multiLevel);
            SetControlEnabled(_weaponTypeComboBox, fundamentals);
            SetControlEnabled(_enemyTypeTextBox, fundamentals);
            SetControlEnabled(_forcedActionTextBox, fundamentals);

            if (_encountersLabel != null)
                _encountersLabel.Foreground = fundamentals ? Brushes.White : Brushes.Gray;
            if (_battlesLabel != null)
                _battlesLabel.Foreground = !fundamentals ? Brushes.White : Brushes.Gray;
        }

        private static void SetNumeric(NumericUpDown? control, int? value)
        {
            if (control != null && value != null)
                control.Value = value.Value;
        }

        private static void SetNumeric(NumericUpDown? control, double? value)
        {
            if (control != null && value != null)
                control.Value = (decimal)value.Value;
        }

        private static void SetControlEnabled(Control? control, bool enabled)
        {
            if (control != null)
                control.IsEnabled = enabled;
        }

        private void PopulateRunModes()
        {
            if (_runModeComboBox == null)
                return;

            _runModeComboBox.ItemsSource = new[]
            {
                new RunModeItem(BalanceTuningRunMode.SimulateAndAnalyze, "Simulate + analyze (recommended)"),
                new RunModeItem(BalanceTuningRunMode.SimulateOnly, "Simulate only"),
                new RunModeItem(BalanceTuningRunMode.AnalyzeOnly, "Analyze only (uses saved session)"),
                new RunModeItem(BalanceTuningRunMode.FullCycle, "Full cycle (sim → analyze → apply)"),
            };
            _runModeComboBox.SelectedIndex = 0;
        }

        private BalanceTuningRunOptions BuildRunOptions(string profileId)
        {
            return new BalanceTuningRunOptions
            {
                ProfileId = profileId,
                MaxIterations = (int)(_iterationsUpDown?.Value ?? 1),
                AutoApply = _autoApplyCheckBox?.IsChecked == true,
                DryRunApply = _dryRunCheckBox?.IsChecked == true,
                StopWhenChecksPass = _stopWhenPassCheckBox?.IsChecked != false,
                Mode = _runModeComboBox?.SelectedItem is RunModeItem modeItem
                    ? modeItem.Mode
                    : BalanceTuningRunMode.SimulateAndAnalyze,
                BattlesPerCombination = (int?)_battlesPerCombinationUpDown?.Value,
                EncounterCount = (int?)_encounterCountUpDown?.Value,
                BattlesPerWeapon = (int?)_battlesPerWeaponUpDown?.Value,
                NumberOfBattles = (int?)_numberOfBattlesUpDown?.Value,
                PlayerLevel = (int?)_playerLevelUpDown?.Value,
                EnemyLevel = (int?)_enemyLevelUpDown?.Value,
                LevelsCsv = _levelsTextBox?.Text,
                WeaponType = _weaponTypeComboBox?.SelectedItem?.ToString(),
                EnemyType = _enemyTypeTextBox?.Text,
                ForcedCatalogAction = _forcedActionTextBox?.Text,
                OptimizeWinRate = _optimizeWinRateCheckBox?.IsChecked == true,
                MaxSuggestions = (int?)_maxSuggestionsUpDown?.Value,
                TargetMedianCombinedActions = (double?)_targetCombinedUpDown?.Value,
                TargetMedianPlayerTurns = (double?)_targetHeroTurnsUpDown?.Value,
                TargetMedianEnemyTurns = (double?)_targetEnemyTurnsUpDown?.Value,
                MinAverageActions = (double?)_minAverageActionsUpDown?.Value,
                MaxAverageActions = (double?)_maxAverageActionsUpDown?.Value,
                MinAverageComboStreakRuns2Plus = (double?)_minComboStreakRunsUpDown?.Value,
                MinAverageMaxComboStreak = (double?)_minMaxComboStreakUpDown?.Value
            };
        }

        private async void OnStartClick(object? sender, RoutedEventArgs e)
        {
            if (_runCts != null || _profileComboBox == null)
                return;

            if (_profileComboBox.SelectedItem is not ProfileListItem profileItem)
                return;

            if (profileItem.Profile.Id == "(none)")
            {
                AppendLiveLog("No tuning profiles loaded. Run from repo root or check GameData/TuningProfiles.", BalanceTuningLogLevel.Error);
                return;
            }

            var options = BuildRunOptions(profileItem.Profile.Id);

            _runCts = new CancellationTokenSource();
            _lastLoggedProgressCompleted = -1;
            SetRunningState(true);
            AppendLiveLog($"Starting {profileItem.Profile.Name} — {options.MaxIterations} iteration(s), mode {options.Mode}.");

            try
            {
                await Task.Run(async () =>
                        await _coordinator.RunAsync(options, this, _runCts.Token).ConfigureAwait(false))
                    .ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                AppendLiveLog($"Run failed: {ex.Message}", BalanceTuningLogLevel.Error);
                SetStep(BalanceTuningRunStep.Failed, 0, options.MaxIterations, ex.Message);
            }
            finally
            {
                _runCts?.Dispose();
                _runCts = null;
                SetRunningState(false);
            }
        }

        private void OnStopClick(object? sender, RoutedEventArgs e)
        {
            _runCts?.Cancel();
            AppendLiveLog("Stop requested...", BalanceTuningLogLevel.Warning);
        }

        private void OnClearClick(object? sender, RoutedEventArgs e)
        {
            _liveLog.Clear();
            _changelog.Clear();
            _ideas.Clear();
            _results.Clear();
            if (_liveLogTextBox != null) _liveLogTextBox.Text = "";
            if (_changelogTextBox != null) _changelogTextBox.Text = "";
            if (_ideasTextBox != null) _ideasTextBox.Text = "";
            if (_resultsTextBox != null) _resultsTextBox.Text = "";
            if (_stepTextBlock != null)
                _stepTextBlock.Text = "Idle — select a profile and press Start";
            if (_progressDetailTextBlock != null)
                _progressDetailTextBlock.Text = "";
            if (_runProgressBar != null)
                _runProgressBar.Value = 0;
            HideProgressionDiagnosis();
        }

        private void HideProgressionDiagnosis()
        {
            if (_progressionDiagnosisPanel != null)
                _progressionDiagnosisPanel.IsVisible = false;
        }

        private void UpdateProgressionDiagnosis(BalanceTuningIterationResult result)
        {
            var session = LevelTuningSessionStore.Load();
            var fundamentals = session.Fundamentals != null
                ? ComprehensiveSimulationMapper.ToFundamentalsResult(session.Fundamentals)
                : null;

            if (fundamentals == null || fundamentals.LevelSnapshots.Count == 0)
            {
                Dispatcher.UIThread.Post(HideProgressionDiagnosis);
                return;
            }

            FundamentalsAnalysisTargets targets;
            try
            {
                targets = BalanceTuningProfileLoader.Load(result.ProfileId).Analysis?.FundamentalsTargets
                          ?? new FundamentalsAnalysisTargets();
            }
            catch
            {
                targets = new FundamentalsAnalysisTargets();
            }
            var diagnosis = FundamentalsDurationAdjustmentSuggester.Diagnose(fundamentals, targets);

            Dispatcher.UIThread.Post(() =>
            {
                if (_progressionDiagnosisPanel == null)
                    return;

                _progressionDiagnosisPanel.IsVisible = true;
                if (_worstAnchorTextBlock != null)
                {
                    _worstAnchorTextBlock.Text = diagnosis.WorstAnchorLevel.HasValue
                        ? $"Worst anchor: L{diagnosis.WorstAnchorLevel} (decade measurement point farthest from targets)"
                        : "Worst anchor: — (all decade anchors in band)";
                }

                if (_primaryAxisTextBlock != null)
                    _primaryAxisTextBlock.Text = $"Primary axis: {diagnosis.PrimaryAxis}";

                if (_suggestionSummaryTextBlock != null)
                {
                    _suggestionSummaryTextBlock.Text = diagnosis.SuggestionSummary != null
                        ? $"Suggested knob: {diagnosis.SuggestionSummary}"
                        : result.AllChecksPass
                            ? "No adjustment needed — checks pass."
                            : "No single-knob suggestion this iteration.";
                }
            });
        }

        private void SetRunningState(bool running)
        {
            if (_startButton != null) _startButton.IsEnabled = !running;
            if (_stopButton != null) _stopButton.IsEnabled = running;
            if (_profileComboBox != null) _profileComboBox.IsEnabled = !running;
            if (_runModeComboBox != null) _runModeComboBox.IsEnabled = !running;
            if (_iterationsUpDown != null) _iterationsUpDown.IsEnabled = !running;
            if (_autoApplyCheckBox != null) _autoApplyCheckBox.IsEnabled = !running;
            if (_dryRunCheckBox != null) _dryRunCheckBox.IsEnabled = !running;
            if (_stopWhenPassCheckBox != null) _stopWhenPassCheckBox.IsEnabled = !running;

            bool paramEnabled = !running;
            SetControlEnabled(_battlesPerCombinationUpDown, paramEnabled);
            SetControlEnabled(_encounterCountUpDown, paramEnabled);
            SetControlEnabled(_playerLevelUpDown, paramEnabled);
            SetControlEnabled(_enemyLevelUpDown, paramEnabled);
            SetControlEnabled(_levelsTextBox, paramEnabled);
            SetControlEnabled(_weaponTypeComboBox, paramEnabled);
            SetControlEnabled(_enemyTypeTextBox, paramEnabled);
            SetControlEnabled(_forcedActionTextBox, paramEnabled);
            SetControlEnabled(_battlesPerWeaponUpDown, paramEnabled);
            SetControlEnabled(_numberOfBattlesUpDown, paramEnabled);
            SetControlEnabled(_optimizeWinRateCheckBox, paramEnabled);
            SetControlEnabled(_maxSuggestionsUpDown, paramEnabled);
            SetControlEnabled(_targetCombinedUpDown, paramEnabled);
            SetControlEnabled(_targetHeroTurnsUpDown, paramEnabled);
            SetControlEnabled(_targetEnemyTurnsUpDown, paramEnabled);
            SetControlEnabled(_minAverageActionsUpDown, paramEnabled);
            SetControlEnabled(_maxAverageActionsUpDown, paramEnabled);
            SetControlEnabled(_minComboStreakRunsUpDown, paramEnabled);
            SetControlEnabled(_minMaxComboStreakUpDown, paramEnabled);

            if (!running && _profileComboBox?.SelectedItem is ProfileListItem item && item.Profile.Id != "(none)")
                UpdateFieldVisibility(item.Profile.Simulation.Mode);
        }

        public void SetStep(BalanceTuningRunStep step, int iteration, int maxIterations, string? detail = null)
        {
            _currentIteration = iteration;
            _currentMaxIterations = maxIterations;
            Dispatcher.UIThread.Post(() =>
            {
                if (_stepTextBlock == null)
                    return;

                string label = step switch
                {
                    BalanceTuningRunStep.Simulate => $"Iteration {iteration}/{maxIterations} — Simulating",
                    BalanceTuningRunStep.Analyze => $"Iteration {iteration}/{maxIterations} — Analyzing",
                    BalanceTuningRunStep.Apply => $"Iteration {iteration}/{maxIterations} — Applying",
                    BalanceTuningRunStep.Completed => "Completed",
                    BalanceTuningRunStep.Cancelled => "Cancelled",
                    BalanceTuningRunStep.Failed => "Failed",
                    BalanceTuningRunStep.Initializing => "Initializing",
                    _ => "Idle"
                };
                _stepTextBlock.Text = string.IsNullOrWhiteSpace(detail) ? label : $"{label}: {detail}";
            });
        }

        public void Log(BalanceTuningLogLevel level, string message, int iteration = 0, int maxIterations = 0)
        {
            if (level == BalanceTuningLogLevel.Progress)
                return;
            AppendLiveLog(message, level, iteration, maxIterations);
        }

        public void ReportProgress(int completed, int total, string status, int iteration, int maxIterations)
        {
            bool shouldLog = SimulationProgressReporter.ShouldReport(completed, total, _lastLoggedProgressCompleted);
            if (shouldLog)
            {
                _lastLoggedProgressCompleted = completed;
                int pct = total > 0 ? (int)((double)completed / total * 100) : 0;
                AppendLiveLog($"{status} ({pct}%)", BalanceTuningLogLevel.Info, iteration, maxIterations);
            }

            Dispatcher.UIThread.Post(() =>
            {
                double pct = total > 0 ? (double)completed / total * 100 : 0;
                if (_runProgressBar != null)
                {
                    if (maxIterations > 0)
                    {
                        double iterBase = (iteration - 1) / (double)maxIterations * 100;
                        double iterSlice = pct / maxIterations;
                        _runProgressBar.Value = Math.Min(100, iterBase + iterSlice);
                    }
                    else
                    {
                        _runProgressBar.Value = pct;
                    }
                }

                if (_progressDetailTextBlock != null)
                    _progressDetailTextBlock.Text = $"[{completed}/{total}] {pct:F0}% — {status}";
            });
        }

        public void RecordIdea(int iteration, string title, string detail)
        {
            string line = $"[Iter {iteration}] {title}\n    {detail}\n";
            _ideas.AppendLine(line.TrimEnd());
            AppendIdeas(line);
            AppendLiveLog($"Idea: {title} — {detail}", BalanceTuningLogLevel.Idea, iteration, _currentMaxIterations);
        }

        public void RecordChange(BalanceTuningChangeEntry entry)
        {
            string line =
                $"[{entry.TimestampUtc:HH:mm:ss}] Iter {entry.Iteration}: {entry.Parameter} {entry.FromValue} → {entry.ToValue}\n" +
                $"  Reason: {entry.Reason}\n" +
                $"  Applied: {(entry.Applied ? "yes" : "no")}" +
                (entry.PatchName != null ? $"  Patch: {entry.PatchName}" : "") + "\n";
            _changelog.AppendLine(line.TrimEnd());
            AppendChangelog(line);
        }

        public void IterationCompleted(BalanceTuningIterationResult result)
        {
            string block =
                $"══════════════════════════════════════\n" +
                $"ITERATION {result.Iteration}/{result.MaxIterations} — {result.ProfileId}\n" +
                $"Quality: {result.QualityScore:F1}/100  Pass: {result.AllChecksPass}  " +
                $"Suggestion: {result.SuggestionAvailable}  Applied: {result.ChangeApplied}\n" +
                $"──────────────────────────────────────\n" +
                $"{result.ReportText}\n\n";
            _results.Append(block);
            AppendResults(block);
            UpdateProgressionDiagnosis(result);
            AppendLiveLog($"Iteration {result.Iteration} complete. Quality {result.QualityScore:F1}/100.",
                BalanceTuningLogLevel.Success, result.Iteration, result.MaxIterations);
        }

        private void AppendLiveLog(string message, BalanceTuningLogLevel level = BalanceTuningLogLevel.Info, int iteration = 0, int maxIterations = 0)
        {
            string prefix = iteration > 0 ? $"[{iteration}/{maxIterations}] " : "";
            string line = $"[{DateTime.Now:HH:mm:ss}] {prefix}{message}\n";
            _liveLog.Append(line);
            Dispatcher.UIThread.Post(() =>
            {
                if (_liveLogTextBox == null)
                    return;
                _liveLogTextBox.Text = _liveLog.ToString();
                _liveLogTextBox.CaretIndex = _liveLogTextBox.Text?.Length ?? 0;
            });
        }

        private void AppendChangelog(string text) =>
            Dispatcher.UIThread.Post(() =>
            {
                if (_changelogTextBox != null)
                    _changelogTextBox.Text = _changelog.ToString();
            });

        private void AppendIdeas(string text) =>
            Dispatcher.UIThread.Post(() =>
            {
                if (_ideasTextBox != null)
                    _ideasTextBox.Text = _ideas.ToString();
            });

        private void AppendResults(string text) =>
            Dispatcher.UIThread.Post(() =>
            {
                if (_resultsTextBox != null)
                    _resultsTextBox.Text = _results.ToString();
            });

        private sealed class ProfileListItem
        {
            public BalanceTuningProfile Profile { get; }
            public ProfileListItem(BalanceTuningProfile profile) => Profile = profile;
            public override string ToString() => $"{Profile.Name} ({Profile.Id})";
        }

        private sealed class RunModeItem
        {
            public BalanceTuningRunMode Mode { get; }
            public string Label { get; }
            public RunModeItem(BalanceTuningRunMode mode, string label)
            {
                Mode = mode;
                Label = label;
            }
            public override string ToString() => Label;
        }
    }
}
