using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Runners
{
    /// <summary>
    /// Test runner for Text System Accuracy tests
    /// </summary>
    public static class TextSystemAccuracyTestRunner
    {
        /// <summary>
        /// Tests word spacing, blank line spacing, overlap prevention, and color application
        /// </summary>
        public static void RunTest()
        {
            TextDisplayIntegration.DisplaySystem("=== Test 8: Text System Accuracy Test ===");
            TextDisplayIntegration.DisplaySystem("Running comprehensive text system accuracy tests...");
            TextDisplayIntegration.DisplaySystem("Testing: word spacing, blank lines, overlap, colors");
            TextDisplayIntegration.DisplaySystem("");
            
            try
            {
                TextSystemAccuracyTests.RunAllTests();
                TextDisplayIntegration.DisplaySystem("\n✓ Text System Accuracy Test completed successfully!");
            }
            catch (Exception ex)
            {
                TextDisplayIntegration.DisplaySystem($"\n✗ Text System Accuracy Test failed: {ex.Message}");
                TextDisplayIntegration.DisplaySystem($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
