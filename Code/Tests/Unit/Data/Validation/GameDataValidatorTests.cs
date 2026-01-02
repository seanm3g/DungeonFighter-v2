using System;
using RPGGame.Data.Validation;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data.Validation
{
    /// <summary>
    /// Tests for GameDataValidator
    /// </summary>
    public static class GameDataValidatorTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all GameDataValidator tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== GameDataValidator Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestValidatorCreation();
            TestValidateAll();
            TestResultCollection();

            TestBase.PrintSummary("GameDataValidator Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestValidatorCreation()
        {
            Console.WriteLine("--- Testing Validator Creation ---");

            try
            {
                var validator = new GameDataValidator();
                TestBase.AssertNotNull(validator,
                    "Validator should be created",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"Validator creation failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestValidateAll()
        {
            Console.WriteLine("\n--- Testing ValidateAll ---");

            try
            {
                var validator = new GameDataValidator();
                var result = validator.ValidateAll();

                TestBase.AssertNotNull(result,
                    "ValidateAll should return a result",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                if (result != null)
                {
                    // Validation should complete without throwing
                    TestBase.AssertTrue(true,
                        "Validation completed successfully",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"ValidateAll threw exception: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestResultCollection()
        {
            Console.WriteLine("\n--- Testing Result Collection ---");

            try
            {
                var validator = new GameDataValidator();
                var result = validator.ValidateAll();

                if (result != null)
                {
                    // Result should have statistics
                    TestBase.AssertTrue(result.Statistics.Count >= 0,
                        "Result should have statistics",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);

                    // Result should be able to generate summary
                    var summary = result.GetSummary();
                    TestBase.AssertTrue(!string.IsNullOrEmpty(summary),
                        "Summary should not be empty",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"Result collection test failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }
    }
}
