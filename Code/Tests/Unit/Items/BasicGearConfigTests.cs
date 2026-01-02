using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Items
{
    /// <summary>
    /// Comprehensive tests for BasicGearConfig
    /// Tests basic gear name retrieval and validation
    /// </summary>
    public static class BasicGearConfigTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all BasicGearConfig tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== BasicGearConfig Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestGetBasicGearNames();
            TestIsBasicGear();
            TestGetBasicGearNamesByType();
            TestGetBasicGearNamesByMaterial();
            TestGetBasicGearCount();
            TestGetBasicGearNamesString();

            TestBase.PrintSummary("BasicGearConfig Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Configuration Tests

        private static void TestGetBasicGearNames()
        {
            Console.WriteLine("--- Testing GetBasicGearNames ---");

            var names = BasicGearConfig.GetBasicGearNames();
            TestBase.AssertTrue(names.Length > 0,
                "Basic gear names should be returned",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(Array.Exists(names, n => n.Contains("Leather")),
                "Should include leather gear",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestIsBasicGear()
        {
            Console.WriteLine("\n--- Testing IsBasicGear ---");

            TestBase.AssertTrue(BasicGearConfig.IsBasicGear("Leather Helmet"),
                "Leather Helmet should be basic gear",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(BasicGearConfig.IsBasicGear("CLOTH ROBES"),
                "Case should be ignored",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(BasicGearConfig.IsBasicGear("Epic Sword"),
                "Epic Sword should not be basic gear",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(BasicGearConfig.IsBasicGear(""),
                "Empty string should not be basic gear",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGetBasicGearNamesByType()
        {
            Console.WriteLine("\n--- Testing GetBasicGearNamesByType ---");

            var helmets = BasicGearConfig.GetBasicGearNamesByType("Helmet");
            TestBase.AssertTrue(helmets.Length > 0,
                "Should return helmet gear",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var empty = BasicGearConfig.GetBasicGearNamesByType("");
            TestBase.AssertEqual(0, empty.Length,
                "Empty type should return empty array",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGetBasicGearNamesByMaterial()
        {
            Console.WriteLine("\n--- Testing GetBasicGearNamesByMaterial ---");

            var leather = BasicGearConfig.GetBasicGearNamesByMaterial("Leather");
            TestBase.AssertTrue(leather.Length > 0,
                "Should return leather gear",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var cloth = BasicGearConfig.GetBasicGearNamesByMaterial("Cloth");
            TestBase.AssertTrue(cloth.Length > 0,
                "Should return cloth gear",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGetBasicGearCount()
        {
            Console.WriteLine("\n--- Testing GetBasicGearCount ---");

            int count = BasicGearConfig.GetBasicGearCount();
            TestBase.AssertTrue(count > 0,
                "Basic gear count should be positive",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGetBasicGearNamesString()
        {
            Console.WriteLine("\n--- Testing GetBasicGearNamesString ---");

            var str = BasicGearConfig.GetBasicGearNamesString();
            TestBase.AssertTrue(!string.IsNullOrEmpty(str),
                "Basic gear names string should not be empty",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var str2 = BasicGearConfig.GetBasicGearNamesString(" | ");
            TestBase.AssertTrue(str2.Contains(" | "),
                "Custom separator should be used",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
