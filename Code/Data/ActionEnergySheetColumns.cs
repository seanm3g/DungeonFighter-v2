using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Data
{
    /// <summary>
    /// Ensures the ACTIONS sheet <c>ENERGY</c> column exists on push (unscoped label, after SPEED).
    /// Push inserts a physical column; it does not rewrite other header labels.
    /// </summary>
    public static class ActionEnergySheetColumns
    {
        public const string Label = "ENERGY";

        /// <summary>
        /// Inserts <see cref="Label"/> when missing. Returns updated header and whether a column was added.
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

            int insertAt = FindInsertIndex(labels);
            if (insertAt >= 0)
            {
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

        private static int FindInsertIndex(List<string> labels)
        {
            int speedAt = labels.FindIndex(l => SpreadsheetHeader.NormalizeLabel(l) == "SPEED");
            if (speedAt >= 0)
                return speedAt + 1;

            int finisherAt = labels.FindIndex(l => SpreadsheetHeader.NormalizeLabel(l) == "FINISHER");
            if (finisherAt >= 0)
                return finisherAt + 1;

            return -1;
        }
    }
}
