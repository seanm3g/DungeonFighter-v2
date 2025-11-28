using Avalonia.Media;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;
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
            currentLineCount = 0;
            int startY = y;
            
            // Inventory items section
            canvas.AddText(x + 2, y, "═══ INVENTORY ITEMS ═══", AsciiArtAssets.Colors.Gold);
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
                    string coloredItemName = ItemDisplayFormatter.GetColoredItemName(item);
                    List<string> itemStats = GetItemStats(item, character);
                    
                    // Add clickable item
                    var itemElement = new ClickableElement
                    {
                        X = x + 2,
                        Y = y,
                        Width = width - 4,
                        Height = 1,
                        Type = ElementType.Item,
                        Value = i.ToString(),
                        DisplayText = $"[{i + 1}] {item.Name}"
                    };
                    clickableElements.Add(itemElement);
                    
                    // Render item name with color markup support
                    string displayLine = $"[{i + 1}] {coloredItemName}";
                    textWriter.WriteLineColored(displayLine, x + 2, y);
                    y++;
                    currentLineCount++;
                    
                    // Render each stat on its own indented line
                    if (itemStats.Count > 0)
                    {
                        foreach (var stat in itemStats)
                        {
                            textWriter.WriteLineColored($"    {stat}", x + 2, y);
                            y++;
                            currentLineCount++;
                        }
                    }
                }
            }
            
            // Actions section at bottom
            y = startY + height - 10;
            canvas.AddText(x + 2, y, "═══ ACTIONS ═══", AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;
            
            // Create inventory action buttons
            var equipButton = CreateButton(x + 2, y, 28, "1", "[1] Equip Item");
            var unequipButton = CreateButton(x + 32, y, 28, "2", "[2] Unequip Item");
            var discardButton = CreateButton(x + 2, y + 1, 28, "3", "[3] Discard Item");
            var comboButton = CreateButton(x + 32, y + 1, 28, "4", "[4] Manage Combo Actions");
            var dungeonButton = CreateButton(x + 2, y + 2, 28, "5", "[5] Continue to Dungeon");
            var mainMenuButton = CreateButton(x + 32, y + 2, 28, "6", "[6] Return to Main Menu");
            var exitButton = CreateButton(x + 2, y + 3, 28, "0", "[0] Exit Game");
            
            clickableElements.AddRange(new[] { equipButton, unequipButton, discardButton, comboButton, dungeonButton, mainMenuButton, exitButton });
            
            // Render buttons in two columns
            canvas.AddMenuOption(x + 2, y, 1, "Equip Item", AsciiArtAssets.Colors.White, equipButton.IsHovered);
            canvas.AddMenuOption(x + 32, y, 2, "Unequip Item", AsciiArtAssets.Colors.White, unequipButton.IsHovered);
            currentLineCount++;
            canvas.AddMenuOption(x + 2, y + 1, 3, "Discard Item", AsciiArtAssets.Colors.White, discardButton.IsHovered);
            canvas.AddMenuOption(x + 32, y + 1, 4, "Manage Combo Actions", AsciiArtAssets.Colors.White, comboButton.IsHovered);
            currentLineCount++;
            canvas.AddMenuOption(x + 2, y + 2, 5, "Continue to Dungeon", AsciiArtAssets.Colors.White, dungeonButton.IsHovered);
            canvas.AddMenuOption(x + 32, y + 2, 6, "Return to Main Menu", AsciiArtAssets.Colors.White, mainMenuButton.IsHovered);
            currentLineCount++;
            canvas.AddMenuOption(x + 2, y + 3, 0, "Exit Game", AsciiArtAssets.Colors.White, exitButton.IsHovered);
            currentLineCount++;
        }
        
        /// <summary>
        /// Gets formatted item stats for display as a list (one stat per line)
        /// </summary>
        private List<string> GetItemStats(Item item, Character character)
        {
            var stats = new List<string>();
            
            if (item is WeaponItem weapon)
            {
                stats.Add($"Damage: {weapon.GetTotalDamage()}");
                stats.Add($"Speed: {weapon.GetTotalAttackSpeed():F1}s");
            }
            else if (item is HeadItem headItem)
            {
                stats.Add($"Armor: +{headItem.GetTotalArmor()}");
            }
            else if (item is FeetItem feetItem)
            {
                stats.Add($"Armor: +{feetItem.GetTotalArmor()}");
            }
            else if (item is ChestItem chestItem)
            {
                stats.Add($"Armor: +{chestItem.GetTotalArmor()}");
            }
            
            if (item.StatBonuses.Count > 0)
            {
                foreach (var bonus in item.StatBonuses)
                {
                    // Format the stat value
                    string formattedValue = bonus.StatType switch
                    {
                        "AttackSpeed" => $"+{bonus.Value:F3} AttackSpeed",
                        _ => $"+{bonus.Value} {bonus.StatType}"
                    };
                    
                    // Include the label (e.g., "of Lightning") if it exists
                    if (!string.IsNullOrEmpty(bonus.Name))
                    {
                        stats.Add($"{bonus.Name}: {formattedValue}");
                    }
                    else
                    {
                        stats.Add(formattedValue);
                    }
                }
            }
            
            return stats;
        }
        
        /// <summary>
        /// Renders item selection prompt for equip/discard actions
        /// </summary>
        public void RenderItemSelectionPrompt(int x, int y, int width, int height, Character character, List<Item> inventory, string promptMessage, string actionType)
        {
            currentLineCount = 0;
            int startY = y;
            
            // Show prompt message
            canvas.AddText(x + 2, y, "═══ " + promptMessage.ToUpper() + " ═══", AsciiArtAssets.Colors.Gold);
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
                    List<string> itemStats = GetItemStats(item, character);
                    
                    // Create clickable button for each item (1-based numbering)
                    var itemButton = CreateButton(x + 2, y, width - 4, (i + 1).ToString(), $"[{i + 1}] {item.Name}");
                    clickableElements.Add(itemButton);
                    
                    // Build colored text for item selection line using new color system
                    var displayBuilder = new ColoredTextBuilder();
                    displayBuilder.Add($"[{i + 1}] ", Colors.White);
                    
                    // Add item name with proper colors
                    var itemNameSegments = ItemDisplayColoredText.FormatFullItemName(item);
                    displayBuilder.AddRange(itemNameSegments);
                    
                    // Render the colored text
                    var displaySegments = displayBuilder.Build();
                    textWriter.RenderSegments(displaySegments, x + 2, y);
                    y++;
                    currentLineCount++;
                    
                    // Render each stat on its own indented line with colors
                    if (itemStats.Count > 0)
                    {
                        foreach (var stat in itemStats)
                        {
                            // Parse stat string and format with colors
                            var statSegments = FormatStatLine(stat);
                            textWriter.RenderSegments(statSegments, x + 2, y);
                            y++;
                            currentLineCount++;
                        }
                    }
                }
                y++;
                currentLineCount++;
            }
            
            // Add cancel button
            var cancelButton = CreateButton(x + 2, y, 28, "0", "[0] Cancel");
            clickableElements.Add(cancelButton);
            canvas.AddMenuOption(x + 2, y, 0, "Cancel", AsciiArtAssets.Colors.White, cancelButton.IsHovered);
            currentLineCount++;
        }
        
        /// <summary>
        /// Formats a stat line string into colored text segments
        /// </summary>
        private List<ColoredText> FormatStatLine(string stat)
        {
            var builder = new ColoredTextBuilder();
            builder.Add("    ", Colors.White); // Indentation
            
            // Parse common stat formats and apply colors
            if (stat.StartsWith("Armor: +"))
            {
                var parts = stat.Split(new[] { ": +" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    builder.Add("Armor: +", ColorPalette.Info);
                    builder.Add(parts[1], ColorPalette.Success);
                }
                else
                {
                    builder.Add(stat, Colors.White);
                }
            }
            else if (stat.StartsWith("Damage: "))
            {
                var parts = stat.Split(new[] { ": " }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    builder.Add("Damage: ", ColorPalette.Info);
                    builder.Add(parts[1], ColorPalette.Damage);
                }
                else
                {
                    builder.Add(stat, Colors.White);
                }
            }
            else if (stat.StartsWith("Speed: "))
            {
                var parts = stat.Split(new[] { ": " }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    builder.Add("Speed: ", ColorPalette.Info);
                    builder.Add(parts[1], Colors.White);
                }
                else
                {
                    builder.Add(stat, Colors.White);
                }
            }
            else
            {
                // Default: check for stat bonus patterns
                if (stat.Contains("+") && stat.Contains(" "))
                {
                    var plusIndex = stat.IndexOf("+");
                    if (plusIndex > 0)
                    {
                        builder.Add(stat.Substring(0, plusIndex), ColorPalette.Info);
                        var rest = stat.Substring(plusIndex);
                        var spaceIndex = rest.IndexOf(" ");
                        if (spaceIndex > 0)
                        {
                            builder.Add(rest.Substring(0, spaceIndex + 1), ColorPalette.Success);
                            builder.Add(rest.Substring(spaceIndex + 1), Colors.White);
                        }
                        else
                        {
                            builder.Add(rest, ColorPalette.Success);
                        }
                    }
                    else
                    {
                        builder.Add(stat, Colors.White);
                    }
                }
                else
                {
                    builder.Add(stat, Colors.White);
                }
            }
            
            return builder.Build();
        }
        
        /// <summary>
        /// Renders slot selection prompt for unequip action
        /// </summary>
        public void RenderSlotSelectionPrompt(int x, int y, int width, int height, Character character)
        {
            currentLineCount = 0;
            
            // Show prompt message
            canvas.AddText(x + 2, y, "═══ SELECT SLOT TO UNEQUIP ═══", AsciiArtAssets.Colors.Gold);
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
            var cancelButton = CreateButton(x + 2, y, 28, "0", "[0] Cancel");
            clickableElements.Add(cancelButton);
            canvas.AddMenuOption(x + 2, y, 0, "Cancel", AsciiArtAssets.Colors.White, cancelButton.IsHovered);
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



