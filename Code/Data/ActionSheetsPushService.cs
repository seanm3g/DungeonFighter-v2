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

            header = await RemoveLegacyCadenceColumnsIfPresentAsync(
                    service, cfg, header, cancellationToken)
                .ConfigureAwait(false);

            var ensuredTriggers = ActionTriggerSheetColumns.EnsureHeader(header);
            header = ensuredTriggers.Header;
            bool headerExpanded = ensuredTriggers.ColumnsAdded;

            var ensuredCadences = ActionCadenceSheetColumns.EnsureHeader(header);
            header = ensuredCadences.Header;
            headerExpanded = headerExpanded || ensuredCadences.ColumnsAdded;

            var ensuredReserve = ActionReservePoolSheetColumns.EnsureHeader(header);
            header = ensuredReserve.Header;
            headerExpanded = headerExpanded || ensuredReserve.ColumnsAdded;

            if (headerExpanded)
            {
                await WriteHeaderRowsAsync(service, cfg, sheet, header, cancellationToken).ConfigureAwait(false);
                Console.WriteLine(
                    $"Sheets push: appended TRIGGERS / CADENCES / RESERVE POOL columns to tab {cfg.ActionsSheetTabName} header.");
            }

            try
            {
                await ActionSheetsAnnotationPush.PushReferenceTabsAndHeaderNotesAsync(
                        service, cfg.SpreadsheetId, cfg.ActionsSheetTabName.Trim(), header, cancellationToken)
                    .ConfigureAwait(false);
                Console.WriteLine(
                    "Sheets push: refreshed CADENCE_LIST + MECHANIC_LIST (full TURN/ACTION/FIGHT/DUNGEON + mechanic hover notes).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sheets push: reference tabs / header notes skipped ({ex.Message}).");
            }

            var existingSheetRows = await FetchExistingDataRowsAsync(
                    service, cfg, sheet, firstDataRowOneBased, cancellationToken)
                .ConfigureAwait(false);
            int previousDataRowCount = existingSheetRows.Count;

            var merge = ActionSheetsPushRowMerger.BuildBodyRowsPreservingSheetOrder(existingSheetRows, actions, header);
            var bodyRows = merge.BodyRows.ToList();

            if (bodyRows.Count == 0)
                throw new InvalidOperationException("No valid action rows to push (every row failed IsValid).");

            // Column F is reserved for on-sheet formulas (e.g. e(V)); TAGS in column E is pushed with A–D.
            int lastDataRowOneBased = firstDataRowOneBased + bodyRows.Count - 1;
            int trailingClearStart = lastDataRowOneBased + 1;
            int trailingClearEnd = Math.Max(firstDataRowOneBased + previousDataRowCount - 1, trailingClearStart);
            var clearRanges = new List<string>();
            if (trailingClearStart <= trailingClearEnd)
            {
                clearRanges.Add($"{sheet}!A{trailingClearStart}:E{trailingClearEnd}");
                clearRanges.Add($"{sheet}!G{trailingClearStart}:ZZ{trailingClearEnd}");
            }

            if (clearRanges.Count > 0)
            {
                await service.Spreadsheets.Values
                    .BatchClear(
                        new BatchClearValuesRequest { Ranges = clearRanges },
                        cfg.SpreadsheetId)
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

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

            int leftEndIndex = Math.Min(SheetsPushUtilities.ActionsSheetPushLeftBlockLastZeroBased, Math.Max(0, maxWidth - 1));
            string leftEndLetter = SheetsPushUtilities.ColumnIndexToA1Letters(leftEndIndex);
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

            if (maxWidth > SheetsPushUtilities.ActionsSheetPushDataResumeColumnZeroBased)
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
                $"Sheets push: wrote {bodyRows.Count} row(s) to tab {cfg.ActionsSheetTabName} starting at row {firstDataRowOneBased} " +
                $"({merge.UpdatedActionCount} action(s) updated, {merge.PreservedSectionRowCount} section header(s) preserved, {merge.AppendedActionCount} new action(s) appended; " +
                $"column F left unchanged for sheet formulas; TAGS column E included; API reports TotalUpdatedRows={updated}, TotalUpdatedCells={updatedCells}).");

            return new ActionSheetsPushOutcome(bodyRows.Count, firstDataRowOneBased, updated, updatedCells);
        }

        /// <summary>
        /// Deletes legacy cadence columns (old <c>* CADENCE</c> triples + compact DURATION/CADENCE/MECHANICS,
        /// typically sheet columns K–Y) via Sheets API, then returns an in-memory header with those columns removed.
        /// </summary>
        private static async Task<SpreadsheetHeader> RemoveLegacyCadenceColumnsIfPresentAsync(
            SheetsService service,
            SheetsPushConfig cfg,
            SpreadsheetHeader header,
            CancellationToken cancellationToken)
        {
            var legacyIndices = ActionCadenceSheetColumns.CollectLegacyColumnIndicesToRemove(header);
            if (legacyIndices.Count == 0)
                return header;

            var ranges = ActionCadenceSheetColumns.BuildDescendingDeleteRanges(legacyIndices);
            int sheetId = await ResolveActionsSheetIdAsync(service, cfg, cancellationToken).ConfigureAwait(false);

            var requests = new List<Request>(ranges.Count);
            foreach (var (start, end) in ranges)
            {
                requests.Add(new Request
                {
                    DeleteDimension = new DeleteDimensionRequest
                    {
                        Range = new DimensionRange
                        {
                            SheetId = sheetId,
                            Dimension = "COLUMNS",
                            StartIndex = start,
                            EndIndex = end
                        }
                    }
                });
            }

            await service.Spreadsheets
                .BatchUpdate(new BatchUpdateSpreadsheetRequest { Requests = requests }, cfg.SpreadsheetId)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            string letters = string.Join(", ",
                legacyIndices.Select(SheetsPushUtilities.ColumnIndexToA1Letters));
            Console.WriteLine(
                $"Sheets push: deleted {legacyIndices.Count} legacy cadence column(s) on tab {cfg.ActionsSheetTabName} " +
                $"({letters}; old * CADENCE triples + compact DURATION/CADENCE/MECHANICS).");

            return ActionCadenceSheetColumns.RemoveColumns(header, legacyIndices);
        }

        private static async Task<int> ResolveActionsSheetIdAsync(
            SheetsService service,
            SheetsPushConfig cfg,
            CancellationToken cancellationToken)
        {
            string want = cfg.ActionsSheetTabName.Trim();
            var req = service.Spreadsheets.Get(cfg.SpreadsheetId);
            req.Fields = "sheets(properties(sheetId,title))";
            var meta = await req.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            if (meta.Sheets != null)
            {
                foreach (var s in meta.Sheets)
                {
                    if (string.Equals(s.Properties?.Title, want, StringComparison.Ordinal)
                        && s.Properties?.SheetId != null)
                        return s.Properties.SheetId.Value;
                }
            }

            throw new InvalidOperationException(
                $"Could not resolve sheetId for ACTIONS tab '{want}' when deleting legacy cadence columns.");
        }

        private static async Task WriteHeaderRowsAsync(
            SheetsService service,
            SheetsPushConfig cfg,
            string escapedSheetName,
            SpreadsheetHeader header,
            CancellationToken cancellationToken)
        {
            int labelRowOneBased = header.LabelRowIndex + 1;
            int endColIndex = Math.Max(0, header.LabelByIndex.Count - 1);
            string endLetter = SheetsPushUtilities.ColumnIndexToA1Letters(endColIndex);

            var batchData = new List<ValueRange>();

            if (header.LabelRowIndex > 0)
            {
                int contextRowOneBased = header.LabelRowIndex; // 0-based LabelRowIndex=1 → context row 1
                batchData.Add(new ValueRange
                {
                    Range = $"{escapedSheetName}!A{contextRowOneBased}:{endLetter}{contextRowOneBased}",
                    MajorDimension = "ROWS",
                    Values = new List<IList<object>> { ActionTriggerSheetColumns.BuildHeaderContextRow(header).ToList() }
                });
            }

            batchData.Add(new ValueRange
            {
                Range = $"{escapedSheetName}!A{labelRowOneBased}:{endLetter}{labelRowOneBased}",
                MajorDimension = "ROWS",
                Values = new List<IList<object>> { ActionTriggerSheetColumns.BuildHeaderLabelRow(header).ToList() }
            });

            SheetsPushUtilities.NormalizeValueRangeGridsForUpload(batchData);

            await service.Spreadsheets.Values
                .BatchUpdate(
                    new BatchUpdateValuesRequest
                    {
                        ValueInputOption = "RAW",
                        Data = batchData
                    },
                    cfg.SpreadsheetId)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
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

        private static async Task<List<string[]>> FetchExistingDataRowsAsync(
            SheetsService service,
            SheetsPushConfig cfg,
            string escapedSheetName,
            int firstDataRowOneBased,
            CancellationToken cancellationToken)
        {
            string dataRange = $"{escapedSheetName}!A{firstDataRowOneBased}:ZZ5000";
            var response = await service.Spreadsheets.Values
                .Get(cfg.SpreadsheetId, dataRange)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            var rows = SheetsPushUtilities.NormalizeToStringRows(response.Values);
            ActionSheetsPushRowMerger.TrimTrailingEmptyRows(rows);
            return rows;
        }
    }
}
