using System;
using System.IO;
using System.Text.Json;

namespace RPGGame
{
    /// <summary>
    /// Handles file I/O operations for game data generation including backup creation and safe writing
    /// </summary>
    public static class FileManager
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Safely writes JSON data to a file with backup creation
        /// Uses ErrorHandler for comprehensive error handling
        /// </summary>
        public static void SafeWriteJsonFile(string filePath, object data, bool createBackup = true)
        {
            ErrorHandler.TryExecute(() =>
            {
                // Create backup if file exists and backup is requested
                if (File.Exists(filePath) && createBackup)
                {
                    string backupPath = filePath + ".backup";
                    File.Copy(filePath, backupPath, true);
                }

                // Ensure directory exists
                string? directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Write the file
                string json = JsonSerializer.Serialize(data, _jsonOptions);
                File.WriteAllText(filePath, json);
            }, $"SafeWriteJsonFile({filePath})");
        }

        /// <summary>
        /// Gets the GameData file path using JsonLoader's existing logic
        /// </summary>
        public static string GetGameDataFilePath(string fileName)
        {
            // Use JsonLoader's existing file finding logic
            string? filePath = JsonLoader.FindGameDataFile(fileName);
            if (filePath != null)
            {
                return filePath;
            }

            // Fallback: try to find GameData directory and create file path
            string? gameDataDir = FindGameDataDirectory();
            if (gameDataDir != null)
            {
                return Path.Combine(gameDataDir, fileName);
            }

            // Last resort: use current directory
            return Path.Combine("GameData", fileName);
        }

        /// <summary>
        /// Simplified GameData directory finder
        /// </summary>
        private static string? FindGameDataDirectory()
        {
            string currentDir = Directory.GetCurrentDirectory();
            string executableDir = AppDomain.CurrentDomain.BaseDirectory;
            
            // Check common locations in order of preference
            string[] possibleDirs = {
                Path.Combine(currentDir, "GameData"),           // Current directory
                Path.Combine(currentDir, "..", "GameData"),     // Parent directory
                Path.Combine(executableDir, "GameData"),        // Executable directory
                Path.Combine(executableDir, "..", "GameData"),  // Executable parent
                "GameData"                                      // Relative path
            };

            foreach (string dir in possibleDirs)
            {
                if (Directory.Exists(dir))
                {
                    return dir;
                }
            }

            return null;
        }

        /// <summary>
        /// Creates a backup of a file if it exists
        /// Uses ErrorHandler for comprehensive error handling
        /// </summary>
        public static bool CreateBackup(string filePath)
        {
            return ErrorHandler.TryExecute<bool>(() =>
            {
                if (File.Exists(filePath))
                {
                    string backupPath = filePath + ".backup";
                    File.Copy(filePath, backupPath, true);
                    return true;
                }
                return false;
            }, $"CreateBackup({filePath})", false);
        }
    }
}
