using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RPGGame.Data
{
    /// <summary>
    /// Parser for converting Google Sheets CSV export to SpreadsheetActionData objects
    /// Supports both local files and published Google Sheets URLs
    /// </summary>
    public static class SpreadsheetActionParser
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        
        /// <summary>
        /// Parses CSV from either a file path or a URL (e.g., published Google Sheets CSV).
        /// Returns actions and header (row 1 context + row 2 labels) for mechanics and ingestion.
        /// </summary>
        public static async Task<SpreadsheetParseResult> ParseCsvAsync(string pathOrUrl)
        {
            string csvContent;
            if (pathOrUrl.StartsWith("http://") || pathOrUrl.StartsWith("https://"))
            {
                csvContent = await _httpClient.GetStringAsync(pathOrUrl);
            }
            else
            {
                if (!File.Exists(pathOrUrl))
                {
                    throw new FileNotFoundException($"CSV file not found: {pathOrUrl}");
                }
                csvContent = File.ReadAllText(pathOrUrl);
            }
            return ParseCsvContent(csvContent);
        }
        
        /// <summary>
        /// Parses a CSV file and returns a parse result with actions and header (row 1 context + row 2 labels).
        /// </summary>
        public static SpreadsheetParseResult ParseCsvFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"CSV file not found: {filePath}");
            }
            var lines = File.ReadAllLines(filePath);
            return ParseCsvLines(lines);
        }
        
        /// <summary>
        /// Parses a CSV line, handling quoted fields and commas within quotes
        /// </summary>
        private static string[] ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var currentField = new StringBuilder();
            bool inQuotes = false;
            
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // Escaped quote
                        currentField.Append('"');
                        i++; // Skip next quote
                    }
                    else
                    {
                        // Toggle quote state
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    // Field separator
                    fields.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }
            
            // Add last field
            fields.Add(currentField.ToString());
            
            return fields.ToArray();
        }
        
        /// <summary>
        /// Parses CSV content from a string. Uses row 1 (context/section labels) and row 2 (column labels)
        /// to determine column semantics and mechanics when mapping to SpreadsheetActionData.
        /// </summary>
        public static SpreadsheetParseResult ParseCsvContent(string csvContent)
        {
            var lines = csvContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            return ParseCsvLines(lines);
        }

        /// <summary>
        /// Parses lines: row 0 as optional context row (if not "ACTION,..."), then label row ("ACTION,..."), then data rows.
        /// </summary>
        private static SpreadsheetParseResult ParseCsvLines(string[] lines)
        {
            var actions = new List<SpreadsheetActionData>();
            SpreadsheetHeader? header = null;

            if (lines == null || lines.Length == 0)
                return new SpreadsheetParseResult(actions, null);

            // Row 1 (index 0): if it does NOT start with "ACTION,", treat as context row giving section info for row 2 labels
            int labelRowIndex = 0;
            string[] contextFilled = Array.Empty<string>();

            if (!(lines[0].StartsWith("ACTION,") || lines[0].StartsWith("\"ACTION\",")))
            {
                var contextRow = ParseCsvLine(lines[0]);
                contextFilled = SpreadsheetHeader.FillMergedContext(contextRow);
                labelRowIndex = 1;
            }
            else
            {
                for (int i = 0; i < Math.Min(5, lines.Length); i++)
                {
                    if (lines[i].StartsWith("ACTION,") || lines[i].StartsWith("\"ACTION\","))
                    {
                        labelRowIndex = i;
                        break;
                    }
                }
            }

            if (labelRowIndex >= lines.Length)
                return new SpreadsheetParseResult(actions, null);

            var labelRow = ParseCsvLine(lines[labelRowIndex]);
            if (contextFilled.Length == 0)
                contextFilled = new string[labelRow.Length];
            else if (contextFilled.Length < labelRow.Length)
            {
                var extended = new string[labelRow.Length];
                string last = contextFilled.Length > 0 ? contextFilled[contextFilled.Length - 1] : "";
                for (int i = 0; i < extended.Length; i++)
                    extended[i] = i < contextFilled.Length ? contextFilled[i] : last;
                contextFilled = extended;
            }
            else if (contextFilled.Length > labelRow.Length)
            {
                var trimmed = new string[labelRow.Length];
                Array.Copy(contextFilled, trimmed, labelRow.Length);
                contextFilled = trimmed;
            }

            header = new SpreadsheetHeader(
                contextFilled,
                labelRow.ToList(),
                labelRowIndex,
                dataStartRowIndex: labelRowIndex + 1);

            int dataStartRow = labelRowIndex + 1;

            for (int i = dataStartRow; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var columns = ParseCsvLine(line);
                var actionData = SpreadsheetActionData.FromCsvRow(columns, header);

                if (actionData.IsValid())
                {
                    actions.Add(actionData);
                }
            }

            return new SpreadsheetParseResult(actions, header);
        }
    }
}
