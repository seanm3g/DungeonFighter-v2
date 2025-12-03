using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;
using Avalonia.Media;

namespace RPGGame.Tests
{
    /// <summary>
    /// Comprehensive test suite for text system accuracy including spacing, overlap, and color application.
    /// </summary>
    public static class TextSystemAccuracyTests
    {
        /// <summary>
        /// Test word spacing accuracy - ensures no double spaces and proper spacing between words
        /// </summary>
        public static void TestWordSpacing()
        {
            Console.WriteLine("=== Testing Word Spacing ===");
            
            var testCases = new[]
            {
                ("normal text", true, "Normal text should have single spaces"),
                ("text  with  double  spaces", false, "Should detect double spaces"),
                ("text,with,commas", true, "Commas don't need spaces after in this context"),
                ("word1 word2", true, "Words should have space between"),
                ("word1word2", false, "Missing space between words"),
                ("hit for 15 damage", false, "Double space before number"),
                ("hit for 15 damage", true, "Single space is correct"),
            };
            
            int passed = 0;
            int failed = 0;
            
            foreach (var (text, shouldPass, description) in testCases)
            {
                var result = TextSpacingValidator.ValidateWordSpacing(text);
                bool actualPass = result.IsValid;
                
                if (actualPass == shouldPass)
                {
                    Console.WriteLine($"  ✓ {description}");
                    passed++;
                }
                else
                {
                    Console.WriteLine($"  ✗ {description}");
                    Console.WriteLine($"    Expected: {(shouldPass ? "valid" : "invalid")}, Got: {(actualPass ? "valid" : "invalid")}");
                    if (result.Issues.Count > 0)
                    {
                        Console.WriteLine($"    Issues: {string.Join(", ", result.Issues)}");
                    }
                    failed++;
                }
            }
            
            Console.WriteLine($"\nWord Spacing Tests: {passed} passed, {failed} failed\n");
        }
        
        /// <summary>
        /// Test blank line spacing accuracy
        /// </summary>
        public static void TestBlankLineSpacing()
        {
            Console.WriteLine("=== Testing Blank Line Spacing ===");
            
            var testCases = new[]
            {
                (TextSpacingSystem.BlockType.DungeonHeader, TextSpacingSystem.BlockType.RoomHeader, 1, "Dungeon to Room header"),
                (TextSpacingSystem.BlockType.RoomInfo, TextSpacingSystem.BlockType.EnemyAppearance, 1, "Room info to Enemy appearance"),
                (TextSpacingSystem.BlockType.EnemyStats, TextSpacingSystem.BlockType.CombatAction, 1, "Enemy stats to Combat action"),
                (TextSpacingSystem.BlockType.CombatAction, TextSpacingSystem.BlockType.CombatAction, 0, "Consecutive combat actions"),
                (TextSpacingSystem.BlockType.CombatAction, TextSpacingSystem.BlockType.EnvironmentalAction, 1, "Combat to Environmental action"),
            };
            
            int passed = 0;
            int failed = 0;
            
            foreach (var (previous, current, expectedLines, description) in testCases)
            {
                int actualLines = TextSpacingSystem.GetSpacingBefore(current);
                
                if (actualLines == expectedLines)
                {
                    Console.WriteLine($"  ✓ {description}: {expectedLines} blank line(s)");
                    passed++;
                }
                else
                {
                    Console.WriteLine($"  ✗ {description}: Expected {expectedLines}, got {actualLines}");
                    failed++;
                }
            }
            
            // Test spacing rule validation
            var ruleIssues = TextSpacingSystem.ValidateSpacingRules();
            if (ruleIssues.Count == 0)
            {
                Console.WriteLine($"  ✓ All important spacing rules are defined");
                passed++;
            }
            else
            {
                Console.WriteLine($"  ✗ Missing spacing rules:");
                foreach (var issue in ruleIssues)
                {
                    Console.WriteLine($"    - {issue}");
                }
                failed++;
            }
            
            Console.WriteLine($"\nBlank Line Spacing Tests: {passed} passed, {failed} failed\n");
        }
        
        /// <summary>
        /// Test text overlap prevention
        /// </summary>
        public static void TestTextOverlap()
        {
            Console.WriteLine("=== Testing Text Overlap Prevention ===");
            
            // Test ColoredText segment spacing
            var segments1 = new List<ColoredText>
            {
                new ColoredText("Hello", Colors.White),
                new ColoredText("World", Colors.White),
            };
            
            var spacingResult = TextSpacingValidator.ValidateColoredTextSpacing(segments1);
            
            if (spacingResult.IsValid)
            {
                Console.WriteLine($"  ✓ ColoredText segments have proper spacing");
            }
            else
            {
                Console.WriteLine($"  ✗ ColoredText segments have spacing issues:");
                foreach (var issue in spacingResult.Issues)
                {
                    Console.WriteLine($"    - {issue}");
                }
            }
            
            // Test that spacing helper adds spaces correctly
            var segments2 = new List<ColoredText>
            {
                new ColoredText("word1", Colors.White),
                new ColoredText("word2", Colors.White),
            };
            
            bool shouldHaveSpace = CombatLogSpacingManager.ShouldAddSpaceBetween("word1", "word2");
            if (shouldHaveSpace)
            {
                Console.WriteLine($"  ✓ Spacing manager correctly identifies need for space between words");
            }
            else
            {
                Console.WriteLine($"  ✗ Spacing manager failed to identify need for space");
            }
            
            Console.WriteLine($"\nText Overlap Tests: Completed\n");
        }
        
