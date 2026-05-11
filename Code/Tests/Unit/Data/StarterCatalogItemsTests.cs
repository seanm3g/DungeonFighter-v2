using System;
using System.Collections.Generic;
using System.Linq;
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
                if (slotKey != "head" && slotKey != "chest" && slotKey != "legs" && slotKey != "feet")
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
                TestBase.AssertTrue(resolved.Count <= tagged.Count,
                    "Starter weapon menu should not list more rows than starter-tagged catalog entries",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                static int TypeOrderIndex(WeaponData w)
                {
                    if (!Enum.TryParse(w.Type?.Trim(), ignoreCase: true, out WeaponType wt))
                        return int.MaxValue;
                    int ix = Array.IndexOf(ClassPresentationConfig.ClassWeaponOrder, wt);
                    return ix >= 0 ? ix : int.MaxValue;
                }

                var expectedOrdered = tagged
                    .Select((w, taggedIndex) => (w, taggedIndex))
                    .OrderBy(x => TypeOrderIndex(x.w))
                    .ThenBy(x => x.taggedIndex)
                    .Select(x => x.w)
                    .ToList();
                var seen = new HashSet<WeaponType>();
                var expected = new List<WeaponData>();
                foreach (var w in expectedOrdered)
                {
                    if (!Enum.TryParse(w.Type?.Trim(), ignoreCase: true, out WeaponType wt))
                        continue;
                    if (!seen.Add(wt))
                        continue;
                    expected.Add(w);
                }

                TestBase.AssertEqual(expected.Count, resolved.Count,
                    "Starter menu should keep first starter-tagged row per weapon type (class path order)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(ClassPresentationConfig.ClassWeaponOrder.Length, resolved.Count,
                    "Starter menu should include one starter-tagged row for every class weapon path",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                int comparableCount = Math.Min(expected.Count, resolved.Count);
                for (int i = 0; i < comparableCount; i++)
                {
                    TestBase.AssertTrue(ReferenceEquals(expected[i], resolved[i]),
                        $"starter menu row {i} should match deduped catalog row reference",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }

                int prevOrder = -1;
                for (int i = 0; i < resolved.Count; i++)
                {
                    TestBase.AssertTrue(
                        Enum.TryParse(resolved[i].Type?.Trim(), ignoreCase: true, out WeaponType wt),
                        $"starter menu row {i} should parse to WeaponType",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    int ord = Array.IndexOf(ClassPresentationConfig.ClassWeaponOrder, wt);
                    TestBase.AssertTrue(ord >= 0,
                        $"starter menu row {i} type should appear in ClassWeaponOrder",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertTrue(ord >= prevOrder,
                        "Starter weapon menu should follow Barbarian→Warrior→Rogue→Wizard path order",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    prevOrder = ord;
                }

                foreach (WeaponType expectedType in ClassPresentationConfig.ClassWeaponOrder)
                {
                    bool hasType = resolved.Any(w =>
                        Enum.TryParse(w.Type?.Trim(), ignoreCase: true, out WeaponType wt) &&
                        wt == expectedType);
                    TestBase.AssertTrue(hasType,
                        $"Starter weapon menu should include {expectedType}",
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
