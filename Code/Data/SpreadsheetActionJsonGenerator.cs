using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RPGGame;

namespace RPGGame.Data
{
    /// <summary>
    /// Generates JSON output from parsed spreadsheet actions
    /// Now outputs spreadsheet-compatible JSON format
    /// </summary>
    public static class SpreadsheetActionJsonGenerator
    {
        /// <summary>
        /// Generates JSON from a list of SpreadsheetActionJson objects (spreadsheet-compatible format)
        /// Uses custom converter to omit empty string properties, significantly reducing file size
        /// </summary>
        public static string GenerateJson(List<SpreadsheetActionJson> actions)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                // Note: Custom converter (SpreadsheetActionJsonConverter) handles omitting empty strings
            };
            
            return JsonSerializer.Serialize(actions, options);
        }
        
        /// <summary>
        /// Generates JSON and writes to file (spreadsheet-compatible format)
        /// </summary>
        public static void GenerateJsonFile(List<SpreadsheetActionJson> actions, string outputPath)
        {
            string json = GenerateJson(actions);
            File.WriteAllText(outputPath, json);
        }
        
        /// <summary>
        /// Converts spreadsheet actions to SpreadsheetActionJson and generates JSON
        /// This is the new primary method that outputs spreadsheet-compatible JSON
        /// </summary>
        public static string ConvertAndGenerateJson(List<SpreadsheetActionData> spreadsheetActions)
        {
            var jsonList = new List<SpreadsheetActionJson>();
            
            foreach (var spreadsheet in spreadsheetActions)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(spreadsheet.Action))
                    {
                        var json = SpreadsheetActionJson.FromSpreadsheetActionData(spreadsheet);
                        jsonList.Add(json);
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue processing
                    Console.WriteLine($"Error converting action {spreadsheet.Action}: {ex.Message}");
                }
            }
            
            return GenerateJson(jsonList);
        }
        
        /// <summary>
        /// Converts spreadsheet actions to SpreadsheetActionJson and writes JSON to file
        /// This is the new primary method that outputs spreadsheet-compatible JSON
        /// </summary>
        public static void ConvertAndGenerateJsonFile(List<SpreadsheetActionData> spreadsheetActions, string outputPath)
        {
            var jsonList = new List<SpreadsheetActionJson>();
            
            foreach (var spreadsheet in spreadsheetActions)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(spreadsheet.Action))
                    {
                        var jsonAction = SpreadsheetActionJson.FromSpreadsheetActionData(spreadsheet);
                        jsonList.Add(jsonAction);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error converting action {spreadsheet.Action}: {ex.Message}");
                }
            }
            
            // Generate JSON from SpreadsheetActionJson list
            string jsonContent = GenerateJson(jsonList);
            File.WriteAllText(outputPath, jsonContent);
        }
    }
}
