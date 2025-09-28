using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Main controller for inventory management, orchestrating display, equipment, and combo management
    /// </summary>
    public class InventoryController
    {
        private Character player;
        private List<Item> inventory;
        private InventoryDisplayManager displayManager;
        private EquipmentManager equipmentManager;
        private ComboManager comboManager;

        public InventoryController(Character player, List<Item> inventory)
        {
            this.player = player;
            this.inventory = inventory;
            
            // Initialize components
            displayManager = new InventoryDisplayManager(player, inventory);
            equipmentManager = new EquipmentManager(player, inventory);
            comboManager = new ComboManager(player, displayManager);
        }

        /// <summary>
        /// Shows the main gear menu and handles user input
        /// </summary>
        /// <returns>True if user wants to continue to dungeon, false if returning to main menu</returns>
        public bool ShowGearMenu()
        {
            while (true)
            {
                displayManager.ShowMainDisplay();

                if (int.TryParse(Console.ReadLine(), out int choice))
                {
                    switch (choice)
                    {
                        case 1:
                            equipmentManager.EquipItem();
                            break;
                        case 2:
                            equipmentManager.UnequipItem();
                            break;
                        case 3:
                            equipmentManager.DiscardItem();
                            break;
                        case 4:
                            comboManager.ManageComboActions();
                            break;
                        case 5:
                            return true; // Continue to dungeon
                        case 6:
                            return false; // Exit inventory menu and return to main menu
                        default:
                            Console.WriteLine("Invalid option.");
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("Invalid input.");
                }
            }
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
            // Update the display manager and equipment manager with the new inventory reference
            displayManager = new InventoryDisplayManager(player, inventory);
            equipmentManager = new EquipmentManager(player, inventory);
        }
    }
}