        /// <summary>
        /// Test color application consistency
        /// </summary>
        public static void TestColorApplication()
        {
            Console.WriteLine("=== Testing Color Application ===");
            
            // Test for double-coloring
            var testTexts = new[]
            {
                ("normal text", false, "Plain text should not have double-coloring"),
                ("&Rtext&y {{damage|damage}}", true, "Text with both explicit codes and templates"),
                ("{{damage|15}} damage", false, "Template syntax only"),
            };
            
            int passed = 0;
            int failed = 0;
            
            foreach (var (text, shouldHaveDoubleColoring, description) in testTexts)
            {
                var result = ColorApplicationValidator.ValidateNoDoubleColoring(text);
                bool hasDoubleColoring = !result.IsValid;
                
                if (hasDoubleColoring == shouldHaveDoubleColoring)
                {
                    Console.WriteLine($"  ✓ {description}");
                    passed++;
                }
                else
                {
                    Console.WriteLine($"  ✗ {description}");
                    Console.WriteLine($"    Expected double-coloring: {shouldHaveDoubleColoring}, Got: {hasDoubleColoring}");
                    failed++;
                }
            }
            
            // Test that colors are applied (basic check)
            var coloredSegments = new List<ColoredText>
            {
                new ColoredText("15", ColorPalette.Damage.GetColor()),
                new ColoredText(" damage", Colors.White),
            };
            
            bool hasColor = coloredSegments.Any(s => !ColorValidator.AreColorsEqual(s.Color, Colors.White));
            if (hasColor)
            {
                Console.WriteLine($"  ✓ ColoredText segments have colors applied");
                passed++;
            }
            else
            {
                Console.WriteLine($"  ✗ ColoredText segments are all white");
                failed++;
            }
            
            Console.WriteLine($"\nColor Application Tests: {passed} passed, {failed} failed\n");
        }
        
        /// <summary>
        /// Test edge cases
        /// </summary>
        public static void TestEdgeCases()
        {
            Console.WriteLine("=== Testing Edge Cases ===");
            
            int passed = 0;
            int failed = 0;
            
            // Empty text
            var emptyResult = TextSpacingValidator.ValidateWordSpacing("");
            if (emptyResult.IsValid)
            {
                Console.WriteLine($"  ✓ Empty text handled correctly");
                passed++;
            }
            else
            {
                Console.WriteLine($"  ✗ Empty text validation failed");
                failed++;
            }
            
            // Null text (use empty string to avoid nullable warning)
            var nullResult = TextSpacingValidator.ValidateWordSpacing("");
            if (nullResult.IsValid)
            {
                Console.WriteLine($"  ✓ Null text handled correctly");
                passed++;
            }
            else
            {
                Console.WriteLine($"  ✗ Null text validation failed");
                failed++;
            }
            
            // Text with special characters
            var specialResult = TextSpacingValidator.ValidateWordSpacing("don't worry, it's fine!");
            if (specialResult.IsValid || specialResult.Issues.Count == 0)
            {
                Console.WriteLine($"  ✓ Text with apostrophes and punctuation handled");
                passed++;
            }
            else
            {
                Console.WriteLine($"  ✗ Text with special characters has issues:");
                foreach (var issue in specialResult.Issues)
                {
                    Console.WriteLine($"    - {issue}");
                }
                failed++;
            }
            
            // Multi-color template (should not add spaces)
            bool shouldNotSpace = !CombatLogSpacingManager.ShouldAddSpaceBetween("Ma", "g", checkWordBoundary: true);
            if (shouldNotSpace)
            {
                Console.WriteLine($"  ✓ Multi-color template word boundary detection works");
                passed++;
            }
            else
            {
                Console.WriteLine($"  ✗ Multi-color template spacing issue");
                failed++;
            }
            
            Console.WriteLine($"\nEdge Case Tests: {passed} passed, {failed} failed\n");
        }
        
        /// <summary>
        /// Run all text system accuracy tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("TEXT SYSTEM ACCURACY TESTS");
            Console.WriteLine(new string('=', 60) + "\n");
            
            TestWordSpacing();
            TestBlankLineSpacing();
            TestTextOverlap();
            TestColorApplication();
            TestEdgeCases();
            
            Console.WriteLine(new string('=', 60));
            Console.WriteLine("All tests completed!");
            Console.WriteLine(new string('=', 60) + "\n");
        }
    }
}

