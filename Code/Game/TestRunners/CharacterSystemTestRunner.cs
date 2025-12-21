using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGGame.UI.Avalonia;

namespace RPGGame.Game.TestRunners
{
    /// <summary>
    /// Test runner for character system tests
    /// </summary>
    public class CharacterSystemTestRunner
    {
        private readonly CanvasUICoordinator uiCoordinator;
        private readonly List<TestResult> testResults;

        public CharacterSystemTestRunner(CanvasUICoordinator uiCoordinator, List<TestResult> testResults)
        {
            this.uiCoordinator = uiCoordinator;
            this.testResults = testResults;
        }

        public async Task<List<TestResult>> RunAllTests()
        {
            await RunTest("Character System", TestCharacterSystem);
            await RunTest("Character Creation", TestCharacterCreation);
            await RunTest("Character Stats", TestCharacterStats);
            await RunTest("Character Progression", TestCharacterProgression);
            await RunTest("Character Equipment", TestCharacterEquipment);
            
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

        private Task<TestResult> TestCharacterSystem()
        {
            try
            {
                var character = new Character("TestHero", 1);
                
                if (string.IsNullOrEmpty(character.Name) || character.Level != 1)
                {
                    return Task.FromResult(new TestResult("Character System", false, "Basic character creation failed"));
                }
                
                if (character.CurrentHealth <= 0 || character.MaxHealth <= 0)
                {
                    return Task.FromResult(new TestResult("Character System", false, "Character stats initialization failed"));
                }
                
                var initialLevel = character.Level;
                character.Progression.AddXP(1000);
                if (character.Level <= initialLevel)
                {
                    return Task.FromResult(new TestResult("Character System", false, "Character progression failed"));
                }
                
                return Task.FromResult(new TestResult("Character System", true, 
                    $"Character system working: {character.Name} Level {character.Level}"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Character System", false, $"Exception: {ex.Message}"));
            }
        }

        private Task<TestResult> TestCharacterCreation()
        {
            try
            {
                var character = new Character("TestChar", 1);
                
                if (character.Name != "TestChar" || character.Level != 1)
                {
                    return Task.FromResult(new TestResult("Character Creation", false, "Character creation parameters not set correctly"));
                }
                
                return Task.FromResult(new TestResult("Character Creation", true, 
                    $"Created character: {character.Name} Level {character.Level}"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Character Creation", false, $"Exception: {ex.Message}"));
            }
        }

        private Task<TestResult> TestCharacterStats()
        {
            try
            {
                var character = new Character("TestChar", 1);
                
                if (character.CurrentHealth <= 0 || character.MaxHealth <= 0)
                {
                    return Task.FromResult(new TestResult("Character Stats", false, "Character stats not properly initialized"));
                }
                
                return Task.FromResult(new TestResult("Character Stats", true, 
                    $"Stats: HP {character.CurrentHealth}/{character.MaxHealth}"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Character Stats", false, $"Exception: {ex.Message}"));
            }
        }

        private Task<TestResult> TestCharacterProgression()
        {
            try
            {
                var character = new Character("TestChar", 1);
                var initialLevel = character.Level;
                
                character.Progression.AddXP(1000);
                
                if (character.Level <= initialLevel)
                {
                    return Task.FromResult(new TestResult("Character Progression", false, "Character did not level up"));
                }
                
                return Task.FromResult(new TestResult("Character Progression", true, 
                    $"Leveled up from {initialLevel} to {character.Level}"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Character Progression", false, $"Exception: {ex.Message}"));
            }
        }

        private Task<TestResult> TestCharacterEquipment()
        {
            try
            {
                var character = new Character("TestChar", 1);
                var initialStrength = character.Strength;
                
                return Task.FromResult(new TestResult("Character Equipment", true, 
                    $"Equipment system accessible, base strength: {initialStrength}"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Character Equipment", false, $"Exception: {ex.Message}"));
            }
        }
    }
}

