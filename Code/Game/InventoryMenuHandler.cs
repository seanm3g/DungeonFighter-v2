namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using RPGGame.UI.Avalonia;

    /// <summary>
    /// Handles inventory menu display and item management.
    /// Extracted from Game.cs to manage all inventory operations.
    /// 
    /// Responsibilities:
    /// - Display inventory
    /// - Handle item equipping/unequipping/discarding
    /// - Manage multi-step inventory actions
    /// - Combo management
    /// </summary>
    public class InventoryMenuHandler
    {
        private GameStateManager stateManager;
        private IUIManager? customUIManager;
        
        // State tracking for multi-step actions
        private bool waitingForItemSelection = false;
        private bool waitingForSlotSelection = false;
        private string itemSelectionAction = "";
        
        // Delegates
        public delegate void OnShowMessage(string message);
        public delegate void OnShowInventory();
        public delegate void OnShowGameLoop();
        public delegate void OnShowMainMenu();
        
        public event OnShowMessage? ShowMessageEvent;
        public event OnShowInventory? ShowInventoryEvent;
        public event OnShowGameLoop? ShowGameLoopEvent;
        public event OnShowMainMenu? ShowMainMenuEvent;

        public InventoryMenuHandler(GameStateManager stateManager, IUIManager? customUIManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
        }

        /// <summary>
        /// Display the inventory menu
        /// </summary>
        public void ShowInventory()
        {
            if (customUIManager is CanvasUICoordinator canvasUI && stateManager.CurrentPlayer != null)
            {
                // Clear dungeon/room context when transitioning to inventory
                canvasUI.ClearCurrentEnemy();
                canvasUI.SetDungeonName(null);
                canvasUI.SetRoomName(null);
                
                canvasUI.SetCharacter(stateManager.CurrentPlayer);
                canvasUI.RenderInventory(stateManager.CurrentPlayer, stateManager.CurrentInventory);
            }
            stateManager.TransitionToState(GameState.Inventory);
        }

        /// <summary>
        /// Handle inventory menu input
        /// </summary>
        public void HandleMenuInput(string input)
        {
            if (stateManager.CurrentPlayer == null) return;
            
            // Handle multi-step actions (item selection, slot selection)
            if (waitingForItemSelection && int.TryParse(input, out int itemIndex))
            {
                waitingForItemSelection = false;
                
                if (itemIndex == 0)
                {
                    ShowMessageEvent?.Invoke("Cancelled.");
                    ShowInventoryEvent?.Invoke();
                    return;
                }
                
                itemIndex--; // Convert 1-based to 0-based
                
                if (itemSelectionAction == "equip")
                {
                    EquipItem(itemIndex);
                }
                else if (itemSelectionAction == "discard")
                {
                    DiscardItem(itemIndex);
                }
                
                itemSelectionAction = "";
                return;
            }
            
            if (waitingForSlotSelection && int.TryParse(input, out int slotChoice))
            {
                waitingForSlotSelection = false;
                
                if (slotChoice == 0)
                {
                    ShowMessageEvent?.Invoke("Cancelled.");
                    ShowInventoryEvent?.Invoke();
                    return;
                }
                
                UnequipItem(slotChoice);
                return;
            }
            
            // If waiting for item/slot selection but input is not numeric, ignore it
            // This prevents non-numeric keys from triggering normal menu actions
            if (waitingForItemSelection || waitingForSlotSelection)
            {
                // Only allow numeric input or cancel (0) when waiting for selection
                if (!int.TryParse(input, out _))
                {
                    // Show error message for invalid input during selection
                    ShowMessageEvent?.Invoke("Please enter a number to select an item, or 0 to cancel.");
                    return;
                }
            }
            
            // Normal menu actions
            switch (input)
            {
                case "1":
                    PromptEquipItem();
                    break;
                case "2":
                    PromptUnequipItem();
                    break;
                case "3":
                    PromptDiscardItem();
                    break;
                case "4":
                    ShowComboManagement();
                    break;
                case "5":
                    stateManager.TransitionToState(GameState.GameLoop);
                    ShowGameLoopEvent?.Invoke();
                    break;
                case "6":
                    stateManager.TransitionToState(GameState.MainMenu);
                    ShowMainMenuEvent?.Invoke();
                    break;
                case "0":
                    // Exit handled by Game.cs
                    break;
                default:
                    ShowMessageEvent?.Invoke("Invalid choice. Press 1-6, 0, or ESC to go back.");
                    break;
            }
        }

        /// <summary>
        /// Prompt user to equip an item
        /// </summary>
        private void PromptEquipItem()
        {
            if (stateManager.CurrentPlayer == null) return;
            
            if (stateManager.CurrentInventory.Count == 0)
            {
                ShowMessageEvent?.Invoke("No items in inventory to equip.");
                ShowInventoryEvent?.Invoke();
                return;
            }
            
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderItemSelectionPrompt(stateManager.CurrentPlayer, stateManager.CurrentInventory, "Select Item to Equip", "equip");
            }
            
            waitingForItemSelection = true;
            itemSelectionAction = "equip";
        }

        /// <summary>
        /// Prompt user to unequip an item
        /// </summary>
        private void PromptUnequipItem()
        {
            if (stateManager.CurrentPlayer == null) return;
            
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderSlotSelectionPrompt(stateManager.CurrentPlayer);
            }
            
            waitingForSlotSelection = true;
        }

        /// <summary>
        /// Prompt user to discard an item
        /// </summary>
        private void PromptDiscardItem()
        {
            if (stateManager.CurrentPlayer == null) return;
            
            if (stateManager.CurrentInventory.Count == 0)
            {
                ShowMessageEvent?.Invoke("No items in inventory to discard.");
                ShowInventoryEvent?.Invoke();
                return;
            }
            
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderItemSelectionPrompt(stateManager.CurrentPlayer, stateManager.CurrentInventory, "Select Item to Discard", "discard");
            }
            
            waitingForItemSelection = true;
            itemSelectionAction = "discard";
        }

        /// <summary>
        /// Equip a specific item
        /// </summary>
        private void EquipItem(int itemIndex)
        {
            if (stateManager.CurrentPlayer == null)
            {
                ShowMessageEvent?.Invoke("Error: No player character available.");
                ShowInventoryEvent?.Invoke();
                return;
            }
            
            if (itemIndex < 0 || itemIndex >= stateManager.CurrentInventory.Count)
            {
                ShowMessageEvent?.Invoke($"Invalid item selection. Please choose a number between 1 and {stateManager.CurrentInventory.Count}.");
                ShowInventoryEvent?.Invoke();
                return;
            }
            
            var item = stateManager.CurrentInventory[itemIndex];
            string slot = item.Type switch
            {
                ItemType.Weapon => "weapon",
                ItemType.Head => "head",
                ItemType.Chest => "body",
                ItemType.Feet => "feet",
                _ => ""
            };
            
            if (string.IsNullOrEmpty(slot))
            {
                ShowMessageEvent?.Invoke($"Cannot equip {item.Name}: Invalid item type.");
                ShowInventoryEvent?.Invoke();
                return;
            }
            
            var previousItem = stateManager.CurrentPlayer.EquipItem(item, slot);
            stateManager.CurrentInventory.RemoveAt(itemIndex);
            
            if (previousItem != null)
            {
                ShowMessageEvent?.Invoke($"Unequipped and destroyed {previousItem.Name}. Equipped {item.Name}.");
            }
            else
            {
                ShowMessageEvent?.Invoke($"Equipped {item.Name}.");
            }
            
            ShowInventoryEvent?.Invoke();
        }

        /// <summary>
        /// Unequip an item from a specific slot
        /// </summary>
        private void UnequipItem(int slotChoice)
        {
            if (stateManager.CurrentPlayer == null) return;
            
            string slot = slotChoice switch
            {
                1 => "weapon",
                2 => "head",
                3 => "body",
                4 => "feet",
                _ => ""
            };
            
            if (string.IsNullOrEmpty(slot))
            {
                ShowMessageEvent?.Invoke("Invalid slot choice.");
                ShowInventoryEvent?.Invoke();
                return;
            }
            
            var unequippedItem = stateManager.CurrentPlayer.UnequipItem(slot);
            if (unequippedItem != null)
            {
                stateManager.CurrentInventory.Add(unequippedItem);
                ShowMessageEvent?.Invoke($"Unequipped {unequippedItem.Name}.");
            }
            else
            {
                ShowMessageEvent?.Invoke($"No item was equipped in the {slot} slot.");
            }
            
            ShowInventoryEvent?.Invoke();
        }

        /// <summary>
        /// Discard a specific item
        /// </summary>
        private void DiscardItem(int itemIndex)
        {
            if (stateManager.CurrentPlayer == null || itemIndex < 0 || itemIndex >= stateManager.CurrentInventory.Count) return;
            
            var item = stateManager.CurrentInventory[itemIndex];
            stateManager.CurrentInventory.RemoveAt(itemIndex);
            ShowMessageEvent?.Invoke($"Discarded {item.Name}.");
            
            ShowInventoryEvent?.Invoke();
        }

        /// <summary>
        /// Show combo management (future feature)
        /// </summary>
        private void ShowComboManagement()
        {
            if (stateManager.CurrentPlayer == null) return;
            
            ShowMessageEvent?.Invoke("Combo Management - Coming soon!\nPress any key to return to inventory.");
            ShowInventoryEvent?.Invoke();
        }
    }
}

