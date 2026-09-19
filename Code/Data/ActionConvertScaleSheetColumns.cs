using System;

namespace RPGGame.Data
{
    /// <summary>
    /// ACTIONS convert-scale columns (legacy letters DS/DT/DU) and designer-facing aliases
    /// <c>bonus per keyword</c> / <c>effect</c> / <c>keyword</c>.
    /// </summary>
    public static class ActionConvertScaleSheetColumns
    {
        public static readonly string[] MaterialScaleLabels =
        {
            "DS",
            "MATERIAL SCALE",
            "MATERIALSCALE",
            "CONVERT MATERIAL",
            "BONUS PER KEYWORD"
        };

        public static readonly string[] KeywordScaleLabels =
        {
            "DT",
            "KEYWORD SCALE",
            "KEYWORDSCALE",
            "CONVERT KEYWORD",
            "EFFECT"
        };

        public static readonly string[] ScaleFormulaLabels =
        {
            "DU",
            "SCALE FORMULA",
            "SCALEFORMULA",
            "CONVERT FORMULA",
            "KEYWORD"
        };

        public static string ReadMaterialScale(SpreadsheetHeader header, string[] columns)
            => ReadFirst(header, columns, MaterialScaleLabels);

        public static string ReadKeywordScale(SpreadsheetHeader header, string[] columns)
            => ReadFirst(header, columns, KeywordScaleLabels);

        public static string ReadScaleFormula(SpreadsheetHeader header, string[] columns)
            => ReadFirst(header, columns, ScaleFormulaLabels);

        public static void Write(
            SpreadsheetHeader header,
            string[] row,
            SpreadsheetActionData data,
            string[]? existingRow = null)
        {
            if (header == null || row == null || data == null)
                return;

            WriteFirst(header, row, existingRow, MaterialScaleLabels, data.MaterialScale);
            WriteFirst(header, row, existingRow, KeywordScaleLabels, data.KeywordScale);
            WriteFirst(header, row, existingRow, ScaleFormulaLabels, data.ScaleFormula);
        }

        private static string ReadFirst(SpreadsheetHeader header, string[] columns, string[] labels)
        {
            if (header == null || columns == null || labels == null)
                return "";

            foreach (string label in labels)
            {
                string value = header.GetValue(columns, null, label);
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }

            return "";
        }

        private static void WriteFirst(
            SpreadsheetHeader header,
            string[] row,
            string[]? existingRow,
            string[] labels,
            string value)
        {
            int idx = header.GetFirstColumnIndex(labels);
            if (idx < 0 || idx >= row.Length)
                return;

            if (string.IsNullOrWhiteSpace(value)
                && existingRow != null
                && idx < existingRow.Length
                && !string.IsNullOrWhiteSpace(existingRow[idx]))
            {
                row[idx] = existingRow[idx];
                return;
            }

            row[idx] = SheetsPushUtilities.NormalizeSheetString(value);
        }
    }
}
