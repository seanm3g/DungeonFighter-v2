using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Data
{
    /// <summary>
    /// Ensures the ACTIONS sheet <c>RESERVE POOL</c> column exists on push (unscoped label, near OPENER/FINISHER).
    /// </summary>
    public static class ActionReservePoolSheetColumns
    {
        public const string Label = ActionTagSyncHelper.ReservePoolColumnLabel;

        /// <summary>
        /// Appends <see cref="Label"/> when missing. Returns updated header and whether a column was added.
        /// </summary>
        public static (SpreadsheetHeader Header, bool ColumnsAdded) EnsureHeader(SpreadsheetHeader header)
        {
            if (header == null)
                throw new ArgumentNullException(nameof(header));

            if (header.GetColumnIndex(null, Label) >= 0)
                return (header, false);

            var contexts = header.ContextByIndex.ToList();
            var labels = header.LabelByIndex.ToList();

            while (contexts.Count < labels.Count)
                contexts.Add("");
            while (labels.Count < contexts.Count)
                labels.Add("");

            // Prefer inserting after FINISHER when present; otherwise append.
            int insertAt = labels.FindIndex(l => SpreadsheetHeader.NormalizeLabel(l) == "FINISHER");
            if (insertAt >= 0)
            {
                insertAt++;
                contexts.Insert(insertAt, "");
                labels.Insert(insertAt, Label);
            }
            else
            {
                contexts.Add("");
                labels.Add(Label);
            }

            var newHeader = new SpreadsheetHeader(
                contexts,
                labels,
                header.LabelRowIndex,
                header.DataStartRowIndex);
            return (newHeader, true);
        }
    }
}
