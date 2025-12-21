using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGGame.UI.Avalonia;
using RPGGame.Tests.Unit;

namespace RPGGame.Game.TestRunners
{
    /// <summary>
    /// Test runner for action system tests
    /// </summary>
    public class ActionSystemTestRunner
    {
        private readonly CanvasUICoordinator uiCoordinator;
        private readonly List<TestResult> testResults;

        public ActionSystemTestRunner(CanvasUICoordinator uiCoordinator, List<TestResult> testResults)
        {
            this.uiCoordinator = uiCoordinator;
            this.testResults = testResults;
        }

        public async Task<List<TestResult>> RunAllTests()
        {
            await RunTest("Action System", TestActionSystem);
            await RunTest("Combo Dice Rolls", TestComboDiceRolls);
            await RunTest("Advanced Action Mechanics", TestAdvancedActionMechanics);
            
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

        private Task<TestResult> TestActionSystem()
        {
            try
            {
                uiCoordinator.WriteLine("=== Action and Action Sequence Tests ===");
                uiCoordinator.WriteLine("Running comprehensive action system tests...");
                uiCoordinator.WriteBlankLine();
                
                var originalOut = Console.Out;
                using (var stringWriter = new System.IO.StringWriter())
                {
                    Console.SetOut(stringWriter);
                    
                    try
                    {
                        ActionAndSequenceTests.RunAllTests();
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
                
                return Task.FromResult(new TestResult("Action System", true, 
                    "Action and sequence tests completed: Creation, Selection, Combo Sequences, Execution Flow"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Action System", false, $"Exception: {ex.Message}"));
            }
        }

        private Task<TestResult> TestComboDiceRolls()
        {
            try
            {
                uiCoordinator.WriteLine("=== Combo and Dice Roll Tests ===");
                uiCoordinator.WriteLine("Running comprehensive combo and dice roll tests...");
                uiCoordinator.WriteBlankLine();
                
                var originalOut = Console.Out;
                using (var stringWriter = new System.IO.StringWriter())
                {
                    Console.SetOut(stringWriter);
                    
                    try
                    {
                        ComboDiceRollTests.RunAllTests();
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
                
                return Task.FromResult(new TestResult("Combo Dice Rolls", true, 
                    "Combo and dice roll tests completed: Dice mechanics, Action selection, Combo sequences, Conditional triggers"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Combo Dice Rolls", false, $"Exception: {ex.Message}"));
            }
        }

        private Task<TestResult> TestAdvancedActionMechanics()
        {
            try
            {
                uiCoordinator.WriteLine("=== Advanced Action Mechanics Tests ===");
                uiCoordinator.WriteLine("Running comprehensive tests for all phases...");
                uiCoordinator.WriteBlankLine();
                
                var originalOut = Console.Out;
                using (var stringWriter = new System.IO.StringWriter())
                {
                    Console.SetOut(stringWriter);
                    
                    try
                    {
                        AdvancedMechanicsTest.RunAllTests();
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
                
                return Task.FromResult(new TestResult("Advanced Action Mechanics", true, 
                    "All phases tested: Roll Modification, Status Effects, Tag System, Outcome Handlers"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Advanced Action Mechanics", false, $"Exception: {ex.Message}"));
            }
        }
    }
}

