using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive unit tests for the CombatLogSpacingManager system.
    /// Tests spacing rules, word boundary detection, normalization, and edge cases.
    /// </summary>
    public static class CombatLogSpacingTest
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;
        
        /// <summary>
        /// Runs all spacing system tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== Combat Log Spacing System Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            
            // Core spacing logic tests
            TestShouldAddSpaceBetween();
            TestWordBoundaryDetection();
            TestPunctuationSpacing();
            TestWhitespaceHandling();
            
            // Normalization tests
            TestNormalizeSpacing();
            TestFormatWithSpacing();
            
            // ColoredText integration tests
            TestColoredTextBuilderSpacing();
            TestAddSpacingBetweenSegments();
            
            // Validation tests
            TestValidateSpacing();
            
            // Edge cases
            TestEdgeCases();
            
            // Print summary
            Console.WriteLine($"\n=== Test Summary ===");
            Console.WriteLine($"Tests Run: {_testsRun}");
            Console.WriteLine($"Passed: {_testsPassed}");
            Console.WriteLine($"Failed: {_testsFailed}");
            Console.WriteLine($"Success Rate: {(_testsPassed * 100.0 / _testsRun):F1}%");
        }
        
        #region Core Spacing Logic Tests
        
        private static void TestShouldAddSpaceBetween()
        {
            Console.WriteLine("\n--- Testing ShouldAddSpaceBetween ---");
            
            // Basic word spacing
            AssertTrue(CombatLogSpacingManager.ShouldAddSpaceBetween("hits", "target"), 
                "Should add space between words");
            AssertTrue(CombatLogSpacingManager.ShouldAddSpaceBetween("for", "42"), 
                "Should add space between word and number");
            AssertTrue(CombatLogSpacingManager.ShouldAddSpaceBetween("42", "damage"), 
                "Should add space between number and word");
            
            // No space after punctuation
            AssertFalse(CombatLogSpacingManager.ShouldAddSpaceBetween("hits!", "target"), 
                "Should not add space after exclamation");
            AssertFalse(CombatLogSpacingManager.ShouldAddSpaceBetween("hits?", "target"), 
                "Should not add space after question mark");
            AssertFalse(CombatLogSpacingManager.ShouldAddSpaceBetween("hits.", "target"), 
                "Should not add space after period");
            AssertFalse(CombatLogSpacingManager.ShouldAddSpaceBetween("hits:", "target"), 
                "Should not add space after colon");
            AssertFalse(CombatLogSpacingManager.ShouldAddSpaceBetween("hits,", "target"), 
                "Should not add space after comma");
            
            // No space before punctuation
            AssertFalse(CombatLogSpacingManager.ShouldAddSpaceBetween("hits", "!target"), 
                "Should not add space before exclamation");
            AssertFalse(CombatLogSpacingManager.ShouldAddSpaceBetween("hits", "?target"), 
                "Should not add space before question mark");
            AssertFalse(CombatLogSpacingManager.ShouldAddSpaceBetween("hits", ".target"), 
                "Should not add space before period");
            
            // No space after opening brackets
            AssertFalse(CombatLogSpacingManager.ShouldAddSpaceBetween("hits", "[target"), 
                "Should not add space before opening bracket");
            AssertFalse(CombatLogSpacingManager.ShouldAddSpaceBetween("[target", "hits"), 
                "Should not add space after opening bracket");
            
            // No space before closing brackets
            AssertFalse(CombatLogSpacingManager.ShouldAddSpaceBetween("target", "]hits"), 
                "Should not add space before closing bracket");
            AssertFalse(CombatLogSpacingManager.ShouldAddSpaceBetween("target]", "hits"), 
                "Should not add space after closing bracket");
            
            // Null/empty handling
            AssertFalse(CombatLogSpacingManager.ShouldAddSpaceBetween(null, "target"), 
                "Should not add space with null previous");
            AssertFalse(CombatLogSpacingManager.ShouldAddSpaceBetween("hits", null), 
                "Should not add space with null next");
            AssertFalse(CombatLogSpacingManager.ShouldAddSpaceBetween("", "target"), 
                "Should not add space with empty previous");
            AssertFalse(CombatLogSpacingManager.ShouldAddSpaceBetween("hits", ""), 
                "Should not add space with empty next");
        }
        
        private static void TestWordBoundaryDetection()
        {
            Console.WriteLine("\n--- Testing Word Boundary Detection ---");
            
            // Without word boundary check (default)
            AssertTrue(CombatLogSpacingManager.ShouldAddSpaceBetween("h", "i", checkWordBoundary: false), 
                "Should add space between single chars without word boundary check");
            
            // With word boundary check - same word
            AssertFalse(CombatLogSpacingManager.ShouldAddSpaceBetween("h", "i", checkWordBoundary: true), 
                "Should not add space between letters in same word");
            AssertFalse(CombatLogSpacingManager.ShouldAddSpaceBetween("h", "i", checkWordBoundary: true), 
                "Should not add space between digits in same word");
            AssertFalse(CombatLogSpacingManager.ShouldAddSpaceBetween("hi", "t", checkWordBoundary: true), 
                "Should not add space between word parts");
            
            // With word boundary check - different words
            AssertTrue(CombatLogSpacingManager.ShouldAddSpaceBetween("hits", "target", checkWordBoundary: true), 
                "Should add space between different words");
            AssertTrue(CombatLogSpacingManager.ShouldAddSpaceBetween("hit", "s", checkWordBoundary: true), 
                "Should add space when one segment has whitespace");
            
            // Edge case: single character words
            AssertTrue(CombatLogSpacingManager.ShouldAddSpaceBetween("a", "b", checkWordBoundary: true), 
                "Should add space between single character words");
        }
        
        private static void TestPunctuationSpacing()
        {
            Console.WriteLine("\n--- Testing Punctuation Spacing ---");
            
            // No space after punctuation
            var punctuationAfter = new[] { '!', '?', '.', ',', ':', ';', '[', '(', '{' };
            foreach (var punct in punctuationAfter)
            {
                AssertFalse(CombatLogSpacingManager.ShouldAddSpaceBetween($"word{punct}", "next"), 
                    $"Should not add space after '{punct}'");
            }
            
            // No space before punctuation
            var punctuationBefore = new[] { '!', '?', '.', ',', ':', ';', ']', ')', '}' };
            foreach (var punct in punctuationBefore)
            {
                AssertFalse(CombatLogSpacingManager.ShouldAddSpaceBetween("word", $"{punct}next"), 
                    $"Should not add space before '{punct}'");
            }
        }
        
        private static void TestWhitespaceHandling()
        {
            Console.WriteLine("\n--- Testing Whitespace Handling ---");
            
            // No space if previous ends with whitespace
            AssertFalse(CombatLogSpacingManager.ShouldAddSpaceBetween("word ", "next"), 
                "Should not add space when previous ends with space");
            AssertFalse(CombatLogSpacingManager.ShouldAddSpaceBetween("word\t", "next"), 
                "Should not add space when previous ends with tab");
            
            // No space if next starts with whitespace
            AssertFalse(CombatLogSpacingManager.ShouldAddSpaceBetween("word", " next"), 
                "Should not add space when next starts with space");
            AssertFalse(CombatLogSpacingManager.ShouldAddSpaceBetween("word", "\tnext"), 
                "Should not add space when next starts with tab");
            
            // No space for whitespace-only segments
            AssertFalse(CombatLogSpacingManager.ShouldAddSpaceBetween(" ", "next"), 
                "Should not add space for whitespace-only previous");
            AssertFalse(CombatLogSpacingManager.ShouldAddSpaceBetween("word", " "), 
                "Should not add space for whitespace-only next");
            AssertFalse(CombatLogSpacingManager.ShouldAddSpaceBetween("   ", "next"), 
                "Should not add space for multiple whitespace previous");
        }
        
        #endregion
        
        #region Normalization Tests
        
        private static void TestNormalizeSpacing()
        {
            Console.WriteLine("\n--- Testing NormalizeSpacing ---");
            
            // Double spaces
            AssertEqual("word next", CombatLogSpacingManager.NormalizeSpacing("word  next"), 
                "Should normalize double spaces");
            AssertEqual("word next", CombatLogSpacingManager.NormalizeSpacing("word   next"), 
                "Should normalize triple spaces");
            AssertEqual("word next", CombatLogSpacingManager.NormalizeSpacing("word    next"), 
                "Should normalize multiple spaces");
            
            // Leading/trailing spaces
            AssertEqual("word next", CombatLogSpacingManager.NormalizeSpacing(" word next "), 
                "Should trim leading and trailing spaces");
            AssertEqual("word next", CombatLogSpacingManager.NormalizeSpacing("  word next  "), 
                "Should trim multiple leading/trailing spaces");
            
            // Mixed spacing
            AssertEqual("word next more", CombatLogSpacingManager.NormalizeSpacing("word  next   more"), 
                "Should normalize multiple double spaces");
            
            // Edge cases
            AssertEqual("", CombatLogSpacingManager.NormalizeSpacing(""), 
                "Should handle empty string");
            AssertEqual("", CombatLogSpacingManager.NormalizeSpacing("   "), 
                "Should handle whitespace-only string");
            AssertEqual("word", CombatLogSpacingManager.NormalizeSpacing("word"), 
                "Should handle single word");
        }
        
        private static void TestFormatWithSpacing()
        {
            Console.WriteLine("\n--- Testing FormatWithSpacing ---");
            
            // Basic formatting
            AssertEqual("hits target for 42 damage", 
                CombatLogSpacingManager.FormatWithSpacing("hits", "target", "for", "42", "damage"), 
                "Should format multiple parts with spacing");
            
            // Null/empty handling
            AssertEqual("hits target", 
                CombatLogSpacingManager.FormatWithSpacing("hits", null, "target", ""), 
                "Should skip null and empty parts");
            
            // Already spaced parts
            AssertEqual("hits target for 42 damage", 
                CombatLogSpacingManager.FormatWithSpacing("hits ", " target ", " for ", " 42 ", " damage"), 
                "Should normalize spacing in parts");
        }
        
        #endregion
        
        #region ColoredText Integration Tests
        
        private static void TestColoredTextBuilderSpacing()
        {
            Console.WriteLine("\n--- Testing ColoredTextBuilder Spacing ---");
            
            var builder = new ColoredTextBuilder();
            builder.Add("hits", Colors.White);
            builder.Add("target", ColorPalette.Enemy);
            builder.Add("for", Colors.White);
            builder.Add("42", ColorPalette.Damage);
            builder.Add("damage", Colors.White);
            
            var result = builder.Build();
            var plainText = string.Join("", result.Select(s => s.Text));
            
            AssertTrue(plainText.Contains("hits target for 42 damage"), 
                "ColoredTextBuilder should add spaces between segments");
            AssertFalse(plainText.Contains("hits  target"), 
                "ColoredTextBuilder should not add double spaces");
        }
        
        private static void TestAddSpacingBetweenSegments()
        {
            Console.WriteLine("\n--- Testing AddSpacingBetweenSegments ---");
            
            var segments = new List<ColoredText>
            {
                new ColoredText("hits", Colors.White),
                new ColoredText("target", ColorPalette.Enemy.GetColor()),
                new ColoredText("for", Colors.White)
            };
            
            var spaced = CombatLogSpacingManager.AddSpacingBetweenSegments(segments);
            var plainText = string.Join("", spaced.Select(s => s.Text));
            
            AssertTrue(plainText.Contains("hits target for"), 
                "Should add spaces between segments");
            AssertEqual(5, spaced.Count, 
                "Should add 2 space segments (between 3 text segments)");
        }
        
        #endregion
        
        #region Validation Tests
        
        private static void TestValidateSpacing()
        {
            Console.WriteLine("\n--- Testing ValidateSpacing ---");
            
            // Valid spacing
            var issues1 = CombatLogSpacingManager.ValidateSpacing("hits target for 42 damage");
            AssertEqual(0, issues1.Count, 
                "Should find no issues in correctly spaced text");
            
            // Double spaces
            var issues2 = CombatLogSpacingManager.ValidateSpacing("hits  target");
            AssertTrue(issues2.Count > 0 && issues2[0].Contains("double spaces"), 
                "Should detect double spaces");
            
            // Space before punctuation
            var issues3 = CombatLogSpacingManager.ValidateSpacing("hits ! target");
            AssertTrue(issues3.Count > 0 && issues3[0].Contains("space before punctuation"), 
                "Should detect space before punctuation");
        }
        
        #endregion
        
        #region Edge Cases
        
        private static void TestEdgeCases()
        {
            Console.WriteLine("\n--- Testing Edge Cases ---");
            
            // Single character segments
            AssertTrue(CombatLogSpacingManager.ShouldAddSpaceBetween("a", "b"), 
                "Should handle single character segments");
            
            // Very long strings
            var longString1 = new string('a', 1000);
            var longString2 = new string('b', 1000);
            AssertTrue(CombatLogSpacingManager.ShouldAddSpaceBetween(longString1, longString2), 
                "Should handle very long strings");
            
            // Unicode characters
            AssertTrue(CombatLogSpacingManager.ShouldAddSpaceBetween("hits", "⚔️"), 
                "Should handle unicode characters");
            
            // Mixed case
            AssertTrue(CombatLogSpacingManager.ShouldAddSpaceBetween("Hits", "Target"), 
                "Should handle mixed case");
            
            // Numbers
            AssertTrue(CombatLogSpacingManager.ShouldAddSpaceBetween("42", "damage"), 
                "Should handle numbers");
            AssertTrue(CombatLogSpacingManager.ShouldAddSpaceBetween("damage", "42"), 
                "Should handle numbers in reverse");
        }
        
        #endregion
        
        #region Test Helpers
        
        private static void AssertTrue(bool condition, string message)
        {
            _testsRun++;
            if (condition)
            {
                Console.WriteLine($"✓ {message}");
                _testsPassed++;
            }
            else
            {
                Console.WriteLine($"✗ {message}");
                _testsFailed++;
            }
        }
        
        private static void AssertFalse(bool condition, string message)
        {
            AssertTrue(!condition, message);
        }
        
        private static void AssertEqual<T>(T expected, T actual, string message) where T : IEquatable<T>
        {
            _testsRun++;
            if (expected.Equals(actual))
            {
                Console.WriteLine($"✓ {message}");
                _testsPassed++;
            }
            else
            {
                Console.WriteLine($"✗ {message} (Expected: {expected}, Actual: {actual})");
                _testsFailed++;
            }
        }
        
        private static void AssertEqual(int expected, int actual, string message)
        {
            _testsRun++;
            if (expected == actual)
            {
                Console.WriteLine($"✓ {message}");
                _testsPassed++;
            }
            else
            {
                Console.WriteLine($"✗ {message} (Expected: {expected}, Actual: {actual})");
                _testsFailed++;
            }
        }
        
        #endregion
    }
}

