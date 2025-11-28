using System;
using System.Collections.Generic;
using Avalonia.Media;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// Demo class to showcase the new color system
    /// This demonstrates how the new system works and can be used as a reference
    /// </summary>
    public static class ColorSystemDemo
    {
        /// <summary>
        /// Demonstrates basic colored text creation
        /// </summary>
        public static void DemoBasicColoredText()
        {
            Console.WriteLine("=== Basic Colored Text Demo ===");
            
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
        public static void DemoPatternBasedColoring()
        {
            Console.WriteLine("=== Pattern-Based Coloring Demo ===");
            
            // Use semantic patterns for consistent coloring
            var statusMessage = new ColoredTextBuilder()
                .AddWithPattern("Level up!", "success")
                .Add(" You gained ", Colors.White)
                .AddWithPattern("100", "experience")
                .Add(" experience points!", Colors.White)
                .Build();
            
            var plainText = ColoredTextRenderer.RenderAsPlainText(statusMessage);
            Console.WriteLine($"Status Message: {plainText}");
            Console.WriteLine();
        }
        
        /// <summary>
        /// Demonstrates character-specific colors
        /// </summary>
        public static void DemoCharacterSpecificColors()
        {
            Console.WriteLine("=== Character-Specific Colors Demo ===");
            
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
                .Add("Player ", ColorPalette.Player)  // Uses player's primary color
                .Add("casts ", Colors.White)
                .AddWithPattern("Fireball", "spell")  // Uses player's custom spell color
                .Add("!", Colors.White)
                .Build();
            
            var enemyMessage = new ColoredTextBuilder()
                .Add("Enemy ", ColorPalette.Enemy)    // Uses enemy's primary color
                .Add("attacks for ", Colors.White)
                .Add("15", ColorPalette.Damage)       // Uses enemy's damage color
                .Add(" damage!", Colors.White)
                .Build();
            
            Console.WriteLine($"Player Message: {ColoredTextRenderer.RenderAsPlainText(playerMessage)}");
            Console.WriteLine($"Enemy Message: {ColoredTextRenderer.RenderAsPlainText(enemyMessage)}");
            Console.WriteLine();
        }
        
        /// <summary>
        /// Demonstrates markup parsing
        /// </summary>
        public static void DemoMarkupParsing()
        {
            Console.WriteLine("=== Markup Parsing Demo ===");
            
            // Parse markup text
            var markupText = "[damage]25[/damage] damage to [enemy]Goblin[/enemy]!";
            var coloredText = ColoredTextParser.Parse(markupText);
            
            var plainText = ColoredTextRenderer.RenderAsPlainText(coloredText);
            Console.WriteLine($"Markup: {markupText}");
            Console.WriteLine($"Parsed: {plainText}");
            Console.WriteLine();
        }
        
        /// <summary>
        /// Demonstrates compatibility with old system
        /// </summary>
        public static void DemoCompatibility()
        {
            Console.WriteLine("=== Compatibility Demo ===");
            
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
            Console.WriteLine();
        }
        
        /// <summary>
        /// Runs all demos
        /// </summary>
        public static void RunAllDemos()
        {
            Console.WriteLine("🎨 NEW COLOR SYSTEM DEMO 🎨");
            Console.WriteLine("=============================");
            Console.WriteLine();
            
            DemoBasicColoredText();
            DemoPatternBasedColoring();
            DemoCharacterSpecificColors();
            DemoMarkupParsing();
            DemoCompatibility();
            
            Console.WriteLine("✅ All demos completed successfully!");
            Console.WriteLine("The new color system is working perfectly!");
        }
    }
}
