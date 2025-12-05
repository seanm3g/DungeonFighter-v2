namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
        private bool waitingForComparisonChoice = false;
        private string itemSelectionAction = "";
        private int selectedItemIndex = -1;
        private string selectedSlot = "";
        
        // Combo management state
        private bool inComboManagement = false;
        private bool waitingForComboActionSelection = false;
        private bool waitingForComboRemoveSelection = false;
        private bool waitingForComboReorderInput = false;
        private string reorderInputSequence = "";
        
        // Delegates
        public delegate void OnShowMessage(string message);
        public delegate void OnShowInventory();
        public delegate void OnShowGameLoop();
        public delegate void OnShowMainMenu();
        public delegate void OnExitGame();
        
        public event OnShowMessage? ShowMessageEvent;
        public event OnShowInventory? ShowInventoryEvent;
        public event OnShowGameLoop? ShowGameLoopEvent;
        public event OnShowMainMenu? ShowMainMenuEvent;
        public event OnExitGame? ExitGameEvent;

        public InventoryMenuHandler(GameStateManager stateManager, IUIManager? customUIManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
        }

        /// <summary>
        /// Display the inventory menu.
        /// 
        /// NOTE: This method now delegates to Game.ShowInventory() via the
        /// ShowInventoryEvent, which uses GameScreenCoordinator for rendering.
        /// This keeps all screen rendering logic centralized.
        /// </summary>
        public void ShowInventory()
        {
            // Trigger the event, which will call Game.ShowInventory(),
            // which delegates to GameScreenCoordinator.ShowInventory().
            // This ensures all inventory screen rendering goes through
            // the centralized coordinator.
            ShowInventoryEvent?.Invoke();
        }

        /// <summary>
        /// Handle inventory menu input
        /// </summary>
        public void HandleMenuInput(string input)
        {
            if (stateManager.CurrentPlayer == null) return;
            
            // Handle combo management input first
            if (inComboManagement)
            {
                HandleComboManagementInput(input);
                return;
            }
            
            // Handle comparison choice (1=new item, 2=old item, 0=cancel)
            if (waitingForComparisonChoice && int.TryParse(input, out int comparisonChoice))
            {
                waitingForComparisonChoice = false;
                
                if (comparisonChoice == 0)
                {
                    ShowMessageEvent?.Invoke("Cancelled.");
                    ShowInventoryEvent?.Invoke();
                    selectedItemIndex = -1;
                    selectedSlot = "";
                    return;
                }
                
                if (comparisonChoice == 1)
                {
                    // Equip new item
                    ConfirmEquipItem(selectedItemIndex, selectedSlot, equipNew: true);
                }
                else if (comparisonChoice == 2)
                {
                    // Keep old item
                    ConfirmEquipItem(selectedItemIndex, selectedSlot, equipNew: false);
                }
                else
                {
                    ShowMessageEvent?.Invoke("Invalid choice. Please select 1 (new item), 2 (old item), or 0 (cancel).");
                    waitingForComparisonChoice = true;
                    return;
                }
                
                selectedItemIndex = -1;
                selectedSlot = "";
                return;
            }
            
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
                    ShowItemComparison(itemIndex);
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
            
            // If waiting for item/slot selection or comparison choice but input is not numeric, ignore it
            // This prevents non-numeric keys from triggering normal menu actions
            if (waitingForItemSelection || waitingForSlotSelection || waitingForComparisonChoice)
            {
                // Only allow numeric input or cancel (0) when waiting for selection
                if (!int.TryParse(input, out _))
                {
                    // Show error message for invalid input during selection
                    if (waitingForComparisonChoice)
                    {
                        ShowMessageEvent?.Invoke("Please enter 1 (new item), 2 (old item), or 0 (cancel).");
                    }
                    else
                    {
                        ShowMessageEvent?.Invoke("Please enter a number to select an item, or 0 to cancel.");
                    }
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
                    // Exit game
                    ExitGameEvent?.Invoke();
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
        /// Show item comparison screen before equipping
        /// </summary>
        private void ShowItemComparison(int itemIndex)
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
            
            var newItem = stateManager.CurrentInventory[itemIndex];
            string slot = newItem.Type switch
            {
                ItemType.Weapon => "weapon",
                ItemType.Head => "head",
                ItemType.Chest => "body",
                ItemType.Feet => "feet",
                _ => ""
            };
            
            if (string.IsNullOrEmpty(slot))
            {
                ShowMessageEvent?.Invoke($"Cannot equip {newItem.Name}: Invalid item type.");
                ShowInventoryEvent?.Invoke();
                return;
            }
            
            // Get currently equipped item for this slot
            Item? currentItem = slot switch
            {
                "weapon" => stateManager.CurrentPlayer.Weapon,
                "head" => stateManager.CurrentPlayer.Head,
                "body" => stateManager.CurrentPlayer.Body,
                "feet" => stateManager.CurrentPlayer.Feet,
                _ => null
            };
            
            // Store selection for later confirmation
            selectedItemIndex = itemIndex;
            selectedSlot = slot;
            waitingForComparisonChoice = true;
            
            // Render comparison screen
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderItemComparison(stateManager.CurrentPlayer, newItem, currentItem, slot);
            }
        }
        
        /// <summary>
        /// Confirm equip choice after comparison
        /// </summary>
        private void ConfirmEquipItem(int itemIndex, string slot, bool equipNew)
        {
            if (stateManager.CurrentPlayer == null)
            {
                ShowMessageEvent?.Invoke("Error: No player character available.");
                ShowInventoryEvent?.Invoke();
                return;
            }
            
            if (itemIndex < 0 || itemIndex >= stateManager.CurrentInventory.Count)
            {
                ShowMessageEvent?.Invoke("Error: Invalid item selection.");
                ShowInventoryEvent?.Invoke();
                return;
            }
            
            var newItem = stateManager.CurrentInventory[itemIndex];
            
            if (equipNew)
            {
                // Equip the new item
                var previousItem = stateManager.CurrentPlayer.EquipItem(newItem, slot);
                stateManager.CurrentInventory.RemoveAt(itemIndex);
                
                if (previousItem != null)
                {
                    ShowMessageEvent?.Invoke($"Unequipped and destroyed {previousItem.Name}. Equipped {newItem.Name}.");
                }
                else
                {
                    ShowMessageEvent?.Invoke($"Equipped {newItem.Name}.");
                }
            }
            else
            {
                // Keep the old item, do nothing
                ShowMessageEvent?.Invoke("Kept current equipment.");
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
        /// Show combo management menu
        /// </summary>
        private void ShowComboManagement()
        {
            if (stateManager.CurrentPlayer == null) return;
            
            inComboManagement = true;
            RenderComboManagementScreen();
        }
        
        /// <summary>
        /// Helper method to render combo management screen
        /// </summary>
        private void RenderComboManagementScreen()
        {
            if (stateManager.CurrentPlayer == null) return;
            
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderComboManagement(stateManager.CurrentPlayer);
            }
        }
        
        /// <summary>
        /// Handle input for combo management menu
        /// </summary>
        private void HandleComboManagementInput(string input)
        {
            if (stateManager.CurrentPlayer == null) return;
            
            // Handle action selection for adding to combo
            if (waitingForComboActionSelection && int.TryParse(input, out int actionIndex))
            {
                waitingForComboActionSelection = false;
                
                if (actionIndex == 0)
                {
                    ShowMessageEvent?.Invoke("Cancelled.");
                    inComboManagement = true;
                    RenderComboManagementScreen();
                    return;
                }
                
                var actionPool = stateManager.CurrentPlayer.GetActionPool();
                if (actionIndex >= 1 && actionIndex <= actionPool.Count)
                {
                    var action = actionPool[actionIndex - 1];
                    stateManager.CurrentPlayer.AddToCombo(action);
                    ShowMessageEvent?.Invoke($"Added {action.Name} to combo sequence.");
                }
                else
                {
                    ShowMessageEvent?.Invoke("Invalid action selection.");
                }
                
                inComboManagement = true;
                RenderComboManagementScreen();
                return;
            }
            
            // Handle action removal
            if (waitingForComboRemoveSelection && int.TryParse(input, out int removeIndex))
            {
                waitingForComboRemoveSelection = false;
                
                if (removeIndex == 0)
                {
                    ShowMessageEvent?.Invoke("Cancelled.");
                    inComboManagement = true;
                    RenderComboManagementScreen();
                    return;
                }
                
                var comboActions = stateManager.CurrentPlayer.GetComboActions();
                if (removeIndex >= 1 && removeIndex <= comboActions.Count)
                {
                    var action = comboActions[removeIndex - 1];
                    stateManager.CurrentPlayer.RemoveFromCombo(action);
                    ShowMessageEvent?.Invoke($"Removed {action.Name} from combo sequence.");
                }
                else
                {
                    ShowMessageEvent?.Invoke("Invalid action selection.");
                }
                
                inComboManagement = true;
                RenderComboManagementScreen();
                return;
            }
            
            // Handle reorder input
            if (waitingForComboReorderInput)
            {
                var comboActions = stateManager.CurrentPlayer.GetComboActions();
                
                // Check if user wants to confirm with 0
                if (input == "0")
                {
                    waitingForComboReorderInput = false;
                    
                    if (string.IsNullOrEmpty(reorderInputSequence))
                    {
                        ShowMessageEvent?.Invoke("Cancelled.");
                        reorderInputSequence = "";
                        inComboManagement = true;
                        RenderComboManagementScreen();
                        return;
                    }
                    
                    // Validate and apply the accumulated sequence
                    if (ValidateReorderInput(reorderInputSequence, comboActions.Count))
                    {
                        if (ApplyReorder(reorderInputSequence, comboActions))
                        {
                            ShowMessageEvent?.Invoke("Combo sequence reordered successfully!");
                        }
                        else
                        {
                            ShowMessageEvent?.Invoke("Error: Failed to reorder combo sequence.");
                        }
                    }
                    else
                    {
                        ShowMessageEvent?.Invoke($"Invalid input. Please enter numbers 1-{comboActions.Count} in any order (e.g., 15324).");
                    }
                    
                    reorderInputSequence = "";
                    inComboManagement = true;
                    RenderComboManagementScreen();
                    return;
                }
                
                // Handle cancel
                if (input.ToLower() == "cancel")
                {
                    waitingForComboReorderInput = false;
                    reorderInputSequence = "";
                    ShowMessageEvent?.Invoke("Cancelled.");
                    inComboManagement = true;
                    RenderComboManagementScreen();
                    return;
                }
                
                // Accumulate numeric input
                if (input.Length == 1 && char.IsDigit(input[0]))
                {
                    int digit = int.Parse(input);
                    // Only accept digits that are valid for the combo (1 to comboActions.Count)
                    if (digit >= 1 && digit <= comboActions.Count)
                    {
                        // Check if we've already entered all required digits
                        if (reorderInputSequence.Length >= comboActions.Count)
                        {
                            ShowMessageEvent?.Invoke($"You've entered all {comboActions.Count} digits. Press 0 to confirm.");
                            return;
                        }
                        
                        // Check if this digit is already in the sequence
                        if (!reorderInputSequence.Contains(input))
                        {
                            reorderInputSequence += input;
                            // Re-render to show the current sequence
                            if (customUIManager is CanvasUICoordinator canvasUI)
                            {
                                canvasUI.RenderComboReorderPrompt(stateManager.CurrentPlayer, reorderInputSequence);
                            }
                            
                            // If we've entered all digits, suggest confirming
                            if (reorderInputSequence.Length >= comboActions.Count)
                            {
                                ShowMessageEvent?.Invoke("All digits entered. Press 0 to confirm.");
                            }
                        }
                        else
                        {
                            ShowMessageEvent?.Invoke($"Number {input} is already in the sequence. Each number can only be used once.");
                        }
                    }
                    else
                    {
                        ShowMessageEvent?.Invoke($"Please enter numbers between 1 and {comboActions.Count}.");
                    }
                }
                else
                {
                    ShowMessageEvent?.Invoke("Please enter a single digit (1-9) or press 0 to confirm.");
                }
                
                return;
            }
            
            // Normal combo management menu actions
            switch (input)
            {
                case "1":
                    PromptAddActionToCombo();
                    break;
                case "2":
                    PromptRemoveActionFromCombo();
                    break;
                case "3":
                    PromptReorderComboActions();
                    break;
                case "4":
                    AddAllAvailableActionsToCombo();
                    break;
                case "5":
                    inComboManagement = false;
                    ShowInventoryEvent?.Invoke();
                    break;
                default:
                    ShowMessageEvent?.Invoke("Invalid choice. Press 1-5.");
                    break;
            }
        }
        
        /// <summary>
        /// Prompt user to add an action to combo
        /// </summary>
        private void PromptAddActionToCombo()
        {
            if (stateManager.CurrentPlayer == null) return;
            
            var actionPool = stateManager.CurrentPlayer.GetActionPool();
            if (actionPool.Count == 0)
            {
                ShowMessageEvent?.Invoke("No actions available to add to combo.");
                inComboManagement = true;
                RenderComboManagementScreen();
                return;
            }
            
            waitingForComboActionSelection = true;
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderComboActionSelection(stateManager.CurrentPlayer, "add");
            }
        }
        
        /// <summary>
        /// Prompt user to remove an action from combo
        /// </summary>
        private void PromptRemoveActionFromCombo()
        {
            if (stateManager.CurrentPlayer == null) return;
            
            var comboActions = stateManager.CurrentPlayer.GetComboActions();
            if (comboActions.Count == 0)
            {
                ShowMessageEvent?.Invoke("No actions in combo sequence to remove.");
                inComboManagement = true;
                RenderComboManagementScreen();
                return;
            }
            
            waitingForComboRemoveSelection = true;
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderComboActionSelection(stateManager.CurrentPlayer, "remove");
            }
        }
        
        /// <summary>
        /// Prompt user to reorder combo actions
        /// </summary>
        private void PromptReorderComboActions()
        {
            if (stateManager.CurrentPlayer == null) return;
            
            var comboActions = stateManager.CurrentPlayer.GetComboActions();
            if (comboActions.Count < 2)
            {
                ShowMessageEvent?.Invoke("You need at least 2 actions to reorder them.");
                inComboManagement = true;
                RenderComboManagementScreen();
                return;
            }
            
            waitingForComboReorderInput = true;
            reorderInputSequence = "";
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderComboReorderPrompt(stateManager.CurrentPlayer, "");
            }
        }
        
        /// <summary>
        /// Add all available actions to combo
        /// </summary>
        private void AddAllAvailableActionsToCombo()
        {
            if (stateManager.CurrentPlayer == null) return;
            
            var actionPool = stateManager.CurrentPlayer.GetActionPool();
            var comboActions = stateManager.CurrentPlayer.GetComboActions();
            
            if (actionPool.Count == 0)
            {
                ShowMessageEvent?.Invoke("No actions available in action pool to add to combo.");
                inComboManagement = true;
                RenderComboManagementScreen();
                return;
            }
            
            var actionsToAdd = actionPool.Where(action => 
                !comboActions.Any(comboAction => comboAction.Name == action.Name)).ToList();
            
            if (actionsToAdd.Count == 0)
            {
                ShowMessageEvent?.Invoke("All available actions are already in your combo sequence.");
                inComboManagement = true;
                RenderComboManagementScreen();
                return;
            }
            
            int addedCount = 0;
            foreach (var action in actionsToAdd)
            {
                stateManager.CurrentPlayer.AddToCombo(action);
                addedCount++;
            }
            
            ShowMessageEvent?.Invoke($"Successfully added {addedCount} actions to combo sequence!");
            inComboManagement = true;
            RenderComboManagementScreen();
        }
        
        /// <summary>
        /// Validates the reorder input string
        /// </summary>
        private bool ValidateReorderInput(string input, int actionCount)
        {
            if (string.IsNullOrEmpty(input))
                return false;
                
            // Check if input contains only digits
            if (!input.All(char.IsDigit))
                return false;
                
            // Check if all numbers 1 to actionCount are present exactly once
            var numbers = input.Select(c => int.Parse(c.ToString())).ToList();
            
            if (numbers.Count != actionCount)
                return false;
                
            // Check if all numbers from 1 to actionCount are present
            for (int i = 1; i <= actionCount; i++)
            {
                if (!numbers.Contains(i))
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Applies the reorder to the combo sequence
        /// </summary>
        private bool ApplyReorder(string input, List<Action> currentComboActions)
        {
            if (stateManager.CurrentPlayer == null) return false;
            
            try
            {
                var newOrder = input.Select(c => int.Parse(c.ToString())).ToList();
                
                // Create a new list with actions in the specified order
                var reorderedActions = new List<Action>();
                for (int i = 0; i < newOrder.Count; i++)
                {
                    int actionIndex = newOrder[i] - 1; // Convert to 0-based index
                    if (actionIndex >= 0 && actionIndex < currentComboActions.Count)
                    {
                        reorderedActions.Add(currentComboActions[actionIndex]);
                    }
                }
                
                if (reorderedActions.Count != currentComboActions.Count)
                {
                    return false;
                }
                
                // Clear current combo (this sets ComboOrder to 0 on removed actions)
                foreach (var action in currentComboActions)
                {
                    stateManager.CurrentPlayer.RemoveFromCombo(action);
                }
                
                // Set ComboOrder values AFTER removing but BEFORE adding
                // This ensures that when AddToCombo calls ReorderComboSequence, it sorts correctly
                for (int i = 0; i < reorderedActions.Count; i++)
                {
                    reorderedActions[i].ComboOrder = i + 1;
                }
                
                // Add actions back in the desired order
                // Since we've set ComboOrder values, ReorderComboSequence will maintain the order
                foreach (var action in reorderedActions)
                {
                    stateManager.CurrentPlayer.AddToCombo(action);
                }
                
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

