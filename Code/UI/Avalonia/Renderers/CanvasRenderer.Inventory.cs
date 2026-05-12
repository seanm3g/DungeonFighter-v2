using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Handlers.Inventory;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.UI.Avalonia.Renderers
{
    public partial class CanvasRenderer
    {
        public void RenderInventory(Character character, List<Item> inventory, CanvasContext context, string? pendingMutatingInventoryMenuAction = null, int itemScrollOffset = 0, InventoryItemSortMode sortMode = InventoryItemSortMode.InventoryOrder, bool hideRequirementBlockedItems = false)
        {
            RenderWithLayout(character, "INVENTORY", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderInventory(contentX, contentY, contentWidth, contentHeight, character, inventory, pendingMutatingInventoryMenuAction, itemScrollOffset, sortMode, hideRequirementBlockedItems);
            }, context, null, null, null, clearCanvas: true, inventoryComboRightPanel: true);
            dungeonRenderer.RenderActionInfoStrip(character);
            canvas.Refresh();
        }

        public void RenderItemSelectionPrompt(Character character, List<Item> inventory, string promptMessage, string actionType, CanvasContext context)
        {
            RenderWithLayout(character, "INVENTORY", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderItemSelectionPrompt(contentX, contentY, contentWidth, contentHeight, character, inventory, promptMessage, actionType);
            }, context, null, null, null, inventoryComboRightPanel: true);
            dungeonRenderer.RenderActionInfoStrip(character);
            canvas.Refresh();
        }

        public void RenderSlotSelectionPrompt(Character character, CanvasContext context)
        {
            RenderWithLayout(character, "INVENTORY", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderSlotSelectionPrompt(contentX, contentY, contentWidth, contentHeight, character);
            }, context, null, null, null, inventoryComboRightPanel: true);
            dungeonRenderer.RenderActionInfoStrip(character);
            canvas.Refresh();
        }

        public void RenderRaritySelectionPrompt(Character character, List<IGrouping<string, Item>> rarityGroups, CanvasContext context)
        {
            RenderWithLayout(character, "INVENTORY", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderRaritySelectionPrompt(contentX, contentY, contentWidth, contentHeight, character, rarityGroups);
            }, context, null, null, null, inventoryComboRightPanel: true);
            dungeonRenderer.RenderActionInfoStrip(character);
            canvas.Refresh();
        }

        public void RenderTradeUpPreview(Character character, List<Item> itemsToTrade, Item resultingItem, string currentRarity, string nextRarity, CanvasContext context)
        {
            RenderWithLayout(character, "INVENTORY", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderTradeUpPreview(contentX, contentY, contentWidth, contentHeight, character, itemsToTrade, resultingItem, currentRarity, nextRarity);
            }, context, null, null, null, inventoryComboRightPanel: true);
            dungeonRenderer.RenderActionInfoStrip(character);
            canvas.Refresh();
        }

        public void RenderItemComparison(Character character, Item newItem, Item? currentItem, string slot, CanvasContext context, int newItemInventoryIndex = -1)
        {
            RenderWithLayout(character, "INVENTORY", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderItemComparison(contentX, contentY, contentWidth, contentHeight, character, newItem, currentItem, slot, newItemInventoryIndex);
            }, context, null, null, null, inventoryComboRightPanel: true);
            dungeonRenderer.RenderActionInfoStrip(character);
            canvas.Refresh();
        }

        public void RenderComboManagement(Character character, CanvasContext context)
        {
            RenderWithLayout(character, "ACTIONS", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderComboManagement(contentX, contentY, contentWidth, contentHeight, character);
            }, context, null, null, null, inventoryComboRightPanel: true);
            dungeonRenderer.RenderActionInfoStrip(character);
            canvas.Refresh();
        }

        public void RenderComboActionSelection(Character character, string actionType, CanvasContext context)
        {
            RenderWithLayout(character, "ACTIONS", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderComboActionSelection(contentX, contentY, contentWidth, contentHeight, character, actionType);
            }, context, null, null, null, inventoryComboRightPanel: true);
            dungeonRenderer.RenderActionInfoStrip(character);
            canvas.Refresh();
        }

        public void RenderComboReorderPrompt(Character character, string currentSequence, CanvasContext context)
        {
            RenderWithLayout(character, "ACTIONS", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderComboReorderPrompt(contentX, contentY, contentWidth, contentHeight, character, currentSequence);
            }, context, null, null, null, inventoryComboRightPanel: true);
            dungeonRenderer.RenderActionInfoStrip(character);
            canvas.Refresh();
        }
    }
}
