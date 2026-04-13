using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RPGGame.Data
{
    /// <summary>CSV parsing for single-header-row game data tabs (quoted fields, newlines inside quotes).</summary>
    public static class SimpleGameDataCsvParser
    {
        /// <summary>Parses full CSV text into rows; each row is a list of unescaped field strings.</summary>
        public static List<string[]> ParseToRows(string csvContent)
        {
            var rows = new List<string[]>();
            if (string.IsNullOrEmpty(csvContent))
                return rows;

            var currentRow = new List<string>();
            var field = new StringBuilder();
            bool inQuotes = false;
            int len = csvContent.Length;

            void EndField()
            {
                currentRow.Add(field.ToString());
                field.Clear();
            }

            void EndRow()
            {
                EndField();
                rows.Add(currentRow.ToArray());
                currentRow = new List<string>();
            }

            for (int i = 0; i < len; i++)
            {
                char c = csvContent[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < len && csvContent[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                        inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                    EndField();
                else if (!inQuotes && (c == '\n' || c == '\r'))
                {
                    EndRow();
                    if (c == '\r' && i + 1 < len && csvContent[i + 1] == '\n')
                        i++;
                }
                else
                    field.Append(c);
            }

            EndField();
            if (currentRow.Count > 0)
                rows.Add(currentRow.ToArray());

            while (rows.Count > 0 && rows[^1].All(string.IsNullOrEmpty))
                rows.RemoveAt(rows.Count - 1);

            return rows;
        }
    }
}
