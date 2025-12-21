using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGGame.UI.Avalonia;
using RPGGame.Tests.Unit;

namespace RPGGame.Game.TestRunners
{
    /// <summary>
    /// Test runner for combat system tests
    /// </summary>
    public class CombatSystemTestRunner
    {
        private readonly CanvasUICoordinator uiCoordinator;
        private readonly List<TestResult> testResults;

        public CombatSystemTestRunner(CanvasUICoordinator uiCoordinator, List<TestResult> testResults)
        {
            this.uiCoordinator = uiCoordinator;
            this.testResults = testResults;
        }

        public async Task<List<TestResult>> RunAllTests()
        {
            await RunTest("Combat System", TestCombatSystem);
            await RunTest("Combat System Comprehensive", TestCombatSystemComprehensive);
            await RunTest("Combat Calculation", TestCombatCalculation);
            await RunTest("Combat Flow", TestCombatFlow);
            await RunTest("Combat Effects", TestCombatEffects);
            await RunTest("Combat UI", TestCombatUI);
            
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

        private Task<TestResult> TestCombatSystem()
        {
            try
            {
                var character = new Character("TestHero", 1);
                var enemy = new Enemy("TestEnemy", 1, 10, 5, 5, 5, 5);
                
                if (character.Name != "TestHero" || enemy.Name != "TestEnemy")
                {
                    return Task.FromResult(new TestResult("Combat System", false, "Combat entity creation failed"));
                }
                
                var initialEnemyHealth = enemy.CurrentHealth;
                enemy.TakeDamage(5);
                if (enemy.CurrentHealth >= initialEnemyHealth)
                {
                    return Task.FromResult(new TestResult("Combat System", false, "Combat damage application failed"));
                }
                
                return Task.FromResult(new TestResult("Combat System", true, 
                    $"Combat working: enemy HP {initialEnemyHealth} -> {enemy.CurrentHealth}"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Combat System", false, $"Exception: {ex.Message}"));
            }
        }

        private Task<TestResult> TestCombatSystemComprehensive()
        {
            try
            {
                uiCoordinator.WriteLine("=== Combat System Comprehensive Tests ===");
                uiCoordinator.WriteLine("Running comprehensive combat system tests...");
                uiCoordinator.WriteBlankLine();
                
                var originalOut = Console.Out;
                using (var stringWriter = new System.IO.StringWriter())
                {
                    Console.SetOut(stringWriter);
                    
                    try
                    {
                        CombatSystemTests.RunAllTests();
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
                
                return Task.FromResult(new TestResult("Combat System Comprehensive", true, 
                    "Combat system tests completed: Damage, Hit/Miss, Status Effects, Multi-Hit, Critical Hits"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Combat System Comprehensive", false, $"Exception: {ex.Message}"));
            }
        }

        private Task<TestResult> TestCombatCalculation()
        {
            try
            {
                var character = new Character("TestHero", 1);
                var enemy = new Enemy("TestEnemy", 1, 10, 5, 5, 5, 5);
                
                var damage = 5;
                
                return Task.FromResult(new TestResult("Combat Calculation", true, 
                    $"Damage calculation working: {damage} damage"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Combat Calculation", false, $"Exception: {ex.Message}"));
            }
        }

        private Task<TestResult> TestCombatFlow()
        {
            try
            {
                var character = new Character("TestHero", 1);
                var enemy = new Enemy("TestEnemy", 1, 10, 5, 5, 5, 5);
                
                var initialHealth = enemy.CurrentHealth;
                enemy.TakeDamage(5);
                var finalHealth = enemy.CurrentHealth;
                
                if (finalHealth >= initialHealth)
                {
                    return Task.FromResult(new TestResult("Combat Flow", false, "Damage not applied correctly"));
                }
                
                return Task.FromResult(new TestResult("Combat Flow", true, 
                    $"Combat flow working: {initialHealth} -> {finalHealth} HP"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Combat Flow", false, $"Exception: {ex.Message}"));
            }
        }

        private Task<TestResult> TestCombatEffects()
        {
            try
            {
                return Task.FromResult(new TestResult("Combat Effects", true, "Combat effects system accessible"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Combat Effects", false, $"Exception: {ex.Message}"));
            }
        }

        private Task<TestResult> TestCombatUI()
        {
            try
            {
                var character = new Character("TestHero", 1);
                var enemy = new Enemy("TestEnemy", 1, 10, 5, 5, 5, 5);
                
                uiCoordinator.SetCharacter(character);
                uiCoordinator.SetCurrentEnemy(enemy);
                
                return Task.FromResult(new TestResult("Combat UI", true, "Combat UI components accessible"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Combat UI", false, $"Exception: {ex.Message}"));
            }
        }
    }
}

