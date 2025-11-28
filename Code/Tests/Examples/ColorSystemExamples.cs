using System;
using System.Collections.Generic;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;
using RPGGame.Utils;

namespace RPGGame.UI
{
    /// <summary>
    /// Examples and test cases for the Caves of Qud-inspired color system
    /// </summary>
    public static class ColorSystemExamples
    {
        /// <summary>
        /// Demonstrates basic color codes
        /// </summary>
        public static void DemoBasicColors()
        {
            Console.WriteLine("\n=== BASIC COLOR CODES ===\n");
            
            ColoredConsoleWriter.WriteLine("&RRed text");
            ColoredConsoleWriter.WriteLine("&GGreen text");
            ColoredConsoleWriter.WriteLine("&BBlue text");
            ColoredConsoleWriter.WriteLine("&WYellow/Gold text");
            ColoredConsoleWriter.WriteLine("&MMagenta text");
            ColoredConsoleWriter.WriteLine("&CCyan text");
            ColoredConsoleWriter.WriteLine("&OOrange text");
            ColoredConsoleWriter.WriteLine("&YWhite text");
            ColoredConsoleWriter.WriteLine("&yGrey text (default)");
            
            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates dark color variants
        /// </summary>
        public static void DemoDarkColors()
        {
            Console.WriteLine("\n=== DARK COLOR VARIANTS ===\n");
            
            ColoredConsoleWriter.WriteLine("&rDark red / crimson");
            ColoredConsoleWriter.WriteLine("&gDark green");
            ColoredConsoleWriter.WriteLine("&bDark blue");
            ColoredConsoleWriter.WriteLine("&oDark orange");
            ColoredConsoleWriter.WriteLine("&wBrown");
            ColoredConsoleWriter.WriteLine("&cDark cyan / teal");
            ColoredConsoleWriter.WriteLine("&mDark magenta / purple");
            ColoredConsoleWriter.WriteLine("&KDark grey / black");
            
            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates color templates
        /// </summary>
        public static void DemoTemplates()
        {
            Console.WriteLine("\n=== COLOR TEMPLATES ===\n");
            
            ColoredConsoleWriter.WriteLine("{{fiery|Blazing Sword of Fire}}");
            ColoredConsoleWriter.WriteLine("{{icy|Frozen Staff of Winter}}");
            ColoredConsoleWriter.WriteLine("{{toxic|Venomous Dagger}}");
            ColoredConsoleWriter.WriteLine("{{crystalline|Prism Shield}}");
            ColoredConsoleWriter.WriteLine("{{electric|Lightning Bolt}}");
            ColoredConsoleWriter.WriteLine("{{holy|Divine Blessing}}");
            ColoredConsoleWriter.WriteLine("{{demonic|Hellfire Curse}}");
            ColoredConsoleWriter.WriteLine("{{arcane|Mystic Runes}}");
            ColoredConsoleWriter.WriteLine("{{natural|Forest Guardian}}");
            ColoredConsoleWriter.WriteLine("{{shadow|Dark Stalker}}");
            ColoredConsoleWriter.WriteLine("{{golden|Golden Crown}}");
            ColoredConsoleWriter.WriteLine("{{bloodied|Bloodied Warrior}}");
            ColoredConsoleWriter.WriteLine("{{ethereal|Ethereal Spirit}}");
            ColoredConsoleWriter.WriteLine("{{corrupted|Corrupted Soul}}");
            ColoredConsoleWriter.WriteLine("{{rainbow|Rainbow Bridge}}");
            
            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates item rarity colors
        /// </summary>
        public static void DemoItemRarities()
        {
            Console.WriteLine("\n=== ITEM RARITY COLORS ===\n");
            
            ColoredConsoleWriter.WriteLine("{{common|Common Iron Sword}}");
            ColoredConsoleWriter.WriteLine("{{uncommon|Uncommon Steel Axe}}");
            ColoredConsoleWriter.WriteLine("{{rare|Rare Mithril Armor}}");
            ColoredConsoleWriter.WriteLine("{{epic|Epic Dragon Slayer}}");
            ColoredConsoleWriter.WriteLine("{{legendary|Legendary Excalibur}}");
            
            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates combat message coloring
        /// </summary>
        public static void DemoCombatMessages()
        {
            Console.WriteLine("\n=== COMBAT MESSAGES ===\n");
            
            ColoredConsoleWriter.WriteLine("You attack the {{red|Goblin}} for &R25&y damage!");
            ColoredConsoleWriter.WriteLine("{{critical|CRITICAL HIT}}! You deal &R{{damage|55}}&y damage!");
            ColoredConsoleWriter.WriteLine("You {{heal|heal}} for &G{{heal|15}}&y health.");
            ColoredConsoleWriter.WriteLine("Enemy {{miss|MISSES}} the attack!");
            ColoredConsoleWriter.WriteLine("You are {{poisoned|POISONED}} and take &g5&y damage per turn.");
            ColoredConsoleWriter.WriteLine("Enemy is {{stunned|STUNNED}} for 2 turns!");
            ColoredConsoleWriter.WriteLine("You are {{burning|BURNING}} - take &R3&y fire damage!");
            ColoredConsoleWriter.WriteLine("Enemy is {{frozen|FROZEN}} solid!");
            ColoredConsoleWriter.WriteLine("You are {{bleeding|BLEEDING}} - &r-2 HP&y per turn.");
            
            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates mixing colors and templates
        /// </summary>
        public static void DemoMixedFormatting()
        {
            Console.WriteLine("\n=== MIXED FORMATTING ===\n");
            
            ColoredConsoleWriter.WriteLine("You found a {{legendary|Legendary}} {{fiery|Flaming Sword}}!");
            ColoredConsoleWriter.WriteLine("&RDamage: &W+50&y | &BSpeed: &W+10&y");
            ColoredConsoleWriter.WriteLine("{{holy|Divine Light}} &Ybanishes&y the {{demonic|Demon Lord}}!");
            ColoredConsoleWriter.WriteLine("The {{icy|Frozen Cavern}} is filled with {{crystalline|ice crystals}}.");
            ColoredConsoleWriter.WriteLine("&WGold:&y {{golden|1,234}} | &BLevel:&y &C15");
            
            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates status effect coloring
        /// </summary>
        public static void DemoStatusEffects()
        {
            Console.WriteLine("\n=== STATUS EFFECTS ===\n");
            
            ColoredConsoleWriter.WriteLine("[ {{poisoned|POISONED}} ] &g-5 HP per turn");
            ColoredConsoleWriter.WriteLine("[ {{stunned|STUNNED}} ] &YCannot act");
            ColoredConsoleWriter.WriteLine("[ {{burning|BURNING}} ] &R-3 HP per turn");
            ColoredConsoleWriter.WriteLine("[ {{frozen|FROZEN}} ] &CMovement reduced");
            ColoredConsoleWriter.WriteLine("[ {{bleeding|BLEEDING}} ] &r-2 HP per turn");
            
            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates dungeon/environment descriptions
        /// </summary>
        public static void DemoDungeonDescriptions()
        {
            Console.WriteLine("\n=== DUNGEON DESCRIPTIONS ===\n");
            
            ColoredConsoleWriter.WriteLine("You enter the {{natural|Forest}} dungeon...");
            ColoredConsoleWriter.WriteLine("{{fiery|Lava}} bubbles and hisses around you.");
            ColoredConsoleWriter.WriteLine("The {{shadow|Crypt}} is filled with {{ethereal|ghostly apparitions}}.");
            ColoredConsoleWriter.WriteLine("{{icy|Frozen}} stalactites hang from the ceiling.");
            ColoredConsoleWriter.WriteLine("The {{toxic|Swamp}} reeks of {{corrupted|decay}}.");
            ColoredConsoleWriter.WriteLine("{{holy|Sacred}} runes glow on the temple walls.");
            ColoredConsoleWriter.WriteLine("{{arcane|Magical}} energy crackles in the air.");
            
            Console.WriteLine();
        }

        /// <summary>
        /// Runs all demonstrations
        /// </summary>
        public static void RunAllDemos()
        {
            Console.Clear();
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║   CAVES OF QUD-INSPIRED COLOR SYSTEM DEMONSTRATION        ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
            
            DemoBasicColors();
            DemoDarkColors();
            DemoTemplates();
            DemoItemRarities();
            DemoCombatMessages();
            DemoMixedFormatting();
            DemoStatusEffects();
            DemoDungeonDescriptions();
            
            Console.WriteLine("\n=== UTILITY FUNCTIONS ===\n");
            
            string markup = "{{fiery|Blazing}} Sword of Ice";
            Console.WriteLine($"Original: {markup}");
            var segments = ColoredTextParser.Parse(markup);
            Console.WriteLine($"Stripped: {ColoredTextRenderer.RenderAsPlainText(segments)}");
            Console.WriteLine($"Display Length: {ColoredTextRenderer.GetDisplayLength(segments)}");
            bool hasMarkup = markup.Contains("{{") && markup.Contains("}}") || (markup.Contains("[") && markup.Contains("]"));
            Console.WriteLine($"Has Markup: {hasMarkup}");
            
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        /// <summary>
        /// Tests parsing and rendering
        /// </summary>
        public static void TestParsing()
        {
            Console.WriteLine("\n=== PARSING TESTS ===\n");
            
            var testCases = new List<string>
            {
                "&RRed",
                "&R^gRed on green",
                "{{fiery|Fire}}",
                "Normal &Rred&y normal",
                "{{legendary|Item}} with &R25&y damage",
                "No markup here",
                ""
            };
            
            foreach (var test in testCases)
            {
                Console.WriteLine($"Input: '{test}'");
                var segments = ColoredTextParser.Parse(test);
                Console.WriteLine($"  Segments: {segments.Count}");
                
                foreach (var segment in segments)
                {
                    Console.WriteLine($"    Text: '{segment.Text}', Color: RGB({segment.Color.R},{segment.Color.G},{segment.Color.B})");
                }
                
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Performance test for color parsing
        /// </summary>
        public static void TestPerformance()
        {
            Console.WriteLine("\n=== PERFORMANCE TEST ===\n");
            
            var testStrings = new List<string>
            {
                "Simple text",
                "&RSimple color",
                "{{fiery|Template text}}",
                "Mixed &R{{legendary|complex}}&y markup {{icy|with}} multiple &Gcolors&y"
            };
            
            int iterations = 10000;
            
            foreach (var testString in testStrings)
            {
                var startTime = DateTime.Now;
                
                for (int i = 0; i < iterations; i++)
                {
                    ColoredTextParser.Parse(testString);
                }
                
                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                Console.WriteLine($"Test: '{testString}'");
                Console.WriteLine($"  Time: {elapsed:F2}ms for {iterations} iterations");
                Console.WriteLine($"  Average: {elapsed / iterations:F4}ms per parse");
                Console.WriteLine();
            }
        }
    }
}

