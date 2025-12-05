using Avalonia.Media;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications;
using RPGGame.UI.Avalonia.Renderers.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Items.Helpers;
using RPGGame.UI.ColorSystem.Themes;

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
                    
                    // Get actions for this item
                    var itemActions = character.Equipment.GetGearActions(item);
                    
                    // Add clickable item
                    string slotName = GetSlotName(item.Type);
                    clickableElements.Add(new ClickableElement
                    {
                        X = x + 2,
                        Y = y,
                        Width = width - 4,
                        Height = 1,
                        Type = ElementType.Item,
                        Value = i.ToString(),
                        DisplayText = $"[{i + 1}] [{slotName}] {item.Name}"
                    });
                    
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
                    string slotName = GetSlotName(item.Type);
                    clickableElements.Add(CreateButton(x + 2, y, width - 4, (i + 1).ToString(), $"[{i + 1}] [{slotName}] {item.Name}"));
                    
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
        /// Renders item comparison screen for equip decision
        /// </summary>
        public void RenderItemComparison(int x, int y, int width, int height, Character character, Item newItem, Item? currentItem, string slot)
        {
            clickableElements.Clear();
            currentLineCount = 0;
            
            canvas.ClearTextInArea(x, y, width, height);
            canvas.ClearProgressBarsInArea(x, y, width, height);
            
            int startY = y;
            
            // Header
            string slotDisplayName = slot switch
            {
                "weapon" => "Weapon",
                "head" => "Head",
                "body" => "Body",
                "feet" => "Feet",
                _ => "Item"
            };
            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.CreateHeader($"EQUIP {slotDisplayName.ToUpper()}?"), AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;
            
            // Calculate column widths for side-by-side display
            int columnWidth = (width - 6) / 2; // Leave space for separator
            int leftColumnX = x + 2;
            int rightColumnX = x + 2 + columnWidth + 2;
            
            // Left column: Current Item
            int leftY = y;
            canvas.AddText(leftColumnX, leftY, "CURRENT ITEM:", ColorPalette.Warning.GetColor());
            leftY++;
            currentLineCount++;
            
            if (currentItem != null)
            {
                // Render current item name
                ItemRendererHelper.RenderItemName(textWriter, canvas, leftColumnX, leftY, -1, currentItem, useColoredText: true);
                leftY++;
                currentLineCount++;
                
                // Render current item stats
                var currentItemStats = ItemStatFormatter.GetItemStats(currentItem, character);
                ItemRendererHelper.RenderItemStats(textWriter, canvas, leftColumnX, leftY, currentItemStats, ref leftY, ref currentLineCount, useColoredText: true);
                
                // Render current item bonuses/modifications
                if (currentItem.StatBonuses.Count > 0 || currentItem.ActionBonuses.Count > 0 || currentItem.Modifications.Count > 0)
                {
                    leftY++;
                    currentLineCount++;
                    RenderItemBonuses(currentItem, leftColumnX, leftY, columnWidth, ref leftY, ref currentLineCount);
                }
            }
            else
            {
                canvas.AddText(leftColumnX, leftY, "(empty slot)", AsciiArtAssets.Colors.DarkGray);
                leftY++;
                currentLineCount++;
            }
            
            // Right column: New Item
            int rightY = y;
            canvas.AddText(rightColumnX, rightY, "NEW ITEM:", ColorPalette.Success.GetColor());
            rightY++;
            currentLineCount++;
            
            // Render new item name
            ItemRendererHelper.RenderItemName(textWriter, canvas, rightColumnX, rightY, -1, newItem, useColoredText: true);
            rightY++;
            currentLineCount++;
            
            // Render new item stats
            var newItemStats = ItemStatFormatter.GetItemStats(newItem, character);
            ItemRendererHelper.RenderItemStats(textWriter, canvas, rightColumnX, rightY, newItemStats, ref rightY, ref currentLineCount, useColoredText: true);
            
            // Render new item bonuses/modifications
            if (newItem.StatBonuses.Count > 0 || newItem.ActionBonuses.Count > 0 || newItem.Modifications.Count > 0)
            {
                rightY++;
                currentLineCount++;
                RenderItemBonuses(newItem, rightColumnX, rightY, columnWidth, ref rightY, ref currentLineCount);
            }
            
            // Options at bottom
            y = startY + height - 6;
            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.CreateHeader("CHOOSE:"), AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;
            
            // Create buttons for choices
            var newItemButton = CreateButton(x + 2, y, 28, "2", MenuOptionFormatter.Format(2, "Equip new item"));
            var oldItemButton = CreateButton(x + 32, y, 28, "1", MenuOptionFormatter.Format(1, "Keep current item"));
            var cancelButton = CreateButton(x + 2, y + 1, 28, "0", MenuOptionFormatter.Format(0, UIConstants.MenuOptions.Cancel));
            
            clickableElements.AddRange(new[] { newItemButton, oldItemButton, cancelButton });
            
            canvas.AddMenuOption(x + 2, y, 2, "Equip new item", AsciiArtAssets.Colors.White, newItemButton.IsHovered);
            canvas.AddMenuOption(x + 32, y, 1, "Keep current item", AsciiArtAssets.Colors.White, oldItemButton.IsHovered);
            currentLineCount++;
            canvas.AddMenuOption(x + 2, y + 1, 0, UIConstants.MenuOptions.Cancel, AsciiArtAssets.Colors.White, cancelButton.IsHovered);
            currentLineCount++;
        }
        
        /// <summary>
        /// Helper method to render item bonuses and modifications
        /// </summary>
        private void RenderItemBonuses(Item item, int x, int y, int maxWidth, ref int currentY, ref int lineCount)
        {
            // Stat bonuses
            if (item.StatBonuses.Count > 0)
            {
                var statsBuilder = new ColoredTextBuilder();
                statsBuilder.Add("Stats: ", ColorPalette.Cyan);
                
                var bonusTexts = new List<string>();
                foreach (var bonus in item.StatBonuses)
                {
                    string formatted = bonus.StatType switch
                    {
                        "AttackSpeed" => $"+{bonus.Value:F3} AttackSpeed",
                        _ => $"+{bonus.Value} {bonus.StatType}"
                    };
                    bonusTexts.Add(formatted);
                }
                
                statsBuilder.Add(string.Join(", ", bonusTexts), Colors.White);
                textWriter.RenderSegments(statsBuilder.Build(), x, currentY);
                currentY++;
                lineCount++;
            }
            
            // Action bonuses
            if (item.ActionBonuses.Count > 0)
            {
                var actionsBuilder = new ColoredTextBuilder();
                actionsBuilder.Add("Actions: ", ColorPalette.Cyan);
                actionsBuilder.Add(string.Join(", ", item.ActionBonuses.Select(b => $"{b.Name} +{b.Weight}")), Colors.White);
                textWriter.RenderSegments(actionsBuilder.Build(), x, currentY);
                currentY++;
                lineCount++;
            }
            
            // Modifications
            if (item.Modifications.Count > 0)
            {
                var modsBuilder = new ColoredTextBuilder();
                modsBuilder.Add("Mods: ", ColorPalette.Cyan);
                
                var modTexts = item.Modifications.Select(m => 
                {
                    string details = ItemDisplayFormatter.GetModificationDisplayText(m);
                    return details;
                });
                
                modsBuilder.Add(string.Join(", ", modTexts), Colors.White);
                textWriter.RenderSegments(modsBuilder.Build(), x, currentY);
                currentY++;
                lineCount++;
            }
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
        /// Renders the combo management menu
        /// </summary>
        public void RenderComboManagement(int x, int y, int width, int height, Character character)
        {
            clickableElements.Clear();
            currentLineCount = 0;
            
            canvas.ClearTextInArea(x, y, width, height);
            canvas.ClearProgressBarsInArea(x, y, width, height);
            
            int startY = y;
            
            // Header
            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.CreateHeader("COMBO MANAGEMENT"), AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;
            
            // Combo info
            var comboActions = character.GetComboActions();
            canvas.AddText(x + 2, y, $"Current combo step: {character.ComboStep + 1}", AsciiArtAssets.Colors.White);
            y++;
            currentLineCount++;
            canvas.AddText(x + 2, y, $"Combo sequence length: {comboActions.Count}", AsciiArtAssets.Colors.White);
            y += 2;
            currentLineCount += 2;
            
            // Current combo sequence
            if (comboActions.Count > 0)
            {
                canvas.AddText(x + 2, y, "Combo Sequence:", AsciiArtAssets.Colors.Gold);
                y++;
                currentLineCount++;
                for (int i = 0; i < comboActions.Count; i++)
                {
                    var action = comboActions[i];
                    string currentStep = (character.ComboStep % comboActions.Count == i) ? " ← NEXT" : "";
                    canvas.AddText(x + 4, y, $"{i + 1}. {action.Name}{currentStep}", AsciiArtAssets.Colors.White);
                    y++;
                    currentLineCount++;
                }
                y++;
                currentLineCount++;
            }
            else
            {
                canvas.AddText(x + 2, y, "(No combo actions set)", AsciiArtAssets.Colors.White);
                y += 2;
                currentLineCount += 2;
            }
            
            // Action pool info
            var actionPool = character.GetActionPool();
            canvas.AddText(x + 2, y, $"Total actions in pool: {actionPool.Count}", AsciiArtAssets.Colors.White);
            y += 2;
            currentLineCount += 2;
            
            // Menu options
            y = startY + height - 8;
            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.Actions), AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;
            
            var addButton = CreateButton(x + 2, y, 28, "1", MenuOptionFormatter.Format(1, "Add action to combo"));
            var removeButton = CreateButton(x + 32, y, 28, "2", MenuOptionFormatter.Format(2, "Remove action from combo"));
            var reorderButton = CreateButton(x + 2, y + 1, 28, "3", MenuOptionFormatter.Format(3, "Reorder combo actions"));
            var addAllButton = CreateButton(x + 32, y + 1, 28, "4", MenuOptionFormatter.Format(4, "Add all available actions"));
            var backButton = CreateButton(x + 2, y + 2, 28, "5", MenuOptionFormatter.Format(5, "Back to inventory"));
            
            clickableElements.AddRange(new[] { addButton, removeButton, reorderButton, addAllButton, backButton });
            
            canvas.AddMenuOption(x + 2, y, 1, "Add action to combo", AsciiArtAssets.Colors.White, addButton.IsHovered);
            canvas.AddMenuOption(x + 32, y, 2, "Remove action from combo", AsciiArtAssets.Colors.White, removeButton.IsHovered);
            currentLineCount++;
            canvas.AddMenuOption(x + 2, y + 1, 3, "Reorder combo actions", AsciiArtAssets.Colors.White, reorderButton.IsHovered);
            canvas.AddMenuOption(x + 32, y + 1, 4, "Add all available actions", AsciiArtAssets.Colors.White, addAllButton.IsHovered);
            currentLineCount++;
            canvas.AddMenuOption(x + 2, y + 2, 5, "Back to inventory", AsciiArtAssets.Colors.White, backButton.IsHovered);
            currentLineCount++;
        }
        
        /// <summary>
        /// Renders action selection prompt for adding/removing combo actions
        /// </summary>
        public void RenderComboActionSelection(int x, int y, int width, int height, Character character, string actionType)
        {
            clickableElements.Clear();
            currentLineCount = 0;
            
            canvas.ClearTextInArea(x, y, width, height);
            canvas.ClearProgressBarsInArea(x, y, width, height);
            
            int startY = y;
            
            // Header
            string headerText = actionType == "add" ? "ADD ACTION TO COMBO" : "REMOVE ACTION FROM COMBO";
            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.CreateHeader(headerText), AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;
            
            if (actionType == "add")
            {
                var actionPool = character.GetActionPool();
                var comboActions = character.GetComboActions();
                
                if (actionPool.Count == 0)
                {
                    canvas.AddText(x + 2, y, "No actions available to add to combo.", AsciiArtAssets.Colors.White);
                    y += 2;
                    currentLineCount += 2;
                }
                else
                {
                    canvas.AddText(x + 2, y, "Available actions:", AsciiArtAssets.Colors.White);
                    y += 2;
                    currentLineCount += 2;
                    
                    for (int i = 0; i < actionPool.Count; i++)
                    {
                        var action = actionPool[i];
                        int timesInCombo = comboActions.Count(ca => ca.Name == action.Name);
                        int timesAvailable = actionPool.Count(ap => ap.Name == action.Name);
                        string usageInfo = timesInCombo > 0 ? $" [In combo: {timesInCombo}/{timesAvailable}]" : "";
                        
                        var button = CreateButton(x + 2, y, width - 4, (i + 1).ToString(), MenuOptionFormatter.FormatItem(i + 1, action.Name + usageInfo));
                        clickableElements.Add(button);
                        
                        canvas.AddMenuOption(x + 2, y, i + 1, $"{action.Name}{usageInfo}", AsciiArtAssets.Colors.White, button.IsHovered);
                        y++;
                        currentLineCount++;
                        
                        // Show action stats
                        double speedPercentage = 100.0 / action.Length;
                        string speedText = speedPercentage >= 150 ? "Very Fast" : speedPercentage >= 120 ? "Fast" : speedPercentage >= 100 ? "Normal" : speedPercentage >= 80 ? "Slow" : "Very Slow";
                        canvas.AddText(x + 4, y, $"  {action.Description} | Damage: {action.DamageMultiplier:F1}x | Speed: {speedPercentage:F0}% ({speedText})", AsciiArtAssets.Colors.Gray);
                        y++;
                        currentLineCount++;
                    }
                }
            }
            else // remove
            {
                var comboActions = character.GetComboActions();
                
                if (comboActions.Count == 0)
                {
                    canvas.AddText(x + 2, y, "No actions in combo sequence to remove.", AsciiArtAssets.Colors.White);
                    y += 2;
                    currentLineCount += 2;
                }
                else
                {
                    canvas.AddText(x + 2, y, "Current combo sequence:", AsciiArtAssets.Colors.White);
                    y += 2;
                    currentLineCount += 2;
                    
                    for (int i = 0; i < comboActions.Count; i++)
                    {
                        var action = comboActions[i];
                        string currentStep = (character.ComboStep % comboActions.Count == i) ? " ← NEXT" : "";
                        
                        var button = CreateButton(x + 2, y, width - 4, (i + 1).ToString(), MenuOptionFormatter.FormatItem(i + 1, action.Name + currentStep));
                        clickableElements.Add(button);
                        
                        canvas.AddMenuOption(x + 2, y, i + 1, $"{action.Name}{currentStep}", AsciiArtAssets.Colors.White, button.IsHovered);
                        y++;
                        currentLineCount++;
                        
                        canvas.AddText(x + 4, y, $"  {action.Description}", AsciiArtAssets.Colors.Gray);
                        y++;
                        currentLineCount++;
                    }
                }
            }
            
            y++;
            currentLineCount++;
            
            // Cancel button
            var cancelButton = CreateButton(x + 2, y, 28, "0", MenuOptionFormatter.Format(0, UIConstants.MenuOptions.Cancel));
            clickableElements.Add(cancelButton);
            canvas.AddMenuOption(x + 2, y, 0, UIConstants.MenuOptions.Cancel, AsciiArtAssets.Colors.White, cancelButton.IsHovered);
            currentLineCount++;
        }
        
        /// <summary>
        /// Renders combo reorder prompt
        /// </summary>
        public void RenderComboReorderPrompt(int x, int y, int width, int height, Character character, string currentSequence = "")
        {
            clickableElements.Clear();
            currentLineCount = 0;
            
            canvas.ClearTextInArea(x, y, width, height);
            canvas.ClearProgressBarsInArea(x, y, width, height);
            
            // Header
            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.CreateHeader("REORDER COMBO ACTIONS"), AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;
            
            var comboActions = character.GetComboActions();
            
            canvas.AddText(x + 2, y, "Current combo sequence:", AsciiArtAssets.Colors.White);
            y += 2;
            currentLineCount += 2;
            
            for (int i = 0; i < comboActions.Count; i++)
            {
                var action = comboActions[i];
                string currentStep = (character.ComboStep % comboActions.Count == i) ? " ← NEXT" : "";
                canvas.AddText(x + 4, y, $"{i + 1}. {action.Name}{currentStep}", AsciiArtAssets.Colors.White);
                y++;
                currentLineCount++;
            }
            
            y += 2;
            currentLineCount += 2;
            
            canvas.AddText(x + 2, y, $"Enter the new order using numbers 1-{comboActions.Count} (e.g., 15324):", AsciiArtAssets.Colors.White);
            y += 2;
            currentLineCount += 2;
            
            canvas.AddText(x + 2, y, $"New order: {currentSequence}", AsciiArtAssets.Colors.White);
            y += 2;
            currentLineCount += 2;
            
            canvas.AddText(x + 2, y, "Press 0 to confirm, or continue entering numbers.", AsciiArtAssets.Colors.Gray);
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



