using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGGame.UI.Avalonia;
using RPGGame.Tests;

namespace RPGGame.Game.TestRunners
{
    /// <summary>
    /// Test runner for analysis tests
    /// </summary>
    public class AnalysisTestRunner
    {
        private readonly CanvasUICoordinator uiCoordinator;
        private readonly List<TestResult> testResults;

        public AnalysisTestRunner(CanvasUICoordinator uiCoordinator, List<TestResult> testResults)
        {
            this.uiCoordinator = uiCoordinator;
            this.testResults = testResults;
        }

        public async Task<List<TestResult>> RunAllTests()
        {
            await RunTest("Item Generation Analysis", TestItemGenerationAnalysis);
            await RunTest("Tier Distribution Verification", TestTierDistribution);
            await RunTest("Common Item Modification Chance", TestCommonItemModification);
            
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

        private Task<TestResult> TestItemGenerationAnalysis()
        {
            try
            {
                uiCoordinator.WriteLine("=== Item Generation Analysis Test ===");
                uiCoordinator.WriteLine("This will generate 100 items at each level from 1-20 and analyze the results.");
                uiCoordinator.WriteBlankLine();
                
                var originalOut = Console.Out;
                using (var stringWriter = new System.IO.StringWriter())
                {
                    Console.SetOut(stringWriter);
                    
                    try
                    {
                        TestManager.RunItemGenerationTest();
                        string output = stringWriter.ToString();
                        
                        foreach (var line in output.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None))
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                uiCoordinator.WriteLine(line);
                            }
                        }
                    }
                    finally
                    {
                        Console.SetOut(originalOut);
                    }
                }
                
                return Task.FromResult(new TestResult("Item Generation Analysis", true, 
                    "Analysis completed. Results saved to 'item_generation_test_results.txt'"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Item Generation Analysis", false, $"Exception: {ex.Message}"));
            }
        }

        private Task<TestResult> TestTierDistribution()
        {
            try
            {
                uiCoordinator.WriteLine("=== Tier Distribution Verification Test ===");
                uiCoordinator.WriteLine("Testing tier distribution across various player/dungeon level scenarios.");
                uiCoordinator.WriteBlankLine();
                
                var originalOut = Console.Out;
                using (var stringWriter = new System.IO.StringWriter())
                {
                    Console.SetOut(stringWriter);
                    
                    try
                    {
                        TierDistributionTest.TestTierDistribution();
                        string output = stringWriter.ToString();
                        
                        foreach (var line in output.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None))
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                uiCoordinator.WriteLine(line);
                            }
                        }
                    }
                    finally
                    {
                        Console.SetOut(originalOut);
                    }
                }
                
                return Task.FromResult(new TestResult("Tier Distribution Verification", true, 
                    "Tier distribution test completed"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Tier Distribution Verification", false, $"Exception: {ex.Message}"));
            }
        }

        private Task<TestResult> TestCommonItemModification()
        {
            try
            {
                uiCoordinator.WriteLine("=== Common Item Modification Test ===");
                uiCoordinator.WriteLine("This will generate 1000 Common items and verify the 25% chance for modifications.");
                uiCoordinator.WriteBlankLine();
                
                var originalOut = Console.Out;
                using (var stringWriter = new System.IO.StringWriter())
                {
                    Console.SetOut(stringWriter);
                    
                    try
                    {
                        TestManager.RunCommonItemModificationTest();
                        string output = stringWriter.ToString();
                        
                        foreach (var line in output.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None))
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                uiCoordinator.WriteLine(line);
                            }
                        }
                    }
                    finally
                    {
                        Console.SetOut(originalOut);
                    }
                }
                
                return Task.FromResult(new TestResult("Common Item Modification Chance", true, 
                    "Common item modification test completed"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Common Item Modification Chance", false, $"Exception: {ex.Message}"));
            }
        }
    }
}

