using System.Collections.Generic;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    public static class ItemGenerationLabServiceTests
    {
        private static int _run;
        private static int _pass;
        private static int _fail;

        public static void RunAllTests()
        {
            System.Console.WriteLine("=== ItemGenerationLabService Tests ===\n");
            _run = _pass = _fail = 0;

            TestForcedGenerationConstraints();
            TestSortingByRarityThenTier();

            TestBase.PrintSummary("ItemGenerationLabService Tests", _run, _pass, _fail);
            TestBase.ClearCurrentTestName();
        }

        private static void TestForcedGenerationConstraints()
        {
            TestBase.SetCurrentTestName(nameof(TestForcedGenerationConstraints));

            var spec = new ItemGenerationSpec
            {
                ItemType = ItemGenerationItemType.Weapons,
                Rarity = "Rare",
                Tier = 3,
                WeaponType = WeaponType.Sword,
                ArmorSlot = ItemGenerationArmorSlot.Any,
                Seed = 42,
                PlayerLevel = 1,
                DungeonLevel = 1
            };

            var rows = ItemGenerationLabService.GenerateBatch(spec, 50);
            TestBase.AssertTrue(rows.Count > 0, "generates some rows", ref _run, ref _pass, ref _fail);

            bool allWeapons = true;
            bool allTier3 = true;
            bool allRare = true;

            foreach (var r in rows)
            {
                if (r.Item is not WeaponItem) allWeapons = false;
                if (r.Item.Tier != 3) allTier3 = false;
                if (!string.Equals(r.Item.Rarity?.Trim(), "Rare", System.StringComparison.OrdinalIgnoreCase)) allRare = false;
            }

            TestBase.AssertTrue(allWeapons, "all are weapons", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(allTier3, "all are tier 3", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(allRare, "all are Rare", ref _run, ref _pass, ref _fail);
        }

        private static void TestSortingByRarityThenTier()
        {
            TestBase.SetCurrentTestName(nameof(TestSortingByRarityThenTier));

            var commonT5 = new WeaponItem("Common Weapon", tier: 5, baseDamage: 10, baseAttackSpeed: 1.0, weaponType: WeaponType.Sword)
            {
                Rarity = "Common"
            };
            var rareT1 = new WeaponItem("Rare Weapon", tier: 1, baseDamage: 1, baseAttackSpeed: 1.0, weaponType: WeaponType.Sword)
            {
                Rarity = "Rare"
            };

            var input = new List<ItemGeneratedRow>
            {
                new() { Item = commonT5, Index = 1, SortKey = "a" },
                new() { Item = rareT1, Index = 2, SortKey = "b" },
            };

            var sorted = ItemGenerationLabService.SortBestToWorst(input);
            TestBase.AssertEqual("Rare", sorted[0].Rarity, "rarity dominates tier", ref _run, ref _pass, ref _fail);
        }
    }
}

