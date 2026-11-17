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
                
                // Write to the same Actions.json the game already uses (not a second copy under a different GameData folder).
                outputPath ??= ActionLoader.ResolveActionsFilePath();
                
                Console.WriteLine($"Fetching actions from Google Sheets: {googleSheetsUrl}");
                Console.WriteLine($"Writing Actions.json to: {outputPath}");
                
                // Parse CSV and generate JSON (replaces the file entirely — removed sheet rows are dropped).
                await SpreadsheetParserRunner.ParseAndGenerateAsync(googleSheetsUrl, outputPath);

                string resolvedOut = outputPath;
                try { resolvedOut = Path.GetFullPath(outputPath); } catch { }

                ActionLoader.ReloadActions(resolvedOut);
                int loadedCount = ActionLoader.GetAllLoadedActionNames().Count;
                Console.WriteLine($"Successfully updated {resolvedOut} from Google Sheets ({loadedCount} actions loaded)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating actions from Google Sheets: {ex.Message}");
                throw;
            }
        }
        
    }
}
