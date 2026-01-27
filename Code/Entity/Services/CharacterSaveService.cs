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
        /// Includes timeout protection to prevent indefinite hangs
        /// </summary>
        public async Task<Character?> LoadCharacterAsync(string? characterId = null, string? filename = null)
        {
            // #region agent log
            try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H1,H2", location = "CharacterSaveService.cs:100", message = "LoadCharacterAsync ENTRY", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), characterId, filename, threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion
            try
            {
                filename = fileManager.ResolveFilename(characterId, filename);
                DebugLogger.Log("CharacterSaveService", $"LoadCharacterAsync: Resolved filename: {filename}");
                // #region agent log
                try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H1", location = "CharacterSaveService.cs:105", message = "Resolved filename", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), resolvedFilename = filename, threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion
                
                if (!fileManager.FileExists(filename))
                {
                    DebugLogger.Log("CharacterSaveService", $"LoadCharacterAsync: File does not exist: {filename}");
                    // Only show message in console mode (not in custom UI mode)
                    if (UIManager.GetCustomUIManager() == null)
                    {
                        UIManager.WriteLine($"No save file found at {filename}");
                    }
                    return null;
                }

                DebugLogger.Log("CharacterSaveService", $"LoadCharacterAsync: File exists, starting read with 3 second timeout: {filename}");
                
                // Add timeout wrapper to prevent indefinite hangs
                // Use 3 seconds timeout for file read operations
                // #region agent log
                try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H1,H2", location = "CharacterSaveService.cs:118", message = "BEFORE ReadAllTextAsync", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), filename, threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion
                var readTask = fileManager.ReadAllTextAsync(filename);
                // #region agent log
                try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H1,H2", location = "CharacterSaveService.cs:120", message = "AFTER ReadAllTextAsync call", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), readTaskStatus = readTask.Status.ToString(), threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(3));
                // #region agent log
                try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H1,H2", location = "CharacterSaveService.cs:122", message = "BEFORE Task.WhenAny", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), readTaskStatus = readTask.Status.ToString(), timeoutTaskStatus = timeoutTask.Status.ToString(), threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion
                var completedTask = await Task.WhenAny(readTask, timeoutTask).ConfigureAwait(false);
                // #region agent log
                try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H1,H2", location = "CharacterSaveService.cs:124", message = "AFTER Task.WhenAny", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), completedTaskIsTimeout = (completedTask == timeoutTask), readTaskStatus = readTask.Status.ToString(), timeoutTaskStatus = timeoutTask.Status.ToString(), threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion

                if (completedTask == timeoutTask)
                {
                    DebugLogger.Log("CharacterSaveService", $"LoadCharacterAsync: File read timed out after 3 seconds: {filename}");
                    // Only show error in console mode (not in custom UI mode)
                    if (UIManager.GetCustomUIManager() == null)
                    {
                        UIManager.WriteLine($"Error loading character: File read operation timed out");
                    }
                    return null;
                }

                string json = await readTask.ConfigureAwait(false);
                // #region agent log
                try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H1", location = "CharacterSaveService.cs:129", message = "File read completed", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), jsonLength = json?.Length ?? 0, threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion
                DebugLogger.Log("CharacterSaveService", $"LoadCharacterAsync: File read completed, size: {json?.Length ?? 0} bytes");
                
                if (string.IsNullOrEmpty(json))
                {
                    DebugLogger.Log("CharacterSaveService", "LoadCharacterAsync: JSON string is null or empty");
                    // Only show message in console mode (not in custom UI mode)
                    if (UIManager.GetCustomUIManager() == null)
                    {
                        UIManager.WriteLine("Failed to deserialize character data");
                    }
                    return null;
                }
                
                var saveData = serializer.Deserialize(json);

                if (saveData == null)
                {
                    DebugLogger.Log("CharacterSaveService", "LoadCharacterAsync: Deserialization returned null");
                    // Only show message in console mode (not in custom UI mode)
                    if (UIManager.GetCustomUIManager() == null)
                    {
                        UIManager.WriteLine("Failed to deserialize character data");
                    }
                    return null;
                }

                DebugLogger.Log("CharacterSaveService", $"LoadCharacterAsync: Deserialization successful, creating character: {saveData.Name}");
                var character = serializer.CreateCharacterFromSaveData(saveData);
                DebugLogger.Log("CharacterSaveService", $"LoadCharacterAsync: Character created successfully: {character?.Name}");

                // Only show load message in console mode (not in custom UI mode)
                if (UIManager.GetCustomUIManager() == null)
                {
                    UIManager.WriteLine($"Character loaded from {filename}");
                }
                return character;
            }
            catch (FileNotFoundException ex)
            {
                DebugLogger.Log("CharacterSaveService", $"LoadCharacterAsync: FileNotFoundException: {ex.Message}");
                // Only show error in console mode (not in custom UI mode)
                if (UIManager.GetCustomUIManager() == null)
                {
                    UIManager.WriteLine($"Error loading character: File not found - {ex.Message}");
                }
                return null;
            }
            catch (IOException ex)
            {
                DebugLogger.Log("CharacterSaveService", $"LoadCharacterAsync: IOException: {ex.Message}");
                // Only show error in console mode (not in custom UI mode)
                if (UIManager.GetCustomUIManager() == null)
                {
                    UIManager.WriteLine($"Error loading character: I/O error - {ex.Message}");
                }
                return null;
            }
            catch (UnauthorizedAccessException ex)
            {
                DebugLogger.Log("CharacterSaveService", $"LoadCharacterAsync: UnauthorizedAccessException: {ex.Message}");
                // Only show error in console mode (not in custom UI mode)
                if (UIManager.GetCustomUIManager() == null)
                {
                    UIManager.WriteLine($"Error loading character: Access denied - {ex.Message}");
                }
                return null;
            }
            catch (System.Text.Json.JsonException ex)
            {
                DebugLogger.Log("CharacterSaveService", $"LoadCharacterAsync: JsonException: {ex.Message}");
                // Only show error in console mode (not in custom UI mode)
                if (UIManager.GetCustomUIManager() == null)
                {
                    UIManager.WriteLine($"Error loading character: JSON deserialization error - {ex.Message}");
                }
                return null;
            }
            catch (TaskCanceledException ex)
            {
                DebugLogger.Log("CharacterSaveService", $"LoadCharacterAsync: TaskCanceledException (timeout): {ex.Message}");
                // Only show error in console mode (not in custom UI mode)
                if (UIManager.GetCustomUIManager() == null)
                {
                    UIManager.WriteLine($"Error loading character: File read operation timed out");
                }
                return null;
            }
            catch (Exception ex)
            {
                // Catch-all for any other unexpected errors
                DebugLogger.Log("CharacterSaveService", $"LoadCharacterAsync: Unexpected exception: {ex.Message}\n{ex.StackTrace}");
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
        /// Returns characters sorted by modification time (most recent first)
        /// </summary>
        public List<(string characterId, string characterName, int level)> ListAllSavedCharacters()
        {
            var resultsWithFiles = new List<(string characterId, string characterName, int level, string filePath, DateTime lastWriteTime)>();
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
                        
                        // Get file modification time for sorting
                        DateTime lastWriteTime = File.Exists(file) ? File.GetLastWriteTime(file) : DateTime.MinValue;
                        
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
                        
                        resultsWithFiles.Add((characterId, name, level, file, lastWriteTime));
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
            
            // Sort by modification time (most recent first)
            resultsWithFiles.Sort((a, b) => b.lastWriteTime.CompareTo(a.lastWriteTime));
            
            // Extract just the character info for return
            var results = resultsWithFiles.Select(r => (r.characterId, r.characterName, r.level)).ToList();
            
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
                ScrollDebugLogger.LogAlways($"ClearAllSavedCharacters: Found {saveFiles.Length} save file(s) to process");
                
                foreach (var file in saveFiles)
                {
                    try
                    {
                        // Normalize the path to ensure consistent file operations
                        string normalizedPath = Path.GetFullPath(file);
                        
                        // Check if file exists (try both normalized and original paths)
                        bool fileExists = fileManager.FileExists(normalizedPath) || fileManager.FileExists(file);
                        
                        if (fileExists)
                        {
                            // Try deleting with normalized path first, then fallback to original
                            bool deleted = false;
                            if (fileManager.FileExists(normalizedPath))
                            {
                                fileManager.DeleteFile(normalizedPath);
                                deleted = true;
                            }
                            else if (fileManager.FileExists(file))
                            {
                                fileManager.DeleteFile(file);
                                deleted = true;
                            }
                            
                            if (deleted)
                            {
                                deletedCount++;
                                ScrollDebugLogger.LogAlways($"Deleted save file: {normalizedPath}");
                            }
                        }
                        else
                        {
                            ScrollDebugLogger.LogAlways($"Save file not found (skipped): {file}");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue deleting other files
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

