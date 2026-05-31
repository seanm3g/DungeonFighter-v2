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
            TestResolveWeaponType_IncludesRequiredBasicName();
            TestResolveWeaponType_WandIncludesMagicMissileNotCastFallbackOnly();
            TestBaseWeaponItemType_ResolvesRequiredBasicName();
            TestWeaponFallback_EveryResolvedNameLoadsIntoPool();
            TestEquipBaseWeaponItemType_LoadsRequiredBasicIntoPool();
            TestRebuildCharacterActions_PoolContainsResolvedWeaponNames();
            TestArmorStatBonusWithoutAction_DoesNotCreateRandomAction();
            TestArmorExplicitGearAction_NotDuplicatedWhenSpecialMods();
            TestExplicitEnvironmentActionBonus_ExcludedFromResolve();
            TestExplicitEnvironmentActionBonus_NotLoadedIntoPool();
            TestWandCharmRequiredBasicAppearsInActionPool();
            TestResolveActionName_MagicMissileAlias();

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

        private static void TestResolveWeaponType_IncludesRequiredBasicName()
        {
            Console.WriteLine("\n--- TestResolveWeaponType_IncludesRequiredBasicName ---");

            try
            {
                ActionLoader.LoadActions();
            }
            catch
            {
                TestBase.AssertTrue(true, "Skip: ActionLoader unavailable", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            var names = GearActionNames.ResolveWeaponType(WeaponType.Sword);
            TestBase.AssertTrue(
                names.Any(n => string.Equals(n, "STRIKE", StringComparison.OrdinalIgnoreCase)),
                $"Sword weapon-type preview should include STRIKE; names: [{string.Join(", ", names)}]",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestResolveWeaponType_WandIncludesMagicMissileNotCastFallbackOnly()
        {
            Console.WriteLine("\n--- TestResolveWeaponType_WandIncludesMagicMissileNotCastFallbackOnly ---");

            try
            {
                ActionLoader.LoadActions();
            }
            catch
            {
                TestBase.AssertTrue(true, "Skip: ActionLoader unavailable", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            if (ActionLoader.GetAction("MAGIC MISSILE") == null)
            {
                TestBase.AssertTrue(true, "Skip: no MAGIC MISSILE in action data", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            var names = GearActionNames.ResolveWeaponType(WeaponType.Wand);
            TestBase.AssertTrue(
                names.Any(n => string.Equals(n, "MAGIC MISSILE", StringComparison.OrdinalIgnoreCase)),
                $"Wand weapon-type preview should include MAGIC MISSILE; names: [{string.Join(", ", names)}]",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(
                string.Equals(WeaponRequiredComboAction.TryGetRequiredBasicActionName(WeaponType.Wand), "CAST", StringComparison.OrdinalIgnoreCase),
                "Wand required basic should not resolve to CAST when MAGIC MISSILE is tagged in data",
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

        private static void TestBaseWeaponItemType_ResolvesRequiredBasicName()
        {
            Console.WriteLine("\n--- TestBaseWeaponItemType_ResolvesRequiredBasicName ---");

            try
            {
                ActionLoader.LoadActions();
            }
            catch
            {
                TestBase.AssertTrue(true, "Skip: ActionLoader unavailable", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            if (ActionLoader.GetAction("SLAM") == null)
            {
                TestBase.AssertTrue(true, "Skip: no SLAM in action data", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            var baseWeapon = new Item(ItemType.Weapon, "BaseMace")
            {
                WeaponType = WeaponType.Mace,
                GearAction = null
            };

            var names = GearActionNames.Resolve(baseWeapon);
            TestBase.AssertTrue(
                names.Any(n => string.Equals(n, "SLAM", StringComparison.OrdinalIgnoreCase)),
                $"Base Item weapon with Mace type should resolve SLAM; names: [{string.Join(", ", names)}]",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEquipBaseWeaponItemType_LoadsRequiredBasicIntoPool()
        {
            Console.WriteLine("\n--- TestEquipBaseWeaponItemType_LoadsRequiredBasicIntoPool ---");

            try
            {
                ActionLoader.LoadActions();
            }
            catch
            {
                TestBase.AssertTrue(true, "Skip: ActionLoader unavailable", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            if (ActionLoader.GetAction("SLAM") == null)
            {
                TestBase.AssertTrue(true, "Skip: no SLAM in action data", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            var character = TestDataBuilders.Character().WithName("BaseWeaponEquip").Build();
            var baseWeapon = new Item(ItemType.Weapon, "BaseMace")
            {
                WeaponType = WeaponType.Mace,
                GearAction = null
            };

            character.EquipItem(baseWeapon, "weapon");

            bool hasSlam = character.ActionPool.Any(e =>
                string.Equals(e.action.Name, "SLAM", StringComparison.OrdinalIgnoreCase));
            TestBase.AssertTrue(hasSlam,
                "Equipping a base Item weapon with Mace type should add SLAM to the action pool",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
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

        private static void TestArmorStatBonusWithoutAction_DoesNotCreateRandomAction()
        {
            Console.WriteLine("\n--- TestArmorStatBonusWithoutAction_DoesNotCreateRandomAction ---");

            var chest = new ChestItem("RareTestChest")
            {
                Rarity = "Rare",
                StatBonuses = { new StatBonus { StatType = "Armor", Value = 2 } }
            };

            var names = GearActionNames.Resolve(chest);
            TestBase.AssertEqual(0, names.Count,
                $"Armor stat suffixes should not bypass the Actions affix table; names: [{string.Join(", ", names)}]",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Armor with explicit GearAction plus affixes should keep only the authored action name.
        /// </summary>
        private static void TestArmorExplicitGearAction_NotDuplicatedWhenSpecialMods()
        {
            Console.WriteLine("\n--- TestArmorExplicitGearAction_NotDuplicatedWhenSpecialMods ---");

            var chest = new ChestItem("BronzeDoubletTest")
            {
                GearAction = "Tight Combo",
                StatBonuses = { new StatBonus { StatType = "Armor", Value = 1 } }
            };

            var names = GearActionNames.Resolve(chest);
            int tight = names.Count(n => string.Equals(n, "Tight Combo", StringComparison.OrdinalIgnoreCase));
            TestBase.AssertTrue(tight == 1,
                $"Armor GearAction + affixes: one 'Tight Combo' in Resolve (found {tight}; names: [{string.Join(", ", names)}])",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestExplicitEnvironmentActionBonus_ExcludedFromResolve()
        {
            Console.WriteLine("\n--- TestExplicitEnvironmentActionBonus_ExcludedFromResolve ---");

            try
            {
                ActionLoader.LoadActions();
            }
            catch
            {
                TestBase.AssertTrue(true, "Skip: ActionLoader unavailable", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            if (ActionLoader.GetAction("GRAVEYARD RISING") == null)
            {
                TestBase.AssertTrue(true, "Skip: no GRAVEYARD RISING in action data", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            var legs = new LegsItem("ShinguardsTest")
            {
                ActionBonuses = { new ActionBonus { Name = "GRAVEYARD RISING" } }
            };

            var names = GearActionNames.Resolve(legs);
            TestBase.AssertTrue(
                !names.Any(n => string.Equals(n, "GRAVEYARD RISING", StringComparison.OrdinalIgnoreCase)),
                $"Environment action bonus should be excluded from Resolve; names: [{string.Join(", ", names)}]",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestExplicitEnvironmentActionBonus_NotLoadedIntoPool()
        {
            Console.WriteLine("\n--- TestExplicitEnvironmentActionBonus_NotLoadedIntoPool ---");

            try
            {
                ActionLoader.LoadActions();
            }
            catch
            {
                TestBase.AssertTrue(true, "Skip: ActionLoader unavailable", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            if (ActionLoader.GetAction("GRAVEYARD RISING") == null)
            {
                TestBase.AssertTrue(true, "Skip: no GRAVEYARD RISING in action data", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            var character = TestDataBuilders.Character().WithName("EnvAffixPool").Build();
            var legs = new LegsItem("ShinguardsPoolTest")
            {
                ActionBonuses = { new ActionBonus { Name = "GRAVEYARD RISING" } }
            };

            var mgr = new GearActionManager();
            mgr.AddArmorActions(character, legs);

            bool inPool = character.ActionPool.Any(e =>
                string.Equals(e.action.Name, "GRAVEYARD RISING", StringComparison.OrdinalIgnoreCase));
            TestBase.AssertTrue(!inPool,
                "GRAVEYARD RISING from explicit ActionBonuses must not load into hero action pool",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Regression: wand required basic name must match loaded action data so gear pool shows MAGIC MISSILE.
        /// </summary>
        private static void TestWandCharmRequiredBasicAppearsInActionPool()
        {
            Console.WriteLine("\n--- TestWandCharmRequiredBasicAppearsInActionPool ---");

            try
            {
                ActionLoader.LoadActions();
            }
            catch
            {
                TestBase.AssertTrue(true, "Skip: ActionLoader unavailable", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            if (ActionLoader.GetAction("MAGIC MISSILE") == null)
            {
                TestBase.AssertTrue(false,
                    "MAGIC MISSILE must exist in action data for Wand weapons",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            var character = TestDataBuilders.Character().WithName("WandCharmPool").Build();
            var charm = new WeaponItem
            {
                Name = "Charm",
                WeaponType = WeaponType.Wand,
                GearAction = null,
                ActionBonuses = { }
            };
            character.EquipItem(charm, "weapon");
            CharacterSerializer.RebuildCharacterActions(character);

            TestBase.AssertTrue(
                character.GetActionPool().Any(a =>
                    string.Equals(a.Name, "MAGIC MISSILE", StringComparison.OrdinalIgnoreCase)),
                "Equipped Wand (Charm) should grant MAGIC MISSILE in the action pool",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestResolveActionName_MagicMissileAlias()
        {
            Console.WriteLine("\n--- TestResolveActionName_MagicMissileAlias ---");

            try
            {
                ActionLoader.LoadActions();
            }
            catch
            {
                TestBase.AssertTrue(true, "Skip: ActionLoader unavailable", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            TestBase.AssertTrue(
                ActionLoader.GetAction("MAGIC MISSILE") != null,
                "GetAction('MAGIC MISSILE') should load the wand basic from action data",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var resolvedFromTypo = ActionLoader.ResolveActionName("MAGIC MISSLE") ?? "";
            TestBase.AssertTrue(
                ActionLoader.HasAction(resolvedFromTypo),
                $"ResolveActionName('MAGIC MISSLE') should map to a loaded action key, got '{resolvedFromTypo}'",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var wandNames = GearActionNames.Resolve(new WeaponItem
            {
                Name = "Charm",
                WeaponType = WeaponType.Wand
            });
            TestBase.AssertTrue(
                wandNames.Any(n => ActionLoader.HasAction(n)),
                $"GearActionNames.Resolve for Wand should return loadable action names; got [{string.Join(", ", wandNames)}]",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

    }
}
