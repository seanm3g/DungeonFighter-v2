using System;
using RPGGame;
using RPGGame.Tests.Unit.UI;

namespace RPGGame.Tests.Runners
{
    /// <summary>
    /// Test runner for UI system tests
    /// </summary>
    public static class UISystemTestRunner
    {
        /// <summary>
        /// Runs all UI system tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine(GameConstants.StandardSeparator);
            Console.WriteLine("  UI SYSTEM TEST SUITE");
            Console.WriteLine($"{GameConstants.StandardSeparator}\n");

            UIManagerTests.RunAllTests();
            Console.WriteLine();
            BlockDisplayManagerTests.RunAllTests();
            Console.WriteLine();
            ItemDisplayFormatterTests.RunAllTests();
            Console.WriteLine();
            CanvasUICoordinatorTests.RunAllTests();
            Console.WriteLine();
            CanvasRendererTests.RunAllTests();
            Console.WriteLine();
            SettingsManagerTests.RunAllTests();
            Console.WriteLine();
            DungeonRendererTests.RunAllTests();
            Console.WriteLine();
            ItemModifiersTabManagerTests.RunAllTests();
            Console.WriteLine();
            ItemsTabManagerTests.RunAllTests();
            Console.WriteLine();
            KeywordColorSystemTests.RunAllTests();
            Console.WriteLine();
            DisplayRendererTests.RunAllTests();
            Console.WriteLine();
            GameCanvasControlTests.RunAllTests();
            Console.WriteLine();
            CanvasTextManagerTests.RunAllTests();
            Console.WriteLine();
            MenuRendererTests.RunAllTests();
            Console.WriteLine();
            CombatRendererTests.RunAllTests();
            Console.WriteLine();
            InventoryRendererTests.RunAllTests();
        }
    }
}
