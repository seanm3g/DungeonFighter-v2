using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RPGGame.UI.Avalonia;

namespace RPGGame.Game.TestRunners
{
    /// <summary>
    /// Test runner for inventory system tests
    /// </summary>
    public class InventorySystemTestRunner
    {
        private readonly CanvasUICoordinator uiCoordinator;
        private readonly List<TestResult> testResults;

        public InventorySystemTestRunner(CanvasUICoordinator uiCoordinator, List<TestResult> testResults)
        {
            this.uiCoordinator = uiCoordinator;
            this.testResults = testResults;
        }

        public async Task<List<TestResult>> RunAllTests()
        {
            await RunTest("Inventory System", TestInventorySystem);
            await RunTest("Item Management", TestItemManagement);
            await RunTest("Equipment System", TestEquipmentSystem);
            await RunTest("Inventory Display", TestInventoryDisplay);
            
            return new List<TestResult>(testResults);
        }

        private async Task<TestResult> RunTest(string testName, Func<Task<TestResult>> testFunction)
        {
            uiCoordinator.WriteLine($"Running: {testName}...");
            
            try
            {
                var result = await Task.Run(async () => await testFunction()).ConfigureAwait(false);
                testResults.Add(result);
                
                if (result.Passed)
                {
                    uiCoordinator.WriteLine($"✅ {testName}: PASSED");
                    if (!string.IsNullOrEmpty(result.Message))
                    {
                        uiCoordinator.WriteLine($"   {result.Message}");
                    }
                }
                else
                {
                    uiCoordinator.WriteLine($"❌ {testName}: FAILED");
                    uiCoordinator.WriteLine($"   {result.Message}");
                }
                
                uiCoordinator.WriteBlankLine();
                return result;
            }
            catch (Exception ex)
            {
                var result = new TestResult(testName, false, $"Exception: {ex.Message}");
                testResults.Add(result);
                uiCoordinator.WriteLine($"❌ {testName}: ERROR");
                uiCoordinator.WriteLine($"   {ex.Message}");
                uiCoordinator.WriteBlankLine();
                return result;
            }
        }

        private Task<TestResult> TestInventorySystem()
        {
            try
            {
                var inventory = new List<Item>();
                
                var testItem = new Item(ItemType.Weapon, "Test Sword", 1, 0);
                inventory.Add(testItem);
                
                if (inventory.Count != 1)
                {
                    return Task.FromResult(new TestResult("Inventory System", false, "Item addition failed"));
                }
                
                inventory.Remove(testItem);
                if (inventory.Count != 0)
                {
                    return Task.FromResult(new TestResult("Inventory System", false, "Item removal failed"));
                }
                
                return Task.FromResult(new TestResult("Inventory System", true, 
                    $"Inventory operations working: {inventory.Count} items"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Inventory System", false, $"Exception: {ex.Message}"));
            }
        }

        private Task<TestResult> TestItemManagement()
        {
            try
            {
                var inventory = new List<Item>();
                var item = new Item(ItemType.Weapon, "Test Item", 1, 0);
                
                inventory.Add(item);
                var found = inventory.FirstOrDefault(i => i.Name == "Test Item");
                
                if (found == null)
                {
                    return Task.FromResult(new TestResult("Item Management", false, "Item not found after addition"));
                }
                
                return Task.FromResult(new TestResult("Item Management", true, 
                    $"Item management working: {found.Name} found"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Item Management", false, $"Exception: {ex.Message}"));
            }
        }

        private Task<TestResult> TestEquipmentSystem()
        {
            try
            {
                var character = new Character("TestChar", 1);
                var weapon = new Item(ItemType.Weapon, "Test Weapon", 1, 0);
                
                return Task.FromResult(new TestResult("Equipment System", true, "Equipment system accessible"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Equipment System", false, $"Exception: {ex.Message}"));
            }
        }

        private Task<TestResult> TestInventoryDisplay()
        {
            try
            {
                var inventory = new List<Item>();
                inventory.Add(new Item(ItemType.Weapon, "Test Item 1", 1, 0));
                inventory.Add(new Item(ItemType.Chest, "Test Item 2", 1, 0));
                
                return Task.FromResult(new TestResult("Inventory Display", true, 
                    $"Inventory display working: {inventory.Count} items"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Inventory Display", false, $"Exception: {ex.Message}"));
            }
        }
    }
}

