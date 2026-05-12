namespace RPGGame.UI.Avalonia.Renderers.Inventory
{
    using System;
    using System.Collections.Generic;
    using RPGGame;
    using RPGGame.UI;
    using RPGGame.UI.Avalonia;
    using RPGGame.UI.Avalonia.Renderers.Helpers;
    using RPGGame.UI.ColorSystem.Applications.ItemFormatting;
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
                ItemType.Legs => "Legs",
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

        private static string GetMutatingMenuActionTitle(string pendingChoice) => pendingChoice switch
        {
            "1" => UIConstants.MenuOptions.EquipItem,
            "2" => UIConstants.MenuOptions.UnequipItem,
            "3" => UIConstants.MenuOptions.DiscardItem,
            "4" => UIConstants.MenuOptions.TradeUpItems,
            _ => "This action"
        };

        /// <summary>
        /// Renders the inventory screen with items and actions
        /// </summary>
        public int RenderInventory(int x, int y, int width, int height, Character character, List<Item> inventory, string? pendingMutatingInventoryMenuAction = null, int itemScrollOffset = 0)
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
            int actionsStartY = InventoryItemScrollLayout.GetBottomMenuStartY(startY, height, pendingMutatingInventoryMenuAction != null);
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
                int availableItemRows = Math.Max(0, actionsStartY - y - 1);
                var itemStatsByIndex = new List<string>[inventory.Count];
                var itemActionsByIndex = new List<string>?[inventory.Count];
                var itemLineCounts = new List<int>(inventory.Count);
                for (int i = 0; i < inventory.Count; i++)
                {
                    var item = inventory[i];
                    var itemStats = ItemStatFormatter.GetItemStats(item, character);
                    var itemActions = character.Equipment.GetGearActions(item);
                    itemStatsByIndex[i] = itemStats;
                    itemActionsByIndex[i] = itemActions;
                    int rowLines = 1;
                    if (itemActions != null && itemActions.Count > 0)
                        rowLines++;
                    rowLines += itemStats.Count;
                    itemLineCounts.Add(Math.Max(1, rowLines));
                }

                int clampedScrollOffset = InventoryItemScrollLayout.ClampFirstVisibleIndex(itemScrollOffset, inventory.Count);
                bool showScrollStatus = InventoryItemScrollLayout.RequiresScrollStatus(itemLineCounts, availableItemRows, clampedScrollOffset);
                if (!showScrollStatus)
                    clampedScrollOffset = 0;
                int renderRowsAvailable = showScrollStatus ? Math.Max(0, availableItemRows - 1) : availableItemRows;
                var visibleRange = InventoryItemScrollLayout.CalculateVisibleRange(itemLineCounts, clampedScrollOffset, renderRowsAvailable);
                bool blockRowClicks = pendingMutatingInventoryMenuAction != null;
                for (int i = visibleRange.FirstIndex; i < visibleRange.LastExclusiveIndex; i++)
                {
                    var item = inventory[i];
                    var itemStats = itemStatsByIndex[i];
                    
                    // Get actions for this item
                    var itemActions = itemActionsByIndex[i];
                    
                    int rowLines = itemLineCounts[i];
                    string slotName = GetSlotName(item);
                    string rarity = item.Rarity?.Trim() ?? "Common";
                    // During mutating-action confirmation, row Values are "1","2",… — same as Continue / menu
                    // digits and hijack clicks. Rows are display-only until the user confirms or cancels.
                    if (!blockRowClicks)
                    {
                        clickableElements.Add(InventoryButtonFactory.CreateButton(
                            x + 2,
                            y,
                            width - 4,
                            rowLines,
                            (i + 1).ToString(),
                            $"[{i + 1}] [{rarity}] [{slotName}] {item.Name}",
                            Prefix + "inv:" + i));
                    }
                    
                    // Render item name (slot bracket goes red when attribute requirements block equip)
                    ItemRendererHelper.RenderItemName(textWriter, canvas, x + 2, y, i, item, useColoredText: true, character: character);
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
                        var actionsColor = ItemNameFormatter.IsCommonRarity(item.Rarity)
                            ? AsciiArtAssets.Colors.White
                            : AsciiArtAssets.Colors.Cyan;
                        canvas.AddText(x + 4, y, actionsText, actionsColor);
                        y++;
                        currentLineCount++;
                    }
                    
                    // Render stats (weapon speed vs currently equipped weapon)
                    var equippedWeapon = character.Weapon as WeaponItem;
                    ItemRendererHelper.RenderItemStats(textWriter, canvas, x + 2, y, itemStats, ref y, ref currentLineCount, useColoredText: true,
                        displayedItem: item, weaponSpeedBaseline: equippedWeapon);
                }

                if (showScrollStatus)
                {
                    int firstDisplay = visibleRange.VisibleItemCount > 0 ? visibleRange.FirstIndex + 1 : 0;
                    int lastDisplay = visibleRange.VisibleItemCount > 0 ? visibleRange.LastExclusiveIndex : 0;
                    string status = $"Items {firstDisplay}-{lastDisplay} of {inventory.Count} - scroll Up/Down";
                    if (status.Length > width - 4)
                        status = status.Substring(0, Math.Max(0, width - 7)) + "...";
                    int statusY = actionsStartY - 2;
                    canvas.AddText(x + 2, statusY, status, AsciiArtAssets.Colors.DarkGray);
                    currentLineCount++;
                }
            }
            
            // Actions / confirm section at bottom
            y = actionsStartY;
            if (pendingMutatingInventoryMenuAction != null)
            {
                string actionTitle = GetMutatingMenuActionTitle(pendingMutatingInventoryMenuAction);
                canvas.AddText(x + 2, y, "WARNING: " + actionTitle + " can change equipment or your bag.", AsciiArtAssets.Colors.Yellow);
                y++;
                currentLineCount++;
                canvas.AddText(x + 2, y, "Use Continue / Cancel below — item rows are inactive for now.", AsciiArtAssets.Colors.DarkGray);
                y++;
                currentLineCount++;
                canvas.AddText(x + 2, y, AsciiArtAssets.UIText.CreateHeader("CONFIRM"), AsciiArtAssets.Colors.Gold);
                y += 2;
                currentLineCount += 2;
                var continueBtn = InventoryButtonFactory.CreateButton(x + 2, y, width - 4, 1, "1", MenuOptionFormatter.Format(1, "Continue — " + actionTitle), tooltipHoverValue: null);
                var cancelBtn = InventoryButtonFactory.CreateButton(x + 2, y + 1, width - 4, 1, "0", MenuOptionFormatter.Format(0, "Cancel"), tooltipHoverValue: null);
                clickableElements.Add(continueBtn);
                clickableElements.Add(cancelBtn);
                canvas.AddMenuOption(x + 2, y, 1, "Continue — " + actionTitle, AsciiArtAssets.Colors.White, continueBtn.IsHovered);
                canvas.AddMenuOption(x + 2, y + 1, 0, "Cancel", AsciiArtAssets.Colors.White, cancelBtn.IsHovered);
                currentLineCount += 2;
                return currentLineCount;
            }

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

    public readonly struct InventoryItemVisibleRange
    {
        public InventoryItemVisibleRange(int firstIndex, int lastExclusiveIndex, int usedRows, int itemCount)
        {
            FirstIndex = firstIndex;
            LastExclusiveIndex = lastExclusiveIndex;
            UsedRows = usedRows;
            ItemCount = itemCount;
        }

        public int FirstIndex { get; }
        public int LastExclusiveIndex { get; }
        public int UsedRows { get; }
        public int ItemCount { get; }
        public int VisibleItemCount => Math.Max(0, LastExclusiveIndex - FirstIndex);
        public bool HasItemsAbove => FirstIndex > 0;
        public bool HasItemsBelow => LastExclusiveIndex < ItemCount;
    }

    public static class InventoryItemScrollLayout
    {
        public const int ScrollStepItems = 1;
        public const int PageScrollStepItems = 5;

        public static int GetBottomMenuStartY(int contentStartY, int contentHeight, bool hasConfirmationBlock)
        {
            int confirmationRows = hasConfirmationBlock ? 6 : 0;
            return contentStartY + contentHeight - 10 - confirmationRows;
        }

        public static int ClampFirstVisibleIndex(int requestedFirstIndex, int itemCount)
        {
            if (itemCount <= 0)
                return 0;
            return Math.Max(0, Math.Min(requestedFirstIndex, itemCount - 1));
        }

        public static bool RequiresScrollStatus(IReadOnlyList<int> itemLineCounts, int availableRows, int firstVisibleIndex)
        {
            if (itemLineCounts == null || itemLineCounts.Count == 0 || availableRows <= 0)
                return false;

            int totalRows = 0;
            for (int i = 0; i < itemLineCounts.Count; i++)
                totalRows += Math.Max(1, itemLineCounts[i]);

            return totalRows > availableRows;
        }

        public static InventoryItemVisibleRange CalculateVisibleRange(IReadOnlyList<int> itemLineCounts, int requestedFirstIndex, int availableRows)
        {
            if (itemLineCounts == null)
                return new InventoryItemVisibleRange(0, 0, 0, 0);

            int itemCount = itemLineCounts.Count;
            int firstIndex = ClampFirstVisibleIndex(requestedFirstIndex, itemCount);
            if (itemCount == 0 || availableRows <= 0)
                return new InventoryItemVisibleRange(firstIndex, firstIndex, 0, itemCount);

            int usedRows = 0;
            int index = firstIndex;
            while (index < itemCount)
            {
                int rowLines = Math.Max(1, itemLineCounts[index]);
                if (usedRows + rowLines > availableRows)
                    break;

                usedRows += rowLines;
                index++;
            }

            return new InventoryItemVisibleRange(firstIndex, index, usedRows, itemCount);
        }
    }
}

