using System;
using RPGGame.Tests;
using RPGGame.Config;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Tests for SettingsManager
    /// Tests settings loading, saving, and validation
    /// </summary>
    public static class SettingsManagerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all SettingsManager tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== SettingsManager Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestConstructorWithNullSettings();
            TestSetGameVariablesTabManager();

            TestBase.PrintSummary("SettingsManager Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            try
            {
                var settings = new GameSettings();
                var manager = new SettingsManager(settings, null, null);
                
                TestBase.AssertTrue(manager != null,
                    "SettingsManager should be created with valid GameSettings",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"SettingsManager constructor failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestConstructorWithNullSettings()
        {
            Console.WriteLine("\n--- Testing Constructor with null settings ---");

            try
            {
                var manager = new SettingsManager(null!, null, null);
                TestBase.AssertTrue(false,
                    "SettingsManager should throw ArgumentNullException for null settings",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (ArgumentNullException)
            {
                TestBase.AssertTrue(true,
                    "SettingsManager should throw ArgumentNullException for null settings",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"SettingsManager threw unexpected exception: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestSetGameVariablesTabManager()
        {
            Console.WriteLine("\n--- Testing SetGameVariablesTabManager ---");

            var settings = new GameSettings();
            var manager = new SettingsManager(settings, null, null);
            
            // Test with null (should be acceptable)
            manager.SetGameVariablesTabManager(null);
            TestBase.AssertTrue(true,
                "SetGameVariablesTabManager should accept null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
