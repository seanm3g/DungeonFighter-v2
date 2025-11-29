using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive unit tests for ColoredTextMerger.
    /// Tests merging logic, space normalization, and edge cases.
    /// </summary>
    public static class ColoredTextMergerTest
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;
        
        /// <summary>
        /// Runs all ColoredTextMerger tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== ColoredTextMerger Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            
            // Core merging tests
            TestEmptyList();
            TestSingleSegment();
            TestSameColorMerging();
            TestDifferentColors();
            TestSpaceSegments();
            
            // Space normalization tests
            TestSpaceNormalization();
            TestMultipleSpaces();
            TestBoundarySpaces();
            
            // Edge cases
            TestEmptySegments();
            TestNullInput();
            TestSingleCharacterSegments();
            
            // Print summary
            PrintSummary();
        }
        
        #region Core Merging Tests
        
        private static void TestEmptyList()
        {
            Console.WriteLine("--- Testing Empty List ---");
            
            var result = ColoredTextMerger.MergeAdjacentSegments(new List<ColoredText>());
            AssertTrue(result.Count == 0, "Empty list should return empty list");
            
            var result2 = ColoredTextMerger.MergeSameColorSegments(new List<ColoredText>());
            AssertTrue(result2.Count == 0, "Empty list should return empty list (same color)");
        }
        
        private static void TestSingleSegment()
        {
            Console.WriteLine("\n--- Testing Single Segment ---");
            
            var segments = new List<ColoredText> { new ColoredText("Hello", Colors.White) };
            var result = ColoredTextMerger.MergeAdjacentSegments(segments);
            
            AssertTrue(result.Count == 1, "Single segment should return single segment");
            AssertTrue(result[0].Text == "Hello", "Should preserve text");
            AssertTrue(ColorValidator.AreColorsEqual(result[0].Color, Colors.White), "Should preserve color");
        }
        
        private static void TestSameColorMerging()
        {
            Console.WriteLine("\n--- Testing Same Color Merging ---");
            
            var segments = new List<ColoredText>
            {
                new ColoredText("Hello", Colors.Red),
                new ColoredText("World", Colors.Red)
            };
            
            var result = ColoredTextMerger.MergeSameColorSegments(segments);
            
            AssertTrue(result.Count == 1, "Same color segments should be merged");
            AssertTrue(result[0].Text.Contains("Hello") && result[0].Text.Contains("World"), 
                "Merged segment should contain both texts");
            AssertTrue(ColorValidator.AreColorsEqual(result[0].Color, Colors.Red), "Should preserve color");
        }
        
        private static void TestDifferentColors()
        {
            Console.WriteLine("\n--- Testing Different Colors ---");
            
            var segments = new List<ColoredText>
            {
                new ColoredText("Hello", Colors.Red),
                new ColoredText("World", Colors.Blue)
            };
            
            var result = ColoredTextMerger.MergeSameColorSegments(segments);
            
            AssertTrue(result.Count == 2, "Different color segments should not be merged");
            
            var redSegment = result.FirstOrDefault(s => ColorValidator.AreColorsEqual(s.Color, Colors.Red));
            var blueSegment = result.FirstOrDefault(s => ColorValidator.AreColorsEqual(s.Color, Colors.Blue));
            
            AssertTrue(redSegment != null && redSegment.Text.Contains("Hello"), "Should preserve red segment");
            AssertTrue(blueSegment != null && blueSegment.Text.Contains("World"), "Should preserve blue segment");
        }
        
        private static void TestSpaceSegments()
        {
            Console.WriteLine("\n--- Testing Space Segments ---");
            
            var segments = new List<ColoredText>
            {
                new ColoredText("Hello", Colors.White),
                new ColoredText(" ", Colors.White),  // Space segment
                new ColoredText("World", Colors.White)
            };
            
            var result = ColoredTextMerger.MergeSameColorSegments(segments);
            
            // Space segments should be kept separate
            AssertTrue(result.Count >= 2, "Should have at least 2 segments (space kept separate)");
            
            var hasHello = result.Any(s => s.Text.Contains("Hello"));
            var hasWorld = result.Any(s => s.Text.Contains("World"));
            
            AssertTrue(hasHello, "Should contain 'Hello'");
            AssertTrue(hasWorld, "Should contain 'World'");
        }
        
        #endregion
        
        #region Space Normalization Tests
        
        private static void TestSpaceNormalization()
        {
            Console.WriteLine("\n--- Testing Space Normalization ---");
            
            var segments = new List<ColoredText>
            {
                new ColoredText("Hello  ", Colors.White),  // Trailing spaces
                new ColoredText("  World", Colors.White)   // Leading spaces
            };
            
            var result = ColoredTextMerger.MergeAdjacentSegments(segments);
            
            // Should normalize multiple spaces
            var combined = string.Join("", result.Select(s => s.Text));
            AssertTrue(!combined.Contains("  "), "Should normalize multiple spaces");
        }
        
        private static void TestMultipleSpaces()
        {
            Console.WriteLine("\n--- Testing Multiple Spaces ---");
            
            var segments = new List<ColoredText>
            {
                new ColoredText("Hello", Colors.White),
                new ColoredText("   ", Colors.White),  // Multiple spaces
                new ColoredText("World", Colors.White)
            };
            
            var result = ColoredTextMerger.MergeAdjacentSegments(segments);
            
            // Should normalize to single space
            var combined = string.Join("", result.Select(s => s.Text));
            AssertTrue(!combined.Contains("   "), "Should normalize multiple spaces to single space");
        }
        
        private static void TestBoundarySpaces()
        {
            Console.WriteLine("\n--- Testing Boundary Spaces ---");
            
            var segments = new List<ColoredText>
            {
                new ColoredText("Hello ", Colors.White),
                new ColoredText(" World", Colors.White)
            };
            
            var result = ColoredTextMerger.MergeAdjacentSegments(segments);
            
            // Should handle boundary spaces correctly (avoid double spaces)
            var combined = string.Join("", result.Select(s => s.Text));
            AssertTrue(combined.Contains("Hello") && combined.Contains("World"), 
                "Should contain both words");
        }
        
        #endregion
        
        #region Edge Case Tests
        
        private static void TestEmptySegments()
        {
            Console.WriteLine("\n--- Testing Empty Segments ---");
            
            var segments = new List<ColoredText>
            {
                new ColoredText("Hello", Colors.White),
                new ColoredText("", Colors.White),  // Empty segment
                new ColoredText("World", Colors.White)
            };
            
            var result = ColoredTextMerger.MergeAdjacentSegments(segments);
            
            // Empty segments should be removed
            AssertTrue(result.All(s => !string.IsNullOrEmpty(s.Text)), "Should remove empty segments");
            
            var hasHello = result.Any(s => s.Text.Contains("Hello"));
            var hasWorld = result.Any(s => s.Text.Contains("World"));
            
            AssertTrue(hasHello, "Should contain 'Hello'");
            AssertTrue(hasWorld, "Should contain 'World'");
        }
        
        private static void TestNullInput()
        {
            Console.WriteLine("\n--- Testing Null Input ---");
            
            var result = ColoredTextMerger.MergeAdjacentSegments(null!);
            AssertTrue(result != null && result.Count == 0, "Null input should return empty list");
            
            var result2 = ColoredTextMerger.MergeSameColorSegments(null!);
            AssertTrue(result2 != null && result2.Count == 0, "Null input should return empty list (same color)");
        }
        
        private static void TestSingleCharacterSegments()
        {
            Console.WriteLine("\n--- Testing Single Character Segments ---");
            
            var segments = new List<ColoredText>
            {
                new ColoredText("H", Colors.Red),
                new ColoredText("e", Colors.Red),
                new ColoredText("l", Colors.Red),
                new ColoredText("l", Colors.Red),
                new ColoredText("o", Colors.Red)
            };
            
            var result = ColoredTextMerger.MergeSameColorSegments(segments);
            
            // Single character segments of same color should be merged
            AssertTrue(result.Count == 1, "Single character segments of same color should be merged");
            AssertTrue(result[0].Text == "Hello", "Should merge to 'Hello'");
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

