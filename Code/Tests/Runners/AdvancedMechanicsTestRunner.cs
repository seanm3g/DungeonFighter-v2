using System;
using System.IO;
using RPGGame.Tests.Unit;

namespace RPGGame.Tests.Runners
{
    /// <summary>
    /// Test runner for Advanced Action Mechanics tests
    /// </summary>
    public static class AdvancedMechanicsTestRunner
    {
        /// <summary>
        /// Runs comprehensive tests for all phases of the advanced mechanics system
        /// </summary>
        public static void RunTest()
        {
            TextDisplayIntegration.DisplaySystem("=== Test 9: Advanced Action Mechanics Test ===");
            TextDisplayIntegration.DisplaySystem("Running comprehensive tests for all advanced mechanics phases...");
            TextDisplayIntegration.DisplaySystem("Press any key to continue or 'q' to quit...");
            
            var key = Console.ReadKey();
            if (key.KeyChar == 'q' || key.KeyChar == 'Q')
            {
                TextDisplayIntegration.DisplaySystem("Test cancelled.");
                return;
            }
            
            Console.WriteLine();
            Console.WriteLine();
            
            // Capture console output and redirect to TextDisplayIntegration
            var originalOut = Console.Out;
            using (var stringWriter = new StringWriter())
            {
                Console.SetOut(stringWriter);
                
                try
                {
                    AdvancedMechanicsTest.RunAllTests();
                    string output = stringWriter.ToString();
                    
                    // Display output line by line
                    foreach (var line in output.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None))
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            TextDisplayIntegration.DisplaySystem(line);
                        }
                    }
                }
                catch (Exception ex)
                {
                    TextDisplayIntegration.DisplaySystem($"Error running Advanced Mechanics tests: {ex.Message}");
                    TextDisplayIntegration.DisplaySystem($"Stack trace: {ex.StackTrace}");
                }
                finally
                {
                    Console.SetOut(originalOut);
                }
            }
            
            TextDisplayIntegration.DisplaySystem("\nAdvanced Action Mechanics Test completed!");
            TextDisplayIntegration.DisplaySystem("Press any key to continue...");
            Console.ReadKey();
        }
    }
}
