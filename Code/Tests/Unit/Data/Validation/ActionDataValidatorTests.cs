using System;
using RPGGame.Data.Validation;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data.Validation
{
    /// <summary>
    /// Tests for ActionDataValidator
    /// </summary>
    public static class ActionDataValidatorTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all ActionDataValidator tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionDataValidator Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestValidatorCreation();
            TestValidationExecution();
            TestRangeValidation();

            TestBase.PrintSummary("ActionDataValidator Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestValidatorCreation()
        {
            Console.WriteLine("--- Testing Validator Creation ---");

            try
            {
                var validator = new ActionDataValidator();
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

        private static void TestValidationExecution()
        {
            Console.WriteLine("\n--- Testing Validation Execution ---");

            try
            {
                var validator = new ActionDataValidator();
                var result = validator.Validate();

                TestBase.AssertNotNull(result,
                    "Validate should return a result",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"Validation execution failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestRangeValidation()
        {
            Console.WriteLine("\n--- Testing Range Validation Rules ---");

            try
            {
                // Test that validation rules are accessible
                TestBase.AssertTrue(ValidationRules.Actions.MinDamageMultiplier > 0,
                    "MinDamageMultiplier should be positive",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertTrue(ValidationRules.Actions.MaxDamageMultiplier > ValidationRules.Actions.MinDamageMultiplier,
                    "MaxDamageMultiplier should be greater than MinDamageMultiplier",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertTrue(ValidationRules.Actions.ValidTypes.Count > 0,
                    "ValidTypes should contain entries",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"Range validation test failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }
    }
}
