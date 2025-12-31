using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Manages dungeon display state tracking
    /// Handles current dungeon, room, enemy, and player state
    /// Supports per-character dungeon state for background dungeon support
    /// </summary>
    public class DungeonDisplayState
    {
        // Current state (for active character's dungeon)
        private Dungeon? currentDungeon;
        private Environment? currentRoom;
        private Enemy? currentEnemy;
        private Character? currentPlayer; // Active character's dungeon (for UI context)
        private int currentRoomNumber;
        private int totalRooms;
        
        // Per-character dungeon state tracking
        // Maps character ID to the character that owns that character's dungeon state
        // This allows Character A's dungeon to continue running in background while Character B is active
        private readonly Dictionary<string, Character> dungeonOwners = new Dictionary<string, Character>();
        
        private readonly GameStateManager? stateManager;

        public DungeonDisplayState(GameStateManager? stateManager = null)
        {
            this.stateManager = stateManager;
        }

        /// <summary>
        /// Gets the current dungeon
        /// </summary>
        public Dungeon? CurrentDungeon => currentDungeon;

        /// <summary>
        /// Gets the current room
        /// </summary>
        public Environment? CurrentRoom => currentRoom;

        /// <summary>
        /// Gets the current enemy
        /// </summary>
        public Enemy? CurrentEnemy => currentEnemy;

        /// <summary>
        /// Gets the current player
        /// </summary>
        public Character? CurrentPlayer => currentPlayer;

        /// <summary>
        /// Gets the current room number
        /// </summary>
        public int CurrentRoomNumber => currentRoomNumber;

        /// <summary>
        /// Gets the total number of rooms
        /// </summary>
        public int TotalRooms => totalRooms;

        /// <summary>
        /// Gets the dungeon name
        /// </summary>
        public string? GetDungeonName() => currentDungeon?.Name;

        /// <summary>
        /// Gets the room name
        /// </summary>
        public string? GetRoomName() => currentRoom?.Name;

        /// <summary>
        /// Sets the current dungeon and player
        /// </summary>
        public void SetDungeon(Dungeon dungeon, Character player)
        {
            if (dungeon == null) throw new ArgumentNullException(nameof(dungeon));
            if (player == null) throw new ArgumentNullException(nameof(player));

            // CRITICAL: Only update currentPlayer if this character is the active character
            // This prevents overwriting currentPlayer when another character's dungeon is still running
            var activeCharacter = stateManager?.GetActiveCharacter();
            if (activeCharacter != null && player != activeCharacter)
            {
                // Character is not active - don't start dungeon (another character's dungeon is still running)
                return;
            }

            currentDungeon = dungeon;
            currentPlayer = player;
            currentRoomNumber = 0;
            totalRooms = dungeon.Rooms.Count;
            
            // Store dungeon owner per-character for background dungeon support
            string? characterId = stateManager?.GetCharacterId(player);
            if (!string.IsNullOrEmpty(characterId))
            {
                dungeonOwners[characterId] = player;
            }
        }

        /// <summary>
        /// Sets the current room
        /// </summary>
        public void SetRoom(Environment room, int roomNumber, int totalRooms)
        {
            if (room == null) throw new ArgumentNullException(nameof(room));

            currentRoom = room;
            currentRoomNumber = roomNumber;
            this.totalRooms = totalRooms;
            currentEnemy = null;
        }

        /// <summary>
        /// Sets the current enemy
        /// </summary>
        public void SetEnemy(Enemy enemy)
        {
            if (enemy == null) throw new ArgumentNullException(nameof(enemy));
            currentEnemy = enemy;
        }

        /// <summary>
        /// Clears the current enemy
        /// </summary>
        public void ClearEnemy()
        {
            currentEnemy = null;
        }

        /// <summary>
        /// Determines the character that owns a message
        /// If sourceCharacter is provided, use it directly (most reliable)
        /// Otherwise, prioritize dungeonOwners over currentPlayer because currentPlayer is the ACTIVE character,
        /// not necessarily the character whose dungeon is running (for background dungeons)
        /// </summary>
        public Character? DetermineMessageOwner(Character? sourceCharacter)
        {
            if (sourceCharacter != null)
            {
                return sourceCharacter;
            }

            // First, try to find the character from dungeonOwners
            // This is the character whose dungeon is actually running
            if (dungeonOwners.Count > 0)
            {
                // If there's only one dungeon owner, use it (most common case)
                if (dungeonOwners.Count == 1)
                {
                    return dungeonOwners.Values.First();
                }
                else
                {
                    // Multiple dungeons running - try to match with currentPlayer if available
                    if (currentPlayer != null)
                    {
                        string? characterId = stateManager?.GetCharacterId(currentPlayer);
                        if (!string.IsNullOrEmpty(characterId) && dungeonOwners.TryGetValue(characterId, out var owner))
                        {
                            return owner;
                        }
                    }
                    
                    // If still no match, use the first one (shouldn't happen in normal flow)
                    return dungeonOwners.Values.FirstOrDefault();
                }
            }
            else if (currentPlayer != null)
            {
                // No dungeon owners - fall back to currentPlayer
                return currentPlayer;
            }

            return null;
        }

        /// <summary>
        /// Clears all state
        /// </summary>
        public void ClearAll()
        {
            currentDungeon = null;
            currentRoom = null;
            currentEnemy = null;
            currentPlayer = null;
            currentRoomNumber = 0;
            totalRooms = 0;
            
            // Only clear dungeon owner for the active character, not all characters
            // This allows background dungeons to continue running
            var activeCharacter = stateManager?.GetActiveCharacter();
            if (activeCharacter != null && stateManager != null)
            {
                string? characterId = stateManager.GetCharacterId(activeCharacter);
                if (!string.IsNullOrEmpty(characterId))
                {
                    dungeonOwners.Remove(characterId);
                }
            }
        }

        /// <summary>
        /// Handles character switch events
        /// Updates currentPlayer to the new active character
        /// </summary>
        public void OnCharacterSwitched()
        {
            var newActiveCharacter = stateManager?.GetActiveCharacter();
            
            // Only update currentPlayer if the new character is different
            // This prevents unnecessary updates when the same character is "switched" to
            if (currentPlayer != newActiveCharacter)
            {
                // If the new character is in a dungeon, currentPlayer will be set when they enter combat
                // For now, clear it to ensure background combat from old character doesn't interfere
                // The currentPlayer will be set correctly when the new character starts combat
                currentPlayer = null;
                // Don't clear dungeonOwners here - they should persist so background combat messages route correctly
                // Only clear dungeonOwners when explicitly clearing all (which happens when a character leaves their dungeon)
            }
        }

        /// <summary>
        /// Gets the count of dungeon owners (for debugging)
        /// </summary>
        public int DungeonOwnersCount => dungeonOwners.Count;
    }
}
