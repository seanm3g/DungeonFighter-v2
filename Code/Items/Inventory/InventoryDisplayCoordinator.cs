using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Handles display coordination for inventory management
    /// Extracted from InventoryManager to separate display logic
    /// </summary>
    public class InventoryDisplayCoordinator
    {
        private readonly GameDisplayManager displayManager;
        private readonly ComboManager comboManager;
        private readonly InventoryOperations operations;

        public InventoryDisplayCoordinator(
            Character player,
            List<Item> inventory,
            InventoryOperations operations)
        {
            this.displayManager = new GameDisplayManager(player, inventory);
            this.comboManager = new ComboManager(player, displayManager);
            this.operations = operations ?? throw new ArgumentNullException(nameof(operations));
        }

        /// <summary>
        /// Shows the main gear menu and handles user input
        /// </summary>
        /// <returns>True if user wants to continue to dungeon, false if returning to main menu, null if exiting game</returns>
        public bool? ShowGearMenu()
        {
            while (true)
            {
                displayManager.ShowMainDisplay();

                if (int.TryParse(Console.ReadLine(), out int choice))
                {
                    switch (choice)
                    {
                        case 1:
                            operations.EquipItem();
                            break;
                        case 2:
                            operations.UnequipItem();
                            break;
                        case 3:
                            operations.DiscardItem();
                            break;
                        case 4:
                            comboManager.ManageComboActions();
                            break;
                        case 5:
                            operations.TradeUpItems();
                            break;
                        case 6:
                            return true; // Continue to dungeon
                        case 0:
                            // Return to main menu
                            return false; // Return to main menu
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
        /// Updates the display manager with new inventory
        /// </summary>
        public void UpdateInventory(Character player, List<Item> inventory)
        {
            // Display manager will be recreated with new inventory
            // This is handled by the parent InventoryManager
        }
    }
}

