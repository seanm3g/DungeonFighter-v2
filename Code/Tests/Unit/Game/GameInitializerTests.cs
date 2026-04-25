using System;
using RPGGame;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Game
{
    /// <summary>
    /// Comprehensive tests for GameInitializer
    /// Tests game initialization, character creation, starting equipment, and initial state
    /// </summary>
    public static class GameInitializerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all GameInitializer tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== GameInitializer Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestLoadStartingGear();
            TestCatalogStarterKeepsWeaponsJsonBaseDamage();
            TestLegacySlotPathUsesStartingGearDamage();
            TestCreateStarterWeaponForMenuIndex_UsesCatalogRow();

            TestBase.PrintSummary("GameInitializer Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            var initializer = new GameInitializer();
            TestBase.AssertNotNull(initializer,
                "GameInitializer should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Starting Gear Tests

        private static void TestLoadStartingGear()
        {
            Console.WriteLine("\n--- Testing LoadStartingGear ---");

            var initializer = new GameInitializer();
            var startingGear = initializer.LoadStartingGear();

            TestBase.AssertNotNull(startingGear,
                "LoadStartingGear should return starting gear data",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (startingGear != null)
            {
                TestBase.AssertNotNull(startingGear.weapons,
                    "Starting gear should have weapons list (always empty; weapons come from Weapons.json)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertEqual(0, startingGear.weapons.Count,
                    "StartingGear.json should not define separate starter weapons",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertNotNull(startingGear.armor,
                    "Starting gear should have armor list",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertTrue(startingGear.armor.Count > 0,
                    "Starting gear should load armor from StartingGear.json",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestCatalogStarterKeepsWeaponsJsonBaseDamage()
        {
            Console.WriteLine("\n--- Testing catalog starter keeps Weapons.json baseDamage ---");

            _ = GameConfiguration.Instance;

            TestBase.AssertTrue(WeaponTypeFromCatalog.TryGetFirstTierOneCatalogRow(WeaponType.Mace, out var row) && row != null,
                "Tier-1 mace row exists",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (row == null)
                return;

            var weapon = ItemGenerator.GenerateWeaponItem(row);
            int expectedFromSheet = weapon.BaseDamage;

            GameInitializer.ApplyStartingWeaponTuning(weapon, WeaponType.Mace, slotFallback: null, baseDamageFromWeaponsCatalog: true);

            TestBase.AssertEqual(expectedFromSheet, weapon.BaseDamage,
                "High slot damage must not replace catalog base damage when baseDamageFromWeaponsCatalog is true",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestLegacySlotPathUsesStartingGearDamage()
        {
            Console.WriteLine("\n--- Testing legacy slot path uses StartingGear damage ---");

            _ = GameConfiguration.Instance;

            var weapon = new WeaponItem("Temp", 1, 1, 1.0, WeaponType.Mace);
            var slot = new StartingWeapon { name = "Club", damage = 7.5, attackSpeed = 0.8 };

            GameInitializer.ApplyStartingWeaponTuning(weapon, WeaponType.Mace, slot, baseDamageFromWeaponsCatalog: false);

            TestBase.AssertEqual(8, weapon.BaseDamage,
                "Legacy path rounds StartingGear slot damage (7.5) to 8",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(Math.Abs(weapon.BaseAttackSpeed - 0.8) < 0.0001,
                "Legacy path applies slot attackSpeed override",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCreateStarterWeaponForMenuIndex_UsesCatalogRow()
        {
            Console.WriteLine("\n--- Testing CreateStarterWeaponForMenuIndex uses starter weapon menu row ---");

            _ = GameConfiguration.Instance;

            var menuRows = StarterCatalogItems.ResolveStarterWeaponMenuCatalogRows();
            TestBase.AssertTrue(menuRows.Count > 0,
                "Starter weapon menu should have at least one row",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (menuRows.Count == 0)
                return;

            int daggerMenuIndex = -1;
            WeaponData? daggerRow = null;
            for (int i = 0; i < menuRows.Count; i++)
            {
                if (Enum.TryParse(menuRows[i].Type?.Trim(), ignoreCase: true, out WeaponType wt) && wt == WeaponType.Dagger)
                {
                    daggerMenuIndex = i + 1;
                    daggerRow = menuRows[i];
                    break;
                }
            }

            TestBase.AssertTrue(daggerMenuIndex >= 1 && daggerRow != null,
                "Starter weapon menu should include a Dagger row",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (daggerRow == null)
                return;

            var preview = GameInitializer.CreateStarterWeaponForMenuIndex(daggerMenuIndex);

            TestBase.AssertEqual(daggerRow.Name, preview.Name,
                "Dagger menu slot should use the matching Weapons.json row name",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(
                Math.Abs(preview.BaseAttackSpeed - daggerRow.AttackSpeed) < 0.0001,
                "Starter preview should keep Weapons.json attack speed (no StartingGear override)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
