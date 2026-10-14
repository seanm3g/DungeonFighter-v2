namespace RPGGame.Handlers.Inventory
{
    using System;
    using RPGGame.UI.Avalonia;

    /// <summary>
    /// Handles item comparison logic for equip decisions.
    /// </summary>
    public class InventoryItemComparisonHandler
    {
        private readonly GameStateManager stateManager;
        private readonly IUIManager? customUIManager;
        private readonly InventoryStateManager stateTracker;
        
        // Event delegates
        public delegate void OnShowMessage(string message);
        public delegate void OnShowInventory();
        
        public event OnShowMessage? ShowMessageEvent;
        public event OnShowInventory? ShowInventoryEvent;
        
        public InventoryItemComparisonHandler(
            GameStateManager stateManager,
            IUIManager? customUIManager,
            InventoryStateManager stateTracker)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
            this.stateTracker = stateTracker ?? throw new ArgumentNullException(nameof(stateTracker));
        }
        
        /// <summary>
        /// Show item comparison screen before equipping
        /// </summary>
        public void ShowItemComparison(int itemIndex)
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
                ItemType.Legs => "legs",
                ItemType.Feet => "feet",
                ItemType.Charm => "charm",
                _ => ""
            };
            
            if (string.IsNullOrEmpty(slot))
            {
                ShowMessageEvent?.Invoke($"Cannot equip {newItem.Name}: Invalid item type.");
                ShowInventoryEvent?.Invoke();
                return;
            }

            var player = stateManager.CurrentPlayer;
            if (!newItem.MeetsRequirements(player))
            {
                // Still open the comparison so the item can be locked from trade-up; equipping it is refused by TryEquipItem.
                string? reason = newItem.GetEquipBlockedReason(player);
                ShowMessageEvent?.Invoke(string.IsNullOrEmpty(reason)
                    ? $"Cannot equip {newItem.Name} yet: attribute requirements not met."
                    : $"Cannot equip {newItem.Name} yet. {reason}");
            }

            Item? currentItem = player.Equipment.GetSlotItem(slot);
            
            // Store selection for later confirmation
            stateTracker.SelectedItemIndex = itemIndex;
            stateTracker.SelectedSlot = slot;
            stateTracker.WaitingForComparisonChoice = true;
            
            // Render comparison screen
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderItemComparison(player, newItem, currentItem, slot, itemIndex);
            }
        }
        
        /// <summary>
        /// Handles comparison choice input (1=old item, 2=new item, 3=toggle trade-up lock, 0=cancel)
        /// </summary>
        public bool HandleComparisonChoice(int comparisonChoice, InventoryItemActionHandler actionHandler)
        {
            stateTracker.WaitingForComparisonChoice = false;
            
            if (comparisonChoice == 0)
            {
                ShowMessageEvent?.Invoke("Cancelled.");
                ShowInventoryEvent?.Invoke();
                stateTracker.SelectedItemIndex = -1;
                stateTracker.SelectedSlot = "";
                return true;
            }
            
            if (comparisonChoice == 1)
            {
                // Keep old item
                actionHandler.ConfirmEquipItem(stateTracker.SelectedItemIndex, stateTracker.SelectedSlot, equipNew: false);
            }
            else if (comparisonChoice == 2)
            {
                // Equip new item
                actionHandler.ConfirmEquipItem(stateTracker.SelectedItemIndex, stateTracker.SelectedSlot, equipNew: true);
            }
            else if (comparisonChoice == 3)
            {
                ToggleTradeUpLock();
                stateTracker.WaitingForComparisonChoice = true;
                return true;
            }
            else
            {
                ShowMessageEvent?.Invoke("Invalid choice. Please select 1 (keep current), 2 (equip new), 3 (lock/unlock trade-up), or 0 (cancel).");
                stateTracker.WaitingForComparisonChoice = true;
                return false;
            }
            
            stateTracker.SelectedItemIndex = -1;
            stateTracker.SelectedSlot = "";
            return true;
        }

        /// <summary>
        /// Flips <see cref="Item.ExcludeFromTradeUp"/> on the item under comparison and redraws the comparison screen.
        /// </summary>
        private void ToggleTradeUpLock()
        {
            var player = stateManager.CurrentPlayer;
            int itemIndex = stateTracker.SelectedItemIndex;
            if (player == null || itemIndex < 0 || itemIndex >= stateManager.CurrentInventory.Count)
                return;

            var item = stateManager.CurrentInventory[itemIndex];
            item.ExcludeFromTradeUp = !item.ExcludeFromTradeUp;
            ShowMessageEvent?.Invoke(item.ExcludeFromTradeUp
                ? $"{item.Name} is locked and will not be used in trade-ups."
                : $"{item.Name} is unlocked and can be used in trade-ups.");

            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                Item? currentItem = player.Equipment.GetSlotItem(stateTracker.SelectedSlot);
                canvasUI.RenderItemComparison(player, item, currentItem, stateTracker.SelectedSlot, itemIndex);
            }
        }
    }
}

