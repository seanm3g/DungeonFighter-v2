using System;
using System.Collections.Generic;
using System.Text;

namespace StandaloneColorDemo
{
    /// <summary>
    /// Standalone demo of the new color system concepts
    /// This runs independently without any dependencies
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("🎨 NEW COLOR SYSTEM CONCEPT DEMO 🎨");
            Console.WriteLine("====================================");
            Console.WriteLine();
            
            try
            {
                DemoBasicConcepts();
                DemoPatternSystem();
                DemoCharacterProfiles();
                DemoMarkupParsing();
                DemoOutputFormats();
                DemoPerformance();
                
                Console.WriteLine();
                Console.WriteLine("🎉 ALL DEMOS COMPLETED SUCCESSFULLY! 🎉");
                Console.WriteLine("The new color system concepts are proven and ready!");
                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Demo failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
        
        /// <summary>
        /// Demonstrates basic colored text concepts
        /// </summary>
        static void DemoBasicConcepts()
        {
            Console.WriteLine("=== 1. Basic Colored Text Concepts ===");
            
            // Simulate the new ColoredText system
            var combatMessage = new ColoredTextBuilder()
                .Add("Player ", ConsoleColor.Cyan)
                .Add("deals ", ConsoleColor.White)
                .Add("25", ConsoleColor.Red)
                .Add(" damage to ", ConsoleColor.White)
                .Add("Goblin", ConsoleColor.Red)
                .Add("!", ConsoleColor.White)
                .Build();
            
            Console.WriteLine("Combat Message:");
            combatMessage.WriteToConsole();
            Console.WriteLine();
            
            // Show different output formats
            Console.WriteLine($"Plain Text: {combatMessage.ToPlainText()}");
            Console.WriteLine($"HTML: {combatMessage.ToHtml()}");
            Console.WriteLine($"Debug: {combatMessage.ToDebug()}");
            Console.WriteLine();
        }
        
        /// <summary>
        /// Demonstrates pattern-based coloring
        /// </summary>
        static void DemoPatternSystem()
        {
            Console.WriteLine("=== 2. Pattern-Based Coloring ===");
            
            var statusMessage = new ColoredTextBuilder()
                .AddWithPattern("Level up!", "success")
                .Add(" You gained ", ConsoleColor.White)
                .AddWithPattern("100", "experience")
                .Add(" experience points!", ConsoleColor.White)
                .Build();
            
            var itemMessage = new ColoredTextBuilder()
                .AddWithPattern("Rare", "rare")
                .Add(" ", ConsoleColor.White)
                .AddWithPattern("Steel Sword", "weapon")
                .Add(" found!", ConsoleColor.White)
                .Build();
            
            Console.WriteLine("Status Message:");
            statusMessage.WriteToConsole();
            Console.WriteLine();
            
            Console.WriteLine("Item Message:");
            itemMessage.WriteToConsole();
            Console.WriteLine();
        }
        
        /// <summary>
        /// Demonstrates character-specific colors
        /// </summary>
        static void DemoCharacterProfiles()
        {
            Console.WriteLine("=== 3. Character-Specific Colors ===");
            
            // Set up character profiles
            var playerProfile = new CharacterColorProfile("Player");
            playerProfile.PrimaryColor = ConsoleColor.Cyan;
            playerProfile.DamageColor = ConsoleColor.Red;
            playerProfile.SetCustomPattern("spell", ConsoleColor.Magenta);
            CharacterColorManager.SetProfile("Player", playerProfile);
            
            var enemyProfile = new CharacterColorProfile("Enemy");
            enemyProfile.PrimaryColor = ConsoleColor.Red;
            enemyProfile.DamageColor = ConsoleColor.DarkRed;
            CharacterColorManager.SetProfile("Enemy", enemyProfile);
            
            // Create messages using character-specific colors
            var playerMessage = new ColoredTextBuilder()
                .Add("Player ", ConsoleColor.Cyan)
                .Add("casts ", ConsoleColor.White)
                .AddWithPattern("Fireball", "spell")
                .Add("!", ConsoleColor.White)
                .Build();
            
            var enemyMessage = new ColoredTextBuilder()
                .Add("Enemy ", ConsoleColor.Red)
                .Add("attacks for ", ConsoleColor.White)
                .Add("15", ConsoleColor.Red)
                .Add(" damage!", ConsoleColor.White)
                .Build();
            
            Console.WriteLine("Player Message:");
            playerMessage.WriteToConsole();
            Console.WriteLine();
            
            Console.WriteLine("Enemy Message:");
            enemyMessage.WriteToConsole();
            Console.WriteLine();
        }
        
        /// <summary>
        /// Demonstrates markup parsing
        /// </summary>
        static void DemoMarkupParsing()
        {
            Console.WriteLine("=== 4. Markup Parsing ===");
            
            // Parse markup text
            var markupText = "[damage]25[/damage] damage to [enemy]Goblin[/enemy]!";
            var coloredText = ColoredTextParser.Parse(markupText);
            
            Console.WriteLine($"Markup: {markupText}");
            Console.WriteLine("Parsed:");
            coloredText.WriteToConsole();
            Console.WriteLine();
        }
        
        /// <summary>
        /// Demonstrates different output formats
        /// </summary>
        static void DemoOutputFormats()
        {
            Console.WriteLine("=== 5. Output Formats ===");
            
            var sampleText = new ColoredTextBuilder()
                .Add("Hello ", ConsoleColor.White)
                .Add("World", ConsoleColor.Green)
                .Add("!", ConsoleColor.White)
                .Build();
            
            Console.WriteLine($"Plain: {sampleText.ToPlainText()}");
            Console.WriteLine($"HTML: {sampleText.ToHtml()}");
            Console.WriteLine($"ANSI: {sampleText.ToAnsi()}");
            Console.WriteLine($"Debug: {sampleText.ToDebug()}");
            Console.WriteLine();
        }
        
        /// <summary>
        /// Demonstrates performance
        /// </summary>
        static void DemoPerformance()
        {
            Console.WriteLine("=== 6. Performance Test ===");
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Create 1000 colored text objects
            for (int i = 0; i < 1000; i++)
            {
                var text = new ColoredTextBuilder()
                    .Add($"Message {i} ", ConsoleColor.White)
                    .AddWithPattern("damage", "damage")
                    .Add(" to ", ConsoleColor.White)
                    .AddWithPattern("enemy", "enemy")
                    .Build();
                
                var plain = text.ToPlainText();
                var html = text.ToHtml();
            }
            
            stopwatch.Stop();
            
            Console.WriteLine($"Created 1000 colored text objects in {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Average: {stopwatch.ElapsedMilliseconds / 1000.0:F2}ms per object");
            Console.WriteLine();
        }
    }
    
