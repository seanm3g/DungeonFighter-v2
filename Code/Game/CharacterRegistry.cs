using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Manages the character registry for multi-character support.
    /// Handles character registration, lookup, and active character management.
    /// Extracted from GameStateManager to improve Single Responsibility Principle compliance.
    /// </summary>
    public class CharacterRegistry
    {
        private readonly Dictionary<string, CharacterContext> _characters = new();
        private string? _activeCharacterId;

        /// <summary>
        /// Event fired when the active character is switched.
        /// Allows subscribers to react to character changes.
        /// </summary>
        public event EventHandler<CharacterSwitchedEventArgs>? CharacterSwitched;

        /// <summary>
        /// Gets the ID of the currently active character.
        /// </summary>
        public string? ActiveCharacterId => _activeCharacterId;

        /// <summary>
        /// Gets the number of registered characters.
        /// </summary>
        public int Count => _characters.Count;

        /// <summary>
        /// Adds a character to the registry and returns its ID.
        /// If character already exists, returns existing ID.
        /// </summary>
        /// <param name="character">The character to add</param>
        /// <param name="characterId">Optional character ID. If not provided, one will be generated.</param>
        /// <returns>The character ID</returns>
        public string AddCharacter(Character character, string? characterId = null)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            
            // Check if character already exists
            var existing = _characters.Values.FirstOrDefault(c => c.Character == character);
            if (existing != null)
            {
                return existing.CharacterId;
            }
            
            // Generate ID if not provided
            if (string.IsNullOrEmpty(characterId))
            {
                characterId = GenerateCharacterId(character);
            }
            
            // Create context and add to registry
            var context = new CharacterContext(character, characterId);
            _characters[characterId] = context;
            
            // If no active character, make this one active
            if (_activeCharacterId == null)
            {
                _activeCharacterId = characterId;
            }
            
            return characterId;
        }
        
        /// <summary>
        /// Switches the active character to the one with the given ID.
        /// </summary>
        /// <param name="characterId">The character ID to switch to</param>
        /// <returns>The character context if switch was successful, null if character not found</returns>
        public CharacterContext? SwitchCharacter(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return null;
            
            if (!_characters.TryGetValue(characterId, out var context))
            {
                return null;
            }
            
            var previousCharacterId = _activeCharacterId;
            _activeCharacterId = characterId;
            context.UpdateLastActive();
            
            // Fire character switched event
            CharacterSwitched?.Invoke(this, new CharacterSwitchedEventArgs(previousCharacterId, characterId, context.Character));
            
            return context;
        }
        
        /// <summary>
        /// Gets a character by ID.
        /// </summary>
        /// <param name="characterId">The character ID</param>
        /// <returns>The character, or null if not found</returns>
        public Character? GetCharacter(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return null;
            
            return _characters.TryGetValue(characterId, out var context) ? context.Character : null;
        }
        
        /// <summary>
        /// Gets the character context by ID.
        /// </summary>
        /// <param name="characterId">The character ID</param>
        /// <returns>The character context, or null if not found</returns>
        public CharacterContext? GetCharacterContext(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return null;
            
            return _characters.TryGetValue(characterId, out var context) ? context : null;
        }
        
        /// <summary>
        /// Gets all registered characters.
        /// </summary>
        /// <returns>List of all characters</returns>
        public List<Character> GetAllCharacters()
        {
            return _characters.Values.Select(c => c.Character).ToList();
        }
        
        /// <summary>
        /// Gets all character contexts.
        /// </summary>
        /// <returns>List of all character contexts</returns>
        public List<CharacterContext> GetAllCharacterContexts()
        {
            return _characters.Values.ToList();
        }
        
        /// <summary>
        /// Gets the currently active character.
        /// </summary>
        /// <returns>The active character, or null if none</returns>
        public Character? GetActiveCharacter()
        {
            if (string.IsNullOrEmpty(_activeCharacterId)) return null;
            
            return _characters.TryGetValue(_activeCharacterId, out var context) ? context.Character : null;
        }
        
        /// <summary>
        /// Gets the currently active character context.
        /// </summary>
        /// <returns>The active character context, or null if none</returns>
        public CharacterContext? GetActiveCharacterContext()
        {
            if (string.IsNullOrEmpty(_activeCharacterId)) return null;
            
            return _characters.TryGetValue(_activeCharacterId, out var context) ? context : null;
        }
        
        /// <summary>
        /// Gets the character ID for a given character.
        /// </summary>
        /// <param name="character">The character</param>
        /// <returns>The character ID, or null if not found</returns>
        public string? GetCharacterId(Character character)
        {
            if (character == null) return null;
            
            var context = _characters.Values.FirstOrDefault(c => c.Character == character);
            return context?.CharacterId;
        }
        
        /// <summary>
        /// Removes a character from the registry.
        /// </summary>
        /// <param name="characterId">The character ID to remove</param>
        /// <param name="onActiveCharacterRemoved">Callback when active character is removed, receives remaining character IDs</param>
        /// <returns>True if removed successfully, false if not found</returns>
        public bool RemoveCharacter(string characterId, Action<List<string>>? onActiveCharacterRemoved = null)
        {
            if (string.IsNullOrEmpty(characterId)) return false;
            
            if (!_characters.ContainsKey(characterId)) return false;
            
            // If removing active character, notify caller to handle switching
            if (_activeCharacterId == characterId)
            {
                var remaining = _characters.Keys.Where(id => id != characterId).ToList();
                _activeCharacterId = null;
                onActiveCharacterRemoved?.Invoke(remaining);
            }
            
            _characters.Remove(characterId);
            return true;
        }
        
        /// <summary>
        /// Sets the active character ID directly (used for initialization).
        /// </summary>
        /// <param name="characterId">The character ID to set as active</param>
        public void SetActiveCharacterId(string? characterId)
        {
            _activeCharacterId = characterId;
        }
        
        /// <summary>
        /// Clears all characters from the registry.
        /// Used when resetting game state or cleaning up after tests.
        /// </summary>
        public void ClearAllCharacters()
        {
            _characters.Clear();
            _activeCharacterId = null;
        }
        
        /// <summary>
        /// Generates a unique character ID for a character.
        /// Format: {Name}_{Level}_{ShortGuid}
        /// </summary>
        /// <param name="character">The character</param>
        /// <returns>A unique character ID</returns>
        private string GenerateCharacterId(Character character)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            
            // Sanitize name for filename use
            var sanitizedName = character.Name?.Replace(" ", "_").Replace("/", "_").Replace("\\", "_") ?? "Character";
            var shortGuid = Guid.NewGuid().ToString("N")[..8];
            return $"{sanitizedName}_{character.Level}_{shortGuid}";
        }
    }
}

