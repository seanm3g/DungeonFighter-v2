using System;
using RPGGame;
using RPGGame.Tests.Unit.Entity;

namespace RPGGame.Tests.Runners
{
    /// <summary>
    /// Test runner for Entity system tests
    /// </summary>
    public static class EntitySystemTestRunner
    {
        /// <summary>
        /// Runs all Entity system tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine(GameConstants.StandardSeparator);
            Console.WriteLine("  ENTITY SYSTEM TEST SUITE");
            Console.WriteLine($"{GameConstants.StandardSeparator}\n");

            CharacterTests.RunAllTests();
            Console.WriteLine();
            EnemyTests.RunAllTests();
            Console.WriteLine();
            EquipmentManagerTests.RunAllTests();
            Console.WriteLine();
            LevelUpManagerTests.RunAllTests();
            Console.WriteLine();
            CharacterHealthManagerTests.RunAllTests();
            Console.WriteLine();
            CharacterCombatCalculatorTests.RunAllTests();
            Console.WriteLine();
            CharacterBuilderTests.RunAllTests();
        }
    }
}
