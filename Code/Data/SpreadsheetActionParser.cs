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
        /// Parses CSV from either a file path or a URL (e.g., published Google Sheets CSV)
        /// </summary>
        public static async Task<List<SpreadsheetActionData>> ParseCsvAsync(string pathOrUrl)
        {
            string csvContent;
            
            if (pathOrUrl.StartsWith("http://") || pathOrUrl.StartsWith("https://"))
            {
                // Fetch from URL
                csvContent = await _httpClient.GetStringAsync(pathOrUrl);
            }
            else
            {
                // Read from file
                if (!File.Exists(pathOrUrl))
                {
                    throw new FileNotFoundException($"CSV file not found: {pathOrUrl}");
                }
                csvContent = File.ReadAllText(pathOrUrl);
            }
            
            return ParseCsvContent(csvContent);
        }
        
        /// <summary>
        /// Parses a CSV file and returns a list of SpreadsheetActionData objects
        /// </summary>
        public static List<SpreadsheetActionData> ParseCsvFile(string filePath)
        {
            var actions = new List<SpreadsheetActionData>();
            
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"CSV file not found: {filePath}");
            }
            
            var lines = File.ReadAllLines(filePath);
            
            // Skip header rows - look for the row that starts with "ACTION" (usually row 2, index 1)
            // Then skip the next 2 rows which are continuation of headers
            int dataStartRow = 1; // Default to row 2 (index 1)
            for (int i = 0; i < Math.Min(5, lines.Length); i++)
            {
                if (lines[i].StartsWith("ACTION,") || lines[i].StartsWith("\"ACTION\","))
                {
                    dataStartRow = i + 3; // Skip this row and next 2 continuation rows
                    break;
                }
            }
            
            for (int i = dataStartRow; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                
                var columns = ParseCsvLine(line);
                var actionData = SpreadsheetActionData.FromCsvRow(columns);
                
                if (actionData.IsValid())
                {
                    actions.Add(actionData);
                }
            }
            
            return actions;
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
        /// Parses CSV content from a string
        /// </summary>
        public static List<SpreadsheetActionData> ParseCsvContent(string csvContent)
        {
            var actions = new List<SpreadsheetActionData>();
            
            var lines = csvContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            
            // Skip header rows - look for the row that starts with "ACTION" (usually row 2, index 1)
            // Then skip the next 2 rows which are continuation of headers
            int dataStartRow = 1; // Default to row 2 (index 1)
            for (int i = 0; i < Math.Min(5, lines.Length); i++)
            {
                if (lines[i].StartsWith("ACTION,") || lines[i].StartsWith("\"ACTION\","))
                {
                    dataStartRow = i + 3; // Skip this row and next 2 continuation rows
                    break;
                }
            }
            
            for (int i = dataStartRow; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                
                var columns = ParseCsvLine(line);
                var actionData = SpreadsheetActionData.FromCsvRow(columns);
                
                if (actionData.IsValid())
                {
                    actions.Add(actionData);
                }
            }
            
            return actions;
        }
    }
}
