namespace RPGGame.Handlers.Inventory
{
    using System;

    /// <summary>
    /// Manages state tracking for multi-step inventory actions.
    /// Handles item selection, slot selection, and comparison choice states.
    /// </summary>
    public class InventoryStateManager
    {
        // State tracking for multi-step actions
        public bool WaitingForItemSelection { get; set; } = false;
        public bool WaitingForSlotSelection { get; set; } = false;
        public bool WaitingForComparisonChoice { get; set; } = false;
        public string ItemSelectionAction { get; set; } = "";
        public int SelectedItemIndex { get; set; } = -1;
        public string SelectedSlot { get; set; } = "";
        
        // Combo management state
        public bool InComboManagement { get; set; } = false;
        public bool WaitingForComboActionSelection { get; set; } = false;
        public bool WaitingForComboRemoveSelection { get; set; } = false;
        public bool WaitingForComboReorderInput { get; set; } = false;
        public string ReorderInputSequence { get; set; } = "";
        
        // Trade-up state
        public bool WaitingForRaritySelection { get; set; } = false;
        
        /// <summary>
        /// Resets all item action states
        /// </summary>
        public void ResetItemActionStates()
        {
            WaitingForItemSelection = false;
            WaitingForSlotSelection = false;
            WaitingForComparisonChoice = false;
            WaitingForRaritySelection = false;
            ItemSelectionAction = "";
            SelectedItemIndex = -1;
            SelectedSlot = "";
        }
        
        /// <summary>
        /// Resets all combo management states
        /// </summary>
        public void ResetComboStates()
        {
            InComboManagement = false;
            WaitingForComboActionSelection = false;
            WaitingForComboRemoveSelection = false;
            WaitingForComboReorderInput = false;
            ReorderInputSequence = "";
        }
        
        /// <summary>
        /// Checks if any selection state is active
        /// </summary>
        public bool IsAnySelectionActive()
        {
            return WaitingForItemSelection || WaitingForSlotSelection || WaitingForComparisonChoice || WaitingForRaritySelection;
        }
        
        /// <summary>
        /// Validates input during selection states
        /// </summary>
        public bool IsValidSelectionInput(string input, out string? errorMessage)
        {
            errorMessage = null;
            
            if (!IsAnySelectionActive())
                return true;
            
            if (!int.TryParse(input, out _))
            {
                if (WaitingForComparisonChoice)
                {
                    errorMessage = "Please enter 1 (keep current), 2 (equip new), or 0 (cancel).";
                }
                else
                {
                    errorMessage = "Please enter a number to select an item, or 0 to cancel.";
                }
                return false;
            }
            
            return true;
        }
    }
}

