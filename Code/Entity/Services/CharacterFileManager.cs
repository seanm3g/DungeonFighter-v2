using System;
using System.Collections.Generic;
using System.IO;
using RPGGame.Utils;

namespace RPGGame.Entity.Services
{
    /// <summary>
    /// Handles file operations for character save files.
    /// Extracted from CharacterSaveManager to separate file I/O from serialization logic.
    /// </summary>
    public class CharacterFileManager
    {
        /// <summary>
        /// Gets the save filename for a character ID
        /// </summary>
        /// <param name="characterId">The character ID</param>
        /// <returns>The full path to the character's save file</returns>
        public string GetCharacterSaveFilename(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
                throw new ArgumentException("Character ID cannot be null or empty", nameof(characterId));
            
            // Sanitize character ID for filename (remove any invalid characters)
            var sanitizedId = characterId.Replace(" ", "_").Replace("/", "_").Replace("\\", "_");
            var fileName = $"character_{sanitizedId}_save.json";
            return GameConstants.GetGameDataFilePath(fileName);
        }

        /// <summary>
        /// Gets the default save filename (for backward compatibility)
        /// </summary>
        /// <returns>The full path to the default character save file</returns>
        public string GetDefaultSaveFilename()
        {
            return GameConstants.GetGameDataFilePath(GameConstants.CharacterSaveJson);
        }

        /// <summary>
        /// Resolves the filename to use for save/load operations
        /// </summary>
        /// <param name="characterId">Optional character ID</param>
        /// <param name="filename">Optional explicit filename</param>
        /// <returns>The resolved filename</returns>
        public string ResolveFilename(string? characterId = null, string? filename = null)
        {
            if (!string.IsNullOrEmpty(filename))
            {
                return filename;
            }
            
            if (!string.IsNullOrEmpty(characterId))
            {
                return GetCharacterSaveFilename(characterId);
            }
            
            return GetDefaultSaveFilename();
        }

        /// <summary>
        /// Checks if a save file exists
        /// </summary>
        /// <param name="filename">The filename to check</param>
        /// <returns>True if the save file exists</returns>
        public bool FileExists(string filename)
        {
            return File.Exists(filename);
        }

        /// <summary>
        /// Deletes a save file
        /// </summary>
        /// <param name="filename">The filename to delete</param>
        public void DeleteFile(string filename)
        {
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }
        }

        /// <summary>
        /// Writes text to a file
        /// Ensures the directory exists before writing
        /// </summary>
        /// <param name="filename">The filename to write to</param>
        /// <param name="content">The content to write</param>
        public void WriteAllText(string filename, string content)
        {
            // Ensure the directory exists before writing
            var directory = Path.GetDirectoryName(filename);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            File.WriteAllText(filename, content);
        }

        /// <summary>
        /// Reads text from a file asynchronously
        /// </summary>
        /// <param name="filename">The filename to read from</param>
        /// <returns>The file content</returns>
        public System.Threading.Tasks.Task<string> ReadAllTextAsync(string filename)
        {
            return File.ReadAllTextAsync(filename);
        }

        /// <summary>
        /// Reads text from a file synchronously
        /// </summary>
        /// <param name="filename">The filename to read from</param>
        /// <returns>The file content</returns>
        public string ReadAllText(string filename)
        {
            return File.ReadAllText(filename);
        }

        /// <summary>
        /// Gets the GameData directory path
        /// </summary>
        /// <returns>The GameData directory path</returns>
        public string GetGameDataDirectory()
        {
            // Use a known file to get the GameData directory path
            // This ensures we get the correct resolved path
            var gameDataFilePath = GameConstants.GetGameDataFilePath(GameConstants.CharacterSaveJson);
            var directory = Path.GetDirectoryName(gameDataFilePath);
            
            // Normalize the path to resolve any ".." components
            if (!string.IsNullOrEmpty(directory))
            {
                directory = Path.GetFullPath(directory);
            }
            
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                // Fallback: try to find GameData directory using empty string
                var gameDataPath = GameConstants.GetGameDataFilePath("");
                directory = Path.GetDirectoryName(gameDataPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    directory = Path.GetFullPath(directory);
                }
            }
            
            return directory ?? "";
        }

        /// <summary>
        /// Gets all character save files in the GameData directory
        /// Includes both per-character saves (character_*_save.json) and legacy save (character_save.json)
        /// </summary>
        /// <returns>Array of save file paths</returns>
        public string[] GetCharacterSaveFiles()
        {
            var directory = GetGameDataDirectory();
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                // Log for debugging
                ScrollDebugLogger.LogAlways($"GetCharacterSaveFiles: Directory not found or empty. Directory: '{directory}'");
                return Array.Empty<string>();
            }
            
            var files = new List<string>();
            
            try
            {
                // Get per-character save files (character_*_save.json)
                var perCharacterFiles = Directory.GetFiles(directory, "character_*_save.json");
                files.AddRange(perCharacterFiles);
                
                // Also check for legacy save file (character_save.json) if it exists
                var legacyFile = GetDefaultSaveFilename();
                if (File.Exists(legacyFile))
                {
                    files.Add(legacyFile);
                }
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.LogAlways($"GetCharacterSaveFiles: Error searching directory '{directory}': {ex.Message}");
            }
            
            return files.ToArray();
        }
    }
}

