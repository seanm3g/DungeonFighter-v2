using System;
using RPGGame.Handlers.Inventory;
using RPGGame.Tests;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Renderers.Inventory;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>New-inventory paper doll drag and drop: drag state, doll sizing, drop bounds, slot unequip.</summary>
    public static class InventoryDragDropTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== Inventory drag and drop Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            TestPressInPlaceStaysAClick(ref run, ref passed, ref failed);
            TestLeavingPressCellActivatesDrag(ref run, ref passed, ref failed);
            TestPaperDollTileRowsFit(ref run, ref passed, ref failed);
            TestBoundsContain(ref run, ref passed, ref failed);
            TestUnequipSlotMovesGearToBag(ref run, ref passed, ref failed);

            InventoryDragState.Clear();
            TestBase.PrintSummary("Inventory drag and drop Tests", run, passed, failed);
        }

        private static void TestPressInPlaceStaysAClick(ref int run, ref int passed, ref int failed)
        {
            var item = TestDataBuilders.Weapon().WithName("Drag Sword").Build();
            InventoryDragState.ArmFromBag(3, item, 10, 20);
            bool redraw = InventoryDragState.MoveTo(10, 20);

            TestBase.AssertTrue(
                InventoryDragState.IsArmed && !InventoryDragState.IsActive && !redraw
                && InventoryDragState.FromBag && !InventoryDragState.FromSlot
                && InventoryDragState.SourceInventoryIndex == 3,
                "Pointer still on the press cell keeps the drag armed but inactive",
                ref run, ref passed, ref failed);

            InventoryDragState.Clear();
            TestBase.AssertTrue(
                !InventoryDragState.IsArmed && InventoryDragState.Item == null && InventoryDragState.SourceInventoryIndex == -1,
                "Clear drops the drag source",
                ref run, ref passed, ref failed);
        }

        private static void TestLeavingPressCellActivatesDrag(ref int run, ref int passed, ref int failed)
        {
            var item = TestDataBuilders.Armor().WithType(ItemType.Head).WithName("Drag Helm").Build();
            InventoryDragState.ArmFromSlot("head", item, 5, 5);
            bool first = InventoryDragState.MoveTo(6, 5);
            bool same = InventoryDragState.MoveTo(6, 5);
            bool back = InventoryDragState.MoveTo(5, 5);

            TestBase.AssertTrue(
                first && !same && back && InventoryDragState.IsActive
                && InventoryDragState.FromSlot && InventoryDragState.SourceSlot == "head"
                && InventoryDragState.PointerX == 5 && InventoryDragState.PointerY == 5,
                "Leaving the press cell activates the drag; it stays active and redraws per new cell",
                ref run, ref passed, ref failed);
            InventoryDragState.Clear();
        }

        private static void TestPaperDollTileRowsFit(ref int run, ref int passed, ref int failed)
        {
            TestBase.AssertTrue(
                InventoryScreenRenderer.PaperDollTileRows(40) == InventoryScreenRenderer.GridTileRows
                && InventoryScreenRenderer.PaperDollTileRows(20) == 4
                && InventoryScreenRenderer.PaperDollTileRows(19) == 3
                && InventoryScreenRenderer.PaperDollTileRows(16) == 3
                && InventoryScreenRenderer.PaperDollTileRows(15) == 0,
                "Paper doll uses grid-size slots when a name row plus four stacked slots fit, shrinks to 3 rows, else hides",
                ref run, ref passed, ref failed);
        }

        private static void TestBoundsContain(ref int run, ref int passed, ref int failed)
        {
            var bounds = (X: 10, Y: 4, Width: 5, Height: 3);
            TestBase.AssertTrue(
                InventoryScreenRenderer.BoundsContain(bounds, 10, 4)
                && InventoryScreenRenderer.BoundsContain(bounds, 14, 6)
                && !InventoryScreenRenderer.BoundsContain(bounds, 15, 6)
                && !InventoryScreenRenderer.BoundsContain(bounds, 14, 7)
                && !InventoryScreenRenderer.BoundsContain(default, 0, 0),
                "Drop bounds are half-open and empty bounds never hit",
                ref run, ref passed, ref failed);
        }

        private static void TestUnequipSlotMovesGearToBag(ref int run, ref int passed, ref int failed)
        {
            var sm = new GameStateManager();
            var character = TestDataBuilders.Character().WithName("DollUnequip").Build();
            var helm = TestDataBuilders.Armor().WithType(ItemType.Head).WithName("Doll Helm").Build();
            character.EquipItem(helm, "head");
            sm.SetCurrentPlayer(character);
            sm.TransitionToState(GameState.Inventory);

            int refreshes = 0;
            var handler = new InventoryItemActionHandler(sm, null, new InventoryStateManager());
            handler.ShowInventoryEvent += () => refreshes++;

            var moved = handler.UnequipSlot("head", refreshInventoryScreen: false);
            var empty = handler.UnequipSlot("head", refreshInventoryScreen: false);

            TestBase.AssertTrue(
                ReferenceEquals(moved, helm) && character.Head == null
                && sm.CurrentInventory.Count > 0 && ReferenceEquals(sm.CurrentInventory[^1], helm)
                && empty == null && refreshes == 0,
                "UnequipSlot moves the slot's item to the end of the bag and returns it (null when empty)",
                ref run, ref passed, ref failed);
        }
    }
}
