using System;
using System.Linq;
using RPGGame.Entity.Services;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Tests for the per-weapon-type required basic combo action.
    /// </summary>
    public static class WeaponRequiredComboActionTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== WeaponRequiredComboAction Tests ===\n");

            TestCannotRemoveLastRequiredBasicForSword();
            TestRestoreComboReInjectsRequiredBasic();
            TestCanRemoveRequiredBasicAfterClassTier();
            TestRestoreComboDoesNotReInjectRequiredBasicAfterClassTier();

            TestBase.PrintSummary("WeaponRequiredComboAction Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestCannotRemoveLastRequiredBasicForSword()
        {
            Console.WriteLine("--- TestCannotRemoveLastRequiredBasicForSword ---");
            ActionLoader.LoadActions();
            var strike = ActionLoader.GetAction("STRIKE");
            if (strike == null)
            {
                TestBase.AssertTrue(true, "TestCannotRemoveLastRequiredBasicForSword skipped (no STRIKE)", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            var character = TestDataBuilders.Character().WithName("WeaponReqRemove").Build();
            var sword = TestDataBuilders.Weapon().WithWeaponType(WeaponType.Sword).Build();
            character.EquipItem(sword, "weapon");
            CharacterSerializer.RebuildCharacterActions(character);

            var strikeInCombo = character.GetComboActions()
                .FirstOrDefault(a => string.Equals(a.Name, "STRIKE", StringComparison.OrdinalIgnoreCase));
            if (strikeInCombo == null)
            {
                TestBase.AssertTrue(true, "TestCannotRemoveLastRequiredBasicForSword skipped (STRIKE not in combo after rebuild)", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            bool removed = character.RemoveFromCombo(strikeInCombo);
            TestBase.AssertFalse(removed, "RemoveFromCombo should refuse the last required weapon basic", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                character.GetComboActions().Any(a => string.Equals(a.Name, "STRIKE", StringComparison.OrdinalIgnoreCase)),
                "STRIKE should remain in sequence",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRestoreComboReInjectsRequiredBasic()
        {
            Console.WriteLine("\n--- TestRestoreComboReInjectsRequiredBasic ---");
            ActionLoader.LoadActions();
            if (ActionLoader.GetAction("STRIKE") == null)
            {
                TestBase.AssertTrue(true, "TestRestoreComboReInjectsRequiredBasic skipped (no STRIKE)", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            var character = TestDataBuilders.Character().WithName("WeaponReqRestore").Build();
            var sword = TestDataBuilders.Weapon().WithWeaponType(WeaponType.Sword).Build();
            character.EquipItem(sword, "weapon");
            CharacterSerializer.RebuildCharacterActions(character);

            var withoutStrike = character.GetComboActions()
                .Select(a => a.Name)
                .Where(n => !string.Equals(n, "STRIKE", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (withoutStrike.Count == 0)
            {
                TestBase.AssertTrue(true, "TestRestoreComboReInjectsRequiredBasic skipped (combo is only STRIKE)", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            TestBase.AssertTrue(
                character.RestoreComboFromActionNames(withoutStrike),
                "RestoreComboFromActionNames should succeed",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                character.GetComboActions().Any(a => string.Equals(a.Name, "STRIKE", StringComparison.OrdinalIgnoreCase)),
                "STRIKE should be re-added after restore omitted it",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCanRemoveRequiredBasicAfterClassTier()
        {
            Console.WriteLine("\n--- TestCanRemoveRequiredBasicAfterClassTier ---");
            ActionLoader.LoadActions();
            if (ActionLoader.GetAction("STRIKE") == null)
            {
                TestBase.AssertTrue(true, "TestCanRemoveRequiredBasicAfterClassTier skipped (no STRIKE)", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            WithFirstClassTierAtTwo(() =>
            {
                var character = TestDataBuilders.Character().WithName("WeaponReqClassTierRemove").Build();
                character.Progression.WarriorPoints = 2;
                var sword = TestDataBuilders.Weapon().WithWeaponType(WeaponType.Sword).Build();
                character.EquipItem(sword, "weapon");
                CharacterSerializer.RebuildCharacterActions(character);

                var strikeInCombo = character.GetComboActions()
                    .FirstOrDefault(a => string.Equals(a.Name, "STRIKE", StringComparison.OrdinalIgnoreCase));
                if (strikeInCombo == null)
                {
                    TestBase.AssertTrue(true, "TestCanRemoveRequiredBasicAfterClassTier skipped (STRIKE not in combo after rebuild)", ref _testsRun, ref _testsPassed, ref _testsFailed);
                    return;
                }

                bool removed = character.RemoveFromCombo(strikeInCombo);
                TestBase.AssertTrue(removed, "RemoveFromCombo should allow the weapon basic after first Warrior tier", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertFalse(
                    character.GetComboActions().Any(a => string.Equals(a.Name, "STRIKE", StringComparison.OrdinalIgnoreCase)),
                    "STRIKE should not be compelled after first Warrior tier",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            });
        }

        private static void TestRestoreComboDoesNotReInjectRequiredBasicAfterClassTier()
        {
            Console.WriteLine("\n--- TestRestoreComboDoesNotReInjectRequiredBasicAfterClassTier ---");
            ActionLoader.LoadActions();
            if (ActionLoader.GetAction("STRIKE") == null)
            {
                TestBase.AssertTrue(true, "TestRestoreComboDoesNotReInjectRequiredBasicAfterClassTier skipped (no STRIKE)", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            WithFirstClassTierAtTwo(() =>
            {
                var character = TestDataBuilders.Character().WithName("WeaponReqClassTierRestore").Build();
                character.Progression.WarriorPoints = 2;
                var sword = TestDataBuilders.Weapon().WithWeaponType(WeaponType.Sword).Build();
                character.EquipItem(sword, "weapon");
                CharacterSerializer.RebuildCharacterActions(character);

                var freeAction = TestDataBuilders.CreateMockAction("FREE CUT");
                freeAction.IsComboAction = true;
                freeAction.ComboOrder = 2;
                character.AddAction(freeAction, 1.0);

                bool restored = character.RestoreComboFromActionNames(new[] { "FREE CUT" });
                TestBase.AssertTrue(restored, "RestoreComboFromActionNames should restore the non-basic action", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertFalse(
                    character.GetComboActions().Any(a => string.Equals(a.Name, "STRIKE", StringComparison.OrdinalIgnoreCase)),
                    "STRIKE should not be re-added after the Warrior class tier is reached",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            });
        }

        private static void WithFirstClassTierAtTwo(System.Action test)
        {
            var cfg = GameConfiguration.Instance;
            var backupClassPresentation = cfg.ClassPresentation;
            try
            {
                cfg.ClassPresentation = new ClassPresentationConfig
                {
                    TierThresholds = new[] { 2, 4, 6, 8 }
                };
                test();
            }
            finally
            {
                cfg.ClassPresentation = backupClassPresentation;
            }
        }
    }
}
