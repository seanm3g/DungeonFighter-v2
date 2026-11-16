using System.Linq;
using System;
using RPGGame.Entity.Services;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Game
{
    public static class CharacterCloneServiceTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== CharacterCloneService Tests ===\n");

            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestCloneNameProgression();
            TestCloneAfterDeathKeepsGearAndRevives();
            TestCloneAfterDeathPreservesLevelAndXp();

            TestBase.PrintSummary("CharacterCloneService Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestCloneNameProgression()
        {
            Console.WriteLine("--- Testing Clone Name Progression ---");

            TestBase.AssertEqual("Alden Jr.", CharacterCloneService.GetNextCloneName("Alden"),
                "First clone should append Jr.",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Alden III", CharacterCloneService.GetNextCloneName("Alden Jr."),
                "Jr. clone should advance to III",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Alden IV", CharacterCloneService.GetNextCloneName("Alden III"),
                "III clone should advance to IV",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Alden V", CharacterCloneService.GetNextCloneName("Alden IV"),
                "IV clone should advance to V",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Alden VI", CharacterCloneService.GetNextCloneName("Alden V"),
                "V clone should advance to VI",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCloneAfterDeathKeepsGearAndRevives()
        {
            Console.WriteLine("\n--- Testing Clone Keeps Gear And Revives ---");

            var character = new Character("Alden", 3);
            var bagItem = new Item(ItemType.Head, "Spare Cap", 1, 0);
            character.Equipment.Inventory.Add(bagItem);
            var helm = new Item(ItemType.Head, "Kept Helm", 1, 0);
            var sword = new WeaponItem("Kept Sword", 1) { WeaponType = WeaponType.Sword };
            character.Equipment.Head = helm;
            character.Equipment.Weapon = sword;
            CharacterSerializer.RebuildCharacterActions(character);
            var comboBefore = character.GetComboActions().Select(a => a.Name).ToList();

            character.CurrentHealth = 0;
            character.ApplyBurn(5);

            CharacterCloneService.CloneAfterDeath(character);

            TestBase.AssertEqual("Alden Jr.", character.Name,
                "Clone should receive next generation name",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(character.IsAlive && character.CurrentHealth == character.GetEffectiveMaxHealth(),
                "Clone should revive at full effective health",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(ReferenceEquals(helm, character.Head) && ReferenceEquals(sword, character.Weapon),
                "Clone should keep equipped gear",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(character.Inventory.Count == 1 && ReferenceEquals(bagItem, character.Inventory[0]),
                "Clone should keep bag inventory",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(character.BurnIntensity == 0 && character.PendingBurnFromHits == 0,
                "Clone should clear combat status effects",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(character.GetComboActions().Select(a => a.Name).SequenceEqual(comboBefore),
                "Clone should keep the combo strip",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCloneAfterDeathPreservesLevelAndXp()
        {
            Console.WriteLine("\n--- Testing Clone Preserves Level And XP ---");

            var character = new Character("Brin", 4);
            character.Progression.XP = 321;
            character.Equipment.Weapon = new WeaponItem("Rusty Sword", 1) { WeaponType = WeaponType.Sword };
            CharacterSerializer.RebuildCharacterActions(character);

            CharacterCloneService.CloneAfterDeath(character);

            TestBase.AssertTrue(character.Level == 4 && character.Progression.XP == 321,
                "Clone must keep the same level and XP toward the next level",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
