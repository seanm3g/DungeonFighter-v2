using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Config
{
    /// <summary>
    /// Comprehensive tests for UIConfig
    /// Tests UI settings, display configs, and timing settings
    /// </summary>
    public static class UIConfigTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all UIConfig tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== UIConfig Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestUICustomizationConfig();
            TestRarityPrefixesConfig();
            TestActionNamesConfig();
            TestErrorMessagesConfig();
            TestDebugMessagesConfig();

            TestBase.PrintSummary("UIConfig Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region UICustomizationConfig Tests

        private static void TestUICustomizationConfig()
        {
            Console.WriteLine("--- Testing UICustomizationConfig ---");

            var config = new UICustomizationConfig
            {
                MenuSeparator = "---",
                SubMenuSeparator = "--",
                InvalidChoiceMessage = "Invalid choice",
                PressAnyKeyMessage = "Press any key to continue"
            };

            TestBase.AssertNotNull(config.RarityPrefixes,
                "RarityPrefixes should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(config.ActionNames,
                "ActionNames should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(config.ErrorMessages,
                "ErrorMessages should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual("---", config.MenuSeparator,
                "MenuSeparator should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region RarityPrefixesConfig Tests

        private static void TestRarityPrefixesConfig()
        {
            Console.WriteLine("\n--- Testing RarityPrefixesConfig ---");

            var config = new RarityPrefixesConfig
            {
                Common = "[Common]",
                Uncommon = "[Uncommon]",
                Rare = "[Rare]",
                Epic = "[Epic]",
                Legendary = "[Legendary]"
            };

            TestBase.AssertEqual("[Common]", config.Common,
                "Common prefix should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual("[Rare]", config.Rare,
                "Rare prefix should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region ActionNamesConfig Tests

        private static void TestActionNamesConfig()
        {
            Console.WriteLine("\n--- Testing ActionNamesConfig ---");

            var config = new ActionNamesConfig
            {
                BasicAttackName = "Basic Attack",
                DefaultActionDescription = "A basic attack action"
            };

            TestBase.AssertEqual("Basic Attack", config.BasicAttackName,
                "BasicAttackName should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual("A basic attack action", config.DefaultActionDescription,
                "DefaultActionDescription should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region ErrorMessagesConfig Tests

        private static void TestErrorMessagesConfig()
        {
            Console.WriteLine("\n--- Testing ErrorMessagesConfig ---");

            var config = new ErrorMessagesConfig
            {
                FileNotFoundError = "File not found",
                JsonDeserializationError = "JSON deserialization failed",
                InvalidDataError = "Invalid data",
                SaveError = "Save failed",
                LoadError = "Load failed"
            };

            TestBase.AssertEqual("File not found", config.FileNotFoundError,
                "FileNotFoundError should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual("Save failed", config.SaveError,
                "SaveError should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region DebugMessagesConfig Tests

        private static void TestDebugMessagesConfig()
        {
            Console.WriteLine("\n--- Testing DebugMessagesConfig ---");

            var config = new DebugMessagesConfig
            {
                DebugPrefix = "[DEBUG]",
                WarningPrefix = "[WARNING]",
                ErrorPrefix = "[ERROR]",
                InfoPrefix = "[INFO]"
            };

            TestBase.AssertEqual("[DEBUG]", config.DebugPrefix,
                "DebugPrefix should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual("[WARNING]", config.WarningPrefix,
                "WarningPrefix should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
