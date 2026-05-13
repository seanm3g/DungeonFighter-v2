using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame;
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
        public int RenderItemComparison(int x, int y, int width, int height, Character character, Item newItem, Item? currentItem, string slot, int newItemInventoryIndex = -1)
        {
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
                "legs" => "Legs",
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
            int columnContentTopY = y;
            
            // Left column: Current Item
            int leftY = y;
            canvas.AddText(leftColumnX, leftY, "[1] CURRENT ITEM:", ColorPalette.Warning.GetColor());
            leftY++;
            currentLineCount++;
            
            if (currentItem != null)
            {
                // Render current item name
                ItemRendererHelper.RenderItemName(textWriter, canvas, leftColumnX, leftY, -1, currentItem, useColoredText: true, character: character);
                leftY++;
                currentLineCount++;
                
                // Render current item stats
                var currentItemStats = ItemStatFormatter.GetItemStats(currentItem, character);
                var newWeaponBaseline = newItem as WeaponItem;
                ItemRendererHelper.RenderItemStats(textWriter, canvas, leftColumnX, leftY, currentItemStats, ref leftY, ref currentLineCount, useColoredText: true,
                    displayedItem: currentItem, weaponSpeedBaseline: newWeaponBaseline, armorComparisonBaseline: newItem,
                    characterForEquipRequirements: character);
                
                // Render current item bonuses/modifications
                if (currentItem.StatBonuses.Count > 0 || currentItem.ActionBonuses.Count > 0 || currentItem.Modifications.Count > 0)
                {
                    leftY++;
                    currentLineCount++;
                    RenderItemBonuses(currentItem, leftColumnX, leftY, columnWidth, ref leftY, ref currentLineCount, character);
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
            
            // Render new item name (slot bracket red when requirements block equip)
            ItemRendererHelper.RenderItemName(textWriter, canvas, rightColumnX, rightY, -1, newItem, useColoredText: true, character: character);
            rightY++;
            currentLineCount++;
            
            // Render new item stats
            var newItemStats = ItemStatFormatter.GetItemStats(newItem, character);
            var currentWeaponBaseline = currentItem as WeaponItem;
            ItemRendererHelper.RenderItemStats(textWriter, canvas, rightColumnX, rightY, newItemStats, ref rightY, ref currentLineCount, useColoredText: true,
                displayedItem: newItem, weaponSpeedBaseline: currentWeaponBaseline, armorComparisonBaseline: currentItem,
                characterForEquipRequirements: character);
            
            // Render new item bonuses/modifications
            if (newItem.StatBonuses.Count > 0 || newItem.ActionBonuses.Count > 0 || newItem.Modifications.Count > 0)
            {
                rightY++;
                currentLineCount++;
                RenderItemBonuses(newItem, rightColumnX, rightY, columnWidth, ref rightY, ref currentLineCount, character);
            }
            
            // Render action changes comparison
            RenderActionChanges(character, newItem, currentItem, leftColumnX, rightColumnX, ref leftY, ref rightY, ref currentLineCount);
            RegisterComparisonTooltipTargets(slot, newItemInventoryIndex, leftColumnX, rightColumnX, columnWidth, columnContentTopY, leftY, rightY);
            
            // Options at bottom
            y = startY + height - 6;
            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.CreateHeader("CHOOSE:"), AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;
            
            // Buttons must align with column labels above: [1] CURRENT (left), [2] NEW (right).
            // Input routing: 1 = keep current, 2 = equip new (see InventoryMenuHandler / InventoryItemComparisonHandler).
            var keepCurrentButton = InventoryButtonFactory.CreateButton(x + 2, y, 28, "1", MenuOptionFormatter.Format(1, "Keep current item"));
            var equipNewButton = InventoryButtonFactory.CreateButton(x + 32, y, 28, "2", MenuOptionFormatter.Format(2, "Equip new item"));
            var cancelButton = InventoryButtonFactory.CreateButton(x + 2, y + 1, 28, "0", MenuOptionFormatter.Format(0, UIConstants.MenuOptions.Cancel));
            
            clickableElements.AddRange(new[] { keepCurrentButton, equipNewButton, cancelButton });
            
            canvas.AddMenuOption(x + 2, y, 1, "Keep current item", AsciiArtAssets.Colors.White, keepCurrentButton.IsHovered);
            canvas.AddMenuOption(x + 32, y, 2, "Equip new item", AsciiArtAssets.Colors.White, equipNewButton.IsHovered);
            currentLineCount++;
            canvas.AddMenuOption(x + 2, y + 1, 0, UIConstants.MenuOptions.Cancel, AsciiArtAssets.Colors.White, cancelButton.IsHovered);
            currentLineCount++;
            
            return currentLineCount;
        }

        internal static string? GetComparisonTooltipHoverValue(bool currentColumn, string slot, int newItemInventoryIndex)
        {
            if (currentColumn)
            {
                string? slotKey = NormalizeSlotHoverKey(slot);
                return slotKey == null ? null : LeftPanelHoverState.Prefix + "gear:" + slotKey;
            }

            return newItemInventoryIndex >= 0
                ? LeftPanelHoverState.Prefix + "inv:" + newItemInventoryIndex
                : null;
        }

        private static string? NormalizeSlotHoverKey(string slot) => slot switch
        {
            "weapon" => "weapon",
            "head" => "head",
            "body" => "body",
            "legs" => "legs",
            "feet" => "feet",
            _ => null
        };

        private void RegisterComparisonTooltipTargets(
            string slot,
            int newItemInventoryIndex,
            int leftColumnX,
            int rightColumnX,
            int columnWidth,
            int columnContentTopY,
            int leftColumnEndY,
            int rightColumnEndY)
        {
            AddComparisonTooltipTarget(
                leftColumnX,
                columnContentTopY,
                columnWidth,
                leftColumnEndY - columnContentTopY,
                GetComparisonTooltipHoverValue(true, slot, newItemInventoryIndex),
                "Current item details");

            AddComparisonTooltipTarget(
                rightColumnX,
                columnContentTopY,
                columnWidth,
                rightColumnEndY - columnContentTopY,
                GetComparisonTooltipHoverValue(false, slot, newItemInventoryIndex),
                "New item details");
        }

        private void AddComparisonTooltipTarget(int x, int y, int width, int height, string? hoverValue, string displayText)
        {
            if (string.IsNullOrEmpty(hoverValue))
                return;

            clickableElements.Add(new ClickableElement
            {
                X = x,
                Y = y,
                Width = Math.Max(1, width),
                Height = Math.Max(1, height),
                Type = ElementType.Text,
                Value = hoverValue,
                DisplayText = displayText
            });
        }
        
        /// <summary>
        /// Helper method to render item bonuses and modifications
        /// </summary>
        private void RenderItemBonuses(Item item, int x, int y, int maxWidth, ref int currentY, ref int lineCount, Character character)
        {
            bool allRed = ItemRendererHelper.IsEquipBlockedForCharacter(item, character);
            var labelColor = allRed ? Colors.Red : ColorPalette.Cyan.GetColor();
            var valueColor = allRed ? Colors.Red : Colors.White;
            // Stat bonuses
            if (item.StatBonuses.Count > 0)
            {
                var statsBuilder = new ColoredTextBuilder();
                statsBuilder.Add("Stats: ", labelColor);
                
                var bonusTexts = new List<string>();
                foreach (var bonus in item.StatBonuses)
                {
                    foreach (var (contribType, contribValue) in bonus.EnumerateContributions())
                    {
                        string formatted = contribType switch
                        {
                            "AttackSpeed" => $"+{contribValue:F3} AttackSpeed",
                            _ => $"+{contribValue} {contribType}"
                        };
                        bonusTexts.Add(formatted);
                    }
                }
                
                statsBuilder.Add(string.Join(", ", bonusTexts), valueColor);
                textWriter.RenderSegments(statsBuilder.Build(), x, currentY);
                currentY++;
                lineCount++;
            }
            
            // Action bonuses
            if (item.ActionBonuses.Count > 0)
            {
                var actionsBuilder = new ColoredTextBuilder();
                actionsBuilder.Add("Actions: ", labelColor);
                actionsBuilder.Add(string.Join(", ", item.ActionBonuses.Select(b => $"{b.Name} +{b.Weight}")), valueColor);
                textWriter.RenderSegments(actionsBuilder.Build(), x, currentY);
                currentY++;
                lineCount++;
            }
            
            // Modifications
            if (item.Modifications.Count > 0)
            {
                var modsBuilder = new ColoredTextBuilder();
                modsBuilder.Add("Mods: ", labelColor);
                
                var modTexts = item.Modifications.Select(m => 
                {
                    string details = ItemDisplayFormatter.GetModificationDisplayText(m);
                    return details;
                });
                
                modsBuilder.Add(string.Join(", ", modTexts), valueColor);
                textWriter.RenderSegments(modsBuilder.Build(), x, currentY);
                currentY++;
                lineCount++;
            }
        }
        
        /// <summary>
        /// Renders action changes when equipping new item (added in green, removed in red)
        /// </summary>
        private void RenderActionChanges(Character character, Item newItem, Item? currentItem, int leftColumnX, int rightColumnX, ref int leftY, ref int rightY, ref int lineCount)
        {
            // Get actions from both items
            var currentItemActions = currentItem != null 
                ? character.Equipment.GetGearActions(currentItem).Distinct().ToList() 
                : new List<string>();
            var newItemActions = character.Equipment.GetGearActions(newItem).Distinct().ToList();
            
            // Find added and removed actions
            var addedActions = newItemActions.Except(currentItemActions).ToList();
            var removedActions = currentItemActions.Except(newItemActions).ToList();
            
            // Only render if there are changes
            if (addedActions.Count == 0 && removedActions.Count == 0)
                return;
            
            // Add spacing
            leftY++;
            rightY++;
            lineCount++;
            
            // Render removed actions in red (left column - current item)
            if (removedActions.Count > 0)
            {
                var removedBuilder = new ColoredTextBuilder();
                removedBuilder.Add("Actions: ", ColorPalette.Cyan);
                removedBuilder.Add(string.Join(", ", removedActions), ColorPalette.Error);
                textWriter.RenderSegments(removedBuilder.Build(), leftColumnX, leftY);
                leftY++;
                lineCount++;
            }
            
            // Render added actions in green (right column - new item)
            if (addedActions.Count > 0)
            {
                var addedBuilder = new ColoredTextBuilder();
                addedBuilder.Add("Actions: ", ColorPalette.Cyan);
                addedBuilder.Add(string.Join(", ", addedActions), ColorPalette.Success);
                textWriter.RenderSegments(addedBuilder.Build(), rightColumnX, rightY);
                rightY++;
                lineCount++;
            }
        }
    }
}

