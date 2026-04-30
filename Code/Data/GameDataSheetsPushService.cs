using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using RPGGame;

namespace RPGGame.Data
{
    /// <summary>Pushes Actions plus optional WEAPONS / Prefix (Modifications.json + PrefixMaterialQuality.json when present) / ARMOR / SUFFIXES (StatBonuses) / ENEMIES / ENVIRONMENTS / DUNGEONS / CLASSES / CLASS ACTIONS tabs via Sheets API.</summary>
    public static class GameDataSheetsPushService
    {
        public static async Task<GameDataSheetsPushResult> PushAllGameDataSheetsAsync(
            string? pushConfigPath = null,
            CancellationToken cancellationToken = default)
        {
            var result = new GameDataSheetsPushResult();
            pushConfigPath = SheetsPushUtilities.EnsurePushConfigExistsOrThrow(pushConfigPath);
            var cfg = SheetsPushUtilities.LoadPushConfigWithSheetsIdSync(pushConfigPath);
            cfg.Validate();

            if (cfg.ApplyDefaultOptionalSheetTabNamesWhenAllUnset())
            {
                try
                {
                    cfg.Save(pushConfigPath);
                    result.AddLine(
                        "Set default push tab names in SheetsPushConfig.json: WEAPONS, Prefix (Modifications.json + PrefixMaterialQuality.json), ARMOR, SUFFIXES, ENEMIES, ENVIRONMENTS, DUNGEONS, CLASSES, CLASS ACTIONS " +
                        "(all optional tabs were blank). Edit the file if your sheet uses different tab titles.");
                }
                catch (Exception ex)
                {
                    result.AddLine($"Note: could not save default tab names to config ({ex.Message}); push still uses them for this run.");
                }
            }

            if (cfg.ApplyDefaultEnemiesAndEnvironmentsTabNamesIfUnset())
            {
                try
                {
                    cfg.Save(pushConfigPath);
                    result.AddLine(
                        "Set default ENEMIES / ENVIRONMENTS tab names in SheetsPushConfig.json (those fields were blank). " +
                        "Edit if your spreadsheet uses different tab titles.");
                }
                catch (Exception ex)
                {
                    result.AddLine(
                        $"Note: could not save SheetsPushConfig after ENEMIES/ENVIRONMENTS defaults ({ex.Message}); push still uses those tab names for this run.");
                }
            }

            if (cfg.ApplyDefaultDungeonsTabNameIfUnset())
            {
                try
                {
                    cfg.Save(pushConfigPath);
                    result.AddLine(
                        "Set default DUNGEONS tab name in SheetsPushConfig.json (that field was blank). " +
                        "Edit if your spreadsheet uses a different tab title.");
                }
                catch (Exception ex)
                {
                    result.AddLine(
                        $"Note: could not save SheetsPushConfig after DUNGEONS default ({ex.Message}); push still uses that tab name for this run.");
                }
            }

            if (cfg.ApplyDefaultStatBonusesTabNameIfUnset())
            {
                try
                {
                    cfg.Save(pushConfigPath);
                    result.AddLine(
                        "Set default SUFFIXES tab name in SheetsPushConfig.json (statBonusesSheetTabName was blank). " +
                        "Rename your sheet tab to match or edit the config if your tab title differs.");
                }
                catch (Exception ex)
                {
                    result.AddLine(
                        $"Note: could not save SheetsPushConfig after SUFFIXES default ({ex.Message}); push still uses that tab name for this run.");
                }
            }

            if (cfg.ApplyDefaultClassActionsTabNameIfUnset())
            {
                try
                {
                    cfg.Save(pushConfigPath);
                    result.AddLine(
                        "Set default CLASS ACTIONS tab name in SheetsPushConfig.json (classActionsSheetTabName was blank). " +
                        "Edit if your spreadsheet uses a different tab title.");
                }
                catch (Exception ex)
                {
                    result.AddLine(
                        $"Note: could not save SheetsPushConfig after CLASS ACTIONS default ({ex.Message}); push still uses that tab name for this run.");
                }
            }

            var service = await SheetsPushUtilities.CreateAuthorizedSheetsServiceAsync(cfg, pushConfigPath, cancellationToken)
                .ConfigureAwait(false);

            await SheetsPushPreflight.EnsureSpreadsheetAccessAndTabsAsync(
                    service,
                    cfg.SpreadsheetId,
                    CollectRequiredTabNamesForPush(cfg),
                    cancellationToken)
                .ConfigureAwait(false);

            if (cfg.PushActionsTab)
            {
                var actionOutcome = await ActionSheetsPushService.PushActionsWithServiceAsync(service, cfg, pushConfigPath, null, cancellationToken)
                    .ConfigureAwait(false);
                result.AddLine(
                    $"Actions tab '{cfg.ActionsSheetTabName.Trim()}': {actionOutcome.DataRowCount} data row(s) written starting at row {actionOutcome.FirstDataRowOneBased} " +
                    $"(API UpdatedRows={actionOutcome.ApiUpdatedRows}, UpdatedCells={actionOutcome.ApiUpdatedCells}).");
            }
            else
                result.AddLine($"Skipped Actions tab '{cfg.ActionsSheetTabName.Trim()}' (push disabled in SheetsPushConfig).");

            await PushOptionalJsonArrayTabAsync(
                    service,
                    cfg,
                    cfg.PushWeaponsTab,
                    "WEAPONS",
                    cfg.WeaponsSheetTabName,
                    GameConstants.WeaponsJson,
                    GameDataTabularSheetKind.Weapons,
                    "Weapons.json",
                    result,
                    cancellationToken)
                .ConfigureAwait(false);

            await PushOptionalJsonArrayTabAsync(
                    service,
                    cfg,
                    cfg.PushModificationsTab,
                    "PREFIX",
                    cfg.ModificationsSheetTabName,
                    GameConstants.ModificationsJson,
                    GameDataTabularSheetKind.Modifications,
                    "Modifications.json",
                    result,
                    cancellationToken,
                    mergeSecondJsonFileName: GameConstants.PrefixMaterialQualityJson,
                    mergeSecondDisplayName: GameConstants.PrefixMaterialQualityJson)
                .ConfigureAwait(false);

            await PushOptionalJsonArrayTabAsync(
                    service,
                    cfg,
                    cfg.PushArmorTab,
                    "ARMOR",
                    cfg.ArmorSheetTabName,
                    GameConstants.ArmorJson,
                    GameDataTabularSheetKind.Armor,
                    "Armor.json",
                    result,
                    cancellationToken)
                .ConfigureAwait(false);

            await PushOptionalJsonArrayTabAsync(
                    service,
                    cfg,
                    cfg.PushStatBonusesTab,
                    "SUFFIXES",
                    cfg.StatBonusesSheetTabName,
                    GameConstants.StatBonusesJson,
                    GameDataTabularSheetKind.StatBonuses,
                    "StatBonuses.json",
                    result,
                    cancellationToken)
                .ConfigureAwait(false);

            await PushOptionalJsonArrayTabAsync(
                    service,
                    cfg,
                    cfg.PushEnemiesTab,
                    "ENEMIES",
                    cfg.EnemiesSheetTabName,
                    GameConstants.EnemiesJson,
                    GameDataTabularSheetKind.Enemies,
                    "Enemies.json",
                    result,
                    cancellationToken)
                .ConfigureAwait(false);

            await PushOptionalJsonArrayTabAsync(
                    service,
                    cfg,
                    cfg.PushEnvironmentsTab,
                    "ENVIRONMENTS",
                    cfg.EnvironmentsSheetTabName,
                    GameConstants.RoomsJson,
                    GameDataTabularSheetKind.Environments,
                    "Rooms.json",
                    result,
                    cancellationToken)
                .ConfigureAwait(false);

            await PushOptionalJsonArrayTabAsync(
                    service,
                    cfg,
                    cfg.PushDungeonsTab,
                    "DUNGEONS",
                    cfg.DungeonsSheetTabName,
                    GameConstants.DungeonsJson,
                    GameDataTabularSheetKind.Dungeons,
                    "Dungeons.json",
                    result,
                    cancellationToken)
                .ConfigureAwait(false);

            await PushOptionalClassPresentationTabAsync(service, cfg, result, cancellationToken).ConfigureAwait(false);

            await PushOptionalClassActionsTabAsync(service, cfg, result, cancellationToken).ConfigureAwait(false);

            return result;
        }

