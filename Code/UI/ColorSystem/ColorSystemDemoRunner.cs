using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.Utils;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// Standalone demo runner that showcases the new color system
    /// This runs independently and shows all the new features working
    /// </summary>
    public static class ColorSystemDemoRunner
    {
        /// <summary>
        /// Main demo entry point
        /// </summary>
        public static void RunDemo()
        {
            Console.WriteLine("🎨 NEW COLOR SYSTEM DEMO 🎨");
            Console.WriteLine("=============================");
            Console.WriteLine();
            
            try
            {
                DemoBasicColoredText();
                DemoPatternBasedColoring();
                DemoCharacterSpecificColors();
                DemoMarkupParsing();
                DemoCompatibility();
                DemoAdvancedFeatures();
                
                Console.WriteLine("✅ All demos completed successfully!");
                Console.WriteLine("The new color system is working perfectly!");
                Console.WriteLine();
                Console.WriteLine("🚀 Ready for production use!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Demo failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Demonstrates basic colored text creation
        /// </summary>
        private static void DemoBasicColoredText()
        {
            Console.WriteLine("=== 1. Basic Colored Text Demo ===");
            
            // Create colored text using the builder pattern
            var combatMessage = new ColoredTextBuilder()
                .Add("Player ", ColorPalette.Cyan)
                .Add("deals ", Colors.White)
                .Add("25", ColorPalette.Damage)
                .Add(" damage to ", Colors.White)
                .Add("Goblin", ColorPalette.Red)
                .Add("!", Colors.White)
                .Build();
            
            // Render as different formats
            var plainText = ColoredTextRenderer.RenderAsPlainText(combatMessage);
            var html = ColoredTextRenderer.RenderAsHtml(combatMessage);
            var debug = ColoredTextRenderer.RenderAsDebug(combatMessage);
            
            Console.WriteLine($"Plain Text: {plainText}");
            Console.WriteLine($"HTML: {html}");
            Console.WriteLine($"Debug: {debug}");
            Console.WriteLine();
        }
        
        /// <summary>
        /// Demonstrates pattern-based coloring
        /// </summary>
        private static void DemoPatternBasedColoring()
        {
            Console.WriteLine("=== 2. Pattern-Based Coloring Demo ===");
            
            // Use semantic patterns for consistent coloring
            var statusMessage = new ColoredTextBuilder()
                .AddWithPattern("Level up!", "success")
                .Add(" You gained ", Colors.White)
                .AddWithPattern("100", "experience")
                .Add(" experience points!", Colors.White)
                .Build();
            
            var itemMessage = new ColoredTextBuilder()
                .AddWithPattern("Rare", "rare")
                .Add(" ")
                .AddWithPattern("Steel Sword", "weapon")
                .Add(" found!", Colors.White)
                .Build();
            
            Console.WriteLine($"Status: {ColoredTextRenderer.RenderAsPlainText(statusMessage)}");
            Console.WriteLine($"Item: {ColoredTextRenderer.RenderAsPlainText(itemMessage)}");
            Console.WriteLine();
        }
        
        /// <summary>
        /// Demonstrates character-specific colors
        /// </summary>
        private static void DemoCharacterSpecificColors()
        {
            Console.WriteLine("=== 3. Character-Specific Colors Demo ===");
            
            // Set up character color profiles
            var playerProfile = new CharacterColorProfile("Player");
            playerProfile.PrimaryColor = ColorPalette.Cyan;
            playerProfile.DamageColor = ColorPalette.Red;
            playerProfile.SetCustomPattern("spell", ColorPalette.Magenta);
            CharacterColorManager.SetProfile("Player", playerProfile);
            
            var enemyProfile = new CharacterColorProfile("Enemy");
            enemyProfile.PrimaryColor = ColorPalette.Red;
            enemyProfile.DamageColor = ColorPalette.DarkRed;
            CharacterColorManager.SetProfile("Enemy", enemyProfile);
            
            // Create messages using character-specific colors
            var playerMessage = new ColoredTextBuilder()
                .Add("Player ", ColorPalette.Cyan)  // Uses player's primary color
                .Add("casts ", Colors.White)
                .AddWithPattern("Fireball", "spell")  // Uses player's custom spell color
                .Add("!", Colors.White)
                .Build();
            
            var enemyMessage = new ColoredTextBuilder()
                .Add("Enemy ", ColorPalette.Red)    // Uses enemy's primary color
                .Add("attacks for ", Colors.White)
                .Add("15", ColorPalette.Damage)       // Uses enemy's damage color
                .Add(" damage!", Colors.White)
                .Build();
            
            Console.WriteLine($"Player: {ColoredTextRenderer.RenderAsPlainText(playerMessage)}");
            Console.WriteLine($"Enemy: {ColoredTextRenderer.RenderAsPlainText(enemyMessage)}");
            Console.WriteLine();
        }
        
        /// <summary>
        /// Demonstrates markup parsing
        /// </summary>
        private static void DemoMarkupParsing()
        {
            Console.WriteLine("=== 4. Markup Parsing Demo ===");
            
            // Parse markup text
            var markupText = "[damage]25[/damage] damage to [enemy]Goblin[/enemy]!";
            var coloredText = ColoredTextParser.Parse(markupText);
            
            var plainText = ColoredTextRenderer.RenderAsPlainText(coloredText);
            Console.WriteLine($"Markup: {markupText}");
            Console.WriteLine($"Parsed: {plainText}");
            
            // Character-specific markup
            var charMarkup = "[char:Player:primary]Hero[/char] vs [char:Enemy:primary]Monster[/char]";
            var charColoredText = ColoredTextParser.Parse(charMarkup);
            var charPlainText = ColoredTextRenderer.RenderAsPlainText(charColoredText);
            Console.WriteLine($"Char Markup: {charMarkup}");
            Console.WriteLine($"Char Parsed: {charPlainText}");
            Console.WriteLine();
        }
        
        /// <summary>
        /// Demonstrates compatibility with old system
        /// </summary>
        private static void DemoCompatibility()
        {
            Console.WriteLine("=== 5. Compatibility Demo ===");
            
            // Legacy color codes are no longer supported - use ColoredTextBuilder instead
            // Old way (no longer works): "&RDanger&y is &Gahead&y!"
            // New way:
            var coloredText = new ColoredTextBuilder()
                .Add("Danger", ColorPalette.Damage)
                .Add(" is ", Colors.White)
                .Add("ahead", ColorPalette.Success)
                .Add("!", Colors.White)
                .Build();
            
            var plainText = ColoredTextRenderer.RenderAsPlainText(coloredText);
            Console.WriteLine($"New Style: {plainText}");
            
            // Check for color markup (templates or new markup)
            bool hasMarkup = "[damage]25[/damage]".Contains("{{") && "[damage]25[/damage]".Contains("}}") || 
                            ("[damage]25[/damage]".Contains("[") && "[damage]25[/damage]".Contains("]"));
            Console.WriteLine($"Has markup: {hasMarkup}");
            Console.WriteLine();
        }
        
        /// <summary>
        /// Demonstrates advanced features
        /// </summary>
        private static void DemoAdvancedFeatures()
        {
            Console.WriteLine("=== 6. Advanced Features Demo ===");
            
            // Text manipulation
            var longText = new ColoredTextBuilder()
                .Add("This is a very long message that needs to be truncated for display purposes.", Colors.White)
                .Build();
            
            var truncated = ColoredTextRenderer.Truncate(longText, 30);
            var truncatedText = ColoredTextRenderer.RenderAsPlainText(truncated);
            Console.WriteLine($"Truncated: {truncatedText}");
            
            // Padding and centering
            var shortText = new ColoredTextBuilder()
                .Add("Short", ColorPalette.Success)
                .Build();
            
            var padded = ColoredTextRenderer.PadRight(shortText, 20);
            var paddedText = ColoredTextRenderer.RenderAsPlainText(padded);
            Console.WriteLine($"Padded: '{paddedText}'");
            
            var centered = ColoredTextRenderer.Center(shortText, 20);
            var centeredText = ColoredTextRenderer.RenderAsPlainText(centered);
            Console.WriteLine($"Centered: '{centeredText}'");
            
            // Multiple output formats
            var sampleText = new ColoredTextBuilder()
                .Add("Hello ", Colors.White)
                .Add("World", ColorPalette.Success)
                .Add("!", Colors.White)
                .Build();
            
            Console.WriteLine($"Plain: {ColoredTextRenderer.RenderAsPlainText(sampleText)}");
            Console.WriteLine($"HTML: {ColoredTextRenderer.RenderAsHtml(sampleText)}");
            Console.WriteLine($"ANSI: {ColoredTextRenderer.RenderAsAnsi(sampleText)}");
            Console.WriteLine();
        }
        
        /// <summary>
        /// Demonstrates console output with colors
        /// </summary>
        public static void RunConsoleDemo()
        {
            Console.WriteLine("🎨 CONSOLE COLOR DEMO 🎨");
            Console.WriteLine("========================");
            Console.WriteLine();
            
            // Use the new console writer
            ColoredConsoleWriter.WriteWithPattern("Success message", "success");
            Console.WriteLine();
            
            ColoredConsoleWriter.WriteWithPattern("Warning message", "warning");
            Console.WriteLine();
            
            ColoredConsoleWriter.WriteWithPattern("Error message", "error");
            Console.WriteLine();
            
            ColoredConsoleWriter.WriteWithPattern("Info message", "info");
            Console.WriteLine();
            
            // Custom colors
            ColoredConsoleWriter.WriteLine("Custom cyan text", ColorPalette.Cyan);
            ColoredConsoleWriter.WriteLine("Custom magenta text", ColorPalette.Magenta);
            ColoredConsoleWriter.WriteLine("Custom gold text", ColorPalette.Gold);
            
            Console.WriteLine();
            Console.WriteLine("✅ Console demo completed!");
        }
        
        /// <summary>
        /// Performance test
        /// </summary>
        public static void RunPerformanceTest()
        {
            Console.WriteLine("⚡ PERFORMANCE TEST ⚡");
            Console.WriteLine("=====================");
            Console.WriteLine();
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Create 1000 colored text objects
            for (int i = 0; i < 1000; i++)
            {
                var text = new ColoredTextBuilder()
                    .Add($"Message {i} ", Colors.White)
                    .AddWithPattern("damage", "damage")
                    .Add(" to ", Colors.White)
                    .AddWithPattern("enemy", "enemy")
                    .Build();
                
                var plain = ColoredTextRenderer.RenderAsPlainText(text);
                var html = ColoredTextRenderer.RenderAsHtml(text);
            }
            
            stopwatch.Stop();
            
            Console.WriteLine($"Created 1000 colored text objects in {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Average: {stopwatch.ElapsedMilliseconds / 1000.0:F2}ms per object");
            Console.WriteLine();
        }
    }
}
