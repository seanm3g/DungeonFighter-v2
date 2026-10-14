namespace RPGGame.Handlers.Inventory
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

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
        public bool WaitingForTradeUpConfirmation { get; set; } = false;
        public string? SelectedTradeUpRarity { get; set; } = null;
        public Item? PreviewTradeUpItem { get; set; } = null;

        /// <summary>True after choosing unequip/discard/trade-up (menus 2–4) until user confirms (1) or cancels (0/ESC). Equip skips this.</summary>
        public bool WaitingForMenuMutatingActionConfirmation { get; set; } = false;
        /// <summary>Pending main-menu choice: "1" equip, "2" unequip, "3" discard, "4" trade-up.</summary>
        public string PendingMutatingMenuChoice { get; set; } = "";

        public InventoryItemSortMode ItemSortMode { get; set; } = InventoryItemSortMode.InventoryOrder;
        public bool HideRequirementBlockedItems { get; set; } = false;

        /// <summary>
        /// When set, the main inventory list (and equip/discard item prompts) only show bag items that equip to this slot (<c>weapon</c>, <c>head</c>, etc.). Numpad / cycles this filter.
        /// </summary>
        public string? InventoryEquipSlotFilter { get; set; }

        private static readonly string[] InventoryEquipSlotFilterCycleOrder =
        {
            "weapon", "head", "body", "legs", "feet", "charm"
        };

        /// <summary>
        /// Advances <see cref="InventoryEquipSlotFilter"/> in order: weapon → head → body → legs → feet → charm → off → weapon…
        /// </summary>
        public string? CycleInventoryEquipSlotFilter()
        {
            int idx = InventoryEquipSlotFilter == null
                ? -1
                : Array.IndexOf(InventoryEquipSlotFilterCycleOrder, InventoryEquipSlotFilter);
            if (idx < 0)
                idx = -1;
            int next = idx + 1;
            InventoryEquipSlotFilter = next >= InventoryEquipSlotFilterCycleOrder.Length
                ? null
                : InventoryEquipSlotFilterCycleOrder[next];
            return InventoryEquipSlotFilter;
        }

        /// <summary>
        /// When set, the main inventory list (and equip/discard item prompts) only show bag items of this material (<see cref="Item.Material"/>). M cycles this filter.
        /// </summary>
        public string? InventoryMaterialFilter { get; set; }

        /// <summary>
        /// Advances <see cref="InventoryMaterialFilter"/> through the materials present in the bag
        /// (canonical <see cref="ItemMaterialRules.AllMaterials"/> order, then others alphabetically) → off → first…
        /// </summary>
        public string? CycleInventoryMaterialFilter(IEnumerable<Item>? inventory)
        {
            var materials = GetInventoryMaterials(inventory);
            int idx = InventoryMaterialFilter == null
                ? -1
                : materials.FindIndex(m => string.Equals(m, InventoryMaterialFilter, StringComparison.OrdinalIgnoreCase));
            int next = idx + 1;
            InventoryMaterialFilter = next >= materials.Count ? null : materials[next];
            return InventoryMaterialFilter;
        }

        /// <summary>Distinct non-empty materials in the bag, in canonical material order.</summary>
        public static List<string> GetInventoryMaterials(IEnumerable<Item>? inventory)
        {
            if (inventory == null)
                return new List<string>();
            return inventory
                .Select(item => item?.Material?.Trim() ?? "")
                .Where(m => m.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(m =>
                {
                    int i = Array.FindIndex(ItemMaterialRules.AllMaterials, a => string.Equals(a, m, StringComparison.OrdinalIgnoreCase));
                    return i < 0 ? int.MaxValue : i;
                })
                .ThenBy(m => m, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public InventoryItemSortMode CycleItemSortMode()
        {
            ItemSortMode = ItemSortMode switch
            {
                InventoryItemSortMode.InventoryOrder => InventoryItemSortMode.Rarity,
                InventoryItemSortMode.Rarity => InventoryItemSortMode.ItemSlot,
                _ => InventoryItemSortMode.InventoryOrder
            };

            return ItemSortMode;
        }
        
        /// <summary>
        /// Resets all item action states
        /// </summary>
        public void ResetItemActionStates()
        {
            WaitingForItemSelection = false;
            WaitingForSlotSelection = false;
            WaitingForComparisonChoice = false;
            WaitingForRaritySelection = false;
            WaitingForTradeUpConfirmation = false;
            WaitingForMenuMutatingActionConfirmation = false;
            PendingMutatingMenuChoice = "";
            ItemSelectionAction = "";
            SelectedItemIndex = -1;
            SelectedSlot = "";
            SelectedTradeUpRarity = null;
            PreviewTradeUpItem = null;
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
            return WaitingForItemSelection || WaitingForSlotSelection || WaitingForComparisonChoice || WaitingForRaritySelection || WaitingForTradeUpConfirmation || WaitingForMenuMutatingActionConfirmation;
        }
        
        /// <summary>
        /// Validates input during selection states
        /// </summary>
        public bool IsValidSelectionInput(string input, out string? errorMessage)
        {
            errorMessage = null;
            
            if (!IsAnySelectionActive())
                return true;

            if (WaitingForMenuMutatingActionConfirmation)
            {
                if (!int.TryParse(input, out int d) || (d != 0 && d != 1))
                {
                    errorMessage = "Press 1 to continue or 0 to cancel.";
                    return false;
                }
                return true;
            }
            
            if (!int.TryParse(input, out _))
            {
                if (WaitingForComparisonChoice)
                {
                    errorMessage = "Please enter 1 (keep current), 2 (equip new), 3 (lock/unlock trade-up), or 0 (cancel).";
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

