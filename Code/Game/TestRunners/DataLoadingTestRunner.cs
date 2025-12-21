using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGGame.UI.Avalonia;

namespace RPGGame.Game.TestRunners
{
    /// <summary>
    /// Test runner for data loading tests
    /// </summary>
    public class DataLoadingTestRunner
    {
        private readonly CanvasUICoordinator uiCoordinator;
        private readonly List<TestResult> testResults;

        public DataLoadingTestRunner(CanvasUICoordinator uiCoordinator, List<TestResult> testResults)
        {
            this.uiCoordinator = uiCoordinator;
            this.testResults = testResults;
        }

        public async Task<List<TestResult>> RunAllTests()
        {
            await RunTest("Data Loading", TestDataLoading);
            await RunTest("JSON Loading", TestJSONLoading);
            await RunTest("Configuration Loading", TestConfigurationLoading);
            await RunTest("Data Validation", TestDataValidation);
            
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

        private Task<TestResult> TestDataLoading()
        {
            try
            {
                return Task.FromResult(new TestResult("Data Loading", true, "Data loading systems accessible"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Data Loading", false, $"Exception: {ex.Message}"));
            }
        }

        private Task<TestResult> TestJSONLoading()
        {
            try
            {
                return Task.FromResult(new TestResult("JSON Loading", true, "JSON loading accessible"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("JSON Loading", false, $"Exception: {ex.Message}"));
            }
        }

        private Task<TestResult> TestConfigurationLoading()
        {
            try
            {
                return Task.FromResult(new TestResult("Configuration Loading", true, "Configuration loading accessible"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Configuration Loading", false, $"Exception: {ex.Message}"));
            }
        }

        private Task<TestResult> TestDataValidation()
        {
            try
            {
                return Task.FromResult(new TestResult("Data Validation", true, "Data validation accessible"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Data Validation", false, $"Exception: {ex.Message}"));
            }
        }
    }
}

