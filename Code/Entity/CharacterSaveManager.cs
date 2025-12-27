using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using RPGGame.Entity.Services;

namespace RPGGame
{
    /// <summary>
    /// Handles character save and load operations
    /// Extracted from Character.cs to reduce complexity
    /// Refactored to use instance-based service for better testability and dependency injection.
    /// Maintains static methods for backward compatibility.
    /// </summary>
    public static class CharacterSaveManager
    {
        // Default service instance for backward compatibility
        private static readonly ICharacterSaveService defaultService = new CharacterSaveService();

        /// <summary>
        /// Gets or sets the service instance used by static methods.
        /// Allows dependency injection for testing.
        /// </summary>
        public static ICharacterSaveService Service { get; set; } = defaultService;
        /// <summary>
        /// Saves a character to a JSON file
        /// Delegates to the service instance for implementation.
        /// </summary>
        /// <param name="character">The character to save</param>
        /// <param name="characterId">Optional character ID for multi-character support. If provided, generates per-character filename.</param>
        /// <param name="filename">The filename to save to. If provided, overrides characterId-based naming.</param>
        public static void SaveCharacter(Character character, string? characterId = null, string? filename = null)
        {
            Service.SaveCharacter(character, characterId, filename);
        }

        /// <summary>
        /// Loads a character from a JSON file (async version to prevent UI freezing)
        /// Delegates to the service instance for implementation.
        /// </summary>
        /// <param name="characterId">Optional character ID for multi-character support. If provided, loads from per-character filename.</param>
        /// <param name="filename">The filename to load from. If provided, overrides characterId-based naming.</param>
        /// <returns>The loaded character, or null if loading failed</returns>
        public static async Task<Character?> LoadCharacterAsync(string? characterId = null, string? filename = null)
        {
            return await Service.LoadCharacterAsync(characterId, filename).ConfigureAwait(false);
        }

        /// <summary>
        /// Loads a character from a JSON file (synchronous version for backward compatibility)
        /// NOTE: This method is deprecated. Use LoadCharacterAsync instead for proper async handling.
        /// This synchronous wrapper blocks the calling thread and should not be used in UI contexts.
        /// Delegates to the service instance for implementation.
        /// </summary>
        /// <param name="characterId">Optional character ID for multi-character support. If provided, loads from per-character filename.</param>
        /// <param name="filename">The filename to load from. If provided, overrides characterId-based naming.</param>
        /// <returns>The loaded character, or null if loading failed</returns>
        [Obsolete("Use LoadCharacterAsync instead. This method blocks the calling thread and may freeze the UI.")]
        public static Character? LoadCharacter(string? characterId = null, string? filename = null)
        {
            return Service.LoadCharacter(characterId, filename);
        }

        /// <summary>
        /// Deletes a character save file
        /// Delegates to the service instance for implementation.
        /// </summary>
        /// <param name="filename">The filename to delete</param>
        public static void DeleteSaveFile(string? filename = null)
        {
            Service.DeleteSaveFile(filename);
        }

        /// <summary>
        /// Checks if a save file exists
        /// Delegates to the service instance for implementation.
        /// </summary>
        /// <param name="filename">The filename to check</param>
        /// <returns>True if the save file exists</returns>
        public static bool SaveFileExists(string? filename = null)
        {
            return Service.SaveFileExists(filename);
        }

        /// <summary>
        /// Gets information about a saved character without loading it
        /// Delegates to the service instance for implementation.
        /// </summary>
        /// <param name="filename">The filename to check</param>
        /// <returns>Tuple of (characterName, level) or (null, 0) if not found</returns>
        public static (string? characterName, int level) GetSavedCharacterInfo(string? filename = null)
        {
            return Service.GetSavedCharacterInfo(filename);
        }
        
        /// <summary>
        /// Gets the save filename for a character ID
        /// Delegates to the service instance for implementation.
        /// </summary>
        /// <param name="characterId">The character ID</param>
        /// <returns>The full path to the character's save file</returns>
        public static string GetCharacterSaveFilename(string characterId)
        {
            return Service.GetCharacterSaveFilename(characterId);
        }
        
        /// <summary>
        /// Lists all saved characters in the GameData directory
        /// Delegates to the service instance for implementation.
        /// </summary>
        /// <returns>List of tuples containing (characterId, characterName, level) for each saved character</returns>
        public static List<(string characterId, string characterName, int level)> ListAllSavedCharacters()
        {
            return Service.ListAllSavedCharacters();
        }
    }

}
