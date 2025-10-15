using System;
using System.Text;

namespace RPGGame.UI
{
    /// <summary>
    /// Helper for debugging color text issues
    /// </summary>
    public static class ColorDebugHelper
    {
        public static void DebugSegments(string originalText)
        {
            Console.WriteLine($"\n=== DEBUG: Original Text ===");
            Console.WriteLine($"'{originalText}'");
            Console.WriteLine($"Length: {originalText.Length}");
            
            // Show each character with its ASCII code
            Console.WriteLine("\nCharacter breakdown:");
            for (int i = 0; i < originalText.Length; i++)
            {
                char c = originalText[i];
                if (c == ' ')
                {
                    Console.WriteLine($"  [{i}] = SPACE (32)");
                }
                else if (c == '\t')
                {
                    Console.WriteLine($"  [{i}] = TAB (9)");
                }
                else if (c == '&' || c == '^')
                {
                    Console.WriteLine($"  [{i}] = '{c}' (COLOR MARKER)");
                }
                else
                {
                    Console.WriteLine($"  [{i}] = '{c}' ({(int)c})");
                }
            }
            
            // Parse and show segments
            var segments = ColorParser.Parse(originalText);
            Console.WriteLine($"\n=== Parsed Segments: {segments.Count} ===");
            for (int i = 0; i < segments.Count; i++)
            {
                var seg = segments[i];
                Console.WriteLine($"Segment {i}:");
                Console.WriteLine($"  Text: '{seg.Text}' (length: {seg.Text.Length})");
                Console.WriteLine($"  Has Foreground: {seg.Foreground.HasValue}");
                if (seg.Foreground.HasValue)
                {
                    Console.WriteLine($"  Color: R={seg.Foreground.Value.R}, G={seg.Foreground.Value.G}, B={seg.Foreground.Value.B}");
                }
                
                // Show characters in this segment
                for (int j = 0; j < seg.Text.Length; j++)
                {
                    char c = seg.Text[j];
                    if (c == ' ')
                    {
                        Console.WriteLine($"    [{j}] = SPACE");
                    }
                    else
                    {
                        Console.WriteLine($"    [{j}] = '{c}'");
                    }
                }
            }
            
            // Reconstruct text
            var reconstructed = new StringBuilder();
            foreach (var seg in segments)
            {
                reconstructed.Append(seg.Text);
            }
            Console.WriteLine($"\n=== Reconstructed ===");
            Console.WriteLine($"'{reconstructed}'");
            Console.WriteLine($"Length: {reconstructed.Length}");
            Console.WriteLine($"Original length: {ColorParser.StripColorMarkup(originalText).Length}");
        }
    }
}

