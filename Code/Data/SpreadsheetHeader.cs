using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Data
{
    /// <summary>
    /// Header context from the Google Sheet: row 1 (section/context labels) and row 2 (column labels).
    /// Row 1 provides section context (e.g. "STATUS EFFECT", "HERO DICE ROLL MODIFICATIONS") so that
    /// column semantics and mechanics can be determined correctly when labels repeat or columns move.
    /// </summary>
    public class SpreadsheetHeader
    {
        /// <summary>Section/context per column index (row 1). Empty cells under merged headers carry the last non-empty section.</summary>
        public IReadOnlyList<string> ContextByIndex { get; }

        /// <summary>Column label per index (row 2).</summary>
        public IReadOnlyList<string> LabelByIndex { get; }

        /// <summary>Label row index in the CSV (0-based), for reference.</summary>
        public int LabelRowIndex { get; }

        /// <summary>Column index at which data rows start (after any continuation rows).</summary>
        public int DataStartRowIndex { get; }

        public SpreadsheetHeader(
            IReadOnlyList<string> contextByIndex,
            IReadOnlyList<string> labelByIndex,
            int labelRowIndex,
            int dataStartRowIndex)
        {
            ContextByIndex = contextByIndex ?? Array.Empty<string>();
            LabelByIndex = labelByIndex ?? Array.Empty<string>();
            LabelRowIndex = labelRowIndex;
            DataStartRowIndex = dataStartRowIndex;
        }

        /// <summary>
        /// Gets the column index for a label, optionally scoped by context (row 1 section).
        /// Tries (context, label) first, then label-only, so row 1 context disambiguates when the same label appears in multiple sections.
        /// </summary>
        /// <param name="contextHint">Section name from row 1 (e.g. "STATUS EFFECT", "HERO HEAL"). Can be null to match any context.</param>
        /// <param name="label">Column label from row 2 (e.g. "STUN", "HEAL"). Normalized: trimmed, case-insensitive.</param>
        /// <param name="rawLabelMustContain">If non-null, only consider columns whose raw header contains this substring (case-insensitive). Use to avoid matching e.g. "Damage (DPS)" when looking for "Damage (%)".</param>
        /// <returns>Column index, or -1 if not found.</returns>
        public int GetColumnIndex(string? contextHint, string label, string? rawLabelMustContain = null)
        {
            if (string.IsNullOrWhiteSpace(label))
                return -1;

            string normalizedLabel = NormalizeLabel(label);
            bool useContext = !string.IsNullOrWhiteSpace(contextHint);
            string? normalizedContext = useContext ? NormalizeLabel(contextHint!) : null;
            bool filterRaw = !string.IsNullOrWhiteSpace(rawLabelMustContain);

            // Prefer exact (context, label) match when context is specified
            if (useContext && normalizedContext != null)
            {
                for (int i = 0; i < Math.Min(ContextByIndex.Count, LabelByIndex.Count); i++)
                {
                    if (NormalizeLabel(LabelByIndex[i]) == normalizedLabel
                        && NormalizeLabel(ContextByIndex[i]) == normalizedContext
                        && (!filterRaw || (LabelByIndex[i]?.IndexOf(rawLabelMustContain!, StringComparison.OrdinalIgnoreCase) >= 0)))
                        return i;
                }
            }

            // Fall back to first label-only match (with optional raw-label filter)
            for (int i = 0; i < LabelByIndex.Count; i++)
            {
                if (NormalizeLabel(LabelByIndex[i]) == normalizedLabel
                    && (!filterRaw || (LabelByIndex[i]?.IndexOf(rawLabelMustContain!, StringComparison.OrdinalIgnoreCase) >= 0)))
                    return i;
            }

            return -1;
        }

        /// <summary>Gets cell value by (context, label). Returns empty string if column not found or value missing.</summary>
        /// <param name="rawLabelMustContain">If non-null, only consider columns whose raw header contains this substring (e.g. "%" to match "Damage (%)" but not "Damage (DPS)").</param>
        public string GetValue(string[] row, string? contextHint, string label, string? rawLabelMustContain = null)
        {
            int idx = GetColumnIndex(contextHint, label, rawLabelMustContain);
            if (idx < 0 || idx >= row.Length)
                return "";
            return row[idx].Trim();
        }

        /// <summary>
        /// Writes a cell if a matching column exists (inverse of <see cref="GetValue"/>). Used when pushing rows to Google Sheets.
        /// </summary>
        public void SetCell(string[] row, string? contextHint, string label, string value, string? rawLabelMustContain = null)
        {
            int idx = GetColumnIndex(contextHint, label, rawLabelMustContain);
            if (idx >= 0 && idx < row.Length)
                row[idx] = value ?? "";
        }

        /// <summary>Normalizes a header cell for matching: trim, uppercase, collapse whitespace, remove common punctuation in parentheses.</summary>
        public static string NormalizeLabel(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";
            value = value.Trim();
            // Remove parenthetical suffixes for matching (e.g. "DPS(%)" -> "DPS", "ACCUARCY" stays)
            int paren = value.IndexOf('(');
            if (paren > 0)
                value = value.Substring(0, paren).Trim();
            return value.ToUpperInvariant().Replace(" ", "");
        }

        /// <summary>Fills merged-cell semantics: empty cells in context row carry the last non-empty value.</summary>
        public static string[] FillMergedContext(string[] contextRow)
        {
            if (contextRow == null || contextRow.Length == 0)
                return contextRow ?? Array.Empty<string>();
            var filled = new string[contextRow.Length];
            string current = "";
            for (int i = 0; i < contextRow.Length; i++)
            {
                string cell = contextRow[i].Trim();
                if (!string.IsNullOrEmpty(cell))
                    current = cell;
                filled[i] = current;
            }
            return filled;
        }
    }

    /// <summary>
    /// Result of parsing spreadsheet CSV: actions and the header (row 1 context + row 2 labels) used for ingestion.
    /// </summary>
    public class SpreadsheetParseResult
    {
        public List<SpreadsheetActionData> Actions { get; }
        public SpreadsheetHeader? Header { get; }

        public SpreadsheetParseResult(List<SpreadsheetActionData> actions, SpreadsheetHeader? header)
        {
            Actions = actions ?? new List<SpreadsheetActionData>();
            Header = header;
        }
    }
}
