using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Game
{
    public static class CharacterResurrectionServiceTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== CharacterResurrectionService Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestResurrectDevNoPenaltyRevivesAndPreservesGear();
            TestResurrectDevNoPenaltyPreservesNameAndProgress();

            TestBase.PrintSummary("CharacterResurrectionService Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestResurrectDevNoPenaltyRevivesAndPreservesGear()
        {
            Console.WriteLine("--- Testing Resurrect Dev Revival And Gear ---");

            var character = new Character("Hero", 3);
            character.Equipment.Weapon = new WeaponItem("Kept Sword", 1) { WeaponType = WeaponType.Sword };
            character.CurrentHealth = 0;
            character.ApplyPoison(3, 2);
            character.SessionStats.EndSession();

            CharacterResurrectionService.ResurrectDevNoPenalty(character);

            TestBase.AssertTrue(character.IsAlive,
                "Dev resurrect should restore the hero to life",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(character.GetEffectiveMaxHealth(), character.CurrentHealth,
                "Dev resurrect should heal to full effective health",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertNotNull(character.Weapon,
                "Dev resurrect should keep equipped gear",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0.0, character.PoisonPercentOfMaxHealth,
                "Dev resurrect should clear combat status effects",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(character.SessionStats.SessionEndTime == null,
                "Dev resurrect should reopen the session after death ended it",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestResurrectDevNoPenaltyPreservesNameAndProgress()
        {
            Console.WriteLine("--- Testing Resurrect Dev Preserves Name And Progress ---");

            var character = new Character("Alden", 5);
            character.Progression.XP = 42;
            character.CurrentHealth = 0;

            CharacterResurrectionService.ResurrectDevNoPenalty(character);

            TestBase.AssertEqual("Alden", character.Name,
                "Dev resurrect should not rename the hero",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(5, character.Level,
                "Dev resurrect should keep level",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(42, character.Progression.XP,
                "Dev resurrect should keep XP",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
