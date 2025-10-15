using System;
using System.Linq;
using RPGGame.UI;

namespace RPGGame.Tests
{
    /// <summary>
    /// Diagnostic utility to debug color template application issues
    /// </summary>
    public static class ColorSystemDiagnostic
    {
        public static void DiagnoseDungeonNameColoring(string dungeonName, string theme)
        {
            Console.WriteLine($"\n=== Diagnosing: '{dungeonName}' with theme '{theme}' ===\n");
            
            // Step 1: Get template name
            string templateName = theme.ToLower();
            Console.WriteLine($"1. Template name: '{templateName}'");
            
            // Step 2: Check if template exists
            bool templateExists = ColorTemplateLibrary.HasTemplate(templateName);
            Console.WriteLine($"2. Template exists: {templateExists}");
            
            if (!templateExists)
            {
                Console.WriteLine($"   -> Using fallback color code 'y'");
                templateName = "y";
            }
            
            // Step 3: Create markup
            string markup = ColorParser.Colorize(dungeonName, templateName);
            Console.WriteLine($"3. Created markup: '{markup}'");
            
            // Step 4: Show what template expansion produces
            if (ColorTemplateLibrary.HasTemplate(templateName))
            {
                var template = ColorTemplateLibrary.GetTemplate(templateName);
                if (template != null)
                {
                    Console.WriteLine($"4. Template: {template.Name}, Type: {template.ShaderType}, Colors: [{string.Join(", ", template.ColorSequence)}]");
                    
                    var segments = template.Apply(dungeonName);
                    Console.WriteLine($"5. Applied template creates {segments.Count} segments:");
                    
                    for (int i = 0; i < Math.Min(10, segments.Count); i++)
                    {
                        var seg = segments[i];
                        string fg = seg.Foreground.HasValue ? 
                            $"RGB({seg.Foreground.Value.R},{seg.Foreground.Value.G},{seg.Foreground.Value.B})" : 
                            "null";
                        string text = seg.Text?.Replace(" ", "·") ?? "null";
                        Console.WriteLine($"   [{i}] Text='{text}' Foreground={fg}");
                    }
                    
                    if (segments.Count > 10)
                    {
                        Console.WriteLine($"   ... and {segments.Count - 10} more segments");
                    }
                }
            }
            
            // Step 5: Parse the markup
            var parsedSegments = ColorParser.Parse(markup);
            Console.WriteLine($"6. Parsed markup creates {parsedSegments.Count} segments:");
            
            for (int i = 0; i < Math.Min(10, parsedSegments.Count); i++)
            {
                var seg = parsedSegments[i];
                string fg = seg.Foreground.HasValue ? 
                    $"RGB({seg.Foreground.Value.R},{seg.Foreground.Value.G},{seg.Foreground.Value.B})" : 
                    "null";
                string text = seg.Text?.Replace(" ", "·") ?? "null";
                Console.WriteLine($"   [{i}] Text='{text}' (len={seg.Text?.Length ?? 0}) Foreground={fg}");
            }
            
            if (parsedSegments.Count > 10)
            {
                Console.WriteLine($"   ... and {parsedSegments.Count - 10} more segments");
            }
            
            // Step 6: Check for empty segments
            var emptySegments = parsedSegments.Where(s => string.IsNullOrEmpty(s.Text)).ToList();
            if (emptySegments.Any())
            {
                Console.WriteLine($"\n⚠️  WARNING: Found {emptySegments.Count} EMPTY segments!");
            }
            
            // Step 7: Reconstruct text to verify
            string reconstructed = string.Concat(parsedSegments.Select(s => s.Text ?? ""));
            Console.WriteLine($"7. Reconstructed text: '{reconstructed}'");
            Console.WriteLine($"   Original text:      '{dungeonName}'");
            Console.WriteLine($"   Match: {reconstructed == dungeonName}");
            
            if (reconstructed != dungeonName)
            {
                Console.WriteLine($"\n❌ ERROR: Text mismatch!");
                Console.WriteLine($"   Expected length: {dungeonName.Length}");
                Console.WriteLine($"   Actual length:   {reconstructed.Length}");
                Console.WriteLine($"   Missing chars:   {dungeonName.Length - reconstructed.Length}");
            }
            
            Console.WriteLine("\n=== End Diagnostic ===\n");
        }
        
        public static void RunStandardTests()
        {
            Console.WriteLine("======================================");
            Console.WriteLine("COLOR SYSTEM DIAGNOSTIC TEST");
            Console.WriteLine("======================================");
            
            DiagnoseDungeonNameColoring("Celestial Observatory", "Arcane");
            DiagnoseDungeonNameColoring("Crystal Caverns", "Crystal");
            DiagnoseDungeonNameColoring("Ocean Depths", "Ocean");
            DiagnoseDungeonNameColoring("Dreamscape", "Dream");
        }
    }
}

