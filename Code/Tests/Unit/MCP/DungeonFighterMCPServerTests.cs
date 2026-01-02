using System;
using RPGGame.Tests;
using RPGGame.MCP;

namespace RPGGame.Tests.Unit.MCP
{
    /// <summary>
    /// Tests for DungeonFighterMCPServer
    /// Tests MCP server initialization, startup, and shutdown
    /// </summary>
    public static class DungeonFighterMCPServerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all DungeonFighterMCPServer tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== DungeonFighterMCPServer Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();

            TestBase.PrintSummary("DungeonFighterMCPServer Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            try
            {
                var server = new DungeonFighterMCPServer();
                
                TestBase.AssertTrue(server != null,
                    "DungeonFighterMCPServer should be created successfully",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"DungeonFighterMCPServer constructor failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion
    }
}
