using System;
using RPGGame;
using RPGGame.Tests.Unit.Config;

namespace RPGGame.Tests.Runners
{
    /// <summary>
    /// Test runner for Config system tests
    /// </summary>
    public static class ConfigSystemTestRunner
    {
        /// <summary>
        /// Runs all Config system tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine(GameConstants.StandardSeparator);
            Console.WriteLine("  CONFIG SYSTEM TEST SUITE");
            Console.WriteLine($"{GameConstants.StandardSeparator}\n");

            CharacterConfigTests.RunAllTests();
            Console.WriteLine();
            CombatConfigTests.RunAllTests();
            Console.WriteLine();
            ItemConfigTests.RunAllTests();
            Console.WriteLine();
            EnemyConfigTests.RunAllTests();
            Console.WriteLine();
            UIConfigTests.RunAllTests();
            Console.WriteLine();
            SystemConfigTests.RunAllTests();
            Console.WriteLine();
            DungeonConfigTests.RunAllTests();
        }
    }
}
