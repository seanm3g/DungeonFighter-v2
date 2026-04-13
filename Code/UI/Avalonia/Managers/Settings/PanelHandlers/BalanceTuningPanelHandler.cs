using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using RPGGame;
using RPGGame.Data;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Settings;
using RPGGame.Utils;

namespace RPGGame.UI.Avalonia.Managers.Settings.PanelHandlers
{
    /// <summary>
    /// Wires Import (Balance Tuning) panel: Google Sheets URLs, Resync, and Push.
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

            var resync = balancePanel.FindControl<Button>("ResyncSheetsButton");
            if (resync != null)
                resync.Click += async (_, _) => await ResyncFromGoogleSheetsAsync(balancePanel);

            var push = balancePanel.FindControl<Button>("PushSheetsButton");
            if (push != null)
                push.Click += async (_, _) => await PushGameDataToGoogleSheetsAsync(balancePanel);

            Dispatcher.UIThread.Post(() => LoadSettings(balancePanel), DispatcherPriority.Loaded);
        }

        public void LoadSettings(UserControl panel)
        {
            if (panel is not BalanceTuningSettingsPanel balancePanel)
                return;
            var cfg = SheetsConfig.Load();
            SetText(balancePanel, "SpreadsheetEditUrlTextBox", cfg.SpreadsheetEditUrl);
            SetText(balancePanel, "ActionsSheetUrlTextBox", cfg.ActionsSheetUrl);

            SetText(balancePanel, "WeaponsTabGidTextBox",
                GoogleSheetsUrlHelper.TryGetDerivedTabGidForDisplay(cfg.ActionsSheetUrl, cfg.WeaponsSheetUrl, out string wg) ? wg : "");
            SetText(balancePanel, "ModificationsTabGidTextBox",
                GoogleSheetsUrlHelper.TryGetDerivedTabGidForDisplay(cfg.ActionsSheetUrl, cfg.ModificationsSheetUrl, out string mg) ? mg : "");
            SetText(balancePanel, "ArmorTabGidTextBox",
                GoogleSheetsUrlHelper.TryGetDerivedTabGidForDisplay(cfg.ActionsSheetUrl, cfg.ArmorSheetUrl, out string ag) ? ag : "");
            SetText(balancePanel, "ClassPresentationTabGidTextBox",
                GoogleSheetsUrlHelper.TryGetDerivedTabGidForDisplay(cfg.ActionsSheetUrl, cfg.ClassPresentationSheetUrl, out string cg) ? cg : "");
        }

        public void SaveSettings(UserControl panel)
        {
            if (panel is not BalanceTuningSettingsPanel balancePanel)
                return;
            if (!TrySaveSheetsUrlsFromPanel(balancePanel, logErrors: true))
                ScrollDebugLogger.Log("BalanceTuningPanel: Sheets URLs were not saved due to validation.");
        }

        private static void SetText(BalanceTuningSettingsPanel panel, string textBoxName, string value)
        {
            var box = panel.FindControl<TextBox>(textBoxName);
            if (box != null)
                box.Text = value ?? "";
        }

        private static bool IsHttpUrl(string u) =>
            u.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || u.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        private static bool TrySaveSheetsUrlsFromPanel(BalanceTuningSettingsPanel balancePanel, bool logErrors)
        {
            static string TrimBox(TextBox? b) => (b?.Text ?? "").Trim();

            string edit = TrimBox(balancePanel.FindControl<TextBox>("SpreadsheetEditUrlTextBox"));
            string actions = TrimBox(balancePanel.FindControl<TextBox>("ActionsSheetUrlTextBox"));
            string wGid = TrimBox(balancePanel.FindControl<TextBox>("WeaponsTabGidTextBox"));
            string mGid = TrimBox(balancePanel.FindControl<TextBox>("ModificationsTabGidTextBox"));
            string aGid = TrimBox(balancePanel.FindControl<TextBox>("ArmorTabGidTextBox"));
            string cGid = TrimBox(balancePanel.FindControl<TextBox>("ClassPresentationTabGidTextBox"));

            foreach (string u in new[] { edit, actions })
            {
                if (string.IsNullOrEmpty(u))
                    continue;
                if (!IsHttpUrl(u))
                {
                    if (logErrors)
                        ScrollDebugLogger.Log("BalanceTuningPanel: Spreadsheet / Actions URL must start with http:// or https://.");
                    return false;
                }
            }

            foreach (string g in new[] { wGid, mGid, aGid, cGid })
            {
                if (string.IsNullOrEmpty(g))
                    continue;
                if (!Regex.IsMatch(g, @"^\d+$"))
                {
                    if (logErrors)
                        ScrollDebugLogger.Log("BalanceTuningPanel: Tab gids must be numeric (from Publish to web) or empty.");
                    return false;
                }
            }

            bool anyGid = wGid.Length > 0 || mGid.Length > 0 || aGid.Length > 0 || cGid.Length > 0;
            if (anyGid && !IsHttpUrl(actions))
            {
                if (logErrors)
                    ScrollDebugLogger.Log("BalanceTuningPanel: Set the Actions CSV URL when using tab gids (same spreadsheet).");
                return false;
            }

            var cfg = SheetsConfig.Load();
            cfg.SpreadsheetEditUrl = edit;
            cfg.ActionsSheetUrl = actions;

            MergeTabUrlFromGid(actions, wGid, cfg, (c, v) => { c.WeaponsSheetUrl = v; }, c => c.WeaponsSheetUrl);
            MergeTabUrlFromGid(actions, mGid, cfg, (c, v) => { c.ModificationsSheetUrl = v; }, c => c.ModificationsSheetUrl);
            MergeTabUrlFromGid(actions, aGid, cfg, (c, v) => { c.ArmorSheetUrl = v; }, c => c.ArmorSheetUrl);
            MergeTabUrlFromGid(actions, cGid, cfg, (c, v) => { c.ClassPresentationSheetUrl = v; }, c => c.ClassPresentationSheetUrl);

            cfg.Save();
            GoogleSheetsUrlHelper.TrySyncSpreadsheetIdToPushConfigFromSheetsConfig(cfg);
            return true;
        }

        private static void MergeTabUrlFromGid(
            string actionsUrl,
            string gidText,
            SheetsConfig cfg,
            Action<SheetsConfig, string> setUrl,
            Func<SheetsConfig, string> getUrl)
        {
            string gid = gidText.Trim();
            if (Regex.IsMatch(gid, @"^\d+$") && IsHttpUrl(actionsUrl))
            {
                setUrl(cfg, GoogleSheetsUrlHelper.ReplaceGidInPublishedGoogleSheetsUrl(actionsUrl, gid));
                return;
            }

            if (string.IsNullOrEmpty(gid)
                && GoogleSheetsUrlHelper.SamePublishedSheetsUrlIgnoringGid(actionsUrl, getUrl(cfg)))
                setUrl(cfg, "");
        }

        private async Task ResyncFromGoogleSheetsAsync(BalanceTuningSettingsPanel panel)
        {
            if (sheetsRunner == null)
                return;

            try
            {
                sheetsRunner.AppendOutput("=== Resync from Google Sheets ===\n");
                if (!TrySaveSheetsUrlsFromPanel(panel, logErrors: false))
                {
                    sheetsRunner.AppendOutput("✗ Invalid URL or tab gid (see tooltips / documentation).\n\n");
                    return;
                }

                var cfg = SheetsConfig.Load();
                if (string.IsNullOrWhiteSpace(cfg.ActionsSheetUrl)
                    && string.IsNullOrWhiteSpace(cfg.WeaponsSheetUrl)
                    && string.IsNullOrWhiteSpace(cfg.ModificationsSheetUrl)
                    && string.IsNullOrWhiteSpace(cfg.ArmorSheetUrl)
                    && string.IsNullOrWhiteSpace(cfg.ClassPresentationSheetUrl))
                {
                    sheetsRunner.AppendOutput("✗ Set the Actions CSV URL (base for all tabs) and optional tab gids, or full URLs in JSON.\n\n");
                    var st = panel.FindControl<TextBlock>("SheetsSyncProgressTextBlock");
                    if (st != null) st.Text = "URL required";
                    return;
                }

                sheetsRunner.AppendOutput("Fetching from configured CSV URLs…\n");
                var statusTextBlock = panel.FindControl<TextBlock>("SheetsSyncProgressTextBlock");
                if (statusTextBlock != null)
                    statusTextBlock.Text = "Resyncing…";

                await GameDataSheetsPullService.PullAllFromSheetsConfigAsync().ConfigureAwait(true);

                sheetsRunner.AppendOutput("✓ Pull steps completed.\n");
                if (!string.IsNullOrWhiteSpace(cfg.ActionsSheetUrl))
                {
                    ActionLoader.LoadActions();
                    int actionCount = ActionLoader.GetAllActions().Count;
                    sheetsRunner.AppendOutput($"✓ Actions reloaded ({actionCount} loaded).\n");
                }

                sheetsRunner.AppendOutput("=== Resync complete ===\n\n");
                if (statusTextBlock != null)
                    statusTextBlock.Text = "Resync complete";
            }
            catch (Exception ex)
            {
                sheetsRunner.AppendOutput($"✗ Error: {ex.Message}\n{ex.StackTrace}\n\n");
                var statusTextBlock = panel.FindControl<TextBlock>("SheetsSyncProgressTextBlock");
                if (statusTextBlock != null)
                    statusTextBlock.Text = $"Error: {ex.Message}";
            }
        }

        private async Task PushGameDataToGoogleSheetsAsync(BalanceTuningSettingsPanel panel)
        {
            if (sheetsRunner == null)
                return;

            var statusTextBlock = panel.FindControl<TextBlock>("SheetsSyncProgressTextBlock");
            Window? dialogOwner = TopLevel.GetTopLevel(panel) as Window ?? canvasUI?.GetMainWindow();

            bool confirmed = await ConfirmationDialog.ShowAsync(
                dialogOwner,
                "Push game data to Google Sheets",
                "This will replace data on the configured spreadsheet tabs (Actions below the header block; other tabs from row 1). " +
                "Google Sheets has no automatic undo—export or back up the sheet first if you are unsure.\n\nContinue?");
            if (!confirmed)
            {
                sheetsRunner.AppendOutput("Push cancelled.\n\n");
                if (statusTextBlock != null)
                    statusTextBlock.Text = "Push cancelled";
                return;
            }

            TrySaveSheetsUrlsFromPanel(panel, logErrors: false);

            var game = canvasUI?.GetGame();
            var settingsWindow = TopLevel.GetTopLevel(panel) as SettingsWindow;

            try
            {
                if (game != null)
                    game.SuppressEscapeClosingSettings = true;
                if (settingsWindow != null)
                    settingsWindow.SuppressEscapeClose = true;

                sheetsRunner.AppendOutput("=== Push game data to Google Sheets ===\n");
                sheetsRunner.AppendOutput("Using GameData/SheetsPushConfig.json (OAuth). Browser may open on first push…\n");
                if (statusTextBlock != null)
                    statusTextBlock.Text = "Pushing…";

                var pushResult = await GameDataSheetsPushService.PushAllGameDataSheetsAsync().ConfigureAwait(true);

                foreach (string line in pushResult.SummaryLines)
                    sheetsRunner.AppendOutput(line + "\n");
                sheetsRunner.AppendOutput("✓ Push completed successfully.\n");
                sheetsRunner.AppendOutput("=== Push complete ===\n\n");
                if (statusTextBlock != null)
                    statusTextBlock.Text = "Push complete";
            }
            catch (Exception ex)
            {
                sheetsRunner.AppendOutput($"✗ Push failed: {ex.Message}\n{ex.StackTrace}\n\n");
                if (ex.Message.Contains("NotFound", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("404", StringComparison.Ordinal))
                {
                    sheetsRunner.AppendOutput(
                        "Hint: OAuth push needs the real spreadsheet id from an **Edit** link (…/d/<id>/edit…), not the publish-only e/2PACX id. " +
                        "Paste that into **Spreadsheet edit link** in this panel and save, or set spreadsheetId in SheetsPushConfig.json.\n\n");
                }
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
    }
}
