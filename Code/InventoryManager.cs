using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Consolidated inventory management including display, equipment, and combo management
    /// </summary>
    public class InventoryManager
    {
        private Character player;
        private List<Item> inventory;
        private InventoryDisplayManager displayManager;
        private ComboManager comboManager;

        public InventoryManager(Character player, List<Item> inventory)
        {
            this.player = player;
            this.inventory = inventory;
            
            // Initialize components
            displayManager = new InventoryDisplayManager(player, inventory);
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
                            EquipItem();
                            break;
                        case 2:
                            UnequipItem();
                            break;
                        case 3:
                            DiscardItem();
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
        /// Equips an item from inventory, destroying the previously equipped item
        /// </summary>
        public void EquipItem()
        {
            Console.Write("Enter the number of the item to equip: ");
            if (int.TryParse(Console.ReadLine(), out int equipChoice) && 
                equipChoice > 0 && equipChoice <= inventory.Count)
            {
                var item = inventory[equipChoice - 1];
                string slot = item.Type switch
                {
                    ItemType.Weapon => "weapon",
                    ItemType.Head => "head",
                    ItemType.Chest => "body",
                    ItemType.Feet => "feet",
                    _ => ""
                };
                
                // Get the previously equipped item (if any)
                var previousItem = player.EquipItem(item, slot);
                
                // Remove the new item from inventory
                inventory.RemoveAt(equipChoice - 1);
                
                // Destroy the previous item (do not add back to inventory)
                if (previousItem != null)
                {
                    Console.WriteLine($"Unequipped and destroyed {previousItem.Name}. Equipped {item.Name}.");
                }
                else
                {
                    Console.WriteLine($"Equipped {item.Name}.");
                }
            }
            else
            {
                Console.WriteLine("Invalid choice.");
            }
        }

        /// <summary>
        /// Unequips an item from a specific slot and adds it back to inventory
        /// </summary>
        public void UnequipItem()
        {
            UIManager.WriteMenuLine("Choose slot to unequip:");
            UIManager.WriteMenuLine("1. Weapon");
            UIManager.WriteMenuLine("2. Head");
            UIManager.WriteMenuLine("3. Body");
            UIManager.WriteMenuLine("4. Feet");
            UIManager.Write("Enter your choice: ");

            if (int.TryParse(Console.ReadLine(), out int slotChoice) && 
                slotChoice >= 1 && slotChoice <= 4)
            {
                string slot = slotChoice switch
                {
                    1 => "weapon",
                    2 => "head",
                    3 => "body",
                    4 => "feet",
                    _ => ""
                };
                
                var unequippedItem = player.UnequipItem(slot);
                if (unequippedItem != null)
                {
                    inventory.Add(unequippedItem); // Add the unequipped item back to inventory
                    Console.WriteLine($"Unequipped {unequippedItem.Name}.");
                }
                else
                {
                    Console.WriteLine($"No item was equipped in the {slot} slot.");
                }
            }
            else
            {
                Console.WriteLine("Invalid choice.");
            }
        }

        /// <summary>
        /// Discards an item from inventory permanently
        /// </summary>
        public void DiscardItem()
        {
            Console.Write("Enter the number of the item to discard: ");
            if (int.TryParse(Console.ReadLine(), out int discardChoice) && 
                discardChoice > 0 && discardChoice <= inventory.Count)
            {
                var item = inventory[discardChoice - 1];
                inventory.RemoveAt(discardChoice - 1);
                Console.WriteLine($"Discarded {item.Name}.");
            }
            else
            {
                Console.WriteLine("Invalid choice.");
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
            // Update the display manager with the new inventory reference
            displayManager = new InventoryDisplayManager(player, inventory);
        }
    }
}
