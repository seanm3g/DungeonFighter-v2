using System;
using System.Linq;
using RPGGame.UI;

namespace RPGGame.Tests
{
    /// <summary>
    /// Tests the undulate feature to verify color offset works correctly
    /// </summary>
    public static class UndulateEffectTest
    {
        public static void RunTests()
        {
            Console.WriteLine("====================================");
            Console.WriteLine("UNDULATE EFFECT TESTS");
            Console.WriteLine("====================================\n");
            
            TestBasicUndulation();
            TestUndulationAdvance();
            TestMultipleOffsets();
            TestWithDungeonNames();
            
            Console.WriteLine("\n====================================");
            Console.WriteLine("ALL TESTS COMPLETE");
            Console.WriteLine("====================================");
        }
        
        private static void TestBasicUndulation()
        {
            Console.WriteLine("TEST 1: Basic Undulation");
            Console.WriteLine("-------------------------");
            
            var text = ColoredText.FromTemplate("HELLO", "fiery", undulate: false);
            var segments = text.GetSegments();
            
            Console.WriteLine("Without undulate:");
            PrintSegments(segments);
            
            text.Undulate = true;
            text.UndulateOffset = 1;
            segments = text.GetSegments();
            
            Console.WriteLine("\nWith undulate (offset=1):");
            PrintSegments(segments);
            
            // Verify text integrity
            string reconstructed = string.Concat(segments.Select(s => s.Text));
            Console.WriteLine($"\nText integrity check: '{reconstructed}' == 'HELLO'? {reconstructed == "HELLO"}");
            Console.WriteLine();
        }
        
        private static void TestUndulationAdvance()
        {
            Console.WriteLine("TEST 2: Undulation Advance");
            Console.WriteLine("---------------------------");
            
            var text = ColoredText.FromTemplate("TEST", "electric", undulate: true);
            
            Console.WriteLine("Advancing undulation through 5 frames:\n");
            
            for (int frame = 0; frame < 5; frame++)
            {
                var segments = text.GetSegments();
                Console.WriteLine($"Frame {frame} (offset={text.UndulateOffset}):");
                PrintSegments(segments);
                Console.WriteLine();
                
                text.AdvanceUndulation();
            }
        }
        
        private static void TestMultipleOffsets()
        {
            Console.WriteLine("TEST 3: Multiple Offsets");
            Console.WriteLine("------------------------");
            
            string testText = "WAVE";
            string template = "ocean";
            
            Console.WriteLine($"Text: '{testText}', Template: '{template}'\n");
            
            for (int offset = 0; offset < 4; offset++)
            {
                var text = ColoredText.FromTemplate(testText, template, undulate: true);
                text.UndulateOffset = offset;
                
                var segments = text.GetSegments();
                Console.WriteLine($"Offset {offset}:");
                PrintSegments(segments);
                Console.WriteLine();
            }
        }
        
        private static void TestWithDungeonNames()
        {
            Console.WriteLine("TEST 4: Dungeon Names with Undulate");
            Console.WriteLine("------------------------------------");
            
            var dungeonTests = new[]
            {
                ("Celestial Observatory", "astral"),
                ("Crystal Caverns", "crystalline"),
                ("Ocean Depths", "ocean"),
                ("Mystical Garden", "natural")
            };
            
            foreach (var (name, template) in dungeonTests)
            {
                Console.WriteLine($"\nDungeon: {name}");
                Console.WriteLine($"Template: {template}");
                
                // Without undulate
                var text = ColoredText.FromTemplate(name, template, undulate: false);
                var segments = text.GetSegments();
                Console.WriteLine($"  Normal: {segments.Count} segments");
                
                // With undulate
                text = ColoredText.FromTemplate(name, template, undulate: true);
                segments = text.GetSegments();
                Console.WriteLine($"  Undulate: {segments.Count} segments");
                
                // Verify text integrity
                string reconstructed = string.Concat(segments.Select(s => s.Text));
                bool match = reconstructed == name;
                Console.WriteLine($"  Text integrity: {(match ? "✓ PASS" : "✗ FAIL")}");
                
                if (!match)
                {
                    Console.WriteLine($"    Expected: '{name}'");
                    Console.WriteLine($"    Got: '{reconstructed}'");
                }
            }
        }
        
        private static void PrintSegments(List<ColorDefinitions.ColoredSegment> segments)
        {
            foreach (var seg in segments)
            {
                string text = seg.Text?.Replace(" ", "·") ?? "";
                string color = seg.Foreground.HasValue ? 
                    $"RGB({seg.Foreground.Value.R,3},{seg.Foreground.Value.G,3},{seg.Foreground.Value.B,3})" : 
                    "none";
                Console.WriteLine($"  '{text}' → {color}");
            }
        }
        
        /// <summary>
        /// Visual demonstration showing how undulation creates a shimmer effect
        /// </summary>
        public static void VisualDemo()
        {
            Console.WriteLine("\n====================================");
            Console.WriteLine("VISUAL UNDULATION DEMONSTRATION");
            Console.WriteLine("====================================\n");
            
            string text = "SHIMMERING PORTAL";
            string template = "crystalline";
            
            Console.WriteLine($"Simulating 10 frames of undulation:");
            Console.WriteLine($"Text: '{text}'");
            Console.WriteLine($"Template: '{template}'");
            Console.WriteLine();
            
            var coloredText = ColoredText.FromTemplate(text, template, undulate: true);
            
            for (int frame = 0; frame < 10; frame++)
            {
                var segments = coloredText.GetSegments();
                
                // Build a visual representation
                Console.Write($"Frame {frame,2}: ");
                foreach (var seg in segments)
                {
                    if (seg.Foreground.HasValue)
                    {
                        // Show color code approximation
                        char colorChar = GetColorChar(seg.Foreground.Value);
                        Console.Write(colorChar);
                    }
                    else
                    {
                        Console.Write(" ");
                    }
                }
                Console.WriteLine($"  (offset={coloredText.UndulateOffset})");
                
                coloredText.AdvanceUndulation();
            }
            
            Console.WriteLine("\nNotice how the color pattern shifts across the text!");
        }
        
        private static char GetColorChar(ColorDefinitions.ColorRGB color)
        {
            // Map colors to characters for visualization
            if (color.R > 200 && color.G < 100 && color.B < 100) return 'R';
            if (color.R > 200 && color.G > 150) return 'Y';
            if (color.G > 150 && color.R < 100 && color.B < 100) return 'G';
            if (color.B > 150 && color.R < 100 && color.G < 100) return 'B';
            if (color.R > 150 && color.G < 100 && color.B > 150) return 'M';
            if (color.G > 150 && color.B > 150 && color.R < 100) return 'C';
            if (color.R > 200) return 'O';
            return 'W';
        }
    }
}

