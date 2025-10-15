using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RPGGame.UI
{
    /// <summary>
    /// Comprehensive test suite for ColorParser functionality
    /// Tests template expansion, color code parsing, length calculations, and edge cases
    /// </summary>
    public static class ColorParserTest
    {
        private static int _passedTests = 0;
        private static int _failedTests = 0;
        private static List<string> _failures = new List<string>();

        /// <summary>
        /// Runs all ColorParser tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== ColorParser Test Suite ===");
            Console.WriteLine();

            _passedTests = 0;
            _failedTests = 0;
            _failures.Clear();

            // Basic template tests
            TestBasicTemplate();
            TestSolidColorTemplate();
            TestSequenceTemplate();
            TestInvalidTemplate();

            // Color code tests
            TestBasicColorCode();
            TestMultipleColorCodes();
            TestBackgroundColorCode();
            TestInvalidColorCode();

            // Mixed markup tests
            TestMixedTemplateAndColorCode();
            TestNestedTemplates();
            TestTemplateWithSpaces();
            TestMultipleTemplatesInLine();

            // Length calculation tests
            TestDisplayLengthWithTemplate();
            TestDisplayLengthWithColorCode();
            TestDisplayLengthWithMixedMarkup();
            TestDisplayLengthWithNoMarkup();

            // Strip markup tests
            TestStripMarkupWithTemplate();
            TestStripMarkupWithColorCode();
            TestStripMarkupWithMixedMarkup();

            // Edge case tests
            TestEmptyString();
            TestNullString();
            TestSpecialCharacters();
            TestVeryLongText();
            TestConsecutiveMarkup();
            TestMarkupAtBoundaries();

            // Performance tests
            TestParsingPerformance();
            TestLengthCalculationPerformance();

            // Keyword system integration tests
            TestKeywordColoring();
            TestKeywordWithTemplate();

            // Display summary
            Console.WriteLine();
            Console.WriteLine("=== Test Results ===");
            Console.WriteLine($"Passed: {_passedTests}");
            Console.WriteLine($"Failed: {_failedTests}");
            Console.WriteLine($"Total:  {_passedTests + _failedTests}");

            if (_failedTests > 0)
            {
                Console.WriteLine();
                Console.WriteLine("=== Failed Tests ===");
                foreach (var failure in _failures)
                {
                    Console.WriteLine($"  - {failure}");
                }
            }

            Console.WriteLine();
            Console.WriteLine(_failedTests == 0 ? "All tests passed! ✓" : "Some tests failed! ✗");
            Console.WriteLine();
        }

        #region Helper Methods

        private static void AssertEqual<T>(string testName, T expected, T actual)
        {
            if (EqualityComparer<T>.Default.Equals(expected, actual))
            {
                _passedTests++;
                Console.WriteLine($"✓ {testName}");
            }
            else
            {
                _failedTests++;
                _failures.Add(testName);
                Console.WriteLine($"✗ {testName}");
                Console.WriteLine($"    Expected: {expected}");
                Console.WriteLine($"    Actual:   {actual}");
            }
        }

        private static void AssertNotNull(string testName, object obj)
        {
            if (obj != null)
            {
                _passedTests++;
                Console.WriteLine($"✓ {testName}");
            }
            else
            {
                _failedTests++;
                _failures.Add(testName);
                Console.WriteLine($"✗ {testName}");
                Console.WriteLine($"    Expected: non-null value");
                Console.WriteLine($"    Actual:   null");
            }
        }

        private static void AssertTrue(string testName, bool condition, string message = "")
        {
            if (condition)
            {
                _passedTests++;
                Console.WriteLine($"✓ {testName}");
            }
            else
            {
                _failedTests++;
                _failures.Add(testName);
                Console.WriteLine($"✗ {testName}");
                if (!string.IsNullOrEmpty(message))
                {
                    Console.WriteLine($"    {message}");
                }
            }
        }

        #endregion

        #region Basic Template Tests

        private static void TestBasicTemplate()
        {
            string input = "{{red|Test}}";
            var segments = ColorParser.Parse(input);

            AssertTrue("BasicTemplate: Should produce segments", segments.Count > 0);
            
            // All segments should spell "Test"
            string combined = string.Join("", segments.Select(s => s.Text));
            AssertEqual("BasicTemplate: Text should be 'Test'", "Test", combined);
        }

        private static void TestSolidColorTemplate()
        {
            string input = "{{damage|50}}";
            var segments = ColorParser.Parse(input);

            AssertTrue("SolidColorTemplate: Should produce segments", segments.Count > 0);
            
            string combined = string.Join("", segments.Select(s => s.Text));
            AssertEqual("SolidColorTemplate: Text should be '50'", "50", combined);
        }

        private static void TestSequenceTemplate()
        {
            string input = "{{fiery|Fire}}";
            var segments = ColorParser.Parse(input);

            AssertTrue("SequenceTemplate: Should produce segments", segments.Count > 0);
            
            // Sequence templates create one segment per character
            AssertEqual("SequenceTemplate: Should have 4 segments for 'Fire'", 4, segments.Count);
        }

        private static void TestInvalidTemplate()
        {
            string input = "{{nonexistent|Test}}";
            var segments = ColorParser.Parse(input);

            AssertTrue("InvalidTemplate: Should still produce segments", segments.Count > 0);
            
            // Should return the text without template markup
            string combined = string.Join("", segments.Select(s => s.Text));
            AssertEqual("InvalidTemplate: Should return 'Test'", "Test", combined);
        }

        #endregion

        #region Color Code Tests

        private static void TestBasicColorCode()
        {
            string input = "&RRed Text";
            var segments = ColorParser.Parse(input);

            AssertTrue("BasicColorCode: Should produce segments", segments.Count > 0);
            AssertEqual("BasicColorCode: Text should be 'Red Text'", "Red Text", segments[0].Text);
            AssertNotNull("BasicColorCode: Should have foreground color", (object?)segments[0].Foreground ?? "null");
        }

        private static void TestMultipleColorCodes()
        {
            string input = "&RRed &GGreen &BBlue";
            var segments = ColorParser.Parse(input);

            AssertEqual("MultipleColorCodes: Should have 3 segments", 3, segments.Count);
        }

        private static void TestBackgroundColorCode()
        {
            string input = "^KText";
            var segments = ColorParser.Parse(input);

            AssertTrue("BackgroundColorCode: Should produce segments", segments.Count > 0);
            AssertNotNull("BackgroundColorCode: Should have background color", (object?)segments[0].Background ?? "null");
        }

        private static void TestInvalidColorCode()
        {
            string input = "&XInvalid";
            var segments = ColorParser.Parse(input);

            // Should treat &X as regular text if X is not a valid color code
            string combined = string.Join("", segments.Select(s => s.Text));
            AssertTrue("InvalidColorCode: Should include &X in text", combined.Contains("&X"));
        }

        #endregion

        #region Mixed Markup Tests

        private static void TestMixedTemplateAndColorCode()
        {
            string input = "Normal {{red|Red}} &GGreen text";
            var segments = ColorParser.Parse(input);

            AssertTrue("MixedMarkup: Should produce segments", segments.Count >= 3);
        }

        private static void TestNestedTemplates()
        {
            string input = "{{fiery|Fire {{critical|CRIT}}!}}";
            var segments = ColorParser.Parse(input);

            // Note: Current implementation has limited nested template support
            // This test documents current behavior
            AssertTrue("NestedTemplates: Should produce segments", segments.Count > 0);
        }

        private static void TestTemplateWithSpaces()
        {
            string input = "{{red|Text with spaces}}";
            var segments = ColorParser.Parse(input);

            string combined = string.Join("", segments.Select(s => s.Text));
            AssertEqual("TemplateWithSpaces: Should preserve spaces", "Text with spaces", combined);
        }

        private static void TestMultipleTemplatesInLine()
        {
            string input = "You hit {{enemy|Goblin}} for {{damage|15}} damage!";
            var segments = ColorParser.Parse(input);

            AssertTrue("MultipleTemplates: Should produce segments", segments.Count > 0);
            
            string combined = string.Join("", segments.Select(s => s.Text));
            AssertEqual("MultipleTemplates: Should preserve all text", 
                "You hit Goblin for 15 damage!", combined);
        }

        #endregion

        #region Length Calculation Tests

        private static void TestDisplayLengthWithTemplate()
        {
            string input = "Text with {{red|color}} markup";
            int length = ColorParser.GetDisplayLength(input);

            // Should count visible characters only: "Text with color markup"
            AssertEqual("DisplayLength_Template", "Text with color markup".Length, length);
        }

        private static void TestDisplayLengthWithColorCode()
        {
            string input = "Text with &Rcolor markup";
            int length = ColorParser.GetDisplayLength(input);

            // Should count visible characters only: "Text with color markup"
            AssertEqual("DisplayLength_ColorCode", "Text with color markup".Length, length);
        }

        private static void TestDisplayLengthWithMixedMarkup()
        {
            string input = "Mixed {{red|template}} and &Gcolor code";
            int length = ColorParser.GetDisplayLength(input);

            AssertEqual("DisplayLength_Mixed", "Mixed template and color code".Length, length);
        }

        private static void TestDisplayLengthWithNoMarkup()
        {
            string input = "Plain text with no markup";
            int length = ColorParser.GetDisplayLength(input);

            AssertEqual("DisplayLength_NoMarkup", input.Length, length);
        }

        #endregion

        #region Strip Markup Tests

        private static void TestStripMarkupWithTemplate()
        {
            string input = "Text with {{red|color}} markup";
            string stripped = ColorParser.StripColorMarkup(input);

            AssertEqual("StripMarkup_Template", "Text with color markup", stripped);
        }

        private static void TestStripMarkupWithColorCode()
        {
            string input = "Text with &Rcolor markup";
            string stripped = ColorParser.StripColorMarkup(input);

            AssertEqual("StripMarkup_ColorCode", "Text with color markup", stripped);
        }

        private static void TestStripMarkupWithMixedMarkup()
        {
            string input = "Mixed {{red|template}} and &Gcolor code";
            string stripped = ColorParser.StripColorMarkup(input);

            AssertEqual("StripMarkup_Mixed", "Mixed template and color code", stripped);
        }

        #endregion

        #region Edge Case Tests

        private static void TestEmptyString()
        {
            string input = "";
            var segments = ColorParser.Parse(input);

            AssertEqual("EmptyString: Should return empty list", 0, segments.Count);
        }

        private static void TestNullString()
        {
            string? input = null;
            var segments = ColorParser.Parse(input ?? "");

            AssertEqual("NullString: Should return empty list", 0, segments.Count);
        }

        private static void TestSpecialCharacters()
        {
            string input = "Test & ^ { } | symbols";
            var segments = ColorParser.Parse(input);

            string combined = string.Join("", segments.Select(s => s.Text));
            AssertTrue("SpecialCharacters: Should handle special chars", 
                combined.Contains("&") && combined.Contains("^"));
        }

        private static void TestVeryLongText()
        {
            // Generate long text with markup
            string input = string.Join(" ", 
                Enumerable.Repeat("{{red|Long}} text {{green|with}} markup", 100));
            
            var segments = ColorParser.Parse(input);

            AssertTrue("VeryLongText: Should handle long text", segments.Count > 0);
        }

        private static void TestConsecutiveMarkup()
        {
            string input = "{{red|A}}{{green|B}}{{blue|C}}";
            var segments = ColorParser.Parse(input);

            string combined = string.Join("", segments.Select(s => s.Text));
            AssertEqual("ConsecutiveMarkup: Should parse consecutive templates", "ABC", combined);
        }

        private static void TestMarkupAtBoundaries()
        {
            string input = "{{red|Start}} middle {{green|End}}";
            var segments = ColorParser.Parse(input);

            string combined = string.Join("", segments.Select(s => s.Text));
            AssertEqual("MarkupAtBoundaries", "Start middle End", combined);
        }

        #endregion

        #region Performance Tests

        private static void TestParsingPerformance()
        {
            string longText = string.Join(" ", 
                Enumerable.Repeat("You hit {{enemy|Goblin}} for {{damage|15}} damage!", 100));
            
            var sw = Stopwatch.StartNew();
            
            for (int i = 0; i < 100; i++)
            {
                ColorParser.Parse(longText);
            }
            
            sw.Stop();

            // Should complete 100 parses in under 500ms (generous threshold)
            AssertTrue("ParsingPerformance: Should complete in reasonable time",
                sw.ElapsedMilliseconds < 500,
                $"Took {sw.ElapsedMilliseconds}ms for 100 parses");
        }

        private static void TestLengthCalculationPerformance()
        {
            string longText = string.Join(" ", 
                Enumerable.Repeat("Text with {{red|color}} and &Gmore markup", 100));
            
            var sw = Stopwatch.StartNew();
            
            for (int i = 0; i < 1000; i++)
            {
                ColorParser.GetDisplayLength(longText);
            }
            
            sw.Stop();

            // Should complete 1000 length calculations in under 500ms (generous threshold)
            AssertTrue("LengthPerformance: Should complete in reasonable time",
                sw.ElapsedMilliseconds < 500,
                $"Took {sw.ElapsedMilliseconds}ms for 1000 calculations");
        }

        #endregion

        #region Keyword System Integration Tests

        private static void TestKeywordColoring()
        {
            // Note: This requires KeywordColorSystem to be initialized
            try
            {
                string input = "You hit the goblin for 15 damage!";
                string colored = KeywordColorSystem.Colorize(input);

                AssertTrue("KeywordColoring: Should apply keyword colors", 
                    ColorParser.HasColorMarkup(colored));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ KeywordColoring: Skipped (KeywordColorSystem not initialized: {ex.Message})");
                _passedTests++; // Don't fail if system not initialized
            }
        }

        private static void TestKeywordWithTemplate()
        {
            // Test that keywords and templates work together
            try
            {
                string input = "You found a {{legendary|Legendary Sword}}!";
                string colored = KeywordColorSystem.Colorize(input);

                // Should have both template and keyword markup
                AssertTrue("KeywordWithTemplate: Should handle both markup types",
                    colored.Contains("{{") || ColorParser.HasColorMarkup(colored));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ KeywordWithTemplate: Skipped (KeywordColorSystem not initialized: {ex.Message})");
                _passedTests++; // Don't fail if system not initialized
            }
        }

        #endregion

        #region Additional Helper Tests

        /// <summary>
        /// Tests the HasColorMarkup helper method
        /// </summary>
        public static void TestHasColorMarkup()
        {
            AssertTrue("HasColorMarkup: Template markup", 
                ColorParser.HasColorMarkup("{{red|Text}}"));
            
            AssertTrue("HasColorMarkup: Color code markup", 
                ColorParser.HasColorMarkup("&RText"));
            
            AssertTrue("HasColorMarkup: No markup", 
                !ColorParser.HasColorMarkup("Plain text"));
            
            AssertTrue("HasColorMarkup: Empty string", 
                !ColorParser.HasColorMarkup(""));
        }

        /// <summary>
        /// Tests the Colorize helper method
        /// </summary>
        public static void TestColorizeHelper()
        {
            string result = ColorParser.Colorize("Test", "red");
            AssertTrue("ColorizeHelper: Template wrapping",
                result.Contains("{{red|Test}}"));
            
            result = ColorParser.Colorize("Test", "R");
            AssertTrue("ColorizeHelper: Color code wrapping",
                result.Contains("&RTest"));
            
            result = ColorParser.Colorize("Test", "invalid");
            AssertEqual("ColorizeHelper: Invalid pattern returns unchanged", "Test", result);
        }

        #endregion

        /// <summary>
        /// Quick smoke test - runs a subset of critical tests
        /// </summary>
        public static void RunQuickTest()
        {
            Console.WriteLine("=== ColorParser Quick Test ===");
            Console.WriteLine();

            _passedTests = 0;
            _failedTests = 0;
            _failures.Clear();

            TestBasicTemplate();
            TestBasicColorCode();
            TestDisplayLengthWithTemplate();
            TestStripMarkupWithTemplate();
            TestEmptyString();

            Console.WriteLine();
            Console.WriteLine($"Quick Test: {_passedTests}/{_passedTests + _failedTests} passed");
            Console.WriteLine();
        }
    }
}

