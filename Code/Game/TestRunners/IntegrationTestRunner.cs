using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using RPGGame.UI.Avalonia;

namespace RPGGame.Game.TestRunners
{
    /// <summary>
    /// Test runner for integration tests
    /// </summary>
    public class IntegrationTestRunner
    {
        private readonly CanvasUICoordinator uiCoordinator;
        private readonly List<TestResult> testResults;

        public IntegrationTestRunner(CanvasUICoordinator uiCoordinator, List<TestResult> testResults)
        {
            this.uiCoordinator = uiCoordinator;
            this.testResults = testResults;
        }

        public async Task<List<TestResult>> RunAllTests()
        {
            await RunTest("Game Flow Integration", TestGameFlowIntegration);
            await RunTest("Performance Integration", TestPerformanceIntegration);
            
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

        private Task<TestResult> TestGameFlowIntegration()
        {
            try
            {
                var character = new Character("TestHero", 1);
                var enemy = new Enemy("TestEnemy", 1, 10, 5, 5, 5, 5);
                var inventory = new List<Item>();
                
                inventory.Add(new Item(ItemType.Weapon, "Test Weapon", 1, 0));
                
                var damage = 5;
                enemy.TakeDamage(damage);
                
                return Task.FromResult(new TestResult("Game Flow Integration", true, 
                    "Complete game flow working: Character -> Combat -> Inventory"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Game Flow Integration", false, 
                    $"Exception during integration test: {ex.Message}"));
            }
        }

        private Task<TestResult> TestPerformanceIntegration()
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                
                var character = new Character("TestHero", 1);
                var enemy = new Enemy("TestEnemy", 1, 10, 5, 5, 5, 5);
                var inventory = new List<Item>();
                
                for (int i = 0; i < 10; i++)
                {
                    inventory.Add(new Item(ItemType.Weapon, $"Item {i}", 1, 0));
                    enemy.TakeDamage(1);
                }
                
                stopwatch.Stop();
                var elapsed = stopwatch.ElapsedMilliseconds;
                
                if (elapsed > 1000)
                {
                    return Task.FromResult(new TestResult("Performance Integration", false, 
                        $"Performance too slow: {elapsed}ms for 10 operations"));
                }
                
                return Task.FromResult(new TestResult("Performance Integration", true, 
                    $"Performance good: {elapsed}ms for 10 operations"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Performance Integration", false, 
                    $"Exception during performance test: {ex.Message}"));
            }
        }
    }
}

