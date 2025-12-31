using System;
using System.Collections.Generic;
using RPGGame.UI.ColorSystem;

namespace RPGGame
{
    /// <summary>
    /// Manages multi-character state operations.
    /// Handles character registration, switching, and context management.
    /// Extracted from GameStateManager to improve Single Responsibility Principle compliance.
    /// </summary>
    public class CharacterStateManager
    {
        private readonly CharacterRegistry characterRegistry;
        
        /// <summary>
        /// Event fired when the active character is switched.
        /// Allows systems (like UI) to react to character changes.
        /// </summary>
        public event EventHandler<CharacterSwitchedEventArgs>? CharacterSwitched;
        
        /// <summary>
        /// Initializes a new instance of CharacterStateManager
        /// </summary>
        public CharacterStateManager()
        {
            characterRegistry = new CharacterRegistry();
            // Wire up character switched event from registry
            characterRegistry.CharacterSwitched += (sender, e) => CharacterSwitched?.Invoke(this, e);
        }
        
        /// <summary>
        /// Gets the ID of the currently active character.
        /// </summary>
        public string? ActiveCharacterId => characterRegistry.ActiveCharacterId;
        
        /// <summary>
        /// Gets the number of registered characters.
        /// </summary>
        public int Count => characterRegistry.Count;
        
        /// <summary>
        /// Adds a character to the registry and returns its ID.
        /// If character already exists, returns existing ID.
        /// </summary>
        /// <param name="character">The character to add</param>
        /// <param name="characterId">Optional character ID. If not provided, one will be generated.</param>
        /// <returns>The character ID</returns>
        public string AddCharacter(Character character, string? characterId = null)
        {
            var id = characterRegistry.AddCharacter(character, characterId);
            
            // Register character name with keyword color system for automatic coloring
            if (character != null && !string.IsNullOrEmpty(character.Name))
            {
                // Get character color based on their class
                var characterColor = EntityColorHelper.GetActorColor(character);
                KeywordColorSystem.RegisterCharacterName(character.Name, characterColor);
            }
            
            return id;
        }
        
        /// <summary>
        /// Switches the active character to the one with the given ID.
        /// </summary>
        /// <param name="characterId">The character ID to switch to</param>
        /// <returns>The character context if switch was successful, null if character not found</returns>
        public CharacterContext? SwitchCharacter(string characterId)
        {
            return characterRegistry.SwitchCharacter(characterId);
        }
        
        /// <summary>
        /// Gets a character by ID.
        /// </summary>
        /// <param name="characterId">The character ID</param>
        /// <returns>The character, or null if not found</returns>
        public Character? GetCharacter(string characterId)
        {
            return characterRegistry.GetCharacter(characterId);
        }
        
        /// <summary>
        /// Gets the character context by ID.
        /// </summary>
        /// <param name="characterId">The character ID</param>
        /// <returns>The character context, or null if not found</returns>
        public CharacterContext? GetCharacterContext(string characterId)
        {
            return characterRegistry.GetCharacterContext(characterId);
        }
        
        /// <summary>
        /// Gets all registered characters.
        /// </summary>
        /// <returns>List of all characters</returns>
        public List<Character> GetAllCharacters()
        {
            return characterRegistry.GetAllCharacters();
        }
        
        /// <summary>
        /// Gets all character contexts.
        /// </summary>
        /// <returns>List of all character contexts</returns>
        public List<CharacterContext> GetAllCharacterContexts()
        {
            return characterRegistry.GetAllCharacterContexts();
        }
        
        /// <summary>
        /// Gets the currently active character.
        /// </summary>
        /// <returns>The active character, or null if none</returns>
        public Character? GetActiveCharacter()
        {
            return characterRegistry.GetActiveCharacter();
        }
        
        /// <summary>
        /// Gets the currently active character context.
        /// </summary>
        /// <returns>The active character context, or null if none</returns>
        public CharacterContext? GetActiveCharacterContext()
        {
            return characterRegistry.GetActiveCharacterContext();
        }
        
        /// <summary>
        /// Gets the character ID for a given character.
        /// </summary>
        /// <param name="character">The character</param>
        /// <returns>The character ID, or null if not found</returns>
        public string? GetCharacterId(Character character)
        {
            return characterRegistry.GetCharacterId(character);
        }
        
        /// <summary>
        /// Removes a character from the registry.
        /// </summary>
        /// <param name="characterId">The character ID to remove</param>
        /// <param name="onActiveCharacterRemoved">Callback when active character is removed, receives remaining character IDs</param>
        /// <returns>True if removed successfully, false if not found</returns>
        public bool RemoveCharacter(string characterId, Action<List<string>>? onActiveCharacterRemoved = null)
        {
            return characterRegistry.RemoveCharacter(characterId, onActiveCharacterRemoved);
        }
        
        /// <summary>
        /// Sets the active character ID directly (used for initialization).
        /// </summary>
        /// <param name="characterId">The character ID to set as active</param>
        public void SetActiveCharacterId(string? characterId)
        {
            characterRegistry.SetActiveCharacterId(characterId);
        }
    }
}
