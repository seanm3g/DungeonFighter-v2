using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Renderers.Helpers;
using RPGGame.UI.ColorSystem;
using RPGGame.Items.Helpers;

namespace RPGGame.UI.Avalonia.Renderers.Inventory
{

    /// <summary>
    /// Renders item comparison screen for equip decisions
    /// </summary>
    public class ItemComparisonRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly ColoredTextWriter textWriter;
        private readonly List<ClickableElement> clickableElements;
        
        public ItemComparisonRenderer(
            GameCanvasControl canvas,
            ColoredTextWriter textWriter,
            List<ClickableElement> clickableElements)
        {
            this.canvas = canvas;
            this.textWriter = textWriter;
            this.clickableElements = clickableElements;
        }
        
        /// <summary>
        /// Renders item comparison screen for equip decision
        /// </summary>
        public int RenderItemComparison(int x, int y, int width, int height, Character character, Item newItem, Item? currentItem, string slot)
        {
            clickableElements.Clear();
            int currentLineCount = 0;
            
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
            canvas.AddText(leftColumnX, leftY, "[1] CURRENT ITEM:", ColorPalette.Warning.GetColor());
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
            canvas.AddText(rightColumnX, rightY, "[2] NEW ITEM:", ColorPalette.Success.GetColor());
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
            var newItemButton = InventoryButtonFactory.CreateButton(x + 2, y, 28, "2", MenuOptionFormatter.Format(2, "Equip new item"));
            var oldItemButton = InventoryButtonFactory.CreateButton(x + 32, y, 28, "1", MenuOptionFormatter.Format(1, "Keep current item"));
            var cancelButton = InventoryButtonFactory.CreateButton(x + 2, y + 1, 28, "0", MenuOptionFormatter.Format(0, UIConstants.MenuOptions.Cancel));
            
            clickableElements.AddRange(new[] { newItemButton, oldItemButton, cancelButton });
            
            canvas.AddMenuOption(x + 2, y, 2, "Equip new item", AsciiArtAssets.Colors.White, newItemButton.IsHovered);
            canvas.AddMenuOption(x + 32, y, 1, "Keep current item", AsciiArtAssets.Colors.White, oldItemButton.IsHovered);
            currentLineCount++;
            canvas.AddMenuOption(x + 2, y + 1, 0, UIConstants.MenuOptions.Cancel, AsciiArtAssets.Colors.White, cancelButton.IsHovered);
            currentLineCount++;
            
            return currentLineCount;
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
    }
}

