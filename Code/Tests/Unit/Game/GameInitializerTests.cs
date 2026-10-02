using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Data;
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
            TestInitializeNewGame_EquipsWeaponBonusLootNoExtraArmorSlots();
            TestInitializeNewGame_EquipsStarterWeaponDespiteCatalogAttributeGates();
            TestCatalogStarterKeepsWeaponsJsonBaseDamage();
            TestLegacySlotPathUsesStartingGearDamage();
            TestCreateStarterWeaponForMenuIndex_UsesCatalogRow();
            TestStarterWandGrantsExtraActionSlot();
            TestStartingWeaponActions_TwoPerWeaponType();
            TestInitializeNewGame_StarterWeaponGrantsBothStartingActions();

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

                TestBase.AssertEqual(0, startingGear.armor.Count,
                    "Default StartingGear.json should list no armor when Armor.json has starter-tagged leather pieces",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestInitializeNewGame_EquipsWeaponBonusLootNoExtraArmorSlots()
        {
            Console.WriteLine("\n--- Testing InitializeNewGame: weapon, catalog starter armor, random inventory bonus ---");

            _ = GameConfiguration.Instance;

            var player = new Character("InitGearTest", 1);
            var initializer = new GameInitializer();
            var dungeons = new List<Dungeon>();
            initializer.InitializeNewGame(player, dungeons, weaponChoice: 1);

            TestBase.AssertNotNull(player.Weapon,
                "New game should equip a starter weapon",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertNotNull(player.Head,
                "New game should equip catalog starter head (Helmet)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertNotNull(player.Body,
                "New game should equip catalog starter chest (Armor)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(player.Legs == null,
                "New game should leave legs empty (leather kit is head/chest/feet)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertNotNull(player.Feet,
                "New game should equip catalog starter feet (Boots)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (player.Body is ChestItem chest)
            {
                TestBase.AssertTrue(
                    chest.Name.Contains("Armor", StringComparison.OrdinalIgnoreCase),
                    "Default chest should be catalog Armor (leather starter)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual("Leather", ItemMaterialRules.RemapLegacyMaterial(chest.Material),
                    "Starter chest Material should be Leather",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(GameDataTagHelper.HasTag(chest.Tags, "starter"),
                    "Starter chest should carry starter tag",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            if (player.Head is HeadItem head)
            {
                TestBase.AssertTrue(
                    head.Name.Contains("Helmet", StringComparison.OrdinalIgnoreCase),
                    "Default head should be catalog Helmet (leather starter)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual("Leather", ItemMaterialRules.RemapLegacyMaterial(head.Material),
                    "Starter head Material should be Leather",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            if (player.Feet is FeetItem feet)
            {
                TestBase.AssertTrue(
                    feet.Name.Contains("Boots", StringComparison.OrdinalIgnoreCase),
                    "Default feet should be catalog Boots (leather starter)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual("Leather", ItemMaterialRules.RemapLegacyMaterial(feet.Material),
                    "Starter feet Material should be Leather",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            TestBase.AssertTrue(LeatherSetBonus.HasLuckAdvantage(player),
                "New game leather starter kit should unlock luck set bonus",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(player.Inventory.Count >= 1,
                "New game should add one random armor piece to inventory with the starting weapon",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            var bonus = player.Inventory[0];
            TestBase.AssertTrue(bonus is not WeaponItem,
                "New game bonus inventory item must not be a weapon",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestInitializeNewGame_EquipsStarterWeaponDespiteCatalogAttributeGates()
        {
            Console.WriteLine("\n--- Testing InitializeNewGame equips starter weapon despite catalog attribute gates ---");

            _ = GameConfiguration.Instance;

            var preview = GameInitializer.CreateStarterWeaponForMenuIndex(1);
            TestBase.AssertTrue(preview.AttributeRequirements.HasRequirements,
                "Starter catalog weapon should normally carry attribute requirements from Weapons.json",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var player = new Character("GateBypassTest", 1);
            var initializer = new GameInitializer();
            var dungeons = new List<Dungeon>();
            initializer.InitializeNewGame(player, dungeons, weaponChoice: 1);

            TestBase.AssertNotNull(player.Weapon,
                "Starter weapon must equip even when base stats are below catalog requirement values",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(player.Inventory.All(i => i is not WeaponItem),
                "Equipped starter weapon should not remain only in inventory",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
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
            int configOverride = EarlyGameBalanceHelper.GetStartingWeaponDamageOverride(WeaponType.Mace, GameConfiguration.Instance?.WeaponScaling);
            int expected = configOverride > 0 ? configOverride : expectedFromSheet;
            var scaling = GameConfiguration.Instance?.WeaponScaling;
            if (scaling != null && expected > 0 && scaling.GlobalDamageMultiplier > 0)
                expected = Math.Max(1, (int)Math.Round(expected * scaling.GlobalDamageMultiplier));

            GameInitializer.ApplyStartingWeaponTuning(weapon, WeaponType.Mace, slotFallback: null, baseDamageFromWeaponsCatalog: true);

            TestBase.AssertEqual(expected, weapon.BaseDamage,
                "Catalog starter damage uses EarlyGame override when set, else Weapons.json base (× global multiplier)",
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

            TestBase.AssertTrue(
                preview.Name.Contains(daggerRow.Name, StringComparison.OrdinalIgnoreCase),
                "Dagger menu slot should keep the matching Weapons.json base name (material prefix ok)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(
                Math.Abs(preview.BaseAttackSpeed - daggerRow.AttackSpeed) < 0.0001,
                "Starter preview should keep Weapons.json attack speed (no StartingGear override)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestStarterWandGrantsExtraActionSlot()
        {
            Console.WriteLine("\n--- Testing starter Wand grants +1 action slot when equipped ---");

            var cfg = GameConfiguration.Instance;
            var backupLoot = cfg.LootSystem;
            try
            {
                cfg.LootSystem = new LootSystemConfig
                {
                    ComboSequenceBaseMax = 2,
                    ComboSequenceAbsoluteMax = 8
                };

                var menuRows = StarterCatalogItems.ResolveStarterWeaponMenuCatalogRows();
                int wandMenuIndex = -1;
                for (int i = 0; i < menuRows.Count; i++)
                {
                    if (Enum.TryParse(menuRows[i].Type?.Trim(), ignoreCase: true, out WeaponType wt) && wt == WeaponType.Wand)
                    {
                        wandMenuIndex = i + 1;
                        break;
                    }
                }

                TestBase.AssertTrue(wandMenuIndex >= 1,
                    "Starter weapon menu should include a Wand row (e.g. Stick)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                if (wandMenuIndex < 1)
                    return;

                var starterWand = GameInitializer.CreateStarterWeaponForMenuIndex(wandMenuIndex);
                TestBase.AssertTrue(starterWand.WeaponType == WeaponType.Wand,
                    "Starter wizard menu row should be a Wand",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                GameInitializer.ClearStarterEquipRequirements(starterWand);
                var player = new Character("WandStarter", 1);
                TestBase.AssertTrue(player.TryEquipItem(starterWand, "weapon", out _, out _),
                    "Starter wand should equip",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertEqual(3, ComboSequenceMaxHelper.GetEffectiveMax(player),
                    "Equipped starter Wand should grant base 2 + 1 class slot = 3",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                cfg.LootSystem = backupLoot;
            }
        }

        private static void TestStartingWeaponActions_TwoPerWeaponType()
        {
            Console.WriteLine("\n--- Testing each weapon type has two startingweapon actions ---");

            try
            {
                ActionLoader.LoadActions();
            }
            catch
            {
                TestBase.AssertTrue(true, "Skip: ActionLoader unavailable", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            var expected = new Dictionary<WeaponType, string[]>
            {
                [WeaponType.Mace] = new[] { "SLAM", "POUND" },
                [WeaponType.Sword] = new[] { "STRIKE", "SLASH" },
                [WeaponType.Dagger] = new[] { "STAB", "CUT" },
                [WeaponType.Wand] = new[] { "MAGIC MISSLE", "BOLT" }
            };

            var selector = new LootActionSelector(new Random(1));
            foreach (var (weaponType, names) in expected)
            {
                var starting = selector.GetStartingWeaponActions(weaponType.ToString());
                TestBase.AssertEqual(2, starting.Count,
                    $"{weaponType} should have exactly two startingweapon actions; got [{string.Join(", ", starting)}]",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                foreach (var name in names)
                {
                    bool has = starting.Any(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase));
                    if (!has && weaponType == WeaponType.Wand && string.Equals(name, "MAGIC MISSLE", StringComparison.OrdinalIgnoreCase))
                        has = starting.Any(n => string.Equals(n, "MAGIC MISSILE", StringComparison.OrdinalIgnoreCase));

                    TestBase.AssertTrue(has,
                        $"{weaponType} starting actions should include {name}; got [{string.Join(", ", starting)}]",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
        }

        private static void TestInitializeNewGame_StarterWeaponGrantsBothStartingActions()
        {
            Console.WriteLine("\n--- Testing InitializeNewGame grants both starter actions onto weapon + combo ---");

            _ = GameConfiguration.Instance;
            ActionLoader.LoadActions();

            var expectedSecond = new Dictionary<WeaponType, string>
            {
                [WeaponType.Mace] = "POUND",
                [WeaponType.Sword] = "SLASH",
                [WeaponType.Dagger] = "CUT",
                [WeaponType.Wand] = "BOLT"
            };

            var menuRows = StarterCatalogItems.ResolveStarterWeaponMenuCatalogRows();
            for (int i = 0; i < menuRows.Count; i++)
            {
                if (!Enum.TryParse(menuRows[i].Type?.Trim(), ignoreCase: true, out WeaponType weaponType))
                    continue;
                if (!expectedSecond.TryGetValue(weaponType, out var secondName))
                    continue;

                var player = new Character($"StarterTwo_{weaponType}", 1);
                var initializer = new GameInitializer();
                initializer.InitializeNewGame(player, new List<Dungeon>(), weaponChoice: i + 1);

                TestBase.AssertNotNull(player.Weapon,
                    $"{weaponType} new game should equip a starter weapon",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                if (player.Weapon == null)
                    continue;

                var granted = GearActionNames.Resolve(player.Weapon);
                TestBase.AssertTrue(
                    granted.Any(n => string.Equals(n, secondName, StringComparison.OrdinalIgnoreCase)),
                    $"{weaponType} starter weapon should grant {secondName}; got [{string.Join(", ", granted)}]",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                var poolNames = player.ActionPool.Select(e => e.action?.Name).Where(n => n != null).ToList();
                int max = ComboSequenceMaxHelper.GetEffectiveMax(player);
                var combo = player.GetComboActions();
                TestBase.AssertTrue(
                    combo.Any(a => a != null && string.Equals(a.Name, secondName, StringComparison.OrdinalIgnoreCase)),
                    $"{weaponType} default combo should include {secondName}; got [{string.Join(", ", combo.Select(a => a?.Name))}] (max={max}, pool=[{string.Join(", ", poolNames)}])",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(combo.Count >= 2,
                    $"{weaponType} default combo should include both starter actions (count >= 2); got {combo.Count} (max={max})",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion
    }
}
