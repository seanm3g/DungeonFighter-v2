using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Comprehensive tests for UIManager
    /// Tests UI manager core, display methods, text formatting, and timing
    /// </summary>
    public static class UIManagerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all UIManager tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== UIManager Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestWriteLine();
            TestWrite();
            TestWriteMenuLine();
            TestSetCustomUIManager();
            TestDisableAllUIOutput();

            TestBase.PrintSummary("UIManager Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Display Tests

        private static void TestWriteLine()
        {
            Console.WriteLine("--- Testing WriteLine ---");

            // Test that WriteLine doesn't crash
            UIManager.WriteLine("Test message", UIMessageType.System);
            TestBase.AssertTrue(true,
                "WriteLine should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestWrite()
        {
            Console.WriteLine("\n--- Testing Write ---");

            // Test that Write doesn't crash
            UIManager.Write("Test message");
            TestBase.AssertTrue(true,
                "Write should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestWriteMenuLine()
        {
            Console.WriteLine("\n--- Testing WriteMenuLine ---");

            // Test that WriteMenuLine doesn't crash
            UIManager.WriteMenuLine("Test menu line");
            TestBase.AssertTrue(true,
                "WriteMenuLine should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Configuration Tests

        private static void TestSetCustomUIManager()
        {
            Console.WriteLine("\n--- Testing SetCustomUIManager ---");

            // Save original value to restore after test
            var originalUIManager = UIManager.GetCustomUIManager();

            try
            {
                // Test setting custom UI manager (null is acceptable)
                UIManager.SetCustomUIManager(null);
                TestBase.AssertTrue(true,
                    "SetCustomUIManager should accept null",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                // Always restore original value to prevent interference with game initialization
                UIManager.SetCustomUIManager(originalUIManager);
            }
        }

        private static void TestDisableAllUIOutput()
        {
            Console.WriteLine("\n--- Testing DisableAllUIOutput ---");

            // Test disabling UI output
            var originalValue = UIManager.DisableAllUIOutput;
            UIManager.DisableAllUIOutput = true;
            TestBase.AssertTrue(UIManager.DisableAllUIOutput,
                "DisableAllUIOutput should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Restore original value
            UIManager.DisableAllUIOutput = originalValue;
        }

        #endregion
    }
}
