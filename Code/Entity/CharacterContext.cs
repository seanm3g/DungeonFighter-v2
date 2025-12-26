using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Tracks character-specific state independently for multi-character support.
    /// Each character maintains its own dungeon, room, and inventory state.
    /// </summary>
    public class CharacterContext
    {
        /// <summary>
        /// The character this context belongs to
        /// </summary>
        public Character Character { get; set; }

        /// <summary>
        /// The active dungeon for this character (if any)
        /// </summary>
        public Dungeon? ActiveDungeon { get; set; }

        /// <summary>
        /// The active room within the dungeon for this character (if any)
        /// </summary>
        public Environment? ActiveRoom { get; set; }

        /// <summary>
        /// Snapshot of inventory at the start of dungeon run (for tracking items found)
        /// </summary>
        public List<Item> InventorySnapshot { get; set; } = new List<Item>();

        /// <summary>
        /// Timestamp of when this character was last active
        /// </summary>
        public DateTime LastActive { get; set; } = DateTime.Now;

        /// <summary>
        /// Character ID used for save file naming and registry lookup
        /// </summary>
        public string CharacterId { get; set; } = string.Empty;

        /// <summary>
        /// Creates a new CharacterContext for the given character
        /// </summary>
        public CharacterContext(Character character, string characterId)
        {
            Character = character ?? throw new ArgumentNullException(nameof(character));
            CharacterId = characterId ?? throw new ArgumentNullException(nameof(characterId));
            LastActive = DateTime.Now;
        }

        /// <summary>
        /// Updates the last active timestamp
        /// </summary>
        public void UpdateLastActive()
        {
            LastActive = DateTime.Now;
        }

        /// <summary>
        /// Clears dungeon-related state (dungeon and room)
        /// </summary>
        public void ClearDungeonState()
        {
            ActiveDungeon = null;
            ActiveRoom = null;
        }

        /// <summary>
        /// Creates a snapshot of the character's current inventory
        /// </summary>
        public void SnapshotInventory()
        {
            InventorySnapshot = new List<Item>(Character.Inventory ?? new List<Item>());
        }
    }
}

