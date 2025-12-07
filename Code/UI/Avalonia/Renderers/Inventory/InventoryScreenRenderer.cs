namespace RPGGame.UI.Avalonia.Renderers.Inventory
{
    using System;
    using System.Collections.Generic;
    using RPGGame.UI;
    using RPGGame.UI.Avalonia;
    using RPGGame.UI.Avalonia.Renderers.Helpers;
    using RPGGame.Items.Helpers;

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
        /// Gets the slot name for an item type
        /// </summary>
        private static string GetSlotName(ItemType itemType)
        {
            return itemType switch
            {
                ItemType.Weapon => "Weapon",
                ItemType.Head => "Head",
                ItemType.Chest => "Body",
                ItemType.Feet => "Feet",
                _ => "Item"
            };
        }
        
        /// <summary>
        /// Renders the inventory screen with items and actions
        /// </summary>
        public int RenderInventory(int x, int y, int width, int height, Character character, List<Item> inventory)
        {
            // Clear previous state before rendering
            clickableElements.Clear();
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
            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.InventoryItems), AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;
            
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
                    
                    // Add clickable item
                    string slotName = GetSlotName(item.Type);
                    clickableElements.Add(InventoryButtonFactory.CreateButton(x + 2, y, width - 4, i.ToString(), $"[{i + 1}] [{slotName}] {item.Name}"));
                    
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
            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.Actions), AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;
            
            // Create inventory action buttons
            var equipButton = InventoryButtonFactory.CreateButton(x + 2, y, 28, "1", MenuOptionFormatter.Format(1, UIConstants.MenuOptions.EquipItem));
            var unequipButton = InventoryButtonFactory.CreateButton(x + 32, y, 28, "2", MenuOptionFormatter.Format(2, UIConstants.MenuOptions.UnequipItem));
            var discardButton = InventoryButtonFactory.CreateButton(x + 2, y + 1, 28, "3", MenuOptionFormatter.Format(3, UIConstants.MenuOptions.DiscardItem));
            var comboButton = InventoryButtonFactory.CreateButton(x + 32, y + 1, 28, "4", MenuOptionFormatter.Format(4, UIConstants.MenuOptions.ManageComboActions));
            var dungeonButton = InventoryButtonFactory.CreateButton(x + 2, y + 2, 28, "5", MenuOptionFormatter.Format(5, UIConstants.MenuOptions.ContinueToDungeon));
            var mainMenuButton = InventoryButtonFactory.CreateButton(x + 32, y + 2, 28, "6", MenuOptionFormatter.Format(6, UIConstants.MenuOptions.ReturnToMainMenu));
            var exitButton = InventoryButtonFactory.CreateButton(x + 2, y + 3, 28, "0", MenuOptionFormatter.Format(0, UIConstants.MenuOptions.ExitGame));
            
            clickableElements.AddRange(new[] { equipButton, unequipButton, discardButton, comboButton, dungeonButton, mainMenuButton, exitButton });
            
            // Render buttons in two columns
            canvas.AddMenuOption(x + 2, y, 1, UIConstants.MenuOptions.EquipItem, AsciiArtAssets.Colors.White, equipButton.IsHovered);
            canvas.AddMenuOption(x + 32, y, 2, UIConstants.MenuOptions.UnequipItem, AsciiArtAssets.Colors.White, unequipButton.IsHovered);
            currentLineCount++;
            canvas.AddMenuOption(x + 2, y + 1, 3, UIConstants.MenuOptions.DiscardItem, AsciiArtAssets.Colors.White, discardButton.IsHovered);
            canvas.AddMenuOption(x + 32, y + 1, 4, UIConstants.MenuOptions.ManageComboActions, AsciiArtAssets.Colors.White, comboButton.IsHovered);
            currentLineCount++;
            canvas.AddMenuOption(x + 2, y + 2, 5, UIConstants.MenuOptions.ContinueToDungeon, AsciiArtAssets.Colors.White, dungeonButton.IsHovered);
            canvas.AddMenuOption(x + 32, y + 2, 6, UIConstants.MenuOptions.ReturnToMainMenu, AsciiArtAssets.Colors.White, mainMenuButton.IsHovered);
            currentLineCount++;
            canvas.AddMenuOption(x + 2, y + 3, 0, UIConstants.MenuOptions.ExitGame, AsciiArtAssets.Colors.White, exitButton.IsHovered);
            currentLineCount++;
            
            return currentLineCount;
        }
    }
}

