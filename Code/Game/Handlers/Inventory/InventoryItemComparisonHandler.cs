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
            stateTracker.SelectedItemIndex = itemIndex;
            stateTracker.SelectedSlot = slot;
            stateTracker.WaitingForComparisonChoice = true;
            
            // Render comparison screen
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderItemComparison(stateManager.CurrentPlayer, newItem, currentItem, slot);
            }
        }
        
        /// <summary>
        /// Handles comparison choice input (1=old item, 2=new item, 0=cancel)
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
            else
            {
                ShowMessageEvent?.Invoke("Invalid choice. Please select 1 (keep current), 2 (equip new), or 0 (cancel).");
                stateTracker.WaitingForComparisonChoice = true;
                return false;
            }
            
            stateTracker.SelectedItemIndex = -1;
            stateTracker.SelectedSlot = "";
            return true;
        }
    }
}

