namespace RPGGame.UI.Avalonia.Renderers.Inventory
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using global::Avalonia.Media;
    using RPGGame;
    using RPGGame.Handlers.Inventory;
    using RPGGame.UI;
    using RPGGame.UI.Avalonia;
    using RPGGame.UI.Avalonia.Help;
    using RPGGame.UI.Avalonia.Renderers.Helpers;
    using RPGGame.UI.Avalonia.Renderers.Text;
    using RPGGame.UI.Avalonia.Layout;
    using RPGGame.UI.ColorSystem;
    using RPGGame.UI.ColorSystem.Applications.ItemFormatting;
    using RPGGame.UI.ColorSystem.Themes;
    using RPGGame.Items.Helpers;
    using static RPGGame.UI.LeftPanelHoverState;

    public readonly struct InventoryDisplayEntry
    {
        public InventoryDisplayEntry(int inventoryIndex, Item item, int displayNumber = -1)
        {
            InventoryIndex = inventoryIndex;
            Item = item;
            DisplayNumber = displayNumber > 0 ? displayNumber : inventoryIndex + 1;
        }

        public int InventoryIndex { get; }
        public int DisplayNumber { get; }
        public Item Item { get; }

        public InventoryDisplayEntry WithDisplayNumber(int displayNumber)
        {
            return new InventoryDisplayEntry(InventoryIndex, Item, displayNumber);
        }
    }

    /// <summary>
    /// Renders the main inventory screen with items and actions
    /// </summary>
    public class InventoryScreenRenderer
    {
        public const string NumpadShortcutHint = "Numpad +: sort | Numpad -: filter unmet reqs | Numpad *: equip empty slots | Numpad /: slot filter | M: material filter";

        private readonly GameCanvasControl canvas;
        private readonly ColoredTextWriter textWriter;

        /// <summary>Columns the bottom Actions buttons occupy from the panel's left edge (two 28-wide columns at +2 and +32).</summary>
        private const int ActionsBlockWidth = 61;
        private const string MainMenuLabel = "Save & Main Menu";

        /// <summary>Separator column of a description pane that extended below the Actions header this frame; the shortcut hint stops short of it.</summary>
        private int? detailSeparatorXBesideActions;
        /// <summary>Equipped item chosen for the description pane this frame (paper-doll / left-panel gear click); null = bag selection.</summary>
        private Item? selectedEquippedItem;
        private string? selectedEquippedSlot;

        /// <summary>Bottom row for the description column: the extended bound when it starts right of the Actions buttons, else the bag's bound.</summary>
        private int ResolveDetailBottomY(int x, int separatorX, int bottomY, int extendedBottomY)
        {
            if (splitBand)
            {
                // The hero band takes the list side below the bag; the description owns the full height.
                InventoryHeroBand.ListColumn = (x, Math.Max(1, separatorX - x));
                return Math.Max(bottomY, extendedBottomY);
            }
            if (extendedBottomY <= bottomY || separatorX < x + ActionsBlockWidth)
                return bottomY;
            detailSeparatorXBesideActions = separatorX;
            return extendedBottomY;
        }
        private readonly List<ClickableElement> clickableElements;

        /// <summary>This frame's hub layout puts the hero band under the list (set per <see cref="RenderInventory"/>).</summary>
        private bool splitBand;

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
                ItemType.Charm => "Charm",
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
        public int RenderInventory(
            int x,
            int y,
            int width,
            int height,
            Character character,
            List<Item> inventory,
            string? pendingMutatingInventoryMenuAction = null,
            int itemScrollOffset = 0,
            InventoryItemSortMode sortMode = InventoryItemSortMode.InventoryOrder,
            bool hideRequirementBlockedItems = false,
            string? inventoryEquipSlotFilter = null,
            InventoryDetailViewState? detailView = null,
            string? inventoryMaterialFilter = null)
        {
            // Do not Clear() the shared clickables list: PersistentLayoutManager already cleared it this frame,
            // and CharacterPanelRenderer registered left-panel lphover targets before this callback. Clearing here
            // removed those targets so gear/stats tooltips never fired while inventory was open.
            int currentLineCount = 0;
            
            // Clear the center panel content area to ensure clean rendering
            canvas.ClearTextInArea(x, y, width, height);
            canvas.ClearProgressBarsInArea(x, y, width, height);
            
            int startY = y;
            // LastPaperDollBounds belongs to the left panel's GEAR figure (drawn before this callback).
            LastBagGridBounds = default;
            LastBagGridFreeTopY = 0;
            LastDetailPaneBounds = default;
            LastDetailTextTopY = 0;

            // Null check for inventory
            if (inventory == null)
            {
                canvas.AddText(x + 2, y, "ERROR: Inventory is null", AsciiArtAssets.Colors.Red);
                return 1;
            }
            
            // Inventory items section
            bool hubTabs = HubTabBar.IsActive && LayoutConstants.ActionStripAtBottom;
            // On the hub there is no Actions block (snapshot lives in the Esc menu), so the item list runs to the panel bottom.
            bool hubNoActions = hubTabs && pendingMutatingInventoryMenuAction == null;
            // Full-height frame (hub hero band under the list): the list stops above the band, the description runs on down.
            splitBand = hubNoActions && InventoryHeroBand.SplitTopY is int bandTop && bandTop > startY && bandTop < startY + height;
            int actionsStartY = splitBand
                ? InventoryHeroBand.SplitTopY!.Value
                : hubNoActions
                ? startY + height
                : InventoryItemScrollLayout.GetBottomMenuStartY(startY, height, pendingMutatingInventoryMenuAction != null);
            // The description column may run past the Actions header down to the panel bottom when it sits right of the buttons;
            // the confirmation block's long warning lines span the full width, so it keeps the short bound then.
            int detailExtendedBottomY = pendingMutatingInventoryMenuAction != null ? actionsStartY - 1 : startY + height;
            detailSeparatorXBesideActions = null;
            int itemsHeaderY = y;
            if (hubTabs)
                // Same origin as the Actions / Skills tabs (x + 1) so the strip doesn't jump when switching tabs.
                HubTabBar.Draw(canvas, clickableElements.Add, x + 1, y, width - 2, HubTab.Inventory, character: character);
            else
                canvas.AddText(x + 2, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.InventoryItems), AsciiArtAssets.Colors.Gold);
            if (hubTabs && HubEscapeMenu.IsOpen)
            {
                HubEscapeMenu.Draw(canvas, clickableElements, x, y + 1, width, height - 1);
                return currentLineCount + 1;
            }
            bool gridView = detailView?.Grid == true;
            selectedEquippedItem = detailView?.SelectedEquippedSlot is string eqSlot ? character.Equipment.GetSlotItem(eqSlot) : null;
            selectedEquippedSlot = selectedEquippedItem != null ? detailView!.SelectedEquippedSlot : null;
            int viewToggleWidth = 0; // no old/new inventory toggle button; the grid view is the only one offered
            y += 2;
            currentLineCount += 2;
            var displayEntries = BuildDisplayEntries(inventory, character, sortMode, hideRequirementBlockedItems, inventoryEquipSlotFilter, inventoryMaterialFilter);
            var viewSummarySegments = BuildViewSummaryColoredSegments(
                inventory, character, sortMode, hideRequirementBlockedItems, inventoryEquipSlotFilter, inventoryMaterialFilter);
            if (viewSummarySegments.Count > 0)
            {
                textWriter.RenderSegments(viewSummarySegments, x + 2, y);
                y++;
                currentLineCount++;
            }
            // Header hover tooltip only when the plain header is drawn: on the hub this row holds the tab strip, and a later
            // (last-registered-wins) target here would swallow the tab clicks.
            if (!hubTabs)
            {
                clickableElements.Add(new ClickableElement
                {
                    X = x + 2,
                    Y = itemsHeaderY,
                    Width = Math.Max(1, width - 4 - viewToggleWidth),
                    Height = 1,
                    Type = ElementType.Text,
                    Value = Prefix + "center:itemsHeader",
                    DisplayText = "Inventory items"
                });
            }
            
            if (inventory.Count == 0)
            {
                canvas.AddText(x + 2, y, "No items in inventory", AsciiArtAssets.Colors.White);
                currentLineCount++;
            }
            else if (displayEntries.Count == 0)
            {
                string emptyMsg = !string.IsNullOrEmpty(inventoryMaterialFilter)
                    ? "No items match the current material filter."
                    : !string.IsNullOrEmpty(inventoryEquipSlotFilter)
                    ? "No items in inventory for this equipment slot."
                    : "No items match the current requirements filter";
                canvas.AddText(x + 2, y, emptyMsg, AsciiArtAssets.Colors.White);
                currentLineCount++;
            }
            else if (gridView)
            {
                var hoveredItem = InventoryDragState.IsActive
                    ? null
                    : InventoryAffixSynergy.ResolveHoveredItem(LeftPanelHoverState.Value, inventory, character);
                RenderGridView(x, y, width, actionsStartY, detailExtendedBottomY, character, displayEntries, itemScrollOffset,
                    detailView!.SelectedInventoryIndex, blockRowClicks: pendingMutatingInventoryMenuAction != null, hoveredItem, ref currentLineCount);
            }
            else if (detailView != null)
            {
                RenderDetailView(x, y, width, actionsStartY, detailExtendedBottomY, character, displayEntries, itemScrollOffset,
                    detailView.SelectedInventoryIndex, blockRowClicks: pendingMutatingInventoryMenuAction != null, ref currentLineCount);
            }
            else
            {
                int availableItemRows = Math.Max(0, actionsStartY - y - 1);
                var itemStatsByDisplayIndex = new List<string>[displayEntries.Count];
                var itemActionsByDisplayIndex = new List<string>?[displayEntries.Count];
                var itemLineCounts = new List<int>(displayEntries.Count);
                for (int displayIndex = 0; displayIndex < displayEntries.Count; displayIndex++)
                {
                    var item = displayEntries[displayIndex].Item;
                    var itemStats = ItemStatFormatter.GetItemStats(item, character);
                    var itemActions = character.Equipment.GetGearActions(item);
                    itemStatsByDisplayIndex[displayIndex] = itemStats;
                    itemActionsByDisplayIndex[displayIndex] = itemActions;
                    int rowLines = 1;
                    if (itemActions != null && itemActions.Count > 0)
                        rowLines++;
                    rowLines += itemStats.Count;
                    itemLineCounts.Add(Math.Max(1, rowLines));
                }

                int clampedScrollOffset = InventoryItemScrollLayout.ClampFirstVisibleIndex(itemScrollOffset, displayEntries.Count);
                bool showScrollStatus = InventoryItemScrollLayout.RequiresScrollStatus(itemLineCounts, availableItemRows, clampedScrollOffset);
                if (!showScrollStatus)
                    clampedScrollOffset = 0;
                int indicatorRowsReserved = InventoryItemScrollLayout.GetReservedIndicatorRows(clampedScrollOffset, showScrollStatus);
                int renderRowsAvailable = Math.Max(0, availableItemRows - indicatorRowsReserved);
                var visibleRange = InventoryItemScrollLayout.CalculateVisibleRange(itemLineCounts, clampedScrollOffset, renderRowsAvailable);
                bool blockRowClicks = pendingMutatingInventoryMenuAction != null;
                if (showScrollStatus && visibleRange.HasItemsAbove)
                {
                    string topHint = InventoryItemScrollLayout.BuildTopScrollHint(visibleRange.FirstIndex);
                    canvas.AddText(x + 2, y, topHint, AsciiArtAssets.Colors.Gray);
                    MenuMouseScrollHint.DrawRightAligned(canvas, x, width, y);
                    y++;
                    currentLineCount++;
                }

                for (int displayIndex = visibleRange.FirstIndex; displayIndex < visibleRange.LastExclusiveIndex; displayIndex++)
                {
                    var entry = displayEntries[displayIndex];
                    int inventoryIndex = entry.InventoryIndex;
                    var item = entry.Item;
                    var itemStats = itemStatsByDisplayIndex[displayIndex];
                    
                    // Get actions for this item
                    var itemActions = itemActionsByDisplayIndex[displayIndex];
                    
                    int rowLines = itemLineCounts[displayIndex];
                    string slotName = GetSlotName(item);
                    string rarity = item.Rarity?.Trim() ?? "Common";
                    int displayNumber = entry.DisplayNumber;
                    // During mutating-action confirmation, row Values are "1","2",… — same as Continue / menu
                    // digits and hijack clicks. Rows are display-only until the user confirms or cancels.
                    if (!blockRowClicks)
                    {
                        clickableElements.Add(InventoryButtonFactory.CreateButton(
                            x + 2,
                            y,
                            width - 4,
                            rowLines,
                            displayNumber.ToString(),
                            $"[{displayNumber}] [{rarity}] [{slotName}] {item.Name}",
                            Prefix + "inv:" + inventoryIndex));
                    }
                    
                    // Render item name (slot bracket goes red when attribute requirements block equip)
                    ItemRendererHelper.RenderItemName(textWriter, canvas, x + 2, y, displayNumber - 1, item, useColoredText: true, character: character);
                    y++;
                    currentLineCount++;
                    
                    // Render actions if available
                    if (itemActions != null && itemActions.Count > 0)
                    {
                        string actionsText = ItemStatFormatter.ItemDetailLineIndent + "Actions: " + string.Join(", ", itemActions);
                        // Truncate if too long to fit in available width (indent is part of the string)
                        int maxActionWidth = width - 8;
                        if (actionsText.Length > maxActionWidth)
                        {
                            actionsText = actionsText.Substring(0, maxActionWidth - 3) + "...";
                        }
                        var actionsColor = ItemRendererHelper.IsEquipBlockedForCharacter(item, character)
                            ? AsciiArtAssets.Colors.Red
                            : ItemNameFormatter.IsCommonRarity(item.Rarity)
                                ? AsciiArtAssets.Colors.White
                                : AsciiArtAssets.Colors.Cyan;
                        canvas.AddText(x + 2, y, actionsText, actionsColor);
                        y++;
                        currentLineCount++;
                    }
                    
                    // Render stats against currently equipped same-slot gear when comparable.
                    var equippedWeapon = character.Weapon as WeaponItem;
                    var equippedArmorBaseline = ItemRendererHelper.GetArmorComparisonBaseline(character, item);
                    ItemRendererHelper.RenderItemStats(textWriter, canvas, x + 2, y, itemStats, ref y, ref currentLineCount, useColoredText: true,
                        displayedItem: item, weaponSpeedBaseline: equippedWeapon, armorComparisonBaseline: equippedArmorBaseline,
                        characterForEquipRequirements: character);
                }

                if (showScrollStatus)
                {
                    int firstDisplay = visibleRange.VisibleItemCount > 0 ? visibleRange.FirstIndex + 1 : 0;
                    int lastDisplay = visibleRange.VisibleItemCount > 0 ? visibleRange.LastExclusiveIndex : 0;
                    string status = InventoryItemScrollLayout.BuildBottomScrollHint(visibleRange, firstDisplay, lastDisplay);
                    if (status.Length > width - 4)
                        status = status.Substring(0, Math.Max(0, width - 7)) + "...";
                    int statusY = actionsStartY - 2;
                    var statusColor = visibleRange.HasItemsBelow
                        ? AsciiArtAssets.Colors.Gray
                        : AsciiArtAssets.Colors.DarkGray;
                    canvas.AddText(x + 2, statusY, status, statusColor);
                    // Soft mouse + ↕ cues wheel scroll without the shouty yellow corner arrow.
                    MenuMouseScrollHint.DrawRightAligned(canvas, x, width, statusY);
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

            if (hubNoActions)
                return currentLineCount;

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
            
            // Equip / unequip / discard happen on the items themselves (click, drag, right-click) and Trade Up is a hub
            // tab, so the list keeps only these. Keys 1-4 still work. On the hub, Main Menu lives in the Esc menu.
            var snapshotButton = InventoryButtonFactory.CreateButton(x + 2, y, 28, 1, "5", MenuOptionFormatter.Format(5, UIConstants.MenuOptions.SnapshotForActionLab), Prefix + "menu:snapshot");
            clickableElements.Add(snapshotButton);
            canvas.AddMenuOption(x + 2, y, 5, UIConstants.MenuOptions.SnapshotForActionLab, AsciiArtAssets.Colors.White, snapshotButton.IsHovered);
            currentLineCount++;
            if (hubTabs)
            {
                canvas.AddText(x + 2, y + 1, "Esc: menu (settings, main menu, quit)", AsciiArtAssets.Colors.DarkGray);
            }
            else
            {
                var exitButton = InventoryButtonFactory.CreateButton(x + 2, y + 1, 28, 1, "0", MenuOptionFormatter.Format(0, MainMenuLabel), Prefix + "menu:exit");
                clickableElements.Add(exitButton);
                canvas.AddMenuOption(x + 2, y + 1, 0, MainMenuLabel, AsciiArtAssets.Colors.White, exitButton.IsHovered);
            }
            currentLineCount++;

            // DUNGEON SELECT (D): the hub's right column is the button (RightPanelRenderer.RenderHubDungeonPanel).

            int hintWidth = detailSeparatorXBesideActions is int sepX ? Math.Min(width - 4, sepX - 1 - (x + 2)) : width - 4;
            string shortcutHint = GetNumpadShortcutHint(hintWidth);
            if (!string.IsNullOrEmpty(shortcutHint))
            {
                canvas.AddText(x + 2, y + 3, shortcutHint, AsciiArtAssets.Colors.DarkGray);
                currentLineCount++;
            }
            
            return currentLineCount;
        }

        /// <summary>Width of the detail view's left list: selection marker + "[NN] " + truncated name.</summary>
        private const int DetailViewListWidth = 2 + 5 + InventoryDetailViewState.ListNameMaxChars + 3;

        /// <summary>
        /// F1 detail view: one line per item on the left (names cut at <see cref="InventoryDetailViewState.ListNameMaxChars"/>),
        /// full description of the selected item on the right. Rows click as <c>invsel:{inventoryIndex}</c>.
        /// </summary>
        private void RenderDetailView(
            int x,
            int y,
            int width,
            int actionsStartY,
            int detailExtendedBottomY,
            Character character,
            List<InventoryDisplayEntry> displayEntries,
            int itemScrollOffset,
            int selectedInventoryIndex,
            bool blockRowClicks,
            ref int currentLineCount)
        {
            int listX = x + 2;
            int listWidth = Math.Min(DetailViewListWidth, Math.Max(1, (width - 4) / 2));
            int separatorX = listX + listWidth + 1;
            int detailX = separatorX + 2;
            int detailWidth = Math.Max(1, x + width - 2 - detailX);
            int topY = y;
            int bottomY = actionsStartY - 1;
            int availableRows = Math.Max(0, bottomY - topY);

            var lineCounts = Enumerable.Repeat(1, displayEntries.Count).ToList();
            int firstIndex = InventoryItemScrollLayout.ClampFirstVisibleIndex(itemScrollOffset, displayEntries.Count);
            bool showScrollStatus = InventoryItemScrollLayout.RequiresScrollStatus(lineCounts, availableRows, firstIndex);
            if (!showScrollStatus)
                firstIndex = 0;
            int indicatorRows = InventoryItemScrollLayout.GetReservedIndicatorRows(firstIndex, showScrollStatus);
            var visibleRange = InventoryItemScrollLayout.CalculateVisibleRange(lineCounts, firstIndex, Math.Max(0, availableRows - indicatorRows));

            var selected = ResolveDetailViewSelection(displayEntries, selectedInventoryIndex, visibleRange.FirstIndex);

            int rowY = topY;
            if (showScrollStatus && visibleRange.HasItemsAbove)
            {
                canvas.AddText(listX, rowY, $"{AsciiArtAssets.UIElements.ArrowUp} {visibleRange.FirstIndex} more above", AsciiArtAssets.Colors.Gray);
                rowY++;
                currentLineCount++;
            }

            for (int displayIndex = visibleRange.FirstIndex; displayIndex < visibleRange.LastExclusiveIndex; displayIndex++)
            {
                var entry = displayEntries[displayIndex];
                bool isSelected = selectedEquippedItem == null && entry.InventoryIndex == selected.InventoryIndex;
                textWriter.RenderSegments(BuildDetailViewListRowSegments(entry, character, isSelected), listX, rowY);
                if (!blockRowClicks)
                {
                    clickableElements.Add(InventoryButtonFactory.CreateButton(
                        listX,
                        rowY,
                        listWidth,
                        1,
                        InventoryDetailViewState.SelectInputPrefix + entry.InventoryIndex,
                        $"[{entry.DisplayNumber}] {entry.Item.Name}",
                        tooltipHoverValue: null));
                }
                rowY++;
                currentLineCount++;
            }

            if (showScrollStatus)
            {
                int hiddenBelow = visibleRange.ItemCount - visibleRange.LastExclusiveIndex;
                string status = hiddenBelow > 0
                    ? $"{AsciiArtAssets.UIElements.ArrowDown} {hiddenBelow} more below"
                    : $"{visibleRange.ItemCount} items";
                canvas.AddText(listX, bottomY - 1, status, hiddenBelow > 0 ? AsciiArtAssets.Colors.Gray : AsciiArtAssets.Colors.DarkGray);
                currentLineCount++;
            }

            int detailBottomY = ResolveDetailBottomY(x, separatorX, bottomY, detailExtendedBottomY);
            for (int sepY = topY; sepY < detailBottomY; sepY++)
                canvas.AddText(separatorX, sepY, "│", AsciiArtAssets.Colors.DarkGray);

            var paneItem = selectedEquippedItem ?? selected.Item;
            if (paneItem != null)
                RenderDetailPane(detailX, topY, detailWidth, detailBottomY, paneItem, character, ref currentLineCount);
        }

        /// <summary>Grid tile height in rows; the width is chosen per frame so tiles come out square in pixels.</summary>
        internal const int GridTileRows = 4;

        /// <summary>Narrowest description pane beside the grid.</summary>
        private const int GridDetailMinWidth = 34;

        /// <summary>Last scroll row that still fills the grid (set every grid render; the coordinator caps scrolling at it).</summary>
        public static int LastGridMaxFirstRow { get; private set; }

        /// <summary>Square-in-pixels tile width (columns) for <paramref name="tileRows"/> rows of the current font.</summary>
        internal static int GridTileColumns(double charWidth, double charHeight, int tileRows)
        {
            if (charWidth <= 0 || charHeight <= 0)
                return tileRows * 2;
            return Math.Max(3, (int)Math.Round(tileRows * charHeight / charWidth));
        }

        /// <summary>Square-in-pixels tile height (rows) for <paramref name="columns"/> columns of the current font.</summary>
        internal static int GridRowsForColumns(double charWidth, double charHeight, int columns)
        {
            if (columns <= 0)
                return 0;
            if (charWidth <= 0 || charHeight <= 0)
                return Math.Max(1, (columns + 1) / 2);
            return Math.Max(1, (int)Math.Round(columns * charWidth / charHeight));
        }

        /// <summary>
        /// Selected-item icon in the detail pane. Square in pixels, up to the pane width, and short enough
        /// to leave <paramref name="textRows"/> for the description. When that square would crowd the text,
        /// both axes shrink together. (0, 0) when the pane cannot hold a 4-row picture and that text.
        /// </summary>
        internal static (int Columns, int Rows) DetailHeroIconSize(double charWidth, double charHeight, int detailWidth, int paneRows, int textRows = 12)
        {
            if (detailWidth < 4 || paneRows < 8)
                return (0, 0);
            int maxRows = paneRows - textRows;
            if (maxRows < 4)
                return (0, 0);

            int fullRows = GridRowsForColumns(charWidth, charHeight, detailWidth);
            if (fullRows >= 4 && fullRows <= maxRows)
                return (detailWidth, fullRows);

            if (fullRows < 4)
                return (0, 0);

            // A full-width square is taller than the text budget. Narrow it with the height so the
            // picture stays square instead of stretching across the pane.
            int columns = Math.Min(detailWidth, GridTileColumns(charWidth, charHeight, maxRows));
            int rows = GridRowsForColumns(charWidth, charHeight, columns);
            while (columns > 3 && rows > maxRows)
            {
                columns--;
                rows = GridRowsForColumns(charWidth, charHeight, columns);
            }

            if (rows < 4 || rows > maxRows || columns > detailWidth)
                return (0, 0);
            return (columns, rows);
        }

        /// <summary>Left column of the detail icon. A picture narrower than the pane is centered in it.</summary>
        internal static int DetailHeroIconX(int detailX, int detailWidth, int iconColumns)
        {
            int slack = detailWidth - iconColumns;
            if (slack <= 0)
                return detailX;
            return detailX + slack / 2;
        }

        /// <summary>
        /// Rows the detail description needs, matching <see cref="RenderDetailPane"/>: wrapped name, actions,
        /// one row per stat, then <see cref="EstimateDetailTailRows"/>.
        /// </summary>
        internal static int EstimateDetailTextRows(Item item, Character character, int width)
        {
            if (item == null || character == null)
                return 0;

            int rows = WrappedLineCount(ItemRendererHelper.BuildItemNameSegments(-1, item, character), width) + 1;
            var itemActions = character.Equipment.GetGearActions(item);
            if (itemActions != null && itemActions.Count > 0)
                rows += WrappedLineCount(BuildActionsLine(itemActions), width);
            rows += ItemStatFormatter.GetItemStats(item, character).Count;
            rows += EstimateDetailTailRows(item, character, width);
            return rows;
        }

        /// <summary>
        /// Rows after the stat block: mods, requirement note, trade-up lock, and the full vs-equipped table.
        /// Stat lines give these up first when the pane is short.
        /// </summary>
        internal static int EstimateDetailTailRows(Item item, Character character, int width)
        {
            if (item == null || character == null)
                return 0;

            int rows = 0;
            if (item.Modifications.Count > 0)
            {
                rows += WrappedLineCount(
                    ItemComparisonRenderer.BuildModsLineSegments(item.Modifications, ColorPalette.Cyan.GetColor(), AsciiArtAssets.Colors.White),
                    width);
            }

            if (!item.MeetsRequirements(character))
            {
                rows += 1;
                string reason = item.GetEquipBlockedReason(character) ?? "Attribute requirements not met.";
                rows += WrappedLineCount(new List<ColoredText> { new ColoredText(reason, AsciiArtAssets.Colors.Red) }, width);
            }

            if (item.ExcludeFromTradeUp)
                rows += 2;

            rows += EstimateComparisonRows(item, character, width);
            return rows;
        }

        /// <summary>Rows <see cref="RenderEquippedComparison"/> draws, including the blank row above the rule.</summary>
        internal static int EstimateComparisonRows(Item selectedItem, Character character, int width)
        {
            string? slot = InventoryActionPoolEntries.GetEquipSlotForItem(selectedItem);
            if (slot == null)
                return 0;

            var equipped = character.Equipment.GetSlotItem(slot);
            if (ReferenceEquals(equipped, selectedItem))
                return 0;

            int rows = 2; // gap, then the rule
            if (equipped == null)
                return rows + 1;

            rows += WrappedLineCount(ItemRendererHelper.BuildItemNameSegments(-1, equipped, character), width) + 1;
            rows += 1; // New / Worn / Change
            rows += ItemComparisonTable.Build(
                ItemStatFormatter.GetStatContributions(selectedItem, character),
                ItemStatFormatter.GetStatContributions(equipped, character)).Count;
            rows += 1; // gap before the action diff

            var (gained, lost) = ItemComparisonTable.DiffActions(
                character.Equipment.GetGearActions(selectedItem),
                character.Equipment.GetGearActions(equipped));
            if (gained.Count == 0 && lost.Count == 0)
                return rows + 1;

            if (gained.Count > 0)
            {
                var b = new ColoredTextBuilder();
                b.Add("+ Gain: ", ColorPalette.Success.GetColor());
                b.Add(string.Join(", ", gained), AsciiArtAssets.Colors.White);
                rows += WrappedLineCount(b.Build(), width);
            }

            if (lost.Count > 0)
            {
                var b = new ColoredTextBuilder();
                b.Add("- Lose: ", ColorPalette.Error.GetColor());
                b.Add(string.Join(", ", lost), AsciiArtAssets.Colors.White);
                rows += WrappedLineCount(b.Build(), width);
            }

            return rows;
        }

        private static int WrappedLineCount(List<ColoredText> segments, int width)
        {
            int n = TextWrappingHelper.WrapColoredSegments(segments, Math.Max(1, width)).Count;
            return Math.Max(1, n);
        }

        private static List<ColoredText> BuildActionsLine(IReadOnlyList<string> itemActions)
        {
            var actionsBuilder = new ColoredTextBuilder();
            actionsBuilder.Add("Actions: ", ColorPalette.Cyan.GetColor());
            actionsBuilder.Add(string.Join(", ", itemActions), AsciiArtAssets.Colors.White);
            return actionsBuilder.Build();
        }

        /// <summary>Rows per bag item in the new inventory list (icon height).</summary>
        internal const int ListRowHeight = 4;

        /// <summary>Item names in the new inventory list draw this much larger than the grid font.</summary>
        internal const double ListNameFontScale = 1.5;

        /// <summary>Grid rows between scaled name lines (a 1.5× line needs two rows).</summary>
        private const int ListNameLineStride = 2;

        /// <summary>
        /// New inventory: one row per bag item down the left (icon and name only), rarity-coloured names,
        /// the selected row framed gold; the selected item's large icon and full description on the right. Rows click as
        /// <c>invsel:{inventoryIndex}</c>, drag onto the left panel to equip, and hover the item tooltip.
        /// Scroll offset counts items. While <paramref name="hoveredItem"/> is set, rows sharing one of its affixes glow cyan.
        /// </summary>
        private void RenderGridView(
            int x,
            int y,
            int width,
            int actionsStartY,
            int detailExtendedBottomY,
            Character character,
            List<InventoryDisplayEntry> displayEntries,
            int itemScrollOffset,
            int selectedInventoryIndex,
            bool blockRowClicks,
            Item? hoveredItem,
            ref int currentLineCount)
        {
            // Only the selected item lights partners, and only by material (hover no longer drives the glow).
            hoveredItem = InventoryAffixSynergy.ResolveInventorySource();
            var hoveredAffixes = InventoryAffixSynergy.GetMaterialKeys(hoveredItem);
            // A hovered set row in the left panel's Sets list overrides the selection: every bag item joining that set glows.
            string? hoveredSetMaterial = InventoryAffixSynergy.ResolveHoveredSetMaterial();
            int rowH = ListRowHeight;
            int iconCols = GridTileColumns(canvas.GetCharWidth(), canvas.GetCharHeight(), rowH);

            int topY = y;
            int bottomY = actionsStartY - 1;

            int listX = x + 2;
            int innerWidth = Math.Max(1, x + width - 2 - listX);
            int listWidth = Math.Max(24, Math.Min(innerWidth - GridDetailMinWidth - 3, innerWidth * 3 / 5));
            int separatorX = listX + listWidth + 1;
            int detailX = separatorX + 2;
            int detailWidth = Math.Max(1, x + width - 2 - detailX);
            LastBagGridBounds = (listX, topY, listWidth, bottomY - topY);

            const int statusRows = 1;
            int visibleRows = Math.Max(1, (bottomY - topY - statusRows) / rowH);
            int maxFirst = Math.Max(0, displayEntries.Count - visibleRows);
            LastGridMaxFirstRow = maxFirst;
            int firstIndex = Math.Clamp(itemScrollOffset, 0, maxFirst);

            var selected = ResolveDetailViewSelection(displayEntries, selectedInventoryIndex, firstIndex);
            bool haveArt = ItemArtAtlas.IsAvailable;
            int textX = listX + 2 + iconCols + 1;
            int textWidth = Math.Max(4, listX + listWidth - textX);

            int lastExclusive = Math.Min(displayEntries.Count, firstIndex + visibleRows);
            for (int displayIndex = firstIndex; displayIndex < lastExclusive; displayIndex++)
            {
                int ry = topY + (displayIndex - firstIndex) * rowH;
                var entry = displayEntries[displayIndex];
                var item = entry.Item;
                bool isSelected = selectedEquippedItem == null && entry.InventoryIndex == selected.InventoryIndex;
                bool blocked = ItemRendererHelper.IsEquipBlockedForCharacter(item, character);
                bool beingDragged = InventoryDragState.IsActive && InventoryDragState.SourceInventoryIndex == entry.InventoryIndex;
                bool synergy = !beingDragged && (hoveredSetMaterial != null
                    ? MaterialSetController.JoinsSet(character, item, hoveredSetMaterial)
                    : InventoryAffixSynergy.SharesAffix(item, hoveredItem, hoveredAffixes, includeMaterial: true));
                Color rarityColor = ItemThemeProvider.GetRarityColor(item.Rarity?.Trim() ?? "Common");
                Color nameColor = beingDragged ? AsciiArtAssets.Colors.DarkGray : blocked ? AsciiArtAssets.Colors.Red : rarityColor;

                if (synergy || isSelected)
                {
                    canvas.AddBox(listX, ry, listWidth, rowH, synergy ? SynergyFrameColor : AsciiArtAssets.Colors.Gold,
                        synergy ? SynergyGlowColor : default, borderThicknessPixels: 1);
                }

                if (!beingDragged)
                {
                    if (haveArt && ItemArtAtlas.TryGetIcon(item, out var bitmap, out var sourceRect))
                        canvas.AddImage(listX + 1, ry, iconCols, rowH, bitmap, sourceRect, paddingPixels: 1, followsAsciiStyle: true);
                    else
                        canvas.AddText(listX + 1, ry, TruncateTo(GetSlotName(item), iconCols), rarityColor);
                }

                // Icon + name only (details live in the pane on the right); long names wrap onto the row's second line.
                int scaledNameWidth = Math.Max(4, (int)Math.Floor(textWidth / ListNameFontScale));
                int maxNameLines = Math.Max(1, rowH / ListNameLineStride);
                var nameLines = WrapName(item.Name, scaledNameWidth, maxNameLines);
                // One line sits mid-row; two fill it.
                int nameTop = ry + (nameLines.Count == 1 ? (rowH - ListNameLineStride) / 2 : 0);
                // Normal rows color each word (material, quality, affix, base type); dragged / blocked rows stay one color.
                bool wordColors = !beingDragged && !blocked;
                var nameWords = wordColors ? NameWordColors(item) : null;
                int wordCursor = 0;
                for (int li = 0; li < nameLines.Count; li++)
                {
                    int lineY = nameTop + li * ListNameLineStride;
                    if (nameWords == null)
                        canvas.AddScaledText(textX, lineY, nameLines[li], nameColor, ListNameFontScale);
                    else
                        canvas.AddScaledText(textX, lineY, nameLines[li], LineCharColors(nameLines[li], nameWords, ref wordCursor, nameColor), ListNameFontScale);
                }

                if (!blockRowClicks)
                {
                    clickableElements.Add(InventoryButtonFactory.CreateButton(
                        listX, ry, listWidth, rowH,
                        InventoryDetailViewState.SelectInputPrefix + entry.InventoryIndex,
                        $"[{entry.DisplayNumber}] {item.Name}",
                        Prefix + "inv:" + entry.InventoryIndex));
                }
            }
            currentLineCount += visibleRows * rowH;
            LastBagGridFreeTopY = topY + Math.Max(0, lastExclusive - firstIndex) * rowH;

            bool scrolls = displayEntries.Count > visibleRows;
            string status = scrolls
                ? $"{firstIndex + 1}-{lastExclusive} of {displayEntries.Count} items"
                : $"{displayEntries.Count} items";
            Color statusColor = scrolls ? AsciiArtAssets.Colors.Gray : AsciiArtAssets.Colors.DarkGray;
            if (InventoryDragState.IsActive && InventoryDragState.FromSlot)
            {
                status = "Drop in the bag to unequip";
                statusColor = AsciiArtAssets.Colors.Green;
            }
            else if (InventoryDragState.IsActive && InventoryDragState.FromBag)
            {
                status = "Drop on the left panel to equip";
                statusColor = AsciiArtAssets.Colors.Green;
            }
            canvas.AddText(listX, bottomY - 1, TruncateTo(status, listWidth), statusColor);
            if (scrolls)
                MenuMouseScrollHint.DrawRightAligned(canvas, listX - 2, listWidth + 4, bottomY - 1);
            currentLineCount++;

            int detailBottomY = ResolveDetailBottomY(x, separatorX, bottomY, detailExtendedBottomY);
            for (int sepY = topY; sepY < detailBottomY; sepY++)
                canvas.AddText(separatorX, sepY, "│", AsciiArtAssets.Colors.DarkGray);
            LastDetailPaneBounds = (separatorX + 1, topY, x + width - 1 - separatorX, detailBottomY - topY);

            int tileRows = GridTileRows;
            int tileCols = GridTileColumns(canvas.GetCharWidth(), canvas.GetCharHeight(), tileRows);
            var paneItem = selectedEquippedItem ?? selected.Item;
            if (paneItem != null)
            {
                int detailY = topY;
                int paneRows = Math.Max(0, detailBottomY - topY);
                // +1 is the blank row between the icon and the description. Measure the real text so the
                // comparison table is not clipped under a square icon that only reserved 12 rows.
                int textRows = EstimateDetailTextRows(paneItem, character, detailWidth) + 1;
                var hero = DetailHeroIconSize(canvas.GetCharWidth(), canvas.GetCharHeight(), detailWidth, paneRows, textRows);
                if (haveArt && hero.Rows > 0 && ItemArtAtlas.TryGetHeroIcon(paneItem, out var bigBitmap, out var bigRect))
                {
                    int iconX = DetailHeroIconX(detailX, detailWidth, hero.Columns);
                    canvas.AddImage(iconX, detailY, hero.Columns, hero.Rows, bigBitmap, bigRect, paddingPixels: 0, fillFrame: true, followsAsciiStyle: true);
                    detailY += hero.Rows + 1;
                }
                LastDetailTextTopY = detailY;
                RenderDetailPane(detailX, detailY, detailWidth, detailBottomY, paneItem, character, ref currentLineCount);
            }
            RenderDragGhost(x, width, topY, bottomY, tileCols, tileRows, haveArt);
        }

        /// <summary>The item name's words in order, each with the color the full-name formatter gives it.</summary>
        private static List<(string Word, Color Color)> NameWordColors(Item item)
        {
            var words = new List<(string, Color)>();
            foreach (var seg in ItemNameFormatter.FormatFullItemName(item))
            {
                foreach (var w in (seg.Text ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    words.Add((w, seg.Color));
            }
            return words;
        }

        /// <summary>
        /// Per-character colors for one wrapped name line: each word takes the next matching formatter word's color
        /// (<paramref name="fallback"/> when the formatter spelled it differently, and for "...").
        /// </summary>
        private static List<Color> LineCharColors(string line, List<(string Word, Color Color)> words, ref int cursor, Color fallback)
        {
            var colors = new List<Color>(line.Length);
            int i = 0;
            while (i < line.Length)
            {
                if (line[i] == ' ')
                {
                    colors.Add(fallback);
                    i++;
                    continue;
                }
                int end = line.IndexOf(' ', i);
                if (end < 0) end = line.Length;
                string word = line.Substring(i, end - i);
                string bare = word.EndsWith("...") ? word.Substring(0, word.Length - 3) : word;
                Color wordColor = fallback;
                if (cursor < words.Count)
                {
                    var next = words[cursor];
                    if (string.Equals(next.Word, bare, StringComparison.OrdinalIgnoreCase)
                        || (bare.Length > 0 && bare.Length < word.Length && next.Word.StartsWith(bare, StringComparison.OrdinalIgnoreCase)))
                    {
                        wordColor = next.Color;
                        cursor++;
                    }
                }
                for (int c = 0; c < word.Length; c++)
                    colors.Add(c < bare.Length ? wordColor : fallback);
                i = end;
            }
            return colors;
        }

        /// <summary>Word-wraps an item name to <paramref name="width"/> columns over at most <paramref name="maxLines"/> lines ("..." when cut).</summary>
        private static List<string> WrapName(string? name, int width, int maxLines)
        {
            var lines = new List<string>();
            string rest = (name ?? "").Trim();
            width = Math.Max(4, width);
            while (rest.Length > 0 && lines.Count < maxLines)
            {
                if (rest.Length <= width)
                {
                    lines.Add(rest);
                    rest = "";
                    break;
                }
                if (lines.Count == maxLines - 1)
                {
                    lines.Add(rest.Substring(0, width - 3) + "...");
                    rest = "";
                    break;
                }
                int cut = rest.LastIndexOf(' ', width);
                if (cut <= 0)
                    cut = width;
                lines.Add(rest.Substring(0, cut).TrimEnd());
                rest = rest.Substring(cut).TrimStart();
            }
            return lines;
        }

        /// <summary>Frame of a tile sharing an affix with the hovered item.</summary>
        private static Color SynergyFrameColor => AsciiArtAssets.Colors.Cyan;

        /// <summary>Faint fill behind a synergy tile's icon so the frame reads as a glow.</summary>
        private static Color SynergyGlowColor
        {
            get
            {
                var c = AsciiArtAssets.Colors.Cyan;
                return Color.FromArgb(56, c.R, c.G, c.B);
            }
        }

        /// <summary>Last frame's gear figure (left panel) and bag list areas (grid cells) for drop hit-testing; empty when not drawn.</summary>
        public static (int X, int Y, int Width, int Height) LastPaperDollBounds { get; internal set; }
        public static (int X, int Y, int Width, int Height) LastBagGridBounds { get; private set; }

        /// <summary>Last frame's first grid row below the drawn bag tiles (their number labels included); 0 when not drawn.</summary>
        public static int LastBagGridFreeTopY { get; private set; }

        /// <summary>Last frame's description pane right of the bag grid; item hover tooltips dock here so they never cover tiles.</summary>
        public static (int X, int Y, int Width, int Height) LastDetailPaneBounds { get; private set; }

        /// <summary>First row of the detail pane's text, below the hero item image (0 when nothing drawn). Hover tips dock here so they never cover the image.</summary>
        public static int LastDetailTextTopY { get; private set; }

        public static bool BoundsContain((int X, int Y, int Width, int Height) bounds, int gridX, int gridY) =>
            bounds.Width > 0 && gridX >= bounds.X && gridX < bounds.X + bounds.Width && gridY >= bounds.Y && gridY < bounds.Y + bounds.Height;

        /// <summary>Slot tile height that fits a name row plus four stacked slots in <paramref name="availableRows"/>; 0 when none fits.</summary>
        internal static int PaperDollTileRows(int availableRows)
        {
            for (int rows = GridTileRows; rows >= 3; rows--)
            {
                if (1 + 4 * rows + 3 <= availableRows)
                    return rows;
            }
            return 0;
        }

        /// <summary>
        /// The dragged item's icon centered on the pointer. It may leave the center panel to the left so a bag item can be
        /// carried onto the hero panel (the left panel is drawn before the center, so the ghost paints over it); it stops at
        /// the center panel's right edge because the right panel is drawn afterwards and would cover it.
        /// </summary>
        private void RenderDragGhost(int x, int width, int topY, int bottomY, int tileCols, int tileRows, bool haveArt)
        {
            var item = InventoryDragState.Item;
            if (!InventoryDragState.IsActive || item == null)
                return;

            var (minX, maxX, minY, maxY) = DragGhostBounds(x, width, topY, bottomY, tileCols, tileRows);
            int gx = Math.Clamp(InventoryDragState.PointerX - tileCols / 2, minX, maxX);
            int gy = Math.Clamp(InventoryDragState.PointerY - tileRows / 2, minY, maxY);
            // Overlay pass: body text (the bag's item names) draws after normal boxes and images, so the ghost must not.
            canvas.AddOverlayBox(gx, gy, tileCols, tileRows, AsciiArtAssets.Colors.Gold, AsciiArtAssets.Colors.Black, borderThicknessPixels: 2);
            if (haveArt && ItemArtAtlas.TryGetIcon(item, out var bitmap, out var sourceRect))
                canvas.AddImage(gx, gy, tileCols, tileRows, bitmap, sourceRect, paddingPixels: 3, followsAsciiStyle: true, isOverlay: true);
            else
                canvas.AddOverlayText(gx + 1, gy + tileRows / 2, TruncateTo(item.Name, tileCols - 2), AsciiArtAssets.Colors.Gold);
        }

        /// <summary>
        /// Top-left clamp range for the drag ghost: from the left panel's inner edge to the center panel's right edge,
        /// and over the full height of both panels.
        /// </summary>
        internal static (int MinX, int MaxX, int MinY, int MaxY) DragGhostBounds(int centerX, int centerWidth, int topY, int bottomY, int tileCols, int tileRows)
        {
            int minX = Math.Min(centerX + 1, LayoutConstants.LEFT_PANEL_X + 1);
            int maxX = Math.Max(minX, centerX + centerWidth - 1 - tileCols);
            int minY = Math.Min(topY, LayoutConstants.LEFT_PANEL_Y);
            int panelBottom = Math.Max(bottomY, LayoutConstants.LEFT_PANEL_Y + LayoutConstants.SCREEN_HEIGHT);
            int maxY = Math.Max(minY, panelBottom - tileRows);
            return (minX, maxX, minY, maxY);
        }

        private static string TruncateTo(string? text, int max)
        {
            string s = text ?? "";
            if (max <= 0)
                return "";
            return s.Length <= max ? s : s.Substring(0, max);
        }

        /// <summary>Selected entry when it is still listed; otherwise the first visible row.</summary>
        internal static InventoryDisplayEntry ResolveDetailViewSelection(List<InventoryDisplayEntry> displayEntries, int selectedInventoryIndex, int firstVisibleIndex)
        {
            foreach (var entry in displayEntries)
            {
                if (entry.InventoryIndex == selectedInventoryIndex)
                    return entry;
            }

            if (displayEntries.Count == 0)
                return default;
            return displayEntries[Math.Clamp(firstVisibleIndex, 0, displayEntries.Count - 1)];
        }

        /// <summary>Cuts names longer than <see cref="InventoryDetailViewState.ListNameMaxChars"/> and appends "...".</summary>
        internal static string TruncateDetailViewListName(string? name)
        {
            string full = name ?? "";
            int max = InventoryDetailViewState.ListNameMaxChars;
            return full.Length <= max ? full : full.Substring(0, max).TrimEnd() + "...";
        }

        internal static List<ColoredText> BuildDetailViewListRowSegments(InventoryDisplayEntry entry, Character character, bool isSelected)
        {
            var item = entry.Item;
            bool blocked = ItemRendererHelper.IsEquipBlockedForCharacter(item, character);
            Color nameColor = blocked ? AsciiArtAssets.Colors.Red : ItemThemeProvider.GetRarityColor(item.Rarity?.Trim() ?? "Common");

            var builder = new ColoredTextBuilder();
            builder.Add(isSelected ? "> " : "  ", AsciiArtAssets.Colors.Gold);
            builder.Add($"[{entry.DisplayNumber}] ", isSelected ? AsciiArtAssets.Colors.White : AsciiArtAssets.Colors.Gray);
            builder.Add(TruncateDetailViewListName(item.Name), nameColor);
            return builder.Build();
        }

        /// <summary>Right-hand description: full name, actions, stats, mods, requirement and trade-up lock notes.</summary>
        private void RenderDetailPane(int x, int y, int width, int bottomY, Item item, Character character, ref int currentLineCount)
        {
            int written = textWriter.WriteLineColoredWrapped(ItemRendererHelper.BuildItemNameSegments(-1, item, character), x, y, width);
            y += written + 1;
            currentLineCount += written + 1;

            var itemActions = character.Equipment.GetGearActions(item);
            if (itemActions != null && itemActions.Count > 0 && y < bottomY)
            {
                written = textWriter.WriteLineColoredWrapped(BuildActionsLine(itemActions), x, y, width);
                y += written;
                currentLineCount += written;
            }

            var itemStats = ItemStatFormatter.GetItemStats(item, character);
            // Keep the mods line and the vs-equipped table. Stat lines are the part that can shrink.
            int tailRows = EstimateDetailTailRows(item, character, width);
            int statsBudget = Math.Max(0, bottomY - y - tailRows);
            if (statsBudget > 0 && itemStats.Count > 0)
            {
                var cappedStats = itemStats.Count > statsBudget ? itemStats.GetRange(0, statsBudget) : itemStats;
                ItemRendererHelper.RenderItemStats(textWriter, canvas, x, y, cappedStats, ref y, ref currentLineCount, useColoredText: true,
                    displayedItem: item, weaponSpeedBaseline: character.Weapon as WeaponItem,
                    armorComparisonBaseline: ItemRendererHelper.GetArmorComparisonBaseline(character, item),
                    characterForEquipRequirements: character);
            }

            if (item.Modifications.Count > 0 && y < bottomY)
            {
                written = textWriter.WriteLineColoredWrapped(
                    ItemComparisonRenderer.BuildModsLineSegments(item.Modifications, ColorPalette.Cyan.GetColor(), AsciiArtAssets.Colors.White),
                    x, y, width);
                y += written;
                currentLineCount += written;
            }

            if (!item.MeetsRequirements(character) && y + 1 < bottomY)
            {
                y++;
                string reason = item.GetEquipBlockedReason(character) ?? "Attribute requirements not met.";
                written = textWriter.WriteLineColoredWrapped(new List<ColoredText> { new ColoredText(reason, AsciiArtAssets.Colors.Red) }, x, y, width);
                // Hovering the line explains the attribute it names (e.g. Finesse = AGI + TEC).
                string? blockingKey = item.GetEquipBlockingRequirementKey(character);
                if (blockingKey != null)
                {
                    clickableElements.Add(new ClickableElement
                    {
                        X = x,
                        Y = y,
                        Width = Math.Max(1, Math.Min(width, reason.Length)),
                        Height = Math.Max(1, written),
                        Type = ElementType.Text,
                        Value = Prefix + "attr:" + blockingKey,
                        DisplayText = reason
                    });
                }
                y += written;
                currentLineCount += written;
            }

            if (item.ExcludeFromTradeUp && y + 1 < bottomY)
            {
                y++;
                canvas.AddText(x, y, "Locked: not used in trade-ups", AsciiArtAssets.Colors.Gray);
                y++;
                currentLineCount++;
            }

            RenderEquippedComparison(x, y + 1, width, bottomY, item, character, ref currentLineCount);
        }

        /// <summary>
        /// Below the selected item: one table of stat totals (this item, the piece worn in its slot, and the change from
        /// swapping), then the actions the swap would gain and lose.
        /// </summary>
        private void RenderEquippedComparison(int x, int y, int width, int bottomY, Item selectedItem, Character character, ref int currentLineCount)
        {
            string? slot = InventoryActionPoolEntries.GetEquipSlotForItem(selectedItem);
            if (slot == null || y + 1 >= bottomY)
                return;

            var equipped = character.Equipment.GetSlotItem(slot);
            if (ReferenceEquals(equipped, selectedItem))
                return;

            string rule = "── vs Equipped ";
            canvas.AddText(x, y, TruncateTo(rule + new string('─', Math.Max(0, width - rule.Length)), width), AsciiArtAssets.Colors.DarkGray);
            y++;
            currentLineCount++;

            if (equipped == null)
            {
                canvas.AddText(x, y, "(empty slot - everything here is a gain)", AsciiArtAssets.Colors.DarkGray);
                currentLineCount++;
                return;
            }

            int written = textWriter.WriteLineColoredWrapped(ItemRendererHelper.BuildItemNameSegments(-1, equipped, character), x, y, width);
            y += written + 1;
            currentLineCount += written + 1;

            const int valueCol = 7;
            int labelCol = Math.Max(8, width - valueCol * 3);
            Color headerColor = AsciiArtAssets.Colors.Gray;
            if (y < bottomY)
            {
                canvas.AddText(x + labelCol, y, "New".PadLeft(valueCol), headerColor);
                canvas.AddText(x + labelCol + valueCol, y, "Worn".PadLeft(valueCol), headerColor);
                canvas.AddText(x + labelCol + valueCol * 2, y, "Change".PadLeft(valueCol), headerColor);
                y++;
                currentLineCount++;
            }

            var rows = ItemComparisonTable.Build(
                ItemStatFormatter.GetStatContributions(selectedItem, character),
                ItemStatFormatter.GetStatContributions(equipped, character));
            foreach (var row in rows)
            {
                if (y >= bottomY)
                    return;
                Color changeColor = row.Verdict switch
                {
                    ItemComparisonVerdict.Better => ColorPalette.Success.GetColor(),
                    ItemComparisonVerdict.Worse => ColorPalette.Error.GetColor(),
                    _ => AsciiArtAssets.Colors.DarkGray
                };
                canvas.AddText(x, y, TruncateTo(row.Label, labelCol - 1), ColorPalette.Info.GetColor());
                canvas.AddText(x + labelCol, y, TruncateTo(row.NewText, valueCol).PadLeft(valueCol), AsciiArtAssets.Colors.White);
                canvas.AddText(x + labelCol + valueCol, y, TruncateTo(row.WornText, valueCol).PadLeft(valueCol), AsciiArtAssets.Colors.Gray);
                canvas.AddText(x + labelCol + valueCol * 2, y, TruncateTo(row.ChangeText, valueCol).PadLeft(valueCol), changeColor);
                y++;
                currentLineCount++;
            }

            var (gained, lost) = ItemComparisonTable.DiffActions(
                character.Equipment.GetGearActions(selectedItem),
                character.Equipment.GetGearActions(equipped));
            y++;
            if (gained.Count == 0 && lost.Count == 0)
            {
                if (y < bottomY)
                {
                    canvas.AddText(x, y, "Actions: same", AsciiArtAssets.Colors.DarkGray);
                    currentLineCount += 2;
                }
                return;
            }

            if (gained.Count > 0 && y < bottomY)
            {
                var b = new ColoredTextBuilder();
                b.Add("+ Gain: ", ColorPalette.Success.GetColor());
                b.Add(string.Join(", ", gained), AsciiArtAssets.Colors.White);
                written = textWriter.WriteLineColoredWrapped(b.Build(), x, y, width);
                y += written;
                currentLineCount += written + 1;
            }

            if (lost.Count > 0 && y < bottomY)
            {
                var b = new ColoredTextBuilder();
                b.Add("- Lose: ", ColorPalette.Error.GetColor());
                b.Add(string.Join(", ", lost), AsciiArtAssets.Colors.White);
                written = textWriter.WriteLineColoredWrapped(b.Build(), x, y, width);
                currentLineCount += written;
            }
        }

        public static string GetNumpadShortcutHint(int maxWidth)
        {
            if (maxWidth <= 0)
                return string.Empty;

            if (NumpadShortcutHint.Length <= maxWidth)
                return NumpadShortcutHint;

            const string compactMaterialHint = "+ sort | - reqs | * equip | / slot | M material";
            if (compactMaterialHint.Length <= maxWidth)
                return compactMaterialHint;

            const string compactHint = "+ sort | - reqs | * equip | / slot";
            if (compactHint.Length <= maxWidth)
                return compactHint;

            if (maxWidth <= 3)
                return new string('.', maxWidth);

            return compactHint.Substring(0, maxWidth - 3) + "...";
        }

        public static List<InventoryDisplayEntry> BuildDisplayEntries(
            IReadOnlyList<Item> inventory,
            Character character,
            InventoryItemSortMode sortMode,
            bool hideRequirementBlockedItems,
            string? inventoryEquipSlotFilter = null,
            string? inventoryMaterialFilter = null)
        {
            if (inventory == null)
                return new List<InventoryDisplayEntry>();

            IEnumerable<InventoryDisplayEntry> entries = inventory
                .Select((item, index) => new InventoryDisplayEntry(index, item));

            if (hideRequirementBlockedItems)
                entries = entries.Where(entry => entry.Item.MeetsRequirements(character));

            if (!string.IsNullOrEmpty(inventoryEquipSlotFilter))
            {
                entries = entries.Where(entry =>
                    string.Equals(InventoryActionPoolEntries.GetEquipSlotForItem(entry.Item), inventoryEquipSlotFilter, StringComparison.Ordinal));
            }

            if (!string.IsNullOrEmpty(inventoryMaterialFilter))
            {
                entries = entries.Where(entry =>
                    string.Equals(entry.Item.Material?.Trim(), inventoryMaterialFilter, StringComparison.OrdinalIgnoreCase));
            }

            var orderedEntries = sortMode switch
            {
                InventoryItemSortMode.Rarity => entries
                    .OrderByDescending(entry => GetRaritySortRank(entry.Item.Rarity))
                    .ThenBy(entry => GetSlotSortRank(entry.Item))
                    .ThenBy(entry => GetWeaponTypeSortRank(entry.Item))
                    .ThenBy(entry => entry.InventoryIndex)
                    .ToList(),
                InventoryItemSortMode.ItemSlot => entries
                    .OrderBy(entry => GetSlotSortRank(entry.Item))
                    .ThenBy(entry => GetWeaponTypeSortRank(entry.Item))
                    .ThenBy(entry => entry.InventoryIndex)
                    .ToList(),
                _ => entries
                    .OrderBy(entry => entry.InventoryIndex)
                    .ToList()
            };

            return orderedEntries
                .Select((entry, displayIndex) => entry.WithDisplayNumber(displayIndex + 1))
                .ToList();
        }

        /// <summary>
        /// One-line inventory view options (sort / filter / bag slot). Sort and filter clauses use a light highlight color when that option is applied (non-default).
        /// When a bag-slot filter is active, the bag-slot clause uses the same lime applied color as sort/filter highlights so each slot-only filter reads the same; red if the piece in that equipped slot is blocked. With no slot filter (all equip slots), the clause uses the same neutral dim as inactive sort/filter clauses.
        /// </summary>
        public static List<ColoredText> BuildViewSummaryColoredSegments(
            IReadOnlyList<Item> inventory,
            Character character,
            InventoryItemSortMode sortMode,
            bool hideRequirementBlockedItems,
            string? inventoryEquipSlotFilter,
            string? inventoryMaterialFilter = null)
        {
            Color dim = AsciiArtAssets.Colors.DarkGray;
            Color appliedHighlight = ColorPalette.Lime.GetColor();

            string sortLabel = sortMode switch
            {
                InventoryItemSortMode.Rarity => "Rarity",
                InventoryItemSortMode.ItemSlot => "Item slot",
                _ => "Inventory order"
            };

            string filterLabel = hideRequirementBlockedItems
                ? "hiding unmet requirements"
                : "showing all items";

            int hiddenCount = hideRequirementBlockedItems && inventory != null
                ? inventory.Count(item => !item.MeetsRequirements(character))
                : 0;

            string hiddenText = hiddenCount > 0 ? $" ({hiddenCount} hidden)" : "";

            string slotSummary = string.IsNullOrEmpty(inventoryEquipSlotFilter)
                ? "all equip slots"
                : GetInventoryEquipSlotFilterDisplayName(inventoryEquipSlotFilter) + " only";

            bool sortApplied = sortMode != InventoryItemSortMode.InventoryOrder;
            bool filterApplied = hideRequirementBlockedItems;

            Color bagClauseColor = ResolveBagSlotSummaryColor(
                character,
                inventoryEquipSlotFilter,
                slotFilterActive: !string.IsNullOrEmpty(inventoryEquipSlotFilter),
                appliedHighlight,
                dim);

            return new List<ColoredText>
            {
                new ColoredText($"Sort: {sortLabel} (+)", sortApplied ? appliedHighlight : dim),
                new ColoredText(" | ", dim),
                new ColoredText($"Filter: {filterLabel}{hiddenText} (-)", filterApplied ? appliedHighlight : dim),
                new ColoredText(" | ", dim),
                new ColoredText($"Bag slot: {slotSummary} (/)", bagClauseColor),
                new ColoredText(" | ", dim),
                new ColoredText(
                    $"Material: {(string.IsNullOrEmpty(inventoryMaterialFilter) ? "all" : inventoryMaterialFilter + " only")} (M)",
                    string.IsNullOrEmpty(inventoryMaterialFilter) ? dim : appliedHighlight),
            };
        }

        /// <summary>
        /// Color for the bag-slot summary segment: with an active slot filter, lime (same as other applied options) unless the item in that equipped slot is requirement-blocked (red).
        /// With no slot filter, neutral dim (same as inactive sort/filter), not first-row rarity.
        /// </summary>
        internal static Color ResolveBagSlotSummaryColor(
            Character character,
            string? inventoryEquipSlotFilter,
            bool slotFilterActive,
            Color appliedHighlight,
            Color dim)
        {
            if (slotFilterActive && !string.IsNullOrEmpty(inventoryEquipSlotFilter))
            {
                Item? equipped = GetEquippedItemForInventorySlotFilter(character, inventoryEquipSlotFilter);
                if (equipped != null && ItemRendererHelper.IsEquipBlockedForCharacter(equipped, character))
                    return AsciiArtAssets.Colors.Red;
                return appliedHighlight;
            }

            return dim;
        }

        private static Item? GetEquippedItemForInventorySlotFilter(Character character, string slotKey) => slotKey switch
        {
            "weapon" => character.Weapon,
            "head" => character.Head,
            "body" => character.Body,
            "legs" => character.Legs,
            "feet" => character.Feet,
            _ => null
        };

        /// <summary>Short label for inventory slot-only filter (matches equip menu wording).</summary>
        public static string GetInventoryEquipSlotFilterDisplayName(string equipSlotKey) => equipSlotKey switch
        {
            "weapon" => "Weapon",
            "head" => "Head",
            "body" => "Body",
            "legs" => "Legs",
            "feet" => "Feet",
            _ => equipSlotKey
        };

        private static int GetRaritySortRank(string? rarity)
        {
            return (rarity ?? "Common").Trim().ToLowerInvariant() switch
            {
                "transcendent" => 6,
                "mythic" => 5,
                "legendary" => 4,
                "epic" => 3,
                "rare" => 2,
                "uncommon" => 1,
                _ => 0
            };
        }

        private static int GetSlotSortRank(Item item)
        {
            if (item == null)
                return int.MaxValue;

            return item.Type switch
            {
                ItemType.Weapon => 0,
                ItemType.Head => 1,
                ItemType.Chest => 2,
                ItemType.Legs => 3,
                ItemType.Feet => 4,
                ItemType.Charm => 5,
                ItemType.Consumable => 6,
                _ => 99
            };
        }

        private static int GetWeaponTypeSortRank(Item item)
        {
            if (item == null || item.Type != ItemType.Weapon)
                return 0;

            if (!GearActionNames.TryResolveWeaponType(item, out var weaponType))
                return int.MaxValue;

            int orderIndex = Array.IndexOf(ClassPresentationConfig.ClassWeaponOrder, weaponType);
            return orderIndex >= 0 ? orderIndex : int.MaxValue - 1;
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
            // Header + 4 action rows (equip/unequip, discard/trade-up, snapshot, exit) + shortcut hint padding.
            return contentStartY + contentHeight - 12 - confirmationRows;
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

        /// <summary>Rows reserved for scroll hints: one at the bottom when scrolling is needed, plus one at the top when scrolled down.</summary>
        public static int GetReservedIndicatorRows(int firstVisibleIndex, bool requiresScrollStatus)
        {
            if (!requiresScrollStatus)
                return 0;

            return firstVisibleIndex > 0 ? 2 : 1;
        }

        public static string BuildTopScrollHint(int hiddenItemCount)
        {
            string arrow = AsciiArtAssets.UIElements.ArrowUp;
            return hiddenItemCount == 1
                ? $"{arrow} 1 item above — scroll up"
                : $"{arrow} {hiddenItemCount} items above — scroll up";
        }

        public static string BuildBottomScrollHint(InventoryItemVisibleRange visibleRange, int firstDisplay, int lastDisplay)
        {
            string arrowDown = AsciiArtAssets.UIElements.ArrowDown;
            string arrowUp = AsciiArtAssets.UIElements.ArrowUp;
            int hiddenBelow = visibleRange.ItemCount - visibleRange.LastExclusiveIndex;

            if (visibleRange.HasItemsBelow)
            {
                string hiddenText = hiddenBelow == 1 ? "1 more item below" : $"{hiddenBelow} more items below";
                return $"{arrowDown} {hiddenText} — scroll down ({firstDisplay}-{lastDisplay} of {visibleRange.ItemCount})";
            }

            if (visibleRange.HasItemsAbove)
                return $"{arrowUp} End of list ({firstDisplay}-{lastDisplay} of {visibleRange.ItemCount}) — scroll up";

            return $"Showing {firstDisplay}-{lastDisplay} of {visibleRange.ItemCount}";
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

