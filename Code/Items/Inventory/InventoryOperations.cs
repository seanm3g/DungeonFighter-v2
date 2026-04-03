using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Handles inventory operations (equip, unequip, discard, trade-up)
    /// Extracted from InventoryManager to separate operation logic
    /// </summary>
    public class InventoryOperations
    {
        private readonly Character player;
        private readonly List<Item> inventory;

        public InventoryOperations(Character player, List<Item> inventory)
        {
            this.player = player ?? throw new ArgumentNullException(nameof(player));
            this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        }

        /// <summary>
        /// Equips an item from inventory, destroying the previously equipped item
        /// </summary>
        public void EquipItem()
        {
            Console.Write("Enter the number of the item to equip: ");
            if (int.TryParse(Console.ReadLine(), out int equipChoice) && 
                equipChoice > 0 && equipChoice <= inventory.Count)
            {
                var item = inventory[equipChoice - 1];
                string slot = item.Type switch
                {
                    ItemType.Weapon => "weapon",
                    ItemType.Head => "head",
                    ItemType.Chest => "body",
                    ItemType.Feet => "feet",
                    _ => ""
                };
                
                // Get the previously equipped item (if any)
                var previousItem = player.EquipItem(item, slot);
                
                // Remove the new item from inventory
                inventory.RemoveAt(equipChoice - 1);
                
                // Destroy the previous item (do not add back to inventory)
                if (previousItem != null)
                {
                    Console.WriteLine($"\nUnequipped and destroyed {previousItem.Name}. Equipped {item.Name}.");
                }
                else
                {
                    Console.WriteLine($"\nEquipped {item.Name}.");
                }
            }
            else
            {
                Console.WriteLine("Invalid choice.");
            }
        }

        /// <summary>
        /// Unequips an item from a specific slot and adds it back to inventory
        /// </summary>
        public void UnequipItem()
        {
            UIManager.WriteMenuLine("Choose slot to unequip:");
            UIManager.WriteMenuLine("1. Weapon");
            UIManager.WriteMenuLine("2. Head");
            UIManager.WriteMenuLine("3. Body");
            UIManager.WriteMenuLine("4. Feet");
            UIManager.Write("Enter your choice: ");

            if (int.TryParse(Console.ReadLine(), out int slotChoice) && 
                slotChoice >= 1 && slotChoice <= 4)
            {
                string slot = slotChoice switch
                {
                    1 => "weapon",
                    2 => "head",
                    3 => "body",
                    4 => "feet",
                    _ => ""
                };
                
                var unequippedItem = player.UnequipItem(slot);
                if (unequippedItem != null)
                {
                    inventory.Add(unequippedItem); // Add the unequipped item back to inventory
                    Console.WriteLine($"Unequipped {unequippedItem.Name}.");
                }
                else
                {
                    Console.WriteLine($"No item was equipped in the {slot} slot.");
                }
            }
            else
            {
                Console.WriteLine("Invalid choice.");
            }
        }

        /// <summary>
        /// Discards an item from inventory permanently
        /// </summary>
        public void DiscardItem()
        {
            Console.Write("Enter the number of the item to discard: ");
            if (int.TryParse(Console.ReadLine(), out int discardChoice) && 
                discardChoice > 0 && discardChoice <= inventory.Count)
            {
                var item = inventory[discardChoice - 1];
                inventory.RemoveAt(discardChoice - 1);
                Console.WriteLine($"Discarded {item.Name}.");
            }
            else
            {
                Console.WriteLine("Invalid choice.");
            }
        }

        /// <summary>
        /// Trades up 5 items of the same rarity for 1 item of the next higher rarity
        /// </summary>
        public void TradeUpItems()
        {
            if (inventory.Count < 5)
            {
                Console.WriteLine("You need at least 5 items in your inventory to trade up.");
                return;
            }
            
            // Group items by rarity and find rarities with at least 5 items
            var rarityGroups = inventory
                .GroupBy(item => item.Rarity ?? "Common")
                .Where(group => group.Count() >= 5)
                .OrderBy(group => GetRarityOrder(group.Key))
                .ToList();
            
            if (rarityGroups.Count == 0)
            {
                Console.WriteLine("You need at least 5 items of the same rarity to trade up.");
                return;
            }
            
            // Check if any rarity can be traded up (not already at max)
            var tradeableRarities = rarityGroups
                .Where(group => GetNextRarity(group.Key) != null)
                .ToList();
            
            if (tradeableRarities.Count == 0)
            {
                Console.WriteLine("You have items at the maximum rarity. Cannot trade up further.");
                return;
            }
            
            // Show available rarities
            Console.WriteLine("\nAvailable rarities to trade up:");
            for (int i = 0; i < tradeableRarities.Count; i++)
            {
                var group = tradeableRarities[i];
                string nextRarity = GetNextRarity(group.Key) ?? "MAX";
                Console.WriteLine($"{i + 1}. {group.Key} ({group.Count()} items) → {nextRarity}");
            }
            
            Console.Write("Enter the number of the rarity to trade up (0 to cancel): ");
            if (int.TryParse(Console.ReadLine(), out int rarityChoice) && 
                rarityChoice > 0 && rarityChoice <= tradeableRarities.Count)
            {
                var selectedRarity = tradeableRarities[rarityChoice - 1].Key;
                PerformTradeUp(selectedRarity);
            }
            else if (rarityChoice == 0)
            {
                Console.WriteLine("Cancelled.");
            }
            else
            {
                Console.WriteLine("Invalid choice.");
            }
        }
        
        /// <summary>
        /// Performs the trade-up operation for a specific rarity
        /// </summary>
        private void PerformTradeUp(string rarity)
        {
            var nextRarity = GetNextRarity(rarity);
            if (nextRarity == null)
            {
                Console.WriteLine($"Cannot trade up {rarity} items - already at maximum rarity.");
                return;
            }
            
            // Get 5 items of the specified rarity
            var itemsToTrade = inventory
                .Where(item => (item.Rarity ?? "Common").Equals(rarity, StringComparison.OrdinalIgnoreCase))
                .Take(5)
                .ToList();
            
            if (itemsToTrade.Count < 5)
            {
                Console.WriteLine($"Not enough {rarity} items to trade up. Need 5, have {itemsToTrade.Count}.");
                return;
            }
            
            // Calculate average tier and level from the items being traded
            int averageTier = (int)Math.Round(itemsToTrade.Average(item => item.Tier));
            int averageLevel = (int)Math.Round(itemsToTrade.Average(item => item.Level));
            
            // Determine item type (use the most common type from the traded items)
            bool isWeapon = itemsToTrade.Count(item => item.Type == ItemType.Weapon) >= 3;
            
            // Generate new item with next rarity
            Item? newItem = GenerateItemWithRarity(averageTier, averageLevel, isWeapon, nextRarity, player);
            
            if (newItem == null)
            {
                Console.WriteLine("Failed to generate trade-up item. Please try again.");
                return;
            }
            
            // Remove the 5 items from inventory
            foreach (var item in itemsToTrade)
            {
                inventory.Remove(item);
            }
            
            // Add the new item to inventory
            inventory.Add(newItem);
            
            Console.WriteLine($"Traded up 5 {rarity} items for 1 {nextRarity} {newItem.Name}!");
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
            // Set rarity to the exact requested rarity (should be exactly one tier higher than traded items)
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
            
            // Validate that the item has the correct rarity (defensive check)
            if (!string.Equals(item.Rarity, rarity, StringComparison.OrdinalIgnoreCase))
            {
                // Ensure rarity is set correctly (should not happen, but defensive programming)
                item.Rarity = rarity;
            }
            
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
        /// Gets the next rarity tier in progression (only one tier higher)
        /// Returns null if already at maximum rarity
        /// </summary>
        private string? GetNextRarity(string currentRarity)
        {
            var rarityOrder = new[] { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic", "Transcendent" };
            
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
            var rarityOrder = new[] { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic", "Transcendent" };
            int index = Array.IndexOf(rarityOrder, rarity);
            return index < 0 ? 0 : index;
        }
    }
}

