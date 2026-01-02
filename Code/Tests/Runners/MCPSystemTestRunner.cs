using System;
using RPGGame;
using RPGGame.Tests.Unit.MCP;

namespace RPGGame.Tests.Runners
{
    /// <summary>
    /// Test runner for MCP system tests
    /// </summary>
    public static class MCPSystemTestRunner
    {
        /// <summary>
        /// Runs all MCP system tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine(GameConstants.StandardSeparator);
            Console.WriteLine("  MCP SYSTEM TEST SUITE");
            Console.WriteLine($"{GameConstants.StandardSeparator}\n");

            DungeonFighterMCPServerTests.RunAllTests();
            Console.WriteLine();
            GameWrapperTests.RunAllTests();
        }
    }
}
