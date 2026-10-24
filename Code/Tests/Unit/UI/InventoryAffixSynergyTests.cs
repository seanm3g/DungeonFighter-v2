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
            var affixes = InventoryAffixSynergy.GetAffixKeys(hovered);

            TestBase.AssertEqual(2, affixes.Count, "Material and quality prefixes are not affixes", ref run, ref passed, ref failed);
            TestBase.AssertTrue(InventoryAffixSynergy.SharesAffix(sharesPrefix, hovered, affixes),
                "Shared adjective prefix glows", ref run, ref passed, ref failed);
            TestBase.AssertTrue(InventoryAffixSynergy.SharesAffix(sharesSuffix, hovered, affixes),
                "Shared suffix glows (case-insensitive)", ref run, ref passed, ref failed);
            TestBase.AssertFalse(InventoryAffixSynergy.SharesAffix(sharesMaterialAndQualityOnly, hovered, affixes),
                "Same material/quality alone does not glow", ref run, ref passed, ref failed);
            TestBase.AssertFalse(InventoryAffixSynergy.SharesAffix(hovered, hovered, affixes),
                "Hovered item does not glow for itself", ref run, ref passed, ref failed);
            TestBase.AssertFalse(InventoryAffixSynergy.SharesAffix(sharesPrefix, null, InventoryAffixSynergy.GetAffixKeys(null)),
                "Nothing glows without a hovered item", ref run, ref passed, ref failed);

            var light = new Item(ItemType.Head, "Light");
            light.Modifications.Add(new Modification { Name = "Light", PrefixCategory = "ADJECTIVE", Effect = "MULTI-HIT" });
            var acrobatic = new Item(ItemType.Feet, "Acrobatic");
            acrobatic.Modifications.Add(new Modification { Name = "Acrobatic", PrefixCategory = "ADJECTIVE", Effect = "MULTI-HIT" });
            TestBase.AssertTrue(InventoryAffixSynergy.SharesAffix(acrobatic, light, InventoryAffixSynergy.GetAffixKeys(light)),
                "Differently named prefixes with the same effect glow", ref run, ref passed, ref failed);

            var heavy = new Item(ItemType.Weapon, "Heavy");
            heavy.Modifications.Add(new Modification { Name = "Heavy", PrefixCategory = "ADJECTIVE", Effect = "DAMAGE" });
            var ofPower = new Item(ItemType.Feet, "Galoshes");
            ofPower.StatBonuses.Add(new StatBonus { Name = "of Power", StatType = "Damage", Value = 3 });
            TestBase.AssertTrue(InventoryAffixSynergy.SharesAffix(ofPower, heavy, InventoryAffixSynergy.GetAffixKeys(heavy)),
                "Prefix effect matches suffix stat type", ref run, ref passed, ref failed);

            var sturdy = new Item(ItemType.Head, "Sturdy");
            sturdy.Modifications.Add(new Modification { Name = "Sturdy", PrefixCategory = "ADJECTIVE", Effect = "HEALTH BONUS" });
            var ofVitality = new Item(ItemType.Legs, "Vitality");
            ofVitality.StatBonuses.Add(new StatBonus { Name = "of Vitality", StatType = "Health", Value = 10 });
            TestBase.AssertTrue(InventoryAffixSynergy.SharesAffix(ofVitality, sturdy, InventoryAffixSynergy.GetAffixKeys(sturdy)),
                "HEALTH BONUS prefix matches Health suffix", ref run, ref passed, ref failed);
            TestBase.AssertFalse(InventoryAffixSynergy.SharesAffix(ofVitality, heavy, InventoryAffixSynergy.GetAffixKeys(heavy)),
                "Different effects do not glow", ref run, ref passed, ref failed);

            var glassShoes = new Item(ItemType.Feet, "Glass Shoes") { Material = "Glass" };
            var glassHelm = new Item(ItemType.Head, "Glass Helm") { Material = "Glass" };
            TestBase.AssertFalse(InventoryAffixSynergy.SharesAffix(glassHelm, glassShoes, InventoryAffixSynergy.GetAffixKeys(glassShoes)),
                "Shared material alone does not glow in the bag", ref run, ref passed, ref failed);
            TestBase.AssertTrue(InventoryAffixSynergy.SharesAffix(glassHelm, glassShoes,
                    InventoryAffixSynergy.GetAffixKeys(glassShoes, includeMaterial: true), includeMaterial: true),
                "Shared material glows between equipped gear", ref run, ref passed, ref failed);
            var unknownA = new Item(ItemType.Head, "A") { Material = "Unknown" };
            var unknownB = new Item(ItemType.Legs, "B") { Material = "Unknown" };
            TestBase.AssertFalse(InventoryAffixSynergy.SharesAffix(unknownB, unknownA,
                    InventoryAffixSynergy.GetAffixKeys(unknownA, includeMaterial: true), includeMaterial: true),
                "Unknown material is not a set", ref run, ref passed, ref failed);

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
