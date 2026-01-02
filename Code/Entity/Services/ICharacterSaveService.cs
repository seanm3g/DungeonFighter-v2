using System.Collections.Generic;
using System.Threading.Tasks;

namespace RPGGame.Entity.Services
{
    /// <summary>
    /// Interface for character save and load operations.
    /// Extracted from static CharacterSaveManager to enable dependency injection and testing.
    /// </summary>
    public interface ICharacterSaveService
    {
        /// <summary>
        /// Saves a character to a JSON file
        /// </summary>
        /// <param name="character">The character to save</param>
        /// <param name="characterId">Optional character ID for multi-character support. If provided, generates per-character filename.</param>
        /// <param name="filename">The filename to save to. If provided, overrides characterId-based naming.</param>
        void SaveCharacter(Character character, string? characterId = null, string? filename = null);

        /// <summary>
        /// Loads a character from a JSON file (async version to prevent UI freezing)
        /// </summary>
        /// <param name="characterId">Optional character ID for multi-character support. If provided, loads from per-character filename.</param>
        /// <param name="filename">The filename to load from. If provided, overrides characterId-based naming.</param>
        /// <returns>The loaded character, or null if loading failed</returns>
        Task<Character?> LoadCharacterAsync(string? characterId = null, string? filename = null);

        /// <summary>
        /// Loads a character from a JSON file (synchronous version for backward compatibility)
        /// NOTE: This method is deprecated. Use LoadCharacterAsync instead.
        /// </summary>
        /// <param name="characterId">Optional character ID for multi-character support. If provided, loads from per-character filename.</param>
        /// <param name="filename">The filename to load from. If provided, overrides characterId-based naming.</param>
        /// <returns>The loaded character, or null if loading failed</returns>
        [System.Obsolete("Use LoadCharacterAsync instead. This method blocks the calling thread and may freeze the UI.")]
        Character? LoadCharacter(string? characterId = null, string? filename = null);

        /// <summary>
        /// Deletes a character save file
        /// </summary>
        /// <param name="filename">The filename to delete</param>
        void DeleteSaveFile(string? filename = null);

        /// <summary>
        /// Checks if a save file exists
        /// </summary>
        /// <param name="filename">The filename to check</param>
        /// <returns>True if the save file exists</returns>
        bool SaveFileExists(string? filename = null);

        /// <summary>
        /// Gets information about a saved character without loading it
        /// </summary>
        /// <param name="filename">The filename to check</param>
        /// <returns>Tuple of (characterName, level) or (null, 0) if not found</returns>
        (string? characterName, int level) GetSavedCharacterInfo(string? filename = null);

        /// <summary>
        /// Gets the save filename for a character ID
        /// </summary>
        /// <param name="characterId">The character ID</param>
        /// <returns>The full path to the character's save file</returns>
        string GetCharacterSaveFilename(string characterId);

        /// <summary>
        /// Lists all saved characters in the GameData directory
        /// </summary>
        /// <returns>List of tuples containing (characterId, characterName, level) for each saved character</returns>
        List<(string characterId, string characterName, int level)> ListAllSavedCharacters();

        /// <summary>
        /// Clears all saved characters by deleting all save files
        /// </summary>
        /// <returns>The number of save files deleted</returns>
        int ClearAllSavedCharacters();
    }
}

