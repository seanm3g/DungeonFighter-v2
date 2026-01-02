using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Actions
{
    /// <summary>
    /// Comprehensive tests for ActionFactory
    /// Tests action creation and validation
    /// </summary>
    public static class ActionFactoryTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all ActionFactory tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionFactory Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            // All basic attack tests have been removed - the game no longer uses basic attacks

            TestBase.PrintSummary("ActionFactory Tests", _testsRun, _testsPassed, _testsFailed);
        }

        // All basic attack tests have been removed - the game no longer uses basic attacks
    }
}
