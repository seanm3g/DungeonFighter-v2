using System;
using RPGGame.UI;
using RPGGame.Tests.Unit;
using RPGGame.Tests;
using RPGGame.Utils;

namespace RPGGame.Tests
{
    /// <summary>
    /// Test runner for color debugging tools
    /// </summary>
    public static class ColorDebugTestRunner
    {
        /// <summary>
        /// Runs color debugging tools to diagnose spacing issues
        /// </summary>
        public static void RunTest()
        {
            TextDisplayIntegration.DisplaySystem("Running Color Debug Tool...");
            TextDisplayIntegration.DisplaySystem("This will help diagnose spacing issues with colored text.");
            
            if (!TestHarnessBase.PromptContinue())
                return;
            
            Console.WriteLine();
            Console.WriteLine();
            
            // Run the debug tool
            ColorDebugTool.RunCombatMessageTests();
            
            TextDisplayIntegration.DisplaySystem("\nPress any key to continue...");
            Console.ReadKey();
        }
    }
}

