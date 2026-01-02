using System;
using System.Collections.Generic;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Items
{
    /// <summary>
    /// Comprehensive tests for InventoryManager
    /// Tests inventory management, item addition/removal, and equipment operations
    /// </summary>
    public static class InventoryManagerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all InventoryManager tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== InventoryManager Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestAddItem();
            TestRemoveItem();
            TestGetInventory();
            TestGetInventoryCount();
            TestUpdateInventory();

            TestBase.PrintSummary("InventoryManager Tests", _testsRun, _testsPassed, _testsFailed);
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
            var manager = new InventoryManager(character, inventory);
            TestBase.AssertNotNull(manager,
                "InventoryManager should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with null character
            try
            {
                var manager2 = new InventoryManager(null!, inventory);
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
                var manager3 = new InventoryManager(character, null!);
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

        #region Item Management Tests

        private static void TestAddItem()
        {
            Console.WriteLine("\n--- Testing AddItem ---");

            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            var inventory = new List<Item>();
            var manager = new InventoryManager(character, inventory);

            var item = TestDataBuilders.Item()
                .WithName("Test Item")
                .WithTier(1)
                .Build();

            var initialCount = manager.GetInventoryCount();
            manager.AddItem(item);
            var finalCount = manager.GetInventoryCount();

            TestBase.AssertEqual(initialCount + 1, finalCount,
                "Inventory count should increase after adding item",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(inventory.Contains(item),
                "Inventory should contain added item",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRemoveItem()
        {
            Console.WriteLine("\n--- Testing RemoveItem ---");

            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            var inventory = new List<Item>();
            var item = TestDataBuilders.Item()
                .WithName("Test Item")
                .WithTier(1)
                .Build();

            inventory.Add(item);
            var manager = new InventoryManager(character, inventory);

            var initialCount = manager.GetInventoryCount();
            var result = manager.RemoveItem(item);
            var finalCount = manager.GetInventoryCount();

            TestBase.AssertTrue(result,
                "RemoveItem should return true for existing item",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(initialCount - 1, finalCount,
                "Inventory count should decrease after removing item",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertFalse(inventory.Contains(item),
                "Inventory should not contain removed item",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test removing non-existent item
            var result2 = manager.RemoveItem(item);
            TestBase.AssertFalse(result2,
                "RemoveItem should return false for non-existent item",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Inventory Access Tests

        private static void TestGetInventory()
        {
            Console.WriteLine("\n--- Testing GetInventory ---");

            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            var inventory = new List<Item>();
            var item1 = TestDataBuilders.Item().WithName("Item1").Build();
            var item2 = TestDataBuilders.Item().WithName("Item2").Build();
            inventory.Add(item1);
            inventory.Add(item2);

            var manager = new InventoryManager(character, inventory);
            var retrievedInventory = manager.GetInventory();

            TestBase.AssertNotNull(retrievedInventory,
                "GetInventory should return inventory list",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (retrievedInventory != null)
            {
                TestBase.AssertEqual(2, retrievedInventory.Count,
                    "Retrieved inventory should have correct count",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertTrue(retrievedInventory.Contains(item1),
                    "Retrieved inventory should contain item1",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertTrue(retrievedInventory.Contains(item2),
                    "Retrieved inventory should contain item2",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestGetInventoryCount()
        {
            Console.WriteLine("\n--- Testing GetInventoryCount ---");

            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            var inventory = new List<Item>();
            var manager = new InventoryManager(character, inventory);

            TestBase.AssertEqual(0, manager.GetInventoryCount(),
                "Empty inventory should have count 0",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            manager.AddItem(TestDataBuilders.Item().Build());
            TestBase.AssertEqual(1, manager.GetInventoryCount(),
                "Inventory with 1 item should have count 1",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            manager.AddItem(TestDataBuilders.Item().Build());
            TestBase.AssertEqual(2, manager.GetInventoryCount(),
                "Inventory with 2 items should have count 2",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Update Tests

        private static void TestUpdateInventory()
        {
            Console.WriteLine("\n--- Testing UpdateInventory ---");

            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            var inventory1 = new List<Item>();
            var manager = new InventoryManager(character, inventory1);

            var newInventory = new List<Item>
            {
                TestDataBuilders.Item().WithName("New Item 1").Build(),
                TestDataBuilders.Item().WithName("New Item 2").Build()
            };

            manager.UpdateInventory(newInventory);

            TestBase.AssertEqual(2, manager.GetInventoryCount(),
                "Updated inventory should have correct count",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var retrieved = manager.GetInventory();
            TestBase.AssertEqual(2, retrieved.Count,
                "Retrieved inventory should match updated inventory",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
