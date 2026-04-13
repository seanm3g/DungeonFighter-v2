using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using RPGGame;

namespace RPGGame.Data
{
    /// <summary>Outcome of pushing action body rows (below the header block).</summary>
    public readonly record struct ActionSheetsPushOutcome(
        int DataRowCount,
        int FirstDataRowOneBased,
        int? ApiUpdatedRows,
        int? ApiUpdatedCells);

    /// <summary>
    /// Pushes local <c>Actions.json</c> (<see cref="SpreadsheetActionJson"/>) to an editable Google Sheet using OAuth 2.0 (Desktop).
    /// </summary>
    public static class ActionSheetsPushService
    {
        /// <summary>
        /// Overwrites all data rows below the header block; preserves header rows. Requires Sheets API and OAuth sign-in (browser on first run).
        /// </summary>
        public static async Task PushActionsToGoogleSheetsAsync(
            string? actionsJsonPath = null,
            string? pushConfigPath = null,
            CancellationToken cancellationToken = default)
        {
            pushConfigPath = SheetsPushUtilities.EnsurePushConfigExistsOrThrow(pushConfigPath);
            actionsJsonPath ??= GameConstants.TryGetExistingGameDataFilePath("Actions.json")
                ?? GameConstants.GetGameDataFilePath("Actions.json");

            var cfg = SheetsPushUtilities.LoadPushConfigWithSheetsIdSync(pushConfigPath);
            cfg.Validate();

            if (!File.Exists(actionsJsonPath))
                throw new FileNotFoundException("Actions.json not found.", actionsJsonPath);

            var actions = JsonLoader.LoadJson<List<SpreadsheetActionJson>>(
                actionsJsonPath,
                useCache: false,
                fallbackValue: new List<SpreadsheetActionJson>());
            if (actions == null || actions.Count == 0)
                throw new InvalidOperationException("No actions to push (Actions.json empty or invalid).");

            var service = await SheetsPushUtilities.CreateAuthorizedSheetsServiceAsync(cfg, pushConfigPath, cancellationToken)
                .ConfigureAwait(false);

            await SheetsPushPreflight.EnsureSpreadsheetAccessAndTabsAsync(
                    service,
                    cfg.SpreadsheetId,
                    new[] { cfg.ActionsSheetTabName.Trim() },
                    cancellationToken)
                .ConfigureAwait(false);

            await PushActionsWithServiceAsync(service, cfg, pushConfigPath, actionsJsonPath, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Pushes actions using an already-authorized <see cref="SheetsService"/> (e.g. multi-tab orchestration).
        /// </summary>
        public static async Task<ActionSheetsPushOutcome> PushActionsWithServiceAsync(
            SheetsService service,
            SheetsPushConfig cfg,
            string pushConfigPath,
            string? actionsJsonPath = null,
            CancellationToken cancellationToken = default)
        {
            actionsJsonPath ??= GameConstants.TryGetExistingGameDataFilePath("Actions.json")
                ?? GameConstants.GetGameDataFilePath("Actions.json");

            if (!File.Exists(actionsJsonPath))
                throw new FileNotFoundException("Actions.json not found.", actionsJsonPath);

            var actions = JsonLoader.LoadJson<List<SpreadsheetActionJson>>(
                actionsJsonPath,
                useCache: false,
                fallbackValue: new List<SpreadsheetActionJson>());
            if (actions == null || actions.Count == 0)
                throw new InvalidOperationException("No actions to push (Actions.json empty or invalid).");

            string sheet = SheetsPushUtilities.EscapeSheetName(cfg.ActionsSheetTabName);

            SpreadsheetHeader? header;
            int firstDataRowOneBased;

            // Prefer the same CSV source as pull (SheetsConfig.actionsSheetUrl) so column labels/order match Update-from-Sheets / Resync.
            string sheetsConfigPath = GameConstants.TryGetExistingGameDataFilePath("SheetsConfig.json")
                ?? GameConstants.GetGameDataFilePath("SheetsConfig.json");
            var sheetsCfg = SheetsConfig.Load(sheetsConfigPath);
            if (!string.IsNullOrWhiteSpace(sheetsCfg.ActionsSheetUrl))
            {
                try
                {
                    var csvResult = await SpreadsheetActionParser.ParseCsvAsync(sheetsCfg.ActionsSheetUrl)
                        .ConfigureAwait(false);
                    if (csvResult.Header != null)
                    {
                        header = csvResult.Header;
                        firstDataRowOneBased = header.DataStartRowIndex + 1;
                    }
                    else
                    {
                        (header, firstDataRowOneBased) = await FetchHeaderFromSheetApiAsync(
                            service, cfg, sheet, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Sheets push: could not use CSV header from SheetsConfig ({ex.Message}); using live sheet preview.");
                    (header, firstDataRowOneBased) = await FetchHeaderFromSheetApiAsync(
                        service, cfg, sheet, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                (header, firstDataRowOneBased) = await FetchHeaderFromSheetApiAsync(
                    service, cfg, sheet, cancellationToken).ConfigureAwait(false);
            }

            if (header == null)
                throw new InvalidOperationException("Could not parse header rows. Set actionsSheetUrl in GameData/SheetsConfig.json to match the published CSV used for pull, or check actionsSheetTabName and header rows on the tab.");

            var bodyRows = new List<IList<object>>();
            foreach (var json in actions)
            {
                var data = json.ToSpreadsheetActionData();
                if (!data.IsValid())
                    continue;
                var cells = SpreadsheetActionDataSheetRowSerializer.ToRow(data, header);
                bodyRows.Add(cells.Select(c => (object)c).ToList());
            }

            if (bodyRows.Count == 0)
                throw new InvalidOperationException("No valid action rows to push (every row failed IsValid).");

            string clearRange = $"{sheet}!A{firstDataRowOneBased}:ZZ5000";
            await service.Spreadsheets.Values
                .Clear(new ClearValuesRequest(), cfg.SpreadsheetId, clearRange)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            string updateRange = $"{sheet}!A{firstDataRowOneBased}";
            var valueRange = new ValueRange
            {
                MajorDimension = "ROWS",
                Values = bodyRows
            };

            var updateRequest = service.Spreadsheets.Values.Update(valueRange, cfg.SpreadsheetId, updateRange);
            // RAW keeps values as literal strings (matches CSV export / pull), avoiding Sheets re-interpreting numbers or dates.
            updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            var updateResponse = await updateRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            int? updated = updateResponse?.UpdatedRows;
            int? updatedCells = updateResponse?.UpdatedCells;
            Console.WriteLine(
                $"Sheets push: wrote {bodyRows.Count} action row(s) to tab {cfg.ActionsSheetTabName} starting at row {firstDataRowOneBased} " +
                $"(API reports UpdatedRows={updated}, UpdatedCells={updatedCells}).");

            return new ActionSheetsPushOutcome(bodyRows.Count, firstDataRowOneBased, updated, updatedCells);
        }

        private static async Task<(SpreadsheetHeader? Header, int FirstDataRowOneBased)> FetchHeaderFromSheetApiAsync(
            SheetsService service,
            SheetsPushConfig cfg,
            string escapedSheetName,
            CancellationToken cancellationToken)
        {
            int previewRows = Math.Max(5, cfg.PreviewRowCount);
            string previewRange = $"{escapedSheetName}!A1:ZZ{previewRows}";

            var previewResponse = await service.Spreadsheets.Values
                .Get(cfg.SpreadsheetId, previewRange)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            var rowStrings = SheetsPushUtilities.NormalizeToStringRows(previewResponse.Values);
            return SpreadsheetActionParser.BuildHeaderFromSheetRows(rowStrings);
        }
    }
}
