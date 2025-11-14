using System;
using System.Collections.Generic;
using Avalonia.Media;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// Examples of how to use the new color system
    /// </summary>
    public static class ColorSystemExamples
    {
        /// <summary>
        /// Example: Creating colored text with the builder pattern
        /// </summary>
        public static List<ColoredText> CreateCombatMessage()
        {
            return new ColoredTextBuilder()
                .Add("You deal ", Colors.White)
                .Add("25", ColorPalette.Damage)
                .Add(" damage to the ", Colors.White)
                .Add("Goblin", ColorPalette.Red)
                .Add("!", Colors.White)
                .Build();
        }
        
        /// <summary>
        /// Example: Using patterns for consistent coloring
        /// </summary>
        public static List<ColoredText> CreatePatternBasedMessage()
        {
            return new ColoredTextBuilder()
                .Add("Critical hit! ", ColorPalette.Critical)
                .AddWithPattern("50", "damage")
                .Add(" damage dealt to ", Colors.White)
                .AddWithPattern("Orc", "enemy")
                .Add("!", Colors.White)
                .Build();
        }
        
        /// <summary>
        /// Example: Character-specific coloring
        /// </summary>
        public static List<ColoredText> CreateCharacterSpecificMessage(string characterName)
        {
            var profile = CharacterColorManager.GetProfile(characterName);
            
            return new ColoredTextBuilder()
                .Add($"{characterName} ", profile.PrimaryColor)
                .Add("casts ", Colors.White)
                .AddWithPattern("Fireball", "fire")
                .Add(" for ", Colors.White)
                .AddWithPattern("30", "damage")
                .Add("!", Colors.White)
                .Build();
        }
        
        /// <summary>
        /// Example: Parsing markup text
        /// </summary>
        public static List<ColoredText> ParseMarkupText()
        {
            var markupText = "[damage]25[/damage] damage to [enemy]Goblin[/enemy]!";
            return ColoredTextParser.Parse(markupText);
        }
        
        /// <summary>
        /// Example: Parsing character-specific markup
        /// </summary>
        public static List<ColoredText> ParseCharacterMarkup()
        {
            var markupText = "[char:Player:primary]Hero[/char] attacks [char:Enemy:primary]Monster[/char]!";
            return ColoredTextParser.Parse(markupText);
        }
        
        /// <summary>
        /// Example: Creating a status message
        /// </summary>
        public static List<ColoredText> CreateStatusMessage()
        {
            return new ColoredTextBuilder()
                .Add("Status: ", ColorPalette.Info)
                .AddWithPattern("Healthy", "success")
                .Add(" | Health: ", Colors.White)
                .Add("100/100", ColorPalette.Healing)
                .Add(" | Level: ", Colors.White)
                .Add("5", ColorPalette.Info)
                .Build();
        }
        
        /// <summary>
        /// Example: Creating an item description
        /// </summary>
        public static List<ColoredText> CreateItemDescription()
        {
            return new ColoredTextBuilder()
                .AddWithPattern("Rare", "rare")
                .Add(" ")
                .AddWithPattern("Steel Sword", "weapon")
                .AddLine()
                .Add("Damage: ", Colors.White)
                .AddWithPattern("15-20", "damage")
                .AddLine()
                .Add("Durability: ", Colors.White)
                .Add("100/100", ColorPalette.Info)
                .Build();
        }
        
        /// <summary>
        /// Example: Rendering to different formats
        /// </summary>
        public static void RenderExamples()
        {
            var coloredText = CreateCombatMessage();
            
            // Render as plain text
            var plainText = ColoredTextRenderer.RenderAsPlainText(coloredText);
            Console.WriteLine($"Plain: {plainText}");
            
            // Render as HTML
            var html = ColoredTextRenderer.RenderAsHtml(coloredText);
            Console.WriteLine($"HTML: {html}");
            
            // Render as ANSI (for console)
            var ansi = ColoredTextRenderer.RenderAsAnsi(coloredText);
            Console.WriteLine($"ANSI: {ansi}");
            
            // Render as debug format
            var debug = ColoredTextRenderer.RenderAsDebug(coloredText);
            Console.WriteLine($"Debug: {debug}");
        }
        
        /// <summary>
        /// Example: Setting up character color profiles
        /// </summary>
        public static void SetupCharacterProfiles()
        {
            // Create a custom profile for the player
            var playerProfile = new CharacterColorProfile("Player");
            playerProfile.PrimaryColor = ColorPalette.Cyan;
            playerProfile.SecondaryColor = ColorPalette.Blue;
            playerProfile.AccentColor = ColorPalette.Gold;
            playerProfile.SetCustomPattern("spell", ColorPalette.Magenta);
            
            CharacterColorManager.SetProfile("Player", playerProfile);
            
            // Create a custom profile for enemies
            var enemyProfile = new CharacterColorProfile("Enemy");
            enemyProfile.PrimaryColor = ColorPalette.Red;
            enemyProfile.SecondaryColor = ColorPalette.DarkRed;
            enemyProfile.AccentColor = ColorPalette.Orange;
            enemyProfile.SetCustomPattern("roar", ColorPalette.DarkRed);
            
            CharacterColorManager.SetProfile("Enemy", enemyProfile);
        }
        
        /// <summary>
        /// Example: Using the system in practice
        /// </summary>
        public static List<ColoredText> CreatePracticalExample(string playerName, string enemyName, int damage)
        {
            // Set up character profiles if not already done
            SetupCharacterProfiles();
            
            // Create a combat message using character-specific colors
            return new ColoredTextBuilder()
                .Add($"{playerName} ", ColorPalette.Cyan)
                .Add("attacks ", Colors.White)
                .Add($"{enemyName} ", ColorPalette.Red)
                .Add("for ", Colors.White)
                .Add($"{damage}", ColorPalette.Damage)
                .Add(" damage!", Colors.White)
                .Build();
        }
    }
}
