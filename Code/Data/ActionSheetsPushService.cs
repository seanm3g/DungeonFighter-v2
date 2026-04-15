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
                bodyRows.Add(cells.Select(c => SheetsPushUtilities.NormalizeCellValueForUpload(c)).ToList());
            }

            if (bodyRows.Count == 0)
                throw new InvalidOperationException("No valid action rows to push (every row failed IsValid).");

            // Columns E–F are reserved for on-sheet formulas; do not clear or overwrite them (see SheetsPushUtilities).
            int lastDataRowOneBased = firstDataRowOneBased + bodyRows.Count - 1;
            string clearLeft = $"{sheet}!A{firstDataRowOneBased}:D5000";
            string clearRight = $"{sheet}!G{firstDataRowOneBased}:ZZ5000";
            await service.Spreadsheets.Values
                .BatchClear(
                    new BatchClearValuesRequest { Ranges = new List<string> { clearLeft, clearRight } },
                    cfg.SpreadsheetId)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            var leftRows = new List<IList<object>>(bodyRows.Count);
            var rightRows = new List<IList<object>>(bodyRows.Count);
            int maxWidth = 0;
            foreach (var row in bodyRows)
            {
                var (ad, gPlus) = SheetsPushUtilities.SplitActionPushRowPreservingColumnsEF(row);
                leftRows.Add(ad);
                rightRows.Add(gPlus);
                maxWidth = Math.Max(maxWidth, row.Count);
            }

            string leftEndLetter = SheetsPushUtilities.ColumnIndexToA1Letters(Math.Min(3, Math.Max(0, maxWidth - 1)));
            string leftRange = $"{sheet}!A{firstDataRowOneBased}:{leftEndLetter}{lastDataRowOneBased}";

            var batchData = new List<ValueRange>
            {
                new ValueRange
                {
                    Range = leftRange,
                    MajorDimension = "ROWS",
                    Values = leftRows
                }
            };

            if (maxWidth > SheetsPushUtilities.ActionsSheetPreservedFormulaLastZeroBased + 1)
            {
                int rightEndIndex = maxWidth - 1;
                string rightEndLetter = SheetsPushUtilities.ColumnIndexToA1Letters(rightEndIndex);
                string rightStartLetter = SheetsPushUtilities.ColumnIndexToA1Letters(SheetsPushUtilities.ActionsSheetPushDataResumeColumnZeroBased);
                string rightRange = $"{sheet}!{rightStartLetter}{firstDataRowOneBased}:{rightEndLetter}{lastDataRowOneBased}";
                batchData.Add(new ValueRange
                {
                    Range = rightRange,
                    MajorDimension = "ROWS",
                    Values = rightRows
                });
            }

            SheetsPushUtilities.NormalizeValueRangeGridsForUpload(batchData);

            var batchUpdate = new BatchUpdateValuesRequest
            {
                ValueInputOption = "RAW",
                Data = batchData
            };
            var batchResponse = await service.Spreadsheets.Values
                .BatchUpdate(batchUpdate, cfg.SpreadsheetId)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
            int? updated = batchResponse?.TotalUpdatedRows;
            int? updatedCells = batchResponse?.TotalUpdatedCells;
            Console.WriteLine(
                $"Sheets push: wrote {bodyRows.Count} action row(s) to tab {cfg.ActionsSheetTabName} starting at row {firstDataRowOneBased} " +
                $"(columns E–F left unchanged for sheet formulas; API reports TotalUpdatedRows={updated}, TotalUpdatedCells={updatedCells}).");

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
