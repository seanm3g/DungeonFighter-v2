using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RPGGame;
using RPGGame.Utils;
using RPGGame.UI;
using RPGGame.UI.Avalonia;

namespace RPGGame.Entity.Services
{
    /// <summary>
    /// Service implementation for character save and load operations.
    /// Uses CharacterFileManager for file operations and CharacterSerializer for serialization.
    /// </summary>
    public class CharacterSaveService : ICharacterSaveService
    {
        private readonly CharacterFileManager fileManager;
        private readonly CharacterSerializer serializer;
        private readonly ICharacterSaveErrorReporter errorReporter;

        public CharacterSaveService(
            CharacterFileManager? fileManager = null,
            CharacterSerializer? serializer = null,
            ICharacterSaveErrorReporter? errorReporter = null)
        {
            this.fileManager = fileManager ?? new CharacterFileManager();
            this.serializer = serializer ?? new CharacterSerializer();
            this.errorReporter = errorReporter ?? new CharacterSaveErrorReporter();
        }

        /// <summary>
        /// Saves a character to a JSON file
        /// </summary>
        public void SaveCharacter(Character character, string? characterId = null, string? filename = null)
        {
            if (character == null)
                throw new ArgumentNullException(nameof(character));

            string? errorMessage = null;
            try
            {
                filename = fileManager.ResolveFilename(characterId, filename);
                if (filename == null)
                    throw new InvalidOperationException("Failed to resolve filename for character save");

                string resolvedFilename = filename;
                string json = serializer.Serialize(character);

                bool success = ErrorHandler.TryFileOperation(() =>
                {
                    fileManager.WriteAllText(resolvedFilename, json);
                }, $"SaveCharacter({filename})", () =>
                {
                    errorMessage = "Failed to write file";
                });

                if (!success)
                {
                    errorMessage = errorMessage ?? "Failed to save character";
                    errorReporter.ReportSaveError("Failed to save character", errorMessage, filename);
                    throw new IOException(errorMessage);
                }
            }
            catch (IOException ex)
            {
                errorMessage = $"I/O error: {ex.Message}";
                errorReporter.ReportSaveError("Failed to save character", errorMessage, filename);
                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                errorMessage = $"Access denied: {ex.Message}";
                errorReporter.ReportSaveError("Failed to save character", errorMessage, filename);
                throw;
            }
            catch (System.Text.Json.JsonException ex)
            {
                errorMessage = $"JSON serialization error: {ex.Message}";
                errorReporter.ReportSaveError("Failed to save character", errorMessage, filename);
                throw;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                errorReporter.ReportSaveError("Failed to save character", errorMessage, filename);
                throw;
            }
        }

        /// <summary>
        /// Loads a character from a JSON file (async version to prevent UI freezing).
        /// Includes timeout protection to prevent indefinite hangs.
        /// </summary>
        public async Task<Character?> LoadCharacterAsync(string? characterId = null, string? filename = null)
        {
            try
            {
                filename = fileManager.ResolveFilename(characterId, filename);
                DebugLogger.Log("CharacterSaveService", $"LoadCharacterAsync: Resolved filename: {filename}");

                if (!fileManager.FileExists(filename))
                {
                    DebugLogger.Log("CharacterSaveService", $"LoadCharacterAsync: File does not exist: {filename}");
                    ReportLoadMessage($"No save file found at {filename}");
                    return null;
                }

                DebugLogger.Log("CharacterSaveService", $"LoadCharacterAsync: File exists, starting read with 3 second timeout: {filename}");

                var readTask = fileManager.ReadAllTextAsync(filename);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(3));
                var completedTask = await Task.WhenAny(readTask, timeoutTask).ConfigureAwait(false);

                if (completedTask == timeoutTask)
                {
                    DebugLogger.Log("CharacterSaveService", $"LoadCharacterAsync: File read timed out after 3 seconds: {filename}");
                    errorReporter.ReportLoadError("File read operation timed out");
                    return null;
                }

                string json = await readTask.ConfigureAwait(false);
                DebugLogger.Log("CharacterSaveService", $"LoadCharacterAsync: File read completed, size: {json?.Length ?? 0} bytes");

                if (string.IsNullOrEmpty(json))
                {
                    DebugLogger.Log("CharacterSaveService", "LoadCharacterAsync: JSON string is null or empty");
                    errorReporter.ReportLoadError("Failed to deserialize character data");
                    return null;
                }

                var saveData = serializer.Deserialize(json);
                if (saveData == null)
                {
                    DebugLogger.Log("CharacterSaveService", "LoadCharacterAsync: Deserialization returned null");
                    errorReporter.ReportLoadError("Failed to deserialize character data");
                    return null;
                }

                DebugLogger.Log("CharacterSaveService", $"LoadCharacterAsync: Deserialization successful, creating character: {saveData.Name}");
                var character = serializer.CreateCharacterFromSaveData(saveData);
                DebugLogger.Log("CharacterSaveService", $"LoadCharacterAsync: Character created successfully: {character?.Name}");

                if (UIManager.GetCustomUIManager() == null)
                    UIManager.WriteLine($"Character loaded from {filename}");
                return character;
            }
            catch (Exception ex)
            {
                HandleLoadException(ex);
                return null;
            }
        }

