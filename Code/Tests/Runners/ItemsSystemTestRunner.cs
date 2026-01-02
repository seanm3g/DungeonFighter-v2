using System;
using RPGGame;
using RPGGame.Tests.Unit.Items;

namespace RPGGame.Tests.Runners
{
    /// <summary>
    /// Test runner for Items system tests
    /// </summary>
    public static class ItemsSystemTestRunner
    {
        /// <summary>
        /// Runs all Items system tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine(GameConstants.StandardSeparator);
            Console.WriteLine("  ITEMS SYSTEM TEST SUITE");
            Console.WriteLine($"{GameConstants.StandardSeparator}\n");

            InventoryManagerTests.RunAllTests();
            Console.WriteLine();
            ItemTests.RunAllTests();
            Console.WriteLine();
            ComboManagerTests.RunAllTests();
            Console.WriteLine();
            InventoryOperationsTests.RunAllTests();
            Console.WriteLine();
            AttributeRequirementTests.RunAllTests();
            Console.WriteLine();
            BasicGearConfigTests.RunAllTests();
        }
    }
}
