using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Data
{
    /// <summary>
    /// Ensures the ACTIONS sheet <c>BLOCK</c> column exists on push (unscoped label, after SPEED).
    /// Replaces legacy <c>ENERGY</c> in place when present so push does not leave both columns.
    /// </summary>
    public static class ActionBlockSheetColumns
    {
        public const string Label = "BLOCK";

        /// <summary>
        /// Renames ENERGY / ENERGY COST / ENERGYCOST → BLOCK when BLOCK is missing.
        /// Returns renamed zero-based indices (for writing header cells) and the updated header.
        /// </summary>
        public static (SpreadsheetHeader Header, IReadOnlyList<int> RenamedIndices) TryRenameEnergyToBlock(
            SpreadsheetHeader header)
        {
            if (header == null)
                throw new ArgumentNullException(nameof(header));

            if (HasBlockColumn(header))
                return (header, Array.Empty<int>());

            var energyIndices = CollectEnergyColumnIndices(header);
            if (energyIndices.Count == 0)
                return (header, Array.Empty<int>());

            var contexts = header.ContextByIndex.ToList();
            var labels = header.LabelByIndex.ToList();
            while (contexts.Count < labels.Count)
                contexts.Add("");
            while (labels.Count < contexts.Count)
                labels.Add("");

            // Prefer a single BLOCK column: rename the first ENERGY label; delete any extras later.
            int primary = energyIndices[0];
            labels[primary] = Label;

            var renamed = new List<int> { primary };
            var newHeader = new SpreadsheetHeader(
                contexts,
                labels,
                header.LabelRowIndex,
                header.DataStartRowIndex);
            return (newHeader, renamed);
        }

        /// <summary>
        /// ENERGY columns still present after rename (duplicates, or ENERGY left beside an existing BLOCK).
        /// </summary>
        public static IReadOnlyList<int> CollectEnergyColumnIndicesToRemove(SpreadsheetHeader header)
        {
            return CollectEnergyColumnIndices(header);
        }

        /// <summary>
        /// Inserts <see cref="Label"/> when missing. Returns updated header and whether a column was added.
        /// </summary>
        public static (SpreadsheetHeader Header, bool ColumnsAdded) EnsureHeader(SpreadsheetHeader header)
        {
            if (header == null)
                throw new ArgumentNullException(nameof(header));

            if (HasBlockColumn(header))
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

        public static bool HasBlockColumn(SpreadsheetHeader header)
        {
            if (header == null)
                return false;
            return header.GetColumnIndex(null, Label) >= 0
                || header.GetColumnIndex(null, "BLOCK %") >= 0
                || header.GetColumnIndex(null, "BLOCKPERCENT") >= 0;
        }

        public static bool IsEnergyLabel(string? label)
        {
            string n = SpreadsheetHeader.NormalizeLabel(label ?? "");
            return n is "ENERGY" or "ENERGYCOST";
        }

        private static IReadOnlyList<int> CollectEnergyColumnIndices(SpreadsheetHeader header)
        {
            if (header == null)
                return Array.Empty<int>();

            int count = Math.Max(header.ContextByIndex.Count, header.LabelByIndex.Count);
            var indices = new List<int>();
            for (int i = 0; i < count; i++)
            {
                string label = i < header.LabelByIndex.Count ? header.LabelByIndex[i] : "";
                if (IsEnergyLabel(label))
                    indices.Add(i);
            }

            return indices;
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
