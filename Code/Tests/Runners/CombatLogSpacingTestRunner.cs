using System;
using RPGGame.Tests.Unit;

namespace RPGGame.Tests.Runners
{
    /// <summary>
    /// Test runner for Combat Log Spacing tests
    /// </summary>
    public static class CombatLogSpacingTestRunner
    {
        /// <summary>
        /// Runs comprehensive tests for the combat log spacing system
        /// </summary>
        public static void RunTest()
        {
            TextDisplayIntegration.DisplaySystem("=== Test 7: Combat Log Spacing Test ===");
            TextDisplayIntegration.DisplaySystem("Running comprehensive spacing system tests...");
            TextDisplayIntegration.DisplaySystem("");
            
            try
            {
                CombatLogSpacingTest.RunAllTests();
                TextDisplayIntegration.DisplaySystem("\n✓ Combat Log Spacing Test completed successfully!");
            }
            catch (Exception ex)
            {
                TextDisplayIntegration.DisplaySystem($"\n✗ Combat Log Spacing Test failed: {ex.Message}");
                TextDisplayIntegration.DisplaySystem($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
