using Avalonia.Media;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications;
using RPGGame.UI.Avalonia.Renderers.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Handles rendering of inventory-related screens
    /// </summary>
    public class InventoryRenderer : IInteractiveRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly ColoredTextWriter textWriter;
        private readonly List<ClickableElement> clickableElements;
        private int currentLineCount;
        
        public InventoryRenderer(GameCanvasControl canvas, ColoredTextWriter textWriter, List<ClickableElement> clickableElements)
        {
            this.canvas = canvas;
            this.textWriter = textWriter;
            this.clickableElements = clickableElements;
            this.currentLineCount = 0;
        }
        
        // IScreenRenderer implementation
        public void Render()
        {
            // This is a placeholder - specific render methods are called directly
            // Future refactor could use a state machine pattern here
        }
        
        public void Clear()
        {
            clickableElements.Clear();
            currentLineCount = 0;
        }
        
        public int GetLineCount()
        {
            return currentLineCount;
        }
        
        // IInteractiveRenderer implementation
        public List<ClickableElement> GetClickableElements()
        {
            return clickableElements;
        }
        
        public void UpdateHoverState(int x, int y)
        {
            foreach (var element in clickableElements)
            {
                element.IsHovered = element.Contains(x, y);
            }
        }
        
        public bool HandleClick(int x, int y)
        {
            foreach (var element in clickableElements)
            {
                if (element.Contains(x, y))
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Renders the inventory screen with items and actions
        /// </summary>
        public void RenderInventory(int x, int y, int width, int height, Character character, List<Item> inventory)
        {
            // Clear previous state before rendering
            clickableElements.Clear();
            currentLineCount = 0;
            
            // Clear the center panel content area to ensure clean rendering
            // This is important because the PersistentLayoutManager may have cleared the canvas
            // but we want to ensure our specific area is clean
            canvas.ClearTextInArea(x, y, width, height);
            canvas.ClearProgressBarsInArea(x, y, width, height);
            
            int startY = y;
            
            // Null check for inventory
            if (inventory == null)
            {
                canvas.AddText(x + 2, y, "ERROR: Inventory is null", AsciiArtAssets.Colors.Red);
                return;
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
                    
                    // Add clickable item
                    clickableElements.Add(new ClickableElement
                    {
                        X = x + 2,
                        Y = y,
                        Width = width - 4,
                        Height = 1,
                        Type = ElementType.Item,
                        Value = i.ToString(),
                        DisplayText = MenuOptionFormatter.FormatItem(i + 1, item.Name)
                    });
                    
                    // Render item name
                    ItemRendererHelper.RenderItemName(textWriter, canvas, x + 2, y, i, item, useColoredText: false);
                    y++;
                    currentLineCount++;
                    
                    // Render stats
                    ItemRendererHelper.RenderItemStats(textWriter, canvas, x + 2, y, itemStats, ref y, ref currentLineCount, useColoredText: false);
                }
            }
            
            // Actions section at bottom
            y = startY + height - 10;
            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.Actions), AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;
            
            // Create inventory action buttons
            var equipButton = CreateButton(x + 2, y, 28, "1", MenuOptionFormatter.Format(1, UIConstants.MenuOptions.EquipItem));
            var unequipButton = CreateButton(x + 32, y, 28, "2", MenuOptionFormatter.Format(2, UIConstants.MenuOptions.UnequipItem));
            var discardButton = CreateButton(x + 2, y + 1, 28, "3", MenuOptionFormatter.Format(3, UIConstants.MenuOptions.DiscardItem));
            var comboButton = CreateButton(x + 32, y + 1, 28, "4", MenuOptionFormatter.Format(4, UIConstants.MenuOptions.ManageComboActions));
            var dungeonButton = CreateButton(x + 2, y + 2, 28, "5", MenuOptionFormatter.Format(5, UIConstants.MenuOptions.ContinueToDungeon));
            var mainMenuButton = CreateButton(x + 32, y + 2, 28, "6", MenuOptionFormatter.Format(6, UIConstants.MenuOptions.ReturnToMainMenu));
            var exitButton = CreateButton(x + 2, y + 3, 28, "0", MenuOptionFormatter.Format(0, UIConstants.MenuOptions.ExitGame));
            
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
        }
        
        /// <summary>
        /// Renders item selection prompt for equip/discard actions
        /// </summary>
        public void RenderItemSelectionPrompt(int x, int y, int width, int height, Character character, List<Item> inventory, string promptMessage, string actionType)
        {
            currentLineCount = 0;
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
                    clickableElements.Add(CreateButton(x + 2, y, width - 4, (i + 1).ToString(), MenuOptionFormatter.FormatItem(i + 1, item.Name)));
                    
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
            var cancelButton = CreateButton(x + 2, y, 28, "0", MenuOptionFormatter.Format(0, UIConstants.MenuOptions.Cancel));
            clickableElements.Add(cancelButton);
            canvas.AddMenuOption(x + 2, y, 0, UIConstants.MenuOptions.Cancel, AsciiArtAssets.Colors.White, cancelButton.IsHovered);
            currentLineCount++;
        }
        
        /// <summary>
        /// Renders slot selection prompt for unequip action
        /// </summary>
        public void RenderSlotSelectionPrompt(int x, int y, int width, int height, Character character)
        {
            currentLineCount = 0;
            
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
                var slotButton = CreateButton(x + 2, y, 40, number.ToString(), $"[{number}] {slotName}: {itemName}");
                clickableElements.Add(slotButton);
                
                string displayText = $"{slotName}: {itemName}";
                canvas.AddMenuOption(x + 2, y, number, displayText, AsciiArtAssets.Colors.White, slotButton.IsHovered);
                y++;
                currentLineCount++;
            }
            
            y++;
            currentLineCount++;
            
            // Add cancel button
            var cancelButton = CreateButton(x + 2, y, 28, "0", MenuOptionFormatter.Format(0, UIConstants.MenuOptions.Cancel));
            clickableElements.Add(cancelButton);
            canvas.AddMenuOption(x + 2, y, 0, UIConstants.MenuOptions.Cancel, AsciiArtAssets.Colors.White, cancelButton.IsHovered);
            currentLineCount++;
        }

        /// <summary>
        /// Helper method to create a clickable button
        /// </summary>
        private ClickableElement CreateButton(int x, int y, int width, string value, string displayText)
        {
            return new ClickableElement
            {
                X = x,
                Y = y,
                Width = width,
                Height = 1,
                Type = ElementType.Button,
                Value = value,
                DisplayText = displayText
            };
        }
    }
}



