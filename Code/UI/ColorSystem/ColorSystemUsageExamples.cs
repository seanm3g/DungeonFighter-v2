using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// Examples showing how to use the new ColoredText system
    /// This demonstrates the migration from old color markup to the new structured system
    /// </summary>
    public static class ColorSystemUsageExamples
    {
        /// <summary>
        /// Example 1: Basic colored text creation
        /// </summary>
        public static void Example1_BasicColoredText()
        {
            // OLD WAY (problematic):
            // UIManager.WriteLine("&RPlayer&y deals &G25&y damage to &BEnemy&y!");
            
            // NEW WAY (clean and readable):
            var combatMessage = new ColoredTextBuilder()
                .Add("Player", ColorPalette.Player)
                .Add(" deals ", Colors.White)
                .Add("25", ColorPalette.Damage)
                .Add(" damage to ", Colors.White)
                .Add("Enemy", ColorPalette.Enemy)
                .Add("!", Colors.White)
                .Build();
            
            UIManager.WriteLineColoredSegments(combatMessage, UIMessageType.Combat);
        }
        
        /// <summary>
        /// Example 2: Using pattern-based coloring
        /// </summary>
        public static void Example2_PatternBasedColoring()
        {
            // OLD WAY:
            // UIManager.WriteLine("&RPlayer&y uses &Ghealing&y potion for &C50&y health!");
            
            // NEW WAY:
            var healingMessage = new ColoredTextBuilder()
                .Add("Player", ColorPalette.Player)
                .Add(" uses ", Colors.White)
                .AddWithPattern("healing", "healing")
                .Add(" potion for ", Colors.White)
                .Add("50", ColorPalette.Healing)
                .Add(" health!", Colors.White)
                .Build();
            
            UIManager.WriteLineColoredSegments(healingMessage, UIMessageType.Combat);
        }
        
        /// <summary>
        /// Example 3: Using the new combat message helper
        /// </summary>
        public static void Example3_CombatMessageHelper()
        {
            // OLD WAY:
            // UIManager.WriteLine("&RPlayer&y attacks &BEnemy&y for &G25&y damage!");
            
            // NEW WAY (much simpler):
            UIManager.WriteCombatMessage("Player", "attacks", "Enemy", 25);
            
            // Or with critical hit:
            UIManager.WriteCombatMessage("Player", "critically strikes", "Enemy", 50, isCritical: true);
            
            // Or with miss:
            UIManager.WriteCombatMessage("Player", "misses", "Enemy");
        }
        
        /// <summary>
        /// Example 4: Using healing message helper
        /// </summary>
        public static void Example4_HealingMessageHelper()
        {
            // OLD WAY:
            // UIManager.WriteLine("&RPlayer&y heals &RPlayer&y for &G30&y health!");
            
            // NEW WAY:
            UIManager.WriteHealingMessage("Player", "Player", 30);
        }
        
        /// <summary>
        /// Example 5: Using status effect message helper
        /// </summary>
        public static void Example5_StatusEffectMessageHelper()
        {
            // OLD WAY:
            // UIManager.WriteLine("&RPlayer&y is affected by &Ypoison&y!");
            
            // NEW WAY:
            UIManager.WriteStatusEffectMessage("Player", "poison", isApplied: true);
            
            // Or when effect is removed:
            UIManager.WriteStatusEffectMessage("Player", "poison", isApplied: false);
        }
        
        /// <summary>
        /// Example 6: Complex multi-colored text
        /// </summary>
        public static void Example6_ComplexMultiColoredText()
        {
            // OLD WAY (very hard to read and maintain):
            // UIManager.WriteLine("&RPlayer&y found a &P[Legendary]&y &G+5&y &BSteel Sword&y of &R+3&y &YFire&y damage!");
            
            // NEW WAY (clear and maintainable):
            var itemMessage = new ColoredTextBuilder()
                .Add("Player", ColorPalette.Player)
                .Add(" found a ", Colors.White)
                .Add("[Legendary]", ColorPalette.Legendary)
                .Add(" ", Colors.White)
                .Add("+5", ColorPalette.Success)
                .Add(" ", Colors.White)
                .Add("Steel Sword", ColorPalette.Weapon)
                .Add(" of ", Colors.White)
                .Add("+3", ColorPalette.Success)
                .Add(" ", Colors.White)
                .AddWithPattern("Fire", "fire")
                .Add(" damage!", Colors.White)
                .Build();
            
            UIManager.WriteLineColoredSegments(itemMessage, UIMessageType.System);
        }
        
        /// <summary>
        /// Example 7: Using character-specific colors
        /// </summary>
        public static void Example7_CharacterSpecificColors()
        {
            // Create a custom color profile for a specific character
            var playerProfile = new CharacterColorProfile("Player");
            playerProfile.DamageColor = ColorPalette.Orange; // Custom damage color
            playerProfile.PrimaryColor = ColorPalette.Cyan;   // Custom primary color
            
            CharacterColorManager.SetProfile("Player", playerProfile);
            
            // Now when we use patterns, they'll use the character's custom colors
            var message = new ColoredTextBuilder()
                .Add("Player", ColorPalette.Player)
                .Add(" deals ", Colors.White)
                .AddWithPattern("damage", "damage") // Will use orange instead of red
                .Build();
            
            UIManager.WriteLineColoredSegments(message, UIMessageType.Combat);
        }
        
        /// <summary>
        /// Example 8: Rendering to different formats
        /// </summary>
        public static void Example8_RenderingToDifferentFormats()
        {
            var coloredText = new ColoredTextBuilder()
                .Add("Hello", Colors.Red)
                .Add(" ", Colors.White)
                .Add("World", Colors.Blue)
                .Build();
            
            // Render as plain text (for logging, etc.)
            var plainText = ColoredTextRenderer.RenderAsPlainText(coloredText);
            Console.WriteLine($"Plain: {plainText}"); // Output: "Hello World"
            
            // Render as HTML (for web interfaces)
            var html = ColoredTextRenderer.RenderAsHtml(coloredText);
            Console.WriteLine($"HTML: {html}"); // Output: <span style="color: #FF0000">Hello</span> <span style="color: #0000FF">World</span>
            
            // Render as ANSI (for console)
            var ansi = ColoredTextRenderer.RenderAsAnsi(coloredText);
            Console.WriteLine($"ANSI: {ansi}"); // Output: \x1b[31mHello\x1b[0m \x1b[34mWorld\x1b[0m
            
            // Render as debug format (for development)
            var debug = ColoredTextRenderer.RenderAsDebug(coloredText);
            Console.WriteLine($"Debug: {debug}"); // Output: [Red]Hello[/Red] [Blue]World[/Blue]
        }
        
        /// <summary>
        /// Example 9: Text manipulation (truncation, padding, centering)
        /// </summary>
        public static void Example9_TextManipulation()
        {
            var longText = new ColoredTextBuilder()
                .Add("This is a very long message that might need to be truncated", Colors.Red)
                .Add(" or padded", Colors.Blue)
                .Build();
            
            // Truncate to 20 characters
            var truncated = ColoredTextRenderer.Truncate(longText, 20);
            var truncatedPlain = ColoredTextRenderer.RenderAsPlainText(truncated);
            Console.WriteLine($"Truncated: {truncatedPlain}"); // Output: "This is a very long"
            
            // Pad to 50 characters
            var padded = ColoredTextRenderer.PadRight(longText, 50);
            var paddedPlain = ColoredTextRenderer.RenderAsPlainText(padded);
            Console.WriteLine($"Padded: '{paddedPlain}'"); // Output: "This is a very long message that might need to be truncated or padded    "
            
            // Center within 60 characters
            var centered = ColoredTextRenderer.Center(longText, 60);
            var centeredPlain = ColoredTextRenderer.RenderAsPlainText(centered);
            Console.WriteLine($"Centered: '{centeredPlain}'"); // Output: "  This is a very long message that might need to be truncated or padded  "
        }
        
        /// <summary>
        /// Example 10: Migration from old system
        /// </summary>
        public static void Example10_MigrationFromOldSystem()
        {
            // OLD SYSTEM (problematic):
            string oldMarkup = "&RPlayer&y deals &G25&y damage to &BEnemy&y!";
            
            // Convert old markup to new system
            var convertedSegments = CompatibilityLayer.ConvertOldMarkup(oldMarkup);
            
            // Use the converted segments
            UIManager.WriteLineColoredSegments(convertedSegments, UIMessageType.Combat);
            
            // Or create new content using the new system
            var newMessage = new ColoredTextBuilder()
                .Add("Player", ColorPalette.Player)
                .Add(" deals ", Colors.White)
                .Add("25", ColorPalette.Damage)
                .Add(" damage to ", Colors.White)
                .Add("Enemy", ColorPalette.Enemy)
                .Add("!", Colors.White)
                .Build();
            
            UIManager.WriteLineColoredSegments(newMessage, UIMessageType.Combat);
        }
    }
}
