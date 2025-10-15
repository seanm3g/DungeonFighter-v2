using System;
using System.Text;

namespace RPGGame.UI
{
    /// <summary>
    /// Debugging tool for diagnosing color markup spacing issues
    /// </summary>
    public static class ColorDebugTool
    {
        /// <summary>
        /// Tests a message through the entire color pipeline and shows what happens at each step
        /// </summary>
        public static void DebugMessage(string message)
        {
            Console.WriteLine("=== COLOR DEBUG TOOL ===");
            Console.WriteLine();
            Console.WriteLine($"INPUT: '{message}'");
            Console.WriteLine($"LENGTH: {message.Length}");
            Console.WriteLine();
            
            // Step 1: Apply keyword coloring
            string afterKeywords = KeywordColorSystem.Colorize(message);
            Console.WriteLine("AFTER KEYWORD COLORING:");
            Console.WriteLine($"'{afterKeywords}'");
            Console.WriteLine($"LENGTH: {afterKeywords.Length}");
            Console.WriteLine($"HAS MARKUP: {ColorParser.HasColorMarkup(afterKeywords)}");
            Console.WriteLine();
            
            // Step 2: Parse into segments
            var segments = ColorParser.Parse(afterKeywords);
            Console.WriteLine($"SEGMENTS: {segments.Count}");
            foreach (var seg in segments)
            {
                Console.WriteLine($"  - Text: '{seg.Text}' (len={seg.Text.Length}), " +
                    $"FG: {(seg.Foreground.HasValue ? "Yes" : "No")}, " +
                    $"BG: {(seg.Background.HasValue ? "Yes" : "No")}");
            }
            Console.WriteLine();
            
            // Step 3: Reconstruct text from segments
            var reconstructed = new StringBuilder();
            foreach (var seg in segments)
            {
                reconstructed.Append(seg.Text);
            }
            Console.WriteLine("RECONSTRUCTED TEXT:");
            Console.WriteLine($"'{reconstructed}'");
            Console.WriteLine($"LENGTH: {reconstructed.Length}");
            Console.WriteLine();
            
            // Step 4: Strip markup
            string stripped = ColorParser.StripColorMarkup(afterKeywords);
            Console.WriteLine("STRIPPED TEXT:");
            Console.WriteLine($"'{stripped}'");
            Console.WriteLine($"LENGTH: {stripped.Length}");
            Console.WriteLine();
            
            // Step 5: Compare
            if (message.Length == stripped.Length && message == stripped)
            {
                Console.WriteLine("✓ PASS: Text preserved correctly");
            }
            else if (message.Length == stripped.Length)
            {
                Console.WriteLine("⚠ WARNING: Same length but different text");
                Console.WriteLine($"  Original: '{message}'");
                Console.WriteLine($"  Stripped: '{stripped}'");
            }
            else
            {
                Console.WriteLine("✗ FAIL: Length changed!");
                Console.WriteLine($"  Original length: {message.Length}");
                Console.WriteLine($"  Stripped length: {stripped.Length}");
                Console.WriteLine($"  Difference: {stripped.Length - message.Length} characters");
                
                // Show where the difference is
                int minLen = Math.Min(message.Length, stripped.Length);
                for (int i = 0; i < minLen; i++)
                {
                    if (message[i] != stripped[i])
                    {
                        Console.WriteLine($"  First difference at index {i}:");
                        Console.WriteLine($"    Original: '{message[i]}' (char code: {(int)message[i]})");
                        Console.WriteLine($"    Stripped: '{stripped[i]}' (char code: {(int)stripped[i]})");
                        break;
                    }
                }
            }
            Console.WriteLine();
            Console.WriteLine("===================");
        }
        
        /// <summary>
        /// Tests multiple messages commonly seen in combat
        /// </summary>
        public static void RunCombatMessageTests()
        {
            Console.WriteLine("=== COMBAT MESSAGE TESTS ===");
            Console.WriteLine();
            
            var testMessages = new[]
            {
                "Nolan Swiftarrow hits Nature Spirit for 2 damage",
                "(roll: 9 | attack 4 - 2 armor | speed: 8.5s)",
                "Nature Spirit hits Nolan Swiftarrow for 2 damage",
                "Nolan Swiftarrow CRITICAL MISS on Nature Spirit",
                "Nature Spirit misses Nolan Swiftarrow"
            };
            
            foreach (var msg in testMessages)
            {
                DebugMessage(msg);
                Console.WriteLine();
            }
        }
    }
}

