using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Data;
using RPGGame.Tests;
using RPGGame.Utils;

namespace RPGGame.Tests.Unit.Data
{
    public static class StarterCatalogItemsTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== StarterCatalogItems Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestLoadStarterArmorMatchesExpectedFromArmorJson();
            TestResolveStarterWeaponMenuCatalogRows();

            TestBase.PrintSummary("StarterCatalogItems Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static List<Item> ExpectedStarterArmorItemsFromRows(IReadOnlyList<ArmorData> rows)
        {
            var filledSlots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<Item>();
            foreach (var row in rows)
            {
                if (!GameDataTagHelper.HasTag(row.Tags, StarterCatalogItems.StarterTag))
                    continue;
                string slotKey = (row.Slot ?? "").Trim().ToLowerInvariant();
                if (slotKey != "head" && slotKey != "chest" && slotKey != "feet")
                    continue;
                if (!filledSlots.Add(slotKey))
                    continue;
                result.Add(ItemGenerator.GenerateArmorItem(row));
            }

            return result;
        }

        private static void TestLoadStarterArmorMatchesExpectedFromArmorJson()
        {
            Console.WriteLine("--- Testing LoadStarterArmorItems vs Armor.json starter tag ---");

            var rows = JsonLoader.LoadJsonList<ArmorData>(GameConstants.ArmorJson, useCache: true);
            var expected = ExpectedStarterArmorItemsFromRows(rows);
            var items = StarterCatalogItems.LoadStarterArmorItems();

            TestBase.AssertEqual(expected.Count, items.Count,
                "Starter armor piece count should match first-per-slot starter-tagged rows in Armor.json",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            for (int i = 0; i < items.Count; i++)
            {
                TestBase.AssertEqual(expected[i].Name, items[i].Name,
                    $"Starter armor order/name slot {i}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(GameDataTagHelper.HasTag(items[i].Tags, "starter"),
                    $"Item '{items[i].Name}' should carry starter tag",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestResolveStarterWeaponMenuCatalogRows()
        {
            Console.WriteLine("--- Testing ResolveStarterWeaponMenuCatalogRows ---");

            var tagged = StarterCatalogItems.LoadStarterTaggedWeaponRows();
            var resolved = StarterCatalogItems.ResolveStarterWeaponMenuCatalogRows();

            if (tagged.Count > 0)
            {
                TestBase.AssertEqual(tagged.Count, resolved.Count,
                    "When Weapons.json has starter-tagged rows, menu should list all of them in file order",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                for (int i = 0; i < tagged.Count; i++)
                {
                    TestBase.AssertEqual(tagged[i].Name, resolved[i].Name,
                        $"starter weapon menu row {i} name",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
            else
            {
                TestBase.AssertTrue(resolved.Count > 0,
                    "When no starter-tagged weapons, fallback should still yield at least one tier-1 class-path weapon",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }
    }
}
