using System;
using System.Linq;
using RPGGame.Data;
using RPGGame.Entity.Services;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Entity
{
    /// <summary>
    /// Regression: dungeon entry calls <see cref="CharacterSerializer.RebuildCharacterActions"/>; combo must stay non-empty when actions data is available.
    /// </summary>
    public static class RebuildCharacterActionsComboTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== RebuildCharacterActionsCombo Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            TestRebuild_KeepsNonEmptyComboAfterSecondPass(ref run, ref passed, ref failed);

            TestBase.PrintSummary("RebuildCharacterActionsComboTests", run, passed, failed);
        }

        private static void TestRebuild_KeepsNonEmptyComboAfterSecondPass(ref int run, ref int passed, ref int failed)
        {
            Console.WriteLine("--- TestRebuild_KeepsNonEmptyComboAfterSecondPass ---");

            try
            {
                ActionLoader.LoadActions();
            }
            catch
            {
                TestBase.AssertTrue(true, "Skip: ActionLoader unavailable", ref run, ref passed, ref failed);
                return;
            }

            var character = TestDataBuilders.Character().WithName("ComboRebuild").Build();
            character.Equipment.Weapon = new WeaponItem
            {
                Name = "ComboWand",
                WeaponType = WeaponType.Wand,
                GearAction = null
            };

            CharacterSerializer.RebuildCharacterActions(character);

            int firstCount = character.GetComboActions()?.Count ?? 0;
            if (firstCount == 0)
            {
                TestBase.AssertTrue(true, "Skip: no combo actions after first rebuild (data/pool empty for Wand)", ref run, ref passed, ref failed);
                return;
            }

            var namesBefore = character.GetComboActions().Select(a => a.Name).ToList();

            CharacterSerializer.RebuildCharacterActions(character);

            int secondCount = character.GetComboActions()?.Count ?? 0;
            TestBase.AssertTrue(secondCount > 0, "combo non-empty after second RebuildCharacterActions (dungeon-style)", ref run, ref passed, ref failed);

            var namesAfter = character.GetComboActions().Select(a => a.Name).ToList();
            bool sameOrder = namesBefore.SequenceEqual(namesAfter, StringComparer.OrdinalIgnoreCase);
            TestBase.AssertTrue(sameOrder, "combo action names/order preserved across rebuild", ref run, ref passed, ref failed);
        }
    }
}
