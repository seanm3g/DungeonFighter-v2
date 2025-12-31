using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

        /// <summary>
        /// Initializes a new instance of CharacterSaveService
        /// </summary>
        /// <param name="fileManager">The file manager for file operations</param>
        /// <param name="serializer">The serializer for character data</param>
        public CharacterSaveService(CharacterFileManager? fileManager = null, CharacterSerializer? serializer = null)
        {
            this.fileManager = fileManager ?? new CharacterFileManager();
            this.serializer = serializer ?? new CharacterSerializer();
        }

        /// <summary>
        /// Saves a character to a JSON file
        /// </summary>
        public void SaveCharacter(Character character, string? characterId = null, string? filename = null)
        {
            string? errorMessage = null;
            try
            {
                filename = fileManager.ResolveFilename(characterId, filename);
                
                string json = serializer.Serialize(character);
                
                bool success = ErrorHandler.TryFileOperation(() =>
                {
                    fileManager.WriteAllText(filename, json);
                }, $"SaveCharacter({filename})", () =>
                {
                    errorMessage = "Failed to write file";
                });
                
                if (!success)
                {
                    errorMessage = errorMessage ?? "Failed to save character";
                    HandleSaveError(errorMessage, filename);
                    throw new IOException(errorMessage);
                }
            }
            catch (IOException ex)
            {
                errorMessage = $"I/O error: {ex.Message}";
                HandleSaveError(errorMessage, filename);
                throw; // Re-throw so caller can handle it
            }
            catch (UnauthorizedAccessException ex)
            {
                errorMessage = $"Access denied: {ex.Message}";
                HandleSaveError(errorMessage, filename);
                throw; // Re-throw so caller can handle it
            }
            catch (System.Text.Json.JsonException ex)
            {
                errorMessage = $"JSON serialization error: {ex.Message}";
                HandleSaveError(errorMessage, filename);
                throw; // Re-throw so caller can handle it
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                HandleSaveError(errorMessage, filename);
                throw; // Re-throw so caller can handle it
            }
        }
        
        /// <summary>
        /// Handles save errors by logging and showing messages to the user
        /// </summary>
        private void HandleSaveError(string errorMessage, string? filename)
        {
            string fullMessage = $"Error saving character{(filename != null ? $" to {filename}" : "")}: {errorMessage}";
            
            // Always log to debug logger
            ScrollDebugLogger.LogAlways(fullMessage);
            
            // Show error in console mode
            if (UIManager.GetCustomUIManager() == null)
            {
                UIManager.WriteLine(fullMessage);
            }
            else
            {
                // Show error in custom UI mode
                var customUI = UIManager.GetCustomUIManager();
                if (customUI is CanvasUICoordinator canvasUI)
                {
                    canvasUI.ShowError($"Failed to save character", errorMessage);
                }
                else
                {
                    // Fallback for other UI types
                    UIManager.WriteLine(fullMessage);
                }
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
                    // Only show message in console mode (not in custom UI mode)
                    if (UIManager.GetCustomUIManager() == null)
                    {
                        UIManager.WriteLine($"No save file found at {filename}");
                    }
                    return null;
                }

                string json = await fileManager.ReadAllTextAsync(filename).ConfigureAwait(false);
                var saveData = serializer.Deserialize(json);

                if (saveData == null)
                {
                    // Only show message in console mode (not in custom UI mode)
                    if (UIManager.GetCustomUIManager() == null)
                    {
                        UIManager.WriteLine("Failed to deserialize character data");
                    }
                    return null;
                }

                var character = serializer.CreateCharacterFromSaveData(saveData);

                // Only show load message in console mode (not in custom UI mode)
                if (UIManager.GetCustomUIManager() == null)
                {
                    UIManager.WriteLine($"Character loaded from {filename}");
                }
                return character;
            }
            catch (FileNotFoundException ex)
            {
                // Only show error in console mode (not in custom UI mode)
                if (UIManager.GetCustomUIManager() == null)
                {
                    UIManager.WriteLine($"Error loading character: File not found - {ex.Message}");
                }
                return null;
            }
            catch (IOException ex)
            {
                // Only show error in console mode (not in custom UI mode)
                if (UIManager.GetCustomUIManager() == null)
                {
                    UIManager.WriteLine($"Error loading character: I/O error - {ex.Message}");
                }
                return null;
            }
            catch (UnauthorizedAccessException ex)
            {
                // Only show error in console mode (not in custom UI mode)
                if (UIManager.GetCustomUIManager() == null)
                {
                    UIManager.WriteLine($"Error loading character: Access denied - {ex.Message}");
                }
                return null;
            }
            catch (System.Text.Json.JsonException ex)
            {
                // Only show error in console mode (not in custom UI mode)
                if (UIManager.GetCustomUIManager() == null)
                {
                    UIManager.WriteLine($"Error loading character: JSON deserialization error - {ex.Message}");
                }
                return null;
            }
            catch (Exception ex)
            {
                // Catch-all for any other unexpected errors
                // Only show error in console mode (not in custom UI mode)
                if (UIManager.GetCustomUIManager() == null)
                {
                    UIManager.WriteLine($"Error loading character: {ex.Message}");
                }
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
                // Only show error in console mode (not in custom UI mode)
                if (UIManager.GetCustomUIManager() == null)
                {
                    UIManager.WriteLine($"Error deleting save file: I/O error - {ex.Message}");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                // Only show error in console mode (not in custom UI mode)
                if (UIManager.GetCustomUIManager() == null)
                {
                    UIManager.WriteLine($"Error deleting save file: Access denied - {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                // Catch-all for any other unexpected errors
                // Only show error in console mode (not in custom UI mode)
                if (UIManager.GetCustomUIManager() == null)
                {
                    UIManager.WriteLine($"Error deleting save file: {ex.Message}");
                }
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
                
                // Check for default save file (backward compatibility)
                var defaultFile = fileManager.GetDefaultSaveFilename();
                if (fileManager.FileExists(defaultFile))
                {
                    var lastWriteTime = File.GetLastWriteTime(defaultFile);
                    saveFilesWithTimes.Add((defaultFile, lastWriteTime));
                }
                
                // Check for per-character save files
                var characterSaveFiles = fileManager.GetCharacterSaveFiles();
                foreach (var saveFile in characterSaveFiles)
                {
                    if (fileManager.FileExists(saveFile))
                    {
                        var lastWriteTime = File.GetLastWriteTime(saveFile);
                        saveFilesWithTimes.Add((saveFile, lastWriteTime));
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
            
            try
            {
                // Look for character save files
                var saveFiles = fileManager.GetCharacterSaveFiles();
                
                foreach (var file in saveFiles)
                {
                    try
                    {
                        // Extract character ID from filename
                        var fileName = Path.GetFileName(file);
                        if (fileName.StartsWith("character_") && fileName.EndsWith("_save.json"))
                        {
                            var characterId = fileName.Substring(9, fileName.Length - 18); // Remove "character_" and "_save.json"
                            
                            // Get character info
                            var (name, level) = GetSavedCharacterInfo(file);
                            if (name != null)
                            {
                                results.Add((characterId, name, level));
                            }
                        }
                    }
                    catch
                    {
                        // Skip files that can't be read
                        continue;
                    }
                }
                
                // Also check for legacy single-character save file
                var legacyFile = fileManager.GetDefaultSaveFilename();
                if (fileManager.FileExists(legacyFile))
                {
                    var (name, level) = GetSavedCharacterInfo(legacyFile);
                    if (name != null)
                    {
                        // Generate a character ID for legacy save
                        var legacyId = $"{name}_{level}_legacy";
                        results.Add((legacyId, name, level));
                    }
                }
            }
            catch
            {
                // Return empty list on error
            }
            
            return results;
        }
    }
}

