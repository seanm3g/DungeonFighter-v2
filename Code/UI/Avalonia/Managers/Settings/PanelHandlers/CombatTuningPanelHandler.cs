using System;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia.Controls;
using RPGGame.Tuning;
using RPGGame.UI.Avalonia.Settings;
using RPGGame.UI.Avalonia.Settings.ViewModels;

namespace RPGGame.UI.Avalonia.Managers.Settings.PanelHandlers
{
    /// <summary>
    /// Loads/saves layered combat tuning parameters and runs in-panel balance simulations.
    /// </summary>
    public sealed class CombatTuningPanelHandler : ISettingsPanelHandler
    {
        private readonly Action<string, bool>? showStatusMessage;
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

            if (p.ViewModel == null)
                p.ViewModel = CombatTuningPanelViewModel.FromRegistry();

            WireSimulation(p);
        }

        public void LoadSettings(UserControl panel)
        {
            if (panel is not CombatTuningSettingsPanel p)
                return;

            p.ViewModel?.ReloadFromConfig();
            CombatTuningNavigation.ApplyToPanel(p);
        }

        public void SaveSettings(UserControl panel)
        {
            if (panel is not CombatTuningSettingsPanel p)
                return;

            try
            {
                p.ViewModel?.CommitAllToConfig();
                CombatTuningParameterRegistry.EnsureSanitizedDefaults();
                p.ViewModel?.ReloadFromConfig();
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Combat tuning: {ex.Message}", false);
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

            panel.ViewModel?.CommitAllToConfig();
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
