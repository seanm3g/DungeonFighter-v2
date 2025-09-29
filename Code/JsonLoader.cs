using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace RPGGame
{
    /// <summary>
    /// Centralized JSON loading utility that handles common patterns for loading game data
    /// Eliminates duplication of JSON loading logic across GameDataGenerator.cs and LootGenerator.cs
    /// </summary>
    public static class JsonLoader
    {
        private static readonly JsonSerializerOptions _defaultOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        /// <summary>
        /// Loads a JSON file and deserializes it to the specified type
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="filePath">The path to the JSON file</param>
        /// <param name="defaultValue">Default value to return if loading fails</param>
        /// <param name="options">Optional JsonSerializerOptions (uses default if null)</param>
        /// <returns>The deserialized object or default value</returns>
        public static T LoadJson<T>(string filePath, T defaultValue = default(T)!, JsonSerializerOptions? options = null)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    DebugLogger.LogFormat("JsonLoader", "File not found: {0}", filePath);
                    return defaultValue;
                }

                string json = File.ReadAllText(filePath);
                var result = JsonSerializer.Deserialize<T>(json, options ?? _defaultOptions);
                
                DebugLogger.LogFormat("JsonLoader", "Successfully loaded {0} from {1}", typeof(T).Name, filePath);
                return result ?? defaultValue;
            }
            catch (Exception ex)
            {
                DebugLogger.LogFormat("JsonLoader", "Error loading {0} from {1}: {2}", typeof(T).Name, filePath, ex.Message);
                return defaultValue;
            }
        }

        /// <summary>
        /// Loads a JSON file and deserializes it to a list of the specified type
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="filePath">The path to the JSON file</param>
        /// <param name="options">Optional JsonSerializerOptions (uses default if null)</param>
        /// <returns>The deserialized list or empty list if loading fails</returns>
        public static List<T> LoadJsonList<T>(string filePath, JsonSerializerOptions? options = null)
        {
            return LoadJson<List<T>>(filePath, new List<T>(), options);
        }

        /// <summary>
        /// Loads a JSON file and deserializes it to a dictionary of the specified types
        /// </summary>
        /// <typeparam name="TKey">The key type</typeparam>
        /// <typeparam name="TValue">The value type</typeparam>
        /// <param name="filePath">The path to the JSON file</param>
        /// <param name="options">Optional JsonSerializerOptions (uses default if null)</param>
        /// <returns>The deserialized dictionary or empty dictionary if loading fails</returns>
        public static Dictionary<TKey, TValue> LoadJsonDictionary<TKey, TValue>(string filePath, JsonSerializerOptions? options = null) where TKey : notnull
        {
            return LoadJson<Dictionary<TKey, TValue>>(filePath, new Dictionary<TKey, TValue>(), options);
        }

        /// <summary>
        /// Checks if a JSON file exists and is readable
        /// </summary>
        /// <param name="filePath">The path to the JSON file</param>
        /// <returns>True if the file exists and is readable</returns>
        public static bool JsonFileExists(string filePath)
        {
            try
            {
                return File.Exists(filePath);
            }
            catch (Exception ex)
            {
                DebugLogger.LogFormat("JsonLoader", "Error checking file existence for {0}: {1}", filePath, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Gets the file path for a JSON file in the GameData directory
        /// </summary>
        /// <param name="fileName">The name of the JSON file</param>
        /// <returns>The full path to the file</returns>
        public static string GetGameDataPath(string fileName)
        {
            return Path.Combine("GameData", fileName);
        }

        /// <summary>
        /// Loads a JSON file from the GameData directory
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="fileName">The name of the JSON file in GameData</param>
        /// <param name="defaultValue">Default value to return if loading fails</param>
        /// <param name="options">Optional JsonSerializerOptions (uses default if null)</param>
        /// <returns>The deserialized object or default value</returns>
        public static T LoadGameDataJson<T>(string fileName, T defaultValue = default(T)!, JsonSerializerOptions? options = null)
        {
            string filePath = GetGameDataPath(fileName);
            return LoadJson<T>(filePath, defaultValue, options);
        }

        /// <summary>
        /// Loads a JSON list from the GameData directory
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="fileName">The name of the JSON file in GameData</param>
        /// <param name="options">Optional JsonSerializerOptions (uses default if null)</param>
        /// <returns>The deserialized list or empty list if loading fails</returns>
        public static List<T> LoadGameDataJsonList<T>(string fileName, JsonSerializerOptions? options = null)
        {
            string filePath = GetGameDataPath(fileName);
            return LoadJsonList<T>(filePath, options);
        }

        /// <summary>
        /// Loads a JSON dictionary from the GameData directory
        /// </summary>
        /// <typeparam name="TKey">The key type</typeparam>
        /// <typeparam name="TValue">The value type</typeparam>
        /// <param name="fileName">The name of the JSON file in GameData</param>
        /// <param name="options">Optional JsonSerializerOptions (uses default if null)</param>
        /// <returns>The deserialized dictionary or empty dictionary if loading fails</returns>
        public static Dictionary<TKey, TValue> LoadGameDataJsonDictionary<TKey, TValue>(string fileName, JsonSerializerOptions? options = null) where TKey : notnull
        {
            string filePath = GetGameDataPath(fileName);
            return LoadJsonDictionary<TKey, TValue>(filePath, options);
        }

        /// <summary>
        /// Saves an object to a JSON file
        /// </summary>
        /// <typeparam name="T">The type of object to serialize</typeparam>
        /// <param name="obj">The object to serialize</param>
        /// <param name="filePath">The path to save the JSON file</param>
        /// <param name="options">Optional JsonSerializerOptions (uses default if null)</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool SaveJson<T>(T obj, string filePath, JsonSerializerOptions? options = null)
        {
            try
            {
                string json = JsonSerializer.Serialize(obj, options ?? _defaultOptions);
                File.WriteAllText(filePath, json);
                DebugLogger.LogFormat("JsonLoader", "Successfully saved {0} to {1}", typeof(T).Name, filePath);
                return true;
            }
            catch (Exception ex)
            {
                DebugLogger.LogFormat("JsonLoader", "Error saving {0} to {1}: {2}", typeof(T).Name, filePath, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Saves an object to a JSON file in the GameData directory
        /// </summary>
        /// <typeparam name="T">The type of object to serialize</typeparam>
        /// <param name="obj">The object to serialize</param>
        /// <param name="fileName">The name of the JSON file in GameData</param>
        /// <param name="options">Optional JsonSerializerOptions (uses default if null)</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool SaveGameDataJson<T>(T obj, string fileName, JsonSerializerOptions? options = null)
        {
            string filePath = GetGameDataPath(fileName);
            return SaveJson(obj, filePath, options);
        }

        /// <summary>
        /// Gets the default JsonSerializerOptions used by the loader
        /// </summary>
        /// <returns>The default JsonSerializerOptions</returns>
        public static JsonSerializerOptions GetDefaultOptions()
        {
            return _defaultOptions;
        }

        /// <summary>
        /// Creates a new JsonSerializerOptions with custom settings
        /// </summary>
        /// <param name="propertyNameCaseInsensitive">Whether property names should be case insensitive</param>
        /// <param name="writeIndented">Whether to write indented JSON</param>
        /// <returns>A new JsonSerializerOptions instance</returns>
        public static JsonSerializerOptions CreateOptions(bool propertyNameCaseInsensitive = true, bool writeIndented = true)
        {
            return new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = propertyNameCaseInsensitive,
                WriteIndented = writeIndented
            };
        }

        /// <summary>
        /// Finds a GameData file by searching multiple possible locations
        /// Consolidates the FindGameDataFile logic from GameDataGenerator.cs and LootGenerator.cs
        /// </summary>
        /// <param name="fileName">The name of the file to find</param>
        /// <returns>The full path to the file if found, null otherwise</returns>
        public static string? FindGameDataFile(string fileName)
        {
            // Get the directory where the executable is located
            string executableDir = AppDomain.CurrentDomain.BaseDirectory;
            string currentDir = Directory.GetCurrentDirectory();
            
            // List of possible GameData directory locations relative to different starting points
            string[] possibleGameDataDirs = {
                // Relative to executable directory
                Path.Combine(executableDir, "GameData"),
                Path.Combine(executableDir, "..", "GameData"),
                Path.Combine(executableDir, "..", "..", "GameData"),
                
                // Relative to current working directory
                Path.Combine(currentDir, "GameData"),
                Path.Combine(currentDir, "..", "GameData"),
                Path.Combine(currentDir, "..", "..", "GameData"),
                
                // Common project structure variations
                Path.Combine(executableDir, "..", "..", "..", "GameData"),
                Path.Combine(currentDir, "..", "..", "..", "GameData"),
                
                // Legacy paths for backward compatibility
                Path.Combine("GameData"),
                Path.Combine("..", "GameData"),
                Path.Combine("..", "..", "GameData"),
                Path.Combine("DF4 - CONSOLE", "GameData"),
                Path.Combine("..", "DF4 - CONSOLE", "GameData")
            };

            // Try to find the GameData directory first
            string? gameDataDir = null;
            foreach (string dir in possibleGameDataDirs)
            {
                if (Directory.Exists(dir))
                {
                    gameDataDir = dir;
                    break;
                }
            }

            // If we found a GameData directory, use it
            if (gameDataDir != null)
            {
                string filePath = Path.Combine(gameDataDir, fileName);
                if (JsonFileExists(filePath))
                {
                    return filePath;
                }
            }

            // Fallback: try direct file paths
            string[] directPaths = {
                Path.Combine(executableDir, "GameData", fileName),
                Path.Combine(currentDir, "GameData", fileName),
                Path.Combine("GameData", fileName),
                Path.Combine("..", "GameData", fileName),
                Path.Combine("..", "..", "GameData", fileName)
            };

            foreach (string path in directPaths)
            {
                if (JsonFileExists(path))
                {
                    return path;
                }
            }

            return null;
        }
    }
}
