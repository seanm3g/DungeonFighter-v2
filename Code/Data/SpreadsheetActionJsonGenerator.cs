using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using RPGGame;

namespace RPGGame.Data
{
    /// <summary>
    /// Generates JSON output from parsed spreadsheet actions
    /// </summary>
    public static class SpreadsheetActionJsonGenerator
    {
        /// <summary>
        /// Generates JSON from a list of ActionData objects
        /// </summary>
        public static string GenerateJson(List<ActionData> actions)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            return JsonSerializer.Serialize(actions, options);
        }
        
        /// <summary>
        /// Generates JSON and writes to file
        /// </summary>
        public static void GenerateJsonFile(List<ActionData> actions, string outputPath)
        {
            string json = GenerateJson(actions);
            File.WriteAllText(outputPath, json);
        }
        
        /// <summary>
        /// Converts spreadsheet actions to ActionData and generates JSON
        /// </summary>
        public static string ConvertAndGenerateJson(List<SpreadsheetActionData> spreadsheetActions)
        {
            var actionDataList = new List<ActionData>();
            
            foreach (var spreadsheet in spreadsheetActions)
            {
                try
                {
                    var actionData = SpreadsheetToActionDataConverter.Convert(spreadsheet);
                    if (!string.IsNullOrEmpty(actionData.Name))
                    {
                        actionDataList.Add(actionData);
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue processing
                    Console.WriteLine($"Error converting action {spreadsheet.Action}: {ex.Message}");
                }
            }
            
            return GenerateJson(actionDataList);
        }
        
        /// <summary>
        /// Converts spreadsheet actions to ActionData and writes JSON to file
        /// </summary>
        public static void ConvertAndGenerateJsonFile(List<SpreadsheetActionData> spreadsheetActions, string outputPath)
        {
            // Convert to ActionData first
            var actionDataList = new List<ActionData>();
            
            foreach (var spreadsheet in spreadsheetActions)
            {
                try
                {
                    var actionData = SpreadsheetToActionDataConverter.Convert(spreadsheet);
                    if (!string.IsNullOrEmpty(actionData.Name))
                    {
                        actionDataList.Add(actionData);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error converting action {spreadsheet.Action}: {ex.Message}");
                }
            }
            
            // Generate JSON from converted ActionData list
            string json = GenerateJson(actionDataList);
            File.WriteAllText(outputPath, json);
        }
    }
}