        private static IEnumerable<string> CollectRequiredTabNamesForPush(SheetsPushConfig cfg)
        {
            if (cfg.PushActionsTab)
                yield return cfg.ActionsSheetTabName.Trim();

            if (cfg.PushWeaponsTab && !string.IsNullOrWhiteSpace(cfg.WeaponsSheetTabName))
                yield return cfg.WeaponsSheetTabName.Trim();

            if (cfg.PushModificationsTab && !string.IsNullOrWhiteSpace(cfg.ModificationsSheetTabName))
                yield return cfg.ModificationsSheetTabName.Trim();

            if (cfg.PushArmorTab && !string.IsNullOrWhiteSpace(cfg.ArmorSheetTabName))
                yield return cfg.ArmorSheetTabName.Trim();

            if (cfg.PushStatBonusesTab && !string.IsNullOrWhiteSpace(cfg.StatBonusesSheetTabName))
                yield return cfg.StatBonusesSheetTabName.Trim();

            if (cfg.PushEnemiesTab && !string.IsNullOrWhiteSpace(cfg.EnemiesSheetTabName))
                yield return cfg.EnemiesSheetTabName.Trim();

            if (cfg.PushEnvironmentsTab && !string.IsNullOrWhiteSpace(cfg.EnvironmentsSheetTabName))
                yield return cfg.EnvironmentsSheetTabName.Trim();

            if (cfg.PushDungeonsTab && !string.IsNullOrWhiteSpace(cfg.DungeonsSheetTabName))
                yield return cfg.DungeonsSheetTabName.Trim();

            if (cfg.PushClassPresentationTab && !string.IsNullOrWhiteSpace(cfg.ClassPresentationSheetTabName))
                yield return cfg.ClassPresentationSheetTabName.Trim();

            if (cfg.PushClassActionsTab && !string.IsNullOrWhiteSpace(cfg.ClassActionsSheetTabName))
                yield return cfg.ClassActionsSheetTabName.Trim();
        }

