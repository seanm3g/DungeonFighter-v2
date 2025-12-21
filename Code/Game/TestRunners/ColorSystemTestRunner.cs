using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGGame.UI.Avalonia;
using RPGGame.Tests;
using RPGGame.Tests.Unit;

namespace RPGGame.Game.TestRunners
{
    /// <summary>
    /// Test runner for color system tests
    /// </summary>
    public class ColorSystemTestRunner
    {
        private readonly CanvasUICoordinator uiCoordinator;
        private readonly List<TestResult> testResults;

        public ColorSystemTestRunner(CanvasUICoordinator uiCoordinator, List<TestResult> testResults)
        {
            this.uiCoordinator = uiCoordinator;
            this.testResults = testResults;
        }

        public async Task<List<TestResult>> RunAllTests()
        {
            await RunTest("Color System", TestColorSystem);
            await RunTest("Colored Text Visual Tests", TestColoredTextVisual);
            
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

        private Task<TestResult> TestColorSystem()
        {
            try
            {
                uiCoordinator.WriteLine("=== Color Configuration Loader Tests ===", UIMessageType.System);
                uiCoordinator.WriteBlankLine();
                
                var originalOut = Console.Out;
                var stringWriter = new System.IO.StringWriter();
                Console.SetOut(stringWriter);
                
                int testsRun = 0;
                int testsPassed = 0;
                int testsFailed = 0;
                
                try
                {
                    ColorConfigurationLoaderTest.RunAllTests();
                    
                    var output = stringWriter.ToString();
                    var lines = output.Split('\n');
                    
                    foreach (var line in lines)
                    {
                        if (line.Contains("✓"))
                        {
                            testsPassed++;
                            testsRun++;
                            uiCoordinator.WriteLine($"  {line.Trim()}", UIMessageType.System);
                        }
                        else if (line.Contains("✗") || line.Contains("FAILED"))
                        {
                            testsFailed++;
                            testsRun++;
                            uiCoordinator.WriteLine($"  {line.Trim()}", UIMessageType.System);
                        }
                        else if (line.Contains("===") || line.Contains("---"))
                        {
                            uiCoordinator.WriteLine(line.Trim(), UIMessageType.System);
                        }
                        else if (!string.IsNullOrWhiteSpace(line) && 
                                 (line.Contains("Total Tests:") || line.Contains("Passed:") || 
                                  line.Contains("Failed:") || line.Contains("Success Rate:") ||
                                  line.Contains("All tests passed") || line.Contains("test(s) failed")))
                        {
                            uiCoordinator.WriteLine(line.Trim(), UIMessageType.System);
                        }
                    }
                }
                finally
                {
                    Console.SetOut(originalOut);
                }
                
                uiCoordinator.WriteBlankLine();
                
                bool success = testsFailed == 0 && testsRun > 0;
                string message = testsRun > 0 
                    ? $"{testsPassed} passed, {testsFailed} failed out of {testsRun} tests"
                    : "No tests executed";
                
                return Task.FromResult(new TestResult("Color Configuration Loader", success, message));
            }
            catch (Exception ex)
            {
                uiCoordinator.WriteLine($"✗ Error running color configuration tests: {ex.Message}", UIMessageType.System);
                return Task.FromResult(new TestResult("Color Configuration Loader", false, $"Exception: {ex.Message}"));
            }
        }

        private async Task<TestResult> TestColoredTextVisual()
        {
            try
            {
                uiCoordinator.Clear();
                await ColoredTextVisualTests.RunAllVisualTests(uiCoordinator);
                return new TestResult("Colored Text Visual Tests", true, 
                    "Visual tests displayed. Please review the screen for correctness.");
            }
            catch (Exception ex)
            {
                return new TestResult("Colored Text Visual Tests", false, $"Exception: {ex.Message}");
            }
        }
    }
}

