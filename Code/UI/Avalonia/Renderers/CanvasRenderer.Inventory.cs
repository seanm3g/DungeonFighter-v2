using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Handlers.Inventory;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.UI.Avalonia.Renderers
{
    public partial class CanvasRenderer
    {
        public void RenderInventory(Character character, List<Item> inventory, CanvasContext context, string? pendingMutatingInventoryMenuAction = null, int itemScrollOffset = 0, InventoryItemSortMode sortMode = InventoryItemSortMode.InventoryOrder, bool hideRequirementBlockedItems = false, string? inventoryEquipSlotFilter = null, InventoryDetailViewState? detailView = null, string? inventoryMaterialFilter = null)
        {
            RenderWithLayout(character, "INVENTORY", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderInventory(contentX, contentY, contentWidth, contentHeight, character, inventory, pendingMutatingInventoryMenuAction, itemScrollOffset, sortMode, hideRequirementBlockedItems, inventoryEquipSlotFilter, detailView, inventoryMaterialFilter);
            }, context, null, null, null, clearCanvas: true, inventoryComboRightPanel: true);
            dungeonRenderer.RenderActionInfoStrip(character, damageLineMode: ResolveActionStripDamageLineMode(character));
            canvas.Refresh();
        }

        public void RenderItemSelectionPrompt(
            Character character,
            List<Item> inventory,
            string promptMessage,
            string actionType,
            CanvasContext context,
            InventoryItemSortMode sortMode = InventoryItemSortMode.InventoryOrder,
            bool hideRequirementBlockedItems = false,
            string? inventoryEquipSlotFilter = null,
            string? inventoryMaterialFilter = null)
        {
            RenderWithLayout(character, "INVENTORY", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderItemSelectionPrompt(
                    contentX,
                    contentY,
                    contentWidth,
                    contentHeight,
                    character,
                    inventory,
                    promptMessage,
                    actionType,
                    sortMode,
                    hideRequirementBlockedItems,
                    inventoryEquipSlotFilter,
                    inventoryMaterialFilter);
            }, context, null, null, null, inventoryComboRightPanel: true);
            dungeonRenderer.RenderActionInfoStrip(character, damageLineMode: ResolveActionStripDamageLineMode(character));
            canvas.Refresh();
        }

        public void RenderSlotSelectionPrompt(Character character, CanvasContext context)
        {
            RenderWithLayout(character, "INVENTORY", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderSlotSelectionPrompt(contentX, contentY, contentWidth, contentHeight, character);
            }, context, null, null, null, inventoryComboRightPanel: true);
            dungeonRenderer.RenderActionInfoStrip(character, damageLineMode: ResolveActionStripDamageLineMode(character));
            canvas.Refresh();
        }

        public void RenderRaritySelectionPrompt(Character character, List<IGrouping<string, Item>> rarityGroups, CanvasContext context)
        {
            RenderWithLayout(character, "INVENTORY", (contentX, contentY, contentWidth, contentHeight) =>
            {
                int tabRows = TradeUpTabRows();
                inventoryRenderer.RenderRaritySelectionPrompt(contentX, contentY + tabRows, contentWidth, contentHeight - tabRows, character, rarityGroups);
                DrawTradeUpTabs(contentX, contentY, contentWidth);
            }, context, null, null, null, inventoryComboRightPanel: true);
            dungeonRenderer.RenderActionInfoStrip(character, damageLineMode: ResolveActionStripDamageLineMode(character));
            canvas.Refresh();
        }

        public void RenderTradeUpPreview(Character character, List<Item> itemsToTrade, Item resultingItem, string currentRarity, string nextRarity, CanvasContext context)
        {
            RenderWithLayout(character, "INVENTORY", (contentX, contentY, contentWidth, contentHeight) =>
            {
                int tabRows = TradeUpTabRows();
                inventoryRenderer.RenderTradeUpPreview(contentX, contentY + tabRows, contentWidth, contentHeight - tabRows, character, itemsToTrade, resultingItem, currentRarity, nextRarity);
                DrawTradeUpTabs(contentX, contentY, contentWidth);
            }, context, null, null, null, inventoryComboRightPanel: true);
            dungeonRenderer.RenderActionInfoStrip(character, damageLineMode: ResolveActionStripDamageLineMode(character));
            canvas.Refresh();
        }

        public void RenderItemComparison(Character character, Item newItem, Item? currentItem, string slot, CanvasContext context, int newItemInventoryIndex = -1)
        {
            RenderWithLayout(character, "INVENTORY", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderItemComparison(contentX, contentY, contentWidth, contentHeight, character, newItem, currentItem, slot, newItemInventoryIndex);
            }, context, null, null, null, inventoryComboRightPanel: true);
            dungeonRenderer.RenderActionInfoStrip(character, damageLineMode: ResolveActionStripDamageLineMode(character));
            canvas.Refresh();
        }

        /// <summary>Rows the hub tab strip takes above the trade-up screens (0 outside the hub).</summary>
        private static int TradeUpTabRows() => Inventory.HubTabBar.IsActive && LayoutConstants.ActionStripAtBottom ? 2 : 0;

        /// <summary>Hub tabs over the trade-up screens; switching tabs cancels the unconfirmed trade-up.</summary>
        private void DrawTradeUpTabs(int contentX, int contentY, int contentWidth)
        {
            if (TradeUpTabRows() == 0)
                return;
            canvas.ClearTextInArea(contentX + 1, contentY, contentWidth - 2, 1);
            Inventory.HubTabBar.Draw(canvas, interactionManager.AddClickableElement, contentX + 2, contentY, contentWidth - 4, Inventory.HubTab.TradeUp);
        }

        public void RenderComboManagement(Character character, CanvasContext context)
        {
            if (Inventory.HubTabBar.IsActive)
            {
                // The hub's Actions tab: side panels stay, the editor takes the whole center column (strip band included,
                // since it shows the sequence itself), tab strip on top.
                RenderWithLayout(character, "ACTIONS", (contentX, contentY, contentWidth, contentHeight) =>
                {
                    int frameX = LayoutConstants.CENTER_PANEL_X;
                    int frameY = LayoutConstants.CENTER_COLUMN_FULL_Y;
                    int frameW = LayoutConstants.CENTER_PANEL_WIDTH;
                    int frameH = LayoutConstants.CENTER_COLUMN_FULL_HEIGHT;
                    canvas.ClearTextInArea(frameX, frameY, frameW, frameH);
                    canvas.ClearBoxesInArea(frameX, frameY, frameW, frameH);
                    canvas.AddBox(frameX, frameY, frameW, frameH, PanelJoin.Border(AsciiArtAssets.Colors.Cyan), CenterPanelModeTint.GetBackgroundColor());
                    PanelJoin.ApplyCenter(canvas, frameY, frameH);
                    var (x, y, w, h) = LayoutConstants.GetCenterColumnFullContentRect();
                    inventoryRenderer.RenderComboManagement(x, y, w, h, character, hubTabs: true);
                }, context, null, null, null, clearCanvas: true, inventoryComboRightPanel: true);
                canvas.Refresh();
                return;
            }

            // Full screen (no side panels or strip): the editor shows the sequence itself.
            RenderWithLayout(character, "ACTIONS", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderComboManagement(contentX, contentY, contentWidth, contentHeight, character);
            }, context, null, null, null, clearCanvas: true, usePersistentChrome: false);
            canvas.Refresh();
        }

        public void RenderComboActionSelection(Character character, string actionType, CanvasContext context)
        {
            RenderWithLayout(character, "ACTIONS", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderComboActionSelection(contentX, contentY, contentWidth, contentHeight, character, actionType);
            }, context, null, null, null, inventoryComboRightPanel: true);
            dungeonRenderer.RenderActionInfoStrip(character, damageLineMode: ResolveActionStripDamageLineMode(character));
            canvas.Refresh();
        }

        public void RenderComboReorderPrompt(Character character, string currentSequence, CanvasContext context)
        {
            RenderWithLayout(character, "ACTIONS", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderComboReorderPrompt(contentX, contentY, contentWidth, contentHeight, character, currentSequence);
            }, context, null, null, null, inventoryComboRightPanel: true);
            dungeonRenderer.RenderActionInfoStrip(character, damageLineMode: ResolveActionStripDamageLineMode(character));
            canvas.Refresh();
        }
    }
}
