using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using RPGGame;

namespace RPGGame.Data
{
    /// <summary>
    /// Configuration for Google Sheets URLs
    /// </summary>
    public class SheetsConfig
    {
        [JsonPropertyName("actionsSheetUrl")]
        public string ActionsSheetUrl { get; set; } = "";
        
        /// <summary>
        /// Loads configuration from file
        /// </summary>
        public static SheetsConfig Load(string? configPath = null)
        {
            // Use GameConstants to properly resolve the GameData path
            configPath ??= GameConstants.GetGameDataFilePath("SheetsConfig.json");
            
            try
            {
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<SheetsConfig>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    return config ?? new SheetsConfig();
                }
                else
                {
                    Console.WriteLine($"SheetsConfig.json not found at: {configPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading SheetsConfig from {configPath}: {ex.Message}");
            }
            
            return new SheetsConfig();
        }
        
        /// <summary>
        /// Saves configuration to file
        /// </summary>
        public void Save(string? configPath = null)
        {
            // Use GameConstants to properly resolve the GameData path
            configPath ??= GameConstants.GetGameDataFilePath("SheetsConfig.json");
            
            try
            {
                // Ensure directory exists
                string? directory = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving SheetsConfig: {ex.Message}");
            }
        }
    }
}
