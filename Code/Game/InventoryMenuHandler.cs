namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using RPGGame.UI.Avalonia;
    using RPGGame.Handlers.Inventory;

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
        public delegate void OnShowMainMenu();
        
        public event OnShowMessage? ShowMessageEvent;
        public event OnShowInventory? ShowInventoryEvent;
        public event OnShowGameLoop? ShowGameLoopEvent;
        public event OnShowMainMenu? ShowMainMenuEvent;

        public InventoryMenuHandler(GameStateManager stateManager, IUIManager? customUIManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
            
            // Initialize managers
            this.stateTracker = new InventoryStateManager();
            this.itemActionHandler = new InventoryItemActionHandler(stateManager, customUIManager, stateTracker);
            this.itemComparisonHandler = new InventoryItemComparisonHandler(stateManager, customUIManager, stateTracker);
            this.comboManager = new InventoryComboManager(stateManager, customUIManager, stateTracker);
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
        /// Handle inventory menu input
        /// </summary>
        public void HandleMenuInput(string input)
        {
            if (stateManager.CurrentPlayer == null) return;
            
            // Handle combo management input first
            if (stateTracker.InComboManagement)
            {
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
            if (stateTracker.WaitingForItemSelection && int.TryParse(input, out int itemIndex))
            {
                stateTracker.WaitingForItemSelection = false;
                
                if (itemIndex == 0)
                {
                    ShowMessageEvent?.Invoke("Cancelled.");
                    ShowInventoryEvent?.Invoke();
                    return;
                }
                
                itemIndex--; // Convert 1-based to 0-based
                
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
                
                if (rarityChoice == 0)
                {
                    ShowMessageEvent?.Invoke("Cancelled.");
                    ShowInventoryEvent?.Invoke();
                    return;
                }
                
                // Get available rarities
                var rarityGroups = stateManager.CurrentInventory
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
                    ShowInventoryEvent?.Invoke();
                }
                return;
            }
            
            // Handle trade-up confirmation
            if (stateTracker.WaitingForTradeUpConfirmation && int.TryParse(input, out int confirmationChoice))
            {
                stateTracker.WaitingForTradeUpConfirmation = false;
                
                if (confirmationChoice == 1 && !string.IsNullOrEmpty(stateTracker.SelectedTradeUpRarity))
                {
                    // Confirm trade-up
                    tradeUpHandler.PerformTradeUp(stateTracker.SelectedTradeUpRarity);
                    stateTracker.SelectedTradeUpRarity = null;
                }
                else if (confirmationChoice == 0)
                {
                    // Cancel trade-up
                    ShowMessageEvent?.Invoke("Trade-up cancelled.");
                    stateTracker.SelectedTradeUpRarity = null;
                    stateTracker.PreviewTradeUpItem = null;
                    ShowInventoryEvent?.Invoke();
                }
                else
                {
                    ShowMessageEvent?.Invoke("Invalid choice. Press 1 to confirm or 0 to cancel.");
                }
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
            
            // Normal menu actions
            switch (input)
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
                    comboManager.ShowComboManagement();
                    break;
                case "5":
                    tradeUpHandler.PromptTradeUp();
                    break;
                case "6":
                    stateManager.TransitionToState(GameState.GameLoop);
                    ShowGameLoopEvent?.Invoke();
                    break;
                case "0":
                    // Return to main menu
                    stateManager.TransitionToState(GameState.MainMenu);
                    ShowMainMenuEvent?.Invoke();
                    break;
                default:
                    ShowMessageEvent?.Invoke("Invalid choice. Press 1-6, 0 (Return to Main Menu), or ESC to go back.");
                    break;
            }
        }
        
        /// <summary>
        /// Gets the order index of a rarity (for sorting)
        /// </summary>
        private int GetRarityOrder(string rarity)
        {
            var rarityOrder = new[] { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic", "Transcendent" };
            int index = Array.IndexOf(rarityOrder, rarity);
            return index < 0 ? 0 : index;
        }
        
        /// <summary>
        /// Gets the next rarity tier in progression
        /// </summary>
        private string? GetNextRarity(string currentRarity)
        {
            var rarityOrder = new[] { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic", "Transcendent" };
            
            int currentIndex = Array.IndexOf(rarityOrder, currentRarity);
            if (currentIndex < 0 || currentIndex >= rarityOrder.Length - 1)
            {
                return null; // Not found or already at max
            }
            
            return rarityOrder[currentIndex + 1];
        }

    }
}

