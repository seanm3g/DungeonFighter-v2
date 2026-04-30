using System;
using RPGGame;
using RPGGame.Data;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Tests for <see cref="InventoryActionPoolEntries"/> (bag action rows for the inventory pool UI).
    /// </summary>
    public static class InventoryActionPoolEntriesTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== InventoryActionPoolEntries Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            TestEmptyInventory(ref run, ref passed, ref failed);
            TestWeaponWithGearAction(ref run, ref passed, ref failed);
            TestGetEquipSlot(ref run, ref passed, ref failed);

            TestBase.PrintSummary("InventoryActionPoolEntries Tests", run, passed, failed);
        }

        private static void TestEmptyInventory(ref int run, ref int passed, ref int failed)
        {
            var c = TestDataBuilders.Character().WithName("NoBag").Build();
            c.Inventory.Clear();
            var list = InventoryActionPoolEntries.Build(c);
            TestBase.AssertTrue(list.Count == 0, "Empty inventory yields no entries", ref run, ref passed, ref failed);
        }

        private static void TestWeaponWithGearAction(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var w = TestDataBuilders.Weapon().WithName("BagBlade").Build();
            w.GearAction = "JAB";
            var c = TestDataBuilders.Character().WithName("HasBag").Build();
            c.Inventory.Clear();
            c.Inventory.Add(w);

            var list = InventoryActionPoolEntries.Build(c);
            TestBase.AssertTrue(list.Count >= 1, "Weapon in bag contributes at least one entry", ref run, ref passed, ref failed);
            int jabIx = list.FindIndex(e => string.Equals(e.ActionName, "JAB", StringComparison.OrdinalIgnoreCase));
            TestBase.AssertTrue(jabIx >= 0, "GearAction JAB appears in bag action rows", ref run, ref passed, ref failed);
            var jabEntry = list[jabIx];
            TestBase.AssertTrue(jabEntry.InventoryIndex == 0,
                "JAB row is for bag index 0", ref run, ref passed, ref failed);
            TestBase.AssertTrue(jabEntry.ActionIndexInItem >= 0,
                "JAB has a valid action index on the item", ref run, ref passed, ref failed);
        }

        private static void TestGetEquipSlot(ref int run, ref int passed, ref int failed)
        {
            var w = TestDataBuilders.Weapon().Build();
            TestBase.AssertTrue(InventoryActionPoolEntries.GetEquipSlotForItem(w) == "weapon",
                "Weapon maps to weapon slot", ref run, ref passed, ref failed);
            var h = TestDataBuilders.Armor().WithType(ItemType.Head).Build();
            TestBase.AssertTrue(InventoryActionPoolEntries.GetEquipSlotForItem(h) == "head",
                "Head maps to head slot", ref run, ref passed, ref failed);
        }
    }
}
