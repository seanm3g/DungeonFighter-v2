using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Data
{
    /// <summary>
    /// Builds ACTIONS tab body rows for push while preserving on-sheet row order and section headers.
    /// </summary>
    public static class ActionSheetsPushRowMerger
    {
        public readonly record struct MergeOutcome(
            IReadOnlyList<IList<object>> BodyRows,
            int UpdatedActionCount,
            int PreservedSectionRowCount,
            int AppendedActionCount);

        /// <summary>
        /// Walks existing sheet rows in order, updates matching actions from JSON, preserves TIER/LAYER headers,
        /// then appends JSON actions that were not present on the sheet.
        /// </summary>
        public static MergeOutcome BuildBodyRowsPreservingSheetOrder(
            IReadOnlyList<string[]> existingSheetRows,
            IReadOnlyList<SpreadsheetActionJson> actionsFromJson,
            SpreadsheetHeader header)
        {
            int width = header.LabelByIndex.Count;
            int actionCol = header.GetColumnIndex(null, "ACTION");
            if (actionCol < 0)
                actionCol = 0;

            var actionByName = BuildActionLookup(actionsFromJson);
            var pushedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var bodyRows = new List<IList<object>>();
            int preservedSections = 0;
            int updatedActions = 0;

            foreach (string[] existingRow in existingSheetRows)
            {
                string actionName = GetActionName(existingRow, actionCol);
                if (SpreadsheetActionData.IsPushPreservedSectionRow(actionName))
                {
                    bodyRows.Add(RowStringsToUploadCells(existingRow, width));
                    preservedSections++;
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(actionName) && actionByName.TryGetValue(actionName, out SpreadsheetActionJson? json))
                {
                    bodyRows.Add(BuildUploadRow(json, header, width));
                    pushedNames.Add(actionName);
                    updatedActions++;
                    continue;
                }

                if (RowHasVisibleContent(existingRow))
                    bodyRows.Add(RowStringsToUploadCells(existingRow, width));
            }

            int appended = 0;
            foreach (SpreadsheetActionJson json in actionsFromJson)
            {
                var data = json.ToSpreadsheetActionData();
                if (!data.IsValid())
                    continue;

                string name = data.Action.Trim();
                if (pushedNames.Contains(name))
                    continue;

                bodyRows.Add(BuildUploadRow(json, header, width));
                pushedNames.Add(name);
                appended++;
            }

            return new MergeOutcome(bodyRows, updatedActions, preservedSections, appended);
        }

        /// <summary>Removes trailing rows that are entirely blank.</summary>
        public static void TrimTrailingEmptyRows(List<string[]> rows)
        {
            while (rows.Count > 0 && !RowHasVisibleContent(rows[^1]))
                rows.RemoveAt(rows.Count - 1);
        }

        private static Dictionary<string, SpreadsheetActionJson> BuildActionLookup(IReadOnlyList<SpreadsheetActionJson> actionsFromJson)
        {
            var lookup = new Dictionary<string, SpreadsheetActionJson>(StringComparer.OrdinalIgnoreCase);
            foreach (SpreadsheetActionJson json in actionsFromJson)
            {
                var data = json.ToSpreadsheetActionData();
                if (!data.IsValid())
                    continue;
                lookup[data.Action.Trim()] = json;
            }

            return lookup;
        }

        private static string GetActionName(string[] row, int actionCol)
        {
            if (actionCol < 0 || actionCol >= row.Length)
                return row.Length > 0 ? row[0].Trim() : "";
            return row[actionCol].Trim();
        }

        private static bool RowHasVisibleContent(string[] row)
        {
            foreach (string cell in row)
            {
                if (!string.IsNullOrWhiteSpace(SheetsPushUtilities.NormalizeSheetString(cell)))
                    return true;
            }

            return false;
        }

        private static List<object> RowStringsToUploadCells(string[] row, int width)
        {
            var list = new List<object>(width);
            for (int i = 0; i < width; i++)
            {
                string value = i < row.Length ? row[i] : "";
                list.Add(SheetsPushUtilities.NormalizeCellValueForUpload(value));
            }

            return list;
        }

        private static List<object> BuildUploadRow(SpreadsheetActionJson json, SpreadsheetHeader header, int width)
        {
            var data = json.ToSpreadsheetActionData();
            ActionMechanicsSheetSync.SyncRow(data);
            string[] cells = SpreadsheetActionDataSheetRowSerializer.ToRow(data, header);
            var row = cells.Select(c => SheetsPushUtilities.NormalizeCellValueForUpload(c)).ToList();
            while (row.Count < width)
                row.Add("");
            if (row.Count > width)
                row.RemoveRange(width, row.Count - width);
            return row;
        }
    }
}
