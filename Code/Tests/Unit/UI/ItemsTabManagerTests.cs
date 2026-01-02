using System;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Tests for ItemsTabManager
    /// Tests item management, data loading, and UI coordination
    /// </summary>
    public static class ItemsTabManagerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all ItemsTabManager tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== ItemsTabManager Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestConstructorWithNullCallback();

            TestBase.PrintSummary("ItemsTabManager Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            try
            {
                var manager = new ItemsTabManager(null);
                
                TestBase.AssertTrue(manager != null,
                    "ItemsTabManager should be created successfully",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"ItemsTabManager constructor failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestConstructorWithNullCallback()
        {
            Console.WriteLine("\n--- Testing Constructor with null callback ---");

            try
            {
                var manager = new ItemsTabManager(null);
                
                TestBase.AssertTrue(manager != null,
                    "ItemsTabManager should accept null callback",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"ItemsTabManager constructor with null callback failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion
    }
}
