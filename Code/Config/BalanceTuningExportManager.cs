using System;
using System.IO;
using System.Text.Json;
using RPGGame;
using RPGGame.Utils;

namespace RPGGame.Config
{
    /// <summary>
    /// Manages export and import of balance tuning settings for sharing between players
    /// </summary>
    public class BalanceTuningExportManager
    {
        /// <summary>
        /// Export data structure for balance tuning settings
        /// </summary>
        public class BalanceTuningExport
        {
            public ExportMetadata Metadata { get; set; } = new();
            public BalanceTuningData Data { get; set; } = new();
        }

        /// <summary>
        /// Metadata about the exported settings
        /// </summary>
        public class ExportMetadata
        {
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";
            public string CreatedDate { get; set; } = "";
            public string Version { get; set; } = "1.0";
            public string CreatedBy { get; set; } = "";
            public string Notes { get; set; } = "";
        }

        /// <summary>
        /// Balance tuning configuration data
        /// </summary>
        public class BalanceTuningData
        {
            public BalanceTuningGoalsConfig BalanceTuningGoals { get; set; } = new();
            public EnemyBalanceConfig EnemyBalance { get; set; } = new();
            public CombatBalanceConfig CombatBalance { get; set; } = new();
            public DifficultySettingsConfig DifficultySettings { get; set; } = new();
        }

        /// <summary>
        /// Export current balance tuning settings to a file
        /// </summary>
        public static bool ExportToFile(string filePath, string name = "", string description = "", string createdBy = "", string notes = "")
        {
            try
            {
                var config = GameConfiguration.Instance;
                
                var export = new BalanceTuningExport
                {
                    Metadata = new ExportMetadata
                    {
                        Name = string.IsNullOrWhiteSpace(name) ? "Balance Tuning Settings" : name,
                        Description = description,
                        CreatedDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                        Version = "1.0",
                        CreatedBy = createdBy,
                        Notes = notes
                    },
                    Data = new BalanceTuningData
                    {
                        BalanceTuningGoals = config.BalanceTuningGoals,
                        EnemyBalance = config.EnemyBalance,
                        CombatBalance = config.CombatBalance,
                        DifficultySettings = config.DifficultySettings
                    }
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string json = JsonSerializer.Serialize(export, options);
                
                // Ensure directory exists
                string? directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(filePath, json);

                ScrollDebugLogger.Log($"BalanceTuningExportManager: Exported balance tuning settings to {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalanceTuningExportManager: Error exporting settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Import balance tuning settings from a file
        /// </summary>
        public static (bool Success, string? ErrorMessage, BalanceTuningExport? Export) ImportFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return (false, "File not found", null);
                }

                string json = File.ReadAllText(filePath);
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var export = JsonSerializer.Deserialize<BalanceTuningExport>(json, options);
                
                if (export == null)
                {
                    return (false, "Failed to parse file", null);
                }

                ScrollDebugLogger.Log($"BalanceTuningExportManager: Imported balance tuning settings from {filePath}");
                return (true, null, export);
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalanceTuningExportManager: Error importing settings: {ex.Message}");
                return (false, ex.Message, null);
            }
        }

        /// <summary>
        /// Apply imported balance tuning settings to the current game configuration
        /// </summary>
        public static bool ApplyImportedSettings(BalanceTuningExport export)
        {
            try
            {
                if (export?.Data == null)
                {
                    ScrollDebugLogger.Log("BalanceTuningExportManager: Export data is null");
                    return false;
                }

                var config = GameConfiguration.Instance;
                var data = export.Data;

                // Apply the imported settings
                config.BalanceTuningGoals = data.BalanceTuningGoals;
                config.EnemyBalance = data.EnemyBalance;
                config.CombatBalance = data.CombatBalance;
                config.DifficultySettings = data.DifficultySettings;

                // Save to file
                config.SaveToFile();

                ScrollDebugLogger.Log($"BalanceTuningExportManager: Applied imported balance tuning settings '{export.Metadata.Name}'");
                return true;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalanceTuningExportManager: Error applying imported settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get default export file path
        /// </summary>
        public static string GetDefaultExportPath(string fileName = "balance_tuning_export.json")
        {
            string directory = Path.Combine("GameData", "BalanceTuningExports");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            return Path.Combine(directory, fileName);
        }
    }
}

