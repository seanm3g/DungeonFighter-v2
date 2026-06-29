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
        private static readonly (string Name, System.Action Execute)[] Suites =
        {
            ("DungeonFighterMCPServer", () => DungeonFighterMCPServerTests.RunAllTests()),
            ("GameWrapper", () => GameWrapperTests.RunAllTests()),
            ("AgentGameplay", () => AgentGameplayTests.RunAllTests()),
        };

        public static IReadOnlyList<FilteredTestRunner.TestSuiteEntry> GetSuiteEntries()
        {
            var entries = new List<FilteredTestRunner.TestSuiteEntry>(Suites.Length);
            foreach (var (name, execute) in Suites)
                entries.Add(new FilteredTestRunner.TestSuiteEntry("mcp", name, execute));
            return entries;
        }

        /// <summary>
        /// Runs all MCP system tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine(GameConstants.StandardSeparator);
            Console.WriteLine("  MCP SYSTEM TEST SUITE");
            Console.WriteLine($"{GameConstants.StandardSeparator}\n");

            foreach (var (_, execute) in Suites)
            {
                execute();
                Console.WriteLine();
            }
        }
    }
}
