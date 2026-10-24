using System;
using System.Collections.Generic;
using RPGGame.Tests;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Renderers.Inventory;

namespace RPGGame.Tests.Unit.UI
{
    public static class InventoryAffixSynergyTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== InventoryAffixSynergy Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            var hovered = MakeItem(new[] { ("Sturdy", ""), ("Bone", "MATERIAL"), ("Worn", "QUALITY") }, new[] { "of the Swift" });
            var sharesPrefix = MakeItem(new[] { ("Sturdy", "ADJECTIVE"), ("Iron", "MATERIAL") }, Array.Empty<string>());
            var sharesSuffix = MakeItem(new[] { ("Keen", "ADJECTIVE") }, new[] { "OF THE SWIFT" });
            var sharesMaterialAndQualityOnly = MakeItem(new[] { ("Worn", "QUALITY"), ("Bone", "MATERIAL") }, new[] { "of Agility" });
            var affixes = InventoryAffixSynergy.GetAffixNames(hovered);

            TestBase.AssertEqual(2, affixes.Count, "Material and quality prefixes are not affixes", ref run, ref passed, ref failed);
            TestBase.AssertTrue(InventoryAffixSynergy.SharesAffix(sharesPrefix, hovered, affixes),
                "Shared adjective prefix glows", ref run, ref passed, ref failed);
            TestBase.AssertTrue(InventoryAffixSynergy.SharesAffix(sharesSuffix, hovered, affixes),
                "Shared suffix glows (case-insensitive)", ref run, ref passed, ref failed);
            TestBase.AssertFalse(InventoryAffixSynergy.SharesAffix(sharesMaterialAndQualityOnly, hovered, affixes),
                "Same material/quality alone does not glow", ref run, ref passed, ref failed);
            TestBase.AssertFalse(InventoryAffixSynergy.SharesAffix(hovered, hovered, affixes),
                "Hovered item does not glow for itself", ref run, ref passed, ref failed);
            TestBase.AssertFalse(InventoryAffixSynergy.SharesAffix(sharesPrefix, null, InventoryAffixSynergy.GetAffixNames(null)),
                "Nothing glows without a hovered item", ref run, ref passed, ref failed);

            var inventory = new List<Item> { sharesPrefix, hovered };
            TestBase.AssertTrue(ReferenceEquals(hovered,
                    InventoryAffixSynergy.ResolveHoveredItem(LeftPanelHoverState.Prefix + "inv:1", inventory, null)),
                "Bag hover id resolves to inventory item", ref run, ref passed, ref failed);
            TestBase.AssertTrue(InventoryAffixSynergy.ResolveHoveredItem(LeftPanelHoverState.Prefix + "inv:9", inventory, null) == null,
                "Out-of-range bag index resolves to null", ref run, ref passed, ref failed);
            TestBase.AssertTrue(InventoryAffixSynergy.ResolveHoveredItem(LeftPanelHoverState.Prefix + "stat:damage", inventory, null) == null,
                "Non-item hover resolves to null", ref run, ref passed, ref failed);

            TestBase.PrintSummary("InventoryAffixSynergy Tests", run, passed, failed);
        }

        private static Item MakeItem((string Name, string Category)[] mods, string[] suffixes)
        {
            var item = new Item(ItemType.Head, "Test");
            foreach (var (name, category) in mods)
                item.Modifications.Add(new Modification { Name = name, PrefixCategory = category });
            foreach (var suffix in suffixes)
                item.StatBonuses.Add(new StatBonus { Name = suffix });
            return item;
        }
    }
}
