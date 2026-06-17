using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using RPGGame.Tuning;
using RPGGame.UI.Avalonia.Components;
using RPGGame.UI.Avalonia.Settings;

namespace RPGGame.UI.Avalonia.Managers.Settings.PanelHandlers
{
    /// <summary>
    /// Loads/saves layered combat tuning parameters and runs in-panel balance simulations.
    /// </summary>
    public sealed class CombatTuningPanelHandler : ISettingsPanelHandler
    {
        private readonly Action<string, bool>? showStatusMessage;
        private readonly Dictionary<string, SliderWithTextBox> controlsByParameterId = new(StringComparer.OrdinalIgnoreCase);
        private bool isSimulationRunning;
        private const int SimulationProgressMinIntervalMs = 200;

        public CombatTuningPanelHandler(Action<string, bool>? showStatusMessage = null)
        {
            this.showStatusMessage = showStatusMessage;
        }

        public string PanelType => "CombatTuning";

        public void WireUp(UserControl panel)
        {
            if (panel is not CombatTuningSettingsPanel p)
                return;

            BuildLayerControls(p);
            WireSimulation(p);
            // Sliders are created in code; defer value sync until controls are loaded and text boxes exist.
            Dispatcher.UIThread.Post(() =>
            {
                LoadSettings(p);
                foreach (var control in controlsByParameterId.Values)
                    control.SyncDisplayFromValue();
            }, DispatcherPriority.Loaded);
        }

        public void LoadSettings(UserControl panel)
        {
            if (panel is not CombatTuningSettingsPanel)
                return;

            foreach (var param in CombatTuningParameterRegistry.All)
            {
                if (!controlsByParameterId.TryGetValue(param.Id, out var control))
                    continue;
                double value = Math.Clamp(param.GetValue(), control.Minimum, control.Maximum);
                control.Value = value;
                control.SyncDisplayFromValue();
            }
        }

        public void SaveSettings(UserControl panel)
        {
            if (panel is not CombatTuningSettingsPanel p)
                return;

            try
            {
                PushAllFromPanelToConfig(p);
                CombatTuningParameterRegistry.EnsureSanitizedDefaults();
                LoadSettings(p);
                showStatusMessage?.Invoke("Combat tuning updated (save settings to write balance patch).", true);
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Combat tuning: {ex.Message}", false);
            }
        }

        private void BuildLayerControls(CombatTuningSettingsPanel panel)
        {
            controlsByParameterId.Clear();
            var layerPanels = new Dictionary<CombatTuningLayer, StackPanel?>
            {
                [CombatTuningLayer.Duration] = panel.DurationLayerPanel ?? panel.FindControl<StackPanel>("DurationLayerPanel"),
                [CombatTuningLayer.WinRate] = panel.WinRateLayerPanel ?? panel.FindControl<StackPanel>("WinRateLayerPanel"),
                [CombatTuningLayer.RollFeel] = panel.RollFeelLayerPanel ?? panel.FindControl<StackPanel>("RollFeelLayerPanel"),
                [CombatTuningLayer.ComboAffordance] = panel.ComboLayerPanel ?? panel.FindControl<StackPanel>("ComboLayerPanel"),
                [CombatTuningLayer.Goals] = panel.GoalsLayerPanel ?? panel.FindControl<StackPanel>("GoalsLayerPanel"),
            };

            foreach (var layer in layerPanels.Keys)
            {
                if (layerPanels[layer] is not StackPanel host)
                    continue;
                host.Children.Clear();
                host.Margin = new Thickness(0, 8, 0, 0);

                foreach (var param in CombatTuningParameterRegistry.GetByLayer(layer))
                {
                    var block = new StackPanel { Spacing = 4, Margin = new Thickness(0, 0, 0, 8) };
                    block.Children.Add(new TextBlock
                    {
                        Text = param.Label,
                        Classes = { "settings-field-label" },
                        FontWeight = FontWeight.SemiBold
                    });
                    block.Children.Add(new TextBlock
                    {
                        Text = $"Affects: {param.Affects}",
                        Classes = { "settings-muted" },
                        TextWrapping = TextWrapping.Wrap
                    });

                    var slider = new SliderWithTextBox
                    {
                        ShowLabel = false,
                        Minimum = param.Minimum,
                        Maximum = param.Maximum,
                        TickFrequency = param.TickFrequency,
                        HorizontalAlignment = HorizontalAlignment.Stretch
                    };

                    slider.Loaded += (_, _) =>
                    {
                        double value = Math.Clamp(param.GetValue(), slider.Minimum, slider.Maximum);
                        slider.Value = value;
                        slider.SyncDisplayFromValue();
                    };

                    void push() => PushParameterFromControl(param, slider);
                    if (slider.Slider != null)
                        slider.Slider.ValueChanged += (_, _) => push();
                    if (slider.TextBox != null)
                        slider.TextBox.LostFocus += (_, _) => push();

                    block.Children.Add(slider);
                    host.Children.Add(block);
                    controlsByParameterId[param.Id] = slider;
                }
            }
        }

