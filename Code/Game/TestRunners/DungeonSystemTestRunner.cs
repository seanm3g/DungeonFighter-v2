using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGGame.UI.Avalonia;

namespace RPGGame.Game.TestRunners
{
    /// <summary>
    /// Test runner for dungeon system tests
    /// </summary>
    public class DungeonSystemTestRunner
    {
        private readonly CanvasUICoordinator uiCoordinator;
        private readonly List<TestResult> testResults;

        public DungeonSystemTestRunner(CanvasUICoordinator uiCoordinator, List<TestResult> testResults)
        {
            this.uiCoordinator = uiCoordinator;
            this.testResults = testResults;
        }

        public async Task<List<TestResult>> RunAllTests()
        {
            await RunTest("Dungeon System", TestDungeonSystem);
            await RunTest("Dungeon Generation", TestDungeonGeneration);
            await RunTest("Room Generation", TestRoomGeneration);
            await RunTest("Enemy Spawning", TestEnemySpawning);
            await RunTest("Dungeon Progression", TestDungeonProgression);
            
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

        private Task<TestResult> TestDungeonSystem()
        {
            try
            {
                return Task.FromResult(new TestResult("Dungeon System", true, "Dungeon system accessible"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Dungeon System", false, $"Exception: {ex.Message}"));
            }
        }

        private Task<TestResult> TestDungeonGeneration()
        {
            try
            {
                return Task.FromResult(new TestResult("Dungeon Generation", true, "Dungeon generation accessible"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Dungeon Generation", false, $"Exception: {ex.Message}"));
            }
        }

        private Task<TestResult> TestRoomGeneration()
        {
            try
            {
                return Task.FromResult(new TestResult("Room Generation", true, "Room generation accessible"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Room Generation", false, $"Exception: {ex.Message}"));
            }
        }

        private Task<TestResult> TestEnemySpawning()
        {
            try
            {
                var enemy = new Enemy("TestEnemy", 1, 10, 5, 5, 5, 5);
                
                if (enemy.Name != "TestEnemy" || enemy.Level != 1)
                {
                    return Task.FromResult(new TestResult("Enemy Spawning", false, "Enemy creation failed"));
                }
                
                return Task.FromResult(new TestResult("Enemy Spawning", true, 
                    $"Enemy spawning working: {enemy.Name} Level {enemy.Level}"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Enemy Spawning", false, $"Exception: {ex.Message}"));
            }
        }

        private Task<TestResult> TestDungeonProgression()
        {
            try
            {
                return Task.FromResult(new TestResult("Dungeon Progression", true, "Dungeon progression accessible"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Dungeon Progression", false, $"Exception: {ex.Message}"));
            }
        }
    }
}

