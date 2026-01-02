using System;
using RPGGame;
using RPGGame.Tests.Unit.Actions;

namespace RPGGame.Tests.Runners
{
    /// <summary>
    /// Test runner for Actions system tests
    /// </summary>
    public static class ActionsSystemTestRunner
    {
        /// <summary>
        /// Runs all Actions system tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine(GameConstants.StandardSeparator);
            Console.WriteLine("  ACTIONS SYSTEM TEST SUITE");
            Console.WriteLine($"{GameConstants.StandardSeparator}\n");

            ActionFactoryTests.RunAllTests();
            Console.WriteLine();
            ActionSelectorTests.RunAllTests();
            Console.WriteLine();
            ActionSelectorRollBasedTests.RunAllTests();
            Console.WriteLine();
            ActionSpeedSystemTests.RunAllTests();
            Console.WriteLine();
            ActionUtilitiesTests.RunAllTests();
            Console.WriteLine();
            RollModificationManagerTests.RunAllTests();
            Console.WriteLine();
            ActionTests.RunAllTests();
        }
    }
}
