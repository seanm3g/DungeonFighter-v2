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
    /// <summary>Pushes Actions plus optional WEAPONS / MODIFICATIONS / ARMOR / ENEMIES / ENVIRONMENTS / CLASSES tabs via Sheets API.</summary>
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
                        "Set default push tab names in SheetsPushConfig.json: WEAPONS, MODIFICATIONS, ARMOR, ENEMIES, ENVIRONMENTS, CLASSES " +
                        "(all six optional tabs were blank). Edit the file if your sheet uses different tab titles.");
                }
                catch (Exception ex)
                {
                    result.AddLine($"Note: could not save default tab names to config ({ex.Message}); push still uses them for this run.");
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

            var actionOutcome = await ActionSheetsPushService.PushActionsWithServiceAsync(service, cfg, pushConfigPath, null, cancellationToken)
                .ConfigureAwait(false);
            result.AddLine(
                $"Actions tab '{cfg.ActionsSheetTabName.Trim()}': {actionOutcome.DataRowCount} data row(s) written starting at row {actionOutcome.FirstDataRowOneBased} " +
                $"(API UpdatedRows={actionOutcome.ApiUpdatedRows}, UpdatedCells={actionOutcome.ApiUpdatedCells}).");

            await PushOptionalJsonArrayTabAsync(
                    service,
                    cfg,
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
                    cfg.ModificationsSheetTabName,
                    GameConstants.ModificationsJson,
                    GameDataTabularSheetKind.Modifications,
                    "Modifications.json",
                    result,
                    cancellationToken)
                .ConfigureAwait(false);

            await PushOptionalJsonArrayTabAsync(
                    service,
                    cfg,
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
                    cfg.EnvironmentsSheetTabName,
                    GameConstants.RoomsJson,
                    GameDataTabularSheetKind.Environments,
                    "Rooms.json",
                    result,
                    cancellationToken)
                .ConfigureAwait(false);

            await PushOptionalClassPresentationTabAsync(service, cfg, result, cancellationToken).ConfigureAwait(false);

            return result;
        }

        private static IEnumerable<string> CollectRequiredTabNamesForPush(SheetsPushConfig cfg)
        {
            yield return cfg.ActionsSheetTabName.Trim();

            if (!string.IsNullOrWhiteSpace(cfg.WeaponsSheetTabName))
                yield return cfg.WeaponsSheetTabName.Trim();

            if (!string.IsNullOrWhiteSpace(cfg.ModificationsSheetTabName))
                yield return cfg.ModificationsSheetTabName.Trim();

            if (!string.IsNullOrWhiteSpace(cfg.ArmorSheetTabName))
                yield return cfg.ArmorSheetTabName.Trim();

            if (!string.IsNullOrWhiteSpace(cfg.EnemiesSheetTabName))
                yield return cfg.EnemiesSheetTabName.Trim();

            if (!string.IsNullOrWhiteSpace(cfg.EnvironmentsSheetTabName))
                yield return cfg.EnvironmentsSheetTabName.Trim();

            if (!string.IsNullOrWhiteSpace(cfg.ClassPresentationSheetTabName))
                yield return cfg.ClassPresentationSheetTabName.Trim();
        }

        private static async Task PushOptionalJsonArrayTabAsync(
            SheetsService service,
            SheetsPushConfig cfg,
            string? sheetTabName,
            string jsonFileName,
            GameDataTabularSheetKind kind,
            string displayFileName,
            GameDataSheetsPushResult result,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sheetTabName))
                return;

            string tab = sheetTabName.Trim();
            string? path = GameConstants.TryGetExistingGameDataFilePath(jsonFileName);
            bool hadFile = path != null && File.Exists(path);
            string jsonText = hadFile
                ? await File.ReadAllTextAsync(path!, cancellationToken).ConfigureAwait(false)
                : "[]";

            var rows = JsonArraySheetConverter.BuildPushValueRows(jsonText, kind);
            int dataRows = Math.Max(0, rows.Count - 1);
            await PushRowsAtA1Async(service, cfg.SpreadsheetId, tab, rows, cancellationToken).ConfigureAwait(false);

            string fileNote = hadFile ? "" : $" {displayFileName} was not found — pushed header row only (empty []).";
            result.AddLine($"Tab '{tab}': {dataRows} data row(s), {rows[0].Count} column(s).{fileNote}");
        }

        private static async Task PushOptionalClassPresentationTabAsync(
            SheetsService service,
            SheetsPushConfig cfg,
            GameDataSheetsPushResult result,
            CancellationToken cancellationToken)
        {
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
            await PushRowsAtA1Async(service, cfg.SpreadsheetId, tab, rows, cancellationToken).ConfigureAwait(false);

            string note = File.Exists(tuningPath) ? "" : " TuningConfig.json was not found — pushed default empty classPresentation.";
            result.AddLine($"Tab '{tab}' (class presentation): 1 payload row + header.{note}");
        }

        private static async Task PushRowsAtA1Async(
            SheetsService service,
            string spreadsheetId,
            string tabName,
            List<IList<object>> rows,
            CancellationToken cancellationToken)
        {
            string sheet = SheetsPushUtilities.EscapeSheetName(tabName);
            string clearRange = $"{sheet}!A1:ZZ5000";
            await service.Spreadsheets.Values
                .Clear(new ClearValuesRequest(), spreadsheetId, clearRange)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            string updateRange = $"{sheet}!A1";
            var valueRange = new ValueRange
            {
                MajorDimension = "ROWS",
                Values = rows
            };

            var updateRequest = service.Spreadsheets.Values.Update(valueRange, spreadsheetId, updateRange);
            updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            await updateRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}

