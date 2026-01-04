using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.Spacing;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.UI.Spacing
{
    /// <summary>
    /// Comprehensive tests for SpacingFormatter
    /// Tests spacing formatting logic
    /// </summary>
    public static class SpacingFormatterTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all SpacingFormatter tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== SpacingFormatter Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestFormatRollInfo();
            TestFormatRollInfo_AllParameters();
            TestAddSpacingBetweenSegments();
            TestAddSpacingBetweenSegments_EmptyList();

            TestBase.PrintSummary("SpacingFormatter Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Format Tests

        private static void TestFormatRollInfo()
        {
            Console.WriteLine("--- Testing FormatRollInfo ---");

            string result = SpacingFormatter.FormatRollInfo("15", "10 vs 8");
            
            TestBase.AssertNotNull(result,
                "FormatRollInfo should return a formatted string",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            
            TestBase.AssertTrue(result.Contains("roll:"),
                "Formatted roll info should contain roll information",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestFormatRollInfo_AllParameters()
        {
            Console.WriteLine("\n--- Testing FormatRollInfo - All Parameters ---");

            string result = SpacingFormatter.FormatRollInfo("15", "10 vs 8", "1.2", "1.5");
            
            TestBase.AssertNotNull(result,
                "FormatRollInfo should return a formatted string with all parameters",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            
            TestBase.AssertTrue(result.Contains("speed:") && result.Contains("amp:"),
                "Formatted roll info should contain speed and amplifier information",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Segment Spacing Tests

        private static void TestAddSpacingBetweenSegments()
        {
            Console.WriteLine("\n--- Testing AddSpacingBetweenSegments ---");

            var segments = new List<ColoredText>
            {
                new ColoredText("Hello", Colors.White),
                new ColoredText("World", Colors.White)
            };
            
            var result = SpacingFormatter.AddSpacingBetweenSegments(segments);
            
            TestBase.AssertNotNull(result,
                "AddSpacingBetweenSegments should return a list",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            
            TestBase.AssertTrue(result.Count >= segments.Count,
                "Result should contain at least as many segments as input",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestAddSpacingBetweenSegments_EmptyList()
        {
            Console.WriteLine("\n--- Testing AddSpacingBetweenSegments - Empty List ---");

            var segments = new List<ColoredText>();
            
            var result = SpacingFormatter.AddSpacingBetweenSegments(segments);
            
            TestBase.AssertNotNull(result,
                "AddSpacingBetweenSegments should handle empty list",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            
            TestBase.AssertTrue(result.Count == 0,
                "Result should be empty for empty input",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