        private void ReportLoadMessage(string message)
        {
            if (UIManager.GetCustomUIManager() == null)
                UIManager.WriteLine(message);
            else
                errorReporter.ReportLoadError(message);
        }

        private void HandleLoadException(Exception ex)
        {
            string message = ex switch
            {
                FileNotFoundException f => $"File not found - {f.Message}",
                IOException _ => $"I/O error - {ex.Message}",
                UnauthorizedAccessException _ => "Access denied - " + ex.Message,
                System.Text.Json.JsonException _ => "JSON deserialization error - " + ex.Message,
                TaskCanceledException _ => "File read operation timed out",
                _ => ex.Message
            };
            DebugLogger.Log("CharacterSaveService", $"LoadCharacterAsync: {ex.GetType().Name}: {ex.Message}");
            errorReporter.ReportLoadError(message);
        }

        /// <summary>
        /// Deletes a character save file
        /// </summary>
        public void DeleteSaveFile(string? filename = null)
        {
            try
            {
                if (string.IsNullOrEmpty(filename))
                    filename = fileManager.GetDefaultSaveFilename();
                fileManager.DeleteFile(filename);
            }
            catch (FileNotFoundException) { /* File doesn't exist - fine */ }
            catch (IOException ex) { errorReporter.ReportDeleteError($"I/O error: {ex.Message}"); }
            catch (UnauthorizedAccessException ex) { errorReporter.ReportDeleteError($"Access denied: {ex.Message}"); }
            catch (Exception ex) { errorReporter.ReportDeleteError(ex.Message); }
        }

        /// <summary>
        /// Checks if a save file exists
        /// </summary>
        public bool SaveFileExists(string? filename = null)
        {
            if (!string.IsNullOrEmpty(filename))
                return fileManager.FileExists(filename);
            if (fileManager.FileExists(fileManager.GetDefaultSaveFilename()))
                return true;
            return fileManager.GetCharacterSaveFiles().Length > 0;
        }

        /// <summary>
        /// Returns save files with their modification times (for resolution and sorting).
        /// </summary>
        private List<(string file, DateTime lastWriteTime)> GetSaveFilesWithTimestamps()
        {
            var list = new List<(string file, DateTime lastWriteTime)>();
            foreach (var saveFile in fileManager.GetCharacterSaveFiles())
            {
                if (fileManager.FileExists(saveFile))
                    list.Add((saveFile, File.GetLastWriteTime(saveFile)));
            }
            if (list.Count == 0)
            {
                var defaultFile = fileManager.GetDefaultSaveFilename();
                if (fileManager.FileExists(defaultFile))
                    list.Add((defaultFile, File.GetLastWriteTime(defaultFile)));
            }
            else
            {
                var defaultFile = fileManager.GetDefaultSaveFilename();
                if (fileManager.FileExists(defaultFile))
                {
                    try
                    {
                        string defaultJson = fileManager.ReadAllText(defaultFile);
                        var defaultSaveData = serializer.Deserialize(defaultJson);
                        if (defaultSaveData != null &&
                            !string.Equals(defaultSaveData.Name, "TestCharacter", StringComparison.OrdinalIgnoreCase))
                            list.Add((defaultFile, File.GetLastWriteTime(defaultFile)));
                    }
                    catch { /* skip */ }
                }
            }
            return list;
        }

