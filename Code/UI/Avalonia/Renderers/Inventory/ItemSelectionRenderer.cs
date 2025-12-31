namespace RPGGame.UI.Avalonia.Renderers.Inventory
{
    using System;
    using System.Collections.Generic;
    using RPGGame.UI;
    using RPGGame.UI.Avalonia;
    using RPGGame.UI.Avalonia.Renderers.Helpers;
    using RPGGame.Items.Helpers;

    /// <summary>
    /// Renders item selection prompts for equip/discard actions and slot selection
    /// </summary>
    public class ItemSelectionRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly ColoredTextWriter textWriter;
        private readonly List<ClickableElement> clickableElements;
        
        public ItemSelectionRenderer(
            GameCanvasControl canvas,
            ColoredTextWriter textWriter,
            List<ClickableElement> clickableElements)
        {
            this.canvas = canvas;
            this.textWriter = textWriter;
            this.clickableElements = clickableElements;
        }
        
        /// <summary>
        /// Gets the slot name for an item, showing weapon class for weapons
        /// </summary>
        private static string GetSlotName(Item item)
        {
            if (item is WeaponItem weaponItem)
            {
                return weaponItem.WeaponType.ToString();
            }
            
            return item.Type switch
            {
                ItemType.Head => "Head",
                ItemType.Chest => "Body",
                ItemType.Feet => "Feet",
                _ => "Item"
            };
        }
        
        /// <summary>
        /// Renders item selection prompt for equip/discard actions
        /// </summary>
        public int RenderItemSelectionPrompt(int x, int y, int width, int height, Character character, List<Item> inventory, string promptMessage, string actionType)
        {
            int currentLineCount = 0;
            int startY = y;
            
            // Show prompt message
            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.CreateHeader(promptMessage.ToUpper()), AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;
            
            // Show inventory items as clickable buttons
            if (inventory.Count == 0)
            {
                canvas.AddText(x + 2, y, "No items in inventory", AsciiArtAssets.Colors.White);
                y += 2;
                currentLineCount += 2;
            }
            else
            {
                int maxItems = Math.Min(inventory.Count, 20);
                for (int i = 0; i < maxItems; i++)
                {
                    var item = inventory[i];
                    var itemStats = ItemStatFormatter.GetItemStats(item, character);
                    
                    // Create clickable button for each item
                    string slotName = GetSlotName(item);
                    string rarity = item.Rarity?.Trim() ?? "Common";
                    clickableElements.Add(InventoryButtonFactory.CreateButton(x + 2, y, width - 4, (i + 1).ToString(), $"[{i + 1}] [{rarity}] [{slotName}] {item.Name}"));
                    
                    // Render item name with colored text
                    ItemRendererHelper.RenderItemName(textWriter, canvas, x + 2, y, i, item, useColoredText: true);
                    y++;
                    currentLineCount++;
                    
                    // Render stats with colored text
                    ItemRendererHelper.RenderItemStats(textWriter, canvas, x + 2, y, itemStats, ref y, ref currentLineCount, useColoredText: true);
                }
                y++;
                currentLineCount++;
            }
            
            // Add cancel button
            var cancelButton = InventoryButtonFactory.CreateButton(x + 2, y, 28, "0", MenuOptionFormatter.Format(0, UIConstants.MenuOptions.Cancel));
            clickableElements.Add(cancelButton);
            canvas.AddMenuOption(x + 2, y, 0, UIConstants.MenuOptions.Cancel, AsciiArtAssets.Colors.White, cancelButton.IsHovered);
            currentLineCount++;
            
            return currentLineCount;
        }
        
        /// <summary>
        /// Renders slot selection prompt for unequip action
        /// </summary>
        public int RenderSlotSelectionPrompt(int x, int y, int width, int height, Character character)
        {
            int currentLineCount = 0;
            
            // Show prompt message
            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.MenuOptions.SelectSlotToUnequip), AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;
            
            canvas.AddText(x + 2, y, "Choose which equipment slot to unequip:", AsciiArtAssets.Colors.White);
            y += 2;
            currentLineCount += 2;
            
            // Create clickable buttons for each slot
            var slots = new[]
            {
                (1, "Weapon", character.Weapon?.Name ?? "(empty)"),
                (2, "Head", character.Head?.Name ?? "(empty)"),
                (3, "Body", character.Body?.Name ?? "(empty)"),
                (4, "Feet", character.Feet?.Name ?? "(empty)")
            };
            
            foreach (var (number, slotName, itemName) in slots)
            {
                var slotButton = InventoryButtonFactory.CreateButton(x + 2, y, 40, number.ToString(), $"[{number}] {slotName}: {itemName}");
                clickableElements.Add(slotButton);
                
                string displayText = $"{slotName}: {itemName}";
                canvas.AddMenuOption(x + 2, y, number, displayText, AsciiArtAssets.Colors.White, slotButton.IsHovered);
                y++;
                currentLineCount++;
            }
            
            y++;
            currentLineCount++;
            
            // Add cancel button
            var cancelButton = InventoryButtonFactory.CreateButton(x + 2, y, 28, "0", MenuOptionFormatter.Format(0, UIConstants.MenuOptions.Cancel));
            clickableElements.Add(cancelButton);
            canvas.AddMenuOption(x + 2, y, 0, UIConstants.MenuOptions.Cancel, AsciiArtAssets.Colors.White, cancelButton.IsHovered);
            currentLineCount++;
            
            return currentLineCount;
        }
        
        /// <summary>
        /// Renders rarity selection prompt for trade-up action
        /// </summary>
        public int RenderRaritySelectionPrompt(int x, int y, int width, int height, Character character, List<IGrouping<string, Item>> rarityGroups)
        {
            int currentLineCount = 0;
            
            // Show prompt message
            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.CreateHeader("SELECT RARITY TO TRADE UP"), AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;
            
            canvas.AddText(x + 2, y, "Select a rarity to trade up (5 items → 1 higher rarity):", AsciiArtAssets.Colors.White);
            y += 2;
            currentLineCount += 2;
            
            // Show available rarities
            for (int i = 0; i < rarityGroups.Count; i++)
            {
                var group = rarityGroups[i];
                string rarity = group.Key;
                int count = group.Count();
                string nextRarity = GetNextRarity(rarity) ?? "MAX";
                
                string displayText = $"{rarity} ({count} items) → {nextRarity}";
                var rarityButton = InventoryButtonFactory.CreateButton(x + 2, y, width - 4, (i + 1).ToString(), $"[{i + 1}] {displayText}");
                clickableElements.Add(rarityButton);
                
                canvas.AddMenuOption(x + 2, y, i + 1, displayText, AsciiArtAssets.Colors.White, rarityButton.IsHovered);
                y++;
                currentLineCount++;
            }
            
            y++;
            currentLineCount++;
            
            // No cancel option - must complete trade-up once started
            // Note: Cancel button removed to force completion of trade-up
            
            return currentLineCount;
        }
        
        /// <summary>
        /// Renders trade-up preview screen showing items to trade and the resulting item
        /// </summary>
        public int RenderTradeUpPreview(int x, int y, int width, int height, Character character, List<Item> itemsToTrade, Item resultingItem, string currentRarity, string nextRarity)
        {
            int currentLineCount = 0;
            
            // Show header
            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.CreateHeader("TRADE UP PREVIEW"), AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;
            
            // Show trade-up information
            canvas.AddText(x + 2, y, $"Trade Up: 5 {currentRarity} items → 1 {nextRarity} item", AsciiArtAssets.Colors.Cyan);
            y += 2;
            currentLineCount += 2;
            
            canvas.AddText(x + 2, y, "Items being traded (5 required):", AsciiArtAssets.Colors.White);
            y += 2;
            currentLineCount += 2;
            
            // Show the 5 items being traded
            for (int i = 0; i < itemsToTrade.Count && i < 5; i++)
            {
                var item = itemsToTrade[i];
                var itemStats = ItemStatFormatter.GetItemStats(item, character);
                
                // Render item name with colored text
                ItemRendererHelper.RenderItemName(textWriter, canvas, x + 2, y, i, item, useColoredText: true);
                y++;
                currentLineCount++;
                
                // Render stats with colored text
                ItemRendererHelper.RenderItemStats(textWriter, canvas, x + 2, y, itemStats, ref y, ref currentLineCount, useColoredText: true);
            }
            
            y += 2;
            currentLineCount += 2;
            
            // Show separator and resulting item
            canvas.AddText(x + 2, y, "─────────────────────────────────", AsciiArtAssets.Colors.Gray);
            y += 2;
            currentLineCount += 2;
            
            canvas.AddText(x + 2, y, $"Resulting {nextRarity} item:", AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;
            
            // Render resulting item
            var resultingItemStats = ItemStatFormatter.GetItemStats(resultingItem, character);
            ItemRendererHelper.RenderItemName(textWriter, canvas, x + 2, y, -1, resultingItem, useColoredText: true);
            y++;
            currentLineCount++;
            
            // Render resulting item stats
            ItemRendererHelper.RenderItemStats(textWriter, canvas, x + 2, y, resultingItemStats, ref y, ref currentLineCount, useColoredText: true);
            
            y += 2;
            currentLineCount += 2;
            
            // Show confirmation option
            // Note: Cancel button removed to force completion of trade-up
            var confirmButton = InventoryButtonFactory.CreateButton(x + 2, y, 28, "1", MenuOptionFormatter.Format(1, "Confirm Trade Up"));
            clickableElements.Add(confirmButton);
            canvas.AddMenuOption(x + 2, y, 1, "Confirm Trade Up", AsciiArtAssets.Colors.Green, confirmButton.IsHovered);
            currentLineCount++;
            
            return currentLineCount;
        }
        
        /// <summary>
        /// Gets the next rarity tier in progression
        /// </summary>
        private string GetNextRarity(string currentRarity)
        {
            var rarityOrder = new[] { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic", "Transcendent" };
            
            int currentIndex = Array.IndexOf(rarityOrder, currentRarity);
            if (currentIndex < 0 || currentIndex >= rarityOrder.Length - 1)
            {
                return "MAX"; // Not found or already at max
            }
            
            return rarityOrder[currentIndex + 1];
        }
    }
}

