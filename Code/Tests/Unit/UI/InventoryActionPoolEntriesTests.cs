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
            TestRequirementBlockedItemDoesNotContributeRows(ref run, ref passed, ref failed);
            TestDuplicateBagActionsCollapseToFirstCompatibleItem(ref run, ref passed, ref failed);
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

        private static void TestRequirementBlockedItemDoesNotContributeRows(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var blocked = TestDataBuilders.Weapon().WithName("TooQuickBlade").Build();
            blocked.GearAction = "JAB";
            blocked.AttributeRequirements.Add("agility", 99);
            var allowed = TestDataBuilders.Weapon().WithName("UsableBlade").Build();
            allowed.GearAction = "JAB";

            var c = TestDataBuilders.Character().WithName("ReqGate").WithStats(10, 10, 10, 10).Build();
            c.Inventory.Clear();
            c.Inventory.Add(blocked);
            c.Inventory.Add(allowed);

            var list = InventoryActionPoolEntries.Build(c);
            TestBase.AssertTrue(list.Count >= 1, "Allowed item contributes bag action rows", ref run, ref passed, ref failed);
            TestBase.AssertTrue(list.TrueForAll(e => e.InventoryIndex != 0),
                "Requirement-blocked item contributes no inventory action rows", ref run, ref passed, ref failed);
            TestBase.AssertTrue(list.Exists(e => e.InventoryIndex == 1),
                "Allowed item remains visible in inventory action rows", ref run, ref passed, ref failed);
        }

        private static void TestDuplicateBagActionsCollapseToFirstCompatibleItem(ref int run, ref int passed, ref int failed)
        {
            var first = TestDataBuilders.Armor().WithType(ItemType.Head).WithName("FirstHat").Build();
            first.GearAction = "TAUNT";
            var second = TestDataBuilders.Armor().WithType(ItemType.Chest).WithName("SecondVest").Build();
            second.GearAction = "taunt";

            var c = TestDataBuilders.Character().WithName("DedupBag").Build();
            c.Inventory.Clear();
            c.Inventory.Add(first);
            c.Inventory.Add(second);

            var list = InventoryActionPoolEntries.Build(c);
            int tauntCount = 0;
            int tauntIndex = -1;
            for (int i = 0; i < list.Count; i++)
            {
                if (!string.Equals(list[i].ActionName, "TAUNT", StringComparison.OrdinalIgnoreCase))
                    continue;

                tauntCount++;
                tauntIndex = i;
            }

            TestBase.AssertTrue(tauntCount == 1,
                "Duplicate bag action names collapse to one visible inventory pool row", ref run, ref passed, ref failed);
            TestBase.AssertTrue(tauntIndex >= 0 && list[tauntIndex].InventoryIndex == 0,
                "Collapsed bag action row keeps the first compatible item as its equip target", ref run, ref passed, ref failed);
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