        /// <summary>
        /// Gets information about a saved character without loading it
        /// </summary>
        public (string? characterName, int level) GetSavedCharacterInfo(string? filename = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(filename))
                {
                    if (!fileManager.FileExists(filename))
                        return (null, 0);
                    string fileJson = fileManager.ReadAllText(filename);
                    var fileSaveData = serializer.Deserialize(fileJson);
                    return fileSaveData != null ? (fileSaveData.Name, fileSaveData.Level) : (null, 0);
                }

                var saveFilesWithTimes = GetSaveFilesWithTimestamps();
                if (saveFilesWithTimes.Count == 0)
                    return (null, 0);

                var mostRecent = saveFilesWithTimes.OrderByDescending(x => x.lastWriteTime).First().file;
                string json = fileManager.ReadAllText(mostRecent);
                var saveData = serializer.Deserialize(json);
                return saveData != null ? (saveData.Name, saveData.Level) : (null, 0);
            }
            catch
            {
                return (null, 0);
            }
        }

        public string GetCharacterSaveFilename(string characterId)
        {
            return fileManager.GetCharacterSaveFilename(characterId);
        }

        /// <summary>
        /// Lists all saved characters in the GameData directory (most recent first)
        /// </summary>
        public List<(string characterId, string characterName, int level)> ListAllSavedCharacters()
        {
            var resultsWithFiles = new List<(string characterId, string characterName, int level, string filePath, DateTime lastWriteTime)>();
            var processedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var saveFiles = fileManager.GetCharacterSaveFiles();
                foreach (var file in saveFiles)
                {
                    try
                    {
                        if (processedFiles.Contains(file)) continue;
                        processedFiles.Add(file);

                        DateTime lastWriteTime = File.Exists(file) ? File.GetLastWriteTime(file) : DateTime.MinValue;
                        var (name, level) = GetSavedCharacterInfo(file);
                        if (name == null) continue;

                        string characterId = ExtractCharacterIdFromFilePath(file, name, level);
                        resultsWithFiles.Add((characterId, name, level, file, lastWriteTime));
                    }
                    catch (Exception ex)
                    {
                        ScrollDebugLogger.LogAlways($"Error reading save file {file}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.LogAlways($"Error listing saved characters: {ex.Message}");
            }

            resultsWithFiles.Sort((a, b) => b.lastWriteTime.CompareTo(a.lastWriteTime));
            return resultsWithFiles.Select(r => (r.characterId, r.characterName, r.level)).ToList();
        }

        private static string ExtractCharacterIdFromFilePath(string file, string name, int level)
        {
            var fileName = Path.GetFileName(file);
            if (string.Equals(fileName, GameConstants.CharacterSaveJson, StringComparison.OrdinalIgnoreCase))
                return $"{name}_{level}_legacy";
            if (fileName.StartsWith("character_") && fileName.EndsWith("_save.json"))
            {
                int idLength = fileName.Length - 20;
                return idLength > 0 ? fileName.Substring(9, idLength) : $"{name}_{level}";
            }
            return $"{name}_{level}";
        }

        /// <summary>
        /// Clears all saved characters by deleting all save files
        /// </summary>
        public int ClearAllSavedCharacters()
        {
            int deletedCount = 0;
            try
            {
                var saveFiles = fileManager.GetCharacterSaveFiles();
                ScrollDebugLogger.LogAlways($"ClearAllSavedCharacters: Found {saveFiles.Length} save file(s) to process");

                foreach (var file in saveFiles)
                {
                    try
                    {
                        string normalizedPath = Path.GetFullPath(file);
                        bool fileExists = fileManager.FileExists(normalizedPath) || fileManager.FileExists(file);
                        if (!fileExists)
                        {
                            ScrollDebugLogger.LogAlways($"Save file not found (skipped): {file}");
                            continue;
                        }
                        if (fileManager.FileExists(normalizedPath))
                            fileManager.DeleteFile(normalizedPath);
                        else if (fileManager.FileExists(file))
                            fileManager.DeleteFile(file);
                        else
                            continue;
                        deletedCount++;
                        ScrollDebugLogger.LogAlways($"Deleted save file: {normalizedPath}");
                    }
                    catch (Exception ex)
                    {
                        ScrollDebugLogger.LogAlways($"Error deleting save file {file}: {ex.Message}");
                    }
                }
                ScrollDebugLogger.LogAlways($"ClearAllSavedCharacters: Successfully deleted {deletedCount} save file(s)");
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.LogAlways($"Error clearing saved characters: {ex.Message}");
            }
            return deletedCount;
        }
    }
}
