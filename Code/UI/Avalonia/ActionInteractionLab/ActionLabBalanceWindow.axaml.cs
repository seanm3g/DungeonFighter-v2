using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using RPGGame.ActionInteractionLab;
using RPGGame.Tuning.LabBalance;
using RPGGame.UI.Avalonia.Helpers;
using RPGGame.UI.Avalonia.Settings;
using RPGGame.UI.Avalonia.Tuning;

namespace RPGGame.UI.Avalonia.ActionInteractionLab
{
    public partial class ActionLabBalanceWindow : Window
    {
        private static ActionLabBalanceWindow? _instance;

        private readonly ActionLabBalanceViewModel _vm = new();
        private bool _suppressLayerChange;

        public ActionLabBalanceWindow()
        {
            InitializeComponent();
            DataContext = _vm;

            var apply = this.FindControl<Button>("ApplyScenarioButton");
            var reapply = this.FindControl<Button>("ReapplyButton");
            var run = this.FindControl<Button>("RunStatsButton");
            var suggest = this.FindControl<Button>("SuggestButton");
            var applySug = this.FindControl<Button>("ApplySuggestionButton");
            var save = this.FindControl<Button>("SaveKnobsButton");
            var auto = this.FindControl<Button>("AutoLoopButton");
            var workbench = this.FindControl<Button>("OpenWorkbenchButton");
            var combatTuning = this.FindControl<Button>("OpenCombatTuningButton");
            var resetTargets = this.FindControl<Button>("ResetTargetsButton");
            var layerCombo = this.FindControl<ComboBox>("LayerComboBox");

            if (apply != null) apply.Click += async (_, _) => await ApplyScenarioAsync();
            if (reapply != null) reapply.Click += async (_, _) => await ApplyScenarioAsync();
            if (run != null) run.Click += async (_, _) => await RunStatsAsync();
            if (suggest != null) suggest.Click += (_, _) => Suggest();
            if (applySug != null) applySug.Click += (_, _) => ApplySuggestion();
            if (save != null) save.Click += (_, _) => _vm.SaveKnobs();
            if (auto != null) auto.Click += async (_, _) => await AutoLoopAsync();
            if (workbench != null) workbench.Click += (_, _) => OpenWorkbench();
            if (combatTuning != null) combatTuning.Click += (_, _) => OpenCombatTuning();
            if (resetTargets != null) resetTargets.Click += (_, _) => _vm.ResetTargetsToDefaults();

            var maxUpDown = this.FindControl<NumericUpDown>("AutoIterationsUpDown");
            if (maxUpDown != null)
            {
                maxUpDown.Value = _vm.AutoIterations;
                maxUpDown.ValueChanged += (_, e) =>
                {
                    if (e.NewValue.HasValue)
                        _vm.AutoIterations = (int)e.NewValue.Value;
                };
            }

            if (layerCombo != null)
            {
                // TwoWay SelectedItem can set SelectedLayer before SelectionChanged runs; always
                // re-apply so scenario text + knob list match the dropdown (do not skip when equal).
                layerCombo.SelectionChanged += async (_, _) =>
                {
                    if (_suppressLayerChange)
                        return;
                    if (layerCombo.SelectedItem is not LabBalanceLayerDefinition def)
                        return;
                    _vm.SelectedLayer = def.Layer;
                    await ApplyScenarioAsync().ConfigureAwait(true);
                };
            }

            Closed += (_, _) =>
            {
                if (ReferenceEquals(_instance, this))
                    _instance = null;
            };
        }

        public static ActionLabBalanceWindow? CurrentInstance => _instance;

        public static void Open(Window? owner)
        {
            if (_instance != null)
            {
                _instance.Activate();
                return;
            }

            var w = new ActionLabBalanceWindow();
            _instance = w;
            var effectiveOwner = WindowOwnerResolver.ResolveUsableOwnerWindow(owner);
            if (effectiveOwner != null)
            {
                w.WindowStartupLocation = WindowStartupLocation.Manual;
                EventHandler? layoutOnce = null;
                layoutOnce = (_, _) =>
                {
                    w.Opened -= layoutOnce!;
                    Dispatcher.UIThread.Post(() =>
                    {
                        try
                        {
                            ActionLabWindowPlacement.PlaceBalanceBesideCatalog(
                                effectiveOwner,
                                ActionLabCatalogWindow.CurrentInstance,
                                w);
                        }
                        catch
                        {
                            /* best-effort */
                        }
                    }, DispatcherPriority.Loaded);
                };
                w.Opened += layoutOnce;
                w.Show(effectiveOwner);
            }
            else
            {
                w.Show();
            }

            Dispatcher.UIThread.Post(async () =>
            {
                if (ActionInteractionLabSession.Current != null)
                    await w.ApplyScenarioAsync().ConfigureAwait(true);
                else
                    w._vm.ReloadKnobs();
            }, DispatcherPriority.Background);
        }

        public static void CloseIfOpen()
        {
            if (_instance == null)
                return;
            var w = _instance;
            _instance = null;
            try { w.Close(); }
            catch { /* ignore */ }
        }

        public static void Toggle(Window? owner)
        {
            if (_instance != null)
            {
                CloseIfOpen();
                return;
            }

            Open(owner);
        }

        private async System.Threading.Tasks.Task ApplyScenarioAsync()
        {
            var session = ActionInteractionLabSession.Current;
            if (session == null)
            {
                _vm.StatusText = "Action Lab session is not active";
                return;
            }

            _suppressLayerChange = true;
            try
            {
                _vm.ApplyScenario(session);
                var layerCombo = this.FindControl<ComboBox>("LayerComboBox");
                if (layerCombo != null)
                    layerCombo.SelectedItem = _vm.SelectedLayerDefinition;
                _vm.SuggestionText = "No suggestion yet.";
                ActionLabControlsWindow.RefreshIfOpen();
                ActionLabCatalogWindow.RefreshIfOpen();
            }
            finally
            {
                _suppressLayerChange = false;
            }

            await System.Threading.Tasks.Task.CompletedTask;
        }

        private async System.Threading.Tasks.Task RunStatsAsync()
        {
            var session = ActionInteractionLabSession.Current;
            if (session == null)
            {
                _vm.StatusText = "Action Lab session is not active";
                return;
            }

            await _vm.RunStatsAsync(session).ConfigureAwait(true);
            ActionLabControlsWindow.RefreshIfOpen();
        }

        private void Suggest()
        {
            var session = ActionInteractionLabSession.Current;
            if (session == null)
            {
                _vm.StatusText = "Action Lab session is not active";
                return;
            }

            _vm.Suggest(session);
        }

        private void ApplySuggestion()
        {
            _vm.ApplySuggestion();
            ActionLabControlsWindow.RefreshIfOpen();
        }

        private async System.Threading.Tasks.Task AutoLoopAsync()
        {
            var session = ActionInteractionLabSession.Current;
            if (session == null)
            {
                _vm.StatusText = "Action Lab session is not active";
                return;
            }

            var maxUpDown = this.FindControl<NumericUpDown>("AutoIterationsUpDown");
            if (maxUpDown?.Value is { } maxVal)
                _vm.AutoIterations = (int)maxVal;

            await _vm.RunAutoAsync(session).ConfigureAwait(true);
            ActionLabControlsWindow.RefreshIfOpen();
        }

        private void OpenWorkbench()
        {
            string? profile = _vm.SelectedLayerDefinition.WorkbenchProfileId;
            BalanceTuningWorkbenchWindow.Open(this, profile);
        }

        private void OpenCombatTuning()
        {
            CombatTuningNavigation.RequestOpenProgressionCurveInSettings();
        }
    }
}
