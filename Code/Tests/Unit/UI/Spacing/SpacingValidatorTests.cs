using System;
using System.Collections.Generic;
using RPGGame.Tests;
using RPGGame.UI.Spacing;

namespace RPGGame.Tests.Unit.UI.Spacing
{
    /// <summary>
    /// Comprehensive tests for SpacingValidator
    /// Tests spacing validation
    /// </summary>
    public static class SpacingValidatorTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all SpacingValidator tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== SpacingValidator Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestValidateSpacing_ValidText();
            TestValidateSpacing_DoubleSpaces();
            TestValidateSpacing_SpacesBeforePunctuation();
            TestValidateSpacing_MissingSpacesAfterPunctuation();
            TestValidateSpacing_EmptyText();
            TestValidateSpacing_NullText();

            TestBase.PrintSummary("SpacingValidator Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Validation Tests

        private static void TestValidateSpacing_ValidText()
        {
            Console.WriteLine("--- Testing ValidateSpacing - Valid Text ---");

            string validText = "This is a valid text with proper spacing.";
            var issues = SpacingValidator.ValidateSpacing(validText);
            
            TestBase.AssertNotNull(issues,
                "ValidateSpacing should return a list of issues",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            
            TestBase.AssertTrue(issues.Count == 0,
                "Valid text should have no spacing issues",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestValidateSpacing_DoubleSpaces()
        {
            Console.WriteLine("\n--- Testing ValidateSpacing - Double Spaces ---");

            string textWithDoubleSpaces = "This  has  double  spaces.";
            var issues = SpacingValidator.ValidateSpacing(textWithDoubleSpaces);
            
            TestBase.AssertTrue(issues.Count > 0,
                "Text with double spaces should have issues",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestValidateSpacing_SpacesBeforePunctuation()
        {
            Console.WriteLine("\n--- Testing ValidateSpacing - Spaces Before Punctuation ---");

            string textWithSpaceBeforePunctuation = "This has a space before !";
            var issues = SpacingValidator.ValidateSpacing(textWithSpaceBeforePunctuation);
            
            TestBase.AssertTrue(issues.Count > 0,
                "Text with spaces before punctuation should have issues",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestValidateSpacing_MissingSpacesAfterPunctuation()
        {
            Console.WriteLine("\n--- Testing ValidateSpacing - Missing Spaces After Punctuation ---");

            string textWithMissingSpace = "This is missing a space.And here is another.";
            var issues = SpacingValidator.ValidateSpacing(textWithMissingSpace);
            
            TestBase.AssertTrue(issues.Count > 0,
                "Text with missing spaces after punctuation should have issues",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestValidateSpacing_EmptyText()
        {
            Console.WriteLine("\n--- Testing ValidateSpacing - Empty Text ---");

            var issues = SpacingValidator.ValidateSpacing("");
            
            TestBase.AssertNotNull(issues,
                "ValidateSpacing should handle empty text",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            
            TestBase.AssertTrue(issues.Count == 0,
                "Empty text should have no issues",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestValidateSpacing_NullText()
        {
            Console.WriteLine("\n--- Testing ValidateSpacing - Null Text ---");

            var issues = SpacingValidator.ValidateSpacing(null!);
            
            TestBase.AssertNotNull(issues,
                "ValidateSpacing should handle null text",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            
            TestBase.AssertTrue(issues.Count == 0,
                "Null text should have no issues",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
