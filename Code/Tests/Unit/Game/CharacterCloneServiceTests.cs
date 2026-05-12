using System;
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
            TestCloneAfterDeathStripsEquippedGearAndRevives();

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

        private static void TestCloneAfterDeathStripsEquippedGearAndRevives()
        {
            Console.WriteLine("\n--- Testing Clone Gear Loss And Revival ---");

            var character = new Character("Alden", 3);
            var bagItem = new Item(ItemType.Head, "Spare Cap", 1, 0);
            character.Equipment.Inventory.Add(bagItem);
            character.Equipment.Head = new Item(ItemType.Head, "Lost Helm", 1, 0);
            character.Equipment.Body = new Item(ItemType.Chest, "Lost Mail", 1, 0);
            character.Equipment.Legs = new Item(ItemType.Legs, "Lost Greaves", 1, 0);
            character.Equipment.Weapon = new WeaponItem("Lost Sword", 1) { WeaponType = WeaponType.Sword };
            character.Equipment.Feet = new Item(ItemType.Feet, "Lost Boots", 1, 0);
            character.CurrentHealth = 0;
            character.ApplyBurn(5);

            CharacterCloneService.CloneAfterDeath(character);

            TestBase.AssertEqual("Alden Jr.", character.Name,
                "Clone should receive next generation name",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(character.IsAlive && character.CurrentHealth == character.GetEffectiveMaxHealth(),
                "Clone should revive at full effective health",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(character.Head == null && character.Body == null && character.Legs == null && character.Weapon == null && character.Feet == null,
                "Clone should lose every equipped gear slot",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(character.Inventory.Count == 1 && ReferenceEquals(bagItem, character.Inventory[0]),
                "Clone should keep bag inventory",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(character.BurnIntensity == 0 && character.PendingBurnFromHits == 0,
                "Clone should clear combat status effects",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(character.GetComboActions().Count > 0,
                "Clone should rebuild an unarmed combo",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
