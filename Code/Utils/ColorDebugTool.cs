using System;
using System.Collections.Generic;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Utils
{
    /// <summary>
    /// Debugging tool for color system testing and diagnostics
    /// </summary>
    public static class ColorDebugTool
    {
        /// <summary>
        /// Displays all available colors from the palette
        /// </summary>
        public static void ShowColorPalette()
        {
            Console.WriteLine("=== Color Palette ===");
            Console.WriteLine("Displaying all available colors...");
            Console.WriteLine();
            
            // Display basic colors
            var colorPalettes = new[]
            {
                (ColorPalette.Red, "Red"),
                (ColorPalette.DarkRed, "DarkRed"),
                (ColorPalette.Green, "Green"),
                (ColorPalette.DarkGreen, "DarkGreen"),
                (ColorPalette.Blue, "Blue"),
                (ColorPalette.DarkBlue, "DarkBlue"),
                (ColorPalette.Yellow, "Yellow"),
                (ColorPalette.Orange, "Orange"),
                (ColorPalette.Cyan, "Cyan"),
                (ColorPalette.Magenta, "Magenta"),
                (ColorPalette.Purple, "Purple"),
                (ColorPalette.White, "White"),
                (ColorPalette.Gray, "Gray"),
                (ColorPalette.DarkGray, "DarkGray"),
                (ColorPalette.Black, "Black"),
                (ColorPalette.Gold, "Gold"),
                (ColorPalette.Silver, "Silver"),
                (ColorPalette.Bronze, "Bronze")
            };
            
            foreach (var (palette, name) in colorPalettes)
            {
                var color = palette.GetColor();
                Console.WriteLine($"{name,-15} RGB({color.R:000},{color.G:000},{color.B:000})");
            }
        }
        
        /// <summary>
        /// Tests the color parser with sample text
        /// </summary>
        public static void TestColorParser()
        {
            Console.WriteLine("=== Color Parser Test ===");
            Console.WriteLine();
            
            var testStrings = new[]
            {
                "Simple text",
                "Text with {{fiery|fire effect}}",
                "Multiple {{icy|ice}} and {{toxic|poison}} effects",
                "Nested {{golden|golden {{holy|holy}}}} text"
            };
            
            foreach (var test in testStrings)
            {
                Console.WriteLine($"Input:  {test}");
                var parsed = ColorParser.Parse(test);
                Console.WriteLine($"Output: {parsed.Count} segments");
                Console.WriteLine();
            }
        }
        
        /// <summary>
        /// Tests keyword coloring
        /// </summary>
        public static void TestKeywordColoring()
        {
            Console.WriteLine("=== Keyword Coloring Test ===");
            Console.WriteLine();
            
            var testStrings = new[]
            {
                "You hit the enemy for 25 damage",
                "Critical hit! You deal massive damage",
                "You heal for 15 health",
                "The enemy is poisoned and bleeding"
            };
            
            // KeywordColorSystem initializes automatically in static constructor
            
            foreach (var test in testStrings)
            {
                Console.WriteLine($"Input:  {test}");
                var colored = KeywordColorSystem.ColorText(test);
                Console.WriteLine($"Output: {colored.Count} segments");
                Console.WriteLine();
            }
        }
        
        /// <summary>
        /// Tests color templates
        /// </summary>
        public static void TestColorTemplates()
        {
            Console.WriteLine("=== Color Templates Test ===");
            Console.WriteLine();
            
            var templates = new (string name, Func<string, List<ColoredText>> func)[]
            {
                ("Fiery", ColorTemplateLibrary.Fiery),
                ("Icy", ColorTemplateLibrary.Icy),
                ("Toxic", ColorTemplateLibrary.Toxic),
                ("Crystalline", ColorTemplateLibrary.Crystalline),
                ("Golden", ColorTemplateLibrary.Golden),
                ("Holy", ColorTemplateLibrary.Holy),
                ("Shadow", ColorTemplateLibrary.Shadow)
            };
            
            const string testText = "Test Text";
            
            foreach (var (name, func) in templates)
            {
                Console.WriteLine($"{name}: ");
                var result = func(testText);
                Console.WriteLine($"  Segments: {result.Count}");
                Console.WriteLine();
            }
        }
        
        /// <summary>
        /// Tests combat message coloring
        /// </summary>
        public static void RunCombatMessageTests()
        {
            Console.WriteLine("=== Combat Message Tests ===");
            Console.WriteLine();
            
            var combatMessages = new[]
            {
                "You deal 25 damage to the goblin!",
                "CRITICAL HIT! 50 damage!",
                "You block the attack with your shield!",
                "The enemy misses!",
                "You heal for 15 health"
            };
            
            foreach (var message in combatMessages)
            {
                Console.WriteLine($"Message: {message}");
                var parsed = ColorParser.Parse(message);
                Console.WriteLine($"Segments: {parsed.Count}");
                Console.WriteLine();
            }
        }
        
        /// <summary>
        /// Runs all debug tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("==========================================");
            Console.WriteLine("    Color System Debugging Tool");
            Console.WriteLine("==========================================");
            Console.WriteLine();
            
            ShowColorPalette();
            Console.WriteLine();
            
            TestColorParser();
            Console.WriteLine();
            
            TestKeywordColoring();
            Console.WriteLine();
            
            TestColorTemplates();
            Console.WriteLine();
            
            RunCombatMessageTests();
            Console.WriteLine();
            
            Console.WriteLine("==========================================");
            Console.WriteLine("    All Tests Complete");
            Console.WriteLine("==========================================");
        }
    }
}
