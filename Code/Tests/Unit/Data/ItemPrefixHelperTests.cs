using System.Collections.Generic;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    public static class ItemPrefixHelperTests
    {
        private static int _run;
        private static int _pass;
        private static int _fail;

        public static void RunAllTests()
        {
            System.Console.WriteLine("=== ItemPrefixHelper Tests ===\n");
            _run = _pass = _fail = 0;

            TestOrderedPrefixModifications();
            TestGearPrimaryStatMultiplier();
            TestMeetsMinimumItemRank();
            TestParsePrefixCategory();

            TestBase.PrintSummary("ItemPrefixHelper Tests", _run, _pass, _fail);
        }

        private static void TestOrderedPrefixModifications()
        {
            TestBase.SetCurrentTestName(nameof(TestOrderedPrefixModifications));
            var item = new WeaponItem("Sword", 1, 10, 1.0, WeaponType.Sword);
            item.Modifications = new List<Modification>
            {
                new() { Name = "Sharp", PrefixCategory = "", Effect = "damage" },
                new() { Name = "BONE", PrefixCategory = "Material", Effect = "equipmentStr" },
                new() { Name = "Broken", PrefixCategory = "Quality", Effect = "gearPrimaryStatMultiplier", RolledValue = 0.9 }
            };

            var ordered = ItemPrefixHelper.OrderedPrefixModifications(item.Modifications);
            TestBase.AssertEqual(3, ordered.Count, "count", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual("Broken", ordered[0].Name, "quality first", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual("Sharp", ordered[1].Name, "adjective second", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual("BONE", ordered[2].Name, "material third", ref _run, ref _pass, ref _fail);
        }

        private static void TestGearPrimaryStatMultiplier()
        {
            TestBase.SetCurrentTestName(nameof(TestGearPrimaryStatMultiplier));
            var item = new ChestItem("Robe", 1, 5);
            TestBase.AssertEqual(1.0, ItemPrefixHelper.GetGearPrimaryStatMultiplier(item), "no mods", ref _run, ref _pass, ref _fail);

            item.Modifications.Add(new Modification
            {
                Name = "New",
                PrefixCategory = "Quality",
                Effect = "gearPrimaryStatMultiplier",
                RolledValue = 1.1
            });
            TestBase.AssertEqual(1.1, ItemPrefixHelper.GetGearPrimaryStatMultiplier(item), "one quality", ref _run, ref _pass, ref _fail);
        }

        private static void TestMeetsMinimumItemRank()
        {
            TestBase.SetCurrentTestName(nameof(TestMeetsMinimumItemRank));
            TestBase.AssertTrue(ItemPrefixHelper.MeetsMinimumItemRank("Rare", "Uncommon"), "Rare >= Uncommon", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(!ItemPrefixHelper.MeetsMinimumItemRank("Common", "Rare"), "Common < Rare", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(ItemPrefixHelper.MeetsMinimumItemRank("Epic", ""), "empty min", ref _run, ref _pass, ref _fail);
        }

        private static void TestParsePrefixCategory()
        {
            TestBase.SetCurrentTestName(nameof(TestParsePrefixCategory));
            var m = new Modification { PrefixCategory = "material" };
            TestBase.AssertTrue(m.GetPrefixCategory() == ModificationPrefixCategory.Material, "material", ref _run, ref _pass, ref _fail);
            m.PrefixCategory = "";
            TestBase.AssertTrue(m.GetPrefixCategory() == ModificationPrefixCategory.Adjective, "default", ref _run, ref _pass, ref _fail);
        }
    }
}
