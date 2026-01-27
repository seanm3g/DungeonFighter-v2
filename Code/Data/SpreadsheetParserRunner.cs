using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using RPGGame.Data;

namespace RPGGame.Data
{
    /// <summary>
    /// Utility class to run the spreadsheet parser and generate JSON output
    /// </summary>
    public static class SpreadsheetParserRunner
    {
        /// <summary>
        /// Parses a CSV file or URL and generates Actions.json (synchronous wrapper)
        /// </summary>
        public static void ParseAndGenerate(string csvPathOrUrl, string outputPath)
        {
            ParseAndGenerateAsync(csvPathOrUrl, outputPath).GetAwaiter().GetResult();
        }
        
        /// <summary>
        /// Parses a CSV file or URL and generates Actions.json
        /// </summary>
        public static async Task ParseAndGenerateAsync(string csvPathOrUrl, string outputPath)
        {
            try
            {
                Console.WriteLine($"Reading CSV from: {csvPathOrUrl}");
                
                // Check if it's a URL or file path
                bool isUrl = csvPathOrUrl.StartsWith("http://") || csvPathOrUrl.StartsWith("https://");
                if (!isUrl && !File.Exists(csvPathOrUrl))
                {
                    Console.WriteLine($"Error: CSV file not found: {csvPathOrUrl}");
                    return;
                }
                
                var spreadsheetActions = isUrl 
                    ? await SpreadsheetActionParser.ParseCsvAsync(csvPathOrUrl)
                    : SpreadsheetActionParser.ParseCsvFile(csvPathOrUrl);
                Console.WriteLine($"Parsed {spreadsheetActions.Count} actions from spreadsheet");
                
                Console.WriteLine($"Converting to ActionData...");
                var actionDataList = new List<ActionData>();
                int successCount = 0;
                int errorCount = 0;
                
                foreach (var spreadsheet in spreadsheetActions)
                {
                    try
                    {
                        var actionData = SpreadsheetToActionDataConverter.Convert(spreadsheet);
                        if (!string.IsNullOrEmpty(actionData.Name))
                        {
                            actionDataList.Add(actionData);
                            successCount++;
                            
                            // Log actions with ACTION/ATTACK bonuses
                            if (actionData.ActionAttackBonuses != null && 
                                actionData.ActionAttackBonuses.BonusGroups.Count > 0)
                            {
                                string keywordString = ActionAttackKeywordProcessor.GenerateKeywordString(
                                    actionData.ActionAttackBonuses);
                                Console.WriteLine($"  {actionData.Name}: {keywordString}");
                            }
                            else if (!string.IsNullOrWhiteSpace(spreadsheet.Cadence))
                            {
                                // Debug: show why bonuses weren't captured
                                Console.WriteLine($"  {actionData.Name}: CADENCE='{spreadsheet.Cadence}', DURATION='{spreadsheet.Duration}'");
                                Console.WriteLine($"    Hero: ACC='{spreadsheet.HeroAccuracy}', HIT='{spreadsheet.HeroHit}', COMBO='{spreadsheet.HeroCombo}', CRIT='{spreadsheet.HeroCrit}'");
                                Console.WriteLine($"    Enemy: ACC='{spreadsheet.EnemyAccuracy}', HIT='{spreadsheet.EnemyHit}', COMBO='{spreadsheet.EnemyCombo}', CRIT='{spreadsheet.EnemyCrit}'");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error converting action {spreadsheet.Action}: {ex.Message}");
                        errorCount++;
                    }
                }
                
                Console.WriteLine($"Successfully converted {successCount} actions");
                if (errorCount > 0)
                {
                    Console.WriteLine($"Errors: {errorCount}");
                }
                
                Console.WriteLine($"Generating JSON...");
                SpreadsheetActionJsonGenerator.ConvertAndGenerateJsonFile(spreadsheetActions, outputPath);
                
                Console.WriteLine($"JSON written to: {outputPath}");
                Console.WriteLine($"Total actions in JSON: {actionDataList.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }
    }
}