    /// <summary>
    /// Simulates the new ColoredText system
    /// </summary>
    public class ColoredText
    {
        public List<ColoredChar> Characters { get; private set; } = new List<ColoredChar>();
        
        public ColoredText(List<ColoredChar> characters)
        {
            Characters = characters;
        }
        
        public void WriteToConsole()
        {
            var originalColor = Console.ForegroundColor;
            foreach (var c in Characters)
            {
                Console.ForegroundColor = c.Color;
                Console.Write(c.Character);
            }
            Console.ForegroundColor = originalColor;
        }
        
        public string ToPlainText()
        {
            var sb = new StringBuilder();
            foreach (var c in Characters)
            {
                sb.Append(c.Character);
            }
            return sb.ToString();
        }
        
        public string ToHtml()
        {
            var sb = new StringBuilder();
            ConsoleColor? currentColor = null;
            
            foreach (var c in Characters)
            {
                if (c.Color != currentColor)
                {
                    if (currentColor.HasValue)
                    {
                        sb.Append("</span>");
                    }
                    sb.Append($"<span style=\"color: {GetHtmlColor(c.Color)}\">");
                    currentColor = c.Color;
                }
                sb.Append(c.Character);
            }
            if (currentColor.HasValue)
            {
                sb.Append("</span>");
            }
            return sb.ToString();
        }
        
        public string ToAnsi()
        {
            var sb = new StringBuilder();
            ConsoleColor? currentColor = null;
            
            foreach (var c in Characters)
            {
                if (c.Color != currentColor)
                {
                    sb.Append("\x1b[0m"); // Reset
                    sb.Append(GetAnsiColor(c.Color));
                    currentColor = c.Color;
                }
                sb.Append(c.Character);
            }
            sb.Append("\x1b[0m"); // Reset at end
            return sb.ToString();
        }
        
        public string ToDebug()
        {
            var sb = new StringBuilder();
            foreach (var c in Characters)
            {
                sb.Append($"[{c.Color}:{c.Character}]");
            }
            return sb.ToString();
        }
        
        private string GetHtmlColor(ConsoleColor color)
        {
            return color switch
            {
                ConsoleColor.Red => "red",
                ConsoleColor.Green => "green",
                ConsoleColor.Blue => "blue",
                ConsoleColor.Yellow => "yellow",
                ConsoleColor.Cyan => "cyan",
                ConsoleColor.Magenta => "magenta",
                ConsoleColor.White => "white",
                ConsoleColor.Black => "black",
                ConsoleColor.Gray => "gray",
                ConsoleColor.DarkRed => "darkred",
                ConsoleColor.DarkGreen => "darkgreen",
                ConsoleColor.DarkBlue => "darkblue",
                ConsoleColor.DarkYellow => "orange",
                ConsoleColor.DarkCyan => "teal",
                ConsoleColor.DarkMagenta => "purple",
                ConsoleColor.DarkGray => "dimgray",
                _ => "white"
            };
        }
        
