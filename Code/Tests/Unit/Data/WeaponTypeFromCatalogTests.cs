using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Data;
using RPGGame.Tests;
using RPGGame.Utils;

namespace RPGGame.Tests.Unit.Data
{
    public static class WeaponTypeFromCatalogTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== WeaponTypeFromCatalog (starter row) Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            WeaponTypeFromCatalog.InvalidateCache();
            TestTryGetFirstTierOneMatchesInMemoryCatalog();

            TestBase.PrintSummary("WeaponTypeFromCatalog Tests", _testsRun, _testsPassed, _testsFailed);
        }

        /// <summary>Mirrors <see cref="WeaponTypeFromCatalog.TryGetFirstTierOneCatalogRow"/> selection against a row list.</summary>
        private static WeaponData? SelectExpectedFirstTierOne(IReadOnlyList<WeaponData> rows, WeaponType weaponType)
        {
            foreach (var w in rows)
            {
                if (w.Tier != 1)
                    continue;
                if (string.IsNullOrWhiteSpace(w.Type))
                    continue;
                if (!Enum.TryParse(w.Type.Trim(), ignoreCase: true, out WeaponType wt) || wt != weaponType)
                    continue;
                if (!GameDataTagHelper.HasTag(w.Tags, "starter"))
                    continue;
                return w;
            }

            foreach (var w in rows)
            {
                if (w.Tier != 1)
                    continue;
                if (string.IsNullOrWhiteSpace(w.Type))
                    continue;
                if (!Enum.TryParse(w.Type.Trim(), ignoreCase: true, out WeaponType wt) || wt != weaponType)
                    continue;
                return w;
            }

            return null;
        }

        private static void TestTryGetFirstTierOneMatchesInMemoryCatalog()
        {
            Console.WriteLine("--- Testing TryGetFirstTierOneCatalogRow vs Weapons.json list ---");

            var rows = JsonLoader.LoadJsonList<WeaponData>(GameConstants.WeaponsJson, useCache: true);
            TestBase.AssertTrue(rows.Count > 0,
                "Weapons.json should load at least one row for catalog tests",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            foreach (var weaponType in new[] { WeaponType.Dagger, WeaponType.Mace, WeaponType.Sword, WeaponType.Wand })
            {
                var expected = SelectExpectedFirstTierOne(rows, weaponType);
                TestBase.AssertNotNull(expected,
                    $"Expected a tier-1 {weaponType} row in Weapons.json",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                if (expected == null)
                    continue;

                TestBase.AssertTrue(
                    WeaponTypeFromCatalog.TryGetFirstTierOneCatalogRow(weaponType, out var actual) && actual != null,
                    $"TryGetFirstTierOneCatalogRow should find {weaponType}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                if (actual == null)
                    continue;

                TestBase.AssertEqual(expected.Name, actual.Name,
                    $"Tier-1 {weaponType} catalog row should match starter-preferring selection",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                bool anyStarterTagged = false;
                foreach (var w in rows)
                {
                    if (w.Tier != 1 || string.IsNullOrWhiteSpace(w.Type))
                        continue;
                    if (!Enum.TryParse(w.Type.Trim(), ignoreCase: true, out WeaponType wt) || wt != weaponType)
                        continue;
                    if (GameDataTagHelper.HasTag(w.Tags, "starter"))
                    {
                        anyStarterTagged = true;
                        break;
                    }
                }

                if (anyStarterTagged)
                {
                    TestBase.AssertTrue(GameDataTagHelper.HasTag(actual.Tags, "starter"),
                        $"When tier-1 {weaponType} rows include starter tag, resolved row should be tagged starter",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
        }
    }
}
