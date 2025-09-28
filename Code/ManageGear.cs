using System;

namespace RPGGame
{
    /// <summary>
    /// Legacy Inventory class - now uses InventoryController for all operations
    /// This class is kept for backward compatibility
    /// </summary>
    public class Inventory
    {
        private InventoryController controller;

        public Inventory(Character player, List<Item> inventory)
        {
            controller = new InventoryController(player, inventory);
        }

        public bool ShowGearMenu()
        {
            return controller.ShowGearMenu();
        }

        // All methods moved to specialized components:
        // - ShowCharacterStats, ShowCurrentEquipment, ShowInventory, ShowComboInfo, ShowOptions -> InventoryDisplayManager
        // - EquipItem, UnequipItem, DiscardItem -> EquipmentManager  
        // - ManageComboActions, AddActionToCombo, RemoveActionFromCombo, SwapComboActions -> ComboManager
    }
}