namespace RPGGame.Handlers.Inventory
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
        /// <param name="refreshInventoryScreen">When false, caller must refresh the inventory UI after any follow-up (e.g. combo add).</param>
        /// <param name="announce">When false, success messages are skipped (caller composes a single message).</param>
        public void ConfirmEquipItem(int itemIndex, string slot, bool equipNew, bool refreshInventoryScreen = true, bool announce = true)
        {
            if (stateManager.CurrentPlayer == null)
            {
                if (announce)
                    ShowMessageEvent?.Invoke("Error: No player character available.");
                if (refreshInventoryScreen)
                    ShowInventoryEvent?.Invoke();
                return;
            }
            
            if (itemIndex < 0 || itemIndex >= stateManager.CurrentInventory.Count)
            {
                if (announce)
                    ShowMessageEvent?.Invoke("Error: Invalid item selection.");
                if (refreshInventoryScreen)
                    ShowInventoryEvent?.Invoke();
                return;
            }
            
            var newItem = stateManager.CurrentInventory[itemIndex];
            
            if (equipNew)
            {
                var player = stateManager.CurrentPlayer;
                var comboBefore = player.GetComboActions().ToList();
                // Equip the new item
                var previousItem = player.EquipItem(newItem, slot);
                stateManager.CurrentInventory.RemoveAt(itemIndex);
                string pruneNote = FormatSequencePruneNote(comboBefore, player.GetComboActions());

                if (previousItem != null)
                    stateManager.CurrentInventory.Add(previousItem);

                if (announce)
                {
                    if (previousItem != null)
                        ShowMessageEvent?.Invoke($"Unequipped {previousItem.Name}. Equipped {newItem.Name}.{pruneNote}");
                    else
                        ShowMessageEvent?.Invoke($"Equipped {newItem.Name}.{pruneNote}");
                }
            }
            else
            {
                if (announce)
                    ShowMessageEvent?.Invoke("Kept current equipment.");
            }
            
            if (refreshInventoryScreen)
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
            
            var player = stateManager.CurrentPlayer;
            var comboBefore = player.GetComboActions().ToList();
            var unequippedItem = player.UnequipItem(slot);
            if (unequippedItem != null)
            {
                stateManager.CurrentInventory.Add(unequippedItem);
                string pruneNote = FormatSequencePruneNote(comboBefore, player.GetComboActions());
                ShowMessageEvent?.Invoke($"Unequipped {unequippedItem.Name}.{pruneNote}");
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

        private static string FormatSequencePruneNote(List<RPGGame.Action> before, List<RPGGame.Action> after)
        {
            if (before.Count == 0) return "";
            var afterSet = new HashSet<RPGGame.Action>(after);
            var removedNames = before.Where(a => !afterSet.Contains(a)).Select(a => a.Name).Distinct().ToList();
            if (removedNames.Count == 0) return "";
            return $" Removed from sequence (no longer in pool): {string.Join(", ", removedNames)}.";
        }
    }
}

