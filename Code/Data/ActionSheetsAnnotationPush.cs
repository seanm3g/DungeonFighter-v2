using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace RPGGame.Data
{
    /// <summary>
    /// Pushes CADENCE_LIST + MECHANIC_LIST reference tabs and applies ACTIONS header hover notes.
    /// </summary>
    public static class ActionSheetsAnnotationPush
    {
        public static async Task PushReferenceTabsAndHeaderNotesAsync(
            SheetsService service,
            string spreadsheetId,
            string actionsTabName,
            SpreadsheetHeader actionsHeader,
            CancellationToken cancellationToken)
        {
            var meta = await FetchSheetMetaAsync(service, spreadsheetId, cancellationToken).ConfigureAwait(false);

            int cadenceSheetId = await EnsureTabAsync(
                    service, spreadsheetId, meta, CadenceScopeDescriptions.ListTabName, cancellationToken)
                .ConfigureAwait(false);
            int mechanicSheetId = await EnsureTabAsync(
                    service, spreadsheetId, meta, ActionMechanicDescriptions.ListTabName, cancellationToken)
                .ConfigureAwait(false);

            await WriteCadenceListAsync(service, spreadsheetId, CadenceScopeDescriptions.ListTabName, cancellationToken)
                .ConfigureAwait(false);
            await WriteMechanicListAsync(service, spreadsheetId, ActionMechanicDescriptions.ListTabName, cancellationToken)
                .ConfigureAwait(false);

            await ApplyCadenceListNotesAsync(service, spreadsheetId, cadenceSheetId, cancellationToken)
                .ConfigureAwait(false);
            await ApplyMechanicListNotesAsync(service, spreadsheetId, mechanicSheetId, cancellationToken)
                .ConfigureAwait(false);

            if (!meta.TryGetValue(actionsTabName.Trim(), out int actionsSheetId))
            {
                Console.WriteLine($"Sheets push: could not find tab '{actionsTabName}' for header notes.");
                return;
            }

            await ApplyActionsHeaderNotesAsync(service, spreadsheetId, actionsSheetId, actionsHeader, cancellationToken)
                .ConfigureAwait(false);
        }

        private static async Task<Dictionary<string, int>> FetchSheetMetaAsync(
            SheetsService service, string spreadsheetId, CancellationToken cancellationToken)
        {
            var req = service.Spreadsheets.Get(spreadsheetId);
            req.Fields = "sheets(properties(sheetId,title))";
            var meta = await req.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            var map = new Dictionary<string, int>(StringComparer.Ordinal);
            if (meta.Sheets == null)
                return map;
            foreach (var s in meta.Sheets)
            {
                string? title = s.Properties?.Title;
                int? id = s.Properties?.SheetId;
                if (!string.IsNullOrEmpty(title) && id.HasValue)
                    map[title] = id.Value;
            }

            return map;
        }

        private static async Task<int> EnsureTabAsync(
            SheetsService service,
            string spreadsheetId,
            Dictionary<string, int> meta,
            string tabName,
            CancellationToken cancellationToken)
        {
            if (meta.TryGetValue(tabName, out int existing))
                return existing;

            var add = new Request
            {
                AddSheet = new AddSheetRequest
                {
                    Properties = new SheetProperties { Title = tabName }
                }
            };
            var resp = await service.Spreadsheets.BatchUpdate(
                    new BatchUpdateSpreadsheetRequest { Requests = new List<Request> { add } },
                    spreadsheetId)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            int newId = resp.Replies?[0]?.AddSheet?.Properties?.SheetId
                        ?? throw new InvalidOperationException($"Failed to create tab '{tabName}'.");
            meta[tabName] = newId;
            Console.WriteLine($"Sheets push: created reference tab '{tabName}'.");
            return newId;
        }

        private static async Task WriteCadenceListAsync(
            SheetsService service, string spreadsheetId, string tabName, CancellationToken cancellationToken)
        {
            string sheet = SheetsPushUtilities.EscapeSheetName(tabName);
            var values = new List<IList<object>>
            {
                new List<object> { "CADENCE", "SUMMARY", "DETAIL" }
            };
            foreach (var row in CadenceScopeDescriptions.All)
                values.Add(new List<object> { row.Cadence, row.Summary, row.Detail });

            var cadenceUpdate = service.Spreadsheets.Values.Update(
                new ValueRange { Values = values },
                spreadsheetId,
                $"{sheet}!A1");
            cadenceUpdate.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            await cadenceUpdate.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        }

        private static async Task WriteMechanicListAsync(
            SheetsService service, string spreadsheetId, string tabName, CancellationToken cancellationToken)
        {
            string sheet = SheetsPushUtilities.EscapeSheetName(tabName);
            var values = new List<IList<object>>
            {
                new List<object> { "MECHANIC_ID", "DESCRIPTION", "ALLOWED_CADENCES" }
            };
            foreach (string id in ActionMechanicsRegistry.AllMechanicIds)
            {
                values.Add(new List<object>
                {
                    id,
                    ActionMechanicDescriptions.GetDescription(id),
                    ActionMechanicDescriptions.FormatAllowedCadences(id)
                });
            }

            // Clear then write so removed ids do not linger
            try
            {
                await service.Spreadsheets.Values
                    .Clear(new ClearValuesRequest(), spreadsheetId, $"{sheet}!A:C")
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                // Tab may be empty / new
            }

            var mechUpdate = service.Spreadsheets.Values.Update(
                new ValueRange { Values = values },
                spreadsheetId,
                $"{sheet}!A1");
            mechUpdate.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            await mechUpdate.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        }

        private static async Task ApplyCadenceListNotesAsync(
            SheetsService service, string spreadsheetId, int sheetId, CancellationToken cancellationToken)
        {
            var rowData = new List<RowData>
            {
                new RowData { Values = new List<CellData> { new CellData { Note = "Canonical cadence keyword." } } }
            };
            foreach (var row in CadenceScopeDescriptions.All)
            {
                rowData.Add(new RowData
                {
                    Values = new List<CellData>
                    {
                        new CellData { Note = $"{row.Cadence}: {row.Summary}\n\n{row.Detail}" }
                    }
                });
            }

            await UpdateNotesColumnAsync(service, spreadsheetId, sheetId, startRow: 0, startCol: 0, rowData, cancellationToken)
                .ConfigureAwait(false);
        }

        private static async Task ApplyMechanicListNotesAsync(
            SheetsService service, string spreadsheetId, int sheetId, CancellationToken cancellationToken)
        {
            var rowData = new List<RowData>
            {
                new RowData
                {
                    Values = new List<CellData>
                    {
                        new CellData
                        {
                            Note = "Use these IDs in ACTIONS → MECHANICS and in TRIGGERS → cells. Magnitudes stay in detail columns."
                        }
                    }
                }
            };
            foreach (string id in ActionMechanicsRegistry.AllMechanicIds)
            {
                rowData.Add(new RowData
                {
                    Values = new List<CellData>
                    {
                        new CellData { Note = ActionMechanicDescriptions.BuildHoverNote(id) }
                    }
                });
            }

            await UpdateNotesColumnAsync(service, spreadsheetId, sheetId, startRow: 0, startCol: 0, rowData, cancellationToken)
                .ConfigureAwait(false);
        }

        private static async Task ApplyActionsHeaderNotesAsync(
            SheetsService service,
            string spreadsheetId,
            int sheetId,
            SpreadsheetHeader header,
            CancellationToken cancellationToken)
        {
            int labelRow = header.LabelRowIndex;
            int width = header.LabelByIndex.Count;
            var cells = new List<CellData>(width);
            int notesApplied = 0;
            for (int i = 0; i < width; i++)
            {
                string label = i < header.LabelByIndex.Count ? header.LabelByIndex[i] : "";
                string ctx = i < header.ContextByIndex.Count ? header.ContextByIndex[i] : "";
                if (ActionSheetHeaderNotes.TryGetNote(ctx, label, out string note))
                {
                    cells.Add(new CellData { Note = note });
                    notesApplied++;
                }
                else
                    cells.Add(new CellData());
            }

            if (notesApplied == 0)
                return;

            var request = new Request
            {
                UpdateCells = new UpdateCellsRequest
                {
                    Start = new GridCoordinate
                    {
                        SheetId = sheetId,
                        RowIndex = labelRow,
                        ColumnIndex = 0
                    },
                    Rows = new List<RowData> { new RowData { Values = cells } },
                    Fields = "note"
                }
            };

            await service.Spreadsheets
                .BatchUpdate(new BatchUpdateSpreadsheetRequest { Requests = new List<Request> { request } }, spreadsheetId)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            Console.WriteLine($"Sheets push: applied hover notes to {notesApplied} ACTIONS header cell(s).");
        }

        private static async Task UpdateNotesColumnAsync(
            SheetsService service,
            string spreadsheetId,
            int sheetId,
            int startRow,
            int startCol,
            List<RowData> rowData,
            CancellationToken cancellationToken)
        {
            var request = new Request
            {
                UpdateCells = new UpdateCellsRequest
                {
                    Start = new GridCoordinate
                    {
                        SheetId = sheetId,
                        RowIndex = startRow,
                        ColumnIndex = startCol
                    },
                    Rows = rowData,
                    Fields = "note"
                }
            };

            await service.Spreadsheets
                .BatchUpdate(new BatchUpdateSpreadsheetRequest { Requests = new List<Request> { request } }, spreadsheetId)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
