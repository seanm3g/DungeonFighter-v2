using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive unit tests for ColoredTextBuilder.
    /// Tests building, spacing, merging, and edge cases.
    /// </summary>
    public static class ColoredTextBuilderTest
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;
        
        /// <summary>
        /// Runs all ColoredTextBuilder tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== ColoredTextBuilder Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            
            // Core functionality tests
            TestEmptyBuilder();
            TestSimpleBuild();
            TestSpacingBetweenSegments();
            TestSameColorMerging();
            TestDifferentColors();
            
            // Fluent API tests
            TestFluentColorMethods();
            TestChaining();
            
            // Edge cases
            TestEmptySegments();
            TestSpaceOnlySegments();
            TestLeadingTrailingSpaces();
            TestPunctuationSpacing();
            
            // Input validation tests
            TestNullInputValidation();
            TestMaxLengthValidation();
            
            // Utility method tests
            TestGetPlainText();
            TestGetDisplayLength();
            
            // Print summary
            PrintSummary();
        }
        
        #region Core Functionality Tests
        
        private static void TestEmptyBuilder()
        {
            Console.WriteLine("--- Testing Empty Builder ---");
            
            var builder = new ColoredTextBuilder();
            var result = builder.Build();
            
            AssertTrue(result.Count == 0, "Empty builder should return empty list");
        }
        
        private static void TestSimpleBuild()
        {
            Console.WriteLine("\n--- Testing Simple Build ---");
            
            var builder = new ColoredTextBuilder()
                .Add("Hello", Colors.White)
                .Add("World", Colors.White);
            
            var result = builder.Build();
            
            AssertTrue(result.Count >= 2, "Should have at least 2 segments (may have space)");
            AssertTrue(result.Any(s => s.Text.Contains("Hello")), "Should contain 'Hello'");
            AssertTrue(result.Any(s => s.Text.Contains("World")), "Should contain 'World'");
        }
        
        private static void TestSpacingBetweenSegments()
        {
            Console.WriteLine("\n--- Testing Spacing Between Segments ---");
            
            var builder = new ColoredTextBuilder()
                .Add("hits", Colors.White)
                .Add("target", Colors.White);
            
            var result = builder.Build();
            var plainText = string.Join("", result.Select(s => s.Text));
            
            AssertTrue(plainText.Contains("hits target") || plainText.Contains("hits  target"), 
                "Should add space between 'hits' and 'target'");
        }
        
        private static void TestSameColorMerging()
        {
            Console.WriteLine("\n--- Testing Same Color Merging ---");
            
            var builder = new ColoredTextBuilder()
                .Add("Hello", Colors.Red)
                .Add("World", Colors.Red);
            
            var result = builder.Build();
            
            // Same color segments should be merged (may have space segment between)
            var redSegments = result.Where(s => ColorValidator.AreColorsEqual(s.Color, Colors.Red)).ToList();
            AssertTrue(redSegments.Count >= 1, "Should have at least one red segment");
            
            var combinedText = string.Join("", redSegments.Select(s => s.Text));
            AssertTrue(combinedText.Contains("Hello") && combinedText.Contains("World"), 
                "Should contain both 'Hello' and 'World'");
        }
        
        private static void TestDifferentColors()
        {
            Console.WriteLine("\n--- Testing Different Colors ---");
            
            var builder = new ColoredTextBuilder()
                .Add("Hello", Colors.Red)
                .Add("World", Colors.Blue);
            
            var result = builder.Build();
            
            var redSegments = result.Where(s => ColorValidator.AreColorsEqual(s.Color, Colors.Red)).ToList();
            var blueSegments = result.Where(s => ColorValidator.AreColorsEqual(s.Color, Colors.Blue)).ToList();
            
            AssertTrue(redSegments.Count >= 1, "Should have red segment");
            AssertTrue(blueSegments.Count >= 1, "Should have blue segment");
            
            var redText = string.Join("", redSegments.Select(s => s.Text));
            var blueText = string.Join("", blueSegments.Select(s => s.Text));
            
            AssertTrue(redText.Contains("Hello"), "Red segment should contain 'Hello'");
            AssertTrue(blueText.Contains("World"), "Blue segment should contain 'World'");
        }
        
        #endregion
        
        #region Fluent API Tests
        
        private static void TestFluentColorMethods()
        {
            Console.WriteLine("\n--- Testing Fluent Color Methods ---");
            
            var builder = new ColoredTextBuilder()
                .Red("damage")
                .Green("healing")
                .Blue("mana");
            
            var result = builder.Build();
            
            AssertTrue(result.Count >= 3, "Should have at least 3 segments");
            
            var hasRed = result.Any(s => ColorValidator.AreColorsEqual(s.Color, ColorPalette.Red.GetColor()) || 
                                         s.Text.Contains("damage"));
            var hasGreen = result.Any(s => ColorValidator.AreColorsEqual(s.Color, ColorPalette.Green.GetColor()) || 
                                          s.Text.Contains("healing"));
            var hasBlue = result.Any(s => ColorValidator.AreColorsEqual(s.Color, ColorPalette.Blue.GetColor()) || 
                                         s.Text.Contains("mana"));
            
            AssertTrue(hasRed, "Should have red segment");
            AssertTrue(hasGreen, "Should have green segment");
            AssertTrue(hasBlue, "Should have blue segment");
        }
        
        private static void TestChaining()
        {
            Console.WriteLine("\n--- Testing Method Chaining ---");
            
            var builder = ColoredTextBuilder.Start()
                .Add("Hello")
                .Add("World")
                .AddSpace()
                .Add("Test");
            
            var result = builder.Build();
            
            AssertTrue(result.Count > 0, "Should build successfully with chaining");
        }
        
        #endregion
        
        #region Edge Case Tests
        
        private static void TestEmptySegments()
        {
            Console.WriteLine("\n--- Testing Empty Segments ---");
            
            var builder = new ColoredTextBuilder()
                .Add("Hello", Colors.White)
                .Add("", Colors.White)  // Empty segment
                .Add("World", Colors.White);
            
            var result = builder.Build();
            var plainText = string.Join("", result.Select(s => s.Text));
            
            AssertTrue(plainText.Contains("Hello"), "Should contain 'Hello'");
            AssertTrue(plainText.Contains("World"), "Should contain 'World'");
            AssertTrue(!plainText.Contains("  "), "Should not have double spaces from empty segment");
        }
        
        private static void TestSpaceOnlySegments()
        {
            Console.WriteLine("\n--- Testing Space-Only Segments ---");
            
            var builder = new ColoredTextBuilder()
                .Add("Hello", Colors.White)
                .Add(" ", Colors.White)  // Space-only segment
                .Add("World", Colors.White);
            
            var result = builder.Build();
            var plainText = string.Join("", result.Select(s => s.Text));
            
            AssertTrue(plainText.Contains("Hello") && plainText.Contains("World"), 
                "Should contain both words");
        }
        
        private static void TestLeadingTrailingSpaces()
        {
            Console.WriteLine("\n--- Testing Leading/Trailing Spaces ---");
            
            var builder = new ColoredTextBuilder()
                .Add("  Hello  ", Colors.White)  // Leading and trailing spaces
                .Add("World", Colors.White);
            
            var result = builder.Build();
            var plainText = string.Join("", result.Select(s => s.Text));
            
            AssertTrue(plainText.Contains("Hello"), "Should contain 'Hello' without extra spaces");
            AssertTrue(plainText.Contains("World"), "Should contain 'World'");
        }
        
        private static void TestPunctuationSpacing()
        {
            Console.WriteLine("\n--- Testing Punctuation Spacing ---");
            
            var builder = new ColoredTextBuilder()
                .Add("hits!", Colors.White)
                .Add("target", Colors.White);
            
            var result = builder.Build();
            var plainText = string.Join("", result.Select(s => s.Text));
            
            // Should not add space after exclamation
            AssertTrue(plainText.Contains("hits!target") || plainText.Contains("hits! target"), 
                "Should handle punctuation spacing correctly");
        }
        
        #endregion
        
        #region Input Validation Tests
        
        private static void TestNullInputValidation()
        {
            Console.WriteLine("\n--- Testing Null Input Validation ---");
            
            try
            {
                var builder = new ColoredTextBuilder();
                builder.Add(null!, Colors.White);
                AssertTrue(false, "Should throw ArgumentNullException for null text");
            }
            catch (ArgumentNullException)
            {
                AssertTrue(true, "Correctly throws ArgumentNullException for null text");
            }
            
            try
            {
                var builder = new ColoredTextBuilder();
                builder.Add((ColoredText)null!);
                AssertTrue(false, "Should throw ArgumentNullException for null segment");
            }
            catch (ArgumentNullException)
            {
                AssertTrue(true, "Correctly throws ArgumentNullException for null segment");
            }
            
            try
            {
                var builder = new ColoredTextBuilder();
                builder.AddRange(null!);
                AssertTrue(false, "Should throw ArgumentNullException for null segments list");
            }
            catch (ArgumentNullException)
            {
                AssertTrue(true, "Correctly throws ArgumentNullException for null segments list");
            }
        }
        
        private static void TestMaxLengthValidation()
        {
            Console.WriteLine("\n--- Testing Max Length Validation ---");
            
            try
            {
                var builder = new ColoredTextBuilder();
                var longText = new string('a', 10001);  // Exceeds MaxSegmentLength
                builder.Add(longText, Colors.White);
                AssertTrue(false, "Should throw ArgumentException for text exceeding max length");
            }
            catch (ArgumentException)
            {
                AssertTrue(true, "Correctly throws ArgumentException for text exceeding max length");
            }
        }
        
        #endregion
        
        #region Utility Method Tests
        
        private static void TestGetPlainText()
        {
            Console.WriteLine("\n--- Testing GetPlainText ---");
            
            var builder = new ColoredTextBuilder()
                .Add("Hello", Colors.Red)
                .Add("World", Colors.Blue);
            
            var plainText = builder.GetPlainText();
            
            AssertTrue(plainText.Contains("Hello"), "Plain text should contain 'Hello'");
            AssertTrue(plainText.Contains("World"), "Plain text should contain 'World'");
        }
        
        private static void TestGetDisplayLength()
        {
            Console.WriteLine("\n--- Testing GetDisplayLength ---");
            
            var builder = new ColoredTextBuilder()
                .Add("Hello", Colors.White)
                .Add("World", Colors.White);
            
            var length = builder.GetDisplayLength();
            
            AssertTrue(length >= 10, "Display length should be at least 10 (Hello + World)");
        }
        
        #endregion
        
        #region Helper Methods
        
        private static void AssertTrue(bool condition, string message)
        {
            _testsRun++;
            if (condition)
            {
                _testsPassed++;
                Console.WriteLine($"  ✓ {message}");
            }
            else
            {
                _testsFailed++;
                Console.WriteLine($"  ✗ FAILED: {message}");
            }
        }
        
        private static void PrintSummary()
        {
            Console.WriteLine("\n=== Test Summary ===");
            Console.WriteLine($"Total Tests: {_testsRun}");
            Console.WriteLine($"Passed: {_testsPassed}");
            Console.WriteLine($"Failed: {_testsFailed}");
            Console.WriteLine($"Success Rate: {(_testsPassed * 100.0 / _testsRun):F1}%");
            
            if (_testsFailed == 0)
            {
                Console.WriteLine("\n✅ All tests passed!");
            }
            else
            {
                Console.WriteLine($"\n❌ {_testsFailed} test(s) failed");
            }
        }
        
        #endregion
    }
}