        private static async Task PushOptionalJsonArrayTabAsync(
            SheetsService service,
            SheetsPushConfig cfg,
            bool pushThisTab,
            string pushKindLabel,
            string? sheetTabName,
            string jsonFileName,
            GameDataTabularSheetKind kind,
            string displayFileName,
            GameDataSheetsPushResult result,
            CancellationToken cancellationToken,
            string? mergeSecondJsonFileName = null,
            string? mergeSecondDisplayName = null)
        {
            if (!pushThisTab)
            {
                if (!string.IsNullOrWhiteSpace(sheetTabName))
                    result.AddLine($"Skipped tab '{sheetTabName.Trim()}' ({pushKindLabel}) — push disabled in SheetsPushConfig.");
                return;
            }

            if (string.IsNullOrWhiteSpace(sheetTabName))
                return;

            string tab = sheetTabName.Trim();
            string? path = GameConstants.TryGetExistingGameDataFilePath(jsonFileName);
            bool hadPrimaryFile = path != null && File.Exists(path);
            string jsonText = hadPrimaryFile
                ? await File.ReadAllTextAsync(path!, cancellationToken).ConfigureAwait(false)
                : "[]";

            bool mergedSecond = false;
            if (!string.IsNullOrWhiteSpace(mergeSecondJsonFileName))
            {
                string? mergePath = GameConstants.TryGetExistingGameDataFilePath(mergeSecondJsonFileName);
                if (mergePath != null && File.Exists(mergePath))
                {
                    string extra = await File.ReadAllTextAsync(mergePath, cancellationToken).ConfigureAwait(false);
                    jsonText = JsonArraySheetConverter.MergeJsonRootArrays(jsonText, extra);
                    mergedSecond = true;
                }
            }

            var rows = JsonArraySheetConverter.BuildPushValueRows(jsonText, kind);
            int headerRows = JsonArraySheetConverter.GetTabularSheetHeaderRowCount(kind);
            int dataRows = Math.Max(0, rows.Count - headerRows);
            await PushRowsAtA1Async(
                    service,
                    cfg.SpreadsheetId,
                    tab,
                    rows,
                    JsonArraySheetConverter.GetTabularSheetHeaderRowCount(kind),
                    cancellationToken)
                .ConfigureAwait(false);

            string fileNote;
            if (!hadPrimaryFile)
            {
                if (mergedSecond && dataRows > 0)
                    fileNote = $" {displayFileName} was not found — sheet data built from {mergeSecondDisplayName ?? mergeSecondJsonFileName} only.";
                else
                    fileNote = $" {displayFileName} was not found — pushed header row only (empty []).";
            }
            else
                fileNote = "";

            string mergeNote = mergedSecond && !string.IsNullOrWhiteSpace(mergeSecondDisplayName)
                ? $" Merged {mergeSecondDisplayName}."
                : "";
            result.AddLine($"Tab '{tab}': {dataRows} data row(s), {rows[0].Count} column(s), {headerRows} header row(s).{mergeNote}{fileNote}");
        }

