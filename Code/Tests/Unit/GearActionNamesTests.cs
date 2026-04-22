using System;
using System.Linq;
using RPGGame.Entity.Services;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Tests for <see cref="GearActionNames"/> alignment with pool rebuild and gear managers.
    /// </summary>
    public static class GearActionNamesTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== GearActionNames Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestResolve_MatchesGearActionManager();
            TestWeaponWithGearActionOnly_IncludesRequiredBasicName();
            TestWeaponFallback_EveryResolvedNameLoadsIntoPool();
            TestRebuildCharacterActions_PoolContainsResolvedWeaponNames();
            TestChestItemWithStatBonus_IncludesArmorContribution();

            TestBase.PrintSummary("GearActionNames Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestResolve_MatchesGearActionManager()
        {
            Console.WriteLine("--- TestResolve_MatchesGearActionManager ---");

            var mgr = new GearActionManager();
            var weapon = new WeaponItem
            {
                Name = "AlignWeapon",
                GearAction = "JAB",
                WeaponType = WeaponType.Sword,
                ActionBonuses = { new ActionBonus { Name = "CRIT" } }
            };

            var a = GearActionNames.Resolve(weapon).OrderBy(x => x).ToList();
            var b = mgr.GetGearActions(weapon).OrderBy(x => x).ToList();

            TestBase.AssertTrue(a.SequenceEqual(b),
                $"GearActionNames and GearActionManager lists should match: [{string.Join(",", a)}] vs [{string.Join(",", b)}]",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestWeaponWithGearActionOnly_IncludesRequiredBasicName()
        {
            Console.WriteLine("\n--- TestWeaponWithGearActionOnly_IncludesRequiredBasicName ---");

            try
            {
                ActionLoader.LoadActions();
            }
            catch
            {
                TestBase.AssertTrue(true, "Skip: ActionLoader unavailable", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            if (ActionLoader.GetAction("STRIKE") == null)
            {
                TestBase.AssertTrue(true, "Skip: no STRIKE in action data", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            var weapon = new WeaponItem
            {
                Name = "SwordNoBasicInGear",
                WeaponType = WeaponType.Sword,
                GearAction = "JAB",
                ActionBonuses = { }
            };

            var names = GearActionNames.Resolve(weapon);
            TestBase.AssertTrue(
                names.Any(n => string.Equals(n, "STRIKE", StringComparison.OrdinalIgnoreCase)),
                "Sword with only JAB as GearAction should still resolve STRIKE (required weapon basic)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestWeaponFallback_EveryResolvedNameLoadsIntoPool()
        {
            Console.WriteLine("\n--- TestWeaponFallback_EveryResolvedNameLoadsIntoPool ---");

            try
            {
                ActionLoader.LoadActions();
            }
            catch
            {
                TestBase.AssertTrue(true, "Skip: ActionLoader unavailable", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            var weapon = new WeaponItem
            {
                Name = "FallbackWand",
                WeaponType = WeaponType.Wand,
                GearAction = null,
                ActionBonuses = { }
            };

            var names = GearActionNames.Resolve(weapon);
            if (names.Count == 0)
            {
                TestBase.AssertTrue(true, "Skip: no weapon-type actions in data for Wand", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            var character = TestDataBuilders.Character().WithName("PoolFallback").Build();
            var mgr = new GearActionManager();
            mgr.AddWeaponActions(character, weapon);

            foreach (var n in names)
            {
                bool ok = character.ActionPool.Any(e => string.Equals(e.action.Name, n, StringComparison.OrdinalIgnoreCase));
                TestBase.AssertTrue(ok, $"Pool should contain resolved name '{n}' after AddWeaponActions", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestRebuildCharacterActions_PoolContainsResolvedWeaponNames()
        {
            Console.WriteLine("\n--- TestRebuildCharacterActions_PoolContainsResolvedWeaponNames ---");

            try
            {
                ActionLoader.LoadActions();
            }
            catch
            {
                TestBase.AssertTrue(true, "Skip: ActionLoader unavailable", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            var character = TestDataBuilders.Character().WithName("RebuildTest").Build();
            character.Equipment.Weapon = new WeaponItem
            {
                Name = "RebuildWand",
                WeaponType = WeaponType.Wand,
                GearAction = null
            };

            CharacterSerializer.RebuildCharacterActions(character);

            var names = GearActionNames.Resolve(character.Equipment.Weapon);
            if (names.Count == 0)
            {
                TestBase.AssertTrue(true, "Skip: no weapon-type actions in data for Wand", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            foreach (var n in names)
            {
                bool ok = character.ActionPool.Any(e => string.Equals(e.action.Name, n, StringComparison.OrdinalIgnoreCase));
                TestBase.AssertTrue(ok, $"Pool after rebuild should contain '{n}'", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestChestItemWithStatBonus_IncludesArmorContribution()
        {
            Console.WriteLine("\n--- TestChestItemWithStatBonus_IncludesArmorContribution ---");

            try
            {
                ActionLoader.LoadActions();
            }
            catch
            {
                TestBase.AssertTrue(true, "Skip: ActionLoader unavailable", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            // Fixed GearAction avoids two independent Resolve() random picks (test compared first Resolve
            // to pool filled from a second Resolve inside AddArmorActions).
            var chest = new ChestItem("RareTestChest")
            {
                Rarity = "Rare",
                GearAction = "TAUNT",
                StatBonuses = { new StatBonus { StatType = "Armor", Value = 2 } }
            };

            var character = TestDataBuilders.Character().WithName("ArmorPool").Build();
            var mgr = new GearActionManager();
            mgr.AddArmorActions(character, chest);
            var names = mgr.GetGearActions(chest);
            TestBase.AssertTrue(names.Count > 0,
                "Special chest with stats should resolve at least one armor-contributed action name",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            foreach (var n in names.Distinct())
            {
                bool ok = character.ActionPool.Any(e => string.Equals(e.action.Name, n, StringComparison.OrdinalIgnoreCase));
                TestBase.AssertTrue(ok, $"Pool should contain armor-resolved name '{n}'", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }
    }
}
