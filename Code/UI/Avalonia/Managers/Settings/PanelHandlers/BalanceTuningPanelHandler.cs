using System;
using System.IO;
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
    /// Wires Import (Balance Tuning) panel: Google Sheets URLs, PULL from Sheets, and Push.
    /// </summary>
    public class BalanceTuningPanelHandler : ISettingsPanelHandler
    {
        private readonly CanvasUICoordinator? canvasUI;
        private readonly global::System.Action? onSheetsPullCompleted;
        private TextBoxTestRunner? sheetsRunner;

        public string PanelType => "BalanceTuning";

        /// <param name="onSheetsPullCompleted">Invoked on the UI thread after a successful PULL so lazy-loaded tabs (Enemies, Items, etc.) reload from disk.</param>
        public BalanceTuningPanelHandler(CanvasUICoordinator? canvasUI, global::System.Action? onSheetsPullCompleted = null)
        {
            this.canvasUI = canvasUI;
            this.onSheetsPullCompleted = onSheetsPullCompleted;
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
            SetText(balancePanel, "StatBonusesTabGidTextBox",
                GoogleSheetsUrlHelper.TryGetDerivedTabGidForDisplay(cfg.ActionsSheetUrl, cfg.StatBonusesSheetUrl, out string sg) ? sg : "");
            SetText(balancePanel, "EnemiesTabGidTextBox",
                GoogleSheetsUrlHelper.TryGetDerivedTabGidForDisplay(cfg.ActionsSheetUrl, cfg.EnemiesSheetUrl, out string eg) ? eg : "");
            SetText(balancePanel, "EnvironmentsTabGidTextBox",
                GoogleSheetsUrlHelper.TryGetDerivedTabGidForDisplay(cfg.ActionsSheetUrl, cfg.EnvironmentsSheetUrl, out string vg) ? vg : "");
            SetText(balancePanel, "ClassPresentationTabGidTextBox",
                GoogleSheetsUrlHelper.TryGetDerivedTabGidForDisplay(cfg.ActionsSheetUrl, cfg.ClassPresentationSheetUrl, out string cg) ? cg : "");

            LoadPushTabCheckboxes(balancePanel);
        }

        public void SaveSettings(UserControl panel)
        {
            if (panel is not BalanceTuningSettingsPanel balancePanel)
                return;
            if (!TrySaveSheetsUrlsFromPanel(balancePanel, logErrors: true))
                ScrollDebugLogger.Log("BalanceTuningPanel: Sheets URLs were not saved due to validation.");
            TrySavePushTabSelectionsFromPanel(balancePanel);
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
            string sGid = TrimBox(balancePanel.FindControl<TextBox>("StatBonusesTabGidTextBox"));
            string eGid = TrimBox(balancePanel.FindControl<TextBox>("EnemiesTabGidTextBox"));
            string vGid = TrimBox(balancePanel.FindControl<TextBox>("EnvironmentsTabGidTextBox"));
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

            foreach (string g in new[] { wGid, mGid, aGid, sGid, eGid, vGid, cGid })
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

            bool anyGid = wGid.Length > 0 || mGid.Length > 0 || aGid.Length > 0 || sGid.Length > 0 || eGid.Length > 0 || vGid.Length > 0 || cGid.Length > 0;
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
            MergeTabUrlFromGid(actions, sGid, cfg, (c, v) => { c.StatBonusesSheetUrl = v; }, c => c.StatBonusesSheetUrl);
            MergeTabUrlFromGid(actions, eGid, cfg, (c, v) => { c.EnemiesSheetUrl = v; }, c => c.EnemiesSheetUrl);
            MergeTabUrlFromGid(actions, vGid, cfg, (c, v) => { c.EnvironmentsSheetUrl = v; }, c => c.EnvironmentsSheetUrl);
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
                sheetsRunner.AppendOutput("=== PULL from Google Sheets ===\n");
                if (!TrySaveSheetsUrlsFromPanel(panel, logErrors: false))
                {
                    sheetsRunner.AppendOutput("✗ Invalid URL or tab gid (see tooltips / documentation).\n\n");
                    return;
                }

                TrySavePushTabSelectionsFromPanel(panel);

                var cfg = SheetsConfig.Load();
                if (string.IsNullOrWhiteSpace(cfg.ActionsSheetUrl)
                    && string.IsNullOrWhiteSpace(cfg.WeaponsSheetUrl)
                    && string.IsNullOrWhiteSpace(cfg.ModificationsSheetUrl)
                    && string.IsNullOrWhiteSpace(cfg.ArmorSheetUrl)
                    && string.IsNullOrWhiteSpace(cfg.StatBonusesSheetUrl)
                    && string.IsNullOrWhiteSpace(cfg.EnemiesSheetUrl)
                    && string.IsNullOrWhiteSpace(cfg.EnvironmentsSheetUrl)
                    && string.IsNullOrWhiteSpace(cfg.DungeonsSheetUrl)
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
                    statusTextBlock.Text = "Pulling…";

                await GameDataSheetsPullService.PullAllFromSheetsConfigAsync().ConfigureAwait(true);

                sheetsRunner.AppendOutput("✓ Pull steps completed (loot, enemies, rooms reloaded for this session).\n");
                if (!string.IsNullOrWhiteSpace(cfg.ActionsSheetUrl))
                {
                    // Must clear JsonLoader cache for Actions.json; LoadActions() alone would keep stale data after a pull.
                    ActionLoader.ReloadActions();
                    int actionCount = ActionLoader.GetAllActions().Count;
                    sheetsRunner.AppendOutput($"✓ Actions reloaded ({actionCount} loaded).\n");
                }

                if (onSheetsPullCompleted != null)
                    Dispatcher.UIThread.Post(onSheetsPullCompleted, DispatcherPriority.Normal);

                sheetsRunner.AppendOutput("=== Pull complete ===\n\n");
                if (statusTextBlock != null)
                    statusTextBlock.Text = "Pull complete";
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
            TrySavePushTabSelectionsFromPanel(panel);

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

        private static void LoadPushTabCheckboxes(BalanceTuningSettingsPanel panel)
        {
            string? path = GameConstants.TryGetExistingGameDataFilePath("SheetsPushConfig.json");
            SheetsPushConfig pushCfg;
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                pushCfg = SheetsPushConfig.Load(path);
            else
                pushCfg = new SheetsPushConfig();

            SetPushCheckbox(panel, "PushActionsTabCheckBox", pushCfg.PushActionsTab);
            SetPushCheckbox(panel, "PushWeaponsTabCheckBox", pushCfg.PushWeaponsTab);
            SetPushCheckbox(panel, "PushModificationsTabCheckBox", pushCfg.PushModificationsTab);
            SetPushCheckbox(panel, "PushArmorTabCheckBox", pushCfg.PushArmorTab);
            SetPushCheckbox(panel, "PushStatBonusesTabCheckBox", pushCfg.PushStatBonusesTab);
            SetPushCheckbox(panel, "PushEnemiesTabCheckBox", pushCfg.PushEnemiesTab);
            SetPushCheckbox(panel, "PushEnvironmentsTabCheckBox", pushCfg.PushEnvironmentsTab);
            SetPushCheckbox(panel, "PushDungeonsTabCheckBox", pushCfg.PushDungeonsTab);
            SetPushCheckbox(panel, "PushClassPresentationTabCheckBox", pushCfg.PushClassPresentationTab);
        }

        private static void TrySavePushTabSelectionsFromPanel(BalanceTuningSettingsPanel panel)
        {
            string? path = GameConstants.TryGetExistingGameDataFilePath("SheetsPushConfig.json");
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return;

            var pushCfg = SheetsPushConfig.Load(path);
            pushCfg.PushActionsTab = ReadPushCheckbox(panel, "PushActionsTabCheckBox", defaultIfNull: true);
            pushCfg.PushWeaponsTab = ReadPushCheckbox(panel, "PushWeaponsTabCheckBox", defaultIfNull: true);
            pushCfg.PushModificationsTab = ReadPushCheckbox(panel, "PushModificationsTabCheckBox", defaultIfNull: true);
            pushCfg.PushArmorTab = ReadPushCheckbox(panel, "PushArmorTabCheckBox", defaultIfNull: true);
            pushCfg.PushStatBonusesTab = ReadPushCheckbox(panel, "PushStatBonusesTabCheckBox", defaultIfNull: true);
            pushCfg.PushEnemiesTab = ReadPushCheckbox(panel, "PushEnemiesTabCheckBox", defaultIfNull: true);
            pushCfg.PushEnvironmentsTab = ReadPushCheckbox(panel, "PushEnvironmentsTabCheckBox", defaultIfNull: true);
            pushCfg.PushDungeonsTab = ReadPushCheckbox(panel, "PushDungeonsTabCheckBox", defaultIfNull: true);
            pushCfg.PushClassPresentationTab = ReadPushCheckbox(panel, "PushClassPresentationTabCheckBox", defaultIfNull: true);
            pushCfg.Save(path);
        }

        private static void SetPushCheckbox(BalanceTuningSettingsPanel panel, string checkBoxName, bool isChecked)
        {
            var box = panel.FindControl<CheckBox>(checkBoxName);
            if (box != null)
                box.IsChecked = isChecked;
        }

        private static bool ReadPushCheckbox(BalanceTuningSettingsPanel panel, string checkBoxName, bool defaultIfNull)
        {
            var box = panel.FindControl<CheckBox>(checkBoxName);
            return box?.IsChecked ?? defaultIfNull;
        }
    }
}
