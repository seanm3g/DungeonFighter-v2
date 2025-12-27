using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using RPGGame.Utils;

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
            try
            {
                filename = fileManager.ResolveFilename(characterId, filename);
                
                string json = serializer.Serialize(character);
                
                ErrorHandler.TryFileOperation(() =>
                {
                    fileManager.WriteAllText(filename, json);
                }, $"SaveCharacter({filename})", () =>
                {
                    // Only show error in console mode (not in custom UI mode)
                    if (UIManager.GetCustomUIManager() == null)
                    {
                        UIManager.WriteLine($"Error saving character: Failed to write file");
                    }
                });
            }
            catch (IOException ex)
            {
                // Only show error in console mode (not in custom UI mode)
                if (UIManager.GetCustomUIManager() == null)
                {
                    UIManager.WriteLine($"Error saving character: I/O error - {ex.Message}");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                // Only show error in console mode (not in custom UI mode)
                if (UIManager.GetCustomUIManager() == null)
                {
                    UIManager.WriteLine($"Error saving character: Access denied - {ex.Message}");
                }
            }
            catch (System.Text.Json.JsonException ex)
            {
                // Only show error in console mode (not in custom UI mode)
                if (UIManager.GetCustomUIManager() == null)
                {
                    UIManager.WriteLine($"Error saving character: JSON serialization error - {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                // Catch-all for any other unexpected errors
                // Only show error in console mode (not in custom UI mode)
                if (UIManager.GetCustomUIManager() == null)
                {
                    UIManager.WriteLine($"Error saving character: {ex.Message}");
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
        /// Returns info from the first valid save file found
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

                    string json = fileManager.ReadAllText(filename);
                    var saveData = serializer.Deserialize(json);
                    
                    return saveData != null ? (saveData.Name, saveData.Level) : (null, 0);
                }
                
                // Check for default save file first (backward compatibility)
                var defaultFile = fileManager.GetDefaultSaveFilename();
                if (fileManager.FileExists(defaultFile))
                {
                    string json = fileManager.ReadAllText(defaultFile);
                    var saveData = serializer.Deserialize(json);
                    
                    if (saveData != null)
                    {
                        return (saveData.Name, saveData.Level);
                    }
                }
                
                // Check for per-character save files
                var characterSaveFiles = fileManager.GetCharacterSaveFiles();
                if (characterSaveFiles.Length > 0)
                {
                    // Use the first character save file found
                    // (Could be enhanced to use most recent file based on modification time)
                    var firstSaveFile = characterSaveFiles[0];
                    string json = fileManager.ReadAllText(firstSaveFile);
                    var saveData = serializer.Deserialize(json);
                    
                    if (saveData != null)
                    {
                        return (saveData.Name, saveData.Level);
                    }
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

