using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using RPGGame;
using RPGGame.Data;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Settings;
using RPGGame.Utils;

namespace RPGGame.UI.Avalonia.Managers.Settings.PanelHandlers
{
    /// <summary>
    /// Wires Import (Balance Tuning) panel: Google Sheets URL, Resync, and Push.
    /// </summary>
    public class BalanceTuningPanelHandler : ISettingsPanelHandler
    {
        private readonly CanvasUICoordinator? canvasUI;
        private TextBoxTestRunner? sheetsRunner;

        public string PanelType => "BalanceTuning";

        public BalanceTuningPanelHandler(CanvasUICoordinator? canvasUI)
        {
            this.canvasUI = canvasUI;
        }

        public void WireUp(UserControl panel)
        {
            if (panel is not BalanceTuningSettingsPanel balancePanel)
                return;

            var sheetsOutput = balancePanel.FindControl<TextBox>("SheetsSyncOutputTextBox");
            var sheetsProgress = balancePanel.FindControl<TextBlock>("SheetsSyncProgressTextBlock");
            sheetsRunner = new TextBoxTestRunner(sheetsOutput, sheetsProgress, null);

            var resync = balancePanel.FindControl<Button>("ResyncActionsButton");
            if (resync != null)
                resync.Click += async (_, _) => await ResyncActionsFromGoogleSheetsAsync(balancePanel);

            var push = balancePanel.FindControl<Button>("PushActionsToGoogleSheetsButton");
            if (push != null)
                push.Click += async (_, _) => await PushActionsToGoogleSheetsAsync(balancePanel);

            Dispatcher.UIThread.Post(() => LoadSettings(balancePanel), DispatcherPriority.Loaded);
        }

        public void LoadSettings(UserControl panel)
        {
            if (panel is not BalanceTuningSettingsPanel balancePanel)
                return;
            var urlBox = balancePanel.FindControl<TextBox>("ActionsSheetUrlTextBox");
            if (urlBox == null)
                return;
            var cfg = SheetsConfig.Load();
            urlBox.Text = cfg.ActionsSheetUrl ?? "";
        }

        public void SaveSettings(UserControl panel)
        {
            if (panel is not BalanceTuningSettingsPanel balancePanel)
                return;
            var urlBox = balancePanel.FindControl<TextBox>("ActionsSheetUrlTextBox");
            if (urlBox == null)
                return;

            string trimmed = (urlBox.Text ?? "").Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                var emptyCfg = SheetsConfig.Load();
                emptyCfg.ActionsSheetUrl = "";
                emptyCfg.Save();
                return;
            }

            if (!trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                ScrollDebugLogger.Log("BalanceTuningPanel: skipped saving actions sheet URL (must start with http:// or https://).");
                return;
            }

            var cfg = SheetsConfig.Load();
            cfg.ActionsSheetUrl = trimmed;
            cfg.Save();
            GoogleSheetsUrlHelper.TrySyncSpreadsheetIdToPushConfig(trimmed);
        }

        private async Task ResyncActionsFromGoogleSheetsAsync(BalanceTuningSettingsPanel panel)
        {
            if (sheetsRunner == null)
                return;

            try
            {
                sheetsRunner.AppendOutput("=== Resyncing Actions from Google Sheets ===\n");
                if (!TryApplyActionsSheetUrlFromPanel(panel, out string sheetUrl))
                    return;

                sheetsRunner.AppendOutput("Fetching latest actions from Google Sheets...\n");

                var statusTextBlock = panel.FindControl<TextBlock>("SheetsSyncProgressTextBlock");
                if (statusTextBlock != null)
                    statusTextBlock.Text = "Resyncing actions...";

                await ActionUpdateService.UpdateFromGoogleSheetsAsync(sheetUrl);

                sheetsRunner.AppendOutput("✓ Successfully updated Actions.json from Google Sheets\n");
                sheetsRunner.AppendOutput("Reloading actions...\n");

                ActionLoader.LoadActions();
                var actionCount = ActionLoader.GetAllActions().Count;

                sheetsRunner.AppendOutput($"✓ Actions reloaded successfully ({actionCount} actions loaded)\n");
                sheetsRunner.AppendOutput("=== Resync Complete ===\n\n");

                if (statusTextBlock != null)
                    statusTextBlock.Text = $"Resync complete - {actionCount} actions loaded";
            }
            catch (Exception ex)
            {
                sheetsRunner.AppendOutput($"✗ Error resyncing actions: {ex.Message}\n");
                sheetsRunner.AppendOutput($"{ex.StackTrace}\n\n");

                var statusTextBlock = panel.FindControl<TextBlock>("SheetsSyncProgressTextBlock");
                if (statusTextBlock != null)
                    statusTextBlock.Text = $"Error: {ex.Message}";
            }
        }

