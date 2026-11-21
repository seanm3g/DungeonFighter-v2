namespace RPGGame.Handlers.Inventory
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using RPGGame.UI.Avalonia;

    /// <summary>
    /// Handles trade-up functionality: trading 5 items of the same rarity for 1 item of the next higher rarity
    /// </summary>
    public class InventoryTradeUpHandler
    {
        private readonly GameStateManager stateManager;
        private readonly IUIManager? customUIManager;
        private readonly InventoryStateManager stateTracker;
        
        // Event delegates
        public delegate void OnShowMessage(string message);
        public delegate void OnShowInventory();
        
        public event OnShowMessage? ShowMessageEvent;
        public event OnShowInventory? ShowInventoryEvent;
        
        public InventoryTradeUpHandler(
            GameStateManager stateManager,
            IUIManager? customUIManager,
            InventoryStateManager stateTracker)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
            this.stateTracker = stateTracker ?? throw new ArgumentNullException(nameof(stateTracker));
        }
        
        /// <summary>
        /// Items trade-up may consume: non-consumables the player has not locked.
        /// </summary>
        public static bool IsTradeUpCandidate(Item? item) =>
            item != null && item.Type != ItemType.Consumable && !item.ExcludeFromTradeUp;

        /// <summary>
        /// True when <paramref name="inventory"/> holds 5 unlocked items of one rarity below the maximum
        /// (what <see cref="PromptTradeUp"/> needs to offer a choice). The hub grays out its Trade Up tab otherwise.
        /// </summary>
        public static bool CanTradeUp(IEnumerable<Item>? inventory) =>
            inventory != null && inventory
                .Where(IsTradeUpCandidate)
                .GroupBy(item => item.Rarity ?? "Common")
                .Any(group => group.Count() >= 5 && GetNextRarity(group.Key) != null);

        /// <summary>
        /// Prompts user to select a rarity to trade up
        /// </summary>
        public void PromptTradeUp()
        {
            if (stateManager.CurrentPlayer == null) return;
            
            if (stateManager.CurrentInventory.Count < 5)
            {
                ShowMessageEvent?.Invoke("You need at least 5 items in your inventory to trade up.");
                ShowInventoryEvent?.Invoke();
                return;
            }
            
            // Group items by rarity and find rarities with at least 5 items
            var rarityGroups = stateManager.CurrentInventory
                .Where(IsTradeUpCandidate)
                .GroupBy(item => item.Rarity ?? "Common")
                .Where(group => group.Count() >= 5)
                .OrderBy(group => GetRarityOrder(group.Key))
                .ToList();
            
            if (rarityGroups.Count == 0)
            {
                ShowMessageEvent?.Invoke("You need at least 5 unlocked items of the same rarity to trade up.");
                ShowInventoryEvent?.Invoke();
                return;
            }
            
            // Check if any rarity can be traded up (not already at max)
            var tradeableRarities = rarityGroups
                .Where(group => GetNextRarity(group.Key) != null)
                .ToList();
            
            if (tradeableRarities.Count == 0)
            {
                ShowMessageEvent?.Invoke("You have items at the maximum rarity. Cannot trade up further.");
                ShowInventoryEvent?.Invoke();
                return;
            }
            
            // If only one tradeable rarity, show preview directly
            if (tradeableRarities.Count == 1)
            {
                ShowTradeUpPreview(tradeableRarities[0].Key);
                return;
            }
            
            // Multiple rarities available - show selection
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderRaritySelectionPrompt(stateManager.CurrentPlayer, tradeableRarities);
            }
            
            stateTracker.WaitingForRaritySelection = true;
        }
        
        /// <summary>
        /// Shows the trade-up preview screen for a specific rarity
        /// </summary>
        public void ShowTradeUpPreview(string rarity)
        {
            if (stateManager.CurrentPlayer == null) return;
            
            var nextRarity = GetNextRarity(rarity);
            if (nextRarity == null)
            {
                ShowMessageEvent?.Invoke($"Cannot trade up {rarity} items - already at maximum rarity.");
                ShowInventoryEvent?.Invoke();
                return;
            }
            
            // Get 5 items of the specified rarity
            var itemsToTrade = stateManager.CurrentInventory
                .Where(IsTradeUpCandidate)
                .Where(item => (item.Rarity ?? "Common").Equals(rarity, StringComparison.OrdinalIgnoreCase))
                .Take(5)
                .ToList();
            
            if (itemsToTrade.Count < 5)
            {
                ShowMessageEvent?.Invoke($"Not enough {rarity} items to trade up. Need 5, have {itemsToTrade.Count}.");
                ShowInventoryEvent?.Invoke();
                return;
            }
            
            // Calculate average tier and level from the items being traded
            int averageTier = (int)Math.Round(itemsToTrade.Average(item => item.Tier));
            int averageLevel = (int)Math.Round(itemsToTrade.Average(item => item.Level));
            
            // Determine item type (use the most common type from the traded items)
            bool isWeapon = itemsToTrade.Count(item => item.Type == ItemType.Weapon) >= 3;
            
            // Generate preview item with next rarity (don't add to inventory yet)
            Item? previewItem = GenerateItemWithRarity(averageTier, averageLevel, isWeapon, nextRarity, stateManager.CurrentPlayer);
            
            if (previewItem == null)
            {
                ShowMessageEvent?.Invoke("Failed to generate trade-up preview. Please try again.");
                ShowInventoryEvent?.Invoke();
                return;
            }
            
            // Show preview screen
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderTradeUpPreview(stateManager.CurrentPlayer, itemsToTrade, previewItem, rarity, nextRarity);
            }
            
            // Store state for confirmation (including the preview item so we use the same one)
            stateTracker.WaitingForTradeUpConfirmation = true;
            stateTracker.SelectedTradeUpRarity = rarity;
            stateTracker.PreviewTradeUpItem = previewItem;
        }
        
        /// <summary>
        /// Redraws the open trade-up prompt (rarity pick or the stored preview) without rolling a new preview. Hover and
        /// left-panel refreshes call this so a tooltip over the hero panel does not swap the prompt for the plain inventory.
        /// Returns false when no trade-up prompt is open.
        /// </summary>
        public bool RefreshOpenPrompt()
        {
            if (stateManager.CurrentPlayer == null || customUIManager is not CanvasUICoordinator canvasUI)
                return false;
            if (stateTracker.WaitingForTradeUpConfirmation
                && stateTracker.PreviewTradeUpItem is { } preview
                && !string.IsNullOrEmpty(stateTracker.SelectedTradeUpRarity)
                && GetNextRarity(stateTracker.SelectedTradeUpRarity) is { } next)
            {
                string rarity = stateTracker.SelectedTradeUpRarity;
                var itemsToTrade = stateManager.CurrentInventory
                    .Where(IsTradeUpCandidate)
                    .Where(item => (item.Rarity ?? "Common").Equals(rarity, StringComparison.OrdinalIgnoreCase))
                    .Take(5)
                    .ToList();
                canvasUI.RenderTradeUpPreview(stateManager.CurrentPlayer, itemsToTrade, preview, rarity, next);
                return true;
            }
            if (stateTracker.WaitingForRaritySelection)
            {
                var tradeable = stateManager.CurrentInventory
                    .Where(IsTradeUpCandidate)
                    .GroupBy(item => item.Rarity ?? "Common")
                    .Where(group => group.Count() >= 5)
                    .OrderBy(group => GetRarityOrder(group.Key))
                    .Where(group => GetNextRarity(group.Key) != null)
                    .ToList();
                canvasUI.RenderRaritySelectionPrompt(stateManager.CurrentPlayer, tradeable);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Performs the trade-up operation for a specific rarity (called after confirmation)
        /// </summary>
        public void PerformTradeUp(string rarity)
        {
            if (stateManager.CurrentPlayer == null) return;
            
            var nextRarity = GetNextRarity(rarity);
            if (nextRarity == null)
            {
                ShowMessageEvent?.Invoke($"Cannot trade up {rarity} items - already at maximum rarity.");
                ShowInventoryEvent?.Invoke();
                return;
            }
            
            // Get 5 items of the specified rarity
            var itemsToTrade = stateManager.CurrentInventory
                .Where(IsTradeUpCandidate)
                .Where(item => (item.Rarity ?? "Common").Equals(rarity, StringComparison.OrdinalIgnoreCase))
                .Take(5)
                .ToList();
            
            if (itemsToTrade.Count < 5)
            {
                ShowMessageEvent?.Invoke($"Not enough {rarity} items to trade up. Need 5, have {itemsToTrade.Count}.");
                ShowInventoryEvent?.Invoke();
                return;
            }
            
            // Use the preview item that was shown to the user (ensures consistency)
            Item? newItem = stateTracker.PreviewTradeUpItem;
            
            if (newItem == null)
            {
                // Fallback: generate new item if preview wasn't stored (shouldn't happen in normal flow)
                int averageTier = (int)Math.Round(itemsToTrade.Average(item => item.Tier));
                int averageLevel = (int)Math.Round(itemsToTrade.Average(item => item.Level));
                bool isWeapon = itemsToTrade.Count(item => item.Type == ItemType.Weapon) >= 3;
                newItem = GenerateItemWithRarity(averageTier, averageLevel, isWeapon, nextRarity, stateManager.CurrentPlayer);
                
                if (newItem == null)
                {
                    ShowMessageEvent?.Invoke("Failed to generate trade-up item. Please try again.");
                    ShowInventoryEvent?.Invoke();
                    return;
                }
            }
            
            // Remove the 5 items from inventory
            foreach (var item in itemsToTrade)
            {
                stateManager.CurrentInventory.Remove(item);
            }
            
            // Add the new item to inventory
            stateManager.CurrentInventory.Add(newItem);
            
            // Clear the preview item from state
            stateTracker.PreviewTradeUpItem = null;
            
            ShowMessageEvent?.Invoke($"Traded up 5 {rarity} items for 1 {nextRarity} {newItem.Name}!");
            ShowInventoryEvent?.Invoke();
        }
        

        /// <summary>Trade-up result: one item of <paramref name="rarity"/> at the traded items' tier and level.</summary>
        private static Item? GenerateItemWithRarity(int tier, int level, bool isWeapon, string rarity, Character? player) =>
            LootGenerator.GenerateTradeUp(tier, level, isWeapon, rarity, player);

        /// <summary>
        /// Gets the next rarity tier in progression (only one tier higher)
        /// Returns null if already at maximum rarity
        /// </summary>
        private static string? GetNextRarity(string currentRarity)
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
        
        /// <summary>
        /// Gets the order index of a rarity (for sorting)
        /// </summary>
        private int GetRarityOrder(string rarity)
        {
            var rarityOrder = new[] { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic" };
            int index = Array.IndexOf(rarityOrder, rarity);
            return index < 0 ? 0 : index;
        }
    }
}

