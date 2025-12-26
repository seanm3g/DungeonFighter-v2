using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RPGGame
{
    /// <summary>
    /// Handles character save and load operations
    /// Extracted from Character.cs to reduce complexity
    /// </summary>
    public static class CharacterSaveManager
    {
        /// <summary>
        /// Saves a character to a JSON file
        /// </summary>
        /// <param name="character">The character to save</param>
        /// <param name="characterId">Optional character ID for multi-character support. If provided, generates per-character filename.</param>
        /// <param name="filename">The filename to save to. If provided, overrides characterId-based naming.</param>
        public static void SaveCharacter(Character character, string? characterId = null, string? filename = null)
        {
            try
            {
                // Use proper path resolution if no filename provided
                if (string.IsNullOrEmpty(filename))
                {
                    if (!string.IsNullOrEmpty(characterId))
                    {
                        // Multi-character support: use per-character filename
                        filename = GetCharacterSaveFilename(characterId);
                    }
                    else
                    {
                        // Backward compatibility: use default filename
                        filename = GameConstants.GetGameDataFilePath(GameConstants.CharacterSaveJson);
                    }
                }
                var saveData = new CharacterSaveData
                {
                    Name = character.Name,
                    Level = character.Level,
                    XP = character.Progression.XP,
                    CurrentHealth = character.CurrentHealth,
                    MaxHealth = character.MaxHealth,
                    Strength = character.Stats.Strength,
                    Agility = character.Stats.Agility,
                    Technique = character.Stats.Technique,
                    Intelligence = character.Stats.Intelligence,
                    BarbarianPoints = character.Progression.BarbarianPoints,
                    WarriorPoints = character.Progression.WarriorPoints,
                    RoguePoints = character.Progression.RoguePoints,
                    WizardPoints = character.Progression.WizardPoints,
                    ComboStep = character.Effects.ComboStep,
                    ComboBonus = character.Effects.ComboBonus,
                    TempComboBonus = character.Effects.TempComboBonus,
                    TempComboBonusTurns = character.Effects.TempComboBonusTurns,
                    DamageReduction = character.DamageReduction,
                    Inventory = character.Equipment.Inventory,
                    Head = character.Equipment.Head,
                    Body = character.Equipment.Body,
                    Weapon = character.Equipment.Weapon,
                    Feet = character.Equipment.Feet
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string json = JsonSerializer.Serialize(saveData, options);
                ErrorHandler.TryFileOperation(() =>
                {
                    File.WriteAllText(filename, json);
                }, $"SaveCharacter({filename})", () =>
                {
                    // Only show error in console mode (not in custom UI mode)
                    if (UIManager.GetCustomUIManager() == null)
                    {
                        UIManager.WriteLine($"Error saving character: Failed to write file");
                    }
                });
            }
            catch (System.IO.IOException ex)
            {
                // Only show error in console mode (not in custom UI mode)
                if (UIManager.GetCustomUIManager() == null)
                {
                    UIManager.WriteLine($"Error saving character: I/O error - {ex.Message}");
                }
            }
            catch (System.UnauthorizedAccessException ex)
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
        /// <param name="characterId">Optional character ID for multi-character support. If provided, loads from per-character filename.</param>
        /// <param name="filename">The filename to load from. If provided, overrides characterId-based naming.</param>
        /// <returns>The loaded character, or null if loading failed</returns>
        public static async Task<Character?> LoadCharacterAsync(string? characterId = null, string? filename = null)
        {
            try
            {
                // Use proper path resolution if no filename provided
                if (string.IsNullOrEmpty(filename))
                {
                    if (!string.IsNullOrEmpty(characterId))
                    {
                        // Multi-character support: use per-character filename
                        filename = GetCharacterSaveFilename(characterId);
                    }
                    else
                    {
                        // Backward compatibility: use default filename
                        filename = GameConstants.GetGameDataFilePath(GameConstants.CharacterSaveJson);
                    }
                }
                
                if (!File.Exists(filename))
                {
                    // Only show message in console mode (not in custom UI mode)
                    if (UIManager.GetCustomUIManager() == null)
                    {
                        UIManager.WriteLine($"No save file found at {filename}");
                    }
                    return null;
                }

                string json = await File.ReadAllTextAsync(filename).ConfigureAwait(false);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var saveData = JsonSerializer.Deserialize<CharacterSaveData>(json, options);

                if (saveData == null)
                {
                    // Only show message in console mode (not in custom UI mode)
                    if (UIManager.GetCustomUIManager() == null)
                    {
                        UIManager.WriteLine("Failed to deserialize character data");
                    }
                    return null;
                }

                // Create character with loaded data
                var character = new Character(saveData.Name, saveData.Level);
                
                // Restore all character data
                character.Progression.XP = saveData.XP;
                character.CurrentHealth = saveData.CurrentHealth;
                character.MaxHealth = saveData.MaxHealth;
                character.Stats.Strength = saveData.Strength;
                character.Stats.Agility = saveData.Agility;
                character.Stats.Technique = saveData.Technique;
                character.Stats.Intelligence = saveData.Intelligence;
                character.Progression.BarbarianPoints = saveData.BarbarianPoints;
                character.Progression.WarriorPoints = saveData.WarriorPoints;
                character.Progression.RoguePoints = saveData.RoguePoints;
                character.Progression.WizardPoints = saveData.WizardPoints;
                character.Effects.ComboStep = saveData.ComboStep;
                character.Effects.ComboBonus = saveData.ComboBonus;
                character.Effects.TempComboBonus = saveData.TempComboBonus;
                character.Effects.TempComboBonusTurns = saveData.TempComboBonusTurns;
                character.DamageReduction = saveData.DamageReduction;
                
                // Restore equipment with proper type conversion
                character.Equipment.Inventory = ItemTypeConverter.ConvertItemsToProperTypes(saveData.Inventory);
                character.Equipment.Head = ItemTypeConverter.ConvertItemToProperType(saveData.Head);
                character.Equipment.Body = ItemTypeConverter.ConvertItemToProperType(saveData.Body);
                character.Equipment.Weapon = ItemTypeConverter.ConvertItemToProperType(saveData.Weapon) as WeaponItem;
                character.Equipment.Feet = ItemTypeConverter.ConvertItemToProperType(saveData.Feet);

                // Rebuild action pool with proper structure
                character.ActionPool.Clear();
                // Removed: AddDefaultActions (BASIC ATTACK removed)
                // Removed: AddClassActions (no longer adding all class actions)
                // Actions are now only added via weapon GearAction property
                
                // Re-add gear actions for equipped items (with probability for non-starter items)
                if (character.Equipment.Head != null)
                    character.Actions.AddArmorActions(character, character.Equipment.Head);
                if (character.Equipment.Body != null)
                    character.Actions.AddArmorActions(character, character.Equipment.Body);
                if (character.Equipment.Weapon is WeaponItem weapon)
                    character.Actions.AddWeaponActions(character, weapon);
                if (character.Equipment.Feet != null)
                    character.Actions.AddArmorActions(character, character.Equipment.Feet);

                // Initialize combo sequence after all actions are loaded
                character.InitializeDefaultCombo();
                
                // Reset combo step to first action when loading (InitializeDefaultCombo may have already done this via AddToCombo, but ensure it's reset)
                character.ComboStep = 0;

                // Only show load message in console mode (not in custom UI mode)
                if (UIManager.GetCustomUIManager() == null)
                {
                    UIManager.WriteLine($"Character loaded from {filename}");
                }
                return character;
            }
            catch (System.IO.FileNotFoundException ex)
            {
                // Only show error in console mode (not in custom UI mode)
                if (UIManager.GetCustomUIManager() == null)
                {
                    UIManager.WriteLine($"Error loading character: File not found - {ex.Message}");
                }
                return null;
            }
            catch (System.IO.IOException ex)
            {
                // Only show error in console mode (not in custom UI mode)
                if (UIManager.GetCustomUIManager() == null)
                {
                    UIManager.WriteLine($"Error loading character: I/O error - {ex.Message}");
                }
                return null;
            }
            catch (System.UnauthorizedAccessException ex)
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
        /// NOTE: This method is deprecated. Use LoadCharacterAsync instead for proper async handling.
        /// This synchronous wrapper blocks the calling thread and should not be used in UI contexts.
        /// </summary>
        /// <param name="characterId">Optional character ID for multi-character support. If provided, loads from per-character filename.</param>
        /// <param name="filename">The filename to load from. If provided, overrides characterId-based naming.</param>
        /// <returns>The loaded character, or null if loading failed</returns>
        [Obsolete("Use LoadCharacterAsync instead. This method blocks the calling thread and may freeze the UI.")]
        public static Character? LoadCharacter(string? characterId = null, string? filename = null)
        {
            // For backward compatibility only - callers should migrate to async version
            // Using ConfigureAwait(false) to avoid deadlocks, but this still blocks
            return LoadCharacterAsync(characterId, filename).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Deletes a character save file
        /// </summary>
        /// <param name="filename">The filename to delete</param>
        public static void DeleteSaveFile(string? filename = null)
        {
            try
            {
                // Use proper path resolution if no filename provided
                if (string.IsNullOrEmpty(filename))
                {
                    filename = GameConstants.GetGameDataFilePath(GameConstants.CharacterSaveJson);
                }
                
                if (File.Exists(filename))
                {
                    File.Delete(filename);
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                // File doesn't exist - this is fine, nothing to delete
                // Silently ignore
            }
            catch (System.IO.IOException ex)
            {
                // Only show error in console mode (not in custom UI mode)
                if (UIManager.GetCustomUIManager() == null)
                {
                    UIManager.WriteLine($"Error deleting save file: I/O error - {ex.Message}");
                }
            }
            catch (System.UnauthorizedAccessException ex)
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
        /// </summary>
        /// <param name="filename">The filename to check</param>
        /// <returns>True if the save file exists</returns>
        public static bool SaveFileExists(string? filename = null)
        {
            // Use proper path resolution if no filename provided
            if (string.IsNullOrEmpty(filename))
            {
                filename = GameConstants.GetGameDataFilePath(GameConstants.CharacterSaveJson);
            }
            
            return File.Exists(filename);
        }

        /// <summary>
        /// Gets information about a saved character without loading it
        /// </summary>
        /// <param name="filename">The filename to check</param>
        /// <returns>Tuple of (characterName, level) or (null, 0) if not found</returns>
        public static (string? characterName, int level) GetSavedCharacterInfo(string? filename = null)
        {
            try
            {
                // Use proper path resolution if no filename provided
                if (string.IsNullOrEmpty(filename))
                {
                    filename = GameConstants.GetGameDataFilePath(GameConstants.CharacterSaveJson);
                }
                
                if (!File.Exists(filename))
                    return (null, 0);

                string json = File.ReadAllText(filename);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var saveData = JsonSerializer.Deserialize<CharacterSaveData>(json, options);
                
                return saveData != null ? (saveData.Name, saveData.Level) : (null, 0);
            }
            catch
            {
                return (null, 0);
            }
        }
        
        /// <summary>
        /// Gets the save filename for a character ID
        /// </summary>
        /// <param name="characterId">The character ID</param>
        /// <returns>The full path to the character's save file</returns>
        public static string GetCharacterSaveFilename(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
                throw new ArgumentException("Character ID cannot be null or empty", nameof(characterId));
            
            // Sanitize character ID for filename (remove any invalid characters)
            var sanitizedId = characterId.Replace(" ", "_").Replace("/", "_").Replace("\\", "_");
            var fileName = $"character_{sanitizedId}_save.json";
            return GameConstants.GetGameDataFilePath(fileName);
        }
        
        /// <summary>
        /// Lists all saved characters in the GameData directory
        /// </summary>
        /// <returns>List of tuples containing (characterId, characterName, level) for each saved character</returns>
        public static List<(string characterId, string characterName, int level)> ListAllSavedCharacters()
        {
            var results = new List<(string, string, int)>();
            
            try
            {
                var gameDataPath = GameConstants.GetGameDataFilePath("");
                var directory = Path.GetDirectoryName(gameDataPath);
                
                if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                {
                    // Try to find GameData directory
                    directory = Path.GetDirectoryName(GameConstants.GetGameDataFilePath(GameConstants.CharacterSaveJson));
                }
                
                if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                    return results;
                
                // Look for character save files
                var saveFiles = Directory.GetFiles(directory, "character_*_save.json");
                
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
                var legacyFile = GameConstants.GetGameDataFilePath(GameConstants.CharacterSaveJson);
                if (File.Exists(legacyFile))
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