        private static void PushParameterFromControl(CombatTuningParameter param, SliderWithTextBox control)
        {
            double v = Math.Clamp(control.Value, control.Minimum, control.Maximum);
            param.SetValue(v);
        }

        private void PushAllFromPanelToConfig(CombatTuningSettingsPanel panel)
        {
            foreach (var param in CombatTuningParameterRegistry.All)
            {
                if (!controlsByParameterId.TryGetValue(param.Id, out var control))
                    continue;
                PushParameterFromControl(param, control);
            }
        }

        private void WireSimulation(CombatTuningSettingsPanel panel)
        {
            var runBtn = panel.RunSimulationButton ?? panel.FindControl<Button>("RunSimulationButton");
            if (runBtn == null)
                return;
            runBtn.Click += async (_, _) => await RunSimulationAsync(panel);
        }

        private async Task RunSimulationAsync(CombatTuningSettingsPanel panel)
        {
            if (isSimulationRunning)
                return;

            PushAllFromPanelToConfig(panel);
            CombatTuningParameterRegistry.EnsureSanitizedDefaults();

            var runBtn = panel.RunSimulationButton ?? panel.FindControl<Button>("RunSimulationButton");
            var status = panel.SimStatusTextBlock ?? panel.FindControl<TextBlock>("SimStatusTextBlock");
            var progress = panel.SimProgressBar ?? panel.FindControl<ProgressBar>("SimProgressBar");
            var output = panel.SimOutputTextBox ?? panel.FindControl<TextBox>("SimOutputTextBox");

            int battles = ParseInt(panel.SimBattlesPerComboTextBox?.Text, 50, 1, 500);
            int playerLevel = ParseInt(panel.SimPlayerLevelTextBox?.Text, 1, 1, 50);
            int enemyLevel = ParseInt(panel.SimEnemyLevelTextBox?.Text, 1, 1, 50);

            isSimulationRunning = true;
            if (runBtn != null) runBtn.IsEnabled = false;
            if (progress != null) { progress.IsVisible = true; progress.Value = 0; }
            if (status != null) status.Text = "Running…";
            if (output != null) output.Text = "Running simulation…";

            try
            {
                var request = new CombatTuningSimulatorService.SimulationRequest
                {
                    BattlesPerCombination = battles,
                    PlayerLevel = playerLevel,
                    EnemyLevel = enemyLevel
                };

                int lastReportedCompleted = -1;
                long lastProgressUiTick = 0;
                var uiProgress = new Progress<(int completed, int total, string message)>(p =>
                {
                    // Progress<T> already marshals to the UI thread; avoid flooding the dispatcher.
                    long now = System.Environment.TickCount64;
                    bool finished = p.total > 0 && p.completed >= p.total;
                    if (!finished
                        && p.completed == lastReportedCompleted
                        && (now - lastProgressUiTick) < SimulationProgressMinIntervalMs)
                        return;
                    if (!finished && (now - lastProgressUiTick) < SimulationProgressMinIntervalMs)
                        return;

                    lastReportedCompleted = p.completed;
                    lastProgressUiTick = now;

                    if (progress != null && p.total > 0)
                        progress.Value = 100.0 * p.completed / p.total;
                    if (status != null)
                        status.Text = p.total > 0
                            ? $"{p.message} ({p.completed}/{p.total})"
                            : p.message;
                    if (output != null && (finished || p.completed == 0 || p.completed % 20 == 0))
                        output.Text = $"Running simulation… {p.completed}/{p.total}\r\n{p.message}";
                });

                var summary = await CombatTuningSimulatorService.RunAsync(request, uiProgress).ConfigureAwait(true);

                if (output != null)
                    output.Text = summary.FormattedReport;
                if (status != null)
                    status.Text = $"Done — quality {summary.QualityScore:F1}";
            }
            catch (Exception ex)
            {
                if (output != null)
                    output.Text = $"Simulation failed: {ex.Message}";
                if (status != null)
                    status.Text = "Failed";
                showStatusMessage?.Invoke($"Simulation error: {ex.Message}", false);
            }
            finally
            {
                isSimulationRunning = false;
                if (runBtn != null) runBtn.IsEnabled = true;
                if (progress != null) progress.IsVisible = false;
            }
        }

        private static int ParseInt(string? text, int fallback, int min, int max)
        {
            if (!int.TryParse(text?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
                v = fallback;
            return Math.Clamp(v, min, max);
        }
    }
}
