using System;
using RPGGame.UI.ColorSystem;

namespace ColorSystemTest
{
    /// <summary>
    /// Standalone test program for the new color system
    /// This demonstrates the new color system working independently
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("🎨 DUNGEON FIGHTER COLOR SYSTEM TEST 🎨");
            Console.WriteLine("========================================");
            Console.WriteLine();
            
            try
            {
                // Run the main demo
                ColorSystemDemoRunner.RunDemo();
                
                Console.WriteLine();
                Console.WriteLine("Press any key to run console color demo...");
                Console.ReadKey();
                Console.Clear();
                
                // Run console demo
                ColorSystemDemoRunner.RunConsoleDemo();
                
                Console.WriteLine();
                Console.WriteLine("Press any key to run performance test...");
                Console.ReadKey();
                Console.Clear();
                
                // Run performance test
                ColorSystemDemoRunner.RunPerformanceTest();
                
                Console.WriteLine();
                Console.WriteLine("🎉 ALL TESTS COMPLETED SUCCESSFULLY! 🎉");
                Console.WriteLine("The new color system is ready for production!");
                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}
