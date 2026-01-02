using System;
using RPGGame;
using RPGGame.Tests.Unit.World;

namespace RPGGame.Tests.Runners
{
    /// <summary>
    /// Test runner for World system tests
    /// </summary>
    public static class WorldSystemTestRunner
    {
        /// <summary>
        /// Runs all World system tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine(GameConstants.StandardSeparator);
            Console.WriteLine("  WORLD SYSTEM TEST SUITE");
            Console.WriteLine($"{GameConstants.StandardSeparator}\n");

            DungeonTests.RunAllTests();
            Console.WriteLine();
            DungeonManagerTests.RunAllTests();
            Console.WriteLine();
            EnvironmentTests.RunAllTests();
            Console.WriteLine();
            RoomGeneratorTests.RunAllTests();
        }
    }
}
