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
            TravelSystemTests.RunAllTests();
            Console.WriteLine();
            EnvironmentTests.RunAllTests();
            Console.WriteLine();
            RoomGeneratorTests.RunAllTests();
            Console.WriteLine();
            int run = 0, pass = 0, fail = 0;
            TagDefinitionsTests.RunAll(ref run, ref pass, ref fail);
            Console.WriteLine($"TagDefinitions: {pass}/{run} passed\n");
        }
    }
}
