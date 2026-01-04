using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using RPGGame;
using RPGGame.Utils;
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
        private readonly CharacterSaveErrorHandler errorHandler;

        /// <summary>
        /// Initializes a new instance of CharacterSaveService
        /// </summary>
        /// <param name="fileManager">The file manager for file operations</param>
        /// <param name="serializer">The serializer for character data</param>
        public CharacterSaveService(CharacterFileManager? fileManager = null, CharacterSerializer? serializer = null, CharacterSaveErrorHandler? errorHandler = null)
        {
            this.fileManager = fileManager ?? new CharacterFileManager();
            this.serializer = serializer ?? new CharacterSerializer();
            this.errorHandler = errorHandler ?? new CharacterSaveErrorHandler();
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
                
                // filename is guaranteed to be non-null after ResolveFilename and null check above
                string resolvedFilename = filename!;
                // character is guaranteed to be non-null after null check at method start
                string json = serializer.Serialize(character!);
                
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
                    errorHandler.HandleSaveError(errorMessage, filename);
                    throw new IOException(errorMessage);
                }
            }
            catch (IOException ex)
            {
                errorMessage = $"I/O error: {ex.Message}";
                errorHandler.HandleSaveError(errorMessage, filename);
                throw; // Re-throw so caller can handle it
            }
            catch (UnauthorizedAccessException ex)
            {
                errorMessage = $"Access denied: {ex.Message}";
                errorHandler.HandleSaveError(errorMessage, filename);
                throw; // Re-throw so caller can handle it
            }
            catch (System.Text.Json.JsonException ex)
            {
                errorMessage = $"JSON serialization error: {ex.Message}";
                errorHandler.HandleSaveError(errorMessage, filename);
                throw; // Re-throw so caller can handle it
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                errorHandler.HandleSaveError(errorMessage, filename);
                throw; // Re-throw so caller can handle it
            }
        }

        /// <summary>
        /// Loads a character from a JSON file (async version to prevent UI freezing)
        /// </summary>
        public async Task<Character?> LoadCharacterAsync(string? characterId = null, string? filename = null)
        {
            try
            {
                filename = fileManager.ResolveFilename(characterId, filename);
                
                if (!fileManager.FileExists(filename))
                {
                    errorHandler.HandleFileNotFoundError(filename);
                    return null;
                }

                string json = await fileManager.ReadAllTextAsync(filename).ConfigureAwait(false);
                var saveData = serializer.Deserialize(json);

                if (saveData == null)
                {
                    errorHandler.HandleDeserializationError();
                    return null;
                }

                var character = serializer.CreateCharacterFromSaveData(saveData);
                errorHandler.ShowLoadSuccess(filename);
                return character;
            }
            catch (FileNotFoundException ex)
            {
                errorHandler.HandleGenericError("loading character", $"File not found - {ex.Message}");
                return null;
            }
            catch (IOException ex)
            {
                errorHandler.HandleIOError("loading character", ex.Message);
                return null;
            }
            catch (UnauthorizedAccessException ex)
            {
                errorHandler.HandleAccessDeniedError("loading character", ex.Message);
                return null;
            }
            catch (System.Text.Json.JsonException ex)
            {
                errorHandler.HandleJsonError("loading character", ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                // Catch-all for any other unexpected errors
                errorHandler.HandleGenericError("loading character", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Loads a character from a JSON file (synchronous version for backward compatibility)
        /// </summary>
        [Obsolete("Use LoadCharacterAsync instead. This method blocks the calling thread and may freeze the UI.")]
        public Character? LoadCharacter(string? characterId = null, string? filename = null)
        {
            // For backward compatibility only - callers should migrate to async version
            // Using ConfigureAwait(false) to avoid deadlocks, but this still blocks
            return LoadCharacterAsync(characterId, filename).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Deletes a character save file
        /// </summary>
        public void DeleteSaveFile(string? filename = null)
        {
            try
            {
                if (string.IsNullOrEmpty(filename))
                {
                    filename = fileManager.GetDefaultSaveFilename();
                }
                
                fileManager.DeleteFile(filename);
            }
            catch (FileNotFoundException)
            {
                // File doesn't exist - this is fine, nothing to delete
                // Silently ignore
            }
            catch (IOException ex)
            {
                errorHandler.HandleIOError("deleting save file", ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                errorHandler.HandleAccessDeniedError("deleting save file", ex.Message);
            }
            catch (Exception ex)
            {
                // Catch-all for any other unexpected errors
                errorHandler.HandleGenericError("deleting save file", ex.Message);
            }
        }

        /// <summary>
        /// Checks if a save file exists
        /// Checks both the default save file and per-character save files
        /// </summary>
        public bool SaveFileExists(string? filename = null)
        {
            // If a specific filename is provided, check only that file
            if (!string.IsNullOrEmpty(filename))
            {
                return fileManager.FileExists(filename);
            }
            
            // Check for default save file (backward compatibility)
            var defaultFile = fileManager.GetDefaultSaveFilename();
            if (fileManager.FileExists(defaultFile))
            {
                return true;
            }
            
            // Check for any per-character save files
            var characterSaveFiles = fileManager.GetCharacterSaveFiles();
            return characterSaveFiles.Length > 0;
        }

        /// <summary>
        /// Gets information about a saved character without loading it
        /// Checks both the default save file and per-character save files
        /// Returns info from the most recently modified save file (last played character)
        /// Prioritizes per-character save files over the legacy default file
        /// </summary>
        public (string? characterName, int level) GetSavedCharacterInfo(string? filename = null)
        {
            try
            {
                // If a specific filename is provided, check only that file
                if (!string.IsNullOrEmpty(filename))
                {
                    if (!fileManager.FileExists(filename))
                        return (null, 0);

                    string fileJson = fileManager.ReadAllText(filename);
                    var fileSaveData = serializer.Deserialize(fileJson);
                    
                    return fileSaveData != null ? (fileSaveData.Name, fileSaveData.Level) : (null, 0);
                }
                
                // Collect all potential save files with their modification times
                var saveFilesWithTimes = new List<(string file, DateTime lastWriteTime)>();
                
                // First, check for per-character save files (prioritize these)
                var characterSaveFiles = fileManager.GetCharacterSaveFiles();
                foreach (var saveFile in characterSaveFiles)
                {
                    if (fileManager.FileExists(saveFile))
                    {
                        var lastWriteTime = File.GetLastWriteTime(saveFile);
                        saveFilesWithTimes.Add((saveFile, lastWriteTime));
                    }
                }
                
                // Only check default save file if no per-character files exist
                // This prevents test/default files from being shown when real characters exist
                if (saveFilesWithTimes.Count == 0)
                {
                    var defaultFile = fileManager.GetDefaultSaveFilename();
                    if (fileManager.FileExists(defaultFile))
                    {
                        var lastWriteTime = File.GetLastWriteTime(defaultFile);
                        saveFilesWithTimes.Add((defaultFile, lastWriteTime));
                    }
                }
                else
                {
                    // If we have per-character files, check the default file but exclude it if it's a test character
                    var defaultFile = fileManager.GetDefaultSaveFilename();
                    if (fileManager.FileExists(defaultFile))
                    {
                        try
                        {
                            string defaultJson = fileManager.ReadAllText(defaultFile);
                            var defaultSaveData = serializer.Deserialize(defaultJson);
                            
                            // Only include default file if it's not a test character
                            if (defaultSaveData != null && 
                                !string.Equals(defaultSaveData.Name, "TestCharacter", StringComparison.OrdinalIgnoreCase))
                            {
                                var lastWriteTime = File.GetLastWriteTime(defaultFile);
                                saveFilesWithTimes.Add((defaultFile, lastWriteTime));
                            }
                        }
                        catch
                        {
                            // If we can't read the default file, skip it
                        }
                    }
                }
                
                // If no save files found, return empty
                if (saveFilesWithTimes.Count == 0)
                {
                    return (null, 0);
                }
                
                // Sort by modification time (most recent first) and use the most recent one
                var mostRecentSaveFile = saveFilesWithTimes
                    .OrderByDescending(x => x.lastWriteTime)
                    .First().file;
                
                string json = fileManager.ReadAllText(mostRecentSaveFile);
                var saveData = serializer.Deserialize(json);
                
                if (saveData != null)
                {
                    return (saveData.Name, saveData.Level);
                }
                
                return (null, 0);
            }
            catch
            {
                return (null, 0);
            }
        }

        /// <summary>
        /// Gets the save filename for a character ID
        /// </summary>
        public string GetCharacterSaveFilename(string characterId)
        {
            return fileManager.GetCharacterSaveFilename(characterId);
        }

        /// <summary>
        /// Lists all saved characters in the GameData directory
        /// </summary>
        public List<(string characterId, string characterName, int level)> ListAllSavedCharacters()
        {
            var results = new List<(string, string, int)>();
            var processedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            try
            {
                // Look for character save files (includes both per-character and legacy files)
                var saveFiles = fileManager.GetCharacterSaveFiles();
                
                foreach (var file in saveFiles)
                {
                    try
                    {
                        // Skip if we've already processed this file
                        if (processedFiles.Contains(file))
                        {
                            continue;
                        }
                        processedFiles.Add(file);
                        
                        // Get character info
                        var (name, level) = GetSavedCharacterInfo(file);
                        if (name == null)
                        {
                            continue;
                        }
                        
                        // Extract character ID from filename
                        var fileName = Path.GetFileName(file);
                        string characterId;
                        
                        // Check if this is the legacy save file (character_save.json)
                        if (string.Equals(fileName, GameConstants.CharacterSaveJson, StringComparison.OrdinalIgnoreCase))
                        {
                            // Generate a character ID for legacy save
                            characterId = $"{name}_{level}_legacy";
                        }
                        else if (fileName.StartsWith("character_") && fileName.EndsWith("_save.json"))
                        {
                            // Extract character ID from per-character save file
                            // Remove "character_" (9 chars) and "_save.json" (11 chars)
                            var idLength = fileName.Length - 20;
                            if (idLength > 0)
                            {
                                characterId = fileName.Substring(9, idLength);
                            }
                            else
                            {
                                // Fallback if extraction fails
                                characterId = $"{name}_{level}";
                            }
                        }
                        else
                        {
                            // Unknown format, generate ID from name and level
                            characterId = $"{name}_{level}";
                        }
                        
                        results.Add((characterId, name, level));
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue processing other files
                        ScrollDebugLogger.LogAlways($"Error reading save file {file}: {ex.Message}");
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but return what we've found so far
                ScrollDebugLogger.LogAlways($"Error listing saved characters: {ex.Message}");
            }
            
            return results;
        }

        /// <summary>
        /// Clears all saved characters by deleting all save files
        /// </summary>
        /// <returns>The number of save files deleted</returns>
        public int ClearAllSavedCharacters()
        {
            int deletedCount = 0;
            
            try
            {
                var saveFiles = fileManager.GetCharacterSaveFiles();
                
                foreach (var file in saveFiles)
                {
                    try
                    {
                        if (fileManager.FileExists(file))
                        {
                            fileManager.DeleteFile(file);
                            deletedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue deleting other files
                        ScrollDebugLogger.LogAlways($"Error deleting save file {file}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.LogAlways($"Error clearing saved characters: {ex.Message}");
            }
            
            return deletedCount;
        }
    }
}

