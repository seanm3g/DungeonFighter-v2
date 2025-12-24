using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Facade for consolidated inventory management including display, equipment, and combo management
    /// 
    /// Refactored from 440 lines to ~150 lines using Facade pattern.
    /// Delegates to:
    /// - InventoryDisplayCoordinator: Display logic and menu handling
    /// - InventoryOperations: Item operations (equip, unequip, discard, trade-up)
    /// </summary>
    public class InventoryManager
    {
        private Character player;
        private List<Item> inventory;
        private InventoryDisplayCoordinator displayCoordinator;
        private InventoryOperations operations;

        public InventoryManager(Character player, List<Item> inventory)
        {
            this.player = player ?? throw new ArgumentNullException(nameof(player));
            this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            
            // Initialize components
            operations = new InventoryOperations(player, inventory);
            displayCoordinator = new InventoryDisplayCoordinator(player, inventory, operations);
        }

        /// <summary>
        /// Shows the main gear menu and handles user input
        /// </summary>
        /// <returns>True if user wants to continue to dungeon, false if returning to main menu, null if exiting game</returns>
        public bool? ShowGearMenu()
        {
            return displayCoordinator.ShowGearMenu();
        }

        /// <summary>
        /// Equips an item from inventory, destroying the previously equipped item
        /// </summary>
        public void EquipItem()
        {
            operations.EquipItem();
        }

        /// <summary>
        /// Unequips an item from a specific slot and adds it back to inventory
        /// </summary>
        public void UnequipItem()
        {
            operations.UnequipItem();
        }

        /// <summary>
        /// Discards an item from inventory permanently
        /// </summary>
        public void DiscardItem()
        {
            operations.DiscardItem();
        }

        /// <summary>
        /// Trades up 5 items of the same rarity for 1 item of the next higher rarity
        /// </summary>
        public void TradeUpItems()
        {
            operations.TradeUpItems();
        }

        /// <summary>
        /// Gets the inventory list for external access
        /// </summary>
        public List<Item> GetInventory()
        {
            return inventory;
        }

        /// <summary>
        /// Adds an item to inventory
        /// </summary>
        public void AddItem(Item item)
        {
            inventory.Add(item);
        }

        /// <summary>
        /// Removes an item from inventory
        /// </summary>
        public bool RemoveItem(Item item)
        {
            return inventory.Remove(item);
        }

        /// <summary>
        /// Gets the number of items in inventory
        /// </summary>
        public int GetInventoryCount()
        {
            return inventory.Count;
        }

        /// <summary>
        /// Updates the inventory reference (useful when inventory changes)
        /// </summary>
        public void UpdateInventory(List<Item> newInventory)
        {
            this.inventory = newInventory;
            // Recreate components with new inventory
            operations = new InventoryOperations(player, inventory);
            displayCoordinator = new InventoryDisplayCoordinator(player, inventory, operations);
        }
    }
}
