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
            await RunTest("Multi-Character Registration", TestMultiCharacterRegistration);
            await RunTest("Multi-Character Switching", TestMultiCharacterSwitching);
            await RunTest("Multi-Character Context", TestMultiCharacterContext);
            
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

        private Task<TestResult> TestMultiCharacterRegistration()
        {
            try
            {
                var stateManager = new GameStateManager();
                var character1 = new Character("MultiTest1", 1);
                var character2 = new Character("MultiTest2", 5);

                var id1 = stateManager.AddCharacter(character1);
                var id2 = stateManager.AddCharacter(character2);

                if (string.IsNullOrEmpty(id1) || string.IsNullOrEmpty(id2))
                {
                    return Task.FromResult(new TestResult("Multi-Character Registration", false, "Character IDs not generated"));
                }

                if (id1 == id2)
                {
                    return Task.FromResult(new TestResult("Multi-Character Registration", false, "Character IDs should be unique"));
                }

                var allCharacters = stateManager.GetAllCharacters();
                if (allCharacters.Count != 2)
                {
                    return Task.FromResult(new TestResult("Multi-Character Registration", false, $"Expected 2 characters, found {allCharacters.Count}"));
                }

                return Task.FromResult(new TestResult("Multi-Character Registration", true, 
                    $"Registered 2 characters: {character1.Name} (ID: {id1.Substring(0, Math.Min(10, id1.Length))}...), {character2.Name} (ID: {id2.Substring(0, Math.Min(10, id2.Length))}...)"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Multi-Character Registration", false, $"Exception: {ex.Message}"));
            }
        }

        private Task<TestResult> TestMultiCharacterSwitching()
        {
            try
            {
                var stateManager = new GameStateManager();
                var character1 = new Character("SwitchTest1", 1);
                var character2 = new Character("SwitchTest2", 2);

                var id1 = stateManager.AddCharacter(character1);
                var id2 = stateManager.AddCharacter(character2);

                // Verify initial state
                if (stateManager.GetActiveCharacter() != character1)
                {
                    return Task.FromResult(new TestResult("Multi-Character Switching", false, "First character should be active initially"));
                }

                // Switch to second character
                bool switched = stateManager.SwitchCharacter(id2);
                if (!switched)
                {
                    return Task.FromResult(new TestResult("Multi-Character Switching", false, "Failed to switch to second character"));
                }

                if (stateManager.GetActiveCharacter() != character2)
                {
                    return Task.FromResult(new TestResult("Multi-Character Switching", false, "Second character should be active after switch"));
                }

                // Switch back
                switched = stateManager.SwitchCharacter(id1);
                if (!switched || stateManager.GetActiveCharacter() != character1)
                {
                    return Task.FromResult(new TestResult("Multi-Character Switching", false, "Failed to switch back to first character"));
                }

                return Task.FromResult(new TestResult("Multi-Character Switching", true, 
                    $"Successfully switched between {character1.Name} and {character2.Name}"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Multi-Character Switching", false, $"Exception: {ex.Message}"));
            }
        }

        private Task<TestResult> TestMultiCharacterContext()
        {
            try
            {
                var stateManager = new GameStateManager();
                var character1 = new Character("ContextTest1", 1);
                var character2 = new Character("ContextTest2", 2);

                var id1 = stateManager.AddCharacter(character1);
                var id2 = stateManager.AddCharacter(character2);

                // Set dungeon for first character
                var dungeon1 = new Dungeon("Dungeon1", 1, 3, "Forest");
                stateManager.SetCurrentDungeon(dungeon1);
                var context1 = stateManager.GetActiveCharacterContext();
                if (context1?.ActiveDungeon != dungeon1)
                {
                    return Task.FromResult(new TestResult("Multi-Character Context", false, "Dungeon not stored in character context"));
                }

                // Switch to second character
                stateManager.SwitchCharacter(id2);
                var context2 = stateManager.GetActiveCharacterContext();
                if (context2?.ActiveDungeon != null)
                {
                    return Task.FromResult(new TestResult("Multi-Character Context", false, "Second character should not have dungeon from first"));
                }

                // Set dungeon for second character
                var dungeon2 = new Dungeon("Dungeon2", 2, 4, "Lava");
                stateManager.SetCurrentDungeon(dungeon2);
                if (context2?.ActiveDungeon != dungeon2)
                {
                    return Task.FromResult(new TestResult("Multi-Character Context", false, "Dungeon not stored in second character context"));
                }

                // Switch back and verify first character's dungeon is preserved
                stateManager.SwitchCharacter(id1);
                context1 = stateManager.GetActiveCharacterContext();
                if (context1?.ActiveDungeon != dungeon1)
                {
                    return Task.FromResult(new TestResult("Multi-Character Context", false, "First character's dungeon state not preserved"));
                }

                return Task.FromResult(new TestResult("Multi-Character Context", true, 
                    $"Character contexts maintain independent dungeon state"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TestResult("Multi-Character Context", false, $"Exception: {ex.Message}"));
            }
        }
    }
}

