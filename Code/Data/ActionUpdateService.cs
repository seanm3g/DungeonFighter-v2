using System;
using System.IO;
using System.Threading.Tasks;

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
                
                // Get output path
                outputPath ??= Path.Combine("GameData", "Actions.json");
                
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
