using System;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;
using RPGGame.Tests;

namespace RPGGame.Tests
{
    /// <summary>
    /// Test runner for ColorParser functionality
    /// </summary>
    public static class ColorParserTestRunner
    {
        /// <summary>
        /// Runs comprehensive ColorParser tests
        /// Tests template expansion, color code parsing, length calculations, and edge cases
        /// </summary>
        public static void RunTest()
        {
            TextDisplayIntegration.DisplaySystem("Starting ColorParser Test Suite...");
            TextDisplayIntegration.DisplaySystem("This will test color template and markup parsing functionality.");
            
            if (!TestHarnessBase.PromptContinue())
                return;
            
            Console.WriteLine();
            Console.WriteLine();
            
            // Run comprehensive ColorParser tests
            TextDisplayIntegration.DisplaySystem("Running ColorParser comprehensive tests...");
            
            // Test basic parsing
            TestBasicParsing();
            
            // Test template expansion
            TestTemplateExpansion();
            
            // Test length calculations
            TestLengthCalculations();
            
            // Test edge cases
            TestEdgeCases();
            
            TextDisplayIntegration.DisplaySystem("\nColorParser Test Suite completed!");
            TestHarnessBase.WaitForContinue();
        }
        
        /// <summary>
        /// Runs a quick smoke test of ColorParser (subset of critical tests)
        /// </summary>
        public static void RunQuickTest()
        {
            TextDisplayIntegration.DisplaySystem("Running ColorParser Quick Test...");
            Console.WriteLine();
            
            // Run quick smoke tests
            TextDisplayIntegration.DisplaySystem("Running quick ColorParser smoke tests...");
            
            // Test basic functionality
            TestBasicParsing();
            
            TextDisplayIntegration.DisplaySystem("\nColorParser Quick Test completed!");
            TestHarnessBase.WaitForContinue();
        }
        
        /// <summary>
        /// Tests basic ColorParser functionality
        /// </summary>
        private static void TestBasicParsing()
        {
            TextDisplayIntegration.DisplaySystem("Testing basic ColorParser functionality...");
            
            var testCases = new[]
            {
                "Simple text",
                "Text with {{damage|red}} color",
                "Text with {{damage|red}} on {{success|green}} background",
                "Text with {{fiery|fire effect}}",
                "Mixed {{damage|red}} and {{icy|ice}} effects"
            };
            
            foreach (var test in testCases)
            {
                try
                {
                    var segments = ColoredTextParser.Parse(test);
                    TextDisplayIntegration.DisplaySystem($"  ✓ '{test}' -> {segments.Count} segments");
                }
                catch (Exception ex)
                {
                    TextDisplayIntegration.DisplaySystem($"  ✗ '{test}' -> ERROR: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Tests template expansion functionality
        /// </summary>
        private static void TestTemplateExpansion()
        {
            TextDisplayIntegration.DisplaySystem("Testing template expansion...");
            
            var templates = new[]
            {
                "{{fiery|Fire}}",
                "{{icy|Ice}}",
                "{{toxic|Poison}}",
                "{{crystal|Crystal}}",
                "{{golden|Gold}}",
                "{{holy|Holy}}",
                "{{shadow|Shadow}}"
            };
            
            foreach (var template in templates)
            {
                try
                {
                    var segments = ColoredTextParser.Parse(template);
                    TextDisplayIntegration.DisplaySystem($"  ✓ '{template}' -> {segments.Count} segments");
                }
                catch (Exception ex)
                {
                    TextDisplayIntegration.DisplaySystem($"  ✗ '{template}' -> ERROR: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Tests length calculation functionality
        /// </summary>
        private static void TestLengthCalculations()
        {
            TextDisplayIntegration.DisplaySystem("Testing length calculations...");
            
            var testCases = new[]
            {
                ("Simple text", 11),
                ("{{damage|Red}} text", 9),
                ("{{fiery|Fire}}", 4),
                ("Mixed {{damage|red}} and {{icy|ice}}", 19)
            };
            
            foreach (var (text, expectedLength) in testCases)
            {
                try
                {
                    var segments = ColoredTextParser.Parse(text);
                    var actualLength = ColoredTextRenderer.GetDisplayLength(segments);
                    var status = actualLength == expectedLength ? "✓" : "✗";
                    TextDisplayIntegration.DisplaySystem($"  {status} '{text}' -> Expected: {expectedLength}, Actual: {actualLength}");
                }
                catch (Exception ex)
                {
                    TextDisplayIntegration.DisplaySystem($"  ✗ '{text}' -> ERROR: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Tests edge cases and error handling
        /// </summary>
        private static void TestEdgeCases()
        {
            TextDisplayIntegration.DisplaySystem("Testing edge cases...");
            
            var edgeCases = new[]
            {
                "", // Empty string
                "{{", // Incomplete template
                "{{invalid|template}}", // Invalid template
                "Text with {{", // Incomplete template at end
                "{{fiery|", // Incomplete template
                "Normal text with no markup"
            };
            
            foreach (var test in edgeCases)
            {
                try
                {
                    var segments = ColoredTextParser.Parse(test);
                    TextDisplayIntegration.DisplaySystem($"  ✓ '{test}' -> {segments.Count} segments (handled gracefully)");
                }
                catch (Exception ex)
                {
                    TextDisplayIntegration.DisplaySystem($"  ⚠ '{test}' -> Exception: {ex.Message}");
                }
            }
        }
    }
}

