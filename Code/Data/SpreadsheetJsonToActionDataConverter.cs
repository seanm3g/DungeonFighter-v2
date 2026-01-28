using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;

namespace RPGGame.Data
{
    /// <summary>
    /// Converts SpreadsheetActionJson (spreadsheet-compatible JSON) to ActionData format
    /// Reuses logic from SpreadsheetToActionDataConverter
    /// </summary>
    public static class SpreadsheetJsonToActionDataConverter
    {
        /// <summary>
        /// Converts a SpreadsheetActionJson to ActionData
        /// </summary>
        public static ActionData Convert(SpreadsheetActionJson json)
        {
            // Convert to SpreadsheetActionData first, then use existing converter
            var spreadsheetData = json.ToSpreadsheetActionData();
            return SpreadsheetToActionDataConverter.Convert(spreadsheetData);
        }
        
        /// <summary>
        /// Converts a list of SpreadsheetActionJson to ActionData
        /// </summary>
        public static List<ActionData> ConvertList(List<SpreadsheetActionJson> jsonList)
        {
            var actionDataList = new List<ActionData>();
            
            foreach (var json in jsonList)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(json.Action))
                    {
                        var actionData = Convert(json);
                        if (!string.IsNullOrEmpty(actionData.Name))
                        {
                            actionDataList.Add(actionData);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue processing
                    Console.WriteLine($"Error converting action {json.Action}: {ex.Message}");
                }
            }
            
            return actionDataList;
        }
    }
}
