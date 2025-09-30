using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace RPGGame
{
    /// <summary>
    /// Centralized JSON loading utility that provides consistent JSON loading patterns
    /// Implements lazy loading, caching, and error handling as documented in CODE_PATTERNS.md
    /// </summary>
    public static class JsonLoader
    {
        private static readonly Dictionary<string, object> _cache = new Dictionary<string, object>();
        private static readonly JsonSerializerOptions _defaultOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        /// <summary>
        /// Loads JSON data with caching and error handling
        /// Follows the JSON Loading Pattern from CODE_PATTERNS.md
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="filePath">The path to the JSON file</param>
        /// <param name="useCache">Whether to use caching (default: true)</param>
        /// <param name="fallbackValue">Fallback value if loading fails</param>
        /// <returns>The loaded object or fallback value</returns>
        public static T LoadJson<T>(string filePath, bool useCache = true, T fallbackValue = default(T)!)
        {
            // Check cache first if caching is enabled
            if (useCache && _cache.TryGetValue(filePath, out var cachedValue))
            {
                if (cachedValue is T cachedResult)
                {
                    if (GameConfiguration.IsDebugEnabled)
                        UIManager.WriteSystemLine($"DEBUG: JsonLoader cache hit for {filePath}");
                    return cachedResult;
                }
            }

            return ErrorHandler.TryLoadJson(() =>
            {
                // Validate file path
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
                }

                // Check if file exists
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"JSON file not found: {filePath}");
                }

                // Read and deserialize JSON
                string json = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    throw new InvalidDataException($"JSON file is empty: {filePath}");
                }

                var result = JsonSerializer.Deserialize<T>(json, _defaultOptions);
                if (result == null)
                {
                    throw new InvalidDataException($"JSON deserialization returned null for: {filePath}");
                }

                // Cache the result if caching is enabled
                if (useCache)
                {
                    _cache[filePath] = result;
                    if (GameConfiguration.IsDebugEnabled)
                        UIManager.WriteSystemLine($"DEBUG: JsonLoader cached {filePath}");
                }

                return result;
            }, filePath, fallbackValue);
        }

        /// <summary>
        /// Loads JSON data from multiple possible paths
        /// Useful for finding files in different locations
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="possiblePaths">Array of possible file paths to try</param>
        /// <param name="useCache">Whether to use caching (default: true)</param>
        /// <param name="fallbackValue">Fallback value if loading fails</param>
        /// <returns>The loaded object or fallback value</returns>
        public static T LoadJsonFromPaths<T>(string[] possiblePaths, bool useCache = true, T fallbackValue = default(T)!)
        {
            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    if (GameConfiguration.IsDebugEnabled)
                        UIManager.WriteSystemLine($"DEBUG: JsonLoader found file at: {path}");
                    return LoadJson<T>(path, useCache, fallbackValue);
                }
            }

            // If no file found, log warning and return fallback
            ErrorHandler.LogWarning($"No JSON file found in any of the provided paths: {string.Join(", ", possiblePaths)}", "JsonLoader");
            return fallbackValue;
        }

        /// <summary>
        /// Saves JSON data with error handling
        /// </summary>
        /// <typeparam name="T">The type to serialize</typeparam>
        /// <param name="data">The data to save</param>
        /// <param name="filePath">The path to save to</param>
        /// <param name="updateCache">Whether to update the cache (default: true)</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool SaveJson<T>(T data, string filePath, bool updateCache = true)
        {
            return ErrorHandler.TrySaveJson(() =>
            {
                // Validate parameters
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
                }

                // Ensure directory exists
                string? directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Serialize and save
                string json = JsonSerializer.Serialize(data, _defaultOptions);
                File.WriteAllText(filePath, json);

                // Update cache if requested
                if (updateCache)
                {
                    _cache[filePath] = data!;
                    if (GameConfiguration.IsDebugEnabled)
                        UIManager.WriteSystemLine($"DEBUG: JsonLoader updated cache for {filePath}");
                }
            }, filePath);
        }

        /// <summary>
        /// Clears the JSON cache
        /// Useful for forcing reload of JSON data
        /// </summary>
        public static void ClearCache()
        {
            _cache.Clear();
            if (GameConfiguration.IsDebugEnabled)
                UIManager.WriteSystemLine("DEBUG: JsonLoader cache cleared");
        }

        /// <summary>
        /// Clears cache for a specific file
        /// </summary>
        /// <param name="filePath">The file path to remove from cache</param>
        public static void ClearCacheForFile(string filePath)
        {
            if (_cache.Remove(filePath) && GameConfiguration.IsDebugEnabled)
            {
                UIManager.WriteSystemLine($"DEBUG: JsonLoader cleared cache for {filePath}");
            }
        }

        /// <summary>
        /// Gets cache statistics for debugging
        /// </summary>
        /// <returns>Cache statistics</returns>
        public static (int Count, long EstimatedSize) GetCacheStats()
        {
            int count = _cache.Count;
            long estimatedSize = 0;
            
            // Rough estimation of memory usage
            foreach (var kvp in _cache)
            {
                estimatedSize += kvp.Key.Length * 2; // Unicode characters
                estimatedSize += 100; // Rough estimate for object overhead
            }

            return (count, estimatedSize);
        }

        /// <summary>
        /// Validates JSON file syntax without loading
        /// </summary>
        /// <param name="filePath">The path to the JSON file</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool ValidateJsonSyntax(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return false;
                }

                string json = File.ReadAllText(filePath);
                JsonDocument.Parse(json);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex, "JsonLoader.ValidateJsonSyntax", $"File: {filePath}");
                return false;
            }
        }

        /// <summary>
        /// Gets file information for debugging
        /// </summary>
        /// <param name="filePath">The path to the file</param>
        /// <returns>File information or null if file doesn't exist</returns>
        public static (bool Exists, long Size, DateTime LastModified)? GetFileInfo(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return null;
                }

                var fileInfo = new FileInfo(filePath);
                return (true, fileInfo.Length, fileInfo.LastWriteTime);
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex, "JsonLoader.GetFileInfo", $"File: {filePath}");
                return null;
            }
        }

        /// <summary>
        /// Finds a game data file in possible paths
        /// </summary>
        /// <param name="fileName">The name of the file to find</param>
        /// <returns>The path to the file or null if not found</returns>
        public static string? FindGameDataFile(string fileName)
        {
            var possiblePaths = GameConstants.GetPossibleGameDataFilePaths(fileName);
            
            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }
            
            return null;
        }

        /// <summary>
        /// Loads a JSON list from a file
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="fileName">The name of the file to load</param>
        /// <param name="useCache">Whether to use caching (default: true)</param>
        /// <returns>The loaded list or empty list if loading fails</returns>
        public static List<T> LoadJsonList<T>(string fileName, bool useCache = true)
        {
            var filePath = FindGameDataFile(fileName);
            if (filePath == null)
            {
                ErrorHandler.LogWarning($"File not found: {fileName}", "JsonLoader");
                return new List<T>();
            }
            
            return LoadJson<List<T>>(filePath, useCache, new List<T>());
        }
    }
}