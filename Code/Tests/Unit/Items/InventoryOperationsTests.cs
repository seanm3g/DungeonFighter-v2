using System;
using System.Collections.Generic;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Items
{
    /// <summary>
    /// Comprehensive tests for InventoryOperations
    /// Tests inventory operations (equip, unequip, discard, trade-up)
    /// Note: Some methods require user input, so we test what we can
    /// </summary>
    public static class InventoryOperationsTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all InventoryOperations tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== InventoryOperations Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();

            // Note: EquipItem, UnequipItem, DiscardItem, and TradeUpItems
            // require user input (Console.ReadLine), so we can't easily test them
            // in unit tests without mocking. These would be better suited for
            // integration tests or tests with input mocking

            TestBase.PrintSummary("InventoryOperations Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            var inventory = new List<Item>();

            // Test normal construction
            var operations = new InventoryOperations(character, inventory);
            TestBase.AssertNotNull(operations,
                "InventoryOperations should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with null character
            try
            {
                var operations2 = new InventoryOperations(null!, inventory);
                TestBase.AssertTrue(false,
                    "Constructor should throw for null character",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (ArgumentNullException)
            {
                TestBase.AssertTrue(true,
                    "Constructor should throw ArgumentNullException for null character",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test with null inventory
            try
            {
                var operations3 = new InventoryOperations(character, null!);
                TestBase.AssertTrue(false,
                    "Constructor should throw for null inventory",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (ArgumentNullException)
            {
                TestBase.AssertTrue(true,
                    "Constructor should throw ArgumentNullException for null inventory",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion
    }
}
