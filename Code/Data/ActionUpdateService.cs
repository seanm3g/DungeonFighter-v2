using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RPGGame;

namespace RPGGame.Data
{
    /// <summary>
    /// Service for updating Actions.json from Google Sheets
    /// </summary>
    public static class ActionUpdateService
    {
        /// <summary>
        /// Optimizes the existing Actions.json file by removing empty fields
        /// Loads the current file, converts to SpreadsheetActionJson, and saves with optimization
        /// </summary>
        public static void OptimizeActionsJsonFile(string? filePath = null)
        {
            try
            {
                // Get file path
                filePath ??= GameConstants.GetGameDataFilePath("Actions.json");
                
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Actions.json not found at: {filePath}");
                    return;
                }
                
                Console.WriteLine($"Loading Actions.json from: {filePath}");
                
                // Load the current file
                var spreadsheetActions = RPGGame.JsonLoader.LoadJson<List<SpreadsheetActionJson>>(
                    filePath,
                    useCache: false,
                    fallbackValue: new List<SpreadsheetActionJson>()
                );
                
                if (spreadsheetActions == null || spreadsheetActions.Count == 0)
                {
                    Console.WriteLine("No actions found in file. Nothing to optimize.");
                    return;
                }
                
                Console.WriteLine($"Loaded {spreadsheetActions.Count} actions");
                Console.WriteLine("Optimizing file (removing empty fields)...");
                
                // Save using the generator which uses the converter to omit empty fields
                SpreadsheetActionJsonGenerator.GenerateJsonFile(spreadsheetActions, filePath);
                
                // Get file size info
                var fileInfo = new FileInfo(filePath);
                Console.WriteLine($"✓ Optimized Actions.json saved to: {filePath}");
                Console.WriteLine($"  File size: {fileInfo.Length / 1024.0:F2} KB");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error optimizing Actions.json: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }
        /// <summary>
        /// Updates Actions.json from Google Sheets URL
        /// </summary>
        /// <param name="googleSheetsUrl">URL to Google Sheets CSV export (optional, uses config if not provided)</param>
        /// <param name="outputPath">Path to output JSON file (optional, uses default if not provided)</param>
        public static async Task UpdateFromGoogleSheetsAsync(string? googleSheetsUrl = null, string? outputPath = null)
        {
            try
            {
                // Get URL from parameter or config
                if (string.IsNullOrWhiteSpace(googleSheetsUrl) || googleSheetsUrl == "\"\"")
                {
                    var config = SheetsConfig.Load();
                    googleSheetsUrl = config.ActionsSheetUrl;
                    Console.WriteLine($"Loaded URL from config: {googleSheetsUrl}");
                }
                
                if (string.IsNullOrWhiteSpace(googleSheetsUrl))
                {
                    throw new ArgumentException("Google Sheets URL is required. Provide it as parameter or set it in GameData/SheetsConfig.json");
                }
                
                // Verify it's actually a URL
                if (!googleSheetsUrl.StartsWith("http://") && !googleSheetsUrl.StartsWith("https://"))
                {
                    throw new ArgumentException($"Invalid Google Sheets URL: {googleSheetsUrl}. URL must start with http:// or https://");
                }
                
                // Use same canonical path as ActionLoader (project root GameData, not Code\GameData)
                outputPath ??= GameConstants.GetGameDataFilePath("Actions.json");
                
                Console.WriteLine($"Fetching actions from Google Sheets: {googleSheetsUrl}");
                
                // Parse CSV and generate JSON
                await SpreadsheetParserRunner.ParseAndGenerateAsync(googleSheetsUrl, outputPath);
                
                Console.WriteLine($"Successfully updated {outputPath} from Google Sheets");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating actions from Google Sheets: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Updates Actions.json from Google Sheets URL (synchronous wrapper)
        /// </summary>
        public static void UpdateFromGoogleSheets(string? googleSheetsUrl = null, string? outputPath = null)
        {
            UpdateFromGoogleSheetsAsync(googleSheetsUrl, outputPath).GetAwaiter().GetResult();
        }
    }
}