        private static async Task PushOptionalClassPresentationTabAsync(
            SheetsService service,
            SheetsPushConfig cfg,
            GameDataSheetsPushResult result,
            CancellationToken cancellationToken)
        {
            if (!cfg.PushClassPresentationTab)
            {
                if (!string.IsNullOrWhiteSpace(cfg.ClassPresentationSheetTabName))
                    result.AddLine($"Skipped tab '{cfg.ClassPresentationSheetTabName.Trim()}' (CLASSES) — push disabled in SheetsPushConfig.");
                return;
            }

            if (string.IsNullOrWhiteSpace(cfg.ClassPresentationSheetTabName))
                return;

            string tab = cfg.ClassPresentationSheetTabName.Trim();
            string tuningPath = GameConfiguration.GetTuningConfigFilePathForWrite();
            ClassPresentationConfig pres;
            if (File.Exists(tuningPath))
            {
                string tuningText = await File.ReadAllTextAsync(tuningPath, cancellationToken).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(tuningText);
                if (doc.RootElement.TryGetProperty("classPresentation", out var cpEl))
                {
                    pres = JsonSerializer.Deserialize<ClassPresentationConfig>(cpEl.GetRawText(), new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new ClassPresentationConfig();
                }
                else
                    pres = new ClassPresentationConfig();
            }
            else
                pres = new ClassPresentationConfig();

            var rows = ClassPresentationSheetConverter.BuildPushValueRows(pres);
            await PushRowsAtA1Async(service, cfg.SpreadsheetId, tab, rows, headerRowCount: 1, cancellationToken).ConfigureAwait(false);

            string note = File.Exists(tuningPath) ? "" : " TuningConfig.json was not found — pushed default empty classPresentation.";
            result.AddLine($"Tab '{tab}' (class presentation): 1 payload row + header.{note}");
        }

        private static async Task PushOptionalClassActionsTabAsync(
            SheetsService service,
            SheetsPushConfig cfg,
            GameDataSheetsPushResult result,
            CancellationToken cancellationToken)
        {
            if (!cfg.PushClassActionsTab)
            {
                if (!string.IsNullOrWhiteSpace(cfg.ClassActionsSheetTabName))
                    result.AddLine($"Skipped tab '{cfg.ClassActionsSheetTabName.Trim()}' (CLASS ACTIONS) — push disabled in SheetsPushConfig.");
                return;
            }

            if (string.IsNullOrWhiteSpace(cfg.ClassActionsSheetTabName))
                return;

            string tab = cfg.ClassActionsSheetTabName.Trim();
            ClassActionsUnlockConfig unlock = ClassActionsUnlockConfig.TryLoadFromGameDataFile()
                ?? ClassActionsUnlockConfig.CreateBuiltInDefaults();
            var rows = ClassActionsSheetConverter.BuildPushValueRows(unlock);
            await PushRowsAtA1Async(service, cfg.SpreadsheetId, tab, rows, headerRowCount: 1, cancellationToken).ConfigureAwait(false);
            int dataRows = Math.Max(0, rows.Count - 1);
            result.AddLine($"Tab '{tab}' (class actions): {dataRows} data row(s) + header (from ClassActions.json or built-in defaults).");
        }

        private static async Task PushRowsAtA1Async(
            SheetsService service,
            string spreadsheetId,
            string tabName,
            List<IList<object>> rows,
            int headerRowCount,
            CancellationToken cancellationToken)
        {
            string sheet = SheetsPushUtilities.EscapeSheetName(tabName);
            string clearRange = $"{sheet}!A1:ZZ5000";
            await service.Spreadsheets.Values
                .Clear(new ClearValuesRequest(), spreadsheetId, clearRange)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            SheetsPushUtilities.NormalizeRowsInPlaceForUpload(rows, firstRowIndexToNormalize: headerRowCount);

            string updateRange = $"{sheet}!A1";
            var valueRange = new ValueRange
            {
                MajorDimension = "ROWS",
                Values = rows
            };
            SheetsPushUtilities.NormalizeValueRangeGridsForUpload(new[] { valueRange });

            var updateRequest = service.Spreadsheets.Values.Update(valueRange, spreadsheetId, updateRange);
            updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            await updateRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}

