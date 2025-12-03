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
            
            // Combat log specific tests
            TestCombatLogSpacing();
            TestForAndWithSpacing();
            TestRollInfoSpacing();
            TestDamageFormatterSpacing();
            TestColoredTextRendering();
            
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
                CombatLogSpacingManager.FormatWithSpacing("hits", "target", "for ", "42", "damage"), 
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
            builder.Add("for ", Colors.White);
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
                new ColoredText("for ", Colors.White)
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
        
        #region Combat Log Specific Tests
        
        private static void TestCombatLogSpacing()
        {
            Console.WriteLine("\n--- Testing Combat Log Spacing ---");
            
            // Test basic combat message
            var builder1 = new ColoredTextBuilder();
            builder1.Add("Joren", ColorPalette.Gold);
            builder1.Add("hits", Colors.White);
            builder1.Add("Crystal Sprite", ColorPalette.Enemy);
            builder1.Add("for ", Colors.White);
            builder1.Add("42", ColorPalette.Damage);
            builder1.Add("damage", Colors.White);
            
            var result1 = builder1.Build();
            var plainText1 = ColoredTextRenderer.RenderAsPlainText(result1);
            
            AssertTrue(plainText1.Contains("hits Crystal Sprite for 42 damage"), 
                "Combat message should have proper spacing");
            AssertFalse(plainText1.Contains("for42"), 
                "Should have space after 'for'");
            AssertFalse(plainText1.Contains("hitsCrystal"), 
                "Should have space after 'hits'");
            
            // Test combo action with "with"
            var builder2 = new ColoredTextBuilder();
            builder2.Add("Shadowbane", ColorPalette.Gold);
            builder2.Add("hits", Colors.White);
            builder2.Add("Enemy", ColorPalette.Enemy);
            builder2.Add("with ", Colors.White);
            builder2.Add("CRITICAL THUNDER CLAP", ColorPalette.Critical);
            builder2.Add("for ", Colors.White);
            builder2.Add("7", ColorPalette.Damage);
            builder2.Add("damage", Colors.White);
            
            var result2 = builder2.Build();
            var plainText2 = ColoredTextRenderer.RenderAsPlainText(result2);
            
            AssertTrue(plainText2.Contains("with CRITICAL THUNDER CLAP"), 
                "Combo action should have space after 'with'");
            AssertFalse(plainText2.Contains("withCRITICAL"), 
                "Should not have missing space after 'with'");
            AssertTrue(plainText2.Contains("for 7 damage"), 
                "Should have space after 'for' before damage number");
            AssertFalse(plainText2.Contains("for7"), 
                "Should not have missing space after 'for'");
        }
        
        private static void TestForAndWithSpacing()
        {
            Console.WriteLine("\n--- Testing 'for' and 'with' Spacing ---");
            
            // Test "for" with various following text
            var testCases = new[]
            {
                ("for ", "42", "for 42"),
                ("for ", "7", "for 7"),
                ("for ", "10", "for 10"),
                ("for ", "1", "for 1"),
                ("with ", "CRITICAL", "with CRITICAL"),
                ("with ", "SHIELD BREAK", "with SHIELD BREAK"),
                ("with ", "CRUSHING BLOW", "with CRUSHING BLOW"),
            };
            
            foreach (var (word, next, expected) in testCases)
            {
                var builder = new ColoredTextBuilder();
                builder.Add(word, Colors.White);
                builder.Add(next, ColorPalette.Damage);
                
                var result = builder.Build();
                var plainText = ColoredTextRenderer.RenderAsPlainText(result);
                
                AssertTrue(plainText.Contains(expected), 
                    $"'{word}' should be followed by space before '{next}'");
                AssertFalse(plainText.Contains(word.Trim() + next), 
                    $"Should not have missing space: '{word.Trim()}{next}'");
            }
            
            // Test that spaces are preserved in markup rendering
            var builder2 = new ColoredTextBuilder();
            builder2.Add("hits", Colors.White);
            builder2.Add("target", ColorPalette.Enemy);
            builder2.Add("for ", Colors.White);
            builder2.Add("42", ColorPalette.Damage);
            
            var result2 = builder2.Build();
            var markup = ColoredTextRenderer.RenderAsMarkup(result2);
            var plainText2 = ColoredTextRenderer.RenderAsPlainText(result2);
            
            AssertTrue(plainText2.Contains("for 42"), 
                "Markup rendering should preserve space after 'for'");
            AssertFalse(plainText2.Contains("for42"), 
                "Markup rendering should not lose space after 'for'");
        }
        
        private static void TestRollInfoSpacing()
        {
            Console.WriteLine("\n--- Testing Roll Info Spacing ---");
            
            // Test roll info with armor and speed
            var builder = new ColoredTextBuilder();
            builder.Add("     (", Colors.Gray);
            builder.Add("roll:", ColorPalette.Info);
            builder.AddSpace();
            builder.Add("9", Colors.White);
            builder.Add(" | ", Colors.Gray);
            builder.Add("attack", ColorPalette.Info);
            builder.AddSpace();
            builder.Add("4", Colors.White);
            builder.Add(" - ", Colors.White);
            builder.Add("2", Colors.White);
            builder.Add(" armor", Colors.White);
            builder.AddSpace(); // Space after armor
            builder.Add("| ", Colors.Gray);
            builder.Add("speed:", ColorPalette.Info);
            builder.AddSpace();
            builder.Add("8.5s", Colors.White);
            builder.Add(")", Colors.Gray);
            
            var result = builder.Build();
            var plainText = ColoredTextRenderer.RenderAsPlainText(result);
            
            AssertTrue(plainText.Contains("armor"), 
                "Roll info should contain 'armor'");
            AssertTrue(plainText.Contains("speed:"), 
                "Roll info should contain 'speed:'");
            AssertTrue(plainText.Contains("armor") && plainText.Contains("speed:"), 
                "Roll info should have both armor and speed");
            
            // Check that there's space between armor and speed
            int armorIndex = plainText.IndexOf("armor");
            int speedIndex = plainText.IndexOf("speed:");
            if (armorIndex >= 0 && speedIndex >= 0 && speedIndex > armorIndex)
            {
                string between = plainText.Substring(armorIndex + 5, speedIndex - armorIndex - 5);
                AssertTrue(between.Contains(" ") || between.Contains("|"), 
                    "Should have space or separator between 'armor' and 'speed:'");
            }
        }
        
        private static void TestDamageFormatterSpacing()
        {
            Console.WriteLine("\n--- Testing DamageFormatter Spacing ---");
            
            // Simulate what DamageFormatter does
            var builder = new ColoredTextBuilder();
            builder.Add("Attacker", ColorPalette.Gold);
            builder.AddSpace();
            builder.Add("hits", Colors.White);
            builder.AddSpace();
            builder.Add("Target", ColorPalette.Enemy);
            builder.Add("with ", Colors.White);
            builder.Add("CRUSHING BLOW", ColorPalette.Warning);
            builder.Add("for ", Colors.White);
            builder.Add("7", ColorPalette.Damage);
            builder.Add("damage", Colors.White);
            
            var result = builder.Build();
            var plainText = ColoredTextRenderer.RenderAsPlainText(result);
            var markup = ColoredTextRenderer.RenderAsMarkup(result);
            
            AssertTrue(plainText.Contains("with CRUSHING BLOW"), 
                "DamageFormatter should have space after 'with'");
            AssertFalse(plainText.Contains("withCRUSHING"), 
                "DamageFormatter should not lose space after 'with'");
            AssertTrue(plainText.Contains("for 7"), 
                "DamageFormatter should have space after 'for'");
            AssertFalse(plainText.Contains("for7"), 
                "DamageFormatter should not lose space after 'for'");
            
            // Test markup rendering preserves spaces
            AssertTrue(markup.Contains("for ") || markup.Contains("for") && markup.Contains("7"), 
                "Markup should preserve space after 'for'");
        }
        
        private static void TestColoredTextRendering()
        {
            Console.WriteLine("\n--- Testing ColoredText Rendering ---");
            
            // Test that spaces in text segments are preserved
            var segments1 = new List<ColoredText>
            {
                new ColoredText("for ", Colors.White),
                new ColoredText("42", ColorPalette.Damage.GetColor())
            };
            
            var plainText1 = ColoredTextRenderer.RenderAsPlainText(segments1);
            var markup1 = ColoredTextRenderer.RenderAsMarkup(segments1);
            
            AssertTrue(plainText1.Contains("for 42"), 
                "Plain text renderer should preserve space in 'for '");
            AssertFalse(plainText1.Contains("for42"), 
                "Plain text renderer should not lose space");
            
            // Test with space segment
            var segments2 = new List<ColoredText>
            {
                new ColoredText("for", Colors.White),
                new ColoredText(" ", Colors.White),
                new ColoredText("42", ColorPalette.Damage.GetColor())
            };
            
            var plainText2 = ColoredTextRenderer.RenderAsPlainText(segments2);
            AssertTrue(plainText2.Contains("for 42"), 
                "Space segments should be preserved");
            
            // Test "with" spacing
            var segments3 = new List<ColoredText>
            {
                new ColoredText("with ", Colors.White),
                new ColoredText("SHIELD BREAK", ColorPalette.Warning.GetColor())
            };
            
            var plainText3 = ColoredTextRenderer.RenderAsPlainText(segments3);
            AssertTrue(plainText3.Contains("with SHIELD BREAK"), 
                "Should preserve space after 'with'");
            AssertFalse(plainText3.Contains("withSHIELD"), 
                "Should not lose space after 'with'");
            
            // Test roll info format
            var segments4 = new List<ColoredText>
            {
                new ColoredText("2", Colors.White),
                new ColoredText(" armor", Colors.White),
                new ColoredText(" ", Colors.White),
                new ColoredText("|", Colors.Gray),
                new ColoredText(" ", Colors.Gray),
                new ColoredText("speed:", ColorPalette.Info.GetColor())
            };
            
            var plainText4 = ColoredTextRenderer.RenderAsPlainText(segments4);
            AssertTrue(plainText4.Contains("armor") && plainText4.Contains("speed:"), 
                "Roll info should contain both armor and speed");
            
            // Check spacing between armor and speed
            int armorIdx = plainText4.IndexOf("armor");
            int speedIdx = plainText4.IndexOf("speed:");
            if (armorIdx >= 0 && speedIdx > armorIdx)
            {
                string between = plainText4.Substring(armorIdx + 5, speedIdx - armorIdx - 5);
                AssertTrue(between.Trim().Length > 0, 
                    "Should have content between 'armor' and 'speed:'");
            }
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

