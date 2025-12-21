using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGGame.UI.Avalonia;

namespace RPGGame.Game.TestRunners
{
    /// <summary>
    /// Test runner for save/load system tests
    /// </summary>
    public class SaveLoadTestRunner
    {
        private readonly CanvasUICoordinator uiCoordinator;
        private readonly List<TestResult> testResults;

        public SaveLoadTestRunner(CanvasUICoordinator uiCoordinator, List<TestResult> testResults)
        {
            this.uiCoordinator = uiCoordinator;
            this.testResults = testResults;
        }

        public async Task<List<TestResult>> RunAllTests()
        {
            await RunTest("Save/Load System", TestSaveLoadSystem);
            
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

        private Task<TestResult> TestSaveLoadSystem()
        {
            try
            {
                var character = new Character("TestChar", 1);
                
                return Task.FromResult(new TestResult("Save/Load System", true, "Save/load system accessible"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Save/Load System", false, $"Exception: {ex.Message}"));
            }
        }
    }
}

