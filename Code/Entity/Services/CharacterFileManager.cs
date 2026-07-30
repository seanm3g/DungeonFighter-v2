using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RPGGame.Utils;

namespace RPGGame.Entity.Services
{
    /// <summary>
    /// Handles file operations for character save files.
    /// Extracted from CharacterSaveManager to separate file I/O from serialization logic.
    /// </summary>
    public class CharacterFileManager
    {
        private static readonly object WriteLock = new object();

        /// <summary>
        /// Gets the save filename for a character ID
        /// </summary>
        /// <param name="characterId">The character ID</param>
        /// <returns>The full path to the character's save file</returns>
        public string GetCharacterSaveFilename(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
                throw new ArgumentException("Character ID cannot be null or empty", nameof(characterId));

            var sanitizedId = SanitizeForFilename(characterId);
            var fileName = $"character_{sanitizedId}_save.json";
            return GameConstants.GetGameDataFilePath(fileName);
        }

        /// <summary>Non-loadable tombstone path for a per-character save after the hero dies.</summary>
        public string GetCharacterDeadFilename(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
                throw new ArgumentException("Character ID cannot be null or empty", nameof(characterId));

            var sanitizedId = SanitizeForFilename(characterId);
            var fileName = $"character_{sanitizedId}_dead.json";
            return GameConstants.GetGameDataFilePath(fileName);
        }

        /// <summary>
        /// Replaces characters that are illegal in Windows/macOS/Linux filenames.
        /// </summary>
        public static string SanitizeForFilename(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
                return characterId;

            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(characterId.Length);
            foreach (char c in characterId)
            {
                if (c == ' ' || c == '/' || c == '\\' || Array.IndexOf(invalid, c) >= 0)
                    sb.Append('_');
                else
                    sb.Append(c);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets the default save filename (for backward compatibility)
        /// </summary>
        /// <returns>The full path to the default character save file</returns>
        public string GetDefaultSaveFilename()
        {
            return GameConstants.GetGameDataFilePath(GameConstants.CharacterSaveJson);
        }

        public string GetDefaultDeadSaveFilename()
        {
            return GameConstants.GetGameDataFilePath(GameConstants.CharacterSaveDeadJson);
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
            if (string.IsNullOrEmpty(filename))
                return;

            try
            {
                // Normalize the path to ensure consistent file operations
                string normalizedPath = Path.GetFullPath(filename);

                if (File.Exists(normalizedPath))
                {
                    File.Delete(normalizedPath);
                }

                // Clean companion atomic-write artifacts when present
                TryDeleteQuiet(normalizedPath + ".bak");
                TryDeleteQuiet(normalizedPath + ".tmp");
            }
            catch (Exception ex)
            {
                // Re-throw with context for better error handling
                throw new IOException($"Failed to delete file '{filename}': {ex.Message}", ex);
            }
        }

        private static void TryDeleteQuiet(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // Best-effort companion cleanup
            }
        }

        /// <summary>
        /// Writes text to a file atomically (temp + replace) so a crash cannot leave truncated JSON.
        /// Ensures the directory exists before writing.
        /// </summary>
        /// <param name="filename">The filename to write to</param>
        /// <param name="content">The content to write</param>
        public void WriteAllText(string filename, string content)
        {
            lock (WriteLock)
            {
                WriteAtomic(filename, content);
            }
        }

        /// <summary>
        /// Writes text to a file asynchronously (non-blocking for UI exit paths).
        /// Uses the same atomic temp+replace strategy as the sync path.
        /// </summary>
        public async Task WriteAllTextAsync(
            string filename,
            string content,
            CancellationToken cancellationToken = default)
        {
            // Serialize off the UI thread; lock keeps sync/async writers from interleaving.
            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                lock (WriteLock)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    WriteAtomic(filename, content);
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        private static void WriteAtomic(string filename, string content)
        {
            var directory = Path.GetDirectoryName(filename);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string temp = filename + ".tmp";
            string backup = filename + ".bak";
            File.WriteAllText(temp, content);
            if (File.Exists(filename))
                File.Replace(temp, filename, backup);
            else
                File.Move(temp, filename);
        }

        /// <summary>
        /// Reads text from a file asynchronously
        /// </summary>
        /// <param name="filename">The filename to read from</param>
        /// <returns>The file content</returns>
        public Task<string> ReadAllTextAsync(
            string filename,
            CancellationToken cancellationToken = default)
        {
            return File.ReadAllTextAsync(filename, cancellationToken);
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

                // Verify the directory exists
                if (Directory.Exists(directory))
                {
                    return directory;
                }
            }

            // Fallback: try to find GameData directory using empty string
            var gameDataPath = GameConstants.GetGameDataFilePath("");
            directory = Path.GetDirectoryName(gameDataPath);
            if (!string.IsNullOrEmpty(directory))
            {
                directory = Path.GetFullPath(directory);
                if (Directory.Exists(directory))
                {
                    return directory;
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
