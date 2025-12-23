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
                .GroupBy(item => item.Rarity ?? "Common")
                .Where(group => group.Count() >= 5)
                .OrderBy(group => GetRarityOrder(group.Key))
                .ToList();
            
            if (rarityGroups.Count == 0)
            {
                ShowMessageEvent?.Invoke("You need at least 5 items of the same rarity to trade up.");
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
            
            // If only one tradeable rarity, proceed directly
            if (tradeableRarities.Count == 1)
            {
                PerformTradeUp(tradeableRarities[0].Key);
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
        /// Performs the trade-up operation for a specific rarity
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
            
            // Generate new item with next rarity
            Item? newItem = GenerateItemWithRarity(averageTier, averageLevel, isWeapon, nextRarity, stateManager.CurrentPlayer);
            
            if (newItem == null)
            {
                ShowMessageEvent?.Invoke("Failed to generate trade-up item. Please try again.");
                ShowInventoryEvent?.Invoke();
                return;
            }
            
            // Remove the 5 items from inventory
            foreach (var item in itemsToTrade)
            {
                stateManager.CurrentInventory.Remove(item);
            }
            
            // Add the new item to inventory
            stateManager.CurrentInventory.Add(newItem);
            
            ShowMessageEvent?.Invoke($"Traded up 5 {rarity} items for 1 {nextRarity} {newItem.Name}!");
            ShowInventoryEvent?.Invoke();
        }
        
        /// <summary>
        /// Generates an item with a specific rarity
        /// </summary>
        private Item? GenerateItemWithRarity(int tier, int level, bool isWeapon, string rarity, Character? player)
        {
            var dataCache = LootDataCache.Load();
            var random = new Random();
            
            // Get rarity data
            var rarityData = dataCache.RarityData?.FirstOrDefault(
                r => r.Name.Equals(rarity, StringComparison.OrdinalIgnoreCase));
            
            if (rarityData == null)
            {
                // Fallback to default rarity data
                rarityData = new RarityData
                {
                    Name = rarity,
                    StatBonuses = 2,
                    ActionBonuses = 1,
                    Modifications = 1
                };
            }
            
            // Select item
            var itemSelector = new LootItemSelector(dataCache, random);
            Item? item = itemSelector.SelectItem(tier, isWeapon);
            
            if (item == null)
            {
                return null;
            }
            
            // Set level and tier
            item.Level = level;
            item.Tier = tier;
            item.Rarity = rarity;
            
            // Apply scaling
            var tuning = GameConfiguration.Instance;
            ApplyItemScaling(item, tuning);
            
            // Apply rarity scaling
            var rarityProcessor = new LootRarityProcessor(dataCache, random);
            rarityProcessor.ApplyRarityScaling(item, rarityData);
            
            // Apply bonuses
            var bonusApplier = new LootBonusApplier(dataCache, random);
            var context = LootContext.Create(player, null, null);
            
            if (item is WeaponItem weapon)
            {
                context.WeaponType = weapon.WeaponType.ToString();
            }
            
            bonusApplier.ApplyBonuses(item, rarityData, context);
            
            return item;
        }
        
        /// <summary>
        /// Applies scaling formulas to base stats based on item type and tier
        /// </summary>
        private void ApplyItemScaling(Item item, GameConfiguration tuning)
        {
            if (item is WeaponItem weapon)
            {
                var equipmentScaling = tuning.EquipmentScaling;
                if (equipmentScaling != null)
                {
                    weapon.BonusDamage = (int)(weapon.Tier * equipmentScaling.WeaponDamagePerTier);
                    weapon.BonusAttackSpeed = (int)(weapon.Tier * equipmentScaling.SpeedBonusPerTier);
                }
                else
                {
                    weapon.BonusDamage = weapon.Tier <= 1 ? 1 : Dice.Roll(1, Math.Max(2, weapon.Tier));
                    weapon.BonusAttackSpeed = (int)(weapon.Tier * 0.1);
                }
            }
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
        
        /// <summary>
        /// Gets the order index of a rarity (for sorting)
        /// </summary>
        private int GetRarityOrder(string rarity)
        {
            var rarityOrder = new[] { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic", "Transcendent" };
            int index = Array.IndexOf(rarityOrder, rarity);
            return index < 0 ? 0 : index;
        }
    }
}