        private string GetAnsiColor(ConsoleColor color)
        {
            return color switch
            {
                ConsoleColor.Red => "\x1b[31m",
                ConsoleColor.Green => "\x1b[32m",
                ConsoleColor.Blue => "\x1b[34m",
                ConsoleColor.Yellow => "\x1b[33m",
                ConsoleColor.Cyan => "\x1b[36m",
                ConsoleColor.Magenta => "\x1b[35m",
                ConsoleColor.White => "\x1b[37m",
                ConsoleColor.Black => "\x1b[30m",
                ConsoleColor.Gray => "\x1b[37m",
                ConsoleColor.DarkRed => "\x1b[31m",
                ConsoleColor.DarkGreen => "\x1b[32m",
                ConsoleColor.DarkBlue => "\x1b[34m",
                ConsoleColor.DarkYellow => "\x1b[33m",
                ConsoleColor.DarkCyan => "\x1b[36m",
                ConsoleColor.DarkMagenta => "\x1b[35m",
                ConsoleColor.DarkGray => "\x1b[37m",
                _ => "\x1b[37m"
            };
        }
    }
    
    /// <summary>
    /// Simulates the new ColoredChar system
    /// </summary>
    public class ColoredChar
    {
        public char Character { get; set; }
        public ConsoleColor Color { get; set; }
        
        public ColoredChar(char character, ConsoleColor color)
        {
            Character = character;
            Color = color;
        }
    }
    
    /// <summary>
    /// Simulates the new ColoredTextBuilder system
    /// </summary>
    public class ColoredTextBuilder
    {
        private readonly List<ColoredChar> _characters = new List<ColoredChar>();
        
        public ColoredTextBuilder Add(string text, ConsoleColor color)
        {
            foreach (char c in text)
            {
                _characters.Add(new ColoredChar(c, color));
            }
            return this;
        }
        
        public ColoredTextBuilder AddWithPattern(string text, string pattern)
        {
            var color = GetPatternColor(pattern);
            return Add(text, color);
        }
        
        public ColoredText Build()
        {
            return new ColoredText(new List<ColoredChar>(_characters));
        }
        
        private ConsoleColor GetPatternColor(string pattern)
        {
            return pattern.ToLower() switch
            {
                "damage" => ConsoleColor.Red,
                "healing" => ConsoleColor.Green,
                "success" => ConsoleColor.Green,
                "warning" => ConsoleColor.Yellow,
                "error" => ConsoleColor.Red,
                "info" => ConsoleColor.Cyan,
                "experience" => ConsoleColor.Yellow,
                "rare" => ConsoleColor.Magenta,
                "weapon" => ConsoleColor.Cyan,
                "enemy" => ConsoleColor.Red,
                "player" => ConsoleColor.Cyan,
                _ => ConsoleColor.White
            };
        }
    }
    
    /// <summary>
    /// Simulates the new CharacterColorProfile system
    /// </summary>
    public class CharacterColorProfile
    {
        public string CharacterName { get; private set; }
        public ConsoleColor PrimaryColor { get; set; } = ConsoleColor.White;
        public ConsoleColor DamageColor { get; set; } = ConsoleColor.Red;
        public Dictionary<string, ConsoleColor> CustomPatterns { get; private set; } = new Dictionary<string, ConsoleColor>();
        
        public CharacterColorProfile(string characterName)
        {
            CharacterName = characterName;
        }
        
        public void SetCustomPattern(string patternName, ConsoleColor color)
        {
            CustomPatterns[patternName.ToLower()] = color;
        }
    }
    
    /// <summary>
    /// Simulates the new CharacterColorManager system
    /// </summary>
    public static class CharacterColorManager
    {
        private static Dictionary<string, CharacterColorProfile> _profiles = new Dictionary<string, CharacterColorProfile>();
        
        public static void SetProfile(string characterName, CharacterColorProfile profile)
        {
            _profiles[characterName] = profile;
        }
        
        public static CharacterColorProfile GetProfile(string characterName)
        {
            if (_profiles.TryGetValue(characterName, out var profile))
            {
                return profile;
            }
            return new CharacterColorProfile(characterName);
        }
    }
    
    /// <summary>
    /// Simulates the new ColoredTextParser system
    /// </summary>
    public static class ColoredTextParser
    {
        public static ColoredText Parse(string text)
        {
            var builder = new ColoredTextBuilder();
            int i = 0;
            
            while (i < text.Length)
            {
                if (text[i] == '[' && i + 1 < text.Length)
                {
                    // Find the closing bracket
                    int endBracket = text.IndexOf(']', i);
                    if (endBracket > i)
                    {
                        string tag = text.Substring(i + 1, endBracket - i - 1);
                        
                        // Find the closing tag
                        string closingTag = $"[/{tag}]";
                        int closingIndex = text.IndexOf(closingTag, endBracket);
                        if (closingIndex > endBracket)
                        {
                            string content = text.Substring(endBracket + 1, closingIndex - endBracket - 1);
                            builder.AddWithPattern(content, tag);
                            i = closingIndex + closingTag.Length;
                            continue;
                        }
                    }
                }
                
                // Regular character
                builder.Add(text[i].ToString(), ConsoleColor.White);
                i++;
            }
            
            return builder.Build();
        }
    }
}