        private async Task PushActionsToGoogleSheetsAsync(BalanceTuningSettingsPanel panel)
        {
            if (sheetsRunner == null)
                return;

            var statusTextBlock = panel.FindControl<TextBlock>("SheetsSyncProgressTextBlock");
            Window? dialogOwner = TopLevel.GetTopLevel(panel) as Window ?? canvasUI?.GetMainWindow();

            bool confirmed = await ConfirmationDialog.ShowAsync(
                dialogOwner,
                "Push Actions to Google Sheets",
                "This will clear all data rows below the header on the configured tab and replace them with the current Actions.json. " +
                "Google Sheets has no automatic undo—export or back up the sheet first if you are unsure.\n\nContinue?");
            if (!confirmed)
            {
                sheetsRunner.AppendOutput("Push cancelled.\n\n");
                if (statusTextBlock != null)
                    statusTextBlock.Text = "Push cancelled";
                return;
            }

            if (!TryApplyActionsSheetUrlFromPanel(panel, out _))
                return;

            var game = canvasUI?.GetGame();
            var settingsWindow = TopLevel.GetTopLevel(panel) as SettingsWindow;

            try
            {
                if (game != null)
                    game.SuppressEscapeClosingSettings = true;
                if (settingsWindow != null)
                    settingsWindow.SuppressEscapeClose = true;

                sheetsRunner.AppendOutput("=== Pushing Actions to Google Sheets ===\n");
                sheetsRunner.AppendOutput("Using GameData/SheetsPushConfig.json (OAuth). Browser may open on first push…\n");
                if (statusTextBlock != null)
                    statusTextBlock.Text = "Pushing actions…";

                await ActionSheetsPushService.PushActionsToGoogleSheetsAsync();

                sheetsRunner.AppendOutput("✓ Push completed successfully.\n");
                sheetsRunner.AppendOutput("=== Push Complete ===\n\n");
                if (statusTextBlock != null)
                    statusTextBlock.Text = "Push complete";
            }
            catch (Exception ex)
            {
                sheetsRunner.AppendOutput($"✗ Push failed: {ex.Message}\n");
                sheetsRunner.AppendOutput($"{ex.StackTrace}\n\n");
                if (statusTextBlock != null)
                    statusTextBlock.Text = $"Error: {ex.Message}";
            }
            finally
            {
                if (game != null)
                    game.SuppressEscapeClosingSettings = false;
                if (settingsWindow != null)
                    settingsWindow.SuppressEscapeClose = false;
            }
        }

        private bool TryApplyActionsSheetUrlFromPanel(BalanceTuningSettingsPanel panel, out string sheetUrl)
        {
            sheetUrl = "";
            if (sheetsRunner == null)
                return false;

            var urlBox = panel.FindControl<TextBox>("ActionsSheetUrlTextBox");
            string trimmed = (urlBox?.Text ?? "").Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                sheetsRunner.AppendOutput("✗ Actions sheet URL is empty. Enter the published or export CSV URL above.\n\n");
                var st = panel.FindControl<TextBlock>("SheetsSyncProgressTextBlock");
                if (st != null) st.Text = "URL required";
                return false;
            }

            if (!trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                sheetsRunner.AppendOutput("✗ Actions sheet URL must start with http:// or https://\n\n");
                var st = panel.FindControl<TextBlock>("SheetsSyncProgressTextBlock");
                if (st != null) st.Text = "Invalid URL";
                return false;
            }

            var cfg = SheetsConfig.Load();
            cfg.ActionsSheetUrl = trimmed;
            cfg.Save();
            GoogleSheetsUrlHelper.TrySyncSpreadsheetIdToPushConfig(trimmed);
            sheetUrl = trimmed;
            return true;
        }
    }
}
