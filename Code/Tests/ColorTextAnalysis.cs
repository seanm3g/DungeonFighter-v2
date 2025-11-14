using System;
using System.Linq;
using System.Text;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests
{
    /// <summary>
    /// Analyzes color text processing to identify character corruption issues
    /// </summary>
    public static class ColorTextAnalysis
    {
        public static void AnalyzeFullPipeline(string text, string templateName)
        {
            Console.WriteLine($"\n{'='} ANALYZING: '{text}' with template '{templateName}' {'='}");
            Console.WriteLine();
            
            // Step 1: Create the markup
            var coloredSegments = ColorParser.Colorize(text);
            string markup = string.Join("", coloredSegments.Select(s => s.Text));
            Console.WriteLine($"Step 1 - ColorParser.Colorize() produces:");
            Console.WriteLine($"  \"{markup}\"");
            Console.WriteLine($"  Length: {markup.Length} chars");
            ShowCharacterBreakdown(markup);
            Console.WriteLine();
            
            // Step 2: Parse the markup
            var segments = ColorParser.Parse(markup);
            Console.WriteLine($"Step 2 - ColorParser.Parse() produces {segments.Count} segments:");
            
            int totalChars = 0;
            for (int i = 0; i < segments.Count; i++)
            {
                var seg = segments[i];
                totalChars += seg.Text?.Length ?? 0;
                
                if (i < 20 || i >= segments.Count - 5) // Show first 20 and last 5
                {
                    string fgColor = seg.Foreground.HasValue ? 
                        $"RGB({seg.Foreground.Value.R},{seg.Foreground.Value.G},{seg.Foreground.Value.B})" : 
                        "none";
                    string segText = seg.Text?.Replace(" ", "·") ?? "null";
                    Console.WriteLine($"  [{i,3}] '{segText}' (len={seg.Text?.Length ?? 0}) fg={fgColor}");
                }
                else if (i == 20)
                {
                    Console.WriteLine($"  ... ({segments.Count - 25} more segments) ...");
                }
            }
            Console.WriteLine($"  Total characters in segments: {totalChars}");
            Console.WriteLine();
            
            // Step 3: Reconstruct the text
            string reconstructed = string.Concat(segments.Select(s => s.Text ?? ""));
            Console.WriteLine($"Step 3 - Reconstructed text:");
            Console.WriteLine($"  \"{reconstructed}\"");
            Console.WriteLine($"  Length: {reconstructed.Length} chars");
            Console.WriteLine();
            
            // Step 4: Compare
            Console.WriteLine($"Step 4 - Comparison:");
            Console.WriteLine($"  Original:      \"{text}\" ({text.Length} chars)");
            Console.WriteLine($"  Reconstructed: \"{reconstructed}\" ({reconstructed.Length} chars)");
            Console.WriteLine($"  Match: {text == reconstructed}");
            
            if (text != reconstructed)
            {
                Console.WriteLine($"\n  ❌ ERROR: Text mismatch!");
                Console.WriteLine($"  Character difference: {reconstructed.Length - text.Length}");
                ShowDifferences(text, reconstructed);
            }
            else
            {
                Console.WriteLine($"  ✓ Text preserved correctly");
            }
            
            // Step 5: Check for issues
            Console.WriteLine();
            Console.WriteLine($"Step 5 - Potential Issues:");
            
            var emptySegments = segments.Where(s => string.IsNullOrEmpty(s.Text)).ToList();
            if (emptySegments.Any())
            {
                Console.WriteLine($"  ⚠️  {emptySegments.Count} empty segments found");
            }
            
            var segmentsWithoutColor = segments.Where(s => !s.Foreground.HasValue && !s.Background.HasValue).ToList();
            if (segmentsWithoutColor.Any())
            {
                Console.WriteLine($"  ℹ️  {segmentsWithoutColor.Count} segments without color (whitespace?)");
            }
            
            Console.WriteLine();
        }
        
        private static void ShowCharacterBreakdown(string text)
        {
            Console.WriteLine($"  Character breakdown:");
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < Math.Min(50, text.Length); i++)
            {
                char c = text[i];
                if (c == ' ')
                    sb.Append("·");
                else if (c == '&')
                    sb.Append("[&]");
                else if (c == '^')
                    sb.Append("[^]");
                else if (c == '{')
                    sb.Append("[{]");
                else if (c == '}')
                    sb.Append("[}]");
                else
                    sb.Append(c);
            }
            if (text.Length > 50)
                sb.Append("...");
            Console.WriteLine($"  {sb}");
        }
        
        private static void ShowDifferences(string original, string reconstructed)
        {
            Console.WriteLine($"\n  Character-by-character comparison:");
            int maxLen = Math.Max(original.Length, reconstructed.Length);
            for (int i = 0; i < Math.Min(30, maxLen); i++)
            {
                char origChar = i < original.Length ? original[i] : ' ';
                char reconChar = i < reconstructed.Length ? reconstructed[i] : ' ';
                
                string origStr = origChar == ' ' ? "·" : origChar.ToString();
                string reconStr = reconChar == ' ' ? "·" : reconChar.ToString();
                
                if (origChar != reconChar)
                {
                    Console.WriteLine($"  [{i,2}] '{origStr}' -> '{reconStr}' ❌");
                }
            }
        }
        
        public static void RunTests()
        {
            Console.WriteLine("====================================");
            Console.WriteLine("COLOR TEXT ANALYSIS");
            Console.WriteLine("====================================");
            
            AnalyzeFullPipeline("Celestial Observatory", "astral");
            AnalyzeFullPipeline("Crystal Caverns", "crystal");
            AnalyzeFullPipeline("Ocean Depths", "ocean");
            AnalyzeFullPipeline("Test", "fiery");
        }
    }
}

