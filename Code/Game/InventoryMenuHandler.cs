namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using RPGGame.UI.Avalonia;
    using RPGGame.UI.Avalonia.Renderers.Inventory;
    using RPGGame.Handlers.Inventory;
    using static RPGGame.Handlers.Inventory.ComboPointerInput;
    using static RPGGame.UI.Avalonia.UIConstants.MenuOptions;

    /// <summary>
    /// Handles inventory menu display and item management.
    /// Facade coordinator that delegates to specialized managers.
    /// 
    /// Responsibilities:
    /// - Display inventory
    /// - Coordinate item equipping/unequipping/discarding
    /// - Coordinate multi-step inventory actions
    /// - Coordinate combo management
    /// </summary>
    public class InventoryMenuHandler
    {
        private GameStateManager stateManager;
        private IUIManager? customUIManager;
        
        // Specialized managers using composition pattern
        private readonly InventoryStateManager stateTracker;
        private readonly InventoryItemActionHandler itemActionHandler;
        private readonly InventoryItemComparisonHandler itemComparisonHandler;
        private readonly InventoryComboManager comboManager;
        private readonly InventoryTradeUpHandler tradeUpHandler;
        
        // Delegates
        public delegate void OnShowMessage(string message);
        public delegate void OnShowInventory();
        public delegate void OnShowGameLoop();
        
        public event OnShowMessage? ShowMessageEvent;
        public event OnShowInventory? ShowInventoryEvent;
        public event OnShowGameLoop? ShowGameLoopEvent;

        public InventoryMenuHandler(GameStateManager stateManager, IUIManager? customUIManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
            
            // Initialize managers
            this.stateTracker = new InventoryStateManager();
            this.itemActionHandler = new InventoryItemActionHandler(stateManager, customUIManager, stateTracker);
            this.itemComparisonHandler = new InventoryItemComparisonHandler(stateManager, customUIManager, stateTracker);
            this.comboManager = new InventoryComboManager(stateManager, customUIManager, stateTracker, itemActionHandler);
            this.tradeUpHandler = new InventoryTradeUpHandler(stateManager, customUIManager, stateTracker);
            
            // Wire up events
            itemActionHandler.ShowMessageEvent += (msg) => ShowMessageEvent?.Invoke(msg);
            itemActionHandler.ShowInventoryEvent += () => ShowInventoryEvent?.Invoke();
            itemComparisonHandler.ShowMessageEvent += (msg) => ShowMessageEvent?.Invoke(msg);
            itemComparisonHandler.ShowInventoryEvent += () => ShowInventoryEvent?.Invoke();
            comboManager.ShowMessageEvent += (msg) => ShowMessageEvent?.Invoke(msg);
            comboManager.ShowInventoryEvent += () => ShowInventoryEvent?.Invoke();
            tradeUpHandler.ShowMessageEvent += (msg) => ShowMessageEvent?.Invoke(msg);
            tradeUpHandler.ShowInventoryEvent += () => ShowInventoryEvent?.Invoke();
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
        /// Mouse: right-click a filled action strip card in inventory removes that sequence slot.
        /// Uses the same path as right-panel <see cref="ComboPointerInput.Kind.SequenceRemove"/> (<c>cpi:rm:N</c>):
        /// weapon-required basics stay protected; when not in combo management, equip/compare/trade flows block this
        /// (same as other <c>cpi:</c> tokens).
        /// </summary>
        /// <returns>True when the remove path ran (including when removal was refused for weapon rules).</returns>
        public bool TryHandleStripRightClickRemove(int slotIndex)
        {
            if (stateManager.CurrentState != GameState.Inventory || stateManager.CurrentPlayer == null)
                return false;

            var combo = stateManager.CurrentPlayer.GetComboActions();
            if (combo == null || combo.Count == 0 || slotIndex < 0 || slotIndex >= combo.Count)
                return false;

            if (!stateTracker.InComboManagement && stateTracker.IsAnySelectionActive())
                return false;

            comboManager.HandleComboPointerInput($"{Prefix}rm:{slotIndex}");
            return true;
        }

        /// <summary>
        /// Re-renders inventory or combo management after left-panel chrome changes (e.g. collapsing STATS).
        /// <see cref="RenderCoordinator.PerformRender"/> suppresses display-buffer rendering in <see cref="GameState.Inventory"/>,
        /// so <c>ForceFullLayoutRender</c> does not redraw CanvasRenderer content or the action-info strip.
        /// </summary>
        public void RefreshInventoryScreen()
        {
            var player = stateManager.CurrentPlayer ?? stateManager.GetActiveCharacter();
            if (player == null || customUIManager is not CanvasUICoordinator canvasUI)
                return;
            if (stateManager.CurrentState != GameState.Inventory)
                return;
            if (stateTracker.InComboManagement)
                canvasUI.RenderComboManagement(player);
            else
            {
                var inv = stateManager.CurrentInventory ?? player.Inventory ?? new List<Item>();
                if (stateTracker.WaitingForComparisonChoice
                    && stateTracker.SelectedItemIndex >= 0
                    && stateTracker.SelectedItemIndex < inv.Count
                    && !string.IsNullOrEmpty(stateTracker.SelectedSlot))
                {
                    var newItem = inv[stateTracker.SelectedItemIndex];
                    Item? currentItem = stateTracker.SelectedSlot switch
                    {
                        "weapon" => player.Weapon,
                        "head" => player.Head,
                        "body" => player.Body,
                        "legs" => player.Legs,
                        "feet" => player.Feet,
                        _ => null
                    };
                    canvasUI.RenderItemComparison(player, newItem, currentItem, stateTracker.SelectedSlot, stateTracker.SelectedItemIndex);
                }
                else if (stateTracker.WaitingForItemSelection)
                {
                    string prompt = stateTracker.ItemSelectionAction == "discard"
                        ? "Select Item to Discard"
                        : "Select Item to Equip";
                    canvasUI.RenderItemSelectionPrompt(
                        player,
                        inv,
                        prompt,
                        stateTracker.ItemSelectionAction,
                        stateTracker.ItemSortMode,
                        stateTracker.HideRequirementBlockedItems,
                        stateTracker.InventoryEquipSlotFilter);
                }
                else if (stateTracker.WaitingForSlotSelection)
                    canvasUI.RenderSlotSelectionPrompt(player);
                else
                    canvasUI.RenderInventory(
                        player,
                        inv,
                        stateTracker.WaitingForMenuMutatingActionConfirmation ? stateTracker.PendingMutatingMenuChoice : null,
                        stateTracker.ItemSortMode,
                        stateTracker.HideRequirementBlockedItems,
                        stateTracker.InventoryEquipSlotFilter);
            }
        }

        /// <summary>
        /// Handle inventory menu input
        /// </summary>
        public void HandleMenuInput(string input)
        {
            if (!string.IsNullOrEmpty(input))
                input = input.Trim();
            if (string.IsNullOrEmpty(input))
                return;
            if (stateManager.CurrentPlayer == null) return;

            if (TryHandleInventoryViewShortcut(input))
                return;

            if (TryHandleInventoryItemScrollInput(input))
                return;
            
            // Combo management screen: mouse tokens before keyboard handler
            if (stateTracker.InComboManagement)
            {
                if (!string.IsNullOrEmpty(input) && input.StartsWith(Prefix, StringComparison.Ordinal))
                {
                    comboManager.HandleComboPointerInput(input);
                    return;
                }
                comboManager.HandleComboManagementInput(input);
                return;
            }
            
            // Handle comparison choice (1=old item, 2=new item, 0=cancel)
            if (stateTracker.WaitingForComparisonChoice && int.TryParse(input, out int comparisonChoice))
            {
                itemComparisonHandler.HandleComparisonChoice(comparisonChoice, itemActionHandler);
                return;
            }
            
            // Handle multi-step actions (item selection, slot selection)
            if (stateTracker.WaitingForItemSelection && int.TryParse(input, out int itemChoice))
            {
                if (itemChoice == 0)
                {
                    stateTracker.WaitingForItemSelection = false;
                    ShowMessageEvent?.Invoke("Cancelled.");
                    ShowInventoryEvent?.Invoke();
                    return;
                }
                
                if (!TryResolveVisibleItemChoice(itemChoice, out int itemIndex, out string selectionError))
                {
                    ShowMessageEvent?.Invoke(selectionError);
                    RefreshInventoryScreen();
                    return;
                }
                stateTracker.WaitingForItemSelection = false;
                
                if (stateTracker.ItemSelectionAction == "equip")
                {
                    itemComparisonHandler.ShowItemComparison(itemIndex);
                }
                else if (stateTracker.ItemSelectionAction == "discard")
                {
                    itemActionHandler.DiscardItem(itemIndex);
                }
                
                stateTracker.ItemSelectionAction = "";
                return;
            }
            
            if (stateTracker.WaitingForSlotSelection && int.TryParse(input, out int slotChoice))
            {
                stateTracker.WaitingForSlotSelection = false;
                
                if (slotChoice == 0)
                {
                    ShowMessageEvent?.Invoke("Cancelled.");
                    ShowInventoryEvent?.Invoke();
                    return;
                }
                
                itemActionHandler.UnequipItem(slotChoice);
                return;
            }
            
            // Handle rarity selection for trade-up
            if (stateTracker.WaitingForRaritySelection && int.TryParse(input, out int rarityChoice))
            {
                stateTracker.WaitingForRaritySelection = false;
                
                // No cancel option - must complete trade-up once started
                if (rarityChoice == 0)
                {
                    ShowMessageEvent?.Invoke("You must select a rarity to trade up. Cannot cancel.");
                    stateTracker.WaitingForRaritySelection = true; // Keep waiting
                    return;
                }
                
                // Get available rarities
                var rarityGroups = stateManager.CurrentInventory
                    .Where(item => item != null && item.Type != ItemType.Consumable)
                    .GroupBy(item => item.Rarity ?? "Common")
                    .Where(group => group.Count() >= 5)
                    .OrderBy(group => GetRarityOrder(group.Key))
                    .Where(group => GetNextRarity(group.Key) != null)
                    .ToList();
                
                if (rarityChoice > 0 && rarityChoice <= rarityGroups.Count)
                {
                    var selectedRarity = rarityGroups[rarityChoice - 1].Key;
                    tradeUpHandler.ShowTradeUpPreview(selectedRarity);
                }
                else
                {
                    ShowMessageEvent?.Invoke("Invalid rarity selection.");
                    stateTracker.WaitingForRaritySelection = true; // Keep waiting
                }
                return;
            }
            
            // Handle trade-up confirmation
            if (stateTracker.WaitingForTradeUpConfirmation && int.TryParse(input, out int confirmationChoice))
            {
                if (confirmationChoice == 1 && !string.IsNullOrEmpty(stateTracker.SelectedTradeUpRarity))
                {
                    // Confirm trade-up
                    stateTracker.WaitingForTradeUpConfirmation = false;
                    tradeUpHandler.PerformTradeUp(stateTracker.SelectedTradeUpRarity);
                    stateTracker.SelectedTradeUpRarity = null;
                }
                else if (confirmationChoice == 0)
                {
                    // No cancel option - must complete trade-up once started
                    ShowMessageEvent?.Invoke("You must confirm the trade-up. Press 1 to confirm.");
                    // Keep waiting for confirmation
                }
                else
                {
                    ShowMessageEvent?.Invoke("Invalid choice. Press 1 to confirm the trade-up.");
                }
                return;
            }

            // Confirm equip / unequip / discard / trade-up menu choice before starting sub-flows
            if (stateTracker.WaitingForMenuMutatingActionConfirmation)
            {
                if (!int.TryParse(input, out int menuConfirmDigit))
                {
                    ShowMessageEvent?.Invoke("Press 1 to continue or 0 to cancel.");
                    return;
                }
                if (menuConfirmDigit == 0)
                {
                    stateTracker.WaitingForMenuMutatingActionConfirmation = false;
                    stateTracker.PendingMutatingMenuChoice = "";
                    ShowMessageEvent?.Invoke("Cancelled.");
                    ShowInventoryEvent?.Invoke();
                    return;
                }
                if (menuConfirmDigit != 1)
                {
                    ShowMessageEvent?.Invoke("Press 1 to continue or 0 to cancel.");
                    return;
                }
                string pendingMenu = stateTracker.PendingMutatingMenuChoice;
                stateTracker.WaitingForMenuMutatingActionConfirmation = false;
                stateTracker.PendingMutatingMenuChoice = "";
                switch (pendingMenu)
                {
                    case "1":
                        itemActionHandler.PromptEquipItem();
                        break;
                    case "2":
                        itemActionHandler.PromptUnequipItem();
                        break;
                    case "3":
                        itemActionHandler.PromptDiscardItem();
                        break;
                    case "4":
                        tradeUpHandler.PromptTradeUp();
                        break;
                }
                return;
            }

            // Mouse: right-panel sequence/pool (cpi:…) — ignore during equip/compare/trade flows
            if (!string.IsNullOrEmpty(input) && input.StartsWith(Prefix, StringComparison.Ordinal))
            {
                if (stateTracker.IsAnySelectionActive())
                    return;
                comboManager.HandleComboPointerInput(input);
                return;
            }
            
            // If waiting for item/slot selection or comparison choice but input is not numeric, ignore it
            // This prevents non-numeric keys from triggering normal menu actions
            if (stateTracker.IsAnySelectionActive())
            {
                // Only allow numeric input or cancel (0) when waiting for selection
                if (!stateTracker.IsValidSelectionInput(input, out string? errorMessage))
                {
                    if (errorMessage != null)
                    {
                        ShowMessageEvent?.Invoke(errorMessage);
                    }
                    return;
                }
            }
            
            // Normal menu actions: equip opens item selection immediately; other mutating actions use WaitingForMenuMutatingActionConfirmation.
            switch (input)
            {
                case "*":
                    itemActionHandler.AutoEquipEmptySlots();
                    break;
                case "1":
                case "2":
                case "3":
                case "4":
                    if (stateTracker.WaitingForComparisonChoice || stateTracker.WaitingForItemSelection
                        || stateTracker.WaitingForSlotSelection || stateTracker.WaitingForRaritySelection
                        || stateTracker.WaitingForTradeUpConfirmation)
                        return;
                    if (input == "1")
                    {
                        itemActionHandler.PromptEquipItem();
                        break;
                    }
                    stateTracker.WaitingForMenuMutatingActionConfirmation = true;
                    stateTracker.PendingMutatingMenuChoice = input;
                    ShowMessageEvent?.Invoke(GetMutatingMenuConfirmMessage(input));
                    ShowInventoryEvent?.Invoke();
                    break;
                case "5":
                    if (stateTracker.WaitingForComparisonChoice || stateTracker.WaitingForItemSelection
                        || stateTracker.WaitingForSlotSelection || stateTracker.WaitingForRaritySelection
                        || stateTracker.WaitingForTradeUpConfirmation)
                        return;
                    RequestLabSnapshotCapture();
                    break;
                case "0":
                    stateManager.TransitionToState(GameState.GameLoop);
                    ShowGameLoopEvent?.Invoke();
                    break;
                default:
                    ShowMessageEvent?.Invoke("Invalid choice. Press 1-5, * (auto-equip), + (sort), - (requirements filter), / (cycle slot filter), 0 (Return to game menu), or ESC to go back.");
                    break;
            }
        }

        private bool TryHandleInventoryViewShortcut(string input)
        {
            string normalized = input.Trim();
            if (normalized != "+" && normalized != "-" && normalized != "/")
                return false;

            bool allowSlotCycle = CanCycleInventorySlotFilterShortcut();
            bool allowSortAndReqFilter = IsMainInventoryListVisible();

            if (normalized == "/")
            {
                if (!allowSlotCycle)
                    return true;
            }
            else
            {
                if (!allowSortAndReqFilter)
                    return true;
            }

            if (customUIManager is CanvasUICoordinator canvasUI)
                canvasUI.ResetInventoryItemScroll();

            if (normalized == "+")
            {
                var sortMode = stateTracker.CycleItemSortMode();
                ShowMessageEvent?.Invoke($"Inventory sort: {GetInventorySortLabel(sortMode)}.");
            }
            else if (normalized == "-")
            {
                stateTracker.HideRequirementBlockedItems = !stateTracker.HideRequirementBlockedItems;
                ShowMessageEvent?.Invoke(stateTracker.HideRequirementBlockedItems
                    ? "Inventory filter: hiding items with unmet requirements."
                    : "Inventory filter: showing all items.");
            }
            else
            {
                stateTracker.CycleInventoryEquipSlotFilter();
                string? slot = stateTracker.InventoryEquipSlotFilter;
                ShowMessageEvent?.Invoke(slot == null
                    ? "Inventory bag slot filter: off (all equipment types visible)."
                    : $"Inventory bag slot filter: {InventoryScreenRenderer.GetInventoryEquipSlotFilterDisplayName(slot)} only.");
            }

            RefreshInventoryScreen();
            return true;
        }

        private bool IsMainInventoryListVisible()
        {
            return !stateTracker.InComboManagement
                && !stateTracker.WaitingForItemSelection
                && !stateTracker.WaitingForSlotSelection
                && !stateTracker.WaitingForComparisonChoice
                && !stateTracker.WaitingForRaritySelection
                && !stateTracker.WaitingForTradeUpConfirmation
                && !stateTracker.WaitingForMenuMutatingActionConfirmation;
        }

        /// <summary>
        /// Numpad / may cycle the bag slot filter on the main inventory list and during equip/discard item pick (same filtered rows).
        /// </summary>
        private bool CanCycleInventorySlotFilterShortcut()
        {
            return !stateTracker.InComboManagement
                && !stateTracker.WaitingForSlotSelection
                && !stateTracker.WaitingForComparisonChoice
                && !stateTracker.WaitingForRaritySelection
                && !stateTracker.WaitingForTradeUpConfirmation
                && !stateTracker.WaitingForMenuMutatingActionConfirmation;
        }

        private bool TryResolveVisibleItemChoice(int visibleChoice, out int inventoryIndex, out string errorMessage)
        {
            inventoryIndex = -1;
            errorMessage = "";
            var player = stateManager.CurrentPlayer;
            var inventory = stateManager.CurrentInventory;
            if (player == null || inventory == null || visibleChoice <= 0)
            {
                errorMessage = "Invalid item selection. Choose an item shown above, or 0 to cancel.";
                return false;
            }

            var visibleSelectionEntries = InventoryScreenRenderer.BuildDisplayEntries(
                inventory,
                player,
                stateTracker.ItemSortMode,
                stateTracker.HideRequirementBlockedItems,
                stateTracker.InventoryEquipSlotFilter);

            int selectableCount = Math.Min(visibleSelectionEntries.Count, ItemSelectionRenderer.MaxSelectableItems);
            if (visibleChoice > selectableCount)
            {
                errorMessage = "Invalid item selection. Choose an item shown above, or 0 to cancel.";
                return false;
            }

            inventoryIndex = visibleSelectionEntries[visibleChoice - 1].InventoryIndex;
            if (inventoryIndex < 0 || inventoryIndex >= inventory.Count)
            {
                errorMessage = "Invalid item selection. Choose an item shown above, or 0 to cancel.";
                return false;
            }

            return true;
        }

        private static string GetInventorySortLabel(InventoryItemSortMode sortMode) => sortMode switch
        {
            InventoryItemSortMode.Rarity => "Rarity",
            InventoryItemSortMode.ItemSlot => "Item slot",
            _ => "Inventory order"
        };

        private bool TryHandleInventoryItemScrollInput(string input)
        {
            if (!TryGetInventoryItemScrollDelta(input, out int itemDelta))
                return false;

            bool mainInventoryListVisible =
                !stateTracker.InComboManagement
                && !stateTracker.WaitingForItemSelection
                && !stateTracker.WaitingForSlotSelection
                && !stateTracker.WaitingForComparisonChoice
                && !stateTracker.WaitingForRaritySelection
                && !stateTracker.WaitingForTradeUpConfirmation;

            if (mainInventoryListVisible && customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.AdjustInventoryItemScroll(itemDelta);
                RefreshInventoryScreen();
            }

            return true;
        }

        private static bool TryGetInventoryItemScrollDelta(string input, out int itemDelta)
        {
            itemDelta = 0;
            switch (input.Trim().ToLowerInvariant())
            {
                case "up":
                    itemDelta = -InventoryItemScrollLayout.ScrollStepItems;
                    return true;
                case "down":
                    itemDelta = InventoryItemScrollLayout.ScrollStepItems;
                    return true;
                case "pageup":
                    itemDelta = -InventoryItemScrollLayout.PageScrollStepItems;
                    return true;
                case "pagedown":
                    itemDelta = InventoryItemScrollLayout.PageScrollStepItems;
                    return true;
                default:
                    return false;
            }
        }

        private void RequestLabSnapshotCapture()
        {
            if (customUIManager is not CanvasUICoordinator canvasUI)
            {
                ShowMessageEvent?.Invoke("Snapshot requires the Avalonia UI.");
                return;
            }

            var game = canvasUI.GetGame();
            if (game == null)
            {
                ShowMessageEvent?.Invoke("Snapshot failed: game not available.");
                return;
            }

            _ = game.CaptureLabCharacterSnapshotAsync(canvasUI);
        }

        /// <summary>
        /// When true, the user chose unequip / discard / trade-up and must press 1 to continue or 0/ESC to cancel (equip skips this step).
        /// </summary>
        public bool IsWaitingForMenuMutatingActionConfirmation => stateTracker.WaitingForMenuMutatingActionConfirmation;

        /// <summary>
        /// Cancels the pending main-menu mutating action (equip/unequip/discard/trade-up) and refreshes inventory UI.
        /// </summary>
        public bool TryCancelMutatingInventoryMenuActionConfirmation()
        {
            if (!stateTracker.WaitingForMenuMutatingActionConfirmation)
                return false;
            stateTracker.WaitingForMenuMutatingActionConfirmation = false;
            stateTracker.PendingMutatingMenuChoice = "";
            ShowMessageEvent?.Invoke("Cancelled.");
            ShowInventoryEvent?.Invoke();
            return true;
        }

        private static string GetMutatingMenuActionLabel(string choice) => choice switch
        {
            "1" => EquipItem,
            "2" => UnequipItem,
            "3" => DiscardItem,
            "4" => TradeUpItems,
            _ => "this action"
        };

        private static string GetMutatingMenuConfirmMessage(string choice)
        {
            string label = GetMutatingMenuActionLabel(choice);
            return $"{label} can change your equipment or inventory. Press 1 to continue or 0 to cancel.";
        }
        
        /// <summary>
        /// Checks if the player is currently in a trade-up flow (cannot go back)
        /// </summary>
        public bool IsInTradeUpFlow()
        {
            return stateTracker.WaitingForRaritySelection || stateTracker.WaitingForTradeUpConfirmation;
        }
        
        /// <summary>
        /// Gets the order index of a rarity (for sorting)
        /// </summary>
        private int GetRarityOrder(string rarity)
        {
            var rarityOrder = new[] { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic" };
            int index = Array.IndexOf(rarityOrder, rarity);
            return index < 0 ? 0 : index;
        }
        
        /// <summary>
        /// Gets the next rarity tier in progression (only one tier higher)
        /// Returns null if already at maximum rarity
        /// </summary>
        private string? GetNextRarity(string currentRarity)
        {
            var rarityOrder = new[] { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic" };
            
            // Use case-insensitive comparison to find the current rarity
            int currentIndex = -1;
            for (int i = 0; i < rarityOrder.Length; i++)
            {
                if (string.Equals(rarityOrder[i], currentRarity, StringComparison.OrdinalIgnoreCase))
                {
                    currentIndex = i;
                    break;
                }
            }
            
            // Only return the next tier (one step up), or null if at max
            if (currentIndex < 0 || currentIndex >= rarityOrder.Length - 1)
            {
                return null; // Not found or already at max
            }
            
            return rarityOrder[currentIndex + 1];
        }

    }
}

