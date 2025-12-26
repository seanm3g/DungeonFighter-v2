namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;

    /// <summary>
    /// Manages all game state including current game state, player, inventory, and dungeon progression.
    /// This manager centralizes state management that was previously scattered throughout Game.cs.
    /// 
    /// Responsibilities:
    /// - Track current game state (MainMenu, Combat, Dungeon, etc.)
    /// - Manage player character reference
    /// - Manage inventory
    /// - Manage available dungeons
    /// - Track current dungeon and room
    /// - Validate state transitions
    /// - Provide clean API for state operations
    /// - Notify subscribers of state changes via events
    /// - Multi-character support: Manage character registry and active character
    /// </summary>
    public class GameStateManager
    {
        // State fields
        private GameState currentState = GameState.MainMenu;
        private Character? currentPlayer;
        private List<Item> currentInventory = new();
        private List<Dungeon> availableDungeons = new();
        private Dungeon? currentDungeon;
        private Environment? currentRoom;
        
        // Multi-character support
        private Dictionary<string, CharacterContext> _characters = new();
        private string? _activeCharacterId;
        
        /// <summary>
        /// Event fired when game state transitions occur.
        /// Allows systems (like UI animations) to react to state changes.
        /// </summary>
        public event EventHandler<StateChangedEventArgs>? StateChanged;
        
        /// <summary>
        /// Event fired when the active character is switched.
        /// Allows systems (like UI) to react to character changes.
        /// </summary>
        public event EventHandler<CharacterSwitchedEventArgs>? CharacterSwitched;

        /// <summary>
        /// Gets or sets the current game state.
        /// </summary>
        public GameState CurrentState
        {
            get => currentState;
            private set => currentState = value;
        }

        /// <summary>
        /// Gets or sets the current player character.
        /// When set, also updates the inventory reference if player has inventory.
        /// This is now a computed property that returns the active character from the registry.
        /// </summary>
        public Character? CurrentPlayer
        {
            get => GetActiveCharacter();
            private set => currentPlayer = value;
        }
        
        /// <summary>
        /// Gets the active character context (includes dungeon/room state)
        /// </summary>
        public CharacterContext? ActiveContext => GetActiveCharacterContext();

        /// <summary>
        /// Gets the current inventory.
        /// Returns the active character's inventory if available, otherwise returns the shared inventory list.
        /// </summary>
        public List<Item> CurrentInventory
        {
            get
            {
                var activeCharacter = GetActiveCharacter();
                if (activeCharacter != null && activeCharacter.Inventory != null)
                {
                    // #region agent log
                    try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTime.UtcNow.Ticks}", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "GameStateManager.cs:CurrentInventory.get", message = "Returning active character inventory", data = new { activeCharacterName = activeCharacter.Name, inventoryCount = activeCharacter.Inventory.Count, inventoryReference = activeCharacter.Inventory.GetHashCode() }, sessionId = "debug-session", runId = "run1", hypothesisId = "H" }) + "\n"); } catch { }
                    // #endregion
                    return activeCharacter.Inventory;
                }
                // #region agent log
                try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTime.UtcNow.Ticks}", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "GameStateManager.cs:CurrentInventory.get", message = "Returning shared inventory (no active character)", data = new { inventoryCount = currentInventory.Count }, sessionId = "debug-session", runId = "run1", hypothesisId = "H" }) + "\n"); } catch { }
                // #endregion
                return currentInventory;
            }
            private set => currentInventory = value;
        }

        /// <summary>
        /// Gets the list of available dungeons.
        /// </summary>
        public List<Dungeon> AvailableDungeons
        {
            get => availableDungeons;
            private set => availableDungeons = value;
        }

        /// <summary>
        /// Gets or sets the current dungeon being explored.
        /// Now uses active character context if available.
        /// </summary>
        public Dungeon? CurrentDungeon
        {
            get
            {
                var context = GetActiveCharacterContext();
                return context?.ActiveDungeon ?? currentDungeon;
            }
            private set
            {
                currentDungeon = value;
                var context = GetActiveCharacterContext();
                if (context != null)
                {
                    context.ActiveDungeon = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the current room within a dungeon.
        /// Now uses active character context if available.
        /// </summary>
        public Environment? CurrentRoom
        {
            get
            {
                var context = GetActiveCharacterContext();
                return context?.ActiveRoom ?? currentRoom;
            }
            private set
            {
                currentRoom = value;
                var context = GetActiveCharacterContext();
                if (context != null)
                {
                    context.ActiveRoom = value;
                }
            }
        }

        /// <summary>
        /// Convenience property to check if a player exists.
        /// </summary>
        public bool HasPlayer => CurrentPlayer != null;

        /// <summary>
        /// Convenience property to check if a dungeon is active.
        /// </summary>
        public bool HasCurrentDungeon => CurrentDungeon != null;

        /// <summary>
        /// Convenience property to check if a room is active.
        /// </summary>
        public bool HasCurrentRoom => CurrentRoom != null;

        /// <summary>
        /// Transitions the game to a new state.
        /// Validates the transition before applying and fires StateChanged event.
        /// </summary>
        /// <param name="newState">The new state to transition to.</param>
        /// <returns>True if transition was successful, false if invalid.</returns>
        public bool TransitionToState(GameState newState)
        {
            if (ValidateStateTransition(currentState, newState))
            {
                var previousState = currentState;
                CurrentState = newState;
                
                // Fire state change event to notify subscribers (e.g., UI animations)
                StateChanged?.Invoke(this, new StateChangedEventArgs(previousState, newState));
                
                return true;
            }

            ErrorHandler.LogWarning($"Invalid state transition from {currentState} to {newState}");
            return false;
        }

        /// <summary>
        /// Validates whether a state transition is allowed.
        /// Currently allows all transitions; can be enhanced with business logic.
        /// </summary>
        /// <param name="from">Current state.</param>
        /// <param name="to">Target state.</param>
        /// <returns>True if transition is valid, false otherwise.</returns>
        public bool ValidateStateTransition(GameState from, GameState to)
        {
            // Define invalid transitions based on game logic
            // For now, we allow all transitions as the original Game.cs does
            // This can be enhanced later with specific validation rules
            
            // Example rules that could be added:
            // - Can't go from MainMenu to Combat directly
            // - Can only go to Combat from Dungeon
            // - Can't leave Combat without completing it
            
            return true; // All transitions valid for now
        }

        /// <summary>
        /// Sets the current player and updates inventory reference.
        /// For backward compatibility - also registers character if not already registered.
        /// </summary>
        /// <param name="player">The player character to set, or null to clear.</param>
        public void SetCurrentPlayer(Character? player)
        {
            currentPlayer = player;
            if (player != null)
            {
                // If character is not in registry, add it
                var existingContext = _characters.Values.FirstOrDefault(c => c.Character == player);
                if (existingContext == null)
                {
                    // Generate character ID and add to registry
                    var characterId = GenerateCharacterId(player);
                    AddCharacter(player, characterId);
                }
                else
                {
                    // Switch to existing character
                    _activeCharacterId = existingContext.CharacterId;
                    existingContext.UpdateLastActive();
                }
                
                if (player.Inventory != null)
                {
                    CurrentInventory = player.Inventory;
                }
            }
            else
            {
                _activeCharacterId = null;
                CurrentInventory.Clear();
            }
        }

        /// <summary>
        /// Sets the current dungeon and clears the current room.
        /// </summary>
        /// <param name="dungeon">The dungeon to set, or null to clear.</param>
        public void SetCurrentDungeon(Dungeon? dungeon)
        {
            CurrentDungeon = dungeon;
            CurrentRoom = null; // Reset room when entering new dungeon
        }

        /// <summary>
        /// Sets the current room within the active dungeon.
        /// </summary>
        /// <param name="room">The room to set, or null to clear.</param>
        public void SetCurrentRoom(Environment? room)
        {
            CurrentRoom = room;
        }

        /// <summary>
        /// Updates the list of available dungeons.
        /// </summary>
        /// <param name="dungeons">New list of dungeons.</param>
        public void SetAvailableDungeons(List<Dungeon> dungeons)
        {
            AvailableDungeons = new List<Dungeon>(dungeons ?? new List<Dungeon>());
        }

        /// <summary>
        /// Resets all game state to initial values.
        /// Used when starting a new game or returning to main menu.
        /// </summary>
        public void ResetGameState()
        {
            CurrentState = GameState.MainMenu;
            CurrentPlayer = null;
            CurrentInventory.Clear();
            AvailableDungeons.Clear();
            CurrentDungeon = null;
            CurrentRoom = null;
        }

        /// <summary>
        /// Resets only the dungeon-related state.
        /// Used when completing a dungeon or returning to game loop.
        /// </summary>
        public void ResetDungeonState()
        {
            CurrentDungeon = null;
            CurrentRoom = null;
        }

        /// <summary>
        /// Gets a string representation of the current game state for debugging.
        /// </summary>
        /// <returns>Formatted string showing current state information.</returns>
        public override string ToString()
        {
            return $"GameState: {CurrentState}, " +
                   $"Player: {CurrentPlayer?.Name ?? "None"}, " +
                   $"Dungeon: {CurrentDungeon?.Theme ?? "None"}, " +
                   $"Room: {CurrentRoom?.Name ?? "None"}, " +
                   $"Characters: {_characters.Count}, " +
                   $"Active: {_activeCharacterId ?? "None"}";
        }
        
        // ========== Multi-Character Support Methods ==========
        
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
                currentPlayer = character;
                if (character.Inventory != null)
                {
                    CurrentInventory = character.Inventory;
                }
            }
            
            return characterId;
        }
        
        /// <summary>
        /// Switches the active character to the one with the given ID.
        /// </summary>
        /// <param name="characterId">The character ID to switch to</param>
        /// <returns>True if switch was successful, false if character not found</returns>
        public bool SwitchCharacter(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return false;
            
            if (!_characters.TryGetValue(characterId, out var context))
            {
                return false;
            }
            
            var previousCharacterId = _activeCharacterId;
            _activeCharacterId = characterId;
            currentPlayer = context.Character;
            context.UpdateLastActive();
            
            // #region agent log
            try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTime.UtcNow.Ticks}", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "GameStateManager.cs:SwitchCharacter", message = "Character switch", data = new { previousCharacterId, newCharacterId = characterId, previousCharacterName = _characters.TryGetValue(previousCharacterId ?? "", out var prev) ? prev.Character.Name : "null", newCharacterName = context.Character.Name }, sessionId = "debug-session", runId = "run1", hypothesisId = "A" }) + "\n"); } catch { }
            // #endregion
            
            // Update inventory reference
            if (context.Character.Inventory != null)
            {
                CurrentInventory = context.Character.Inventory;
            }
            else
            {
                CurrentInventory.Clear();
            }
            
            // Restore dungeon/room state from context
            currentDungeon = context.ActiveDungeon;
            currentRoom = context.ActiveRoom;
            
            // Fire character switched event
            CharacterSwitched?.Invoke(this, new CharacterSwitchedEventArgs(previousCharacterId, characterId, context.Character));
            
            return true;
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
            if (string.IsNullOrEmpty(_activeCharacterId)) return currentPlayer;
            
            return _characters.TryGetValue(_activeCharacterId, out var context) ? context.Character : currentPlayer;
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
        /// Gets the ID of the currently active character.
        /// </summary>
        /// <returns>The active character ID, or null if none</returns>
        public string? GetActiveCharacterId()
        {
            return _activeCharacterId;
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
        /// <returns>True if removed successfully, false if not found</returns>
        public bool RemoveCharacter(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return false;
            
            if (!_characters.ContainsKey(characterId)) return false;
            
            // If removing active character, switch to another or clear
            if (_activeCharacterId == characterId)
            {
                var remaining = _characters.Keys.Where(id => id != characterId).FirstOrDefault();
                if (remaining != null)
                {
                    SwitchCharacter(remaining);
                }
                else
                {
                    _activeCharacterId = null;
                    currentPlayer = null;
                    CurrentInventory.Clear();
                    currentDungeon = null;
                    currentRoom = null;
                }
            }
            
            _characters.Remove(characterId);
            return true;
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
    
    /// <summary>
    /// Event arguments for state change events.
    /// </summary>
    public class StateChangedEventArgs : EventArgs
    {
        public GameState PreviousState { get; }
        public GameState NewState { get; }
        
        public StateChangedEventArgs(GameState previousState, GameState newState)
        {
            PreviousState = previousState;
            NewState = newState;
        }
    }
    
    /// <summary>
    /// Event arguments for character switched events.
    /// </summary>
    public class CharacterSwitchedEventArgs : EventArgs
    {
        public string? PreviousCharacterId { get; }
        public string NewCharacterId { get; }
        public Character NewCharacter { get; }
        
        public CharacterSwitchedEventArgs(string? previousCharacterId, string newCharacterId, Character newCharacter)
        {
            PreviousCharacterId = previousCharacterId;
            NewCharacterId = newCharacterId;
            NewCharacter = newCharacter;
        }
    }
}

