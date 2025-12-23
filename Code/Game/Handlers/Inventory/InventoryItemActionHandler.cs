namespace RPGGame.Handlers.Inventory
{
    using System;
    using RPGGame.UI.Avalonia;

    /// <summary>
    /// Handles item action operations: equip, unequip, and discard.
    /// </summary>
    public class InventoryItemActionHandler
    {
        private readonly GameStateManager stateManager;
        private readonly IUIManager? customUIManager;
        private readonly InventoryStateManager stateTracker;
        
        // Event delegates
        public delegate void OnShowMessage(string message);
        public delegate void OnShowInventory();
        
        public event OnShowMessage? ShowMessageEvent;
        public event OnShowInventory? ShowInventoryEvent;
        
        public InventoryItemActionHandler(
            GameStateManager stateManager,
            IUIManager? customUIManager,
            InventoryStateManager stateTracker)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
            this.stateTracker = stateTracker ?? throw new ArgumentNullException(nameof(stateTracker));
        }
        
        /// <summary>
        /// Prompt user to equip an item
        /// </summary>
        public void PromptEquipItem()
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
            
            stateTracker.WaitingForItemSelection = true;
            stateTracker.ItemSelectionAction = "equip";
        }
        
        /// <summary>
        /// Prompt user to unequip an item
        /// </summary>
        public void PromptUnequipItem()
        {
            if (stateManager.CurrentPlayer == null) return;
            
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderSlotSelectionPrompt(stateManager.CurrentPlayer);
            }
            
            stateTracker.WaitingForSlotSelection = true;
        }
        
        /// <summary>
        /// Prompt user to discard an item
        /// </summary>
        public void PromptDiscardItem()
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
            
            stateTracker.WaitingForItemSelection = true;
            stateTracker.ItemSelectionAction = "discard";
        }
        
        /// <summary>
        /// Confirm equip choice after comparison
        /// </summary>
        public void ConfirmEquipItem(int itemIndex, string slot, bool equipNew)
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
                    // Add the previous item back to inventory instead of discarding it
                    stateManager.CurrentInventory.Add(previousItem);
                    ShowMessageEvent?.Invoke($"Unequipped {previousItem.Name}. Equipped {newItem.Name}.");
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
        public void UnequipItem(int slotChoice)
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
        public void DiscardItem(int itemIndex)
        {
            if (stateManager.CurrentPlayer == null || itemIndex < 0 || itemIndex >= stateManager.CurrentInventory.Count) return;
            
            var item = stateManager.CurrentInventory[itemIndex];
            stateManager.CurrentInventory.RemoveAt(itemIndex);
            ShowMessageEvent?.Invoke($"Discarded {item.Name}.");
            
            ShowInventoryEvent?.Invoke();
        }
    }
}

