using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    public class RisingDeadTest
    {
        public static void TestEnvironmentalActions()
        {
            Console.WriteLine("=== Testing Environmental Actions Against Different Enemy Types ===");
            Console.WriteLine();
            
            // Create different enemy types for testing
            var livingEnemy = new Enemy("Goblin", 1, 50, 8, 6, 4, 4, 0, PrimaryAttribute.Strength, true);
            var undeadEnemy = new Enemy("Skeleton", 1, 50, 8, 6, 4, 4, 0, PrimaryAttribute.Strength, false);
            var character = new Character("Test Player", 1);
            
            // Get all environmental actions from the action loader
            var allActions = ActionLoader.GetAllActions();
            var environmentalActions = allActions.Where(a => a.Tags.Contains("environment")).ToList();
            
            if (environmentalActions.Count == 0)
            {
                Console.WriteLine("❌ No environmental actions found in the system!");
                return;
            }
            
            Console.WriteLine($"Found {environmentalActions.Count} environmental actions to test:");
            foreach (var action in environmentalActions)
            {
                Console.WriteLine($"  - {action.Name}");
            }
            Console.WriteLine();
            
            int totalTests = 0;
            int passedTests = 0;
            
            // Test each environmental action
            foreach (var action in environmentalActions)
            {
                Console.WriteLine($"--- Testing {action.Name} ---");
                
                // Reset all entities
                character.IsWeakened = false;
                character.IsBleeding = false;
                livingEnemy.IsWeakened = false;
                livingEnemy.IsBleeding = false;
                undeadEnemy.IsWeakened = false;
                undeadEnemy.IsBleeding = false;
                
                // Create environment with this action
                var environment = new Environment("Test Environment", "A test environment", true, "test");
                environment.AddAction(action, 1.0);
                
                // Test targets
                var targets = new List<Entity> { character, livingEnemy, undeadEnemy };
                
                // Execute the environmental action
                try
                {
                    string result = Combat.ExecuteAreaOfEffectAction(environment, targets, environment, action);
                    Console.WriteLine($"Result: {result}");
                    
                    // Analyze the results based on action type and enemy types
                    bool testPassed = AnalyzeEnvironmentalActionResults(action, character, livingEnemy, undeadEnemy);
                    
                    if (testPassed)
                    {
                        Console.WriteLine("✅ PASS");
                        passedTests++;
                    }
                    else
                    {
                        Console.WriteLine("❌ FAIL");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ ERROR: {ex.Message}");
                }
                
                totalTests++;
                Console.WriteLine();
            }
            
            // Summary
            Console.WriteLine("=== TEST SUMMARY ===");
            Console.WriteLine($"Total Tests: {totalTests}");
            Console.WriteLine($"Passed: {passedTests}");
            Console.WriteLine($"Failed: {totalTests - passedTests}");
            Console.WriteLine($"Success Rate: {(double)passedTests / totalTests * 100:F1}%");
            
            if (passedTests == totalTests)
            {
                Console.WriteLine("🎉 ALL ENVIRONMENTAL ACTION TESTS PASSED!");
            }
            else
            {
                Console.WriteLine("💥 SOME ENVIRONMENTAL ACTION TESTS FAILED!");
            }
        }
        
        private static bool AnalyzeEnvironmentalActionResults(Action action, Character character, Enemy livingEnemy, Enemy undeadEnemy)
        {
            bool testPassed = true;
            
            // Check if the action should affect living vs undead differently
            if (action.Name.ToUpper().Contains("RISING DEAD") || action.Name.ToUpper().Contains("UNDEAD"))
            {
                // Rising Dead should affect living creatures but not undead
                if (action.CausesWeaken)
                {
                    if (!character.IsWeakened)
                    {
                        Console.WriteLine("  ❌ Character should be weakened by Rising Dead");
                        testPassed = false;
                    }
                    if (!livingEnemy.IsWeakened)
                    {
                        Console.WriteLine("  ❌ Living enemy should be weakened by Rising Dead");
                        testPassed = false;
                    }
                    if (undeadEnemy.IsWeakened)
                    {
                        Console.WriteLine("  ❌ Undead enemy should NOT be weakened by Rising Dead");
                        testPassed = false;
                    }
                }
            }
            else
            {
                // Other environmental actions should affect all targets equally
                if (action.CausesWeaken)
                {
                    if (!character.IsWeakened)
                    {
                        Console.WriteLine("  ❌ Character should be weakened");
                        testPassed = false;
                    }
                    if (!livingEnemy.IsWeakened)
                    {
                        Console.WriteLine("  ❌ Living enemy should be weakened");
                        testPassed = false;
                    }
                    if (!undeadEnemy.IsWeakened)
                    {
                        Console.WriteLine("  ❌ Undead enemy should be weakened");
                        testPassed = false;
                    }
                }
                
                if (action.CausesBleed)
                {
                    if (!character.IsBleeding)
                    {
                        Console.WriteLine("  ❌ Character should be bleeding");
                        testPassed = false;
                    }
                    if (!livingEnemy.IsBleeding)
                    {
                        Console.WriteLine("  ❌ Living enemy should be bleeding");
                        testPassed = false;
                    }
                    if (!undeadEnemy.IsBleeding)
                    {
                        Console.WriteLine("  ❌ Undead enemy should be bleeding");
                        testPassed = false;
                    }
                }
            }
            
            return testPassed;
        }
    }
}
