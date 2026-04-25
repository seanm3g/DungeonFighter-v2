namespace RPGGame.UI.Avalonia.Renderers.Inventory
{
    using System;
    using System.Collections.Generic;
    using RPGGame.UI;
    using RPGGame.UI.Avalonia;
    using RPGGame.UI.Avalonia.Renderers.Helpers;
    using RPGGame.Items.Helpers;
    using static RPGGame.UI.LeftPanelHoverState;

    /// <summary>
    /// Renders the main inventory screen with items and actions
    /// </summary>
    public class InventoryScreenRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly ColoredTextWriter textWriter;
        private readonly List<ClickableElement> clickableElements;
        
        public InventoryScreenRenderer(
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
        
        private static int CountItemDisplayLines(Item item, Character character, List<string> itemStats)
        {
            var itemActions = character.Equipment.GetGearActions(item);
            int n = 1;
            if (itemActions != null && itemActions.Count > 0)
                n++;
            n += itemStats.Count;
            return n;
        }

        /// <summary>
        /// Renders the inventory screen with items and actions
        /// </summary>
        public int RenderInventory(int x, int y, int width, int height, Character character, List<Item> inventory)
        {
            // Do not Clear() the shared clickables list: PersistentLayoutManager already cleared it this frame,
            // and CharacterPanelRenderer registered left-panel lphover targets before this callback. Clearing here
            // removed those targets so gear/stats tooltips never fired while inventory was open.
            int currentLineCount = 0;
            
            // Clear the center panel content area to ensure clean rendering
            canvas.ClearTextInArea(x, y, width, height);
            canvas.ClearProgressBarsInArea(x, y, width, height);
            
            int startY = y;
            
            // Null check for inventory
            if (inventory == null)
            {
                canvas.AddText(x + 2, y, "ERROR: Inventory is null", AsciiArtAssets.Colors.Red);
                return 1;
            }
            
            // Inventory items section
            int itemsHeaderY = y;
            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.InventoryItems), AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;
            clickableElements.Add(new ClickableElement
            {
                X = x + 2,
                Y = itemsHeaderY,
                Width = Math.Max(1, width - 4),
                Height = 1,
                Type = ElementType.Text,
                Value = Prefix + "center:itemsHeader",
                DisplayText = "Inventory items"
            });
            
            if (inventory.Count == 0)
            {
                canvas.AddText(x + 2, y, "No items in inventory", AsciiArtAssets.Colors.White);
                currentLineCount++;
            }
            else
            {
                int maxItems = Math.Min(inventory.Count, 20);
                for (int i = 0; i < maxItems; i++)
                {
                    var item = inventory[i];
                    var itemStats = ItemStatFormatter.GetItemStats(item, character);
                    
                    // Get actions for this item
                    var itemActions = character.Equipment.GetGearActions(item);
                    
                    int rowLines = CountItemDisplayLines(item, character, itemStats);
                    string slotName = GetSlotName(item);
                    string rarity = item.Rarity?.Trim() ?? "Common";
                    clickableElements.Add(InventoryButtonFactory.CreateButton(
                        x + 2,
                        y,
                        width - 4,
                        rowLines,
                        i.ToString(),
                        $"[{i + 1}] [{rarity}] [{slotName}] {item.Name}",
                        Prefix + "inv:" + i));
                    
                    // Render item name
                    ItemRendererHelper.RenderItemName(textWriter, canvas, x + 2, y, i, item, useColoredText: true);
                    y++;
                    currentLineCount++;
                    
                    // Render actions if available
                    if (itemActions != null && itemActions.Count > 0)
                    {
                        string actionsText = "Actions: " + string.Join(", ", itemActions);
                        // Truncate if too long to fit in available width
                        int maxActionWidth = width - 10;
                        if (actionsText.Length > maxActionWidth)
                        {
                            actionsText = actionsText.Substring(0, maxActionWidth - 3) + "...";
                        }
                        canvas.AddText(x + 4, y, actionsText, AsciiArtAssets.Colors.Cyan);
                        y++;
                        currentLineCount++;
                    }
                    
                    // Render stats
                    ItemRendererHelper.RenderItemStats(textWriter, canvas, x + 2, y, itemStats, ref y, ref currentLineCount, useColoredText: true);
                }
            }
            
            // Actions section at bottom
            y = startY + height - 10;
            int actionsHeaderY = y;
            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.Actions), AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;
            clickableElements.Add(new ClickableElement
            {
                X = x + 2,
                Y = actionsHeaderY,
                Width = Math.Max(1, width - 4),
                Height = 1,
                Type = ElementType.Text,
                Value = Prefix + "center:actionsHeader",
                DisplayText = "Actions"
            });
            
            // Create inventory action buttons
            var equipButton = InventoryButtonFactory.CreateButton(x + 2, y, 28, 1, "1", MenuOptionFormatter.Format(1, UIConstants.MenuOptions.EquipItem), Prefix + "menu:equip");
            var unequipButton = InventoryButtonFactory.CreateButton(x + 32, y, 28, 1, "2", MenuOptionFormatter.Format(2, UIConstants.MenuOptions.UnequipItem), Prefix + "menu:unequip");
            var discardButton = InventoryButtonFactory.CreateButton(x + 2, y + 1, 28, 1, "3", MenuOptionFormatter.Format(3, UIConstants.MenuOptions.DiscardItem), Prefix + "menu:discard");
            var tradeUpButton = InventoryButtonFactory.CreateButton(x + 32, y + 1, 28, 1, "4", MenuOptionFormatter.Format(4, UIConstants.MenuOptions.TradeUpItems), Prefix + "menu:tradeup");
            var exitButton = InventoryButtonFactory.CreateButton(x + 2, y + 2, 28, 1, "0", MenuOptionFormatter.Format(0, UIConstants.MenuOptions.ReturnToGameMenu), Prefix + "menu:exit");
            
            clickableElements.AddRange(new[] { equipButton, unequipButton, discardButton, tradeUpButton, exitButton });
            
            canvas.AddMenuOption(x + 2, y, 1, UIConstants.MenuOptions.EquipItem, AsciiArtAssets.Colors.White, equipButton.IsHovered);
            canvas.AddMenuOption(x + 32, y, 2, UIConstants.MenuOptions.UnequipItem, AsciiArtAssets.Colors.White, unequipButton.IsHovered);
            currentLineCount++;
            canvas.AddMenuOption(x + 2, y + 1, 3, UIConstants.MenuOptions.DiscardItem, AsciiArtAssets.Colors.White, discardButton.IsHovered);
            canvas.AddMenuOption(x + 32, y + 1, 4, UIConstants.MenuOptions.TradeUpItems, AsciiArtAssets.Colors.White, tradeUpButton.IsHovered);
            currentLineCount++;
            canvas.AddMenuOption(x + 2, y + 2, 0, UIConstants.MenuOptions.ReturnToGameMenu, AsciiArtAssets.Colors.White, exitButton.IsHovered);
            currentLineCount++;
            
            return currentLineCount;
        }
    }
}

